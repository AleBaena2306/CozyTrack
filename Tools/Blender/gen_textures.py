"""Genera las texturas de skins de Cozy Track (patrones geométricos repetibles).

Uso: python gen_textures.py <carpeta_salida>
Todas son 512x512, se repiten sin costura y se dibujan por código (sin imágenes externas).
Se dibujan a 2x y se reducen para suavizar los bordes.
"""
import sys, os, math
from PIL import Image, ImageDraw

S = 1024          # tamaño de trabajo
OUT = 512


def hexc(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def tile_draw(fn, bg):
    """Dibuja en un lienzo 3x3 y recorta el centro para que el patrón se repita sin costura."""
    big = Image.new("RGB", (S * 3, S * 3), hexc(bg))
    d = ImageDraw.Draw(big)
    for ox in range(3):
        for oy in range(3):
            fn(d, ox * S, oy * S)
    return big.crop((S, S, 2 * S, 2 * S)).resize((OUT, OUT), Image.LANCZOS)


def star(cx, cy, ro, ri, n=5, rot=-math.pi / 2):
    return [(cx + (ro if i % 2 == 0 else ri) * math.cos(rot + i * math.pi / n),
             cy + (ro if i % 2 == 0 else ri) * math.sin(rot + i * math.pi / n)) for i in range(2 * n)]


def skins():
    T = {}

    def stripes(d, ox, oy):
        w = S // 8
        for i in range(-2, 10):
            x = ox + i * 2 * w
            d.polygon([(x, oy), (x + w, oy), (x + w + S, oy + S), (x + S, oy + S)], fill=hexc("#FFF4E0"))
    T["Stripes_Coral"] = tile_draw(stripes, "#FF7366")

    def dots(d, ox, oy):
        n = 4; c = S / n
        for i in range(n):
            for j in range(n):
                cx = ox + (i + 0.5) * c + (c / 2 if j % 2 else 0); cy = oy + (j + 0.5) * c
                r = c * 0.22
                d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=hexc("#FFFFFF"))
    T["Dots_Sky"] = tile_draw(dots, "#73BFFF")

    def checker(d, ox, oy):
        n = 4; c = S / n
        for i in range(n):
            for j in range(n):
                if (i + j) % 2 == 0:
                    d.rectangle([ox + i * c, oy + j * c, ox + (i + 1) * c, oy + (j + 1) * c], fill=hexc("#2B2B33"))
    T["Checker_Race"] = tile_draw(checker, "#FAFAFA")

    def zigzag(d, ox, oy):
        n = 4; h = S / n; w = S / 4
        for j in range(n):
            y = oy + j * h
            pts = []
            for i in range(5):
                pts.append((ox + i * w, y + (0 if i % 2 == 0 else h * 0.5)))
            for i in reversed(range(5)):
                pts.append((ox + i * w, y + h * 0.28 + (0 if i % 2 == 0 else h * 0.5)))
            d.polygon(pts, fill=hexc("#FFD64D"))
    T["Zigzag_Grape"] = tile_draw(zigzag, "#734DBF")

    def stars(d, ox, oy):
        n = 4; c = S / n
        for i in range(n):
            for j in range(n):
                cx = ox + (i + 0.5) * c + (c / 2 if j % 2 else 0); cy = oy + (j + 0.5) * c
                d.polygon(star(cx, cy, c * 0.28, c * 0.12), fill=hexc("#FFD64D"))
    T["Stars_Night"] = tile_draw(stars, "#1F2040")

    def hexes(d, ox, oy):
        r = S / 6
        hw = r * math.sqrt(3)
        for j in range(-1, 5):
            for i in range(-1, 5):
                cx = ox + i * hw + (hw / 2 if j % 2 else 0); cy = oy + j * r * 1.5
                pts = [(cx + r * 0.86 * math.cos(math.pi / 6 + k * math.pi / 3), cy + r * 0.86 * math.sin(math.pi / 6 + k * math.pi / 3)) for k in range(6)]
                d.polygon(pts, fill=hexc("#8CE6A8") if (i + j) % 3 else hexc("#FFFFFF"))
    # el panal no encaja exacto en 1024; se usa un periodo compatible
    T["Hex_Mint"] = tile_draw(lambda d, ox, oy: honeycomb(d, ox, oy), "#4FBF85")

    def waves(d, ox, oy):
        n = 4; h = S / n
        for j in range(n):
            pts = []
            for k in range(65):
                x = ox + k * S / 64
                pts.append((x, oy + j * h + h * 0.25 * math.sin(2 * math.pi * 2 * k / 64)))
            for k in reversed(range(65)):
                x = ox + k * S / 64
                pts.append((x, oy + j * h + h * 0.45 + h * 0.25 * math.sin(2 * math.pi * 2 * k / 64)))
            d.polygon(pts, fill=hexc("#B3E6FF"))
    T["Waves_Ocean"] = tile_draw(waves, "#4080F2")

    def plaid(d, ox, oy):
        for k in range(4):
            a = k * S / 4
            d.rectangle([ox + a, oy, ox + a + S / 10, oy + S], fill=hexc("#E6526B"))
            d.rectangle([ox, oy + a, ox + S, oy + a + S / 10], fill=hexc("#E6526B"))
            d.rectangle([ox + a + S / 7, oy, ox + a + S / 7 + S / 50, oy + S], fill=hexc("#FFF4E0"))
            d.rectangle([ox, oy + a + S / 7, ox + S, oy + a + S / 7 + S / 50], fill=hexc("#FFF4E0"))
    T["Plaid_Cherry"] = tile_draw(plaid, "#FF8C99")

    def confetti(d, ox, oy):
        import random
        random.seed(7)
        cols = ["#FF7366", "#FFD64D", "#73BFFF", "#8CE6A8", "#B38CF2"]
        for k in range(70):
            x = ox + random.random() * S; y = oy + random.random() * S
            a = random.random() * math.pi; L = S * 0.05; w = S * 0.018
            c = random.choice(cols)
            dx, dy = math.cos(a) * L, math.sin(a) * L
            nx, ny = -math.sin(a) * w, math.cos(a) * w
            d.polygon([(x - dx - nx, y - dy - ny), (x + dx - nx, y + dy - ny), (x + dx + nx, y + dy + ny), (x - dx + nx, y - dy + ny)], fill=hexc(c))
    T["Confetti_Cream"] = tile_draw(confetti, "#FFF4E0")

    def triangles(d, ox, oy):
        n = 4; c = S / n
        for i in range(n):
            for j in range(n):
                x, y = ox + i * c, oy + j * c
                col = hexc("#FFB04D") if (i + j) % 2 else hexc("#FF7366")
                d.polygon([(x, y + c), (x + c / 2, y), (x + c, y + c)], fill=col)
    T["Triangles_Sunset"] = tile_draw(triangles, "#FFD64D")

    def lightning(d, ox, oy):
        n = 3; c = S / n
        for i in range(n):
            for j in range(n):
                x, y = ox + i * c + c * 0.25, oy + j * c + c * 0.1
                pts = [(x + c * 0.25, y), (x + c * 0.05, y + c * 0.45), (x + c * 0.22, y + c * 0.45),
                       (x + c * 0.08, y + c * 0.8), (x + c * 0.45, y + c * 0.33), (x + c * 0.27, y + c * 0.33), (x + c * 0.42, y)]
                d.polygon(pts, fill=hexc("#FFE14D"))
    T["Bolt_Electric"] = tile_draw(lightning, "#3D3DA6")

    def hearts(d, ox, oy):
        n = 4; c = S / n
        for i in range(n):
            for j in range(n):
                cx = ox + (i + 0.5) * c + (c / 2 if j % 2 else 0); cy = oy + (j + 0.5) * c
                r = c * 0.13
                d.ellipse([cx - 2 * r, cy - 1.6 * r, cx, cy + 0.4 * r], fill=hexc("#FF4D7A"))
                d.ellipse([cx, cy - 1.6 * r, cx + 2 * r, cy + 0.4 * r], fill=hexc("#FF4D7A"))
                d.polygon([(cx - 1.95 * r, cy - 0.3 * r), (cx + 1.95 * r, cy - 0.3 * r), (cx, cy + 1.9 * r)], fill=hexc("#FF4D7A"))
    T["Hearts_Bubblegum"] = tile_draw(hearts, "#FFB3D1")
    return T


