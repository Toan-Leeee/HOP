from __future__ import annotations

import re
import textwrap
from pathlib import Path

from docx import Document
from docx.enum.section import WD_SECTION
from docx.enum.table import WD_CELL_VERTICAL_ALIGNMENT, WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.shared import Inches, Pt, RGBColor
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
WEB = ROOT / "MeetingScheduler.Web"
OUT_DIR = ROOT / "docs" / "qa" / "security_demo"
ASSET_DIR = OUT_DIR / "word_assets"
OUTPUT = OUT_DIR / "Bao_cao_thuc_hanh_buoi_7_Quan_ly_lich_hop.docx"
RESULT_SCREENSHOT = OUT_DIR / "meeting-scheduler-security.png"

ASSET_DIR.mkdir(parents=True, exist_ok=True)


def source_lines(relative_path: str) -> list[str]:
    return (WEB / relative_path).read_text(encoding="utf-8-sig").splitlines()


def section(lines: list[str], start: str, end: str, include_end: bool = False) -> tuple[int, list[str]]:
    first = next(i for i, line in enumerate(lines) if start in line)
    last = next(i for i in range(first + 1, len(lines)) if end in lines[i])
    return first + 1, lines[first:last + (1 if include_end else 0)]


CODE_RE = re.compile(
    r'("(?:\\.|[^"\\])*"|//.*|\b(?:public|private|protected|sealed|static|async|await|return|if|else|new|var|using|namespace|true|false|null|try|catch|int|bool|string|class|void|const)\b|\b(?:Task|IdentityResult|UserManager|User|HttpContext|CookieOptions|CookieSecurePolicy|SameSiteMode|TimeSpan|ClaimTypes|ClaimsIdentity|ClaimsPrincipal|CancellationToken|IDataProtector|JsonSerializer|DateTimeOffset|IUserPasswordStore|IUserEmailStore|EntityState|PasswordVerificationResult|ServiceResult|AuthenticationProperties)\b|\b\d+\b)'
)


def draw_code_card(file_label: str, start_line: int, code_lines: list[str], output_name: str) -> Path:
    width = 1480
    font_path = "C:/Windows/Fonts/consola.ttf"
    bold_path = "C:/Windows/Fonts/consolab.ttf"
    font = ImageFont.truetype(font_path, 20)
    bold = ImageFont.truetype(bold_path, 22)
    small = ImageFont.truetype(font_path, 17)
    max_chars = 116
    expanded: list[tuple[int, str]] = []
    for offset, raw in enumerate(code_lines):
        raw = raw.expandtabs(4).rstrip()
        pieces = textwrap.wrap(raw, width=max_chars, subsequent_indent="    ",
                               break_long_words=True, break_on_hyphens=False) or [""]
        for piece_index, piece in enumerate(pieces):
            expanded.append((start_line + offset if piece_index == 0 else 0, piece))

    top = 68
    row_height = 32
    bottom = 24
    height = top + row_height * len(expanded) + bottom
    image = Image.new("RGB", (width, height), "#1E2430")
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, width, 54), fill="#151A23")
    draw.ellipse((19, 19, 31, 31), fill="#E06C75")
    draw.ellipse((41, 19, 53, 31), fill="#E5C07B")
    draw.ellipse((63, 19, 75, 31), fill="#98C379")
    draw.text((98, 14), file_label, font=bold, fill="#E6EAF2")
    draw.text((width - 270, 18), f"SOURCE LINES {start_line}-{start_line + len(code_lines) - 1}",
              font=small, fill="#929BAC")
    y = top
    for line_number, code in expanded:
        draw.text((24, y + 3), str(line_number) if line_number else "", font=small, fill="#737E91")
        x = 104
        cursor = 0
        for match in CODE_RE.finditer(code):
            if match.start() > cursor:
                plain = code[cursor:match.start()]
                draw.text((x, y), plain, font=font, fill="#D7DAE0")
                x += int(draw.textlength(plain, font=font))
            token = match.group(0)
            if token.startswith("//"):
                color = "#7F899B"
            elif token.startswith('"'):
                color = "#98C379"
            elif token.isdigit():
                color = "#D19A66"
            elif token in {"public", "private", "protected", "sealed", "static", "async", "await", "return", "if", "else", "new", "var", "using", "namespace", "true", "false", "null", "try", "catch", "int", "bool", "string", "class", "void", "const"}:
                color = "#C678DD"
            else:
                color = "#61AFEF"
            draw.text((x, y), token, font=font, fill=color)
            x += int(draw.textlength(token, font=font))
            cursor = match.end()
        if cursor < len(code):
            draw.text((x, y), code[cursor:], font=font, fill="#D7DAE0")
        y += row_height
    path = ASSET_DIR / output_name
    image.save(path, dpi=(150, 150), optimize=True)
    return path


