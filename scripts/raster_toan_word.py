from pathlib import Path
import fitz

folder = Path(__file__).resolve().parents[1] / 'docs' / 'qa' / 'toan_word'
pdf = fitz.open(folder / 'preview.pdf')
for index, page in enumerate(pdf):
    page.get_pixmap(matrix=fitz.Matrix(1.5, 1.5)).save(folder / f'page-{index + 1}.png')
print(f'{len(pdf)} pages rendered')
