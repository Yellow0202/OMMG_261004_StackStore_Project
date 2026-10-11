"""Extract generated complete figures; normalize and mirror into authored animation sprites."""
import json, shutil
from pathlib import Path
import numpy as np
from PIL import Image
def components(mask):
    labels=np.zeros(mask.shape,dtype=np.int32);parents=[0];previous=[]
    def find(x):
        while parents[x]!=x:parents[x]=parents[parents[x]];x=parents[x]
        return x
    for y,row in enumerate(mask):
        edges=np.flatnonzero(np.diff(np.r_[False,row,False]));current=[]
        for start,end in zip(edges[::2],edges[1::2]):
            hits=[find(label) for a,b,label in previous if a<=end and b>=start]
            if hits:
                label=min(hits)
                for other in hits:parents[find(other)]=label
            else:label=len(parents);parents.append(label)
            labels[y,start:end]=label;current.append((start,end,label))
        previous=current
    lookup=np.array([find(i) for i in range(len(parents))]);labels=lookup[labels]
    objects={}
    for label in np.unique(labels):
        if not label:continue
        ys,xs=np.where(labels==label);objects[label]=(slice(ys.min(),ys.max()+1),slice(xs.min(),xs.max()+1))
    return labels,objects
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
ART=ROOT/'Assets/Prototypes/HexWorld/Art/StaffAnimation'
DIRS=['North','North-East','East','South-East','South','South-West','West','North-West']
entries=json.loads((HERE/'Prompts.json').read_text(encoding='utf-8'))['assets']
for e in entries:
    source=HERE/(e['staff']+'-'+e['direction']+'-Source.png')
    if not source.exists(): shutil.copy2(e['path'],source)
    image=Image.open(source).convert('RGBA');array=np.array(image)
    labels,objects=components(array[:,:,3]>=128)
    sizes=np.bincount(labels.ravel());sizes[0]=0
    ids=np.argsort(sizes)[-12:]
    if min(sizes[ids])<200: raise ValueError('Missing full figures: '+str(source))
    parts=[]
    for label in ids:
        ys,xs=objects[label];parts.append((label,xs.start,ys.start,xs.stop,ys.stop))
    parts.sort(key=lambda p:(p[2]+p[4])/2)
    parts=[p for row in range(3) for p in sorted(parts[row*4:row*4+4],key=lambda p:p[1])]
    scale=192/(parts[0][4]-parts[0][2])
    folder=ART/e['staff']/e['direction'];folder.mkdir(parents=True,exist_ok=True)
    review=Image.new('RGBA',(256*4,256*3),(35,40,44,255))
    for f,(label,x0,y0,x1,y1) in enumerate(parts):
        crop=array[y0:y1,x0:x1].copy();crop[:,:,3]=np.where(labels[y0:y1,x0:x1]==label,255,0)
        figure=Image.fromarray(crop).resize((round((x1-x0)*scale),round((y1-y0)*scale)),Image.Resampling.NEAREST)
        if figure.width>224 or figure.height>216: raise ValueError('Inconsistent figure size: '+str(source))
        frame=Image.new('RGBA',(256,256));frame.alpha_composite(figure,((256-figure.width)//2,224-figure.height))
        name=('Walk-%02d'%f) if f<8 else ('Idle-%02d'%(f-8))
        frame.save(folder/(name+'.png'));review.alpha_composite(frame,((f%4)*256,(f//4)*256))
    review.save(HERE/(e['staff']+'-'+e['direction']+'-Frames.png'))
corrections=HERE/'OppositeSteps.json'
if corrections.exists():
    for e in json.loads(corrections.read_text(encoding='utf-8'))['assets']:
        source=HERE/(e['staff']+'-Opposite-Steps.png')
        if not source.exists(): shutil.copy2(e['path'],source)
        array=np.array(Image.open(source).convert('RGBA'));labels,objects=components(array[:,:,3]>=128)
        sizes=np.bincount(labels.ravel());sizes[0]=0;ids=np.argsort(sizes)[-8:]
        if min(sizes[ids])<200:raise ValueError('Missing correction figures: '+str(source))
        parts=[]
        for label in ids:
            ys,xs=objects[label];parts.append((label,xs.start,ys.start,xs.stop,ys.stop))
        parts.sort(key=lambda p:(p[2]+p[4])/2)
        for row,direction in enumerate(['North-East','South-East']):
            poses=sorted(parts[row*4:row*4+4],key=lambda p:p[1]);scale=192/(poses[0][4]-poses[0][2])
            for f,(label,x0,y0,x1,y1) in enumerate(poses):
                crop=array[y0:y1,x0:x1].copy();crop[:,:,3]=np.where(labels[y0:y1,x0:x1]==label,255,0)
                figure=Image.fromarray(crop).resize((round((x1-x0)*scale),round((y1-y0)*scale)),Image.Resampling.NEAREST)
                if figure.width>224 or figure.height>216:raise ValueError('Correction size mismatch')
                frame=Image.new('RGBA',(256,256));frame.alpha_composite(figure,((256-figure.width)//2,224-figure.height))
                frame.save(ART/e['staff']/direction/('Walk-%02d.png'%(f+4)))
for staff in ['Staff-01','Staff-02','Staff-03']:
    for target,source in [('South-West','South-East'),('West','East'),('North-West','North-East')]:
        folder=ART/staff/target;folder.mkdir(parents=True,exist_ok=True)
        for path in (ART/staff/source).glob('*.png'):
            Image.open(path).transpose(Image.Transpose.FLIP_LEFT_RIGHT).save(folder/path.name)
    for direction in DIRS:
        for state,count in [('Walk',8),('Idle',4)]:
            frames=[Image.open(ART/staff/direction/(state+'-%02d.png'%i)).convert('RGBA') for i in range(count)]
            backgrounds=[]
            for frame in frames:
                base=Image.new('RGBA',(256,256),(35,40,44,255));base.alpha_composite(frame);backgrounds.append(base.convert('RGB'))
            backgrounds[0].save(HERE/(staff+'-'+direction+'-'+state+'.gif'),save_all=True,append_images=backgrounds[1:],duration=125 if state=='Walk' else 333,loop=0,disposal=2)
preview=Image.new('RGBA',(256*8,256*3),(35,40,44,255))
for c,staff in enumerate(['Staff-01','Staff-02','Staff-03']):
    for d,direction in enumerate(DIRS):preview.alpha_composite(Image.open(ART/staff/direction/'Idle-00.png'),(d*256,c*256))
preview.save(HERE/'Directions.png')
print('Prepared 288 sprites: 3 staff x 8 directions x (8 walk + 4 idle)')
