using System;
using Duckov.Utilities;
using UnityEngine;

// Token: 0x020000E8 RID: 232
public class ItemAgent_MeleeWeapon : DuckovItemAgent
{
	// Token: 0x17000194 RID: 404
	// (get) Token: 0x060007A9 RID: 1961 RVA: 0x0002283D File Offset: 0x00020A3D
	public float Damage
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_MeleeWeapon.DamageHash);
		}
	}

	// Token: 0x17000195 RID: 405
	// (get) Token: 0x060007AA RID: 1962 RVA: 0x0002284F File Offset: 0x00020A4F
	public float CritRate
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_MeleeWeapon.CritRateHash);
		}
	}

	// Token: 0x17000196 RID: 406
	// (get) Token: 0x060007AB RID: 1963 RVA: 0x00022861 File Offset: 0x00020A61
	public float CritDamageFactor
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_MeleeWeapon.CritDamageFactorHash);
		}
	}

	// Token: 0x17000197 RID: 407
	// (get) Token: 0x060007AC RID: 1964 RVA: 0x00022873 File Offset: 0x00020A73
	public float ArmorPiercing
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_MeleeWeapon.ArmorPiercingHash);
		}
	}

	// Token: 0x17000198 RID: 408
	// (get) Token: 0x060007AD RID: 1965 RVA: 0x00022885 File Offset: 0x00020A85
	public float AttackSpeed
	{
		get
		{
			return Mathf.Max(0.1f, base.Item.GetStatValue(ItemAgent_MeleeWeapon.AttackSpeedHash));
		}
	}

	// Token: 0x17000199 RID: 409
	// (get) Token: 0x060007AE RID: 1966 RVA: 0x000228A1 File Offset: 0x00020AA1
	public float AttackRange
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_MeleeWeapon.AttackRangeHash);
		}
	}

	// Token: 0x1700019A RID: 410
	// (get) Token: 0x060007AF RID: 1967 RVA: 0x000228B3 File Offset: 0x00020AB3
	public float DealDamageTime
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_MeleeWeapon.DealDamageTimeHash);
		}
	}

	// Token: 0x1700019B RID: 411
	// (get) Token: 0x060007B0 RID: 1968 RVA: 0x000228C5 File Offset: 0x00020AC5
	public float StaminaCost
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_MeleeWeapon.StaminaCostHash);
		}
	}

	// Token: 0x1700019C RID: 412
	// (get) Token: 0x060007B1 RID: 1969 RVA: 0x000228D7 File Offset: 0x00020AD7
	public float BleedChance
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_MeleeWeapon.BleedChanceHash);
		}
	}

	// Token: 0x1700019D RID: 413
	// (get) Token: 0x060007B2 RID: 1970 RVA: 0x000228E9 File Offset: 0x00020AE9
	public float MoveSpeedMultiplier
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_MeleeWeapon.MoveSpeedMultiplierHash);
		}
	}

	// Token: 0x1700019E RID: 414
	// (get) Token: 0x060007B3 RID: 1971 RVA: 0x000228FB File Offset: 0x00020AFB
	public float CharacterDamageMultiplier
	{
		get
		{
			if (!base.Holder)
			{
				return 1f;
			}
			return base.Holder.MeleeDamageMultiplier;
		}
	}

	// Token: 0x1700019F RID: 415
	// (get) Token: 0x060007B4 RID: 1972 RVA: 0x0002291B File Offset: 0x00020B1B
	public float CharacterCritRateGain
	{
		get
		{
			if (!base.Holder)
			{
				return 0f;
			}
			return base.Holder.MeleeCritRateGain;
		}
	}

	// Token: 0x170001A0 RID: 416
	// (get) Token: 0x060007B5 RID: 1973 RVA: 0x0002293B File Offset: 0x00020B3B
	public float CharacterCritDamageGain
	{
		get
		{
			if (!base.Holder)
			{
				return 0f;
			}
			return base.Holder.MeleeCritDamageGain;
		}
	}

	// Token: 0x170001A1 RID: 417
	// (get) Token: 0x060007B6 RID: 1974 RVA: 0x0002295B File Offset: 0x00020B5B
	public string SoundKey
	{
		get
		{
			if (string.IsNullOrWhiteSpace(this.soundKey))
			{
				return "Default";
			}
			return this.soundKey;
		}
	}

	// Token: 0x060007B7 RID: 1975 RVA: 0x00022978 File Offset: 0x00020B78
	private int UpdateColliders()
	{
		if (this.colliders == null)
		{
			this.colliders = new Collider[6];
		}
		return Physics.OverlapSphereNonAlloc(base.Holder.transform.position, this.AttackRange, this.colliders, GameplayDataSettings.Layers.damageReceiverLayerMask);
	}

	// Token: 0x060007B8 RID: 1976 RVA: 0x000229C9 File Offset: 0x00020BC9
	public void CheckAndDealDamage()
	{
		this.CheckCollidersInRange(true);
	}

	// Token: 0x060007B9 RID: 1977 RVA: 0x000229D3 File Offset: 0x00020BD3
	public bool AttackableTargetInRange()
	{
		return this.CheckCollidersInRange(false) > 0;
	}

	// Token: 0x060007BA RID: 1978 RVA: 0x000229E0 File Offset: 0x00020BE0
	private int CheckCollidersInRange(bool dealDamage)
	{
		if (this.colliders == null)
		{
			this.colliders = new Collider[6];
		}
		int num = this.UpdateColliders();
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			Collider collider = this.colliders[i];
			DamageReceiver component = collider.GetComponent<DamageReceiver>();
			if (!(component == null) && Team.IsEnemy(component.Team, base.Holder.Team))
			{
				Health health = component.health;
				if (health)
				{
					CharacterMainControl characterMainControl = health.TryGetCharacter();
					if (characterMainControl == base.Holder || (characterMainControl && characterMainControl.Dashing))
					{
						goto IL_2B3;
					}
				}
				Vector3 vector = collider.transform.position - base.Holder.transform.position;
				vector.y = 0f;
				vector.Normalize();
				if (Vector3.Angle(vector, base.Holder.CurrentAimDirection) < 90f)
				{
					num2++;
					if (dealDamage)
					{
						DamageInfo damageInfo = new DamageInfo(base.Holder);
						damageInfo.damageValue = this.Damage * this.CharacterDamageMultiplier;
						damageInfo.armorPiercing = this.ArmorPiercing;
						damageInfo.critDamageFactor = this.CritDamageFactor * (1f + this.CharacterCritDamageGain);
						damageInfo.critRate = this.CritRate * (1f + this.CharacterCritRateGain);
						damageInfo.crit = -1;
						damageInfo.damageNormal = -base.Holder.modelRoot.right;
						damageInfo.damagePoint = collider.transform.position - vector * 0.2f;
						damageInfo.damagePoint.y = base.transform.position.y;
						damageInfo.fromWeaponItemID = base.Item.TypeID;
						damageInfo.bleedChance = this.BleedChance;
						if (this.setting)
						{
							damageInfo.isExplosion = this.setting.dealExplosionDamage;
						}
						component.Hurt(damageInfo);
						component.AddBuff(GameplayDataSettings.Buffs.Pain, base.Holder);
						if (this.hitFx)
						{
							UnityEngine.Object.Instantiate<GameObject>(this.hitFx, damageInfo.damagePoint, Quaternion.LookRotation(damageInfo.damageNormal, Vector3.up));
						}
						if (base.Holder && base.Holder == CharacterMainControl.Main)
						{
							Vector3 a = base.Holder.modelRoot.right;
							a += UnityEngine.Random.insideUnitSphere * 0.3f;
							a.Normalize();
							CameraShaker.Shake(a * 0.05f, CameraShaker.CameraShakeTypes.meleeAttackHit);
						}
					}
				}
			}
			IL_2B3:;
		}
		return num2;
	}

	// Token: 0x060007BB RID: 1979 RVA: 0x00022CAC File Offset: 0x00020EAC
	private void Update()
	{
	}

	// Token: 0x060007BC RID: 1980 RVA: 0x00022CAE File Offset: 0x00020EAE
	protected override void OnInitialize()
	{
		base.OnInitialize();
		this.setting = base.Item.GetComponent<ItemSetting_MeleeWeapon>();
	}

	// Token: 0x0400073D RID: 1853
	public GameObject hitFx;

	// Token: 0x0400073E RID: 1854
	public GameObject slashFx;

	// Token: 0x0400073F RID: 1855
	public float slashFxDelayTime = 0.05f;

	// Token: 0x04000740 RID: 1856
	[SerializeField]
	private string soundKey = "Default";

	// Token: 0x04000741 RID: 1857
	private Collider[] colliders;

	// Token: 0x04000742 RID: 1858
	private ItemSetting_MeleeWeapon setting;

	// Token: 0x04000743 RID: 1859
	private static int DamageHash = "Damage".GetHashCode();

	// Token: 0x04000744 RID: 1860
	private static int CritRateHash = "CritRate".GetHashCode();

	// Token: 0x04000745 RID: 1861
	private static int CritDamageFactorHash = "CritDamageFactor".GetHashCode();

	// Token: 0x04000746 RID: 1862
	private static int ArmorPiercingHash = "ArmorPiercing".GetHashCode();

	// Token: 0x04000747 RID: 1863
	private static int AttackSpeedHash = "AttackSpeed".GetHashCode();

	// Token: 0x04000748 RID: 1864
	private static int AttackRangeHash = "AttackRange".GetHashCode();

	// Token: 0x04000749 RID: 1865
	private static int DealDamageTimeHash = "DealDamageTime".GetHashCode();

	// Token: 0x0400074A RID: 1866
	private static int StaminaCostHash = "StaminaCost".GetHashCode();

	// Token: 0x0400074B RID: 1867
	private static int BleedChanceHash = "BleedChance".GetHashCode();

	// Token: 0x0400074C RID: 1868
	private static int MoveSpeedMultiplierHash = "MoveSpeedMultiplier".GetHashCode();
}
