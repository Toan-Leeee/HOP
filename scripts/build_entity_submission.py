from pathlib import Path
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

BASE = Path(__file__).resolve().parents[1]
OUTPUT = BASE / 'docs' / 'Nhom7_Xac_dinh_thuc_the_DataLayer.docx'
data = {
    'User': [
        ('UserId','int','PK, tự tăng; định danh người dùng.'),
        ('FullName','nvarchar(100)','Bắt buộc; họ và tên người dùng.'),
        ('Email','varchar(100)','Bắt buộc, duy nhất; dùng để đăng nhập.'),
        ('Phone','varchar(20)','Cho phép NULL; số điện thoại (bổ sung).'),
        ('PasswordHash','varchar(255)','Bắt buộc; lưu mật khẩu đã băm bằng PBKDF2.'),
        ('RoleId','int','FK đến Role.RoleId; bắt buộc.'),
        ('DepartmentId','int','FK đến Department.DepartmentId; bắt buộc.'),
        ('IsDisabled','bit','Mặc định 0; 1 là tài khoản bị khóa hoặc xóa mềm.')],
    'Role': [
        ('RoleId','int','PK; mã cố định: 1 = Admin, 2 = Organizer, 3 = Employee; không tự tăng.'),
        ('RoleName','nvarchar(50)','Bắt buộc, duy nhất; tên vai trò (bổ sung).')],
    'Department': [
        ('DepartmentId','int','PK, tự tăng; định danh phòng ban (chi tiết hóa từ SRS).'),
        ('DepartmentName','nvarchar(100)','Bắt buộc, duy nhất; tên phòng ban (chi tiết hóa từ SRS).'),
        ('IsDisabled','bit','Mặc định 0; dùng xóa mềm phòng ban theo SRS 6.3 (bổ sung trường).')],
    'Room': [
        ('RoomId','int','PK, tự tăng; định danh phòng họp.'),
        ('RoomName','nvarchar(100)','Bắt buộc, duy nhất; tên phòng họp.'),
        ('Location','nvarchar(200)','Cho phép NULL; vị trí phòng, chi tiết hóa mô tả SRS 6.1.'),
        ('Capacity','int','Bắt buộc; sức chứa phải lớn hơn 0.'),
        ('Equipment','nvarchar(500)','Cho phép NULL; trang thiết bị, chi tiết hóa SRS 6.1.'),
        ('Status','nvarchar(20)','Enum trong C#: Active hoặc Maintenance; EF Core lưu tên trạng thái.'),
        ('IsDisabled','bit','Mặc định 0; xóa mềm để giữ lịch sử họp theo SRS 6.3 (bổ sung trường).')],
    'Meeting': [
        ('MeetingId','int','PK, tự tăng; định danh cuộc họp.'),
        ('Title','nvarchar(200)','Bắt buộc; tiêu đề cuộc họp.'),
        ('Description','nvarchar(max)','Cho phép NULL; nội dung cuộc họp (bổ sung).'),
        ('StartTime','datetime','Bắt buộc; thời điểm bắt đầu.'),
        ('EndTime','datetime','Bắt buộc; phải lớn hơn StartTime.'),
        ('RoomId','int','FK đến Room.RoomId; phòng được đặt, bắt buộc.'),
        ('CreatedByUserId','int','FK đến User.UserId; người tổ chức, bắt buộc.'),
        ('Status','nvarchar(20)','Enum trong C#: Confirmed hoặc Cancelled; EF Core lưu tên trạng thái.'),
        ('RowVersion','rowversion','SQL Server tự sinh; token kiểm soát cập nhật đồng thời bằng [Timestamp] (bổ sung).'),
        ('CancelledAt','datetime','Cho phép NULL; thời điểm hủy cuộc họp (bổ sung).'),
        ('CancelledByUserId','int','FK tùy chọn đến User.UserId; cho phép NULL khi chưa hủy (bổ sung).'),
        ('CancelReason','nvarchar(500)','Cho phép NULL; lý do hủy cuộc họp (bổ sung).'),
        ('CreatedAt','datetime','Bắt buộc; thời điểm tạo, do ứng dụng gán (bổ sung).'),
        ('UpdatedAt','datetime','Cho phép NULL; thời điểm cập nhật gần nhất (bổ sung).')],
    'MeetingParticipant': [
        ('MeetingId','int','PK ghép và FK đến Meeting.MeetingId; bắt buộc.'),
        ('UserId','int','PK ghép và FK đến User.UserId; bắt buộc.'),
        ('Status','nvarchar(20)','Enum trong C#: Pending, Accepted hoặc Declined; lưu tên trạng thái RSVP.'),
        ('RespondedAt','datetime','Cho phép NULL; chưa có giá trị khi chưa phản hồi (bổ sung).')],
    'AuditLog': [
        ('AuditLogId','int','PK, tự tăng; định danh bản ghi nhật ký (bổ sung).'),
        ('UserId','int','FK đến User.UserId; người thực hiện thao tác (bổ sung).'),
        ('Action','nvarchar(100)','Bắt buộc; tên thao tác đã thực hiện (bổ sung).'),
        ('EntityName','nvarchar(100)','Bắt buộc; tên đối tượng được tác động (bổ sung).'),
        ('EntityId','int','Bắt buộc; mã đối tượng theo EntityName, không phải FK vật lý (bổ sung).'),
        ('CreatedAt','datetime','Bắt buộc; thời điểm ghi nhật ký (bổ sung).'),
        ('Details','nvarchar(max)','Cho phép NULL; thông tin chi tiết của thao tác (bổ sung).')]
}
roles = [
    ('User','Lưu tài khoản, thông tin cá nhân, mật khẩu băm, vai trò và phòng ban của người dùng.'),
    ('Role','Lưu danh mục vai trò Admin, Organizer và Employee để phân quyền truy cập.'),
    ('Department','Lưu danh mục phòng ban và liên kết người dùng với phòng ban trực thuộc.'),
    ('Room','Lưu tên, vị trí, sức chứa, trang thiết bị và trạng thái của phòng họp.'),
    ('Meeting','Lưu nội dung, thời gian, phòng, người tổ chức và trạng thái cuộc họp.'),
    ('MeetingParticipant','Liên kết cuộc họp với người được mời; lưu trạng thái phản hồi RSVP.'),
    ('AuditLog','Ghi nhận người thực hiện, thao tác, đối tượng và thời điểm để tra cứu nhật ký.')]

