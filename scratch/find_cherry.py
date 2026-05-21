import os
from PIL import Image

def main():
    img_path = "Content/spritesheet.png"
    img = Image.open(img_path).convert('RGBA')
    width, height = img.size
    
    # We will search the region x: 160-260, y: 160-260 for a cherry-like object.
    # A cherry usually has red (255, 0, 0) and green/yellow pixels.
    # Let's search in 16x16 grids and 20x20 grids.
    # Let's print pixel color summary of each 16x16 cell in that region.
    print("Searching in 16x16 grid:")
    for r in range(10, 15):
        for c in range(10, 16):
            cell = img.crop((c*16, r*16, (c+1)*16, (r+1)*16))
            pixels = list(cell.getdata())
            reds = sum(1 for p in pixels if p[3] > 0 and p[0] > 200 and p[1] < 50 and p[2] < 50)
            greens = sum(1 for p in pixels if p[3] > 0 and p[1] > 150 and p[0] < 150)
            if reds > 10 and greens > 5:
                print(f"  Found potential Cherry at 16x16 cell: Row {r}, Col {c} (x={c*16}, y={r*16}) with {reds} reds and {greens} greens")
                
    print("\nSearching in 20x20 grid:")
    for r in range(8, 12):
        for c in range(8, 13):
            cell = img.crop((c*20, r*20, (c+1)*20, (r+1)*20))
            pixels = list(cell.getdata())
            reds = sum(1 for p in pixels if p[3] > 0 and p[0] > 200 and p[1] < 50 and p[2] < 50)
            greens = sum(1 for p in pixels if p[3] > 0 and p[1] > 150 and p[0] < 150)
            if reds > 10 and greens > 5:
                print(f"  Found potential Cherry at 20x20 cell: Row {r}, Col {c} (x={c*20}, y={r*20}) with {reds} reds and {greens} greens")

if __name__ == "__main__":
    main()