def shade(cell, fill: str) -> None:
    tc_pr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:fill"), fill)
    tc_pr.append(shd)


def set_cell_borders(cell, color="D9DDE3", size="6") -> None:
    tc_pr = cell._tc.get_or_add_tcPr()
    borders = tc_pr.first_child_found_in("w:tcBorders")
    if borders is None:
        borders = OxmlElement("w:tcBorders")
        tc_pr.append(borders)
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        tag = "w:" + edge
        element = borders.find(qn(tag))
        if element is None:
            element = OxmlElement(tag)
            borders.append(element)
        element.set(qn("w:val"), "single")
        element.set(qn("w:sz"), size)
        element.set(qn("w:space"), "0")
        element.set(qn("w:color"), color)


def set_repeat_table_header(row) -> None:
    tr_pr = row._tr.get_or_add_trPr()
    tbl_header = OxmlElement("w:tblHeader")
    tbl_header.set(qn("w:val"), "true")
    tr_pr.append(tbl_header)


def add_field(paragraph, instruction: str) -> None:
    run = paragraph.add_run()
    begin = OxmlElement("w:fldChar")
    begin.set(qn("w:fldCharType"), "begin")
    instr = OxmlElement("w:instrText")
    instr.set(qn("xml:space"), "preserve")
    instr.text = instruction
    separate = OxmlElement("w:fldChar")
    separate.set(qn("w:fldCharType"), "separate")
    text = OxmlElement("w:t")
    text.text = "1"
    end = OxmlElement("w:fldChar")
    end.set(qn("w:fldCharType"), "end")
    run._r.extend([begin, instr, separate, text, end])


def set_run_font(run, name="Arial", size=10.5, color="222222", bold=False):
    run.font.name = name
    run._element.get_or_add_rPr().rFonts.set(qn("w:ascii"), name)
    run._element.get_or_add_rPr().rFonts.set(qn("w:hAnsi"), name)
    run.font.size = Pt(size)
    run.font.color.rgb = RGBColor.from_string(color)
    run.bold = bold


def format_paragraph(p, before=0, after=6, line=1.12):
    p.paragraph_format.space_before = Pt(before)
    p.paragraph_format.space_after = Pt(after)
    p.paragraph_format.line_spacing = line


def add_body(doc, text, lead=None):
    p = doc.add_paragraph()
    format_paragraph(p, after=6)
    if lead and text.startswith(lead):
        set_run_font(p.add_run(lead), bold=True)
        set_run_font(p.add_run(text[len(lead):]))
    else:
        set_run_font(p.add_run(text))
    return p


def add_bullet(doc, text):
    p = doc.add_paragraph(style="List Bullet")
    format_paragraph(p, after=3, line=1.08)
    set_run_font(p.add_run(text), size=10)
    return p


def add_heading(doc, text, level=1):
    p = doc.add_paragraph(style=f"Heading {level}")
    format_paragraph(p, before=8 if level == 1 else 5, after=5, line=1.0)
    run = p.add_run(text)
    set_run_font(run, size=16 if level == 1 else 12, color="000000", bold=True)
    p.paragraph_format.keep_with_next = True
    return p


def add_code_figure(doc, image_path: Path, caption: str, width=6.62):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(2)
    p.paragraph_format.space_after = Pt(1)
    p.add_run().add_picture(str(image_path), width=Inches(width))
    cap = doc.add_paragraph()
    cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
    format_paragraph(cap, after=7, line=1.0)
    set_run_font(cap.add_run(caption), size=9, color="555555")
    cap.runs[0].italic = True


