from pathlib import Path
from xml.etree import ElementTree as ET
from html import escape

BASE = Path(__file__).resolve().parents[1]
OUT = BASE / 'docs' / 'ERD_MeetingScheduler.drawio'
doc = ET.Element('mxfile', host='app.diagrams.net', type='device', version='26.0.0')
diagram = ET.SubElement(doc, 'diagram', id='meeting-erd', name='ERD 8 bảng')
model = ET.SubElement(diagram, 'mxGraphModel', grid='1', gridSize='10', page='1', pageWidth='1530', pageHeight='1530', dx='1530', dy='1530', background='#ffffff', connect='1', arrows='1')
root = ET.SubElement(model, 'root')
ET.SubElement(root, 'mxCell', id='0')
ET.SubElement(root, 'mxCell', id='1', parent='0')

def cell(id, value, style, x, y, w, h, parent='1'):
    c = ET.SubElement(root, 'mxCell', id=id, value=value, style=style, vertex='1', parent=parent)
    ET.SubElement(c, 'mxGeometry', x=str(x), y=str(y), width=str(w), height=str(h), attrib={'as': 'geometry'})
    return c

def text(id, value, x, y, w, h, size=14, bold=False):
    return cell(id, value, f'text;html=1;whiteSpace=wrap;align=left;verticalAlign=middle;fontFamily=Arial;fontSize={size};fontStyle={1 if bold else 0};fontColor=#243347;', x,y,w,h)

def entity(name, subtitle, x,y,w, rows, color):
    header = f'<b>{name}</b>  <span style="font-size:12px">{subtitle}</span>'
    cell(name, header, f'swimlane;html=1;startSize=34;horizontal=1;collapsible=0;rounded=0;fillColor={color};swimlaneFillColor=#ffffff;strokeColor=#8191a0;fontColor=#16324a;align=left;spacingLeft=12;fontFamily=Arial;fontSize=17;container=1;', x,y,w,34+26*len(rows))
    for i,(key,field,kind,nullable) in enumerate(rows):
        flag = f'<b>{escape(key)}</b>' if key else '&nbsp;'
        value = f'<table style="width:100%;border-collapse:collapse"><tr><td style="width:53px;font-size:11px;color:#405569">{flag}</td><td style="font-size:13px">{escape(field)}</td><td style="text-align:right;font-size:12px;color:#506175">&nbsp;: {escape(kind)}{" ?" if nullable else ""}</td></tr></table>'
        cell(f'{name}.{field}',value,'text;html=1;whiteSpace=wrap;align=left;verticalAlign=middle;spacingLeft=10;spacingRight=10;strokeColor=#e1e7ed;fillColor=#ffffff;fontFamily=Arial;movable=0;resizable=0;',0,34+i*26,w,26,name)

entity('Role','Vai trò',60,220,330,[('PK','RoleId','int',False),('UK','RoleName','nvarchar(50)',False)],'#dcebf8')
entity('Department','Phòng ban',60,390,330,[('PK','DepartmentId','int',False),('UK','DepartmentName','nvarchar(100)',False),('','IsDisabled','bit',False)],'#dcefe6')
entity('User','Người dùng',550,250,360,[('PK','UserId','int',False),('','FullName','nvarchar(100)',False),('UK','Email','varchar(100)',False),('','Phone','varchar(20)',True),('','PasswordHash','varchar(255)',False),('FK','RoleId','int',False),('FK','DepartmentId','int',False),('','IsDisabled','bit',False)],'#dcebf8')
entity('Room','Phòng họp',1070,100,400,[('PK','RoomId','int',False),('UK','RoomName','nvarchar(100)',False),('','Location','nvarchar(200)',True),('','Capacity','int > 0',False),('','Equipment','nvarchar(500)',True),('','Status','nvarchar(20)',False),('','IsDisabled','bit',False)],'#dcefe6')
entity('Meeting','Cuộc họp',1070,500,400,[('PK','MeetingId','int',False),('','Title','nvarchar(200)',False),('','Description','nvarchar(max)',True),('','StartTime','datetime',False),('','EndTime','datetime',False),('FK','RoomId','int',False),('FK','CreatedByUserId','int',False),('','Status','nvarchar(20)',False),('','RowVersion','rowversion',False),('','CancelledAt','datetime',True),('FK','CancelledByUserId','int',True),('','CancelReason','nvarchar(500)',True),('','CreatedAt','datetime',False),('','UpdatedAt','datetime',True)],'#dcebf8')
entity('MeetingParticipant','Người tham dự',550,800,360,[('PK, FK','MeetingId','int',False),('PK, FK','UserId','int',False),('','Status','nvarchar(20)',False),('','RespondedAt','datetime',True)],'#fff0ce')
entity('AuditLog','Nhật ký thao tác',60,760,360,[('PK','AuditLogId','int',False),('FK','UserId','int',False),('','Action','nvarchar(100)',False),('','EntityName','nvarchar(100)',False),('','EntityId','int',False),('','CreatedAt','datetime',False),('','Details','nvarchar(max)',True)],'#eee5f4')
entity('EmailQueue','Hàng đợi email',1070,930,400,[('PK','EmailQueueId','int',False),('','RecipientEmail','nvarchar(320)',False),('','Subject','nvarchar(200)',False),('','Body','nvarchar(max)',False),('','CreatedAt','datetime',False),('','ScheduledAt','datetime',True),('','Status','nvarchar(20)',False),('','AttemptCount','int',False),('','SentAt','datetime',True),('','LastError','nvarchar(1000)',True)],'#fff0ce')

