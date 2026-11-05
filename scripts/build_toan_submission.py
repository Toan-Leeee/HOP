from pathlib import Path
from html import escape
from zipfile import ZipFile, ZIP_DEFLATED
from hashlib import sha256
import json
from reportlab.pdfgen import canvas
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, Preformatted
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_CENTER
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont

BASE=Path(__file__).resolve().parents[1]
OUT=BASE/'Nop_ca_nhan_Le_Xuan_Toan'
OUT.mkdir(exist_ok=True)
NAME='23A1001D0361_Le_Xuan_Toan_DataFlow_Room'
pdfmetrics.registerFont(TTFont('TimesVN',r'C:\Windows\Fonts\times.ttf'))
pdfmetrics.registerFont(TTFont('TimesVN-Bold',r'C:\Windows\Fonts\timesbd.ttf'))
pdfmetrics.registerFont(TTFont('Code',r'C:\Windows\Fonts\consola.ttf'))
styles=getSampleStyleSheet()
styles.add(ParagraphStyle(name='BodyVN',fontName='TimesVN',fontSize=12,leading=16,spaceAfter=8))
styles.add(ParagraphStyle(name='TitleVN',fontName='TimesVN-Bold',fontSize=18,leading=23,alignment=TA_CENTER,spaceAfter=12))
styles.add(ParagraphStyle(name='HeadingVN',fontName='TimesVN-Bold',fontSize=14,leading=18,spaceBefore=10,spaceAfter=8))
styles.add(ParagraphStyle(name='CellVN',fontName='TimesVN',fontSize=10.5,leading=13,spaceAfter=0))
styles.add(ParagraphStyle(name='CellBoldVN',fontName='TimesVN-Bold',fontSize=10.5,leading=13,spaceAfter=0))
styles.add(ParagraphStyle(name='CodeVN',fontName='Code',fontSize=9,leading=12,spaceAfter=9))
story=[]; markdown=[]
def p(text):
    story.append(Paragraph(escape(text),styles['BodyVN']));markdown.append(text+'\n')
def h(text):
    story.append(Paragraph(escape(text),styles['HeadingVN']));markdown.append('## '+text+'\n')
def table(headers,rows,widths):
    items=[[Paragraph(escape(str(v)),styles['CellBoldVN'] if i==0 else styles['CellVN']) for v in row] for i,row in enumerate([headers]+rows)]
    t=Table(items,colWidths=widths,repeatRows=1,hAlign='LEFT')
    t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),colors.HexColor('#E7EEF5')),('GRID',(0,0),(-1,-1),.45,colors.HexColor('#D9D9D9')),('VALIGN',(0,0),(-1,-1),'MIDDLE'),('LEFTPADDING',(0,0),(-1,-1),7),('RIGHTPADDING',(0,0),(-1,-1),7),('TOPPADDING',(0,0),(-1,-1),6),('BOTTOMPADDING',(0,0),(-1,-1),6)]))
    story.extend([t,Spacer(1,10)])
    markdown.append('| '+' | '.join(headers)+' |\n| '+' | '.join(['---']*len(headers))+' |\n'+'\n'.join('| '+' | '.join(str(v).replace('\n','<br>') for v in row)+' |' for row in rows)+'\n')
def code(text):
    story.append(Preformatted(text,styles['CodeVN']));markdown.append('```csharp\n'+text+'\n```\n')
def page():story.append(PageBreak())

