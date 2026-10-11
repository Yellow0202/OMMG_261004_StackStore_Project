#ifndef HEX_EXTERIOR_FOG_INCLUDED
#define HEX_EXTERIOR_FOG_INCLUDED
TEXTURE2D(_HexFogOwnership);SAMPLER(sampler_HexFogOwnership);
float4 _HexFogGrid; // axial minimum q/r and texture width/height
float4 _HexFogColor;
float4 _HexFogOptions; // enabled, opacity, feather width, frontier opacity
float4 _HexFogPreview; // x/z, radius, enabled
float4 _HexFogMotion; // noise amount, drift speed, build mode, unused
float HexFogOwned(float2 cell)
{
    float2 index=cell-_HexFogGrid.xy;
    if(any(index<0)||any(index>=_HexFogGrid.zw))return 0;
    return SAMPLE_TEXTURE2D_LOD(_HexFogOwnership,sampler_HexFogOwnership,(index+.5)/_HexFogGrid.zw,0).r;
}
float2 HexFogCell(float2 position)
{
    float r=position.y/2.1,q=position.x/2.42487113-r*.5;
    float3 cube=float3(q,-q-r,r),rounded=round(cube),error=abs(rounded-cube);
    if(error.x>error.y&&error.x>error.z)rounded.x=-rounded.y-rounded.z;
    else if(error.y>error.z)rounded.y=-rounded.x-rounded.z;
    else rounded.z=-rounded.x-rounded.y;
    return rounded.xz;
}
float HexFogDistance(float2 position,float2 cell)
{
    float2 center=float2(2.42487113*(cell.x+.5*cell.y),2.1*cell.y),p=abs(position-center);
    return max(p.x,dot(p,float2(.5,.866025404)))-1.212435565;
}
float HexFogAmount(float2 position)
{
    if(_HexFogOptions.x<.5)return 0;
    float2 cell=HexFogCell(position);
    float distance=1000,adjacent=0;
    const float2 offsets[7]={float2(0,0),float2(1,0),float2(1,-1),float2(0,-1),float2(-1,0),float2(-1,1),float2(0,1)};
    [unroll]for(int i=0;i<7;i++)
    {
        float2 at=cell+offsets[i];float owned=HexFogOwned(at);adjacent=max(adjacent,owned);
        if(owned>.5)distance=min(distance,HexFogDistance(position,at));
    }
    float width=max(.01,_HexFogOptions.z);
    float noise=sin(position.x*2.1+_Time.y*_HexFogMotion.y)*sin(position.y*1.7-_Time.y*_HexFogMotion.y*.73);
    float amount=smoothstep(0,width,max(0,distance)*(1+noise*_HexFogMotion.x))*_HexFogOptions.y;
    if(_HexFogMotion.z>.5&&adjacent>.5&&HexFogOwned(cell)<.5)amount=min(amount,_HexFogOptions.w);
    if(_HexFogPreview.w>.5)
    {
        float2 p=abs(position-_HexFogPreview.xy);
        float d=max(p.x,dot(p,float2(.5,.866025404)))-_HexFogPreview.z*.866025404;
        amount*=smoothstep(0,width*.3,max(0,d));
    }
    return saturate(amount);
}
half3 HexFogTint(half3 color,float2 position){return lerp(color,_HexFogColor.rgb,HexFogAmount(position));}
#endif