def add_table(doc, headers, rows, widths=None, font_size=9):
    table = doc.add_table(rows=1, cols=len(headers))
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    if widths:
        for row in table.rows:
            for i, width in enumerate(widths):
                row.cells[i].width = Inches(width)
    header = table.rows[0]
    set_repeat_table_header(header)
    for i, label in enumerate(headers):
        cell = header.cells[i]
        shade(cell, "17365D")
        set_cell_borders(cell)
        cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
        p = cell.paragraphs[0]
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
        format_paragraph(p, after=0, line=1.0)
        set_run_font(p.add_run(label), size=font_size, color="FFFFFF", bold=True)
    for row_index, values in enumerate(rows):
        cells = table.add_row().cells
        if widths:
            for i, width in enumerate(widths):
                cells[i].width = Inches(width)
        for i, value in enumerate(values):
            cell = cells[i]
            shade(cell, "F2F5F8" if row_index % 2 == 1 else "FFFFFF")
            set_cell_borders(cell)
            cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
            p = cell.paragraphs[0]
            format_paragraph(p, after=1, line=1.06)
            set_run_font(p.add_run(value), size=font_size)
    doc.add_paragraph().paragraph_format.space_after = Pt(1)
    return table


def code_asset(relative_path, start, end, filename, include_end=False):
    lines = source_lines(relative_path)
    start_line, excerpt = section(lines, start, end, include_end=include_end)
    return draw_code_card(relative_path.replace("\\", "/"), start_line, excerpt, filename)