story.append(Paragraph('Bài thực hành cá nhân Data Layer với EF Core',styles['TitleVN']))
markdown.append('# Bài thực hành cá nhân Data Layer với EF Core\n')
p('Sinh viên: Lê Xuân Toàn | Mã sinh viên: 23A1001D0361 | Nhóm 7')
p('Đề tài 07: Ứng dụng đặt lịch họp trong doanh nghiệp')
h('1 Phạm vi module được phân công')
p('Phần cá nhân của tôi là FS01 và FS02, quản lý phòng họp và cơ sở vật chất; entity chính là Room. Tôi dùng DbContext, database, xác thực và các thành phần nền chung của nhóm để thực hiện CRUD phòng họp và Query Pipeline.')
p('Căn cứ phân công: mục 3 trong tài liệu chia module của nhóm. Chức năng gồm quản lý phòng họp, sức chứa, vị trí tòa nhà, trang thiết bị, trạng thái bảo trì và đóng mở phòng.')
table(['Chức năng','Triển khai'],[
('CRUD phòng họp','Index, Details, Create GET/POST, Edit GET/POST, Delete GET/POST.'),
('Cơ sở vật chất','Tên phòng, vị trí/tòa nhà, sức chứa, trang thiết bị.'),
('Bảo trì và đóng mở','Status = Active/Maintenance; IsDisabled = true/false. Delete là xóa mềm, Restore là khôi phục.'),
('Query Pipeline','Tên phòng, trạng thái, sức chứa tối thiểu, trạng thái sử dụng; sắp xếp tên/sức chứa; phân trang.')],[135,350])
h('2 Các file code thuộc module')
for value in ['Models/Room.cs; Models/Enums/Statuses.cs (RoomStatus).','ViewModels/Rooms/RoomViewModels.cs: RoomFilter, RoomViewModel.','Services/RoomService.cs: QueryAsync, GetAsync, SaveAsync, DisableAsync, GetLookupAsync.','Services/Interfaces/ServiceContracts.cs: interface IRoomService.','Areas/Admin/Controllers/RoomController.cs: các action CRUD, Lock và Restore.','Areas/Admin/Views/Room/: Index, Details, Create, Edit, Delete.']:
    p(value)
