import os
from PIL import Image

def main():
    img_path = "Content/spritesheet.png"
    img = Image.open(img_path).convert('RGBA')
    
    # Check rows 4 to 12
    for r in range(4, 13):
        print(f"Row {r}:")
        # Check first 12 columns
        for c in range(12):
            cell = img.crop((c*20, r*20, (c+1)*20, (r+1)*20))
            bbox = cell.getbbox()
            if bbox is None:
                continue
            
            # Find dominant color
            pixels = list(cell.getdata())
            opaque = [p for p in pixels if p[3] > 0]
            if not opaque:
                continue
                
            counts = {}
            for p in opaque:
                color = p[:3]
                counts[color] = counts.get(color, 0) + 1
            best_rgb = max(counts, key=counts.get)
            
            print(f"  Col {c:2d}: BBox={bbox}, DominantColor={best_rgb}, Count={len(opaque)}")

if __name__ == "__main__":
    main()