def main():
    if not RESULT_SCREENSHOT.exists():
        raise FileNotFoundError(f"Missing current application screenshot: {RESULT_SCREENSHOT}")

    assets = {}
    assets["cookie"] = code_asset("Services/UserActivityStateService.cs", "public void RecordRequest(", "public void StartSession(", "code_01_cookie.png")
    assets["session_config"] = code_asset("Program.cs", "builder.Services.AddSession(options =>", "builder.Services.AddIdentityCore<User>", "code_02_session_config.png")
    assets["session_login"] = code_asset("Controllers/AccountController.cs", "var identity = new ClaimsIdentity", "return Url.IsLocalUrl", "code_03_session_login.png")
    assets["session_logout"] = code_asset("Controllers/AccountController.cs", "public async Task<IActionResult> Logout()", "return RedirectToAction(nameof(Login));", "code_04_session_logout.png", include_end=True)
    assets["identity_setup"] = code_asset("Program.cs", "builder.Services.AddIdentityCore<User>", "builder.Services.AddScoped<IUserActivityStateService", "code_05_identity_setup.png")
    assets["identity_store"] = code_asset("Services/MeetingUserStore.cs", "public Task SetPasswordHashAsync", "public Task<User?> FindByEmailAsync", "code_06_identity_store.png")
    assets["identity_check"] = code_asset("Services/AccountService.cs", "bool passwordValid;", "db.AuditLogs.Add(ServiceSupport.Audit(user.UserId", "code_07_identity_login.png")
    assets["identity_change"] = code_asset("Services/AccountService.cs", "IdentityResult result;", "return ServiceResult.Success(userId);", "code_08_identity_password.png", include_end=True)
    assets["auth_cookie"] = code_asset("Program.cs", "builder.Services.AddAuthentication(", "builder.Services.AddAuthorization();", "code_09_auth_cookie.png")
    assets["role_guard"] = code_asset("Controllers/MeetingController.cs", "[Authorize(Roles = \"Organizer,Admin\"), HttpGet]", "return View(new MeetingEditViewModel());", "code_10_role_guard.png", include_end=True)
    assets["owner_guard"] = code_asset("Controllers/MeetingController.cs", "public async Task<IActionResult> Cancel(int id", "ViewBag.Meeting = meeting;", "code_11_owner_guard.png", include_end=True)

    doc = Document()
    section0 = doc.sections[0]
    section0.page_width = Inches(8.27)
    section0.page_height = Inches(11.69)
    section0.top_margin = Inches(0.62)
    section0.bottom_margin = Inches(0.62)
    section0.left_margin = Inches(0.72)
    section0.right_margin = Inches(0.72)
    section0.header_distance = Inches(0.3)
    section0.footer_distance = Inches(0.3)

    styles = doc.styles
    normal = styles["Normal"]
    normal.font.name = "Arial"
    normal._element.rPr.rFonts.set(qn("w:ascii"), "Arial")
    normal._element.rPr.rFonts.set(qn("w:hAnsi"), "Arial")
    normal.font.size = Pt(10.5)
    normal.font.color.rgb = RGBColor(34, 34, 34)
    for style_name in ("Heading 1", "Heading 2", "Title"):
        style = styles[style_name]
        style.font.name = "Arial"
        style._element.rPr.rFonts.set(qn("w:ascii"), "Arial")
        style._element.rPr.rFonts.set(qn("w:hAnsi"), "Arial")
        style.font.color.rgb = RGBColor(0, 0, 0)

    header = section0.header.paragraphs[0]
    header.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    format_paragraph(header, after=0, line=1.0)
    set_run_font(header.add_run("ỨNG DỤNG QUẢN LÝ LỊCH HỌP"), size=8, color="657185", bold=True)
    footer = section0.footer.paragraphs[0]
    footer.alignment = WD_ALIGN_PARAGRAPH.CENTER
    format_paragraph(footer, after=0, line=1.0)
    set_run_font(footer.add_run("Báo cáo thực hành buổi 7  |  Trang "), size=8, color="657185")
    add_field(footer, "PAGE")

    title = doc.add_paragraph(style="Title")
    title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    title.paragraph_format.space_before = Pt(18)
    title.paragraph_format.space_after = Pt(4)
    set_run_font(title.add_run("BÁO CÁO THỰC HÀNH BUỔI 7"), size=22, color="000000", bold=True)
    subtitle = doc.add_paragraph()
    subtitle.alignment = WD_ALIGN_PARAGRAPH.CENTER
    format_paragraph(subtitle, after=13, line=1.0)
    set_run_font(subtitle.add_run("BẢO MẬT TRONG ỨNG DỤNG QUẢN LÝ LỊCH HỌP"), size=14, color="333333", bold=True)
    meta = doc.add_paragraph()
    meta.alignment = WD_ALIGN_PARAGRAPH.CENTER
    format_paragraph(meta, after=15, line=1.0)
    set_run_font(meta.add_run("Môn học: ASP.NET Core MVC  |  Người thực hiện: ................................  |  Ngày: 28/10/2026"), size=9, color="5C6470")

    add_heading(doc, "Mục tiêu và phạm vi", 1)
    add_body(doc, "Báo cáo trình bày việc áp dụng yêu cầu thực hành về Cookie, Session và ASP.NET Core Identity vào hệ thống quản lý lịch họp MeetingScheduler. Các cơ chế được tích hợp với tài khoản, lịch họp và vai trò có sẵn của ứng dụng; giao diện chỉ hiển thị thông tin hữu ích cho người dùng.")
    add_table(doc, ["Yêu cầu thực hành", "Cách áp dụng trong hệ thống", "Minh chứng"], [
        ("Persistent Cookie", "Ghi nhớ lượt truy cập, thời điểm gần nhất và chức năng thường dùng trên thiết bị trong 30 ngày.", "UserActivityStateService.cs"),
        ("Session", "Giữ thông tin phiên làm việc; khởi tạo khi đăng nhập, hết hạn sau 30 phút không hoạt động và xóa khi đăng xuất.", "Program.cs, AccountController.cs"),
        ("ASP.NET Core Identity", "Dùng UserManager và kho người dùng tùy chỉnh để kiểm tra/đổi mật khẩu trên bảng User hiện tại.", "Program.cs, MeetingUserStore.cs, AccountService.cs"),
        ("Authentication / Authorization", "Dùng cookie xác thực, kiểm tra trạng thái tài khoản, phân quyền theo vai trò và quyền sở hữu cuộc họp.", "Program.cs, MeetingController.cs"),
    ], widths=[1.25, 3.55, 1.8], font_size=8.4)
    add_heading(doc, "Cấu trúc bảo mật", 2)
    add_body(doc, "Cookie đăng nhập được ASP.NET Core bảo vệ và mang Claims đã ký. Cookie ghi nhớ hoạt động chỉ chứa số liệu sử dụng, không chứa mật khẩu hoặc quyền. Session lưu trạng thái làm việc phía máy chủ; quyết định cho phép thao tác vẫn dựa trên Claims và dữ liệu tài khoản trong cơ sở dữ liệu.")

    doc.add_page_break()
    add_heading(doc, "1. Ghi nhớ hoạt động bằng Cookie", 1)
    add_body(doc, "Sau mỗi yêu cầu GET của người dùng đã đăng nhập, middleware ghi tăng số lượt truy cập, thời điểm truy cập và bộ đếm theo chức năng như xem lịch, xem chi tiết hoặc theo dõi RSVP. Danh sách chức năng được giới hạn ở tám mục để giữ dữ liệu nhỏ.")
    add_body(doc, "Giá trị được mã hóa/bảo vệ bằng ASP.NET Core Data Protection trước khi gửi về trình duyệt. Cookie hết hạn sau 30 ngày, đặt HttpOnly và SameSite=Lax; cờ Secure bật khi yêu cầu đi qua HTTPS. Cookie này chỉ phục vụ thống kê sử dụng, không được dùng để xác thực hoặc phân quyền.")
    add_code_figure(doc, assets["cookie"], "Hình 1. Ghi nhận hoạt động và tạo Cookie được bảo vệ trong UserActivityStateService.cs")
    add_heading(doc, "Các thuộc tính an toàn", 2)
    add_table(doc, ["Thuộc tính", "Mục đích trong ứng dụng"], [
        ("Expires", "Lưu thống kê qua các lần mở trình duyệt trong thời hạn 30 ngày."),
        ("HttpOnly", "Ngăn JavaScript phía trình duyệt đọc trực tiếp giá trị Cookie."),
        ("Secure", "Chỉ gửi Cookie qua HTTPS khi ứng dụng đang sử dụng HTTPS."),
        ("SameSite=Lax", "Giảm khả năng trình duyệt gửi Cookie trong yêu cầu liên trang không mong muốn."),
        ("Data Protection", "Bảo vệ tính bí mật và toàn vẹn dữ liệu thống kê được lưu trong Cookie."),
    ], widths=[1.6, 5.0], font_size=9)

    doc.add_page_break()
    add_heading(doc, "2. Quản lý phiên làm việc", 1)
    add_body(doc, "Ứng dụng bật Session với thời gian chờ 30 phút, Cookie phiên HttpOnly, SameSite=Lax và SecurePolicy theo giao thức yêu cầu. Khi đăng nhập thành công, hệ thống tạo trạng thái phiên gồm mã người dùng, vai trò hiển thị, thời điểm đăng nhập và bộ đếm hoạt động. Khi đăng xuất, Session được xóa.")
    add_code_figure(doc, assets["session_config"], "Hình 2. Cấu hình Session trong Program.cs")
    add_code_figure(doc, assets["session_login"], "Hình 3. Đăng nhập, tạo Claims và khởi tạo trạng thái phiên trong AccountController.cs")
    add_code_figure(doc, assets["session_logout"], "Hình 4. Xóa Session và kết thúc phiên xác thực khi đăng xuất")
    add_body(doc, "Giá trị vai trò trong Session chỉ hỗ trợ lưu trạng thái làm việc. Bộ lọc quyền không dựa vào giá trị đó; quyền truy cập dùng Claims trong cookie xác thực và được kiểm tra lại với cơ sở dữ liệu.")

    doc.add_page_break()
    add_heading(doc, "3. Tích hợp ASP.NET Core Identity", 1)
    add_body(doc, "Identity Core được đăng ký với lớp User hiện có. Chính sách mật khẩu yêu cầu tối thiểu tám ký tự, có chữ hoa, chữ thường, chữ số và ký tự đặc biệt. Email phải duy nhất.")
    add_code_figure(doc, assets["identity_setup"], "Hình 5. Đăng ký Identity Core và chính sách mật khẩu trong Program.cs")
    add_body(doc, "Bảng User của dự án không kế thừa IdentityUser và đang được các chức năng lịch họp sử dụng. Vì vậy MeetingUserStore triển khai IUserPasswordStore và IUserEmailStore để nối UserManager với bảng hiện có, không tạo thêm bộ bảng Identity hoặc thay đổi lược đồ dữ liệu.")
    add_code_figure(doc, assets["identity_store"], "Hình 6. Kho người dùng tùy chỉnh ánh xạ mật khẩu và email vào bảng User")

    doc.add_page_break()
    add_heading(doc, "4. Xác thực mật khẩu và quản lý tài khoản", 1)
    add_body(doc, "Đăng nhập dùng UserManager.CheckPasswordAsync để kiểm tra mật khẩu đã băm. Đổi mật khẩu dùng UserManager.ChangePasswordAsync, qua đó áp dụng bộ kiểm tra mật khẩu của Identity; nếu thành công, hệ thống ghi AuditLog. Mật khẩu gốc không được ghi vào cơ sở dữ liệu hoặc Cookie.")
    add_code_figure(doc, assets["identity_check"], "Hình 7. Kiểm tra mật khẩu khi đăng nhập bằng UserManager trong AccountService.cs")
    add_code_figure(doc, assets["identity_change"], "Hình 8. Đổi mật khẩu qua Identity và ghi nhật ký thao tác")
    add_body(doc, "Sau khi kiểm tra mật khẩu, ứng dụng tạo cookie xác thực chứa UserId, tên, email và vai trò. RememberMe quyết định cookie đăng nhập có được duy trì qua lần đóng trình duyệt hay không.")

    doc.add_page_break()
    add_heading(doc, "5. Phân quyền theo vai trò và dữ liệu", 1)
    add_body(doc, "Cookie xác thực có thời hạn tối đa tám giờ, đặt HttpOnly, SameSite=Lax và Secure khi dùng HTTPS. Mỗi yêu cầu được kiểm tra lại với cơ sở dữ liệu để từ chối tài khoản bị khóa, phòng ban bị khóa hoặc vai trò không còn khớp.")
    add_code_figure(doc, assets["auth_cookie"], "Hình 9. Cấu hình cookie xác thực và kiểm tra lại tài khoản trong Program.cs")
    add_body(doc, "Tạo, sửa và hủy cuộc họp yêu cầu vai trò Organizer hoặc Admin. Ngoài kiểm tra vai trò, người tổ chức chỉ được sửa/hủy cuộc họp do mình tạo; Admin có thể quản lý toàn hệ thống.")
    add_code_figure(doc, assets["role_guard"], "Hình 10. Giới hạn chức năng tạo lịch theo vai trò trong MeetingController.cs")
    add_code_figure(doc, assets["owner_guard"], "Hình 11. Kiểm tra quyền sở hữu cuộc họp trước khi cho phép hủy lịch")

    result_heading = add_heading(doc, "6. Kết quả chạy ứng dụng", 1)
    result_heading.paragraph_format.page_break_before = True
    add_body(doc, "Ứng dụng chạy trên ASP.NET Core MVC và kết nối LocalDB đã cấu hình. Trang Quản lý tài khoản hiển thị thông tin hồ sơ, hoạt động gần đây và quyền sử dụng theo vai trò Organizer; không hiển thị chi tiết kỹ thuật nội bộ về cơ chế lưu trạng thái.")
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_after = Pt(1)
    p.add_run().add_picture(str(RESULT_SCREENSHOT), width=Inches(6.62))
    caption = doc.add_paragraph()
    caption.alignment = WD_ALIGN_PARAGRAPH.CENTER
    format_paragraph(caption, after=8, line=1.0)
    set_run_font(caption.add_run("Hình 12. Trang Quản lý tài khoản sau khi đăng nhập với vai trò Người tổ chức"), size=9, color="555555")
    caption.runs[0].italic = True
    add_heading(doc, "Cách chạy và kiểm tra", 2)
    add_body(doc, "Từ thư mục MeetingScheduler, chạy lệnh: dotnet run --project .\\MeetingScheduler.Web\\MeetingScheduler.Web.csproj --urls http://localhost:5086. Đăng nhập bằng tài khoản demo organizer@company.test với mật khẩu Demo@12345, sau đó mở /Account/Security.")
    add_bullet(doc, "Organizer được tạo, cập nhật, hủy lịch trong phạm vi được phép; không có quyền quản trị nhân sự và danh mục.")
    add_bullet(doc, "Kiểm tra đăng xuất: Session bị xóa và cookie xác thực bị thu hồi.")
    add_bullet(doc, "Build gần nhất: thành công, 0 cảnh báo và 0 lỗi. Đã mở trang tài khoản và xác nhận dữ liệu hiển thị đúng vai trò.")
    add_heading(doc, "Kết luận", 2)
    add_body(doc, "Các yêu cầu thực hành về Cookie, Session, Identity, xác thực và phân quyền đã được áp dụng vào quy trình quản lý lịch họp. Cấu trúc tài khoản và cơ sở dữ liệu hiện có được giữ nguyên, trong khi thao tác đăng nhập, đổi mật khẩu và giới hạn quyền được gắn trực tiếp với nghiệp vụ cuộc họp.")

    add_heading(doc, "Tài liệu tham khảo", 2)
    add_body(doc, "Lect7 ASP.NET Core MVC Security ver2.1, phần thực hành buổi 7 về Cookies, Session và Identity Framework.")

    doc.core_properties.title = "Báo cáo thực hành buổi 7 về bảo mật trong ứng dụng quản lý lịch họp"
    doc.core_properties.subject = "Cookie, Session, ASP.NET Core Identity, Authentication và Authorization"
    doc.core_properties.author = ""
    doc.save(OUTPUT)
    print(OUTPUT)
    print(f"Embedded code captures: {len(assets)}")
    print(f"Embedded application screenshot: {RESULT_SCREENSHOT}")


if __name__ == "__main__":
    main()
