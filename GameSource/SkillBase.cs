using System;
using Duckov;
using ItemStatsSystem;
using UnityEngine;

// Token: 0x02000131 RID: 305
public abstract class SkillBase : MonoBehaviour
{
	// Token: 0x17000205 RID: 517
	// (get) Token: 0x060009E3 RID: 2531 RVA: 0x0002A7C5 File Offset: 0x000289C5
	public float LastReleaseTime
	{
		get
		{
			return this.lastReleaseTime;
		}
	}

	// Token: 0x17000206 RID: 518
	// (get) Token: 0x060009E4 RID: 2532 RVA: 0x0002A7CD File Offset: 0x000289CD
	public SkillContext SkillContext
	{
		get
		{
			return this.skillContext;
		}
	}

	// Token: 0x060009E5 RID: 2533 RVA: 0x0002A7D8 File Offset: 0x000289D8
	public void ReleaseSkill(SkillReleaseContext releaseContext, CharacterMainControl from)
	{
		this.lastReleaseTime = Time.time;
		this.skillReleaseContext = releaseContext;
		this.fromCharacter = from;
		this.fromCharacter.UseStamina(this.staminaCost);
		if (this.hasReleaseSound && this.fromCharacter != null && this.onReleaseSound != "")
		{
			AudioManager.Post(this.onReleaseSound, from.gameObject);
		}
		this.OnRelease();
		Action onSkillReleasedEvent = this.OnSkillReleasedEvent;
		if (onSkillReleasedEvent == null)
		{
			return;
		}
		onSkillReleasedEvent();
	}

	// Token: 0x060009E6 RID: 2534
	public abstract void OnRelease();

	// Token: 0x040008A8 RID: 2216
	public bool hasReleaseSound;

	// Token: 0x040008A9 RID: 2217
	public string onReleaseSound;

	// Token: 0x040008AA RID: 2218
	public Sprite icon;

	// Token: 0x040008AB RID: 2219
	public float staminaCost = 10f;

	// Token: 0x040008AC RID: 2220
	public float coolDownTime = 1f;

	// Token: 0x040008AD RID: 2221
	private float lastReleaseTime = -999f;

	// Token: 0x040008AE RID: 2222
	[SerializeField]
	protected SkillContext skillContext;

	// Token: 0x040008AF RID: 2223
	protected SkillReleaseContext skillReleaseContext;

	// Token: 0x040008B0 RID: 2224
	protected CharacterMainControl fromCharacter;

	// Token: 0x040008B1 RID: 2225
	[HideInInspector]
	public Item fromItem;

	// Token: 0x040008B2 RID: 2226
	public Action OnSkillReleasedEvent;
}