def honeycomb(d, ox, oy):
    # periodo horizontal = S/4 ancho de celda; vertical ajustado a 4 filas
    cw = S / 4.0
    rh = S / 4.0
    for j in range(-1, 6):
        for i in range(-1, 6):
            cx = ox + i * cw + (cw / 2 if j % 2 else 0)
            cy = oy + j * rh
            rx, ry = cw * 0.45, rh * 0.62
            pts = [(cx + rx * math.cos(math.pi / 6 + k * math.pi / 3) / math.cos(math.pi / 6),
                    cy + ry * math.sin(math.pi / 6 + k * math.pi / 3)) for k in range(6)]
            pts = [(cx + (px - cx) * 0.95, cy + (py - cy) * 0.95) for px, py in pts]
            d.polygon(pts, fill=hexc("#FFFFFF") if (i % 2 == 0 and j % 2 == 0) else hexc("#8CE6A8"))


def main(out):
    folder = os.path.join(out, "Textures", "Skins")
    os.makedirs(folder, exist_ok=True)
    T = skins()
    for name, img in T.items():
        img.save(os.path.join(folder, f"Skin_{name}.png"), optimize=True)
    # hoja de contacto
    names = list(T)
    cols = 6
    sheet = Image.new("RGB", (cols * 260 + 20, ((len(names) + cols - 1) // cols) * 260 + 20), (235, 232, 226))
    for k, n in enumerate(names):
        tile = T[n].resize((240, 240))
        sheet.paste(tile, (20 + (k % cols) * 260, 20 + (k // cols) * 260))
    os.makedirs(os.path.join(out, "Previews"), exist_ok=True)
    sheet.save(os.path.join(out, "Previews", "skins.png"))
    print(len(names), "texturas")


if __name__ == "__main__":
    main(sys.argv[1])
