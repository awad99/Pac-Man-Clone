import os
from PIL import Image

def main():
    img_path = "Content/spritesheet.png"
    img = Image.open(img_path).convert('RGBA')
    
    r, c = 9, 8
    cell = img.crop((c*20, r*20, (c+1)*20, (r+1)*20))
    
    print(f"ASCII representation of Cell Row {r}, Col {c} (20x20):")
    for y in range(20):
        row_str = ""
        for x in range(20):
            p = cell.getpixel((x, y))
            if p[3] == 0:
                row_str += " "
            else:
                # Classify colors
                r_val, g_val, b_val = p[0], p[1], p[2]
                if r_val > 200 and g_val < 50 and b_val < 50:
                    row_str += "R" # Red
                elif g_val > 150 and r_val < 150:
                    row_str += "G" # Green
                elif r_val > 200 and g_val > 200 and b_val < 50:
                    row_str += "Y" # Yellow
                else:
                    row_str += "#"
        print(row_str)

if __name__ == "__main__":
    main()
