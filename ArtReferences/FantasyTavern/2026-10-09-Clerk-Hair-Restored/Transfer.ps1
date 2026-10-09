Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;
public static class TransferHair{
 public static void Run(string root,string previous){
 using(var guide=new Bitmap(Path.Combine(root,"Edit-Guide.png"))){
 var boxes=new[]{new Rectangle(505,512,32,29),new Rectangle(498,627,19,31),new Rectangle(648,533,39,42)};
 for(int i=0;i<16;i++)using(var original=new Bitmap(Path.Combine(previous,"Frames",String.Format("Clerk-Clean-{0:00}.png",i))))using(var result=new Bitmap(original)){
 int minx=352,miny=352,maxx=0,maxy=0;
 for(int y=0;y<352;y++)for(int x=0;x<352;x++)if(original.GetPixel(x,y).A==255){minx=Math.Min(minx,x);maxx=Math.Max(maxx,x);miny=Math.Min(miny,y);maxy=Math.Max(maxy,y);}
 foreach(var b in boxes)for(int sy=b.Top;sy<b.Bottom;sy++)for(int sx=b.Left;sx<b.Right;sx++){
 Color c=guide.GetPixel(sx,sy);if(c.A<224||Math.Max(c.R,Math.Max(c.G,c.B))>65)continue;
 int x=minx+(int)Math.Round((sx-422)*(maxx-minx)/421.0),y=miny+(int)Math.Round((sy-436)*(maxy-miny)/766.0);
 Color old=original.GetPixel(x,y);if(old.A==255&&old.R==130&&old.G==83&&old.B==62)result.SetPixel(x,y,Color.FromArgb(18,18,22));
 }
 int changed=0;
 for(int y=0;y<352;y++)for(int x=0;x<352;x++){Color a=original.GetPixel(x,y),b=result.GetPixel(x,y);if(a.ToArgb()!=b.ToArgb()){
 if(a.A!=255||a.R!=130||a.G!=83||a.B!=62||b.ToArgb()!=Color.FromArgb(18,18,22).ToArgb()||y>miny+75)throw new Exception("Unexpected change");changed++;
 }if(a.A!=b.A)throw new Exception("Silhouette changed");}
 result.Save(Path.Combine(root,"Frames",String.Format("Clerk-Clean-{0:00}.png",i)),ImageFormat.Png);
 Console.WriteLine("Frame "+i+": "+changed+" hair pixels changed; silhouette unchanged");
 }
 }
 }
}
'@
[TransferHair]::Run($PSScriptRoot,(Join-Path $PSScriptRoot '../2026-10-09-Clerk-Head-Lines'))
