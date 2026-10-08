#!/usr/bin/env python3
"""Fetch genuine Unity managed references into a temporary directory.

The official Linux editor archive is read only for UnityEngine/UnityEditor DLLs.
Nothing is imported into the Unity project or redistributed by this tool.
This supports C# API compilation, not editor execution or a player build.
"""
import argparse
import concurrent.futures
import hashlib
import json
import pathlib
import re
import shutil
import tarfile
import tempfile
import urllib.request

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--output', required=True, type=pathlib.Path)
args = parser.parse_args()
project = pathlib.Path(__file__).resolve().parent.parent
version_file = (project/'ProjectSettings/ProjectVersion.txt').read_text()
version = re.search(r'^m_EditorVersion: (\S+)\s*$', version_file, re.M)
revision = re.search(r'^m_EditorVersionWithRevision: (\S+) \(([0-9a-f]{12})\)\s*$', version_file, re.M)
if not version or not revision or version[1] != revision[1]:
    parser.error('ProjectVersion.txt must contain a consistent version and revision.')
# Verified at https://unity.com/releases/editor/whats-new/6000.0.60f1
if (version[1], revision[2]) != ('6000.0.60f1', '61dfb374e36f'):
    parser.error('Verify the official download link before changing the pinned Unity version.')
url = ('https://download.unity3d.com/download_unity/61dfb374e36f/'
       'LinuxEditorInstaller/Unity-6000.0.60f1.tar.xz')
output = args.output.resolve()
output.mkdir(parents=True, exist_ok=True)
if any(output.iterdir()):
    parser.error('--output must be empty to avoid mixing Unity versions.')
copied = {}
with tempfile.TemporaryDirectory(prefix='unity-managed-fetch-') as temporary:
    archive = pathlib.Path(temporary)/'Unity.tar.xz'
    print('Downloading official Unity ' + version[1] + ' reference archive.', flush=True)
    headers = {'User-Agent': 'CrashBash-Arkenoid-API-Check', 'Accept-Encoding': 'identity'}
    # Unity's resumable CDN can serve independent ranges much faster than one
    # throttled stream. Probe first; a server that ignores Range gets one download.
    total = 0
    probe = urllib.request.Request(url, headers={**headers, 'Range': 'bytes=0-0'})
    with urllib.request.urlopen(probe, timeout=60) as response:
        content_range = response.headers.get('Content-Range', '')
        match = re.fullmatch(r'bytes 0-0/(\d+)', content_range)
        if response.status == 206 and match and response.read(2) == bytes([0xFD]):
            total = int(match[1])
    if total:
        count = 8
        size = (total + count - 1)//count
        def download_part(index):
            start, end = index*size, min(total, (index+1)*size)-1
            path = pathlib.Path(temporary)/f'archive-{index}.part'
            request = urllib.request.Request(url, headers={**headers, 'Range': f'bytes={start}-{end}'})
            with urllib.request.urlopen(request, timeout=60) as response:
                expected = f'bytes {start}-{end}/{total}'
                if response.status != 206 or response.headers.get('Content-Range') != expected:
                    raise RuntimeError('CDN did not honor the requested archive range.')
                with path.open('wb') as target:
                    shutil.copyfileobj(response, target, length=8*1024*1024)
            if path.stat().st_size != end-start+1:
                raise RuntimeError('Incomplete archive range download.')
            return path
        with concurrent.futures.ThreadPoolExecutor(max_workers=count) as pool:
            parts = list(pool.map(download_part, range(count)))
        with archive.open('wb') as target:
            for path in parts:
                with path.open('rb') as source:
                    shutil.copyfileobj(source, target, length=8*1024*1024)
                path.unlink()
        print(f'Downloaded {total} bytes in {count} verified ranges.', flush=True)
    else:
        request = urllib.request.Request(url, headers=headers)
        with urllib.request.urlopen(request, timeout=60) as response, archive.open('wb') as target:
            shutil.copyfileobj(response, target, length=8*1024*1024)
    archive_digest = hashlib.sha256()
    with archive.open('rb') as source:
        for block in iter(lambda: source.read(8*1024*1024), b''):
            archive_digest.update(block)
    print('Reading managed assemblies; no editor executables are extracted.', flush=True)
    with tarfile.open(archive, mode='r|xz') as package:
        for member in package:
            if not member.isfile() or not re.search(r'(^|/)Editor/Data/Managed/', member.name):
                continue
            name = pathlib.PurePosixPath(member.name).name
            if not name.endswith('.dll') or not name.startswith(('UnityEngine', 'UnityEditor')):
                continue
            # Prefer the canonical managed path over nested copies of a module.
            depth = len(pathlib.PurePosixPath(member.name).parts)
            if name in copied and depth >= copied[name]['depth']:
                continue
            source = package.extractfile(member)
            if source is None:
                raise RuntimeError('Missing assembly payload: ' + member.name)
            destination = output/name
            with source, destination.open('wb') as target:
                shutil.copyfileobj(source, target)
            copied[name] = {'archive_path': member.name, 'depth': depth,
                            'sha256': hashlib.sha256(destination.read_bytes()).hexdigest()}
if not {'UnityEngine.CoreModule.dll', 'UnityEditor.CoreModule.dll'} <= copied.keys():
    raise RuntimeError('Official archive did not provide the required engine/editor core modules.')
manifest = {'unity_version': version[1], 'revision': revision[2], 'official_url': url,
            'archive_sha256': archive_digest.hexdigest(), 'assemblies': copied}
(output/'reference-provenance.json').write_text(json.dumps(manifest, indent=2) + '\n')
print(f'Fetched {len(copied)} genuine Unity assemblies to temporary reference storage.')
