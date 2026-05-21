import os
from PIL import Image

def main():
    img_path = "Content/spritesheet.png"
    img = Image.open(img_path).convert('RGBA')
    width, height = img.size
    
    # Let's crop cells using a 20x20 grid starting at (0, 0)
    # We will print the bounding box of each cell in the first few rows/cols
    for r in range(6):
        print(f"Row {r}:")
        for c in range(8):
            cell = img.crop((c*20, r*20, (c+1)*20, (r+1)*20))
            bbox = cell.getbbox()
            if bbox is None:
                print(f"  Col {c}: EMPTY")
            else:
                # Find dominant color
                pixels = list(cell.getdata())
                opaque = [p for p in pixels if p[3] > 0]
                counts = {}
                for p in opaque:
                    color = p[:3]
                    counts[color] = counts.get(color, 0) + 1
                best_rgb = max(counts, key=counts.get) if counts else None
                print(f"  Col {c}: BBox={bbox}, DominantColor={best_rgb}, Count={len(opaque)}")

if __name__ == "__main__":
    main()
