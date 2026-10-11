"""Crop/nearest-neighbour import preparation and inspection sheets; no art painting."""
import json, shutil
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
HERE=Path(__file__).resolve().parent
ART=ROOT/'Assets/Prototypes/HexWorld/Art/Interior'
ART.mkdir(parents=True,exist_ok=True)
manifest=json.loads((HERE/'Prompts.json').read_text(encoding='utf-8'))
manifest['seated']=json.loads((HERE/'Seated-Prompts.json').read_text(encoding='utf-8'))
def crop(im):
    im=im.convert('RGBA')
    alpha=im.getchannel('A').point(lambda a:255 if a>=128 else 0)
    im.putalpha(alpha)
    box=alpha.getbbox()
    if not box: raise ValueError('Empty art')
    return im.crop(box)
for entry in manifest['assets']:
    path=HERE/(entry['name']+'-Source.png')
    if not path.exists(): shutil.copy2(Path(entry['path']),path)
    im=Image.open(path)
    if entry['name']=='Wall':
        im=im.convert('RGB').resize((256,128),Image.Resampling.NEAREST)
    else:
        im=crop(im);width=96 if entry['name'].startswith('Chair') else 160
        im=im.resize((width,round(width*im.height/im.width)),Image.Resampling.NEAREST)
    im.save(ART/(entry['name']+'.png'))
for entry in manifest.get('seated',[]):
    if not entry.get('path'): continue
    path=HERE/(entry['name']+'-Seated-Source.png')
    if not path.exists(): shutil.copy2(Path(entry['path']),path)
    sheet=Image.open(path).convert('RGBA');cw,ch=sheet.width//2,sheet.height//2
    for d in range(4):
        frame=crop(sheet.crop(((d%2)*cw,(d//2)*ch,(d%2+1)*cw,(d//2+1)*ch)))
        frame=frame.resize((round(frame.width*144/frame.height),144),Image.Resampling.NEAREST)
        frame.save(ART/(entry['name']+f'-Seated-{d}.png'))
files=list(ART.glob('*.png'));cols=6;cell=200
preview=Image.new('RGBA',(cols*cell,((len(files)+cols-1)//cols)*cell),(33,39,43,255))
for i,path in enumerate(files):
    im=Image.open(path).convert('RGBA');im.thumbnail((180,180),Image.Resampling.NEAREST)
    preview.alpha_composite(im,((i%cols)*cell+(cell-im.width)//2,(i//cols)*cell+cell-im.height))
preview.convert('RGB').save(HERE/'Asset-Overview.png')
preview.resize((preview.width*2,preview.height*2),Image.Resampling.NEAREST).save(HERE/'Asset-Overview-2x.png')
print('Prepared',len(files),'pixel art assets')
