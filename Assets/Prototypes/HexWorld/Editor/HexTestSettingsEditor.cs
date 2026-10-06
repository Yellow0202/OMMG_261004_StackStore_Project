using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HexTestSettings))]
public sealed class HexTestSettingsEditor : Editor
{
    readonly Dictionary<int,bool> expanded=new Dictionary<int,bool>();
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("이 에셋 하나에서 공용 테스트 값을 조절합니다. Play 중 에셋 수정은 종료 후에도 유지됩니다. 시작 골드·재고는 다음 Play, 좌석 수·대기 시간은 새 배치/예약부터 적용됩니다. 초기 시점은 시점 초기화 버튼 또는 다음 Play에 적용됩니다.",MessageType.Info);
        DrawDefaultInspector();
        var settings=(HexTestSettings)target;
        DrawAsset(settings.levelCurve,"레벨업 조건");
        if(settings.itemCatalog)
        {
            DrawAsset(settings.itemCatalog,"아이템 목록");
            foreach(var item in settings.itemCatalog.items)DrawAsset(item,"아이템 / "+(item?item.key:""));
        }
        if(settings.tileTypes!=null)foreach(var tile in settings.tileTypes)DrawAsset(tile,"타일 / "+(tile?tile.key:""));
        if(settings.shopOffers!=null)foreach(var offer in settings.shopOffers)DrawAsset(offer,"상점 / "+(offer?offer.name:""));
    }
    void DrawAsset(Object asset,string title)
    {
        if(!asset)return;int id=asset.GetInstanceID();expanded.TryGetValue(id,out bool open);
        open=EditorGUILayout.Foldout(open,title,true);expanded[id]=open;if(!open)return;
        EditorGUI.indentLevel++;
        EditorGUILayout.HelpBox("연결된 원본 ScriptableObject를 편집합니다. 변경은 이 에셋을 사용하는 곳에 공통 적용됩니다.",MessageType.None);
        var data=new SerializedObject(asset);data.Update();var property=data.GetIterator();bool first=true;
        while(property.NextVisible(first)){first=false;if(property.name=="m_Script")continue;EditorGUILayout.PropertyField(property,true);}
        data.ApplyModifiedProperties();EditorGUI.indentLevel--;
    }
}
