from pathlib import Path
from copy import deepcopy
from docx import Document
from docx.shared import Pt
from docx.shared import RGBColor
import sys

sys.stdout.reconfigure(encoding="utf-8")

SOURCE = Path(r"C:\Users\acer\Downloads\Phân công thực hiện WNC.docx")
OUTPUT = Path(r"C:\Users\acer\Downloads\Phân công thực hiện WNC_điều chỉnh.docx")


def replace_paragraph(paragraph, text, bold_prefix=None):
    paragraph.clear()
    if bold_prefix and text.startswith(bold_prefix):
        paragraph.add_run(bold_prefix).bold = True
        paragraph.add_run(text[len(bold_prefix):])
    else:
        paragraph.add_run(text)


def replace_cell(cell, text, bold=False):
    p = cell.paragraphs[0]
    p.clear()
    run = p.add_run(text)
    run.bold = bold
    run.font.size = Pt(10)
    for extra in cell.paragraphs[1:]:
        extra.clear()


def main():
    doc = Document(SOURCE)
    p = doc.paragraphs

    # Add the selected per-module query pipeline and index ownership.
    insert_before = p[68]
    additions = [
        (61, "Module Theo dõi phản hồi tham dự (RSVP):", True),
        (62, "Rà soát trạng thái Pending/Accepted/Declined, thời điểm phản hồi và quyền xem danh sách RSVP.", False),
        (61, "Query Pipeline cho RSVP: Lọc theo cuộc họp/trạng thái, sắp xếp theo thời gian phản hồi và phân trang.", True),
        (61, "Module Báo cáo thống kê:", True),
        (62, "Rà soát tổng số cuộc họp, số cuộc họp hủy, RSVP và thời lượng sử dụng theo phòng/khoảng thời gian.", False),
    ]
    for template_index, text, is_heading in additions:
        added = insert_before.insert_paragraph_before(text, style=p[template_index].style)
        template_ppr = p[template_index]._p.pPr
        if template_ppr is not None:
            current_ppr = added._p.pPr
            if current_ppr is not None:
                added._p.remove(current_ppr)
            added._p.insert(0, deepcopy(template_ppr))
        if is_heading:
            added.runs[0].bold = True

    # Clarify that this is an ownership and completion plan, not a claim of sole authorship.
    p[4].insert_paragraph_before(
        "Phân công đã điều chỉnh theo bốn cụm module. Người phụ trách chịu trách nhiệm rà soát, hoàn thiện, kiểm tra và viết phần báo cáo của module. Mã nguồn nền hiện có được xây dựng trong quá trình làm chung giữa Trường và trợ lý AI; bảng này không khẳng định một cá nhân đã viết toàn bộ CRUD."
    )

    table = doc.tables[0]
    summary = [
        ("Toản", "FS01–FS02 · Room: CRUD phòng họp, sức chứa/vị trí, trạng thái bảo trì; tìm kiếm/lọc/sắp xếp/phân trang.", "Mô tả quản lý phòng, truy vấn danh sách và minh chứng CRUD."),
        ("Trường", "FS03 · Meeting/MeetingParticipant: tạo/sửa/hủy lịch, kiểm tra trùng phòng, mời người dự, RSVP và truy vấn lịch.", "Mô tả luồng đặt lịch, kiểm tra xung đột, RSVP và query pipeline."),
        ("Chiến", "FS05 · Department/Role/AuditLog: CRUD phòng ban, quản lý vai trò, tra cứu nhật ký; lọc/sắp xếp/phân trang.", "Mô tả danh mục, phân quyền, nhật ký và query pipeline."),
        ("Hiếu", "FS04 · User: CRUD nhân sự, băm mật khẩu, gán phòng ban/vai trò, khóa/mở khóa; tìm kiếm/lọc/sắp xếp/phân trang.", "Mô tả User Management, query pipeline, ảnh giao diện và tổng hợp báo cáo."),
    ]
    for row, values in zip(table.rows[1:], summary):
        for column, (cell, value) in enumerate(zip(row.cells, values)):
            replace_cell(cell, value, bold=column == 0)

    changes = {
        4: ("1. TOẢN (FS01–FS02 · Quản lý Phòng họp)", None),
        6: ("Rà soát CRUD phòng họp và dữ liệu Room; phối hợp kiểm tra cấu hình entity/migration liên quan đến Room.", None),
        7: ("Quản lý thông tin phòng gồm tên, sức chứa, vị trí, thiết bị và trạng thái hoạt động/bảo trì.", None),
        9: ("Module Quản lý Phòng họp (Room):", None),
        10: ("Kiểm tra thêm, xem, sửa, ngừng sử dụng/khôi phục phòng họp theo quyền Admin.", "Kiểm tra thêm"),
        11: ("Query Pipeline (Index): lọc theo tên phòng, trạng thái và sức chứa tối thiểu; sắp xếp theo tên/sức chứa; phân trang.", "Query Pipeline (Index):"),
        12: ("Kiểm tra tính hợp lệ của sức chứa, vị trí và trạng thái phòng trước khi lưu.", None),
        14: ("Phối hợp với Trường kiểm tra phòng đang hoạt động, sức chứa và tình trạng trùng lịch trong luồng đặt họp.", None),
        16: ("Mô tả module Room, dữ liệu quản lý và quyền Admin.", "Chương:"),
        17: ("Mô tả CRUD phòng họp, lọc/tìm kiếm/sắp xếp/phân trang và ảnh minh chứng.", "Chương:"),
        18: ("Ghi nhận kết quả kiểm tra và các giới hạn còn lại của module Room.", "Chương:"),

        20: ("2. TRƯỜNG (FS03 · Quản lý Đặt lịch họp & RSVP)", None),
        23: ("Rà soát MeetingController, MeetingService và luồng đăng nhập/phân quyền cần cho thao tác đặt lịch.", None),
        24: ("Hoàn thiện tạo lịch: nhập tiêu đề, thời gian, phòng họp và danh sách người tham dự.", None),
        25: ("Kiểm tra xung đột thời gian/phòng, sức chứa và quyền tổ chức trước khi lưu.", None),
        26: ("Rà soát cập nhật/hủy lịch, kiểm tra quyền sở hữu và xử lý dữ liệu cập nhật đồng thời.", None),
        27: ("Module RSVP (MeetingParticipant):", None),
        28: ("Cho phép người được mời phản hồi tham dự hoặc từ chối; lưu trạng thái và thời điểm phản hồi.", None),
        29: ("Kiểm tra người dùng chỉ xem/phản hồi cuộc họp có liên quan; người tổ chức xem danh sách phản hồi.", None),
        30: ("Query Pipeline cho Meeting và RSVP:", None),
        31: ("Lọc lịch theo tiêu đề, phòng, khoảng thời gian; lọc RSVP theo cuộc họp/trạng thái.", None),
        32: ("Sắp xếp theo thời gian họp, tiêu đề hoặc thời điểm phản hồi; phân trang danh sách.", None),
        34: ("Mô tả luồng tạo/sửa/hủy cuộc họp và kiểm tra trùng phòng.", "Chương:"),
        35: ("Mô tả RSVP, quyền truy cập và query pipeline của Meeting/MeetingParticipant.", "Chương:"),
        36: ("Bổ sung ảnh minh chứng luồng đặt lịch và phản hồi tham dự.", "Phụ lục:"),

        38: ("3. CHIẾN (FS05 · Phòng ban, Vai trò & Nhật ký)", None),
        41: ("Rà soát DepartmentController và các giao diện quản trị phòng ban.", None),
        42: ("Kiểm tra thêm, sửa, ngừng sử dụng/khôi phục phòng ban và lưu AuditLog.", None),
        44: ("Quản lý Role và quyền truy cập theo vai trò Admin, Organizer, Employee.", None),
        45: ("Tra cứu nhật ký thao tác theo hành động/người thực hiện; bảo đảm thao tác quản trị quan trọng có ghi log.", None),
        48: ("Query Pipeline cho Department và AuditLog:", None),
        49: ("Lọc phòng ban theo tên/trạng thái; lọc nhật ký theo hành động/người thực hiện.", None),
        50: ("Sắp xếp và phân trang:", None),
        51: ("Sắp xếp phòng ban theo tên và nhật ký theo thời gian tạo.", None),
        52: ("Kiểm tra phân trang danh sách phòng ban và nhật ký khi có nhiều bản ghi.", None),
        53: ("Phối hợp Hiếu rà soát thông tin vai trò trong màn hình quản lý nhân sự; ghi nhận các quyền chưa được triển khai nếu có.", None),
        55: ("Mô tả CRUD Department, quản lý Role, AuditLog và query pipeline; kèm ảnh minh chứng.", "Chương:"),
        56: ("Đánh giá kết quả, hạn chế và hướng phát triển của module quản trị.", "Chương:"),

        58: ("4. HIẾU (FS04 · Quản lý Nhân sự)", None),
        61: ("Rà soát UserController và các giao diện quản trị nhân sự trong Admin Area.", None),
        62: ("Kiểm tra CRUD User, băm mật khẩu, gán phòng ban/vai trò và khóa/mở khóa theo soft delete.", None),
        63: ("Query Pipeline (Index): lọc tên/email, phòng ban, vai trò và trạng thái; sắp xếp họ tên/email; phân trang.", None),
        65: ("Rà soát ProfileController và các màn hình hồ sơ cá nhân/đổi mật khẩu.", None),
        66: ("Kiểm tra hiển thị và cập nhật họ tên, email, số điện thoại; thông tin phòng ban/vai trò lấy theo dữ liệu User.", None),
        67: ("Kiểm tra đổi mật khẩu với mật khẩu hiện tại và quy tắc mật khẩu mới.", None),
        69: ("Kiểm tra giao diện responsive cho trang nhân sự và hồ sơ; phối hợp chụp minh chứng các module nhóm.", None),
        70: ("Ghi nhận và sửa lỗi tràn khung, lệch nút hoặc thành phần khó sử dụng; chụp ảnh minh chứng sau khi kiểm tra.", None),
        72: ("Viết giới thiệu ứng dụng và bảng phân công theo trách nhiệm rà soát/hoàn thiện; ghi rõ mã nguồn được phát triển trong quá trình làm chung.", "Mở đầu:"),
        73: ("Mô tả CRUD User, query pipeline, khóa/mở khóa và thông tin hồ sơ; bổ sung ảnh giao diện.", "Chương:"),
        74: ("Thu thập nội dung các thành viên, thống nhất định dạng Times New Roman 13, căn lề theo yêu cầu môn học, dãn dòng 1.5, số trang, mục lục và danh mục hình.", "Tổng hợp & Định dạng Báo cáo:"),
    }
    for index, (text, bold_prefix) in changes.items():
        replace_paragraph(p[index], text, bold_prefix)
        if p[index].style.name.startswith("Heading"):
            run = p[index].runs[0]
            run.bold = True
            run.italic = False
            run.font.color.rgb = RGBColor(0, 0, 0)

    doc.core_properties.title = "Phân công thực hiện WNC đã điều chỉnh"
    doc.save(OUTPUT)
    print(OUTPUT)


if __name__ == "__main__":
    main()
