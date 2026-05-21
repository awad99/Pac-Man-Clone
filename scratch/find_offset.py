import os
from PIL import Image

def main():
    img_path = "Content/spritesheet.png"
    img = Image.open(img_path).convert('RGBA')
    width, height = img.size
    
    # Find first non-transparent pixel in the whole image
    first_x, first_y = None, None
    for y in range(height):
        for x in range(width):
            p = img.getpixel((x, y))
            if p[3] > 0:
                first_x, first_y = x, y
                break
        if first_x is not None:
            break
            
    print(f"First non-transparent pixel is at: x={first_x}, y={first_y}")
    
    # Let's inspect the bounding box of all non-transparent pixels in the first few rows/cols
    # to see if they form 16x16 blocks or something else
    # Let's print the pixel alpha grid of the top-left 32x32 area
    for y in range(32):
        row = ""
        for x in range(32):
            p = img.getpixel((x, y))
            row += "#" if p[3] > 0 else "."
        print(f"y={y:2d}: {row}")

if __name__ == "__main__":
    main()
