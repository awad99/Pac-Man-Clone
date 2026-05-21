import os
from PIL import Image

def main():
    img_path = "Content/spritesheet.png"
    img = Image.open(img_path).convert('RGBA')
    
    for r in range(4):
        print(f"Row {r}:")
        for c in range(5):
            cell = img.crop((c*16, r*16, (c+1)*16, (r+1)*16))
            bbox = cell.getbbox()
            if bbox is None:
                print(f"  Col {c}: EMPTY")
                continue
            # Get yellow pixel count
            pixels = list(cell.getdata())
            yellows = [p for p in pixels if p[3] > 0 and p[0] > 200 and p[1] > 200 and p[2] < 50]
            print(f"  Col {c}: BBox={bbox}, YellowPixels={len(yellows)}")

if __name__ == "__main__":
    main()
