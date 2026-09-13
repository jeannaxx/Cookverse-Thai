from __future__ import annotations

import html
import re
from datetime import date
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    BaseDocTemplate,
    Frame,
    HRFlowable,
    KeepTogether,
    PageBreak,
    PageTemplate,
    Paragraph,
    Spacer,
    Table,
    TableStyle,
)


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "PROJECT_CONTEXT.md"
OUTPUT = ROOT / "output" / "pdf" / "SiamCulinaryXR_Project_Context.pdf"

PAGE_W, PAGE_H = A4
MARGIN_X = 20 * mm
MARGIN_TOP = 22 * mm
MARGIN_BOTTOM = 18 * mm

GREEN = colors.HexColor("#315B4A")
GREEN_DARK = colors.HexColor("#203F34")
GREEN_LIGHT = colors.HexColor("#E8F0EC")
CREAM = colors.HexColor("#F8F5ED")
GOLD = colors.HexColor("#C89D4A")
TEXT = colors.HexColor("#26332E")
MUTED = colors.HexColor("#64736C")
WHITE = colors.white


pdfmetrics.registerFont(TTFont("ThaiRegular", "/System/Library/Fonts/Supplemental/Tahoma.ttf"))
pdfmetrics.registerFont(TTFont("ThaiBold", "/System/Library/Fonts/Supplemental/Tahoma Bold.ttf"))


class ProjectDocTemplate(BaseDocTemplate):
    def __init__(self, filename: str):
        super().__init__(
            filename,
            pagesize=A4,
            leftMargin=MARGIN_X,
            rightMargin=MARGIN_X,
            topMargin=MARGIN_TOP,
            bottomMargin=MARGIN_BOTTOM,
            title="SiamCulinaryXR - Current Project Context",
            author="SiamCulinaryXR Project Team",
            subject="ขอบเขต แผนงาน การแบ่งงาน และแนวทางใช้ AI ของโครงการ",
        )
        frame = Frame(
            self.leftMargin,
            self.bottomMargin,
            self.width,
            self.height,
            id="content",
            leftPadding=0,
            rightPadding=0,
            topPadding=0,
            bottomPadding=0,
        )
        self.addPageTemplates(
            [
                PageTemplate(id="cover", frames=[frame], onPage=draw_cover_page),
                PageTemplate(id="body", frames=[frame], onPage=draw_body_page),
            ]
        )

    def afterPage(self):
        if self.page == 1:
            self.handle_nextPageTemplate("body")


def draw_cover_page(canvas, doc):
    canvas.saveState()
    canvas.setFillColor(CREAM)
    canvas.rect(0, 0, PAGE_W, PAGE_H, fill=1, stroke=0)
    canvas.setFillColor(GREEN)
    canvas.rect(0, PAGE_H - 12 * mm, PAGE_W, 12 * mm, fill=1, stroke=0)
    canvas.setFillColor(GOLD)
    canvas.rect(0, 0, PAGE_W, 5 * mm, fill=1, stroke=0)
    canvas.restoreState()


def draw_body_page(canvas, doc):
    canvas.saveState()
    canvas.setFillColor(CREAM)
    canvas.rect(0, 0, PAGE_W, PAGE_H, fill=1, stroke=0)

    canvas.setFont("ThaiBold", 8.5)
    canvas.setFillColor(GREEN_DARK)
    canvas.drawString(MARGIN_X, PAGE_H - 12 * mm, "SiamCulinaryXR")
    canvas.setFont("ThaiRegular", 8.5)
    canvas.setFillColor(MUTED)
    canvas.drawRightString(PAGE_W - MARGIN_X, PAGE_H - 12 * mm, "Project Context")
    canvas.setStrokeColor(colors.HexColor("#CED9D3"))
    canvas.setLineWidth(0.6)
    canvas.line(MARGIN_X, PAGE_H - 15 * mm, PAGE_W - MARGIN_X, PAGE_H - 15 * mm)

    canvas.setFont("ThaiRegular", 8)
    canvas.setFillColor(MUTED)
    canvas.drawString(MARGIN_X, 10 * mm, "ข้อมูลอ้างอิงหลักของโครงการ")
    canvas.drawRightString(PAGE_W - MARGIN_X, 10 * mm, f"หน้า {doc.page}")
    canvas.restoreState()


