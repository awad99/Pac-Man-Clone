import os
from PIL import Image

def main():
    img_path = "Content/spritesheet.png"
    img = Image.open(img_path).convert('RGBA')
    
    r = 5
    for c in range(10):
        cell = img.crop((c*16, r*16, (c+1)*16, (r+1)*16))
        whites = []
        pupils = []
        for y in range(16):
            for x in range(16):
                p = cell.getpixel((x, y))
                # White eyeball: (250, 250, 250) or close to white
                if p[0] > 240 and p[1] > 240 and p[2] > 240 and p[3] > 0:
                    whites.append((x, y))
                # Blue pupil: (0, 51, 255)
                elif p[0] < 50 and p[1] < 100 and p[2] > 200 and p[3] > 0:
                    pupils.append((x, y))
        
        if whites and pupils:
            avg_white_x = sum(w[0] for w in whites) / len(whites)
            avg_white_y = sum(w[1] for w in whites) / len(whites)
            avg_pupil_x = sum(p[0] for p in pupils) / len(pupils)
            avg_pupil_y = sum(p[1] for p in pupils) / len(pupils)
            
            dx = avg_pupil_x - avg_white_x
            dy = avg_pupil_y - avg_white_y
            
            # Determine direction
            if abs(dx) > abs(dy):
                dir_str = "Right" if dx > 0 else "Left"
            else:
                dir_str = "Down" if dy > 0 else "Up"
                
            print(f"Col {c}: avg_white=({avg_white_x:.1f},{avg_white_y:.1f}) avg_pupil=({avg_pupil_x:.1f},{avg_pupil_y:.1f}) -> {dir_str} (dx={dx:.1f}, dy={dy:.1f})")
        else:
            print(f"Col {c}: Whites={len(whites)}, Pupils={len(pupils)} -> No clear eyes")

if __name__ == "__main__":
    main()
