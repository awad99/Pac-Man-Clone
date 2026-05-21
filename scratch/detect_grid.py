import os
from PIL import Image

def main():
    img_path = "Content/spritesheet.png"
    img = Image.open(img_path).convert('RGBA')
    width, height = img.size
    
    # We want to find the horizontal and vertical intervals of sprites.
    # Let's project non-transparent pixels onto x and y axes.
    x_proj = [0] * width
    y_proj = [0] * height
    
    for y in range(height):
        for x in range(width):
            p = img.getpixel((x, y))
            if p[3] > 0:
                x_proj[x] += 1
                y_proj[y] += 1
                
    # Print non-zero projection runs
    print("Vertical (Y) spans of non-transparent pixels:")
    in_run = False
    start = 0
    for y in range(height):
        if y_proj[y] > 0 and not in_run:
            in_run = True
            start = y
        elif y_proj[y] == 0 and in_run:
            in_run = False
            print(f"  y={start} to {y-1} (height {y - start})")
    if in_run:
        print(f"  y={start} to {height-1} (height {height - start})")
        
    print("\nHorizontal (X) spans of non-transparent pixels in the first 250px:")
    # We only look at the first 250 pixels horizontally, where the characters are
    # to avoid interference from the maze on the right.
    x_proj_chars = [0] * 250
    for y in range(height):
        for x in range(250):
            p = img.getpixel((x, y))
            if p[3] > 0:
                x_proj_chars[x] += 1
                
    in_run = False
    start = 0
    for x in range(250):
        if x_proj_chars[x] > 0 and not in_run:
            in_run = True
            start = x
        elif x_proj_chars[x] == 0 and in_run:
            in_run = False
            print(f"  x={start} to {x-1} (width {x - start})")
    if in_run:
        print(f"  x={start} to 249 (width {250 - start})")

if __name__ == "__main__":
    main()