def clean_inline(text: str) -> str:
    text = text.replace("—", "-").replace("–", "-").replace("‑", "-")
    text = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", text)
    text = re.sub(r"`(.+?)`", r"<font color='#315B4A'>\1</font>", text)
    return text


styles = getSampleStyleSheet()
styles.add(
    ParagraphStyle(
        name="CoverTitleThai",
        fontName="ThaiBold",
        fontSize=28,
        leading=34,
        textColor=GREEN_DARK,
        alignment=TA_CENTER,
        wordWrap="CJK",
        spaceAfter=5 * mm,
    )
)
styles.add(
    ParagraphStyle(
        name="CoverSubThai",
        fontName="ThaiRegular",
        fontSize=14,
        leading=22,
        textColor=GREEN,
        alignment=TA_CENTER,
        wordWrap="CJK",
    )
)
styles.add(
    ParagraphStyle(
        name="CoverMetaThai",
        fontName="ThaiRegular",
        fontSize=10,
        leading=16,
        textColor=MUTED,
        alignment=TA_CENTER,
        wordWrap="CJK",
    )
)
styles.add(
    ParagraphStyle(
        name="H1Thai",
        fontName="ThaiBold",
        fontSize=17,
        leading=23,
        textColor=GREEN_DARK,
        spaceBefore=5 * mm,
        spaceAfter=2.5 * mm,
        keepWithNext=True,
        wordWrap="CJK",
    )
)
styles.add(
    ParagraphStyle(
        name="H2Thai",
        fontName="ThaiBold",
        fontSize=12.5,
        leading=18,
        textColor=GREEN,
        spaceBefore=3.5 * mm,
        spaceAfter=1.5 * mm,
        keepWithNext=True,
        wordWrap="CJK",
    )
)
styles.add(
    ParagraphStyle(
        name="BodyThai",
        fontName="ThaiRegular",
        fontSize=10.2,
        leading=16.5,
        textColor=TEXT,
        spaceAfter=2.2 * mm,
        alignment=TA_LEFT,
        wordWrap="CJK",
    )
)
styles.add(
    ParagraphStyle(
        name="BulletThai",
        parent=styles["BodyThai"],
        leftIndent=5 * mm,
        firstLineIndent=-3.5 * mm,
        bulletIndent=1.5 * mm,
        spaceAfter=1.2 * mm,
    )
)
styles.add(
    ParagraphStyle(
        name="CodeThai",
        fontName="ThaiRegular",
        fontSize=9.4,
        leading=15,
        textColor=GREEN_DARK,
        leftIndent=3 * mm,
        rightIndent=3 * mm,
        spaceBefore=1.5 * mm,
        spaceAfter=1.5 * mm,
        wordWrap="CJK",
    )
)


def heading_block(text: str, level: int):
    if level == 2:
        return KeepTogether(
            [
                Spacer(1, 1 * mm),
                Table(
                    [[Paragraph(clean_inline(text), styles["H1Thai"]) ]],
                    colWidths=[PAGE_W - 2 * MARGIN_X],
                    style=TableStyle(
                        [
                            ("BACKGROUND", (0, 0), (-1, -1), GREEN_LIGHT),
                            ("BOX", (0, 0), (-1, -1), 0.5, colors.HexColor("#C8D8D0")),
                            ("LEFTPADDING", (0, 0), (-1, -1), 4 * mm),
                            ("RIGHTPADDING", (0, 0), (-1, -1), 4 * mm),
                            ("TOPPADDING", (0, 0), (-1, -1), 1 * mm),
                            ("BOTTOMPADDING", (0, 0), (-1, -1), 1 * mm),
                        ]
                    ),
                ),
            ]
        )
    return Paragraph(clean_inline(text), styles["H2Thai"])


