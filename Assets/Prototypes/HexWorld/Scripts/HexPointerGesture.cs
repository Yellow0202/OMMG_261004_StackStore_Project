using UnityEngine;
public sealed class HexPointerGesture
{
    Vector2 start,last;
    bool blocked;
    public bool Dragging { get; private set; }
    public void Begin(Vector2 point,bool overUI){start=last=point;blocked=overUI;Dragging=false;}
    public float Move(Vector2 point)
    {
        if(blocked)return 0;
        float threshold=HexTestSettings.Current?HexTestSettings.Current.camera.dragThresholdPixels:8;
        if(!Dragging&&(point-start).sqrMagnitude>=threshold*threshold){Dragging=true;float first=point.y-start.y;last=point;return first;}
        float delta=Dragging?point.y-last.y:0;last=point;return delta;
    }
    public bool Click(Vector2 point){Move(point);return !blocked&&!Dragging;}
}
