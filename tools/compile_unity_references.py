#!/usr/bin/env python3
"""Type-check the existing game's C# sources against genuine Unity managed DLLs.

This is NOT a Unity editor import, shader check, PlayMode test or player build.
DLLs are supplied by a local Unity installation; none are vendored in the repo.
Usage: python tools/compile_unity_references.py --dotnet /path/to/dotnet
       --managed /path/to/Editor/Data/Managed [--editor]
"""
import argparse
import pathlib
import shutil
import subprocess
import tempfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet', default=shutil.which('dotnet'))
parser.add_argument('--managed', required=True, type=pathlib.Path)
parser.add_argument('--editor', action='store_true', help='Also compile the editor assembly; requires UnityEditor DLLs')
args = parser.parse_args()
if not args.dotnet:
    parser.error('Install the .NET 8 SDK or pass --dotnet')
dotnet_root = pathlib.Path(args.dotnet).resolve().parent
project = pathlib.Path(__file__).resolve().parent.parent
csc = sorted((dotnet_root/'sdk').glob('8.*/Roslyn/bincore/csc.dll'))[-1]
framework = sorted((dotnet_root/'packs/Microsoft.NETCore.App.Ref').glob('8.*/ref/net8.0'))[-1]
modules = {}
for path in sorted(args.managed.resolve().rglob('Unity*.dll'), key=lambda p: len(p.parts)):
    if path.name.startswith(('UnityEngine', 'UnityEditor')):
        modules.setdefault(path.name, path)
if 'UnityEngine.CoreModule.dll' not in modules:
    parser.error('No genuine UnityEngine.CoreModule.dll in the supplied directory')
if args.editor and 'UnityEditor.CoreModule.dll' not in modules:
    parser.error('--editor requires the genuine UnityEditor.CoreModule.dll')

with tempfile.TemporaryDirectory(prefix='arkenoid-unity-refs-') as temporary:
    directory = pathlib.Path(temporary)
    runtime = directory/'CrashBash.Arkenoid.dll'
    def compile_sources(output, sources, references):
        options = ['-nologo', '-nostdlib+', '-target:library', '-langversion:9.0', '-out:"'+str(output)+'"']
        options += ['-r:"'+str(p)+'"' for p in list(framework.glob('*.dll'))+references]
        options += ['"'+str(p)+'"' for p in sources]
        response = directory/(output.stem+'.rsp')
        response.write_text('\n'.join(options))
        subprocess.run([str(dotnet_root/'dotnet'),str(csc),'-noconfig','@'+str(response)],check=True,cwd=project)
    # The archive also ships legacy monolithic reference facades. Mixing those
    # definitions with CoreModule produces CS0433; Unity 6 uses modular APIs.
    engine = [p for name,p in modules.items() if name.startswith('UnityEngine') and name != 'UnityEngine.dll']
    sources = sorted((project/'Assets/CrashBash/Scripts').rglob('*.cs'))
    compile_sources(runtime, sources, engine)
    print(f'PASS: {len(sources)} runtime C# files compiled against {len(engine)} genuine UnityEngine assemblies.')
    if args.editor:
        editor = [p for name,p in modules.items() if name.startswith('UnityEditor') and name != 'UnityEditor.dll']
        compile_sources(directory/'CrashBash.Editor.dll', (project/'Assets/CrashBash/Editor').glob('*.cs'), engine+editor+[runtime])
        print('PASS: editor C# assembly compiled against genuine UnityEditor references.')
    print('Scope: C# reference compilation only; Unity import/rendering/player build NOT performed.')
