using System;
using UnityEngine;

/// <summary>Per-hire runtime state; never writes progression or unpaid wages into assets.</summary>
public sealed class HexEmployeeState
{
    public readonly int id;
    public readonly HexStaffDefinition definition;
    public readonly float[] experience=new float[4];
    public HexStaffJob job;
    public HexNavNode kitchen;
    public bool hasKitchen;
    public float wageRemaining;
    public int arrears;
    public bool Suspended=>arrears>0;
    public HexEmployeeState(int id,HexStaffDefinition definition,HexTestSettings.StaffOptions options)
    {
        this.id=id;this.definition=definition;wageRemaining=Mathf.Max(.1f,options.wageSeconds);
        for(int i=0;i<4;i++)experience[i]=Threshold((int)definition.Initial((HexStaffAbility)i),options);
    }
    public static float Threshold(int rank,HexTestSettings.StaffOptions options)
    {
        float value=0;for(int i=1;i<=Mathf.Clamp(rank,0,6);i++)value=Mathf.Max(value+1,options.gradeExperience!=null&&i<options.gradeExperience.Length?options.gradeExperience[i]:value+60);
        return value;
    }
    public HexStaffGrade Grade(HexStaffAbility ability,HexTestSettings.StaffOptions options)
    {
        int rank=0;for(int i=1;i<=6;i++)if(experience[(int)ability]>=Threshold(i,options))rank=i;
        return (HexStaffGrade)rank;
    }
    public HexStaffGrade Overall(HexTestSettings.StaffOptions options)
    {
        int sum=0;for(int i=0;i<4;i++)sum+=(int)Grade((HexStaffAbility)i,options);return (HexStaffGrade)(sum/4);
    }
    public float Flaw(HexStaffFlawKind kind)
    {
        float amount=0;if(definition.flaws!=null)foreach(var flaw in definition.flaws)if(flaw!=null&&flaw.kind==kind)amount+=flaw.amount;
        return Mathf.Clamp01(amount);
    }
    public float Skill(HexStaffSkillKind kind,HexTestSettings.StaffOptions options)
    {
        float result=0;var grade=Overall(options);
        if(options.skills!=null)foreach(var skill in options.skills)if(skill&&skill.kind==kind&&grade>=skill.requiredGrade)result+=skill.amount;
        return result;
    }
    public int Wage(HexTestSettings.StaffOptions options)=>definition.wage<=0?0:Mathf.Max(1,Mathf.CeilToInt(definition.wage*(1-Mathf.Clamp01(Skill(HexStaffSkillKind.WageDiscount,options)))));
    // Debt freezes the wage clock. Pay the single missed payroll before any work or XP resumes.
    public float Advance(float seconds,HexTestSettings.StaffOptions options,Func<int,bool> spend)
    {
        if(seconds<=0)return 0;
        if(arrears>0){if(!spend(arrears))return 0;arrears=0;wageRemaining=Mathf.Max(.1f,options.wageSeconds);}
        wageRemaining=Mathf.Min(wageRemaining,Mathf.Max(.1f,options.wageSeconds));
        float worked=0,remaining=seconds;
        while(remaining>0)
        {
            float chunk=Mathf.Min(remaining,wageRemaining);worked+=chunk;remaining-=chunk;wageRemaining-=chunk;
            for(int i=0;i<4;i++)
            {
                bool matching=(job==HexStaffJob.Kitchen&&i==0)||(job==HexStaffJob.Cleaning&&i==1)||(job==HexStaffJob.Recruiting&&i==2)||(job==HexStaffJob.Security&&i==3);
                experience[i]=Mathf.Min(Threshold(6,options),experience[i]+chunk*(Mathf.Max(0,options.passiveExperiencePerSecond)+(matching?Mathf.Max(0,options.jobExperiencePerSecond):0)));
            }
            if(wageRemaining>0)break;
            int due=Wage(options);
            if(due>0&&!spend(due)){arrears=due;break;}
            wageRemaining=Mathf.Max(.1f,options.wageSeconds);
        }
        return worked;
    }
    public float Efficiency(HexStaffAbility ability,HexTestSettings.StaffOptions options)
    {
        int grade=(int)Grade(ability,options);
        float efficiency=options.gradeEfficiency!=null&&grade<options.gradeEfficiency.Length?options.gradeEfficiency[grade]:1;
        float bonus=Skill(HexStaffSkillKind.WorkBonus,options);
        if(ability==HexStaffAbility.Cooking)bonus+=Skill(HexStaffSkillKind.CookingBonus,options);
        float penalty=ability==HexStaffAbility.Cooking?Flaw(HexStaffFlawKind.CookingSlow):ability==HexStaffAbility.Cleaning?Flaw(HexStaffFlawKind.CleaningSlow):0;
        return Mathf.Max(.01f,efficiency*(1+bonus)*(1-penalty));
    }
    public float PersuasionChance(HexTestSettings.StaffOptions options)=>Mathf.Clamp01(options.persuasionBaseChance+options.persuasionPerGrade*(int)Grade(HexStaffAbility.Persuasion,options)+Skill(HexStaffSkillKind.PersuasionBonus,options)-Flaw(HexStaffFlawKind.PersuasionPenalty));
    public bool CanRemove(HexStaffGrade required,HexTestSettings.StaffOptions options)=>Flaw(HexStaffFlawKind.WeakStrength)<=0&&Grade(HexStaffAbility.Strength,options)>=required;
}
