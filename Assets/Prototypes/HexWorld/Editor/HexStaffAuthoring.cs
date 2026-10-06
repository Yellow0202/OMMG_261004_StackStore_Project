using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Author saved UGUI, employee/task prefabs and bilingual balance data.</summary>
public static class HexStaffAuthoring
{
    const string Root="Assets/Prototypes/HexWorld";
    static Font font;
    static LocalizationTable strings;
    static Color Navy=new Color(.045f,.08f,.12f,.98f),Teal=new Color(.10f,.48f,.44f);
    [MenuItem("Stack Store/Hex World/Add Employees")]
    public static void Upgrade()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before authoring.");
        var scene=EditorSceneManager.OpenScene(Root+"/Scenes/HexWorld.unity");var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();
        font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",20);strings=AssetDatabase.LoadAssetAtPath<LocalizationTable>(Root+"/Data/HexWorldStrings.asset");AddStrings();strings.Rebuild();EditorUtility.SetDirty(strings);
        if(game.staffSystem)
        {
            var saved=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/EmployeeUI.prefab");AddPagination(saved.GetComponent<HexStaffUI>());PrefabUtility.SaveAsPrefabAsset(saved,Root+"/Prefabs/EmployeeUI.prefab");PrefabUtility.UnloadPrefabContents(saved);AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Root+"/Scenes/HexWorld.unity");Debug.Log("HEX_STAFF_ALREADY_AUTHORED: preserved data/UI and connected candidate pagination.");return;
        }
        var settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>(HexTestSettingsAuthoring.SettingsPath);
        var rookie=Employee("RookieEmployee","staff.rookie",3,1,new Color(.45f,.9f,.6f),0,0,0,0,
            Flaw(HexStaffFlawKind.CookingSlow,.2f,"staff.flaw.cooking"),Flaw(HexStaffFlawKind.CleaningSlow,.25f,"staff.flaw.cleaning"));
        var promoter=Employee("PromoterEmployee","staff.promoter",7,2,new Color(.9f,.55f,.85f),1,2,4,0,
            Flaw(HexStaffFlawKind.PersuasionPenalty,.1f,"staff.flaw.persuasion"),Flaw(HexStaffFlawKind.WaitingGuestEjection,.05f,"staff.flaw.ejection"),Flaw(HexStaffFlawKind.WeakStrength,1,"staff.flaw.weak"));
        var veteran=Employee("VeteranEmployee","staff.veteran",12,3,new Color(.95f,.72f,.35f),4,4,4,4,
            Flaw(HexStaffFlawKind.CookingSlow,.1f,"staff.flaw.cooking"));
        settings.staff.candidates=new[]{rookie,promoter,veteran};
        settings.staff.skills=new[]{Skill("QuickHands",HexStaffGrade.B,HexStaffSkillKind.CookingBonus,.2f),Skill("SilverTongue",HexStaffGrade.A,HexStaffSkillKind.PersuasionBonus,.15f),Skill("VeteranInstinct",HexStaffGrade.S,HexStaffSkillKind.WorkBonus,.3f),Skill("LoyalPartner",HexStaffGrade.S,HexStaffSkillKind.WageDiscount,.34f)};
        var trash=Asset<HexStaffTargetDefinition>("TrashTask");trash.nameKey="staff.target.trash";trash.kind=HexStaffTargetKind.Trash;trash.workSeconds=8;EditorUtility.SetDirty(trash);
        var threat=Asset<HexStaffTargetDefinition>("DisruptionTask");threat.nameKey="staff.target.disruption";threat.kind=HexStaffTargetKind.Disruption;threat.workSeconds=6;threat.requiredStrength=HexStaffGrade.D;EditorUtility.SetDirty(threat);
        MakeActor("Employee",settings,null);MakeActor("TestTrash",settings,trash);MakeActor("TestDisruption",settings,threat);
        settings.staff.trashTestPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/TestTrash.prefab").GetComponent<HexStaffWorkTarget>();
        settings.staff.disruptionTestPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/TestDisruption.prefab").GetComponent<HexStaffWorkTarget>();EditorUtility.SetDirty(settings);
        var system=game.gameObject.AddComponent<HexStaffSystem>();system.game=game;system.employeePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Employee.prefab").GetComponent<HexWorldActor>();
        var actors=new GameObject("Employees And Work Targets");actors.transform.SetParent(game.transform,false);system.employeeRoot=actors.transform;game.staffSystem=system;
        var canvas=game.buildButton.GetComponentInParent<Canvas>();var root=Rect("Employee UI",canvas.transform,Vector2.zero,new Vector2(1600,900));var ui=root.gameObject.AddComponent<HexStaffUI>();ui.system=system;
        ui.open=Button("Open Employees",root,new Vector2(645,103),new Vector2(245,40),"staff.open");
        var panel=Panel("Employee Modal",root,Vector2.zero,new Vector2(1600,900),new Color(.015f,.028f,.04f,.98f));ui.panel=panel.gameObject;
        Label("Title",panel,new Vector2(0,357),new Vector2(900,50),30,"staff.title");ui.balance=Label("Balance",panel,new Vector2(0,306),new Vector2(1100,35),21);
        ui.hiringTab=Button("Hiring Tab",panel,new Vector2(-150,257),new Vector2(280,42),"staff.tab.hiring");ui.rosterTab=Button("Roster Tab",panel,new Vector2(150,257),new Vector2(280,42),"staff.tab.roster");
        var hiring=Rect("Hiring",panel,Vector2.zero,new Vector2(1600,900));ui.hiringPanel=hiring.gameObject;ui.cards=new HexStaffHireCard[3];
        for(int i=0;i<3;i++)
        {
            var card=Panel("Candidate "+i,hiring,new Vector2((i-1)*430,-20),new Vector2(405,450),Navy);var data=new HexStaffHireCard{definition=settings.staff.candidates[i]};
            var icon=Panel("Portrait",card,new Vector2(0,165),new Vector2(65,65),Color.white).GetComponent<Image>();icon.preserveAspect=true;icon.sprite=data.definition.picture;icon.color=data.definition.color;icon.raycastTarget=false;data.icon=icon;
            data.title=Label("Name And Grade",card,new Vector2(0,112),new Vector2(385,40),23);data.description=Label("Abilities",card,new Vector2(0,13),new Vector2(375,158),19);
            data.terms=Label("Hiring And Wage",card,new Vector2(0,-120),new Vector2(375,72),19);data.hire=Button("Hire",card,new Vector2(0,-185),new Vector2(340,45),"staff.hire");ui.cards[i]=data;
            data.title.text=Resolve(data.definition.nameKey);data.description.text=Resolve(data.definition.descriptionKey);data.terms.text=Resolve("staff.terms").Replace("{price}",data.definition.hirePrice.ToString()).Replace("{wage}",data.definition.wage.ToString()).Replace("{seconds}",settings.staff.wageSeconds.ToString());
        }
        var roster=Rect("Roster",panel,Vector2.zero,new Vector2(1600,900));ui.rosterPanel=roster.gameObject;
        ui.employeeDropdown=Dropdown("Employee Selection",roster,game.tileDropdown,new Vector2(0,195),new Vector2(1080,42));
        ui.jobDropdown=Dropdown("Job",roster,game.tileDropdown,new Vector2(-385,135),new Vector2(250,42));ui.kitchenDropdown=Dropdown("Kitchen",roster,game.tileDropdown,new Vector2(-25,135),new Vector2(430,42));ui.assign=Button("Assign Job",roster,new Vector2(405,135),new Vector2(210,42),"staff.assign");
        ui.summary=Label("Current Assignment",roster,new Vector2(0,76),new Vector2(1320,38),20);
        ui.abilities=Label("Ability Grades And Payroll",roster,new Vector2(-345,-80),new Vector2(600,250),19);ui.abilities.alignment=TextAnchor.UpperLeft;
        ui.traits=Label("Skills And Flaws",roster,new Vector2(340,-80),new Vector2(670,250),18);ui.traits.alignment=TextAnchor.UpperLeft;
        ui.testTrash=Button("Test Trash",roster,new Vector2(-430,-247),new Vector2(330,40),"staff.test.trash");ui.testDisruption=Button("Test Disruption",roster,new Vector2(0,-247),new Vector2(330,40),"staff.test.disruption");ui.testExperience=Button("Test Growth",roster,new Vector2(430,-247),new Vector2(330,40),null);ui.testExperience.GetComponentInChildren<Text>().text=Resolve("staff.test.xp").Replace("{xp}",settings.staff.testExperience.ToString());
        ui.message=Label("Message",panel,new Vector2(0,-297),new Vector2(1450,36),18,"staff.help");
        Label("Pause Notice",panel,new Vector2(0,-334),new Vector2(1100,28),16,"staff.paused");ui.close=Button("Close",panel,new Vector2(0,-382),new Vector2(330,44),"staff.close");ui.testGold=Button("Test Gold",panel,new Vector2(545,-382),new Vector2(270,44),null);ui.testGold.GetComponentInChildren<Text>().text=Resolve("staff.test.gold").Replace("{gold}",settings.staff.testGold.ToString());
        root.gameObject.AddComponent<PrototypeFontBinding>();roster.gameObject.SetActive(false);panel.gameObject.SetActive(false);
        AddPagination(ui);
        // Save an independent UGUI prefab, then bind only the scene instance to its controller.
        ui.system=null;var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"/Prefabs/EmployeeUI.prefab");UnityEngine.Object.DestroyImmediate(root.gameObject);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,canvas.transform);instance.GetComponent<HexStaffUI>().system=system;
        EditorUtility.SetDirty(game);EditorUtility.SetDirty(system);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("HEX_STAFF_AUTHORED: hiring/roster UGUI, three employees, four skills, five flaw kinds, test targets and shared settings.");
    }
    static T Asset<T>(string name) where T:ScriptableObject
    {
        string path=Root+"/Data/"+name+".asset";var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(!asset){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}return asset;
    }
    static HexStaffDefinition Employee(string key,string text,int hire,int wage,Color color,int cooking,int cleaning,int persuasion,int strength,params HexStaffFlaw[] flaws)
    {
        var data=Asset<HexStaffDefinition>(key);data.key=key;data.nameKey=text+".name";data.descriptionKey=text+".desc";data.picture=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/CharacterWalk0.png");data.color=color;data.hirePrice=hire;data.wage=wage;
        data.cooking=(HexStaffGrade)cooking;data.cleaning=(HexStaffGrade)cleaning;data.persuasion=(HexStaffGrade)persuasion;data.strength=(HexStaffGrade)strength;data.flaws=flaws;EditorUtility.SetDirty(data);return data;
    }
    static HexStaffFlaw Flaw(HexStaffFlawKind kind,float amount,string key)=>new HexStaffFlaw{kind=kind,amount=amount,descriptionKey=key};
    static HexStaffSkill Skill(string key,HexStaffGrade grade,HexStaffSkillKind kind,float amount)
    {
        var skill=Asset<HexStaffSkill>(key);skill.key=key;skill.nameKey="staff.skill."+key+".name";skill.descriptionKey="staff.skill."+key+".desc";skill.requiredGrade=grade;skill.kind=kind;skill.amount=amount;EditorUtility.SetDirty(skill);return skill;
    }
    static void MakeActor(string name,HexTestSettings settings,HexStaffTargetDefinition target)
    {
        var root=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/WorldCustomer.prefab");root.name=name;var view=root.GetComponent<HexWorldActor>();view.waiting=target;view.patienceCanvas.gameObject.SetActive(target);
        foreach(var facing in root.GetComponentsInChildren<HexCameraFacingSprite>(true)){facing.settings=settings;facing.overrideSharedVisual=false;}
        if(!target){var marker=root.AddComponent<HexKitchenOccupant>();marker.assigned=false;}
        else{var work=root.AddComponent<HexStaffWorkTarget>();work.definition=target;work.view=view;view.body.color=target.kind==HexStaffTargetKind.Trash?new Color(.55f,.4f,.25f):new Color(.85f,.25f,.2f);if(target.kind==HexStaffTargetKind.Trash){view.body.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Soup.png");view.walkFrames=Array.Empty<Sprite>();}}
        PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+name+".prefab");PrefabUtility.UnloadPrefabContents(root);
    }
    static Dropdown Dropdown(string name,Transform parent,Dropdown source,Vector2 position,Vector2 size)
    {
        var dropdown=UnityEngine.Object.Instantiate(source,parent);dropdown.name=name;dropdown.onValueChanged=new Dropdown.DropdownEvent();var rect=(RectTransform)dropdown.transform;rect.anchorMin=rect.anchorMax=Vector2.one*.5f;rect.anchoredPosition=position;rect.sizeDelta=size;dropdown.ClearOptions();dropdown.AddOptions(new System.Collections.Generic.List<string>{Resolve("staff.none")});return dropdown;
    }
    static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size){var root=new GameObject(name,typeof(RectTransform));var rect=root.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=Vector2.one*.5f;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;}
    static RectTransform Panel(string name,Transform parent,Vector2 position,Vector2 size,Color color){var rect=Rect(name,parent,position,size);rect.gameObject.AddComponent<Image>().color=color;return rect;}
    static Text Label(string name,Transform parent,Vector2 position,Vector2 size,int fontSize,string key=null){var rect=Rect(name,parent,position,size);var text=rect.gameObject.AddComponent<Text>();text.font=font;text.fontSize=fontSize;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;if(key!=null){rect.gameObject.AddComponent<LocalizedLabel>().key=key;text.text=Resolve(key);}return text;}
    static Button Button(string name,Transform parent,Vector2 position,Vector2 size,string key){var rect=Panel(name,parent,position,size,Teal);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();Label("Label",rect,Vector2.zero,size,18,key);return button;}
    static string Resolve(string key)=>strings.Resolve(key,DisplayLanguage.Korean);
    static void AddPagination(HexStaffUI ui)
    {
        if(ui.previousPage&&ui.nextPage&&ui.pageLabel)return;
        ui.previousPage=Button("Previous Candidates",ui.hiringPanel.transform,new Vector2(-390,-264),new Vector2(220,32),"staff.page.previous");
        ui.nextPage=Button("Next Candidates",ui.hiringPanel.transform,new Vector2(390,-264),new Vector2(220,32),"staff.page.next");
        ui.pageLabel=Label("Candidate Page",ui.hiringPanel.transform,new Vector2(0,-264),new Vector2(500,32),16);ui.pageLabel.text=Resolve("staff.page").Replace("{page}","1").Replace("{pages}","1").Replace("{count}","3");
    }
    public static void RefreshStrings(){strings=AssetDatabase.LoadAssetAtPath<LocalizationTable>(Root+"/Data/HexWorldStrings.asset");AddStrings();strings.Rebuild();EditorUtility.SetDirty(strings);AssetDatabase.SaveAssets();}
    static void Add(string key,string en,string ko){var entry=strings.entries.Find(e=>e.key==key);if(entry!=null)return;entry=new TranslationEntry{key=key,english=en,korean=ko};strings.entries.Add(entry);}
    static void AddStrings()
    {
        Add("staff.data.missing","Configure this data string key.","문자열 키를 설정해주세요.");
        Add("staff.page","Candidates {count} / Page {page} of {pages}","후보 {count}명 · {page}/{pages}페이지");Add("staff.page.previous","PREVIOUS","이전 후보");Add("staff.page.next","NEXT","다음 후보");
        Add("staff.open","EMPLOYEES","아르바이트");Add("staff.title","EMPLOYEE MANAGEMENT","아르바이트 고용 · 업무 관리");Add("staff.tab.hiring","HIRE","직원 고용");Add("staff.tab.roster","ROSTER / ASSIGNMENTS","직원 목록 · 업무 지시");Add("staff.hire","HIRE","고용하기");Add("staff.assign","ASSIGN","업무 적용");Add("staff.close","BACK TO GAME","게임으로 돌아가기");
        Add("staff.help","Hire, select an employee and assign a task. Unpaid staff stop work until wages are paid.","고용 후 직원 목록에서 업무를 적용하세요. 급여 부족 시 중단, 미지급 급여 지급 후 재개합니다.");Add("staff.paused","Game time and wages pause while this panel is open.","관리 창이 열려 있으면 게임 시간과 급여 주기가 일시정지됩니다.");
        Add("staff.balance","Gold {gold} / Employees {count} of {max}","보유 골드 {gold} · 직원 {count}/{max}명");Add("staff.terms","Hire: {price} gold\nWage: {wage} gold every {seconds}s","고용 {price}골드\n급여 {seconds}초마다 {wage}골드");Add("staff.candidate.title","{name} / {grade}","{name} · 종합 {grade}");Add("staff.employee.option","#{id} {name}","#{id} {name}");Add("staff.none","No employees hired.","고용된 직원이 없습니다.");
        Add("staff.hire.failed","Not enough gold or employee limit reached.","골드가 부족하거나 고용 한도에 도달했습니다.");Add("staff.hired","Employee hired. Assign a task in the roster.","고용 완료. 직원 목록에서 업무를 지정하세요.");Add("staff.assigned","Task assigned.","업무를 적용했습니다.");Add("staff.assign.route","Kitchen missing or unreachable. Check the route.","주방이 없거나 이동할 수 없습니다. 통로를 확인하세요.");Add("staff.assign.occupied","Another employee is already assigned to this kitchen.","다른 직원이 배치된 주방입니다.");Add("staff.build.worker","An employee occupies or is assigned to this tile. Reassign them before moving it.","직원이 있거나 배치된 타일입니다. 업무를 변경한 뒤 이동·회수하세요.");
        Add("staff.job.Standby","Stand by","대기");Add("staff.job.Kitchen","Cook in kitchen","주방 · 음식 생산");Add("staff.job.Cleaning","Clean litter","쓰레기 청소");Add("staff.job.Recruiting","Persuade customers","주변 고객 회유");Add("staff.job.Security","Remove disruptions","진상 고객 퇴치");
        Add("staff.ability.Cooking","Cooking","요리");Add("staff.ability.Cleaning","Cleaning","청소");Add("staff.ability.Persuasion","Persuasion","회유");Add("staff.ability.Strength","Strength","힘");Add("staff.grade.short","{ability}: {grade}","{ability} {grade}");Add("staff.ability.line","{ability}: {grade} / XP {xp} → {next}","{ability} {grade} · 경험치 {xp} / {next}");Add("staff.max","MAX","최대");
        Add("staff.employee.summary","Overall {grade} / {job} / {status}","종합 {grade} · {job} · {status}");Add("staff.payroll","Wage {wage} gold / Next in {seconds}s\nUnpaid: {debt} gold","급여 {wage}골드 · 다음 지급 {seconds}초\n미지급 급여 {debt}골드");Add("staff.results","Cooked {food} / Persuaded {recruited}\nCleaned {cleaned} / Removed {removed} / Ejected {ejected}","음식 {food}개 · 회유 {recruited}명\n청소 {cleaned}회 · 퇴치 {removed}회 · 손님 퇴출 {ejected}명");
        Add("staff.kitchen.option","Floor {floor} / Kitchen ({x}, {y})","{floor}층 주방 ({x}, {y})");Add("staff.kitchen.none","No kitchens available.","배치 가능한 주방 없음");Add("staff.skills","SKILLS","획득 스킬");Add("staff.skills.none","No skills yet. Higher overall grades unlock skills.","아직 없습니다. 종합 등급 성장 시 스킬을 얻습니다.");Add("staff.flaws","FLAWS","단점");Add("staff.skill.line","{name}: {description}","{name} · {description}");
        Add("staff.rookie.name","Sprout","새싹");Add("staff.rookie.desc","A beginner who grows with paid employment.","낮은 등급에서 꾸준히 성장하는 신입입니다.");Add("staff.promoter.name","Chatter","수다");Add("staff.promoter.desc","A persuasive promoter with unreliable customer manners.","회유에 능하지만 손님 응대와 힘에 약점이 있습니다.");Add("staff.veteran.name","Steady","든든");Add("staff.veteran.desc","A seasoned all-rounder with a careful cooking pace.","여러 업무에 능하지만 요리 속도가 조금 느립니다.");
        Add("staff.flaw.cooking","Own cooking speed -{value}%.","자신의 음식 생산 속도 -{value}%");Add("staff.flaw.cleaning","Own cleaning speed -{value}%.","자신의 청소 속도 -{value}%");Add("staff.flaw.persuasion","Persuasion chance -{value} percentage points.","회유 성공 확률 -{value}%p");Add("staff.flaw.ejection","Each check: {value}% chance to drive away a nearby waiting guest.","주기적 확인 시 {value}% 확률로 주변 대기 손님 퇴출");Add("staff.flaw.weak","Cannot remove disruptive customers.","힘이 약하여 진상 고객 퇴치 불가");
        Add("staff.skill.QuickHands.name","Quick Hands","빠른 손놀림");Add("staff.skill.QuickHands.desc","Own cooking speed +{value}%.","음식 생산 속도 +{value}%");Add("staff.skill.SilverTongue.name","Silver Tongue","설득의 달인");Add("staff.skill.SilverTongue.desc","Persuasion chance +{value} percentage points.","회유 확률 +{value}%p");Add("staff.skill.VeteranInstinct.name","Veteran Instinct","베테랑의 감각");Add("staff.skill.VeteranInstinct.desc","Own task efficiency +{value}%.","자신의 업무 효율 +{value}%");Add("staff.skill.LoyalPartner.name","Loyal Partner","오랜 동료");Add("staff.skill.LoyalPartner.desc","Wage -{value}% (minimum 1 gold).","급여 -{value}% · 최소 1골드");
        Add("staff.status.station.wait","Other employees are preparing remaining dishes.","다른 직원이 남은 음식을 준비 중");
        Add("staff.status.standby","Standing by","대기 중");Add("staff.status.unpaid","SUSPENDED: unpaid wages","미지급 급여 · 업무 중단");Add("staff.status.moving","Moving to task","업무 위치로 이동 중");Add("staff.status.blocked","Route blocked","통로 확인 필요");Add("staff.status.cooking","Preparing food","음식 생산 중");Add("staff.status.food.ready","All dishes ready","모든 음식 준비 완료");Add("staff.status.kitchen.missing","Assign an available kitchen","사용 가능한 주방 배치 필요");Add("staff.status.recruiting","Seeking customers","주변 고객 회유 중");Add("staff.status.cleaning","Cleaning litter","쓰레기 청소 중");Add("staff.status.security","Removing disruption","방해물 퇴치 중");Add("staff.status.clean.wait","Awaiting litter (future comfort system)","쓰레기 대기 · 쾌적함 시스템은 추후 추가");Add("staff.status.security.wait","Awaiting disruptions (future update)","진상·방해물 대기 · 본 시스템은 추후 추가");Add("staff.status.weak","Insufficient strength for disruption","힘 능력 또는 단점으로 퇴치 불가");
        Add("staff.target.trash","Test litter","테스트 쓰레기");Add("staff.target.disruption","Test disruption","테스트 방해물");Add("staff.test.trash","TEST: ADD LITTER","테스트 · 쓰레기 생성");Add("staff.test.disruption","TEST: ADD DISRUPTION","테스트 · 방해물 생성");Add("staff.test.gold","TEST: +{gold} GOLD","테스트 · 골드 +{gold}");Add("staff.test.xp","TEST: +{xp} XP","테스트 · 경험치 +{xp}");
    }
}
