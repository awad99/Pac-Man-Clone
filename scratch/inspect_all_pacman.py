import os
from PIL import Image

def main():
    img_path = "Content/spritesheet.png"
    if not os.path.exists(img_path):
        print(f"Error: {img_path} not found.")
        return
        
    img = Image.open(img_path).convert('RGBA')
    width, height = img.size
    print(f"Spritesheet size: {width}x{height}")
    
    # Grid search (assuming 20x20 or 16x16 layout)
    # Let's search every 20 pixels first, since we know from code:
    # Row 0 = Y:2
    # Row 1 = Y:22
    # Row 2 = Y:42
    # Row 3 = Y:62
    # Row 4 = Y:82 (ghosts)
    # Pitch is 20 pixels.
    # Let's check Y from 0 to height in steps of 20, and X in steps of 20.
    
    pitch = 20
    for r in range(height // pitch):
        y = r * pitch + 2
        if y + 16 > height:
            break
        row_has_yellow = False
        row_yellow_details = []
        for c in range(width // pitch):
            x = c * pitch + 2
            if x + 16 > width:
                break
                
            cell = img.crop((x, y, x + 16, y + 16))
            pixels = list(cell.getdata())
            # Detect yellow (R > 200, G > 200, B < 100, A > 0)
            yellow_pixels = [p for p in pixels if p[3] > 0 and p[0] > 200 and p[1] > 200 and p[2] < 100]
            if len(yellow_pixels) > 5:
                row_has_yellow = True
                row_yellow_details.append((c, len(yellow_pixels)))
        if row_has_yellow:
            print(f"Row {r} (y={y}): yellow cells found in columns: {row_yellow_details}")

if __name__ == "__main__":
    main()