def relation(id, source,target,exitx,exity,entryx,entryy,points=(),optional=False):
    style = f'edgeStyle=orthogonalEdgeStyle;rounded=0;html=1;strokeWidth=1.6;strokeColor={"#9a6262" if optional else "#536a80"};startArrow={"ERzeroToOne" if optional else "ERmandOne"};endArrow=ERzeroToMany;startSize=12;endSize=12;exitX={exitx};exitY={exity};exitPerimeter=0;entryX={entryx};entryY={entryy};entryPerimeter=0;'
    e=ET.SubElement(root,'mxCell',id=id,style=style,edge='1',parent='1',source=source,target=target)
    geo=ET.SubElement(e,'mxGeometry',relative='1',attrib={'as':'geometry'})
    if points:
        arr=ET.SubElement(geo,'Array',attrib={'as':'points'})
        for x,y in points: ET.SubElement(arr,'mxPoint',x=str(x),y=str(y))

relation('fk-role','Role','User',1,.5,0,.165,[(470,263),(470,290)])
relation('fk-department','Department','User',1,.5,0,.62,[(430,446),(430,400)])
relation('fk-organizer','User','Meeting',1,.3,0,.25,[(1010,323),(1010,600)])
relation('fk-canceller','User','Meeting',1,.8,0,.68,[(960,444),(960,771)],True)
relation('fk-room','Room','Meeting',.5,1,.5,0)
relation('fk-user-participant','User','MeetingParticipant',.5,1,.5,0)
relation('fk-meeting-participant','Meeting','MeetingParticipant',0,.94,1,.55,[(990,874)])
relation('fk-audit','User','AuditLog',0,.8,1,.42,[(480,444),(480,851)])

text('title','ERD HỆ THỐNG ĐẶT LỊCH HỌP DOANH NGHIỆP',60,24,1400,38,25,True)
text('subtitle','Mô hình dữ liệu theo Models và ApplicationDbContext của MeetingScheduler · 7 thực thể nghiệp vụ và AuditLog',60,68,1120,28,15)
for args in [('label-role','RoleId · 1 — 0..n',410,190,230,24),('label-dept','DepartmentId\n1 — 0..n',410,345,150,40),('label-organizer','CreatedByUserId\nNgười tổ chức · 1 — 0..n',1080,420,340,48),('label-canceller','CancelledByUserId\nNgười hủy · 0..1 — 0..n',800,635,200,48),('label-room','RoomId\n1 — 0..n',1290,370,160,45),('label-user-part','UserId\n1 — 0..n',570,620,150,44),('label-meeting-part','MeetingId\n1 — 0..n',910,910,150,44),('label-audit','UserId\n1 — 0..n',350,630,130,44)]: text(*args)
text('legend','<b>CHÚ GIẢI</b>  PK: khóa chính · FK: khóa ngoại · UK: duy nhất · ?: cho phép NULL<br/>1: đúng một · 0..1: không có hoặc một · 0..n: không có hoặc nhiều bản ghi',60,1280,1410,58,14)
text('rules','<b>QUAN HỆ VÀ RÀNG BUỘC</b><br/>Meeting n–n User qua MeetingParticipant; khóa chính ghép (MeetingId, UserId).<br/>Meeting.EndTime &gt; StartTime; Room.Capacity &gt; 0. User/Room/Department xóa mềm bằng IsDisabled.<br/>Meeting.Status: Confirmed / Cancelled; Room.Status: Active / Maintenance; RSVP: Pending / Accepted / Declined.<br/>AuditLog.EntityId là tham chiếu logic theo EntityName, không phải FK tới một bảng cố định. EmailQueue được lưu độc lập để worker xử lý hàng đợi.',60,1350,1410,130,14)
ET.indent(doc,space='  ')
OUT.write_bytes(ET.tostring(doc,encoding='utf-8',xml_declaration=True))
assert len(root.findall("mxCell[@edge='1']")) == 8
assert len(root.findall("mxCell[@style]")) > 60
print(str(OUT))
print('Validated: 8 entities, including EmailQueue, and 8 foreign-key relationships')