def code_block(lines: list[str]):
    content = "<br/>".join(html.escape(line).replace(" ", "&nbsp;") for line in lines)
    box = Table(
        [[Paragraph(content, styles["CodeThai"]) ]],
        colWidths=[PAGE_W - 2 * MARGIN_X],
        style=TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#EDF3F0")),
                ("BOX", (0, 0), (-1, -1), 0.6, colors.HexColor("#BDD0C6")),
                ("LEFTPADDING", (0, 0), (-1, -1), 3 * mm),
                ("RIGHTPADDING", (0, 0), (-1, -1), 3 * mm),
                ("TOPPADDING", (0, 0), (-1, -1), 2 * mm),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 2 * mm),
            ]
        ),
    )
    return box


def markdown_to_story(markdown: str):
    lines = markdown.splitlines()
    story = []
    paragraph_lines: list[str] = []
    code_lines: list[str] = []
    in_code = False

    def flush_paragraph():
        nonlocal paragraph_lines
        if paragraph_lines:
            joined = " ".join(line.strip() for line in paragraph_lines)
            story.append(Paragraph(clean_inline(joined), styles["BodyThai"]))
            paragraph_lines = []

    for line in lines[1:]:
        if line.strip().startswith("```"):
            flush_paragraph()
            if in_code:
                story.append(code_block(code_lines))
                story.append(Spacer(1, 1.5 * mm))
                code_lines = []
                in_code = False
            else:
                in_code = True
            continue
        if in_code:
            code_lines.append(line.replace("—", "-").replace("–", "-"))
            continue
        if not line.strip():
            flush_paragraph()
            continue
        if line.startswith("## "):
            flush_paragraph()
            story.append(heading_block(line[3:].strip(), 2))
            continue
        if line.startswith("### "):
            flush_paragraph()
            story.append(heading_block(line[4:].strip(), 3))
            continue
        if re.match(r"^- ", line):
            flush_paragraph()
            item = clean_inline(line[2:].strip())
            story.append(Paragraph(item, styles["BulletThai"], bulletText="•"))
            continue
        numbered = re.match(r"^(\d+)\.\s+(.+)$", line)
        if numbered:
            flush_paragraph()
            story.append(
                Paragraph(
                    clean_inline(numbered.group(2)),
                    styles["BulletThai"],
                    bulletText=f"{numbered.group(1)}.",
                )
            )
            continue
        paragraph_lines.append(line)

    flush_paragraph()
    if code_lines:
        story.append(code_block(code_lines))
    return story


def build():
    markdown = SOURCE.read_text(encoding="utf-8")
    doc = ProjectDocTemplate(str(OUTPUT))

    story = [
        Spacer(1, 48 * mm),
        Paragraph("SiamCulinaryXR", styles["CoverTitleThai"]),
        HRFlowable(
            width="46%",
            thickness=2,
            color=GOLD,
            spaceBefore=3 * mm,
            spaceAfter=6 * mm,
            hAlign="CENTER",
        ),
        Paragraph("ข้อมูลโครงการและแผนการพัฒนาล่าสุด", styles["CoverSubThai"]),
        Spacer(1, 8 * mm),
        Paragraph(
            "Unity XR/VR · WebXR · Gameplay Systems · AI-assisted Assessment",
            styles["CoverMetaThai"],
        ),
        Spacer(1, 62 * mm),
        Paragraph("เอกสารอ้างอิงหลักของทีม", styles["CoverMetaThai"]),
        Paragraph("อัปเดต 13 กันยายน 2026", styles["CoverMetaThai"]),
        PageBreak(),
    ]
    story.extend(markdown_to_story(markdown))
    doc.build(story)


if __name__ == "__main__":
    build()
