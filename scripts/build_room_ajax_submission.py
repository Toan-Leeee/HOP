from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.section import WD_SECTION_START, WD_ORIENT
from docx.oxml.ns import qn
import json
import shutil

root = Path(__file__).resolve().parents[1]
folder = root / 'Nop_ca_nhan_Le_Xuan_Toan' / 'Ajax_Room'
for name in ['Areas/Admin/Controllers/RoomController.cs', 'Areas/Admin/Views/Room/Ajax.cshtml', 'wwwroot/js/room-ajax.js']:
    target = folder / 'Module_Code' / name
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(root / 'MeetingScheduler.Web' / name, target)
doc = Document()
s = doc.sections[0]
s.page_width, s.page_height = Cm(21), Cm(29.7)
s.left_margin = s.right_margin = Cm(2)
s.top_margin = s.bottom_margin = Cm(1.8)
for name, size in [('Normal', 12), ('Title', 18), ('Heading 1', 14)]:
    style = doc.styles[name]
    style.font.name = 'Times New Roman'
    style.font.size = Pt(size)
    style.font.color.rgb = RGBColor(0, 0, 0)
    style.paragraph_format.space_after = Pt(6)
    style.paragraph_format.line_spacing = 1.1
    fonts = style.element.rPr.rFonts
    if fonts is not None:
        for key in list(fonts.attrib):
            if 'Theme' in key:
                del fonts.attrib[key]
        fonts.set(qn('w:eastAsia'), 'Times New Roman')
for line in (folder / 'BAO_CAO_AJAX_ROOM.md').read_text(encoding='utf-8').splitlines():
    if not line:
        continue
    if line.startswith('# '):
        doc.add_paragraph(line[2:], 'Title')
    elif line.startswith('## '):
        doc.add_heading(line[3:], level=1)
    else:
        doc.add_paragraph(line[2:] if line.startswith('- ') else line)
shots = [
    ('01_Danh_sach_Ajax', 'Danh sách phòng tải bằng XMLHttpRequest'),
    ('02_Fetch_Tim_kiem', 'Tìm kiếm và cập nhật bảng bằng fetch'),
    ('03_Phan_trang', 'Tải trang thứ hai không reload trang'),
    ('04_Them_phong', 'Biểu mẫu thêm phòng'),
    ('05_Ket_qua_them', 'Kết quả lưu thành công qua jQuery AJAX'),
    ('06_Sua_bao_tri', 'Sửa sức chứa và chuyển sang bảo trì'),
    ('07_Validation_trung_ten', 'Lỗi tên trùng hiển thị trong modal'),
    ('08_Ngung_su_dung', 'Phòng ngừng dùng vẫn được lưu trong database'),
    ('09_Khoi_phuc', 'Khôi phục phòng và tải lại bảng'),
]
s = doc.add_section(WD_SECTION_START.NEW_PAGE)
s.orientation = WD_ORIENT.LANDSCAPE
s.page_width, s.page_height = Cm(29.7), Cm(21)
s.left_margin = s.right_margin = Cm(1.2)
s.top_margin = s.bottom_margin = Cm(1.2)
for i, (name, caption) in enumerate(shots, 1):
    if i > 1:
        doc.add_page_break()
    doc.add_heading(f'Minh chứng {i} {caption}', level=1)
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.add_run().add_picture(str(folder / 'Minh_chung' / (name + '.png')), width=Cm(23.5))
    p._p.xpath('.//wp:docPr')[0].set('descr', caption)
doc.core_properties.author = 'Le Xuan Toan'
doc.core_properties.title = 'Thuc hanh AJAX module Room'
output = folder / '23A1001D0361_Le_Xuan_Toan_AJAX_Room.docx'
doc.save(output)
skip = {'bin', 'obj', '.vs', 'Logs', 'node_modules', '.git', '__pycache__'}
with ZipFile(folder / '23A1001D0361_Le_Xuan_Toan_AJAX_Source.zip', 'w', ZIP_DEFLATED) as z:
    for name in ['MeetingScheduler.Web', 'MeetingScheduler.Checks']:
        for p in sorted((root / name).rglob('*')):
            if p.is_file() and not skip.intersection(p.relative_to(root).parts) and not p.name.endswith('.user'):
                z.write(p, 'MeetingScheduler/' + p.relative_to(root).as_posix())
    for p in root.glob('*.sln'):
        z.write(p, 'MeetingScheduler/' + p.name)
    z.write(root / 'scripts/check_room_ajax.cjs', 'MeetingScheduler/scripts/check_room_ajax.cjs')
    z.write(folder / 'BAO_CAO_AJAX_ROOM.md', 'MeetingScheduler/BAO_CAO_AJAX_ROOM.md')
with ZipFile(folder / '23A1001D0361_Le_Xuan_Toan_AJAX_Bai_nop.zip', 'w', ZIP_DEFLATED) as z:
    for p in sorted(folder.rglob('*')):
        if p.is_file() and p.name != '23A1001D0361_Le_Xuan_Toan_AJAX_Bai_nop.zip' and p.name != 'ERROR.png' and not p.name.startswith('~$'):
            z.write(p, 'Le_Xuan_Toan_AJAX_Room/' + p.relative_to(folder).as_posix())
print(output)
print('Images embedded:', len(doc.inline_shapes))
