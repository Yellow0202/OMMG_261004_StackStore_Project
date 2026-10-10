"""Deterministic diagonal gait authoring. Read immutable backups; never edit faces.
Run from repository root. Rig coordinates use an 84 pixel authoring grid.
"""
from PIL import Image, ImageDraw
from pathlib import Path
import json,hashlib
ROOT=Path('Assets/Prototypes/HexWorld/Art')
OUT=Path('ArtReferences/2026-10-10-Alternating-Gait')
NAMES=['Male-01','Male-02','Male-03','Female-01','Female-02','Female-03','Player']
DIRS=['North-East','South-East','South-West','North-West']
# Hem cuts and sole heights preserve each character's original costume length.
CUTS=[60,59,63,66,60,74,64]
reports=[]; previews=[]
def shade(c,k):return tuple(round(v*k) for v in c[:3])+(255,)
for ci,name in enumerate(NAMES):
    row=[]
    for direction in DIRS:
        player=name=='Player'; folder=ROOT/('PlayerAnimation/Walk' if player else 'CustomerAnimation')/(direction if player else name)
        path=folder/(f'{direction}_0001.png' if player else f'{direction}_Frames.png')
        backup=OUT/'Sources'/name/(direction+'.png'); backup.parent.mkdir(parents=True,exist_ok=True)
        if not backup.exists():backup.write_bytes(path.read_bytes())
        src=Image.open(backup).convert('RGBA'); orig=src if player else src.crop((0,0,256,256))
        native=orig.resize((84,84),Image.Resampling.NEAREST); bbox=native.getbbox(); bottom=bbox[3]-1 if player else min(81,bbox[3]-1)
        cut=CUTS[ci]; sign=1 if direction.endswith('East') else -1
        back=direction.startswith('North'); center=(bbox[0]+bbox[2])/2
        # Back-facing garments have slightly higher hems; retain the original silhouette.
        cut=min(cut,bottom-7)
        pants=[(64,53,51,255),(61,49,45,255),(70,54,48,255),(79,59,52,255),(65,49,47,255),(79,58,48,255),(59,44,49,255)][ci]
        boot=(111,72,48,255)
        body=orig.copy(); body.paste((0,0,0,0),(0,round(cut*orig.height/84),orig.width,orig.height))
        frames=[]; joints=[]
        for f in range(8):
            canvas=Image.new('RGBA',(84,84)); draw=ImageDraw.Draw(canvas); pose={}
            near='R' if sign>0 else 'L'
            for leg in (('L' if near=='R' else 'R'),near):
                phase=(f+(4 if leg=='R' else 0))%8
                t=[1,.707,0,-.707,-1,-.707,0,.707][phase]
                lift=[0,0,0,0,0,2,4,2][phase]
                n=leg==near; side=(1 if n else -1)*sign*(1 if back else -1)
                hx=center+side*2; hy=cut-3
                fx=hx+sign*t*4; sole=bottom-(0 if n else 2)+t*(-1 if back else 1)-lift
                ay=sole-4; ky=(hy+ay)/2; kx=(hx+fx)/2+sign*(1+lift*.55)
                fill=pants if n else shade(pants,.82); outline=(35,25,28,255)
                pts=[(round(hx),round(hy)),(round(kx),round(ky)),(round(fx),round(ay))]
                draw.line(pts,fill=outline,width=6);draw.line(pts,fill=fill,width=4)
                x=round(fx); y=round(sole)
                foot=[(x-3,y-5),(x+2,y-5),(x+2+sign*2,y-2),(x+2+sign*2,y),(x-3,y)]
                draw.polygon(foot,fill=boot if n else shade(boot,.82),outline=outline)
                draw.line((x-2,y-4,x+1,y-4),fill=shade(boot,1.15),width=1)
                pose[leg]={'hip':[hx,hy],'knee':[kx,ky],'foot':[fx,sole],'lift':lift}
            # Composite original full-resolution upper body over the authored pixel legs.
            final=canvas.resize(orig.size,Image.Resampling.NEAREST);final.alpha_composite(body)
            frames.append(final); joints.append(pose)
        assert (joints[0]['L']['foot'][0]-joints[0]['R']['foot'][0])*sign>0
        assert (joints[4]['L']['foot'][0]-joints[4]['R']['foot'][0])*sign<0
        assert joints[2]['R']['lift']>joints[2]['L']['lift']
        assert joints[6]['L']['lift']>joints[6]['R']['lift']
        assert len({hashlib.sha256(i.tobytes()).hexdigest() for i in frames})==8
        for leg in ('L','R'):
            assert len({tuple(p[leg]['hip']) for p in joints})==1
            for f in range(8):
                a=joints[f][leg]['foot'];b=joints[(f+1)%8][leg]['foot']
                assert abs(a[0]-b[0])<=4 and abs(a[1]-b[1])<=4
        for im in frames:
            bounds=im.getbbox()
            assert bounds[0]>=2 and bounds[1]>=2 and bounds[2]<=im.width-2 and bounds[3]<=im.height-2
        if player:
            for f,im in enumerate(frames):im.save(folder/f'{direction}_{f+1:04}.png')
        else:
            atlas=Image.new('RGBA',(1024,512))
            for f,im in enumerate(frames):atlas.paste(im,((f%4)*256,(f//4)*256))
            atlas.save(folder/f'{direction}_Alternating.png');atlas.save(path)
        gif=[im.resize((252,252),Image.Resampling.NEAREST) for im in frames]
        gif[0].save(OUT/f'{name}-{direction}.gif',save_all=True,append_images=gif[1:],duration=100,loop=0,disposal=2)
        strip=Image.new('RGBA',(84*8,84))
        for f,im in enumerate(frames):strip.paste(im.resize((84,84),Image.Resampling.NEAREST),(84*f,0))
        row.append(strip);reports.append({'character':name,'direction':direction,'frames':joints,'checks':'opposite contact feet, opposite passing lift, eight distinct poses'})
    sheet=Image.new('RGBA',(672,336))
    for i,strip in enumerate(row):sheet.paste(strip,(0,84*i))
    sheet.save(OUT/f'{name}-Corrected.png');previews.append(sheet)
overview=Image.new('RGBA',(1344,1008))
for i,sheet in enumerate(previews[:6]):overview.paste(sheet,((i%2)*672,(i//2)*336))
overview.save(OUT/'All-Customers-Corrected.png')
(OUT/'GaitVerification.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
print('Authored and checked 28 cycles / 224 frames')
