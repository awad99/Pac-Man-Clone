import os
from PIL import Image

def main():
    img_path = "Content/spritesheet.png"
    img = Image.open(img_path).convert('RGBA')
    
    y = 242
    pitch = 20
    print("Inspecting Row 12 (y=242):")
    for col in range(12):
        x = col * pitch + 2
        cell = img.crop((x, y, x + 16, y + 16))
        # Find non-transparent pixels
        non_transparent = 0
        yellows = 0
        for px in cell.getdata():
            if px[3] > 0:
                non_transparent += 1
                # Check if it is yellow/orange-ish
                if px[0] > 200 and px[1] > 150 and px[2] < 100:
                    yellows += 1
        print(f"  Frame {col} (x={x}): Non-Transparent={non_transparent}, Yellow/Orange={yellows}")

if __name__ == "__main__":
    main()