p('Các đường dẫn trên tính từ MeetingScheduler.Web. Phần dùng chung: ApplicationDbContext, DomainConfiguration, SeedData, ServiceSupport, QueryFilter/PagedResult/ServiceResult, CacheKeys, AdminController, AuthenticatedController, layout và pagination.')
page()
h('3 Luồng dữ liệu tổng quát')
p('View → RoomController → IRoomService/RoomService → ApplicationDbContext → DbSet<Room> → LINQ → SQL Server → kết quả trả về Service → Controller → View.')
p('Controller nhận RoomFilter hoặc RoomViewModel, kiểm tra ModelState và gọi service. Service kiểm tra nghiệp vụ, truy vấn/ghi dữ liệu qua EF Core và trả ViewModel hoặc ServiceResult. View không nhận trực tiếp entity Room để hạn chế over-posting.')
table(['HTTP và action','Data Flow và kết quả'],[
('GET /Admin/Room','Index nhận RoomFilter → QueryAsync → AsNoTracking, Where, OrderBy, CountAsync, Skip/Take, ToListAsync → PagedResult<RoomViewModel> → Index.cshtml.'),
('GET /Admin/Room/Details/{id}','GetAsync → Where(RoomId == id), Select sang RoomViewModel, FirstOrDefaultAsync → Details.cshtml; không tồn tại trả 404.'),
('GET /Admin/Room/Create','Tạo RoomViewModel mới và trả form; chưa ghi database.'),
('POST /Admin/Room/Create','ModelState → SaveAsync → quyền Admin, Data Annotation, tên duy nhất → Add Room → SaveChangesAsync → ghi AuditLog trong transaction → xóa cache → redirect Index; lỗi trả lại form.'),
('GET /Admin/Room/Edit/{id}','GetAsync đọc phòng hiện tại → Edit.cshtml; form gửi RoomId và các trường được phép sửa.'),
('POST /Admin/Room/Edit/{id}','Kiểm tra id trùng RoomId → SaveAsync → FindAsync → gán các trường → UPDATE + AuditLog → commit và xóa cache → redirect Index.'),
('GET /Admin/Room/Delete/{id}','Đọc phòng và trả Delete.cshtml để xác nhận; chưa xóa dữ liệu.'),
('POST /Admin/Room/Delete/{id}','DisableAsync → IsDisabled = true → SaveChangesAsync + AuditLog → commit và xóa cache; giữ bản ghi và lịch sử Meeting.RoomId.'),
('POST /Admin/Room/Restore/{id}','GetAsync → đặt IsDisabled = false → SaveAsync → UPDATE, audit và xóa cache → redirect Index.')],[149,336])
p('AdminController có [Authorize(Roles = "Admin")]. Các thao tác ghi chịu kiểm tra antiforgery. CurrentUserId lấy từ claim đăng nhập, không lấy từ form. Service kiểm tra lại tài khoản Admin đang hoạt động trước khi ghi.')
page()
h('4 Query Pipeline của trang Index')
p('RoomFilter kế thừa QueryFilter: SearchString, SortOrder, Page và PageSize; bổ sung Status, MinimumCapacity, IsDisabled. Mặc định Page = 1, PageSize = 10 và IsDisabled = false.')
code('var q = db.Rooms.AsNoTracking();\nif (!string.IsNullOrWhiteSpace(f.SearchString))\n    q = q.Where(r => r.RoomName.Contains(f.SearchString.Trim()));\nif (f.Status.HasValue) q = q.Where(r => r.Status == f.Status);\nif (f.MinimumCapacity.HasValue)\n    q = q.Where(r => r.Capacity >= f.MinimumCapacity);\nif (f.IsDisabled.HasValue)\n    q = q.Where(r => r.IsDisabled == f.IsDisabled);\n// Sau filter: OrderBy/OrderByDescending + ThenBy(RoomId).\n// ServiceSupport.PageAsync thuc hien CountAsync -> Skip/Take.\nvar count = await ordered.CountAsync(ct);\nvar items = await ordered.Skip((page - 1) * size)\n                         .Take(size).ToListAsync(ct);')
p('Đoạn trên lược bớt switch sắp xếp để trình bày; mã đầy đủ ở RoomService.cs và ServiceSupport.cs. capacity: sức chứa tăng; capacity_desc: sức chứa giảm; name_desc: tên giảm; mặc định: tên tăng. ThenBy(RoomId) giữ thứ tự ổn định khi giá trị sắp xếp bằng nhau.')
p('CountAsync đếm sau lọc và trước phân trang. Page được chặn trong khoảng 1–1.000.000. PageSize không hợp lệ hoặc ≤ 0 được chuẩn hóa; tối đa 100 dòng/trang. Phân trang giữ lại query string tìm kiếm, trạng thái và sắp xếp bằng _Pagination.cshtml.')
h('Ví dụ dữ liệu đi qua các tầng')
p('Người dùng nhập “Lotus”, chọn Active, sức chứa tối thiểu 10 và sắp xếp sức chứa giảm. Trình duyệt gửi GET /Admin/Room?SearchString=Lotus&Status=Active&MinimumCapacity=10&IsDisabled=false&SortOrder=capacity_desc&Page=1&PageSize=10.')
p('EF Core tạo truy vấn tham số hóa. SQL dưới đây minh họa ý nghĩa truy vấn; không phải log SQL nguyên văn của lần chạy:')
code("SELECT COUNT(*) FROM Room\nWHERE RoomName LIKE '%' + @search + '%'\n  AND Status = @status AND Capacity >= @capacity\n  AND IsDisabled = 0;\nSELECT RoomId, RoomName, Location, Capacity, Equipment,\n       Status, IsDisabled FROM Room\nWHERE RoomName LIKE '%' + @search + '%'\n  AND Status = @status AND Capacity >= @capacity\n  AND IsDisabled = 0\nORDER BY Capacity DESC, RoomId\nOFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;")
page()
h('5 Ràng buộc và thao tác ghi dữ liệu')
table(['Nội dung','Cách xử lý'],[
('Tên phòng','Required, tối đa 100 ký tự; service Trim và kiểm tra trùng; unique index RoomName tại database.'),
('Sức chứa','Range(1, int.MaxValue) ở Model/ViewModel; CHECK Capacity > 0 trong Fluent API.'),
('Vị trí và thiết bị','Location tối đa 200, Equipment tối đa 500 ký tự; cho phép NULL.'),
('Trạng thái phòng','RoomStatus chỉ Active/Maintenance; lưu bằng chuỗi nvarchar(20). Service từ chối enum ngoài phạm vi.'),
('Ngừng dùng và mở lại','IsDisabled dùng xóa mềm/khôi phục. Maintenance khác với IsDisabled: phòng bảo trì vẫn còn trong danh mục; cả hai đều bị loại khỏi lookup đặt lịch.'),
('Cache và audit','Lookup phòng Active có cache 5 phút. Save/Disable thành công xóa ROOM_LIST. Ghi Room và AuditLog cùng transaction.')],[135,350])
p('Ví dụ câu lệnh ghi do EF Core sinh về mặt ý nghĩa: INSERT INTO Room (...) VALUES (...); UPDATE Room SET ... WHERE RoomId = @id; xóa mềm là UPDATE Room SET IsDisabled = 1 WHERE RoomId = @id. Không có DELETE Room trong luồng xóa mềm.')
h('6 Chạy và trình bày module')
p('Mở MeetingScheduler.sln bằng Visual Studio 2022, chọn MeetingScheduler.Web làm Startup Project, profile http, chạy F5. Đăng nhập admin@company.test với mật khẩu demo Demo@12345; truy cập /Admin/Room. Database và migration thuộc khung chung của nhóm.')
p('Minh chứng nên chụp: danh sách phòng sau tìm kiếm/sắp xếp; trang 2 khi có đủ dữ liệu; form thêm/sửa; validation sức chứa hoặc tên trùng; phòng ngừng dùng và khôi phục. Đây là các bước sinh viên có thể chạy lại, không phải tuyên bố sinh viên đã tự chạy.')
h('7 Kết quả kiểm tra và giới hạn')
p('Ngày 14/10/2026, Codex build solution thành công với 0 warning và 0 error. Bộ kiểm tra tích hợp SQL Server của dự án có 9 nhóm kiểm tra đạt, 0 lỗi; phần Room kiểm tra CRUD, filter/sort/paging, tên trùng, sức chứa, invalidation cache, quyền Admin và giữ lịch sử khi xóa mềm. Database test được tạo riêng và dọn sau khi chạy.')
p('Scaffold Room đã được tạo bằng CLI dotnet-aspnet-codegenerator chính thức, bản gốc giữ trong docs/ScaffoldBaseline. Code đang chạy đã điều chỉnh sang Service/ViewModel và xóa mềm. Chưa thao tác hộp thoại scaffold trong Visual Studio; nếu giảng viên yêu cầu minh chứng thao tác IDE, cần thực hiện và chụp theo docs/SCAFFOLD.md.')
page()
h('8 Trả lời năm câu hỏi lý thuyết')
for title,body in [
('EF Core là gì?','EF Core là ORM của .NET, ánh xạ entity C# với bảng dữ liệu và dịch LINQ được hỗ trợ thành SQL. Trong module này, RoomService dùng db.Rooms để đọc/ghi bảng Room mà không tự ghép SQL từ input.'),
('DbContext có vai trò gì?','ApplicationDbContext quản lý mô hình dữ liệu, kết nối, DbSet và change tracker. db.Rooms đại diện bảng Room. Khi gọi SaveChangesAsync, EF Core ghi thay đổi đã theo dõi vào database. DbContext được DI đăng ký Scoped và không dùng đồng thời giữa nhiều luồng.'),
('Migration dùng để làm gì?','Migration lưu phiên bản schema Code First. InitialCreate tạo các bảng, khóa ngoại, index và CHECK từ Models/Fluent API. Update-Database áp dụng migration chưa chạy. Khi sửa model phải tạo migration mới để database được đồng bộ.'),
('Vì sao nên dùng async?','Truy vấn database là I/O. await CountAsync, ToListAsync hoặc SaveChangesAsync cho phép luồng request không bị giữ trong thời gian chờ I/O, giúp server phục vụ nhiều request. Async không có nghĩa một DbContext có thể chạy nhiều truy vấn song song.'),
('Khi nào dùng AsNoTracking?','Dùng khi chỉ đọc để hiển thị danh sách, chi tiết và lookup. RoomService dùng AsNoTracking ở QueryAsync/GetAsync/GetLookupAsync. Với sửa/xóa mềm, FindAsync lấy entity được theo dõi để SaveChangesAsync ghi thay đổi.')]:
    h(title);p(body)
