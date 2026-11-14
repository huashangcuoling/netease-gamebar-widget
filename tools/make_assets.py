"""Generate the MSIX logo assets for the widget.

Run from the project root:
    python tools/make_assets.py
"""

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parent.parent
ASSETS = ROOT / "Assets"
GAME_BAR = ROOT / "GameBar"


def _gradient(size, top_left, bottom_right):
    image = Image.new("RGBA", (size, size))
    pixels = image.load()
    for y in range(size):
        for x in range(size):
            t = (x + y) / max(1, (size - 1) * 2)
            r = int(top_left[0] + (bottom_right[0] - top_left[0]) * t)
            g = int(top_left[1] + (bottom_right[1] - top_left[1]) * t)
            b = int(top_left[2] + (bottom_right[2] - top_left[2]) * t)
            pixels[x, y] = (r, g, b, 255)
    return image


def _icon(size):
    scale = 4
    s = size * scale
    canvas = Image.new("RGBA", (s, s), (0, 0, 0, 0))

    # Rounded gradient tile.
    tile = _gradient(s, (32, 104, 235), (126, 71, 231))
    mask = Image.new("L", (s, s), 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        (0, 0, s - 1, s - 1), radius=int(s * 0.23), fill=255
    )
    canvas.paste(tile, (0, 0), mask)

    draw = ImageDraw.Draw(canvas)
    border = max(1, int(s * 0.012))
    draw.rounded_rectangle(
        (border, border, s - 1 - border, s - 1 - border),
        radius=int(s * 0.22),
        outline=(255, 255, 255, 46),
        width=border,
    )

    # Three rounded equalizer bars. This stays legible down to 16 px.
    bar_width = s * 0.085
    gap = s * 0.055
    total = bar_width * 3 + gap * 2
    left = (s - total) / 2
    heights = (0.30, 0.46, 0.22)
    baseline = s * 0.68
    for index, height in enumerate(heights):
        x0 = left + index * (bar_width + gap)
        y0 = baseline - s * height
        draw.rounded_rectangle(
            (x0, y0, x0 + bar_width, baseline),
            radius=bar_width / 2,
            fill=(255, 255, 255, 238),
        )

    # Small highlight dot, like a play indicator.
    dot = s * 0.055
    draw.ellipse(
        (s * 0.66, s * 0.27, s * 0.66 + dot, s * 0.27 + dot),
        fill=(255, 255, 255, 210),
    )

    return canvas.resize((size, size), Image.Resampling.LANCZOS)


def _tile(width, height, icon_size):
    canvas = Image.new("RGBA", (width, height), (14, 18, 26, 255))
    draw = ImageDraw.Draw(canvas)
    for y in range(height):
        t = y / max(1, height - 1)
        color = (
            int(18 + 12 * t),
            int(24 + 10 * t),
            int(36 + 16 * t),
            255,
        )
        draw.line((0, y, width, y), fill=color)
    icon = _icon(icon_size)
    canvas.alpha_composite(icon, ((width - icon_size) // 2, (height - icon_size) // 2))
    return canvas


def main():
    ASSETS.mkdir(parents=True, exist_ok=True)
    GAME_BAR.mkdir(parents=True, exist_ok=True)

    outputs = {
        "StoreLogo.png": _icon(50),
        "Square44x44Logo.png": _icon(44),
        "Square150x150Logo.png": _icon(150),
        "SmallTile.png": _icon(71),
        "LargeTile.png": _icon(310),
        "Wide310x150Logo.png": _tile(310, 150, 88),
        "SplashScreen.png": _tile(620, 300, 132),
    }
    for name, image in outputs.items():
        image.save(ASSETS / name)

    _icon(64).save(GAME_BAR / "WidgetIcon.png")


if __name__ == "__main__":
    main()
