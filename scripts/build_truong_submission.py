from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.section import WD_SECTION_START, WD_ORIENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

root = Path(__file__).resolve().parents[1]
folder = root / 'Nop_ca_nhan_Nguyen_Van_Truong'
doc = Document()
sec = doc.sections[0]
sec.page_width, sec.page_height = Cm(21), Cm(29.7)
sec.top_margin = sec.bottom_margin = Cm(1.8)
sec.left_margin = sec.right_margin = Cm(2)
for name, size in [('Normal', 11.5), ('Title', 18), ('Heading 1', 14), ('Heading 2', 12)]:
    style = doc.styles[name]
    style.font.name = 'Times New Roman'
    style.font.size = Pt(size)
    style.font.color.rgb = RGBColor(0, 0, 0)
    style.paragraph_format.space_after = Pt(5)
    style.paragraph_format.line_spacing = 1.08
    if name.startswith('Heading'):
        style.font.bold = True
        style.paragraph_format.space_before = Pt(7)
    fonts = style.element.rPr.rFonts
    if fonts is not None:
        for key in list(fonts.attrib):
            if 'Theme' in key:
                del fonts.attrib[key]
        fonts.set(qn('w:eastAsia'), 'Times New Roman')
footer = sec.footer.paragraphs[0]
footer.alignment = WD_ALIGN_PARAGRAPH.CENTER
field = OxmlElement('w:fldSimple')
field.set(qn('w:instr'), 'PAGE')
footer._p.append(field)

def p(text):
    return doc.add_paragraph(text)

def h(text, level=1):
    return doc.add_heading(text, level)

def table(headers, rows, widths):
    t = doc.add_table(rows=1, cols=len(headers))
    t.autofit = False
    for col, width in zip(t.columns, widths):
        col.width = Cm(width)
    borders = OxmlElement('w:tblBorders')
    for edge in ['top', 'left', 'bottom', 'right', 'insideH', 'insideV']:
        b = OxmlElement('w:' + edge)
        for key, value in {'val': 'single', 'sz': '4', 'color': 'D9D9D9'}.items():
            b.set(qn('w:' + key), value)
        borders.append(b)
    t._tbl.tblPr.append(borders)
    t.rows[0]._tr.get_or_add_trPr().append(OxmlElement('w:tblHeader'))
    for index, values in enumerate([headers] + rows):
        cells = t.rows[0].cells if index == 0 else t.add_row().cells
        cells[0]._tc.getparent().get_or_add_trPr().append(OxmlElement('w:cantSplit'))
        for cell, value, width in zip(cells, values, widths):
            cell.width = Cm(width)
            cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
            para = cell.paragraphs[0]
            para.paragraph_format.space_before = Pt(3)
            para.paragraph_format.space_after = Pt(3)
            para.paragraph_format.line_spacing = 1
            run = para.add_run(value)
            run.font.size = Pt(10.5)
            if index == 0:
                run.bold = True
                shade = OxmlElement('w:shd')
                shade.set(qn('w:fill'), 'E8EDF2')
                cell._tc.get_or_add_tcPr().append(shade)
    p('')

def code(text):
    for line in text.splitlines():
        para = p('')
        para.paragraph_format.space_after = Pt(0)
        para.paragraph_format.line_spacing = 1
        run = para.add_run(line)
        run.font.name = 'Consolas'
        run.font.size = Pt(9)

