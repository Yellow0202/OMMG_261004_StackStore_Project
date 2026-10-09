from pathlib import Path
from PIL import Image
import json
root=Path(__file__).parent
frames=[Image.open(root/'Frames'/f'Clerk-Clean-{i:02d}.png').convert('RGBA') for i in range(16)]
assert all(f.size==(352,352) for f in frames)
colors=set()
for f in frames:
    colors.update(f.getdata())
    assert set(f.getchannel('A').getdata())=={0,255}
    assert f.getpixel((0,0))[3]==0
assert len(colors)<=6,len(colors)
frames[0].save(root/'Clerk-Head-Lines.png',save_all=True,append_images=frames[1:],duration=100,loop=0,disposal=0,blend=0)
preview=[]
for f in frames:
    bg=Image.new('RGBA',f.size,(22,24,30,255));bg.alpha_composite(f)
    preview.append(bg.convert('RGB'))
preview[0].save(root/'Clerk-Head-Lines-Preview.gif',save_all=True,append_images=preview[1:],duration=100,loop=0,optimize=False,disposal=2)
sheet=Image.new('RGBA',(1408,1408))
for i,f in enumerate(frames): sheet.alpha_composite(f,((i%4)*352,(i//4)*352))
sheet.save(root/'Clerk-Head-Lines-Sheet.png')
frames[0].resize((704,704),Image.Resampling.NEAREST).save(root/'Frame-00-Preview-2x.png')
(root/'Animation.json').write_text(json.dumps({'frames':16,'frameSize':[352,352],'grid':[4,4],'durationMs':100,'loopDurationMs':1600,'loop':True,'footBaselineY':336,'unityPivot':[0.5,16/352],'filter':'Point','mipmaps':False,'nativePixelsPreserved':True,'paletteColors':5,'alpha':'binary'},indent=2),encoding='utf-8')
for n in ['Clerk-Head-Lines.png','Clerk-Head-Lines-Preview.gif']:
    im=Image.open(root/n);assert im.n_frames==16;print(n,im.size,im.n_frames)
print('RGBA colors including transparency:',len(colors),'alpha: binary; no frame downscale')




