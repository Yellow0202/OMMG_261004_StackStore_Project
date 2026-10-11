"""Crop, nearest-neighbour export and integer-scale QA for generated icons."""
import json,shutil
from pathlib import Path
from PIL import Image
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
ART=ROOT/'Assets/Prototypes/HexWorld/Art/Items'
ART.mkdir(parents=True,exist_ok=True)
assets=json.loads((HERE/'Prompts.json').read_text(encoding='utf-8'))['assets']
preview=Image.new('RGBA',(96*len(assets),96),(35,40,44,255))
for i,entry in enumerate(assets):
    source=HERE/(entry['name']+'-Source.png')
    if not source.exists():shutil.copy2(entry['path'],source)
    im=Image.open(source).convert('RGBA');alpha=im.getchannel('A').point(lambda x:255 if x>=128 else 0)
    im.putalpha(alpha);im=im.crop(alpha.getbbox());ratio=56/max(im.size)
    im=im.resize((round(im.width*ratio),round(im.height*ratio)),Image.Resampling.NEAREST)
    icon=Image.new('RGBA',(64,64));icon.alpha_composite(im,((64-im.width)//2,(64-im.height)//2))
    icon.save(ART/(entry['name']+'.png'));preview.alpha_composite(icon,(i*96+16,16))
preview.save(HERE/'Icons.png');preview.resize((preview.width*2,preview.height*2),Image.Resampling.NEAREST).save(HERE/'Icons-2x.png')
print('Prepared six 64x64 icons')
