from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

root = Path(__file__).resolve().parents[1]
folder = root / 'Nop_ca_nhan_Nguyen_Van_Truong'
skip = {'bin', 'obj', '.vs', 'Logs', 'node_modules', '.git', '__pycache__'}
with ZipFile(folder / 'Nguyen_Van_Truong_Source_Meeting_RSVP.zip', 'w', ZIP_DEFLATED) as z:
    for name in ['MeetingScheduler.Web', 'MeetingScheduler.Checks']:
        for path in sorted((root / name).rglob('*')):
            if path.is_file() and not skip.intersection(path.relative_to(root).parts) and not path.name.endswith('.user'):
                z.write(path, 'MeetingScheduler/' + path.relative_to(root).as_posix())
    for path in root.glob('*.sln'):
        z.write(path, 'MeetingScheduler/' + path.name)
    z.write(folder / 'HUONG_DAN_NOP.md', 'MeetingScheduler/HUONG_DAN_NOP_TRUONG.md')
print('Source package created')
