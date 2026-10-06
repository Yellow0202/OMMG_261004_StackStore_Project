using System.Collections.Generic;
using UnityEngine;

/// <summary>Each dish retains its own preparation progress until served.</summary>
public sealed class HexFoodPreparation
{
    readonly List<float> progress=new List<float>();
    public int Count=>progress.Count;
    public int ReadyCount{get{int count=0;foreach(float value in progress)if(value>=1)count++;return count;}}
    public float this[int slot]=>progress[slot];
    public void SetCount(int count){while(progress.Count<count)progress.Add(0);while(progress.Count>count)progress.RemoveAt(progress.Count-1);}
    public void Tick(float seconds,float duration,bool atStation)
    {
        if(!atStation)return;
        for(int i=0;i<progress.Count;i++)progress[i]=Mathf.Clamp01(progress[i]+Mathf.Max(0,seconds)/Mathf.Max(.1f,duration));
    }
    public int ReadySlot(){for(int i=0;i<progress.Count;i++)if(progress[i]>=1)return i;return -1;}
    public int PreparingSlot(){for(int i=0;i<progress.Count;i++)if(progress[i]<1)return i;return -1;}
    public void TickSlot(int slot,float seconds,float duration)
    {
        if(slot>=0&&slot<Count)progress[slot]=Mathf.Clamp01(progress[slot]+Mathf.Max(0,seconds)/Mathf.Max(.1f,duration));
    }
    public bool Consume(int slot){if(slot<0||slot>=Count||progress[slot]<1)return false;progress[slot]=0;return true;}
}
