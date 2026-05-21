import os
from PIL import Image

def main():
    img_path = "Content/spritesheet.png"
    img = Image.open(img_path).convert('RGBA')
    
    # Try crop at (171, 182) with size 16x16
    x_start, y_start = 171, 182
    cell = img.crop((x_start, y_start, x_start + 16, y_start + 16))
    
    bbox = cell.getbbox()
    print(f"Cherry BBox: {bbox}")
    
    # Print ASCII
    for y in range(16):
        row_str = ""
        for x in range(16):
            p = cell.getpixel((x, y))
            if p[3] == 0:
                row_str += " "
            else:
                r_val, g_val = p[0], p[1]
                if r_val > 200 and g_val < 50:
                    row_str += "R"
                elif g_val > 150 and r_val < 150:
                    row_str += "G"
                else:
                    row_str += "#"
        print(row_str)

if __name__ == "__main__":
    main()
