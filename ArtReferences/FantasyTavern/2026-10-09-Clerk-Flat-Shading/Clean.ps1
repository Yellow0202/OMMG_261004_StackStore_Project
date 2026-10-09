Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.IO;
public static class CleanSprites {
 public static void Run(string root) {
 Color[] pal={Color.FromArgb(31,20,36),Color.FromArgb(61,39,46),Color.FromArgb(130,83,62),Color.FromArgb(91,58,51),Color.FromArgb(246,196,150),Color.FromArgb(183,127,94),Color.FromArgb(255,240,208),Color.FromArgb(158,147,125),Color.FromArgb(87,18,43),Color.FromArgb(145,27,58),Color.FromArgb(255,250,232),Color.FromArgb(128,103,132)};
 using(var src=new Bitmap(Path.Combine(root,"Generated-Sheet.png"))) {
 for(int i=0;i<16;i++) {
 int x0=(int)Math.Round((i%4)*src.Width/4.0),y0=(int)Math.Round((i/4)*src.Height/4.0);
 int x1=(int)Math.Round((i%4+1)*src.Width/4.0),y1=(int)Math.Round((i/4+1)*src.Height/4.0);
 int w=x1-x0,h=y1-y0; bool[] mask=new bool[w*h],seen=new bool[w*h];
 for(int y=0;y<h;y++)for(int x=0;x<w;x++)mask[y*w+x]=src.GetPixel(x0+x,y0+y).A>=224;
 List<int> largest=new List<int>();
 for(int n=0;n<mask.Length;n++)if(mask[n]&&!seen[n]) {
 var component=new List<int>();var q=new Queue<int>();q.Enqueue(n);seen[n]=true;
 while(q.Count>0){int k=q.Dequeue();component.Add(k);int px=k%w,py=k/w;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int xx=px+dx,yy=py+dy;if(xx<0||yy<0||xx>=w||yy>=h)continue;int j=yy*w+xx;if(mask[j]&&!seen[j]){seen[j]=true;q.Enqueue(j);}}}
 if(component.Count>largest.Count)largest=component;
 }
 Array.Clear(mask,0,mask.Length);foreach(int k in largest)mask[k]=true;
 int minx=w,maxx=0,miny=h,maxy=0;foreach(int k in largest){int x=k%w,y=k/w;minx=Math.Min(minx,x);maxx=Math.Max(maxx,x);miny=Math.Min(miny,y);maxy=Math.Max(maxy,y);}
 using(var output=new Bitmap(352,352,PixelFormat.Format32bppArgb)) {
 int ox=(352-(maxx-minx+1))/2-minx,oy=335-maxy;
 foreach(int k in largest) {
 int x=k%w,y=k/w;bool edge=false;
 for(int d=0;d<4;d++){int xx=x+(d==0?-1:d==1?1:0),yy=y+(d==2?-1:d==3?1:0);if(xx<0||yy<0||xx>=w||yy>=h||!mask[yy*w+xx])edge=true;}
 Color c=src.GetPixel(x0+x,y0+y),best=pal[0];int distance=int.MaxValue;
 if(!edge)foreach(Color p in pal){int dr=c.R-p.R,dg=c.G-p.G,db=c.B-p.B,dd=dr*dr+dg*dg+db*db;if(dd<distance){distance=dd;best=p;}}
 output.SetPixel(x+ox,y+oy,best);
 }
 output.Save(Path.Combine(root,"Frames",String.Format("Clerk-Clean-{0:00}.png",i)),ImageFormat.Png);
 }
 }
 }
 }
}
'@
[CleanSprites]::Run($PSScriptRoot)

