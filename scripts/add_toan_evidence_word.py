from pathlib import Path
from docx import Document
from docx.shared import Cm, Pt
from docx.enum.section import WD_SECTION_START, WD_ORIENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml.ns import qn

root = Path(__file__).resolve().parents[1]
folder = root / 'Nop_ca_nhan_Le_Xuan_Toan'
source = folder / '23A1001D0361_Le_Xuan_Toan_DataFlow_Room.docx'
output = folder / '23A1001D0361_Le_Xuan_Toan_DataFlow_Room_Co_Minh_Chung.docx'
images = Path('C:/Users/acer/OneDrive/Desktop/Minh_chung')
items = [
    ('01_Room_Model.jpg', 'Entity Room', 'Các thuộc tính phòng họp, khóa chính và ràng buộc dữ liệu.'),
    ('02_RoomService_QueryPipeline.jpg', 'Query Pipeline', 'Tìm kiếm, lọc trạng thái và sức chứa, sắp xếp và gọi phân trang.'),
    ('03_RoomService_Save_Disable.jpg', 'Thêm sửa và ngừng sử dụng phòng', 'Kiểm tra dữ liệu, lưu phòng và vô hiệu hóa mềm bằng IsDisabled.'),
    ('04_ServiceSupport_Paging.jpg', 'Phân trang và lưu dữ liệu', 'Helper dùng chung được Room sử dụng: CountAsync, Skip, Take, transaction và audit.'),
    ('05_RoomController_Index_Create_Edit.jpg', 'Controller danh sách và thêm phòng', 'Nhận yêu cầu HTTP, kiểm tra ModelState và gọi RoomService.'),
    ('06_RoomController_Edit_Delete_Restore.jpg', 'Controller sửa và khôi phục phòng', 'Các action cập nhật, ngừng sử dụng và khôi phục phòng họp.'),
    ('07_RoomViewModels_Filter_Validation.jpg', 'ViewModel và bộ lọc', 'RoomFilter và RoomViewModel với các thuộc tính lọc và Data Annotations.'),
]
doc = Document(source)
section = doc.add_section(WD_SECTION_START.NEW_PAGE)
section.orientation = WD_ORIENT.LANDSCAPE
section.page_width, section.page_height = Cm(29.7), Cm(21)
section.top_margin = section.bottom_margin = Cm(1.2)
section.left_margin = section.right_margin = Cm(1.2)
for n, (filename, title, caption) in enumerate(items, 1):
    if n > 1:
        doc.add_page_break()
    p = doc.add_paragraph(f'Minh chứng {n} {title}', 'Heading 1')
    p.paragraph_format.space_before = Pt(0)
    p.paragraph_format.space_after = Pt(4)
    p = doc.add_paragraph(caption)
    p.paragraph_format.space_after = Pt(5)
    p.paragraph_format.keep_with_next = True
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_after = Pt(0)
    p.paragraph_format.line_spacing = 1
    p.add_run().add_picture(str(images / filename), width=Cm(27))
    drawing = p._p.xpath('.//wp:docPr')[0]
    drawing.set('descr', f'Minh chứng {n}: {title}. {caption}')
for style in doc.styles:
    if style.type == 1 and style.font.name:
        fonts = style.element.rPr.rFonts
        if fonts is not None:
            for key in list(fonts.attrib):
                if 'Theme' in key:
                    del fonts.attrib[key]
            fonts.set(qn('w:eastAsia'), style.font.name)
doc.save(output)
print(output)
print(f'Embedded images: {len(doc.inline_shapes)}')