title = doc.add_paragraph('Bài thực hành cá nhân Data Layer với EF Core', 'Title')
title.alignment = WD_ALIGN_PARAGRAPH.CENTER
p('Sinh viên: Nguyễn Văn Trường | Mã sinh viên: .................... | Nhóm 7')
p('Đề tài 07: Ứng dụng đặt lịch họp trong doanh nghiệp')
h('1 Phạm vi module được phân công')
p('Phần cá nhân của tôi là FS03, quản lý đặt lịch họp và phản hồi RSVP theo bảng chia module hiện tại. Hai entity chính là Meeting và MeetingParticipant. Tôi dùng nền tảng chung của nhóm gồm DbContext, xác thực, danh mục phòng và nhân sự để triển khai nghiệp vụ lịch họp.')
table(['Entity', 'Vai trò và quan hệ'], [
    ['Meeting', 'Lưu tiêu đề, thời gian, phòng, người tổ chức, trạng thái và RowVersion. Một phòng có nhiều cuộc họp; mỗi cuộc họp có một người tổ chức.'],
    ['MeetingParticipant', 'Bảng liên kết Meeting và User, khóa chính ghép (MeetingId, UserId). Lưu trạng thái phản hồi và RespondedAt; thể hiện quan hệ nhiều nhiều.'],
], [4, 13])
p('RSVP trong C# dùng enum RsvpStatus; Fluent API HasConversion<string>() lưu xuống database dưới dạng Pending, Accepted, Declined. Vì vậy dữ liệu trong bảng vẫn là chuỗi như yêu cầu phân công.')
h('2 Các file code thuộc module')
p('Models/Meeting.cs; Models/MeetingParticipant.cs; Models/Enums/Statuses.cs; ViewModels/Meetings/MeetingViewModels.cs.')
p('Services/MeetingService.cs; Services/RsvpService.cs; Services/Interfaces/ServiceContracts.cs; Controllers/MeetingController.cs; Views/Meeting/*.cshtml.')
p('Đường dẫn tính từ MeetingScheduler.Web. ApplicationDbContext, DomainConfiguration, ServiceSupport, QueryFilter/PagedResult, xác thực và layout là phần dùng chung. RoomAvailabilityService và API kiểm tra phòng hỗ trợ form; email queue là thành phần tích hợp, không phải hệ thống SMTP độc lập của phần cá nhân.')

doc.add_page_break()
h('3 Data Flow của module')
p('Trình duyệt → MeetingController → MeetingService hoặc RsvpService → ApplicationDbContext → SQL Server → ViewModel hoặc ServiceResult → Controller → Razor View hoặc redirect.')
p('Controller nhận model từ request, kiểm tra ModelState và lấy CurrentUserId từ claim. Service kiểm tra quyền và nghiệp vụ; EF Core dịch LINQ và ghi thay đổi. View dùng ViewModel, không nhận trực tiếp entity để ghi database.')
table(['Request', 'Luồng xử lý và kết quả'], [
    ['GET /Meeting', 'Index nhận MeetingFilter → QueryAsync → kiểm tra quyền xem → Where → OrderBy → CountAsync → Skip/Take → PagedResult<MeetingDetailsViewModel> → Index.cshtml.'],
    ['GET /Meeting/Details/{id}', 'GetAsync kiểm tra Admin, chủ cuộc họp hoặc người được mời → Select sang ViewModel → Details.cshtml; không tìm thấy hoặc không được xem trả 404.'],
    ['POST /Meeting/Create', 'ModelState → SaveAsync → kiểm tra tài khoản, phòng, thời gian, sức chứa và trùng phòng → INSERT Meeting + MeetingParticipant + AuditLog → commit → redirect Details.'],
    ['POST /Meeting/Edit/{id}', 'Kiểm tra id → SaveAsync → quyền chủ cuộc họp hoặc Admin, trạng thái và RowVersion → kiểm tra lại xung đột → UPDATE Meeting và đồng bộ người tham dự → commit.'],
    ['POST /Meeting/Cancel/{id}', 'CancelAsync kiểm tra quyền, lý do, thời gian và RowVersion → Status = Cancelled, lưu thông tin hủy + AuditLog → commit → redirect Details. Không DELETE Meeting.'],
    ['GET /Meeting/RsvpTracking/{id}', 'RsvpFilter → RsvpService.QueryAsync → chỉ Admin hoặc người tổ chức được theo dõi → lọc cuộc họp/trạng thái/tên/email → sắp xếp, phân trang → RsvpTracking.cshtml.'],
    ['POST /Meeting/Respond', 'RespondAsync xác nhận người đăng nhập được mời và cuộc họp còn nhận phản hồi → UPDATE Status, RespondedAt + AuditLog → commit → redirect Details.'],
], [5, 12])
p('Các action GET Create/Edit/Cancel nạp biểu mẫu trước khi ghi. Action Delete chuyển sang luồng Cancel. Các thao tác ghi có kiểm tra antiforgery qua controller nền; thông tin người thao tác không lấy từ form.')

doc.add_page_break()
h('4 Query Pipeline và kiểm tra xung đột')
h('4 1 Danh sách lịch họp', 2)
p('MeetingFilter kế thừa SearchString, SortOrder, Page và PageSize; thêm RoomId, CreatedByUserId, Status, From và To. Admin xem toàn bộ; người dùng khác chỉ xem cuộc họp mình tổ chức hoặc được mời.')
code('''var q = db.Meetings.AsNoTracking();
if (actor.RoleId != 1)
    q = q.Where(m => m.CreatedByUserId == actorUserId
        || m.Participants.Any(p => p.UserId == actorUserId));
if (f.RoomId.HasValue) q = q.Where(m => m.RoomId == f.RoomId);
if (f.From.HasValue) q = q.Where(m => m.EndTime > f.From);
if (f.To.HasValue) q = q.Where(m => m.StartTime < f.To);
// Sau Where: sap xep -> Select(Map) -> PageAsync.
var count = await ordered.CountAsync(ct);
var items = await ordered.Skip((page - 1) * size)
                         .Take(size).ToListAsync(ct);''')
