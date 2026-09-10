#!/usr/bin/env python3
"""Prepare a bounded Windows package from the supplied vendor demo."""
from pathlib import Path
import hashlib
import json
import shutil
import zipfile

base = Path(__file__).resolve().parent
workspace = base.parent
vendor = workspace / '已清洗数据/unpacked'
source = vendor / 'EtherCAT - 总线初始化-点位-Jog运动/CSharpDemo'
payload = base / 'payload'
for path in source.rglob('*'):
    relative = path.relative_to(source)
    if path.is_file() and not any(part in {'bin', 'obj'} for part in relative.parts):
        target = payload / 'source/EtherCATDemo' / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, target)

project = payload / 'source/EtherCATDemo/CSharpDemo.csproj'
text = project.read_text(encoding='utf-8-sig')
text = text.replace("<Platform Condition=\" '$(Platform)' == '' \">x86</Platform>",
                    "<Platform Condition=\" '$(Platform)' == '' \">x64</Platform>")
text = text.replace('processorArchitecture=x86', 'processorArchitecture=AMD64')
project.write_text(text, encoding='utf-8-sig')

form = payload / 'source/EtherCATDemo/Form1.cs'
text = form.read_text(encoding='utf-8-sig')
text = text.replace('public partial class Form1 : Form\n    {',
                    'public partial class Form1 : Form\n    {\n        private bool cardOpened = false;')
text = text.replace('private void timer1_Tick(object sender, EventArgs e)\n        {',
                    'private void timer1_Tick(object sender, EventArgs e)\n        {\n'
                    '            if (!cardOpened)\n            {\n'
                    '                label_InitStatus.Text = "未连接控制卡";\n'
                    '                labelPrfPos.Text = "--";\n'
                    '                label_SlaveCount.Text = "--";\n'
                    '                return;\n            }')
text = text.replace('            if (iRes == 0)\n',
                    '            cardOpened = (iRes == 0);\n            if (iRes == 0)\n', 1)
text = text.replace('当前模式{0:G}，子步{0:G}', '当前模式{1:G}，子步{2:G}')
form.write_text(text, encoding='utf-8-sig')
designer = payload / 'source/EtherCATDemo/Form1.Designer.cs'
text = designer.read_text(encoding='utf-8-sig').replace('this.Text = "Form1";',
                    'this.Text = "EtherCAT Demo - ThinkBook x64";')
designer.write_text(text, encoding='utf-8-sig')

for name in ['MultiCard.dll', 'MultiCardCLR.dll', 'MultiCardCS.dll']:
    original = source / 'bin/x64/Debug' / name
    for folder in ['app', 'source/EtherCATDemo/bin/Debug']:
        target = payload / folder / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(original, target)

for path in (vendor / 'Demo - VC++总线卡点位和插补运动/Demo').iterdir():
    if path.is_file() and path.suffix.lower() in {'.cpp', '.h', '.rc', '.vcxproj', '.filters', '.txt'}:
        target = payload / 'reference/VCppDemo' / path.name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, target)

manual = workspace / '博派科技EthCAT-I32-O16-MPG-GAS2总线运动控制卡用户手册V1.0.pdf'
(payload / 'docs').mkdir(parents=True, exist_ok=True)
shutil.copy2(manual, payload / 'docs/Controller-Manual.pdf')
shutil.copy2(base / 'README.md', payload / 'README.md')
for path in (base / 'scripts').glob('*'):
    target = payload / 'scripts' / path.name
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(path, target)

entries = []
for path in sorted(payload.rglob('*')):
    if path.is_file() and path != payload / 'manifest.json':
        entries.append({'path': path.relative_to(payload).as_posix(), 'size': path.stat().st_size,
                        'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
(payload / 'manifest.json').write_text(json.dumps(entries, indent=2, ensure_ascii=False), encoding='utf-8')
archive = base / 'thinkbook-demo.zip'
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as output:
    for path in sorted(payload.rglob('*')):
        if path.is_file():
            output.write(path, path.relative_to(payload).as_posix())
print(json.dumps({'files': len(entries), 'archive': str(archive), 'bytes': archive.stat().st_size,
                  'sha256': hashlib.sha256(archive.read_bytes()).hexdigest()}, ensure_ascii=False))