doc = Document()
section = doc.sections[0]
section.page_width, section.page_height = Cm(21), Cm(29.7)
section.top_margin, section.bottom_margin = Cm(1.8), Cm(1.8)
section.left_margin, section.right_margin = Cm(2), Cm(2)
section.header_distance, section.footer_distance = Cm(.8), Cm(.8)
for name,size in [('Normal',12),('Title',17),('Heading 1',14),('Heading 2',12.5)]:
    style=doc.styles[name]
    style.font.name='Times New Roman'; style.font.size=Pt(size); style.font.color.rgb=RGBColor(0,0,0)
    style.element.get_or_add_rPr().get_or_add_rFonts().set(qn('w:eastAsia'),'Times New Roman')
    fonts=style.element.get_or_add_rPr().get_or_add_rFonts()
    for attr in ['asciiTheme','hAnsiTheme','eastAsiaTheme','cstheme']:
        fonts.attrib.pop(qn('w:'+attr),None)
    for border in style.element.xpath('./w:pPr/w:pBdr'):
        border.getparent().remove(border)
    style.paragraph_format.space_after=Pt(6)
    style.paragraph_format.line_spacing=1.12
    if name != 'Normal': style.font.bold=True
doc.styles['Normal'].paragraph_format.widow_control=True
for compatibility in doc.settings.element.xpath('./w:compat/w:compatSetting'):
    if compatibility.get(qn('w:name'))=='compatibilityMode': compatibility.set(qn('w:val'),'15')
doc.core_properties.title='Xác định thực thể hệ thống đặt lịch họp doanh nghiệp'
doc.core_properties.subject='Thực hành Data Layer với EF Core - phần nhóm BTL'
doc.core_properties.author='Nhóm 7'

def para(text,style=None,center=False):
    p=doc.add_paragraph(text,style)
    for run in p.runs: run.font.name='Times New Roman'
    if center: p.alignment=WD_ALIGN_PARAGRAPH.CENTER
    return p