p('Đoạn code trên lược bớt các bộ lọc và switch để trình bày. Tìm tiêu đề dùng Contains(SearchString.Trim()). start sắp xếp giờ tăng; title/title_desc sắp xếp tiêu đề; mặc định giờ giảm. ThenBy(MeetingId) giữ thứ tự ổn định. From/To lọc các cuộc họp giao với khoảng thời gian, không chỉ các cuộc họp nằm hoàn toàn bên trong khoảng đó.')
h('4 2 Danh sách người tham dự', 2)
p('RsvpService bắt đầu từ MeetingParticipants.AsNoTracking().Where(p => p.MeetingId == f.MeetingId), sau đó lọc Status và tên/email. Các kiểu sắp xếp gồm status, status_desc, responded, responded_desc, name_desc; mặc định theo tên tăng. ThenBy(UserId) ổn định thứ tự. Cả hai danh sách dùng ServiceSupport.PageAsync để CountAsync trước Skip/Take, PageSize tối đa 100.')
h('4 3 Điều kiện trùng phòng', 2)
code('''x.RoomId == m.RoomId && x.MeetingId != m.MeetingId
&& x.Status == MeetingStatus.Confirmed
&& x.StartTime < m.EndTime && m.StartTime < x.EndTime''')
p('Hai cuộc họp chỉ xung đột nếu cùng phòng, còn hiệu lực và khoảng giờ chồng nhau. Ví dụ 09:00–10:00 và 09:30–10:30 bị chặn; 09:00–10:00 và 10:00–11:00 được phép. Loại trừ MeetingId hiện tại khi sửa. Truy vấn kiểm tra và thao tác ghi nằm cùng transaction Serializable, kèm xử lý lỗi concurrency/deadlock để tránh lưu hai yêu cầu xung đột.')

doc.add_page_break()
h('5 Luồng ghi và các ràng buộc nghiệp vụ')
p('Tạo lịch: Title bắt buộc, tối đa 200 ký tự; EndTime phải sau StartTime và StartTime ở tương lai. Tài khoản và phòng ban phải hoạt động; chỉ Organizer/Admin được tạo. Phòng phải Active và không bị vô hiệu hóa. ParticipantIds được Distinct; số người tham dự cộng người tổ chức không được vượt Capacity.')
p('Sửa lịch: chỉ chủ cuộc họp hoặc Admin; không sửa cuộc họp Cancelled hoặc đã bắt đầu. RowVersion gửi từ form phải khớp dữ liệu hiện tại. Service đồng bộ danh sách mời: bỏ người không còn được chọn, thêm người mới với Pending. Nếu đổi phòng hoặc giờ, phản hồi của người giữ lại được đặt về Pending và RespondedAt = null.')
p('Lưu Meeting và MeetingParticipant, ghi AuditLog rồi commit. Email chỉ được đưa vào queue sau khi lưu thành công; SMTP chưa cấu hình hoặc queue đầy không làm hoàn tác cuộc họp đã lưu. Hủy lịch lưu CancelledAt, CancelledByUserId và CancelReason để bảo toàn lịch sử.')
p('RSVP: chỉ chấp nhận Accepted hoặc Declined; Pending là trạng thái khởi tạo. Người phản hồi phải nằm trong danh sách mời, tài khoản hoạt động, cuộc họp Confirmed và chưa bắt đầu. RespondedAt được ghi bằng DateTime.UtcNow. Người dùng không thể gửi UserId của người khác để phản hồi thay.')
h('6 Trả lời năm câu hỏi lý thuyết')
p('EF Core là gì? Là ORM của .NET, ánh xạ entity với bảng và dịch LINQ được hỗ trợ thành SQL. MeetingService và RsvpService đọc/ghi qua DbSet thay vì tự ghép SQL từ input.')
p('DbContext có vai trò gì? Quản lý mô hình dữ liệu, kết nối, DbSet, change tracker và SaveChangesAsync. DbContext dùng Scoped; không dùng cùng một instance cho nhiều truy vấn chạy đồng thời.')
p('Migration dùng để làm gì? Lưu phiên bản schema Code First; tạo bảng, khóa ngoại, khóa chính ghép, index, CHECK và rowversion. Khi đổi model cần migration mới và áp dụng vào database.')
p('Vì sao dùng async? Truy vấn database là I/O; await cho phép luồng request không bị giữ trong lúc chờ. Async không làm một DbContext an toàn để chạy song song.')
p('Khi nào dùng AsNoTracking? Khi chỉ đọc danh sách/chi tiết/lookup. Entity cần cập nhật được lấy có tracking; SaveChangesAsync ghi các thay đổi được theo dõi.')
h('7 Chạy lại và trách nhiệm bài nộp')
p('Mở MeetingScheduler.sln bằng Visual Studio 2022, chọn MeetingScheduler.Web, profile http và F5. Tài khoản demo: organizer@company.test hoặc admin@company.test; mật khẩu Demo@12345. Đọc lịch tại /Meeting, tạo lịch tương lai, thử trùng phòng và đăng nhập employee@company.test để phản hồi lời mời.')
p('Nguồn: chia module.docx và source đi kèm. Bảy ảnh sau được Codex chụp trực tiếp code trong Visual Studio ngày 14/10/2026; đây không phải minh chứng Trường đã tự chạy các tình huống. Sinh viên chịu trách nhiệm đọc hiểu, chạy lại và trình bày đúng phần được giao.')

