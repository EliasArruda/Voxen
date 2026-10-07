#!/usr/bin/env python3
"""Archive a published Voxen directory with Linux desktop installation support."""
import argparse
from pathlib import Path
import re
import shutil
import tarfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--rid', choices=['linux-x64', 'win-x64'], required=True)
parser.add_argument('--version', required=True)
parser.add_argument('--input', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
if not re.fullmatch(r'\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?', args.version):
    parser.error('Version must be a semantic version without a v prefix.')
root = Path(__file__).resolve().parent.parent
source = args.input.resolve()
executable = source / ('Voxen' if args.rid == 'linux-x64' else 'Voxen.exe')
if not executable.is_file():
    parser.error(f'Published executable missing: {executable}')
args.output.mkdir(parents=True, exist_ok=True)
name = f'Voxen-{args.version}-{args.rid}'
if args.rid == 'linux-x64':
    shutil.copy2(root / 'packaging/install.sh', source / 'install.sh')
    (source / 'install.sh').chmod(0o755)
    with tarfile.open(args.output / f'{name}.tar.gz', 'w:gz') as archive:
        archive.add(source, arcname=name)
else:
    shutil.make_archive(str(args.output / name), 'zip', root_dir=source.parent, base_dir=source.name)
print(f'Packaged {name}')
