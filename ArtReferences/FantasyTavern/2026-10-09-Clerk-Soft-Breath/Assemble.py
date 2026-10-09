from pathlib import Path
from PIL import Image
import shutil, json

root=Path(__file__).parent
source=Path(r'C:\Users\Chang\.codex\generated_images\01a1021f-68a4-7fd1-86af-860c38e5bff3\exec-b5b8e2ae-a544-43c7-9a40-fa2dea328556.png')
shutil.copy2(source,root/'Generated-Sheet.png')
sheet=Image.open(source).convert('RGBA')
cw,ch=sheet.width//4,sheet.height//4
cells=[]
for i in range(16):
    cell=sheet.crop(((i%4)*cw,(i//4)*ch,(i%4+1)*cw,(i//4+1)*ch))
    cell.putalpha(cell.getchannel('A').point(lambda a:255 if a>=128 else 0))
    cells.append(cell)
boxes=[cell.getbbox() for cell in cells]
scale=min(216/max(b[3]-b[1] for b in boxes),200/max(b[2]-b[0] for b in boxes))
frames=[]
out=root/'Frames'
out.mkdir(exist_ok=True)
for i,(cell,b) in enumerate(zip(cells,boxes)):
    sprite=cell.crop(b)
    sprite=sprite.resize((round(sprite.width*scale),round(sprite.height*scale)),Image.Resampling.NEAREST)
    frame=Image.new('RGBA',(256,256))
    # Fixed sole baseline and common scale; no added lateral tremble.
    x=(256-sprite.width)//2
    frame.alpha_composite(sprite,(x,240-sprite.height))
    frame.save(out/f'Clerk-Soft-Breath-{i:02d}.png')
    frames.append(frame)
durations=[100]*16
frames[0].save(root/'Clerk-Soft-Breath.png',save_all=True,append_images=frames[1:],duration=durations,loop=0,disposal=0,blend=0)
preview=[]
for frame in frames:
    bg=Image.new('RGBA',(256,256),(22,24,30,255))
    bg.alpha_composite(frame)
    preview.append(bg.convert('RGB').resize((512,512),Image.Resampling.NEAREST))
preview[0].save(root/'Clerk-Soft-Breath-Preview.gif',save_all=True,append_images=preview[1:],duration=durations,loop=0,optimize=False,disposal=2)
export=Image.new('RGBA',(1024,1024))
for i,frame in enumerate(frames): export.alpha_composite(frame,((i%4)*256,(i//4)*256))
export.save(root/'Clerk-Soft-Breath-Sheet.png')
(root/'Animation.json').write_text(json.dumps({'frames':16,'frameSize':[256,256],'grid':[4,4],'durationsMs':durations,'loop':True,'footBaselineY':240,'unityPivot':[0.5,0.0625],'filter':'Point','mipmaps':False,'note':'Slow shallow breath, brief shiver only around frames 11-12. No added continuous lateral jitter.'},indent=2),encoding='utf-8')
for name in ['Clerk-Soft-Breath.png','Clerk-Soft-Breath-Preview.gif']:
    im=Image.open(root/name)
    assert im.n_frames==16,(name,im.n_frames)
    print(name,im.size,'frames=',im.n_frames)
for f in frames:
    assert f.size==(256,256) and f.getpixel((0,0))[3]==0
print('Verified sixteen 256x256 RGBA frames, transparent corners, duration',sum(durations),'ms')



