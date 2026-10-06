using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[Serializable] public sealed class HexStaffHireCard
{
    public HexStaffDefinition definition;
    public Image icon;
    public Text title,description,terms;
    public Button hire;
}
public sealed class HexStaffUI : MonoBehaviour
{
    public HexStaffSystem system;
    public GameObject panel,hiringPanel,rosterPanel;
    public Button open,close,hiringTab,rosterTab,assign,testGold,testExperience,testTrash,testDisruption;
    public Button previousPage,nextPage;
    public Text pageLabel;
    public int Page { get; private set; }
    public Dropdown employeeDropdown,jobDropdown,kitchenDropdown;
    public Text balance,message,summary,abilities,traits;
    public HexStaffHireCard[] cards;
    public bool IsOpen { get; private set; }
    readonly List<HexNavNode> kitchens=new List<HexNavNode>();
    int listedCount=-1,listedRevision=-1;
    void Awake()
    {
        open.onClick.AddListener(Open);close.onClick.AddListener(Close);hiringTab.onClick.AddListener(()=>ShowTab(false));rosterTab.onClick.AddListener(()=>ShowTab(true));
        foreach(var card in cards){var selectedCard=card;card.hire.onClick.AddListener(()=>{system.Hire(selectedCard.definition);Refresh();});}
        previousPage.onClick.AddListener(()=>{Page=Mathf.Max(0,Page-1);Refresh();});
        nextPage.onClick.AddListener(()=>{Page++;Refresh();});
        employeeDropdown.onValueChanged.AddListener(_=>SelectEmployee());
        assign.onClick.AddListener(()=>{var node=kitchens.Count>0?kitchens[Mathf.Clamp(kitchenDropdown.value,0,kitchens.Count-1)]:default;system.Assign(employeeDropdown.value,(HexStaffJob)jobDropdown.value,node);Refresh();});
        testGold.onClick.AddListener(()=>system.game.AddGold(system.Options.testGold));
        testExperience.onClick.AddListener(()=>{if(system.employees.Count>0){var state=system.employees[employeeDropdown.value].state;for(int i=0;i<4;i++)state.experience[i]=Mathf.Min(HexEmployeeState.Threshold(6,system.Options),state.experience[i]+system.Options.testExperience);Refresh();}});
        testTrash.onClick.AddListener(()=>system.SpawnTestTarget(false));testDisruption.onClick.AddListener(()=>system.SpawnTestTarget(true));
        panel.SetActive(false);RefreshLanguage();
    }
    void OnEnable(){LocalizationService.LanguageChanged+=RefreshLanguage;}
    void OnDisable(){LocalizationService.LanguageChanged-=RefreshLanguage;if(IsOpen)Close();}
    void Update(){if(!system)return;open.interactable=!system.game.IsBuilding&&!system.game.IsChoosing&&!system.game.IsShopping;open.gameObject.SetActive(!system.game.IsChoosing);if(IsOpen)Refresh();}
    public void Open(){if(!system.game.BeginShopping())return;IsOpen=true;panel.SetActive(true);panel.transform.SetAsLastSibling();listedCount=listedRevision=-1;ShowTab(false);Refresh();}
    public void Close(){if(!IsOpen)return;IsOpen=false;panel.SetActive(false);system.game.EndShopping();}
    void ShowTab(bool roster){hiringPanel.SetActive(!roster);rosterPanel.SetActive(roster);Refresh();}
    void RefreshLanguage()
    {
        if(!system)return;listedCount=listedRevision=-1;
        var jobs=new List<string>();foreach(HexStaffJob job in Enum.GetValues(typeof(HexStaffJob)))jobs.Add(LocalizationService.Text("staff.job."+job));
        int selected=jobDropdown.value;jobDropdown.ClearOptions();jobDropdown.AddOptions(jobs);jobDropdown.SetValueWithoutNotify(selected);Refresh();
    }
    void SelectEmployee()
    {
        int i=employeeDropdown.value;if(i<0||i>=system.employees.Count)return;
        var state=system.employees[i].state;jobDropdown.SetValueWithoutNotify((int)state.job);
        int index=kitchens.FindIndex(k=>k.Equals(state.kitchen));if(index>=0)kitchenDropdown.SetValueWithoutNotify(index);Refresh();
    }
    public void Refresh()
    {
        if(!system||!balance)return;var options=system.Options;
        balance.text=LocalizationService.Text("staff.balance","gold",system.game.Gold,"count",system.employees.Count,"max",options.maximumEmployees);
        message.text=LocalizationService.Text(system.MessageKey);
        int candidateCount=options.candidates==null?0:options.candidates.Length;
        int pages=Mathf.Max(1,Mathf.CeilToInt((float)candidateCount/cards.Length));Page=Mathf.Clamp(Page,0,pages-1);
        previousPage.interactable=Page>0;nextPage.interactable=Page<pages-1;
        pageLabel.text=LocalizationService.Text("staff.page","page",Page+1,"pages",pages,"count",candidateCount);
        for(int c=0;c<cards.Length;c++)
        {
            var card=cards[c];int index=Page*cards.Length+c;
            var def=index<candidateCount?options.candidates[index]:null;card.definition=def;card.hire.transform.parent.gameObject.SetActive(def);if(!def)continue;
            var preview=new HexEmployeeState(0,def,options);
            card.icon.sprite=def.picture;card.icon.color=def.color;
            card.title.text=LocalizationService.Text("staff.candidate.title","name",DataText(def.nameKey),"grade",preview.Overall(options));
            card.description.text=DataText(def.descriptionKey)+"\n"+GradeSummary(preview,options);
            card.terms.text=LocalizationService.Text("staff.terms","price",def.hirePrice,"wage",preview.Wage(options),"seconds",options.wageSeconds.ToString("0.#"));card.hire.interactable=IsOpen&&system.CanHire(def);
        }
        if(listedCount!=system.employees.Count)
        {
            int previous=employeeDropdown.value;listedCount=system.employees.Count;
            var names=new List<string>();foreach(var e in system.employees)names.Add(LocalizationService.Text("staff.employee.option","id",e.state.id,"name",DataText(e.state.definition.nameKey)));
            if(names.Count==0)names.Add(LocalizationService.Text("staff.none"));employeeDropdown.ClearOptions();employeeDropdown.AddOptions(names);employeeDropdown.SetValueWithoutNotify(Mathf.Clamp(previous,0,names.Count-1));
            if(system.employees.Count>0)jobDropdown.SetValueWithoutNotify((int)system.employees[employeeDropdown.value].state.job);
        }
        if(system.game.board.Layout!=null&&listedRevision!=system.game.board.Layout.Revision)
        {
            listedRevision=system.game.board.Layout.Revision;kitchens.Clear();kitchens.AddRange(system.Kitchens());
            var names=new List<string>();foreach(var k in kitchens)names.Add(LocalizationService.Text("staff.kitchen.option","floor",k.floor+1,"x",k.cell.x,"y",k.cell.y));
            if(names.Count==0)names.Add(LocalizationService.Text("staff.kitchen.none"));int old=kitchenDropdown.value;kitchenDropdown.ClearOptions();kitchenDropdown.AddOptions(names);kitchenDropdown.SetValueWithoutNotify(Mathf.Clamp(old,0,names.Count-1));
        }
        bool selectedEmployee=system.employees.Count>0;employeeDropdown.interactable=jobDropdown.interactable=selectedEmployee;
        kitchenDropdown.interactable=selectedEmployee&&jobDropdown.value==(int)HexStaffJob.Kitchen;
        assign.interactable=selectedEmployee&&(jobDropdown.value!=(int)HexStaffJob.Kitchen||kitchens.Count>0);
        testExperience.interactable=selectedEmployee;
        testExperience.GetComponentInChildren<Text>().text=LocalizationService.Text("staff.test.xp","xp",options.testExperience);
        testGold.GetComponentInChildren<Text>().text=LocalizationService.Text("staff.test.gold","gold",options.testGold);
        if(!selectedEmployee){summary.text=abilities.text=LocalizationService.Text("staff.none");traits.text=LocalizationService.Text("staff.help");return;}
        var employee=system.employees[employeeDropdown.value];var state=employee.state;
        summary.text=LocalizationService.Text("staff.employee.summary","grade",state.Overall(options),"job",LocalizationService.Text("staff.job."+state.job),"status",LocalizationService.Text(state.Suspended?"staff.status.unpaid":employee.statusKey));
        var stats=new StringBuilder();
        for(int i=0;i<4;i++)
        {
            var ability=(HexStaffAbility)i;var grade=state.Grade(ability,options);float next=HexEmployeeState.Threshold(Mathf.Min(6,(int)grade+1),options);
            stats.AppendLine(LocalizationService.Text("staff.ability.line","ability",LocalizationService.Text("staff.ability."+ability),"grade",grade,"xp",state.experience[i].ToString("0"),"next",grade==HexStaffGrade.S?LocalizationService.Text("staff.max"):next.ToString("0")));
        }
        stats.AppendLine(LocalizationService.Text("staff.payroll","wage",state.Wage(options),"seconds",Mathf.Max(0,state.wageRemaining).ToString("0.0"),"debt",state.arrears));
        stats.AppendLine(LocalizationService.Text("staff.results","food",employee.cooked,"recruited",employee.recruited,"cleaned",employee.cleaned,"removed",employee.removed,"ejected",employee.ejected));abilities.text=stats.ToString();
        var details=new StringBuilder(LocalizationService.Text("staff.skills")+"\n");bool skillFound=false;
        foreach(var skill in options.skills)if(skill&&state.Overall(options)>=skill.requiredGrade){details.AppendLine(LocalizationService.Text("staff.skill.line","name",DataText(skill.nameKey),"description",DataText(skill.descriptionKey,"value",(skill.amount*100).ToString("0"))));skillFound=true;}
        if(!skillFound)details.AppendLine(LocalizationService.Text("staff.skills.none"));
        details.AppendLine("\n"+LocalizationService.Text("staff.flaws"));
        foreach(var flaw in state.definition.flaws)if(flaw!=null)details.AppendLine(DataText(flaw.descriptionKey,"value",(flaw.amount*100).ToString("0")));
        traits.text=details.ToString();
    }
    static string DataText(string key,params object[] data)=>LocalizationService.Text(string.IsNullOrWhiteSpace(key)?"staff.data.missing":key,data);
    static string GradeSummary(HexEmployeeState state,HexTestSettings.StaffOptions options)
    {
        var text=new StringBuilder();for(int i=0;i<4;i++)text.AppendLine(LocalizationService.Text("staff.grade.short","ability",LocalizationService.Text("staff.ability."+(HexStaffAbility)i),"grade",state.Grade((HexStaffAbility)i,options)));return text.ToString();
    }
}
