using System;
using ItemStatsSystem;

// Token: 0x020000F6 RID: 246
public class ItemSetting_Skill : ItemSettingBase
{
	// Token: 0x0600080D RID: 2061 RVA: 0x00023F50 File Offset: 0x00022150
	public override void OnInit()
	{
		if (this.Skill)
		{
			SkillBase skill = this.Skill;
			skill.OnSkillReleasedEvent = (Action)Delegate.Combine(skill.OnSkillReleasedEvent, new Action(this.OnSkillReleased));
			this.Skill.fromItem = base.Item;
		}
	}

	// Token: 0x0600080E RID: 2062 RVA: 0x00023FA4 File Offset: 0x000221A4
	private void OnSkillReleased()
	{
		ItemSetting_Skill.OnReleaseAction onReleaseAction = this.onRelease;
		if (onReleaseAction != ItemSetting_Skill.OnReleaseAction.none && onReleaseAction == ItemSetting_Skill.OnReleaseAction.reduceCount && (!LevelManager.Instance || !LevelManager.Instance.IsBaseLevel))
		{
			if (base.Item.Stackable)
			{
				base.Item.StackCount--;
				return;
			}
			base.Item.Detach();
			base.Item.DestroyTree();
		}
	}

	// Token: 0x0600080F RID: 2063 RVA: 0x0002400E File Offset: 0x0002220E
	private void OnDestroy()
	{
		if (this.Skill)
		{
			SkillBase skill = this.Skill;
			skill.OnSkillReleasedEvent = (Action)Delegate.Remove(skill.OnSkillReleasedEvent, new Action(this.OnSkillReleased));
		}
	}

	// Token: 0x06000810 RID: 2064 RVA: 0x00024044 File Offset: 0x00022244
	public override void SetMarkerParam(Item selfItem)
	{
		selfItem.SetBool("IsSkill", true, true);
	}

	// Token: 0x04000778 RID: 1912
	public ItemSetting_Skill.OnReleaseAction onRelease;

	// Token: 0x04000779 RID: 1913
	public SkillBase Skill;

	// Token: 0x0200046F RID: 1135
	public enum OnReleaseAction
	{
		// Token: 0x04001B62 RID: 7010
		none,
		// Token: 0x04001B63 RID: 7011
		reduceCount
	}
}
