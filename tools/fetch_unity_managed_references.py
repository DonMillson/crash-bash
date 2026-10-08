#!/usr/bin/env python3
"""Fetch genuine Unity managed references into a temporary directory.

The official Linux editor archive is read only for UnityEngine/UnityEditor DLLs.
Nothing is imported into the Unity project or redistributed by this tool.
This supports C# API compilation, not editor execution or a player build.
"""
import argparse
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
    request = urllib.request.Request(url, headers={'User-Agent': 'CrashBash-Arkenoid-API-Check'})
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
