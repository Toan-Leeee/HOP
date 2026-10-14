# ERD: 7 thực thể

Theo G1/G2 rules và `Data/Configurations/DomainConfiguration.cs`. AuditLog cùng các trường đánh dấu bổ sung trong [ENTITY_DICTIONARY.md](ENTITY_DICTIONARY.md) là baseline suy luận ngoài SRS, chưa phải xác nhận nhóm.

```mermaid
erDiagram
    Role ||--o{ User : "RoleId"
    Department ||--o{ User : "DepartmentId"
    Room ||--o{ Meeting : "RoomId"
    User ||--o{ Meeting : "CreatedByUserId organizer"
    User |o--o{ Meeting : "CancelledByUserId optional"
    Meeting ||--o{ MeetingParticipant : "MeetingId"
    User ||--o{ MeetingParticipant : "UserId"
    User ||--o{ AuditLog : "UserId actor"

    Role {
        int RoleId PK "1 Admin, 2 Organizer, 3 Employee"
        nvarchar50 RoleName UK
    }
    Department {
        int DepartmentId PK
        nvarchar100 DepartmentName UK
        bit IsDisabled
    }
    User {
        int UserId PK
        nvarchar100 FullName
        varchar100 Email UK
        varchar20 Phone "nullable"
        varchar255 PasswordHash
        int RoleId FK
        int DepartmentId FK
        bit IsDisabled
    }
    Room {
        int RoomId PK
        nvarchar100 RoomName UK
        nvarchar200 Location "nullable"
        int Capacity "greater than zero"
        nvarchar500 Equipment "nullable"
        nvarchar20 Status "Active or Maintenance"
        bit IsDisabled
    }
    Meeting {
        int MeetingId PK
        nvarchar200 Title
        nvarcharMAX Description "nullable"
        datetime StartTime
        datetime EndTime "after StartTime"
        int RoomId FK
        int CreatedByUserId FK
        nvarchar20 Status "Confirmed or Cancelled"
        rowversion RowVersion
        datetime CancelledAt "nullable"
        int CancelledByUserId FK "nullable"
        nvarchar500 CancelReason "nullable"
        datetime CreatedAt
        datetime UpdatedAt "nullable"
    }
    MeetingParticipant {
        int MeetingId PK,FK "composite key"
        int UserId PK,FK "composite key"
        nvarchar20 Status "Pending Accepted Declined"
        datetime RespondedAt "nullable"
    }
    AuditLog {
        int AuditLogId PK
        int UserId FK
        nvarchar100 Action
        nvarchar100 EntityName
        int EntityId "logical reference only"
        datetime CreatedAt
        nvarcharMAX Details "nullable"
    }
```

Mỗi User bắt buộc có một Role và một Department. Mỗi Meeting bắt buộc có một Room và một organizer; có thể không có người hủy khi chưa hủy. Một bản ghi MeetingParticipant bắt buộc thuộc đúng một Meeting và một User; cặp MeetingId/UserId duy nhất tạo quan hệ n-n Meeting/User. Parent có thể có 0 bản ghi con; `o{` không có nghĩa FK con nullable.

MeetingParticipant bị cascade khi hard delete Meeting ở cấp schema, nhưng luồng nghiệp vụ hủy dùng UPDATE Status nên vẫn giữ participant. Các quan hệ còn lại Restrict để giữ lịch sử và tránh multiple cascade paths SQL Server, đặc biệt hai FK từ Meeting đến User. AuditLog.EntityId không có đường quan hệ vật lý đến mọi bảng: nó chỉ là tham chiếu logic theo EntityName.

Chỉ mục bổ sung `(RoomId, StartTime, EndTime)` hỗ trợ tìm trùng lịch. RowVersion bảo vệ cập nhật; kiểm tra overlap của lịch mới cần transaction Serializable, không phải unique constraint trên thời gian.
