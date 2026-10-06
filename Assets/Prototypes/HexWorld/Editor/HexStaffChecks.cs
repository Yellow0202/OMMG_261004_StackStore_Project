using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HexStaffChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    [MenuItem("Stack Store/Hex World/Validate Employee Rules")]
    public static void Rules()
    {
        var options=new HexTestSettings.StaffOptions{wageSeconds=10,passiveExperiencePerSecond=1,jobExperiencePerSecond=2,gradeExperience=new float[]{0,10,20,30,40,50,60}};
        var definition=ScriptableObject.CreateInstance<HexStaffDefinition>();definition.wage=2;definition.flaws=new[]{new HexStaffFlaw{kind=HexStaffFlawKind.CookingSlow,amount=.5f},new HexStaffFlaw{kind=HexStaffFlawKind.WeakStrength,amount=1}};
        var skill=ScriptableObject.CreateInstance<HexStaffSkill>();skill.kind=HexStaffSkillKind.CookingBonus;skill.requiredGrade=HexStaffGrade.B;skill.amount=.25f;options.skills=new[]{skill};
        try
        {
            int gold=2;Func<int,bool> spend=amount=>{if(gold<amount)return false;gold-=amount;return true;};
            var worker=new HexEmployeeState(1,definition,options){job=HexStaffJob.Kitchen};
            Check(worker.Advance(9,options,spend)==9&&gold==2,"Wage charged before payroll boundary");
            Check(worker.Advance(1,options,spend)==1&&gold==0&&!worker.Suspended,"Periodic wage missing");
            Check(worker.Advance(20,options,spend)==10&&worker.arrears==2,"Unpaid staff kept working after deadline");
            float xp=worker.experience[0];Check(worker.Advance(100,options,spend)==0&&worker.experience[0]==xp&&worker.arrears==2,"Debt or XP increased during suspension");
            gold=2;Check(worker.Advance(.1f,options,spend)>.09f&&gold==0&&!worker.Suspended&&worker.wageRemaining>9,"Arrears repayment did not resume work");
            var fresh=new HexEmployeeState(2,definition,options);Check(fresh.Overall(options)==HexStaffGrade.F,"Fresh hire inherits another employee's XP");
            gold=100;fresh.Advance(60,options,spend);Check(fresh.Overall(options)==HexStaffGrade.S&&Enum.GetValues(typeof(HexStaffAbility)).Cast<HexStaffAbility>().All(a=>fresh.Grade(a,options)==HexStaffGrade.S),"F cannot reach S through employment");
            Check(fresh.Skill(HexStaffSkillKind.CookingBonus,options)==.25f,"High-grade skill not unlocked");
            Check(Mathf.Abs(fresh.Efficiency(HexStaffAbility.Cooking,options)-options.gradeEfficiency[6]*1.25f*.5f)<.001f,"Cooking flaw/skill not applied to own efficiency");
            Check(!fresh.CanRemove(HexStaffGrade.F,options),"Weak strength flaw ignored");
            Check(definition.cooking==HexStaffGrade.F&&definition.wage==2,"Progression modified definition asset");
            Debug.Log("HEX_STAFF_RULES_PASS: periodic wages, exact boundary, suspended work/XP/debt clock, arrears resume, independent hires, F-to-S all stats, grade skills, own flaws and immutable definitions.");
        }
        finally{UnityEngine.Object.DestroyImmediate(definition);UnityEngine.Object.DestroyImmediate(skill);}
    }
    [MenuItem("Stack Store/Hex World/Validate Employee Assets")]
    public static void Assets()
    {
        EditorSceneManager.OpenScene("Assets/Prototypes/HexWorld/Scenes/HexWorld.unity");var game=UnityEngine.Object.FindFirstObjectByType<HexPrototype>();var system=game.staffSystem;var ui=game.GetComponentInChildren<HexStaffUI>(true);
        Check(system&&system.game==game&&system.employeePrefab&&system.employeeRoot&&ui&&ui.system==system&&ui.previousPage&&ui.nextPage&&ui.pageLabel,"Scene employee references missing");
        var settings=AssetDatabase.LoadAssetAtPath<HexTestSettings>(HexTestSettingsAuthoring.SettingsPath);var options=settings.staff;
        Check(options.candidates.Length>0&&options.skills.Length>0&&options.trashTestPrefab&&options.disruptionTestPrefab,"Staff data missing");
        Check(options.candidates.Select(c=>c.key).Distinct().Count()==options.candidates.Length&&options.candidates.All(c=>c.flaws.Length>0),"Duplicate IDs or missing flaws");
        Check(options.candidates.All(c=>Enum.IsDefined(typeof(HexStaffGrade),c.cooking)&&Enum.IsDefined(typeof(HexStaffGrade),c.cleaning)&&Enum.IsDefined(typeof(HexStaffGrade),c.persuasion)&&Enum.IsDefined(typeof(HexStaffGrade),c.strength)),"Invalid ability grade");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prototypes/HexWorld/Prefabs/EmployeeUI.prefab");Check(prefab&&prefab.GetComponent<HexStaffUI>()&&prefab.GetComponentsInChildren<Button>(true).Length>=12,"Saved UGUI controls missing");
        Check(system.employeePrefab.GetComponent<HexKitchenOccupant>()&&!system.employeePrefab.GetComponent<HexKitchenOccupant>().assigned,"Employee starts incorrectly assigned");
        foreach(var dropdown in new[]{ui.employeeDropdown,ui.jobDropdown,ui.kitchenDropdown})Check(dropdown.template&&dropdown.captionText.color.r<.2f&&dropdown.itemText.color.r<.2f&&dropdown.GetComponent<Image>().color.r>.5f,"Dropdown contrast or template missing");
        var strings=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Prototypes/HexWorld/Data/HexWorldStrings.asset");var keys=strings.entries.Select(e=>e.key).ToArray();Check(keys.Distinct().Count()==keys.Length,"Duplicate localization keys");
        foreach(var entry in strings.entries.Where(e=>e.key.StartsWith("staff.")))Check(!string.IsNullOrWhiteSpace(entry.english)&&!string.IsNullOrWhiteSpace(entry.korean),"Missing bilingual staff text: "+entry.key);
        foreach(var candidate in options.candidates){Check(keys.Contains(candidate.nameKey)&&keys.Contains(candidate.descriptionKey),"Candidate localization missing");foreach(var flaw in candidate.flaws)Check(keys.Contains(flaw.descriptionKey),"Flaw localization missing");}
        foreach(var skill in options.skills)Check(keys.Contains(skill.nameKey)&&keys.Contains(skill.descriptionKey),"Skill localization missing");
        Debug.Log("HEX_STAFF_ASSETS_PASS: saved hiring/assignment UGUI, contrasted dropdowns, independent employee/task prefabs, candidates/flaws/skills, shared settings and bilingual unique strings.");
    }
    public static void All(){Rules();Assets();}
    public static void AuthorAndValidate(){HexStaffAuthoring.Upgrade();All();HexDirectServiceChecks.Run();HexDirectServiceAssetChecks.Run();HexShopPolishChecks.Run();HexTestSettingsChecks.Run();}
}
