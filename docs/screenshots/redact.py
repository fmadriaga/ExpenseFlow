"""
redact.py — procesa las 4 capturas de ExpenseFlow para el README.

Uso:
    1. Guardá las 4 imágenes del teléfono en esta misma carpeta con estos nombres:
         01_capture.png   → pantalla principal (Fotografiar ticket)
         02_crop.png      → canvas de recorte con el ticket de Pigalle
         03_history.png   → historial de tickets
         04_detail.png    → pantalla de detalle

    2. Desde esta carpeta:
         pip install Pillow
         python redact.py

    Las imágenes procesadas se guardan como 01_capture_ok.png, etc.
"""

from PIL import Image, ImageDraw

# Altura del status bar del Samsung S23 a recortar (px en la resolución original 1080×2340)
STATUS_BAR_H = 95


def crop_status_bar(img: Image.Image) -> Image.Image:
    """Elimina el status bar superior."""
    w, h = img.size
    return img.crop((0, STATUS_BAR_H, w, h))


def redact_region(img: Image.Image, x0: int, y0: int, x1: int, y1: int,
                  color: tuple = (30, 30, 30)) -> Image.Image:
    """Pinta un rectángulo sólido sobre la región indicada."""
    out = img.copy()
    draw = ImageDraw.Draw(out)
    draw.rectangle([x0, y0, x1, y1], fill=color)
    return out


def process(src: str, dst: str, redactions: list[tuple] | None = None):
    img = Image.open(src)
    img = crop_status_bar(img)
    if redactions:
        for (x0, y0, x1, y1) in redactions:
            img = redact_region(img, x0, y0, x1, y1)
    img.save(dst, optimize=True)
    print(f"  {src} → {dst}  ({img.size[0]}×{img.size[1]})")


# ─────────────────────────────────────────────────────────────────────────────
# Coordenadas de datos sensibles en 02_crop.png (resolución original 1080×2340,
# después de recortar STATUS_BAR_H px del top).
#
# El bloque "Cliente / CI / nombre / dirección" está dentro del recibo impreso,
# aproximadamente en y=750..950 del viewport post-recorte, ancho completo del
# recibo (x=85..995).  Ajustá si tu captura tiene encuadre diferente.
# ─────────────────────────────────────────────────────────────────────────────
CROP_REDACTIONS = [
    (85, 750, 995, 960),   # CI, nombre, dirección, ciudad
]

files = [
    ("01_capture.png",  "01_capture_ok.png",  None),
    ("02_crop.png",     "02_crop_ok.png",     CROP_REDACTIONS),
    ("03_history.png",  "03_history_ok.png",  None),
    ("04_detail.png",   "04_detail_ok.png",   None),
]

print("Procesando capturas…")
for src, dst, redactions in files:
    try:
        process(src, dst, redactions)
    except FileNotFoundError:
        print(f"  [SKIP] {src} no encontrado — renombrá el archivo y volvé a correr.")
print("Listo.")
