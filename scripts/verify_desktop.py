#!/usr/bin/env python3
"""Verify shipped desktop icons and the Windows executable's GUI subsystem/resources."""
import argparse
from pathlib import Path
import struct

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--rid', choices=['linux-x64', 'win-x64'], required=True)
parser.add_argument('--input', type=Path, required=True)
args = parser.parse_args()
folder = args.input
for name in ['voxen.png', 'voxen.ico']:
    if not (folder / 'Assets' / name).is_file():
        raise RuntimeError(f'Missing desktop icon: {name}')
if args.rid == 'win-x64':
    data = (folder / 'Voxen.exe').read_bytes()
    u16 = lambda offset: struct.unpack_from('<H', data, offset)[0]
    u32 = lambda offset: struct.unpack_from('<I', data, offset)[0]
    pe = u32(0x3c)
    if data[pe:pe+4] != b'PE\0\0':
        raise RuntimeError('Windows executable is not PE')
    optional = pe + 24
    if u16(optional + 68) != 2:
        raise RuntimeError('Windows executable must use GUI subsystem, without a console')
    directories = optional + (112 if u16(optional) == 0x20b else 96)
    resource_rva = u32(directories + 16)
    sections = optional + u16(pe + 20)
    resource = None
    for index in range(u16(pe + 6)):
        section = sections + index * 40
        virtual_size, virtual_address, raw_size, raw_offset = struct.unpack_from('<IIII', data, section + 8)
        if virtual_address <= resource_rva < virtual_address + max(virtual_size, raw_size):
            resource = raw_offset + resource_rva - virtual_address
            break
    if resource is None:
        raise RuntimeError('Windows executable has no resource directory')
    count = u16(resource + 12) + u16(resource + 14)
    types = {u32(resource + 16 + index * 8) for index in range(count)}
    if not {3, 14}.issubset(types):
        raise RuntimeError('Windows executable lacks embedded icon and icon group')
    print('PASS Windows GUI subsystem and embedded executable icon')
print('PASS native window icon files packaged')
