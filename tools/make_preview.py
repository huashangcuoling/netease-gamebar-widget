"""Generate a static design preview. This is not a Game Bar screenshot."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "docs" / "preview.png"


def font(name, size):
    path = Path(r"C:\Windows\Fonts") / name
    try:
        return ImageFont.truetype(str(path), size)
    except OSError:
        return ImageFont.load_default()


def rounded_mask(size, radius):
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        (0, 0, size[0] - 1, size[1] - 1), radius=radius, fill=255
    )
    return mask


def main():
    scale = 2
    width, height = 377 * scale, 65 * scale
    margin = 10 * scale
    canvas = Image.new("RGBA", (width + margin * 2, height + margin * 2), (12, 14, 18, 255))

    # Simulated desktop content behind the acrylic.
    backdrop = Image.new("RGBA", (width, height), (20, 24, 31, 255))
    bd = ImageDraw.Draw(backdrop)
    bd.ellipse((-90, -40, 300, 260), fill=(35, 92, 190, 190))
    bd.ellipse((430, -110, 850, 260), fill=(112, 55, 160, 170))
    bd.ellipse((120, 50, 700, 260), fill=(30, 120, 120, 90))
    backdrop = backdrop.filter(ImageFilter.GaussianBlur(34))

    card = Image.new("RGBA", (width, height), (15, 18, 24, 150))
    card.alpha_composite(backdrop)
    shade = Image.new("RGBA", (width, height), (10, 12, 16, 120))
    card.alpha_composite(shade)

    mask = rounded_mask((width, height), 20)
    canvas.paste(card, (margin, margin), mask)

    draw = ImageDraw.Draw(canvas)

    # Border and top highlight.
    draw.rounded_rectangle(
        (margin, margin, margin + width - 1, margin + height - 1),
        radius=20,
        outline=(255, 255, 255, 40),
        width=2,
    )
    highlight = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    hd = ImageDraw.Draw(highlight)
    for y in range(height // 2):
        hd.line((0, y, width, y), fill=(255, 255, 255, max(0, 20 - y // 3)))
    canvas.alpha_composite(highlight, (margin, margin))

    # Album art.
    art_size = 92
    art_x, art_y = margin + 20, margin + 19
    art = Image.new("RGBA", (art_size, art_size), (0, 0, 0, 0))
    ad = ImageDraw.Draw(art)
    for y in range(art_size):
        t = y / (art_size - 1)
        color = (int(42 + 50 * t), int(92 + 20 * t), int(190 - 30 * t), 255)
        ad.line((0, y, art_size, y), fill=color)
    bar_w, gap = 10, 6
    left = (art_size - (bar_w * 3 + gap * 2)) / 2
    for i, h in enumerate((30, 46, 22)):
        x0 = left + i * (bar_w + gap)
        ad.rounded_rectangle(
            (x0, art_size * 0.64 - h, x0 + bar_w, art_size * 0.64),
            radius=bar_w / 2,
            fill=(255, 255, 255, 235),
        )
    art_mask = rounded_mask((art_size, art_size), 14)
    canvas.paste(art, (art_x, art_y), art_mask)
    draw.rounded_rectangle(
        (art_x, art_y, art_x + art_size, art_y + art_size),
        radius=14,
        outline=(255, 255, 255, 30),
        width=1,
    )

    # Text.
    title_font = font("seguisb.ttf", 29)
    artist_font = font("segoeui.ttf", 21)
    draw.text((art_x + art_size + 20, margin + 17), "God is a woman", font=title_font, fill=(255, 255, 255, 255))
    draw.text((art_x + art_size + 20, margin + 51), "Ariana Grande", font=artist_font, fill=(255, 255, 255, 155))

    # Transport icons.
    icon_color = (255, 255, 255, 220)
    cy = margin + height // 2
    prev_x, play_x, next_x = width + margin - 156, width + margin - 96, width + margin - 34

    draw.line((prev_x, cy - 13, prev_x, cy + 13), fill=icon_color, width=3)
    draw.polygon([(prev_x + 3, cy), (prev_x + 17, cy - 12), (prev_x + 17, cy + 12)], fill=icon_color)

    draw.polygon([(play_x - 10, cy - 15), (play_x - 10, cy + 15), (play_x + 17, cy)], fill=icon_color)

    draw.polygon([(next_x - 3, cy - 12), (next_x - 3, cy + 12), (next_x + 11, cy)], fill=icon_color)
    draw.line((next_x + 14, cy - 13, next_x + 14, cy + 13), fill=icon_color, width=3)

    # Bottom progress.
    line_y = margin + height - 10
    draw.rounded_rectangle(
        (margin + 16, line_y, margin + width - 16, line_y + 2),
        radius=1,
        fill=(255, 255, 255, 32),
    )
    draw.rounded_rectangle(
        (margin + 16, line_y, margin + 16 + int((width - 32) * 0.42), line_y + 2),
        radius=1,
        fill=(255, 255, 255, 190),
    )

    OUT.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(OUT)


if __name__ == "__main__":
    main()
