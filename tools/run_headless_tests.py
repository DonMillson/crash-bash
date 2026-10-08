#!/usr/bin/env python3
"""Compile and execute the same Arkenoid simulation sources used by Unity.

Usage: python tools/run_headless_tests.py --dotnet /path/to/dotnet
The Roslyn entry point also works in containers where the SDK CLI cannot read
process start times from /proc. No Unity API substitutes are used in these tests.
"""
import argparse
import json
import pathlib
import shutil
import subprocess
import tempfile

ap = argparse.ArgumentParser(description=__doc__)
ap.add_argument('--dotnet', default=shutil.which('dotnet'))
args = ap.parse_args()
if not args.dotnet:
    ap.error('Install the .NET 8 SDK or pass --dotnet')
root = pathlib.Path(args.dotnet).resolve().parent
project = pathlib.Path(__file__).resolve().parent.parent
sdk = sorted((root/'sdk').glob('8.*/Roslyn/bincore/csc.dll'))[-1]
refs = sorted((root/'packs/Microsoft.NETCore.App.Ref').glob('8.*/ref/net8.0'))[-1]
runtime = refs.parent.parent.name
files = list((project/'Assets/CrashBash/Scripts/Simulation').glob('*.cs')) + [project/p for p in [
    'Assets/CrashBash/Scripts/Core/PlayerIdentity.cs',
    'Assets/CrashBash/Scripts/Gameplay/ArkenoidHeroState.cs',
    'Assets/CrashBash/Scripts/Gameplay/ArkenoidVariant.cs', 'Tests/Headless/Program.cs']]
with tempfile.TemporaryDirectory(prefix='arkenoid-tests-') as tmp:
    output = pathlib.Path(tmp)/'Arkenoid.Headless.dll'
    rsp = pathlib.Path(tmp)/'compile.rsp'
    options = ['-nologo','-nostdlib+','-target:exe','-langversion:9.0','-warnaserror+', f'-out:"{output}"']
    options += [f'-r:"{p}"' for p in refs.glob('*.dll')]
    options += [f'"{p}"' for p in files]
    rsp.write_text('\n'.join(options))
    output.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{
        'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':runtime}}}))
    subprocess.run([str(root/'dotnet'),str(sdk),'-noconfig','@'+str(rsp)],check=True,cwd=project)
    subprocess.run([str(root/'dotnet'),str(output)],check=True,cwd=project)