def table(headers,rows,widths):
    t=doc.add_table(rows=1,cols=len(headers)); t.alignment=WD_TABLE_ALIGNMENT.CENTER; t.autofit=False
    for column,width in zip(t.columns,widths): column.width=Cm(width)
    for cell,width,label in zip(t.rows[0].cells,widths,headers): cell.width=Cm(width); cell.text=label
    for values in rows:
        cells=t.add_row().cells
        for c,width,value in zip(cells,widths,values): c.width=Cm(width); c.text=value
    borders=OxmlElement('w:tblBorders')
    for name in ['top','left','bottom','right','insideH','insideV']:
        edge=OxmlElement('w:'+name)
        for k,v in [('val','single'),('sz','5'),('color','D9D9D9')]: edge.set(qn('w:'+k),v)
        borders.append(edge)
    t._tbl.tblPr.append(borders)
    margins=OxmlElement('w:tblCellMar')
    for name,value in [('top','80'),('bottom','80'),('left','105'),('right','105')]:
        e=OxmlElement('w:'+name); e.set(qn('w:w'),value); e.set(qn('w:type'),'dxa'); margins.append(e)
    t._tbl.tblPr.append(margins)
    repeat=OxmlElement('w:tblHeader'); t.rows[0]._tr.get_or_add_trPr().append(repeat)
    for ri,row in enumerate(t.rows):
        row._tr.get_or_add_trPr().append(OxmlElement('w:cantSplit'))
        for ci,c in enumerate(row.cells):
            c.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
            if ri==0:
                shade=OxmlElement('w:shd'); shade.set(qn('w:fill'),'E7EEF5'); c._tc.get_or_add_tcPr().append(shade)
            for p in c.paragraphs:
                p.paragraph_format.space_before=Pt(0); p.paragraph_format.space_after=Pt(0); p.paragraph_format.line_spacing=1.04
                p.alignment=WD_ALIGN_PARAGRAPH.CENTER if (ri==0 or (len(headers)==4 and ci==2)) else WD_ALIGN_PARAGRAPH.LEFT
                for run in p.runs: run.font.name='Times New Roman'; run.font.size=Pt(11.5); run.font.bold=(ri==0); run.font.color.rgb=RGBColor(0,0,0)
    return t

para('LẬP TRÌNH WEB NÂNG CAO',center=True)
para('THỰC HÀNH DATA LAYER VỚI EF CORE',center=True)
para('Xác định thực thể hệ thống','Title',True)
para('Đề tài 07  Ứng dụng đặt lịch họp trong doanh nghiệp',center=True)
para('Phần thực hiện nhóm BTL  Nhóm 7',center=True)
doc.add_heading('1 Danh sách thực thể và vai trò',level=1)
para('Dựa trên SRS của đề tài, nhóm xác định 7 thực thể để xây dựng mô hình dữ liệu: User, Role, Department, Room, Meeting, MeetingParticipant và AuditLog. Mô hình đáp ứng yêu cầu tối thiểu 5–7 thực thể và thống nhất với ERD, các Domain Model và ApplicationDbContext của dự án.')
table(['Entity','Vai trò'],roles,[4,13])
para('Căn cứ thiết kế: SRS mục 6.1 nêu User, Department, Room, Meeting và MeetingParticipant. Role được triển khai từ khóa ngoại RoleId và yêu cầu phân quyền tại mục 6.2. AuditLog là thực thể bổ sung trong phạm vi code hiện tại để ghi nhật ký; thực thể này không được liệt kê trực tiếp tại mục 6.1–6.2 của SRS đã cung cấp.')
para('Các thuộc tính chi tiết hóa hoặc bổ sung được nêu rõ ở cột Ghi chú. Kiểu dữ liệu trong bảng sau là kiểu lưu trữ thực tế của database MeetingSchedulerLab, đã đối chiếu với Domain Model và cấu hình EF Core.')

doc.add_page_break()
doc.add_heading('2 Bảng mô tả thuộc tính thực thể',level=1)
para('Quy ước: PK là khóa chính; FK là khóa ngoại; NULL là chưa có giá trị. Kiểu bit biểu diễn 0/1. Các Status là enum trong C# và được EF Core chuyển thành tên trạng thái, lưu bằng nvarchar(20).')
rows=[(entity,field,kind,note) for entity,fields in data.items() for field,kind,note in fields]
table(['Entity','Thuộc tính','Kiểu dữ liệu','Ghi chú'],rows,[3.4,4.2,3.3,6.1])
para('Lưu ý về khóa chính: MeetingParticipant dùng khóa ghép (MeetingId, UserId), không có cột ID phụ; một người không được xuất hiện trùng trong cùng cuộc họp. RoleId dùng mã vai trò cố định, còn UserId, DepartmentId, RoomId, MeetingId và AuditLogId tự tăng.')
para('Lưu ý về dữ liệu: RowVersion là token nhị phân do SQL Server tự sinh, không phải ngày giờ. Hủy cuộc họp chuyển Meeting.Status thành Cancelled; xóa mềm User, Room và Department dùng IsDisabled để bảo toàn lịch sử.')
footer=section.footer.paragraphs[0]; footer.alignment=WD_ALIGN_PARAGRAPH.CENTER
footer.add_run('Nhóm 7  |  Danh sách thực thể  |  Trang ')
field=OxmlElement('w:fldSimple'); field.set(qn('w:instr'),'PAGE'); footer._p.append(field)
for r in footer.runs: r.font.size=Pt(10)
doc.save(OUTPUT)
assert len(rows)==45 and len(data)==7
print(OUTPUT)
print('Validated content: 7 entity roles, 45 attribute records, 4 required columns.')