h('9 Nguồn và trách nhiệm phần cá nhân')
p('Nguồn: chia module.docx; đề Thực hành buổi 6; Models/Room.cs, DomainConfiguration.cs, RoomService.cs, ServiceSupport.cs, RoomController.cs và các Razor View trong source đi kèm.')
p('Sinh viên xác nhận phạm vi và chịu trách nhiệm đọc hiểu, chạy lại, kiểm tra, trình bày bài. AI hỗ trợ chuẩn bị code/tài liệu và chạy các kiểm tra được ghi rõ ở mục 7. Bản nộp không xem các module của thành viên khác là công việc cá nhân của Toàn.')

def footer(c,doc):
    c.setFont('TimesVN',10);c.setFillColor(colors.HexColor('#444444'))
    c.drawString(55,29,'Lê Xuân Toàn  |  23A1001D0361  |  Module Room')
    c.drawRightString(A4[0]-55,29,f'Trang {doc.page}')
pdf=OUT/(NAME+'.pdf')
SimpleDocTemplate(str(pdf),pagesize=A4,leftMargin=55,rightMargin=55,topMargin=45,bottomMargin=48,title='Data Flow module Room của Lê Xuân Toàn',author='Lê Xuân Toàn').build(story,onFirstPage=footer,onLaterPages=footer)
(OUT/(NAME+'.md')).write_text('\n'.join(markdown),encoding='utf-8')

