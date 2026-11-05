from pathlib import Path
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

root = Path(__file__).resolve().parents[1]
folder = root / 'Nop_ca_nhan_Le_Xuan_Toan'
source = folder / '23A1001D0361_Le_Xuan_Toan_DataFlow_Room.md'
output = source.with_suffix('.docx')
doc = Document()
sec = doc.sections[0]
sec.page_width, sec.page_height = Cm(21), Cm(29.7)
sec.top_margin = sec.bottom_margin = Cm(2)
sec.left_margin = sec.right_margin = Cm(2)
for name in ['Normal', 'Title', 'Heading 1', 'Heading 2']:
    st = doc.styles[name]
    st.font.name = 'Times New Roman'
    st.font.color.rgb = RGBColor(0, 0, 0)
    st.font.size = Pt({'Normal': 12, 'Title': 18, 'Heading 1': 14, 'Heading 2': 12}[name])
    st.paragraph_format.space_after = Pt(6)
    st.paragraph_format.line_spacing = 1.1
doc.styles['Normal'].paragraph_format.widow_control = True
for name in ['Heading 1', 'Heading 2']:
    doc.styles[name].font.bold = True
    doc.styles[name].paragraph_format.space_before = Pt(10)
footer = sec.footer.paragraphs[0]
footer.alignment = WD_ALIGN_PARAGRAPH.CENTER
field = OxmlElement('w:fldSimple')
field.set(qn('w:instr'), 'PAGE')
footer._p.append(field)

lines = source.read_text(encoding='utf-8').splitlines()
i = 0
while i < len(lines):
    line = lines[i]
    if not line.strip():
        i += 1
        continue
    if line.startswith('```'):
        i += 1
        while i < len(lines) and not lines[i].startswith('```'):
            p = doc.add_paragraph()
            p.paragraph_format.space_after = Pt(0)
            p.paragraph_format.line_spacing = 1
            r = p.add_run(lines[i])
            r.font.name = 'Consolas'
            r.font.size = Pt(9)
            i += 1
        i += 1
        continue
    if line.startswith('|'):
        rows = []
        while i < len(lines) and lines[i].startswith('|'):
            cells = [c.strip() for c in lines[i].strip().strip('|').split('|')]
            if not all(set(c) <= set(' -:') for c in cells):
                rows.append(cells)
            i += 1
        table = doc.add_table(rows=0, cols=len(rows[0]))
        table.alignment = WD_TABLE_ALIGNMENT.CENTER
        table.autofit = False
        for col, width in zip(table.columns, [5, 12]):
            col.width = Cm(width)
        borders = OxmlElement('w:tblBorders')
        for edge in ['top', 'left', 'bottom', 'right', 'insideH', 'insideV']:
            b = OxmlElement('w:' + edge)
            for key, value in {'val': 'single', 'sz': '4', 'color': 'D9D9D9'}.items():
                b.set(qn('w:' + key), value)
            borders.append(b)
        table._tbl.tblPr.append(borders)
        for index, row in enumerate(rows):
            cells = table.add_row().cells
            trpr = cells[0]._tc.getparent().get_or_add_trPr()
            trpr.append(OxmlElement('w:cantSplit'))
            if index == 0:
                trpr.append(OxmlElement('w:tblHeader'))
            for n, (cell, text) in enumerate(zip(cells, row)):
                cell.width = Cm([5, 12][n])
                cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
                p = cell.paragraphs[0]
                p.paragraph_format.space_before = Pt(4)
                p.paragraph_format.space_after = Pt(4)
                p.paragraph_format.line_spacing = 1
                r = p.add_run(text)
                r.font.size = Pt(10.5)
                if index == 0:
                    r.bold = True
                    shd = OxmlElement('w:shd')
                    shd.set(qn('w:fill'), 'E8EDF2')
                    cell._tc.get_or_add_tcPr().append(shd)
        doc.add_paragraph().paragraph_format.space_after = Pt(0)
        continue
    if line.startswith('# '):
        p = doc.add_paragraph(line[2:], 'Title')
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    elif line.startswith('## '):
        text = line[3:]
        doc.add_paragraph(text, 'Heading 1' if text[0].isdigit() else 'Heading 2')
    else:
        doc.add_paragraph(line)
    i += 1
doc.core_properties.author = 'Le Xuan Toan'
doc.core_properties.title = 'Data Flow module Room'
doc.save(output)
print(output)
