from copy import deepcopy
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from io import BytesIO
import base64
from lxml import etree
from docx import Document
from docx.shared import Cm, Pt
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

root = Path(__file__).resolve().parents[1]
folder = root / 'Nop_ca_nhan_Le_Xuan_Toan' / 'Ajax_Room'
source = folder / '23A1001D0361_Le_Xuan_Toan_AJAX_Room.docx'
output = folder / '23A1001D0361_Le_Xuan_Toan_AJAX_Du_Anh_Code_Va_Chay.docx'
try:
    doc = Document(source)
except PermissionError:
    # Read the current Word snapshot without closing or overwriting the user's document.
    flat = etree.parse(str(root / 'docs/qa/ajax_word/current_report.xml'))
    ns = {'pkg': 'http://schemas.microsoft.com/office/2006/xmlPackage'}
    package_bytes = BytesIO()
    with ZipFile(package_bytes, 'w', ZIP_DEFLATED) as archive:
        types_ns = 'http://schemas.openxmlformats.org/package/2006/content-types'
        types = etree.Element('{%s}Types' % types_ns, nsmap={None: types_ns})
        for part in flat.findall('pkg:part', ns):
            name = part.get('{%s}name' % ns['pkg']).lstrip('/')
            etree.SubElement(types, '{%s}Override' % types_ns, PartName='/' + name,
                             ContentType=part.get('{%s}contentType' % ns['pkg']))
            xml_data = part.find('pkg:xmlData', ns)
            binary = part.find('pkg:binaryData', ns)
            if xml_data is not None:
                data = etree.tostring(xml_data[0], encoding='UTF-8', xml_declaration=True)
            elif binary is not None:
                data = base64.b64decode(binary.text or '')
            else:
                continue
            archive.writestr(name, data)
        archive.writestr('[Content_Types].xml', etree.tostring(types, encoding='UTF-8', xml_declaration=True))
    package_bytes.seek(0)
    doc = Document(package_bytes)
target = next(p._p for p in doc.paragraphs if p.text.startswith('3 Data Flow'))
original = set(doc._element.body)
portrait = deepcopy(doc.sections[0]._sectPr)
landscape = deepcopy(doc.sections[-1]._sectPr)

def section_end(properties):
    para = doc.add_paragraph()
    para.paragraph_format.space_after = Pt(0)
    para.paragraph_format.space_before = Pt(0)
    section_type = properties.find(qn('w:type'))
    if section_type is None:
        section_type = OxmlElement('w:type')
        properties.insert(0, section_type)
    section_type.set(qn('w:val'), 'nextPage')
    para._p.get_or_add_pPr().append(properties)

section_end(portrait)
shots = [
    ('01_JS_Render_DOM', 'Render bảng từ JSON', 'wwwroot/js/room-ajax.js',
     'Yêu cầu UI Update: render(data) dùng createElement và textContent để dựng bảng, trạng thái và số trang; dữ liệu không được ghép vào innerHTML.'),
    ('02_JS_XHR_Fetch', 'XMLHttpRequest và fetch', 'wwwroot/js/room-ajax.js',
     'Lần đầu và phân trang dùng XMLHttpRequest. Tìm kiếm dùng fetch, response.json và gọi render(data). sequence và abort ngăn phản hồi cũ ghi đè kết quả mới.'),
    ('03_JS_jQuery_Create', 'Thêm và sửa bằng jQuery AJAX', 'wwwroot/js/room-ajax.js',
     'write() dùng $.ajax POST. Submit bị chặn điều hướng bằng preventDefault; serialize gửi biểu mẫu và token; JSON thành công đóng modal và tải lại bảng.'),
    ('04_JS_Delete_Search_Pagination', 'Xóa mềm tìm kiếm và phân trang', 'wwwroot/js/room-ajax.js',
     'Nút ngừng dùng/khôi phục gọi write() kèm id và token. Tìm kiếm debounce gọi load(fetch); nút trước/sau đổi page và gọi load() mặc định XMLHttpRequest.'),
    ('05_Controller_ListJson_SaveJson', 'Controller trả danh sách JSON', 'Areas/Admin/Controllers/RoomController.cs',
     'ListJson nhận RoomFilter, gọi Service, điều chỉnh trang ngoài phạm vi và trả items, totalCount, page, pageSize, totalPages; không trả Razor HTML.'),
    ('06_Controller_Save_Disable_Restore', 'Controller ghi dữ liệu và trả JSON', 'Areas/Admin/Controllers/RoomController.cs',
     'SaveJson kiểm tra ModelState và mã phòng, gọi SaveAsync; DisableJson gọi DisableAsync; RestoreJson đặt IsDisabled=false. Lỗi trả HTTP 400 hoặc 404 với JSON.'),
    ('07_RoomService_QueryPipeline', 'LINQ lọc và sắp xếp', 'Services/RoomService.cs',
     'QueryAsync dựng AsNoTracking, Where, OrderBy, Select rồi gọi ServiceSupport.PageAsync; dữ liệu vẫn được xử lý ở server qua EF Core.'),
    ('08_EFCore_Count_Skip_Take', 'Phân trang bằng EF Core', 'Services/ServiceSupport.cs',
     'PageAsync đếm CountAsync trước phân trang, sau đó Skip((page-1)*size), Take(size), ToListAsync. Đây là helper dùng chung được module Room gọi.'),
    ('09_Razor_Ajax_Table', 'Razor kết nối giao diện với JavaScript', 'Areas/Admin/Views/Room/Ajax.cshtml',
     'data-list/save/disable/restore chứa URL endpoint; form bộ lọc, tbody room-rows, nút phân trang và antiforgery token được JavaScript sử dụng.'),
]
for index, (filename, title, codefile, note) in enumerate(shots, 1):
    if index > 1:
        doc.add_page_break()
    heading = doc.add_heading(f'Ảnh code {index} {title}', level=1)
    heading.paragraph_format.space_before = Pt(0)
    heading.paragraph_format.space_after = Pt(4)
    para = doc.add_paragraph(f'File: {codefile}. {note}')
    para.paragraph_format.space_after = Pt(4)
    para.paragraph_format.keep_with_next = True
    for run in para.runs:
        run.font.size = Pt(10)
    para = doc.add_paragraph()
    para.alignment = WD_ALIGN_PARAGRAPH.CENTER
    para.paragraph_format.line_spacing = 1
    para.paragraph_format.space_after = Pt(0)
    para.add_run().add_picture(str(folder / 'Anh_code' / (filename + '.jpg')), width=Cm(26.3))
    para._p.xpath('.//wp:docPr')[0].set('descr', title + '. ' + note)
section_end(landscape)
new_nodes = [node for node in doc._element.body if node not in original]
for node in new_nodes:
    target.addprevious(node)
doc.save(output)
package = folder / '23A1001D0361_Le_Xuan_Toan_AJAX_Bai_nop_Du_anh.zip'
with ZipFile(package, 'w', ZIP_DEFLATED) as archive:
    for path in sorted(folder.rglob('*')):
        if not path.is_file() or path.name.startswith('~$') or path.name == 'ERROR.png':
            continue
        if path.suffix == '.zip' and path.name != '23A1001D0361_Le_Xuan_Toan_AJAX_Source.zip':
            continue
        if path.suffix == '.docx' and path != output:
            continue
        archive.write(path, 'Le_Xuan_Toan_AJAX_Room/' + path.relative_to(folder).as_posix())
print(output)
print('Total embedded images:', len(doc.inline_shapes))
print('Code images:', len(shots))
print(package)