personal_files=[
'Models/Room.cs','Models/Enums/Statuses.cs','ViewModels/Rooms/RoomViewModels.cs',
'Services/RoomService.cs','Services/Interfaces/ServiceContracts.cs',
'Areas/Admin/Controllers/RoomController.cs',
*[f'Areas/Admin/Views/Room/{view}.cshtml' for view in ['Index','Details','Create','Edit','Delete']]]
manifest=[]
for rel in personal_files:
    file=BASE/'MeetingScheduler.Web'/rel
    assert file.exists(),file
    manifest.append({'path':'MeetingScheduler.Web/'+rel,'sha256':sha256(file.read_bytes()).hexdigest()})
(OUT/'Module_Room_Files.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
readme='''# Phần cá nhân Lê Xuân Toàn 23A1001D0361

Module xác nhận theo phân chia: Room, FS01 và FS02 quản lý phòng họp/cơ sở vật chất.

- Báo cáo Data Flow ở PDF để nộp; bản Markdown chứa cùng nội dung để chỉnh sửa.
- ZIP source chứa solution chung chạy được. Module_Room_Files.json chỉ rõ file phần Room.
- Các module User/Department/Meeting/RSVP/Report là thành phần của dự án nhóm, không phải toàn bộ đều thuộc Toàn.
- ServiceContracts.cs và Statuses.cs chứa cả khai báo dùng chung; phần cá nhân trong đó là IRoomService và RoomStatus.
- Mở MeetingScheduler/MeetingScheduler.sln bằng VS2022, chọn Web làm Startup Project.
- Chọn http và F5; đăng nhập admin@company.test / Demo@12345; vào /Admin/Room.
- LocalDB (localdb)\\MSSQLLocalDB; migration và seed chạy tự động trong Development.
- Khi cổng 5086 đã có tiến trình dự án, dừng nó trước khi F5. README solution có hướng dẫn.
- Trong Package Manager Console: Update-Database; không tạo lại InitialCreate.
- Không có bin/obj/Logs/database file trong ZIP. Có Migrations, SeedData và cấu hình kết nối không chứa mật khẩu thật.
- Scaffold đã chạy qua CLI; minh chứng hộp thoại VS chưa thực hiện, xem docs/SCAFFOLD.md.
'''
(OUT/'HUONG_DAN_NOP.md').write_text(readme,encoding='utf-8')
zip_path=OUT/'23A1001D0361_Le_Xuan_Toan_Source_Room.zip'
exclude={'bin','obj','Logs','.vs','.git','node_modules','Nop_ca_nhan_Le_Xuan_Toan','qa','__pycache__'}
with ZipFile(zip_path,'w',ZIP_DEFLATED) as archive:
    for top in ['MeetingScheduler.Web','MeetingScheduler.Checks','.config']:
        for file in (BASE/top).rglob('*'):
            rel=file.relative_to(BASE)
            if file.is_file() and not any(part in exclude for part in rel.parts) and not file.name.endswith('.local.json'):
                archive.write(file,'MeetingScheduler/'+rel.as_posix())
    archive.write(BASE/'MeetingScheduler.sln','MeetingScheduler/MeetingScheduler.sln')
    archive.write(BASE/'README.md','MeetingScheduler/README.md')
    for name in ['STRUCTURE.md','ENTITY_DICTIONARY.md','ERD.md','DATA_FLOW.md','SCAFFOLD.md','VERIFICATION.md','ERD_MeetingScheduler.drawio']:
        archive.write(BASE/'docs'/name,'MeetingScheduler/docs/'+name)
    for file in (BASE/'docs'/'ScaffoldBaseline').rglob('*'):
        if file.is_file(): archive.write(file,'MeetingScheduler/'+file.relative_to(BASE).as_posix())
    archive.write(BASE/'scripts'/'Stop-Dev.ps1','MeetingScheduler/scripts/Stop-Dev.ps1')
    archive.write(OUT/'HUONG_DAN_NOP.md','HUONG_DAN_NOP.md')
    archive.write(OUT/'Module_Room_Files.json','Module_Room_Files.json')
with ZipFile(zip_path) as archive:
    names=archive.namelist()
    assert not any(any(p in exclude for p in Path(n).parts) for n in names)
    assert 'MeetingScheduler/MeetingScheduler.Web/Services/RoomService.cs' in names
    assert any('/Migrations/' in name for name in names)
print(pdf)
print(zip_path)
print(f'Archive verified: {len(names)} files; no bin, obj, Logs or .vs.')