items = [
    ('01_Meeting_Model.jpg', 'Entity Meeting', 'Thuộc tính cuộc họp, RowVersion, dữ liệu hủy và navigation.'),
    ('02_MeetingParticipant_Model.jpg', 'Entity MeetingParticipant', 'Liên kết cuộc họp và người tham dự; trạng thái RSVP và thời gian phản hồi.'),
    ('03_MeetingService_QueryPipeline.jpg', 'Query Pipeline lịch họp', 'Phân quyền xem, tìm kiếm, lọc phòng và thời gian, sắp xếp và phân trang.'),
    ('04_MeetingService_CheckConflict.jpg', 'Kiểm tra nghiệp vụ trước khi lưu', 'Quyền sửa, RowVersion, sức chứa, tài khoản và khoảng giờ xung đột.'),
    ('05_MeetingService_Save_Transaction.jpg', 'Đồng bộ người tham dự và lưu dữ liệu', 'Thêm bỏ người tham dự, reset RSVP khi đổi lịch, ghi audit và commit.'),
    ('06_RsvpService_Query_Respond.jpg', 'Service xử lý RSVP', 'Theo dõi theo cuộc họp, lọc và sắp xếp; ghi nhận Accepted hoặc Declined.'),
    ('07_MeetingController_Rsvp.jpg', 'Controller phản hồi và theo dõi RSVP', 'Respond, RsvpTracking và điều phối service với CurrentUserId.'),
]
sec = doc.add_section(WD_SECTION_START.NEW_PAGE)
sec.orientation = WD_ORIENT.LANDSCAPE
sec.page_width, sec.page_height = Cm(29.7), Cm(21)
sec.left_margin = sec.right_margin = Cm(1.2)
sec.top_margin = sec.bottom_margin = Cm(1.2)
for index, (filename, title, caption) in enumerate(items, 1):
    if index > 1:
        doc.add_page_break()
    heading = h(f'Minh chứng {index} {title}')
    heading.paragraph_format.space_before = Pt(0)
    para = p(caption)
    para.paragraph_format.keep_with_next = True
    para = p('')
    para.alignment = WD_ALIGN_PARAGRAPH.CENTER
    para.paragraph_format.space_after = Pt(0)
    para.add_run().add_picture(str(folder / 'Minh_chung' / filename), width=Cm(27))
    para._p.xpath('.//wp:docPr')[0].set('descr', title + '. ' + caption)
output = folder / 'Nguyen_Van_Truong_DataFlow_Meeting_RSVP_Co_Minh_Chung.docx'
doc.core_properties.author = 'Nguyen Van Truong'
doc.core_properties.title = 'Data Flow module Meeting va RSVP'
doc.save(output)
with ZipFile(folder / 'Nguyen_Van_Truong_Minh_chung_Meeting_RSVP.zip', 'w', ZIP_DEFLATED) as z:
    for path in sorted((folder / 'Minh_chung').glob('*.jpg')):
        z.write(path, 'Minh_chung/' + path.name)
print(output)
print(f'Embedded images: {len(doc.inline_shapes)}')
