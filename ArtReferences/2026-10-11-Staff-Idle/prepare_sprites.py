import json, shutil, re, uuid
from pathlib import Path
from PIL import Image
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
ART=ROOT/'Assets/Prototypes/HexWorld/Art/StaffIdle'
ART.mkdir(parents=True,exist_ok=True)
template=(ROOT/'Assets/Prototypes/HexWorld/Art/PlayerAnimation/Idle/South.png.meta').read_text(encoding='utf-8')
preview=Image.new('RGBA',(256*3,256),(35,40,44,255))
for i,entry in enumerate(json.loads((HERE/'Prompts.json').read_text(encoding='utf-8'))['assets']):
    source=HERE/(entry['name']+'-Source.png')
    if not source.exists(): shutil.copy2(entry['path'],source)
    im=Image.open(source).convert('RGBA')
    alpha=im.getchannel('A').point(lambda x:255 if x>=128 else 0)
    im.putalpha(alpha); im=im.crop(alpha.getbbox())
    ratio=min(192/im.height,208/im.width)
    im=im.resize((round(im.width*ratio),round(im.height*ratio)),Image.Resampling.NEAREST)
    sprite=Image.new('RGBA',(256,256));sprite.alpha_composite(im,((256-im.width)//2,224-im.height))
    sprite.save(ART/(entry['name']+'.png'));preview.alpha_composite(sprite,(i*256,0))
    meta=ART/(entry['name']+'.png.meta')
    if not meta.exists():
        content=re.sub(r'(?m)^guid: .*$', 'guid: '+uuid.uuid4().hex,template)
        content=re.sub(r'(?m)^    spriteID: .*$', '    spriteID: '+uuid.uuid4().hex,content)
        meta.write_text(content,encoding='utf-8')
if not ART.with_suffix('.meta').exists():
    ART.with_suffix('.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n',encoding='utf-8')
preview.save(HERE/'Staff-Idle-Preview.png')
preview.resize((preview.width*2,512),Image.Resampling.NEAREST).save(HERE/'Staff-Idle-Preview-2x.png')
print('Prepared 3 front Idle sprites: 256x256, body height 192, feet baseline 224')
