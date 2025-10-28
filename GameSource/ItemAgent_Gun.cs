using System;
using Duckov;
using Duckov.Utilities;
using FMOD.Studio;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using SodaCraft.Localizations;
using SodaCraft.StringUtilities;
using UnityEngine;

// Token: 0x020000E6 RID: 230
public class ItemAgent_Gun : DuckovItemAgent
{
	// Token: 0x1700014F RID: 335
	// (get) Token: 0x06000735 RID: 1845 RVA: 0x00020358 File Offset: 0x0001E558
	public Item BulletItem
	{
		get
		{
			if (this._bulletItem == null || this._bulletItem.ParentItem != base.Item)
			{
				foreach (Item item in base.Item.Inventory)
				{
					if (item != null)
					{
						this._bulletItem = item;
						break;
					}
				}
			}
			return this._bulletItem;
		}
	}

	// Token: 0x17000150 RID: 336
	// (get) Token: 0x06000736 RID: 1846 RVA: 0x000203E4 File Offset: 0x0001E5E4
	public float ShootSpeed
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.ShootSpeedHash);
		}
	}

	// Token: 0x17000151 RID: 337
	// (get) Token: 0x06000737 RID: 1847 RVA: 0x000203F6 File Offset: 0x0001E5F6
	public float ReloadTime
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.ReloadTimeHash) / (1f + this.CharacterReloadSpeedGain);
		}
	}

	// Token: 0x17000152 RID: 338
	// (get) Token: 0x06000738 RID: 1848 RVA: 0x00020415 File Offset: 0x0001E615
	public int Capacity
	{
		get
		{
			return Mathf.RoundToInt(base.Item.GetStatValue(ItemAgent_Gun.CapacityHash));
		}
	}

	// Token: 0x17000153 RID: 339
	// (get) Token: 0x06000739 RID: 1849 RVA: 0x0002042C File Offset: 0x0001E62C
	public float durabilityPercent
	{
		get
		{
			return this.Durability / this.MaxDurability;
		}
	}

	// Token: 0x17000154 RID: 340
	// (get) Token: 0x0600073A RID: 1850 RVA: 0x0002043B File Offset: 0x0001E63B
	public float Durability
	{
		get
		{
			return base.Item.Variables.GetFloat(ItemAgent_Gun.DurabilityHash, 0f);
		}
	}

	// Token: 0x17000155 RID: 341
	// (get) Token: 0x0600073B RID: 1851 RVA: 0x00020457 File Offset: 0x0001E657
	public float MaxDurability
	{
		get
		{
			if (this.maxDurability <= 0f)
			{
				this.maxDurability = base.Item.Constants.GetFloat("MaxDurability", 50f);
			}
			return this.maxDurability;
		}
	}

	// Token: 0x17000156 RID: 342
	// (get) Token: 0x0600073C RID: 1852 RVA: 0x0002048C File Offset: 0x0001E68C
	public float Damage
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.DamageHash);
		}
	}

	// Token: 0x17000157 RID: 343
	// (get) Token: 0x0600073D RID: 1853 RVA: 0x0002049E File Offset: 0x0001E69E
	public int BurstCount
	{
		get
		{
			return Mathf.Max(1, Mathf.RoundToInt(base.Item.GetStatValue(ItemAgent_Gun.BurstCountHash)));
		}
	}

	// Token: 0x17000158 RID: 344
	// (get) Token: 0x0600073E RID: 1854 RVA: 0x000204BB File Offset: 0x0001E6BB
	public float BulletSpeed
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.BulletSpeedHash);
		}
	}

	// Token: 0x17000159 RID: 345
	// (get) Token: 0x0600073F RID: 1855 RVA: 0x000204CD File Offset: 0x0001E6CD
	public float BulletDistance
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.BulletDistanceHash) * (base.Holder ? base.Holder.GunDistanceMultiplier : 1f);
		}
	}

	// Token: 0x1700015A RID: 346
	// (get) Token: 0x06000740 RID: 1856 RVA: 0x000204FF File Offset: 0x0001E6FF
	public int Penetrate
	{
		get
		{
			return Mathf.RoundToInt(base.Item.GetStatValue(ItemAgent_Gun.PenetrateHash));
		}
	}

	// Token: 0x1700015B RID: 347
	// (get) Token: 0x06000741 RID: 1857 RVA: 0x00020516 File Offset: 0x0001E716
	public float ExplosionDamageMultiplier
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.explosionDamageMultiplierHash);
		}
	}

	// Token: 0x1700015C RID: 348
	// (get) Token: 0x06000742 RID: 1858 RVA: 0x00020528 File Offset: 0x0001E728
	public float CritRate
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.CritRateHash);
		}
	}

	// Token: 0x1700015D RID: 349
	// (get) Token: 0x06000743 RID: 1859 RVA: 0x0002053A File Offset: 0x0001E73A
	public float CritDamageFactor
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.CritDamageFactorHash);
		}
	}

	// Token: 0x1700015E RID: 350
	// (get) Token: 0x06000744 RID: 1860 RVA: 0x0002054C File Offset: 0x0001E74C
	public float SoundRange
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.SoundRangeHash);
		}
	}

	// Token: 0x1700015F RID: 351
	// (get) Token: 0x06000745 RID: 1861 RVA: 0x00020560 File Offset: 0x0001E760
	public bool Silenced
	{
		get
		{
			Stat stat = base.Item.GetStat(ItemAgent_Gun.SoundRangeHash);
			return stat.Value < stat.BaseValue * 0.95f;
		}
	}

	// Token: 0x17000160 RID: 352
	// (get) Token: 0x06000746 RID: 1862 RVA: 0x00020592 File Offset: 0x0001E792
	public float ArmorPiercing
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.ArmorPiercingHash);
		}
	}

	// Token: 0x17000161 RID: 353
	// (get) Token: 0x06000747 RID: 1863 RVA: 0x000205A4 File Offset: 0x0001E7A4
	public float ArmorBreak
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.ArmorBreakHash);
		}
	}

	// Token: 0x17000162 RID: 354
	// (get) Token: 0x06000748 RID: 1864 RVA: 0x000205B6 File Offset: 0x0001E7B6
	public int ShotCount
	{
		get
		{
			return Mathf.RoundToInt(base.Item.GetStatValue(ItemAgent_Gun.ShotCountHash));
		}
	}

	// Token: 0x17000163 RID: 355
	// (get) Token: 0x06000749 RID: 1865 RVA: 0x000205CD File Offset: 0x0001E7CD
	public float ShotAngle
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.ShotAngleHash) * (this.IsInAds ? 0.5f : 1f);
		}
	}

	// Token: 0x17000164 RID: 356
	// (get) Token: 0x0600074A RID: 1866 RVA: 0x000205F4 File Offset: 0x0001E7F4
	public float ADSAimDistanceFactor
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.ADSAimDistanceFactorHash);
		}
	}

	// Token: 0x17000165 RID: 357
	// (get) Token: 0x0600074B RID: 1867 RVA: 0x00020606 File Offset: 0x0001E806
	public float AdsSpeed
	{
		get
		{
			return 1f / base.Item.GetStatValue(ItemAgent_Gun.AdsTimeHash);
		}
	}

	// Token: 0x17000166 RID: 358
	// (get) Token: 0x0600074C RID: 1868 RVA: 0x00020620 File Offset: 0x0001E820
	public float DefaultScatter
	{
		get
		{
			float a = base.Item.GetStatValue(ItemAgent_Gun.DefaultScatterHash) * this.scatterFactorHips;
			float b = base.Item.GetStatValue(ItemAgent_Gun.DefaultScatterHashADS) * this.scatterFactorAds;
			return Mathf.Lerp(a, b, this.adsValue);
		}
	}

	// Token: 0x17000167 RID: 359
	// (get) Token: 0x0600074D RID: 1869 RVA: 0x00020668 File Offset: 0x0001E868
	public float MaxScatter
	{
		get
		{
			float a = base.Item.GetStatValue(ItemAgent_Gun.MaxScatterHash) * this.scatterFactorHips;
			float b = base.Item.GetStatValue(ItemAgent_Gun.MaxScatterHashADS) * this.scatterFactorAds;
			return Mathf.Lerp(a, b, this.adsValue);
		}
	}

	// Token: 0x17000168 RID: 360
	// (get) Token: 0x0600074E RID: 1870 RVA: 0x000206B0 File Offset: 0x0001E8B0
	public float ScatterGrow
	{
		get
		{
			float a = base.Item.GetStatValue(ItemAgent_Gun.ScatterGrowHash) * this.scatterFactorHips;
			float b = base.Item.GetStatValue(ItemAgent_Gun.ScatterGrowHashADS) * this.scatterFactorAds;
			return Mathf.Lerp(a, b, this.adsValue);
		}
	}

	// Token: 0x17000169 RID: 361
	// (get) Token: 0x0600074F RID: 1871 RVA: 0x000206F8 File Offset: 0x0001E8F8
	public float ScatterRecover
	{
		get
		{
			float statValue = base.Item.GetStatValue(ItemAgent_Gun.ScatterRecoverHash);
			float statValue2 = base.Item.GetStatValue(ItemAgent_Gun.ScatterRecoverHashADS);
			return Mathf.Lerp(statValue, statValue2, this.adsValue) * this.ScatterGrow * this.ShootSpeed;
		}
	}

	// Token: 0x1700016A RID: 362
	// (get) Token: 0x06000750 RID: 1872 RVA: 0x00020740 File Offset: 0x0001E940
	public float RecoilVMin
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.RecoilVMinHash);
		}
	}

	// Token: 0x1700016B RID: 363
	// (get) Token: 0x06000751 RID: 1873 RVA: 0x00020752 File Offset: 0x0001E952
	public float RecoilVMax
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.RecoilVMaxHash);
		}
	}

	// Token: 0x1700016C RID: 364
	// (get) Token: 0x06000752 RID: 1874 RVA: 0x00020764 File Offset: 0x0001E964
	public float RecoilHMin
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.RecoilHMinHash);
		}
	}

	// Token: 0x1700016D RID: 365
	// (get) Token: 0x06000753 RID: 1875 RVA: 0x00020776 File Offset: 0x0001E976
	public float RecoilHMax
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.RecoilHMaxHash);
		}
	}

	// Token: 0x1700016E RID: 366
	// (get) Token: 0x06000754 RID: 1876 RVA: 0x00020788 File Offset: 0x0001E988
	public float RecoilScaleV
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.RecoilScaleVHash);
		}
	}

	// Token: 0x1700016F RID: 367
	// (get) Token: 0x06000755 RID: 1877 RVA: 0x0002079A File Offset: 0x0001E99A
	public float RecoilScaleH
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.RecoilScaleHHash);
		}
	}

	// Token: 0x17000170 RID: 368
	// (get) Token: 0x06000756 RID: 1878 RVA: 0x000207AC File Offset: 0x0001E9AC
	public float RecoilRecover
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.RecoilRecoverHash);
		}
	}

	// Token: 0x17000171 RID: 369
	// (get) Token: 0x06000757 RID: 1879 RVA: 0x000207BE File Offset: 0x0001E9BE
	public float RecoilTime
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.RecoilTimeHash);
		}
	}

	// Token: 0x17000172 RID: 370
	// (get) Token: 0x06000758 RID: 1880 RVA: 0x000207D0 File Offset: 0x0001E9D0
	public float RecoilRecoverTime
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.RecoilRecoverTimeHash);
		}
	}

	// Token: 0x17000173 RID: 371
	// (get) Token: 0x06000759 RID: 1881 RVA: 0x000207E2 File Offset: 0x0001E9E2
	public float MoveSpeedMultiplier
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.MoveSpeedMultiplierHash);
		}
	}

	// Token: 0x17000174 RID: 372
	// (get) Token: 0x0600075A RID: 1882 RVA: 0x000207F4 File Offset: 0x0001E9F4
	public float AdsWalkSpeedMultiplier
	{
		get
		{
			return Mathf.Min(1f, base.Item.GetStatValue(ItemAgent_Gun.AdsWalkSpeedMultiplierHash));
		}
	}

	// Token: 0x17000175 RID: 373
	// (get) Token: 0x0600075B RID: 1883 RVA: 0x00020810 File Offset: 0x0001EA10
	public float burstCoolTime
	{
		get
		{
			return 1f / this.ShootSpeed * ((float)(3 * this.BurstCount) / ((float)this.BurstCount + 2f));
		}
	}

	// Token: 0x17000176 RID: 374
	// (get) Token: 0x0600075C RID: 1884 RVA: 0x00020836 File Offset: 0x0001EA36
	public float burstShotTimeSpace
	{
		get
		{
			return 1f / this.ShootSpeed * ((float)this.BurstCount / ((float)this.BurstCount + 2f));
		}
	}

	// Token: 0x17000177 RID: 375
	// (get) Token: 0x0600075D RID: 1885 RVA: 0x0002085A File Offset: 0x0001EA5A
	public float BuffChance
	{
		get
		{
			return base.Item.GetStatValue(ItemAgent_Gun.BuffChanceHash);
		}
	}

	// Token: 0x17000178 RID: 376
	// (get) Token: 0x0600075E RID: 1886 RVA: 0x0002086C File Offset: 0x0001EA6C
	public float bulletCritRateGain
	{
		get
		{
			if (this.BulletItem)
			{
				return this.BulletItem.Constants.GetFloat(ItemAgent_Gun.bulletCritRateGainHash, 0f);
			}
			return 0f;
		}
	}

	// Token: 0x17000179 RID: 377
	// (get) Token: 0x0600075F RID: 1887 RVA: 0x0002089B File Offset: 0x0001EA9B
	public float BulletCritDamageFactorGain
	{
		get
		{
			if (this.BulletItem)
			{
				return this.BulletItem.Constants.GetFloat(ItemAgent_Gun.bulletCritDamageFactorGainHash, 0f);
			}
			return 0f;
		}
	}

	// Token: 0x1700017A RID: 378
	// (get) Token: 0x06000760 RID: 1888 RVA: 0x000208CA File Offset: 0x0001EACA
	public float BulletArmorPiercingGain
	{
		get
		{
			if (this.BulletItem)
			{
				return this.BulletItem.Constants.GetFloat(ItemAgent_Gun.bulletArmorPiercingGainHash, 0f);
			}
			return 0f;
		}
	}

	// Token: 0x1700017B RID: 379
	// (get) Token: 0x06000761 RID: 1889 RVA: 0x000208F9 File Offset: 0x0001EAF9
	public float BulletDamageMultiplier
	{
		get
		{
			if (this.BulletItem)
			{
				return this.BulletItem.Constants.GetFloat(ItemAgent_Gun.BulletDamageMultiplierHash, 0f);
			}
			return 0f;
		}
	}

	// Token: 0x1700017C RID: 380
	// (get) Token: 0x06000762 RID: 1890 RVA: 0x00020928 File Offset: 0x0001EB28
	public float BulletExplosionRange
	{
		get
		{
			if (this.BulletItem)
			{
				return this.BulletItem.Constants.GetFloat(ItemAgent_Gun.bulletExplosionRangeHash, 0f);
			}
			return 0f;
		}
	}

	// Token: 0x1700017D RID: 381
	// (get) Token: 0x06000763 RID: 1891 RVA: 0x00020957 File Offset: 0x0001EB57
	public float BulletBuffChanceMultiplier
	{
		get
		{
			if (this.BulletItem)
			{
				return this.BulletItem.Constants.GetFloat(ItemAgent_Gun.BulletBuffChanceMultiplierHash, 0f);
			}
			return 0f;
		}
	}

	// Token: 0x1700017E RID: 382
	// (get) Token: 0x06000764 RID: 1892 RVA: 0x00020986 File Offset: 0x0001EB86
	public float BulletBleedChance
	{
		get
		{
			if (this.BulletItem)
			{
				return this.BulletItem.Constants.GetFloat(ItemAgent_Gun.BulletBleedChanceHash, 0f);
			}
			return 0f;
		}
	}

	// Token: 0x1700017F RID: 383
	// (get) Token: 0x06000765 RID: 1893 RVA: 0x000209B5 File Offset: 0x0001EBB5
	public float BulletExplosionDamage
	{
		get
		{
			if (this.BulletItem)
			{
				return this.BulletItem.Constants.GetFloat(ItemAgent_Gun.bulletExplosionDamageHash, 0f);
			}
			return 0f;
		}
	}

	// Token: 0x17000180 RID: 384
	// (get) Token: 0x06000766 RID: 1894 RVA: 0x000209E4 File Offset: 0x0001EBE4
	public float BulletArmorBreakGain
	{
		get
		{
			if (this.BulletItem)
			{
				return this.BulletItem.Constants.GetFloat(ItemAgent_Gun.armorBreakGainHash, 0f);
			}
			return 0f;
		}
	}

	// Token: 0x17000181 RID: 385
	// (get) Token: 0x06000767 RID: 1895 RVA: 0x00020A13 File Offset: 0x0001EC13
	public float bulletDurabilityCost
	{
		get
		{
			if (this.BulletItem)
			{
				return this.BulletItem.Constants.GetFloat(ItemAgent_Gun.bulletDurabilityCostHash, 0f);
			}
			return 0f;
		}
	}

	// Token: 0x17000182 RID: 386
	// (get) Token: 0x06000768 RID: 1896 RVA: 0x00020A42 File Offset: 0x0001EC42
	public float CharacterDamageMultiplier
	{
		get
		{
			if (!base.Holder)
			{
				return 0f;
			}
			return base.Holder.GunDamageMultiplier;
		}
	}

	// Token: 0x17000183 RID: 387
	// (get) Token: 0x06000769 RID: 1897 RVA: 0x00020A62 File Offset: 0x0001EC62
	public float CharacterReloadSpeedGain
	{
		get
		{
			if (!base.Holder)
			{
				return 0f;
			}
			return base.Holder.ReloadSpeedGain;
		}
	}

	// Token: 0x17000184 RID: 388
	// (get) Token: 0x0600076A RID: 1898 RVA: 0x00020A82 File Offset: 0x0001EC82
	public float CharacterGunCritRateGain
	{
		get
		{
			if (!base.Holder)
			{
				return 0f;
			}
			return base.Holder.GunCritRateGain;
		}
	}

	// Token: 0x17000185 RID: 389
	// (get) Token: 0x0600076B RID: 1899 RVA: 0x00020AA2 File Offset: 0x0001ECA2
	public float CharacterGunCritDamageGain
	{
		get
		{
			if (!base.Holder)
			{
				return 0f;
			}
			return base.Holder.GunCritDamageGain;
		}
	}

	// Token: 0x17000186 RID: 390
	// (get) Token: 0x0600076C RID: 1900 RVA: 0x00020AC2 File Offset: 0x0001ECC2
	public float CharacterRecoilControl
	{
		get
		{
			if (!base.Holder)
			{
				return 1f;
			}
			return base.Holder.RecoilControl;
		}
	}

	// Token: 0x17000187 RID: 391
	// (get) Token: 0x0600076D RID: 1901 RVA: 0x00020AE2 File Offset: 0x0001ECE2
	public float CharacterScatterMultiplier
	{
		get
		{
			if (!base.Holder)
			{
				return 1f;
			}
			return Mathf.Max(0.1f, base.Holder.GunScatterMultiplier);
		}
	}

	// Token: 0x17000188 RID: 392
	// (get) Token: 0x0600076E RID: 1902 RVA: 0x00020B0C File Offset: 0x0001ED0C
	public int BulletCount
	{
		get
		{
			if (!this.GunItemSetting)
			{
				return 0;
			}
			return this.GunItemSetting.BulletCount;
		}
	}

	// Token: 0x17000189 RID: 393
	// (get) Token: 0x0600076F RID: 1903 RVA: 0x00020B28 File Offset: 0x0001ED28
	public float AdsValue
	{
		get
		{
			return this.adsValue;
		}
	}

	// Token: 0x1700018A RID: 394
	// (get) Token: 0x06000770 RID: 1904 RVA: 0x00020B30 File Offset: 0x0001ED30
	public Transform muzzle
	{
		get
		{
			if (this.muzzleIndex != 0 && this.muzzle2 != null)
			{
				return this.muzzle2;
			}
			return this.muzzle1;
		}
	}

	// Token: 0x1700018B RID: 395
	// (get) Token: 0x06000771 RID: 1905 RVA: 0x00020B55 File Offset: 0x0001ED55
	private Transform muzzle1
	{
		get
		{
			if (this._mz1 == null)
			{
				this._mz1 = base.GetSocket("Muzzle", true);
			}
			return this._mz1;
		}
	}

	// Token: 0x1700018C RID: 396
	// (get) Token: 0x06000772 RID: 1906 RVA: 0x00020B80 File Offset: 0x0001ED80
	private Transform muzzle2
	{
		get
		{
			if (this._mz2 == null && this.hasMz2)
			{
				this._mz2 = base.GetSocket("Muzzle2", false);
				if (this._mz2 == null)
				{
					this.hasMz2 = false;
				}
			}
			return this._mz2;
		}
	}

	// Token: 0x1700018D RID: 397
	// (get) Token: 0x06000773 RID: 1907 RVA: 0x00020BD0 File Offset: 0x0001EDD0
	private GameObject muzzleFxPfb
	{
		get
		{
			return this.GunItemSetting.muzzleFxPfb;
		}
	}

	// Token: 0x1700018E RID: 398
	// (get) Token: 0x06000774 RID: 1908 RVA: 0x00020BDD File Offset: 0x0001EDDD
	public ItemSetting_Gun GunItemSetting
	{
		get
		{
			if (!this._gunItemSetting && base.Item)
			{
				this._gunItemSetting = base.Item.GetComponent<ItemSetting_Gun>();
			}
			return this._gunItemSetting;
		}
	}

	// Token: 0x1700018F RID: 399
	// (get) Token: 0x06000775 RID: 1909 RVA: 0x00020C10 File Offset: 0x0001EE10
	public bool IsInAds
	{
		get
		{
			return base.Holder && base.Holder.IsInAdsInput;
		}
	}

	// Token: 0x17000190 RID: 400
	// (get) Token: 0x06000776 RID: 1910 RVA: 0x00020C2C File Offset: 0x0001EE2C
	public float CurrentScatter
	{
		get
		{
			return this.scatterBeforeControl * this.CharacterScatterMultiplier;
		}
	}

	// Token: 0x17000191 RID: 401
	// (get) Token: 0x06000777 RID: 1911 RVA: 0x00020C3B File Offset: 0x0001EE3B
	public float MinScatter
	{
		get
		{
			return this.DefaultScatter;
		}
	}

	// Token: 0x1400002F RID: 47
	// (add) Token: 0x06000778 RID: 1912 RVA: 0x00020C44 File Offset: 0x0001EE44
	// (remove) Token: 0x06000779 RID: 1913 RVA: 0x00020C78 File Offset: 0x0001EE78
	public static event Action<ItemAgent_Gun> OnMainCharacterShootEvent;

	// Token: 0x14000030 RID: 48
	// (add) Token: 0x0600077A RID: 1914 RVA: 0x00020CAC File Offset: 0x0001EEAC
	// (remove) Token: 0x0600077B RID: 1915 RVA: 0x00020CE4 File Offset: 0x0001EEE4
	public event Action OnShootEvent;

	// Token: 0x14000031 RID: 49
	// (add) Token: 0x0600077C RID: 1916 RVA: 0x00020D1C File Offset: 0x0001EF1C
	// (remove) Token: 0x0600077D RID: 1917 RVA: 0x00020D54 File Offset: 0x0001EF54
	public event Action OnLoadedEvent;

	// Token: 0x17000192 RID: 402
	// (get) Token: 0x0600077E RID: 1918 RVA: 0x00020D89 File Offset: 0x0001EF89
	public float StateTimer
	{
		get
		{
			return this.stateTimer;
		}
	}

	// Token: 0x17000193 RID: 403
	// (get) Token: 0x0600077F RID: 1919 RVA: 0x00020D91 File Offset: 0x0001EF91
	public ItemAgent_Gun.GunStates GunState
	{
		get
		{
			return this.gunState;
		}
	}

	// Token: 0x06000780 RID: 1920 RVA: 0x00020D99 File Offset: 0x0001EF99
	private void Update()
	{
		this.UpdateGun();
		this.UpdateScatterFactor();
		this.triggerInput = false;
		this.triggerThisFrame = false;
		this.releaseThisFrame = false;
	}

	// Token: 0x06000781 RID: 1921 RVA: 0x00020DBC File Offset: 0x0001EFBC
	protected override void OnDestroy()
	{
		base.OnDestroy();
		this.StopReloadSound();
	}

	// Token: 0x06000782 RID: 1922 RVA: 0x00020DCA File Offset: 0x0001EFCA
	private void UpdateScatterFactor()
	{
		this.scatterFactorHips = base.Item.GetStatValue(ItemAgent_Gun.ScatterFactorHash);
		this.scatterFactorAds = base.Item.GetStatValue(ItemAgent_Gun.ScatterFactorHashADS);
	}

	// Token: 0x06000783 RID: 1923 RVA: 0x00020DF8 File Offset: 0x0001EFF8
	private void UpdateGun()
	{
		float maxScatter = this.MaxScatter;
		if (this.scatterBeforeControl > maxScatter)
		{
			this.scatterBeforeControl = maxScatter;
		}
		this.scatterBeforeControl = Mathf.MoveTowards(this.scatterBeforeControl, this.DefaultScatter, this.ScatterRecover * Time.deltaTime * ((this.scatterBeforeControl < this.DefaultScatter) ? 6f : 1f));
		this.UpdateStates();
		this.UpdateAds();
		this.UpdateVisualRecoil();
	}

	// Token: 0x06000784 RID: 1924 RVA: 0x00020E6C File Offset: 0x0001F06C
	protected override void OnInitialize()
	{
		base.OnInitialize();
		if (this.GunItemSetting)
		{
			if (base.Holder && base.Holder.CharacterItem && base.Holder.CharacterItem.Inventory)
			{
				this.GunItemSetting.AutoSetTypeInInventory(base.Holder.CharacterItem.Inventory);
			}
			else
			{
				this.GunItemSetting.AutoSetTypeInInventory(null);
			}
			this.scatterBeforeControl = this.DefaultScatter;
			if (this.loadedVisualObject != null)
			{
				this.loadedVisualObject.SetActive(this.GunItemSetting.BulletCount > 0);
			}
		}
	}

	// Token: 0x06000785 RID: 1925 RVA: 0x00020F24 File Offset: 0x0001F124
	public void UpdateStates()
	{
		if (this.GunItemSetting.LoadingBullets)
		{
			return;
		}
		if (this.triggerThisFrame && this.ShootSpeed >= 5f)
		{
			this.triggerBuffer = true;
			this.triggerThisFrame = false;
		}
		switch (this.gunState)
		{
		case ItemAgent_Gun.GunStates.shootCooling:
			this.stateTimer += Time.deltaTime;
			if (this.stateTimer >= this.burstCoolTime)
			{
				this.TransToReady();
				return;
			}
			break;
		case ItemAgent_Gun.GunStates.ready:
		{
			bool flag = false;
			if (this.BulletCount <= 0)
			{
				this.TransToEmpty();
			}
			else if (this.GunItemSetting.triggerMode == ItemSetting_Gun.TriggerModes.auto)
			{
				if (this.triggerInput)
				{
					flag = true;
				}
			}
			else if ((this.GunItemSetting.triggerMode == ItemSetting_Gun.TriggerModes.semi || this.GunItemSetting.triggerMode == ItemSetting_Gun.TriggerModes.bolt) && (this.triggerBuffer || this.triggerThisFrame))
			{
				this.triggerThisFrame = false;
				this.triggerBuffer = false;
				flag = true;
			}
			if (flag)
			{
				this.TransToFire(this.triggerThisFrame);
				return;
			}
			if (this.needAutoReload)
			{
				this.needAutoReload = false;
				this.CharacterReload(null);
				return;
			}
			break;
		}
		case ItemAgent_Gun.GunStates.fire:
			this.triggerBuffer = false;
			if (this.BulletCount <= 0)
			{
				this.muzzleIndex = ((this.muzzleIndex == 0) ? 1 : 0);
				this.TransToEmpty();
				return;
			}
			if (this.burstCounter >= this.BurstCount)
			{
				this.muzzleIndex = ((this.muzzleIndex == 0) ? 1 : 0);
				this.TransToBurstCooling();
				return;
			}
			this.TransToBurstEachShotCooling();
			return;
		case ItemAgent_Gun.GunStates.burstEachShotCooling:
			this.stateTimer += Time.deltaTime;
			if (this.stateTimer >= this.burstShotTimeSpace)
			{
				this.TransToFire(false);
				return;
			}
			break;
		case ItemAgent_Gun.GunStates.empty:
			if (this.needAutoReload)
			{
				this.needAutoReload = false;
				this.CharacterReload(null);
				return;
			}
			if ((this.triggerThisFrame || this.triggerBuffer) && base.Holder != null)
			{
				this.triggerThisFrame = false;
				this.triggerBuffer = false;
				base.Holder.TryToReload(null);
				return;
			}
			break;
		case ItemAgent_Gun.GunStates.reloading:
			this.triggerBuffer = false;
			this.stateTimer += Time.deltaTime;
			if (this.stateTimer < this.ReloadTime)
			{
				this.loadBulletsStarted = false;
				return;
			}
			if (!this.loadBulletsStarted)
			{
				this.loadBulletsStarted = true;
				this.StartLoadBullets();
				return;
			}
			if (!this.GunItemSetting.LoadingBullets)
			{
				if (this.GunItemSetting.LoadBulletsSuccess)
				{
					this.PostReloadSuccessSound();
				}
				this.needAutoReload = (this.GunItemSetting.reloadMode == ItemSetting_Gun.ReloadModes.singleBullet && !this.GunItemSetting.IsFull());
				this.loadBulletsStarted = false;
				if (this.GunItemSetting.BulletCount > 0 && this.loadedVisualObject != null)
				{
					this.loadedVisualObject.SetActive(true);
				}
				Action onLoadedEvent = this.OnLoadedEvent;
				if (onLoadedEvent != null)
				{
					onLoadedEvent();
				}
				this.TransToReady();
			}
			break;
		default:
			return;
		}
	}

	// Token: 0x06000786 RID: 1926 RVA: 0x000211F0 File Offset: 0x0001F3F0
	private void UpdateAds()
	{
		float num = 0f;
		if (base.Holder && base.Holder.IsInAdsInput)
		{
			num = 1f;
		}
		float num2 = this.AdsSpeed;
		if (num == 0f)
		{
			num2 = Mathf.Max(num2, 4f);
		}
		this.adsValue = Mathf.MoveTowards(this.adsValue, num, Time.deltaTime * num2);
	}

	// Token: 0x06000787 RID: 1927 RVA: 0x00021257 File Offset: 0x0001F457
	private void TransToBurstCooling()
	{
		this.gunState = ItemAgent_Gun.GunStates.shootCooling;
		this.burstCounter = 0;
		this.stateTimer = 0f;
	}

	// Token: 0x06000788 RID: 1928 RVA: 0x00021272 File Offset: 0x0001F472
	private void TransToReady()
	{
		this.gunState = ItemAgent_Gun.GunStates.ready;
		this.burstCounter = 0;
	}

	// Token: 0x06000789 RID: 1929 RVA: 0x00021284 File Offset: 0x0001F484
	private void TransToFire(bool isFirstShot)
	{
		if (this.BulletCount <= 0)
		{
			return;
		}
		if (base.Item.Durability <= 0f)
		{
			return;
		}
		this.gunState = ItemAgent_Gun.GunStates.fire;
		Vector3 vector = this.muzzle.forward;
		if (base.Holder && base.Holder.CharacterMoveability > 0.5f)
		{
			Vector3 currentAimPoint = base.Holder.GetCurrentAimPoint();
			currentAimPoint.y = 0f;
			Vector3 position = base.Holder.transform.position;
			position.y = 0f;
			Vector3 position2 = this.muzzle.position;
			position2.y = 0f;
			if (Vector3.Distance(position, currentAimPoint) > Vector3.Distance(position, position2) + 0.1f)
			{
				vector = base.Holder.GetCurrentAimPoint() - this.muzzle.position;
				vector.Normalize();
			}
		}
		for (int i = 0; i < this.ShotCount; i++)
		{
			Vector3 vector2 = vector;
			float num = this.ShotAngle;
			bool flag = num > 359f;
			if (flag)
			{
				num -= num / (float)this.ShotCount;
			}
			float num2 = -num * 0.5f;
			float num3 = num / ((float)this.ShotCount - 1f);
			if ((float)this.ShotCount % 2f < 0.01f && flag)
			{
				num2 -= num3 * 0.5f;
			}
			if (this.ShotCount > 1)
			{
				vector2 = Quaternion.Euler(0f, num2 + (float)i * num3, 0f) * vector;
			}
			Vector3 localPosition = this.muzzle.localPosition;
			localPosition.y = 0f;
			float magnitude = localPosition.magnitude;
			this.ShootOneBullet(this.muzzle.position, vector2, this.muzzle.position - magnitude * vector2);
			if (base.Holder != null)
			{
				AIMainBrain.MakeSound(new AISound
				{
					fromCharacter = base.Holder,
					fromObject = base.gameObject,
					pos = this.muzzle.position,
					soundType = SoundTypes.combatSound,
					fromTeam = base.Holder.Team,
					radius = this.SoundRange
				});
			}
		}
		this.PostShootSound();
		this.scatterBeforeControl = Mathf.Clamp(this.scatterBeforeControl + this.ScatterGrow, this.DefaultScatter, this.MaxScatter);
		this.AimRecoil(vector);
		if (base.Holder == LevelManager.Instance.MainCharacter)
		{
			LevelManager.Instance.InputManager.AddRecoil(this);
		}
		this.StartVisualRecoil();
		this.GunItemSetting.UseABullet();
		base.Holder.TriggerShootEvent(this);
		Action onShootEvent = this.OnShootEvent;
		if (onShootEvent != null)
		{
			onShootEvent();
		}
		if (this.BulletCount <= 0 && this.GunItemSetting.autoReload)
		{
			this.needAutoReload = true;
		}
		if (this.GunItemSetting.BulletCount <= 0 && this.loadedVisualObject != null)
		{
			this.loadedVisualObject.SetActive(false);
		}
		if (base.Holder && base.Holder.IsMainCharacter && LevelManager.Instance.IsRaidMap)
		{
			base.Item.Durability = Mathf.Max(0f, base.Item.Durability - this.bulletDurabilityCost);
		}
		if (this.muzzleFxPfb)
		{
			UnityEngine.Object.Instantiate<GameObject>(this.muzzleFxPfb, this.muzzle.position, this.muzzle.rotation).transform.SetParent(this.muzzle);
		}
		if (this.shellParticle)
		{
			this.shellParticle.Emit(1);
		}
		this.burstCounter++;
		if (base.Holder && base.Holder.IsMainCharacter)
		{
			CameraShaker.Shake(-this.muzzle.forward * 0.07f, CameraShaker.CameraShakeTypes.recoil);
			Action<ItemAgent_Gun> onMainCharacterShootEvent = ItemAgent_Gun.OnMainCharacterShootEvent;
			if (onMainCharacterShootEvent == null)
			{
				return;
			}
			onMainCharacterShootEvent(this);
		}
	}

	// Token: 0x0600078A RID: 1930 RVA: 0x000216A1 File Offset: 0x0001F8A1
	private void TransToBurstEachShotCooling()
	{
		this.gunState = ItemAgent_Gun.GunStates.burstEachShotCooling;
		this.stateTimer = 0f;
	}

	// Token: 0x0600078B RID: 1931 RVA: 0x000216B5 File Offset: 0x0001F8B5
	private void TransToEmpty()
	{
		this.gunState = ItemAgent_Gun.GunStates.empty;
	}

	// Token: 0x0600078C RID: 1932 RVA: 0x000216C0 File Offset: 0x0001F8C0
	private void ShootOneBullet(Vector3 _muzzlePoint, Vector3 _shootDirection, Vector3 firstFrameCheckStartPoint)
	{
		bool flag = false;
		if (this.GunItemSetting.LoadingBullets)
		{
			return;
		}
		if (!this.BulletItem)
		{
			return;
		}
		if (base.Holder && base.Holder.IsMainCharacter)
		{
			flag = true;
		}
		ItemSetting_Bullet component = this.BulletItem.GetComponent<ItemSetting_Bullet>();
		float num = 0f;
		if (flag)
		{
			num = Mathf.Max(1f, this.CurrentScatter) * Mathf.Lerp(1.5f, 0f, Mathf.InverseLerp(0f, 0.5f, this.durabilityPercent));
		}
		float y = UnityEngine.Random.Range(-0.5f, 0.5f) * (this.CurrentScatter + num);
		_shootDirection = Quaternion.Euler(0f, y, 0f) * _shootDirection;
		_shootDirection.Normalize();
		Projectile projectile = this._gunItemSetting.bulletPfb;
		if (projectile == null)
		{
			projectile = GameplayDataSettings.Prefabs.DefaultBullet;
		}
		this.projInst = LevelManager.Instance.BulletPool.GetABullet(projectile);
		this.projInst.transform.position = _muzzlePoint;
		this.projInst.transform.rotation = Quaternion.LookRotation(_shootDirection, Vector3.up);
		ProjectileContext projectileContext = default(ProjectileContext);
		projectileContext.firstFrameCheck = true;
		projectileContext.firstFrameCheckStartPoint = firstFrameCheckStartPoint;
		projectileContext.direction = _shootDirection.normalized;
		projectileContext.speed = this.BulletSpeed;
		if (base.Holder)
		{
			projectileContext.team = base.Holder.Team;
			projectileContext.speed *= base.Holder.GunBulletSpeedMultiplier;
		}
		projectileContext.distance = this.BulletDistance + 0.4f;
		projectileContext.halfDamageDistance = projectileContext.distance * 0.5f;
		if (!flag)
		{
			projectileContext.distance *= 1.05f;
		}
		projectileContext.penetrate = this.Penetrate;
		float characterDamageMultiplier = this.CharacterDamageMultiplier;
		float num2 = 1f;
		projectileContext.damage = this.Damage * this.BulletDamageMultiplier * num2 * characterDamageMultiplier / (float)this.ShotCount;
		if (this.Damage > 1f && projectileContext.damage < 1f)
		{
			projectileContext.damage = 1f;
		}
		projectileContext.critDamageFactor = (this.CritDamageFactor + this.BulletCritDamageFactorGain) * (1f + this.CharacterGunCritDamageGain);
		projectileContext.critRate = this.CritRate * (1f + this.CharacterGunCritRateGain + this.bulletCritRateGain);
		if (flag)
		{
			projectileContext.critRate = (LevelManager.Instance.InputManager.AimingEnemyHead ? 1f : 0f);
		}
		projectileContext.armorPiercing = this.ArmorPiercing + this.BulletArmorPiercingGain;
		projectileContext.armorBreak = this.ArmorBreak + this.BulletArmorBreakGain;
		projectileContext.fromCharacter = base.Holder;
		projectileContext.explosionRange = this.BulletExplosionRange;
		projectileContext.explosionDamage = this.BulletExplosionDamage * this.ExplosionDamageMultiplier;
		switch (this._gunItemSetting.element)
		{
		case ElementTypes.physics:
			projectileContext.element_Physics = 1f;
			break;
		case ElementTypes.fire:
			projectileContext.element_Fire = 1f;
			break;
		case ElementTypes.poison:
			projectileContext.element_Poison = 1f;
			break;
		case ElementTypes.electricity:
			projectileContext.element_Electricity = 1f;
			break;
		case ElementTypes.space:
			projectileContext.element_Space = 1f;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		projectileContext.fromWeaponItemID = base.Item.TypeID;
		projectileContext.buff = this._gunItemSetting.buff;
		if (component)
		{
			projectileContext.buffChance = this.BulletBuffChanceMultiplier * this.BuffChance;
		}
		projectileContext.bleedChance = this.BulletBleedChance;
		if (base.Holder)
		{
			if (flag)
			{
				if (base.Holder.HasNearByHalfObsticle())
				{
					projectileContext.ignoreHalfObsticle = true;
				}
			}
			else
			{
				this.projInst.damagedObjects.AddRange(base.Holder.GetNearByHalfObsticles());
			}
		}
		if (projectileContext.critRate > 0.99f)
		{
			projectileContext.ignoreHalfObsticle = true;
		}
		this.projInst.Init(projectileContext);
	}

	// Token: 0x0600078D RID: 1933 RVA: 0x00021AE0 File Offset: 0x0001FCE0
	private void AimRecoil(Vector3 shootDir)
	{
		if (!base.Holder || !(base.Holder == CharacterMainControl.Main))
		{
			return;
		}
		Vector3 vector = base.Holder.CurrentAimDirection;
		vector.y = 0f;
		vector = vector.normalized * 0.2f;
	}

	// Token: 0x0600078E RID: 1934 RVA: 0x00021B37 File Offset: 0x0001FD37
	public bool CharacterReload(Item prefererdBullet = null)
	{
		return base.Holder && base.Holder.TryToReload(prefererdBullet);
	}

	// Token: 0x0600078F RID: 1935 RVA: 0x00021B54 File Offset: 0x0001FD54
	public bool BeginReload()
	{
		if (this.gunState != ItemAgent_Gun.GunStates.ready && this.gunState != ItemAgent_Gun.GunStates.empty && this.gunState != ItemAgent_Gun.GunStates.shootCooling)
		{
			return false;
		}
		this.burstCounter = 0;
		if (this.GunItemSetting.PreferdBulletsToLoad != null)
		{
			this.GunItemSetting.SetTargetBulletType(this.GunItemSetting.PreferdBulletsToLoad);
		}
		if (this.GunItemSetting.TargetBulletID == -1)
		{
			this.GunItemSetting.AutoSetTypeInInventory(base.Holder.CharacterItem.Inventory);
		}
		if (this.GunItemSetting.TargetBulletID == -1)
		{
			return false;
		}
		int num = -1;
		Item currentLoadedBullet = this.GunItemSetting.GetCurrentLoadedBullet();
		if (currentLoadedBullet != null)
		{
			num = currentLoadedBullet.TypeID;
		}
		if (this.BulletCount >= this.Capacity && num == this.GunItemSetting.TargetBulletID)
		{
			return false;
		}
		if (this.GunItemSetting.PreferdBulletsToLoad == null && this.GunItemSetting.GetBulletCountofTypeInInventory(this.GunItemSetting.TargetBulletID, base.Holder.CharacterItem.Inventory) <= 0)
		{
			if (base.Holder && this.GunItemSetting.BulletCount <= 0)
			{
				base.Holder.PopText("Poptext_OutOfAmmo".ToPlainText(), -1f);
			}
			return false;
		}
		this.gunState = ItemAgent_Gun.GunStates.reloading;
		this.stateTimer = 0f;
		this.PostStartReloadSound();
		return true;
	}

	// Token: 0x06000790 RID: 1936 RVA: 0x00021CB0 File Offset: 0x0001FEB0
	private void PostStartReloadSound()
	{
		if (this._reloadSoundLoopEvent != null)
		{
			this._reloadSoundLoopEvent.Value.stop(STOP_MODE.IMMEDIATE);
		}
		if (!base.gameObject.activeInHierarchy)
		{
			return;
		}
		string soundkey = this.GunItemSetting.reloadKey.ToLower() + "_start";
		string eventName = "SFX/Combat/Gun/Reload/{soundkey}".Format(new
		{
			soundkey
		});
		this._reloadSoundLoopEvent = AudioManager.Post(eventName, base.gameObject);
	}

	// Token: 0x06000791 RID: 1937 RVA: 0x00021D2C File Offset: 0x0001FF2C
	private void PostReloadSuccessSound()
	{
		if (this._reloadSoundLoopEvent != null)
		{
			this._reloadSoundLoopEvent.Value.stop(STOP_MODE.IMMEDIATE);
		}
		if (!base.gameObject.activeInHierarchy)
		{
			return;
		}
		string soundkey = this.GunItemSetting.reloadKey.ToLower() + "_end";
		AudioManager.Post("SFX/Combat/Gun/Reload/{soundkey}".Format(new
		{
			soundkey
		}), base.gameObject);
	}

	// Token: 0x06000792 RID: 1938 RVA: 0x00021DA0 File Offset: 0x0001FFA0
	private void PostShootSound()
	{
		string text = this.GunItemSetting.shootKey.ToLower();
		if (this.Silenced)
		{
			text += "_mute";
		}
		string eventName = "SFX/Combat/Gun/Shoot/{soundkey}".Format(new
		{
			soundkey = text
		});
		this._shootSoundEvent = AudioManager.Post(eventName, base.gameObject);
	}

	// Token: 0x06000793 RID: 1939 RVA: 0x00021DF5 File Offset: 0x0001FFF5
	private void StopAllSound()
	{
		AudioManager.StopAll(base.gameObject, STOP_MODE.IMMEDIATE);
	}

	// Token: 0x06000794 RID: 1940 RVA: 0x00021E04 File Offset: 0x00020004
	private void StopReloadSound()
	{
		if (this._reloadSoundLoopEvent != null)
		{
			this._reloadSoundLoopEvent.Value.stop(STOP_MODE.IMMEDIATE);
		}
	}

	// Token: 0x06000795 RID: 1941 RVA: 0x00021E33 File Offset: 0x00020033
	public void CancleReload()
	{
		this.StopReloadSound();
		if (this.gunState == ItemAgent_Gun.GunStates.reloading)
		{
			this.TransToBurstCooling();
			return;
		}
	}

	// Token: 0x06000796 RID: 1942 RVA: 0x00021E4B File Offset: 0x0002004B
	public bool IsFull()
	{
		return this.BulletCount >= this.Capacity;
	}

	// Token: 0x06000797 RID: 1943 RVA: 0x00021E60 File Offset: 0x00020060
	public int GetBulletCountInInventory()
	{
		if (!this.GunItemSetting || !base.Holder || !base.Holder.CharacterItem)
		{
			return 0;
		}
		return this.GunItemSetting.GetBulletCountofTypeInInventory(this.GunItemSetting.TargetBulletID, base.Holder.CharacterItem.Inventory);
	}

	// Token: 0x06000798 RID: 1944 RVA: 0x00021EC4 File Offset: 0x000200C4
	private void StartLoadBullets()
	{
		this.GunItemSetting.LoadBulletsFromInventory(base.Holder.CharacterItem.Inventory).Forget();
	}

	// Token: 0x06000799 RID: 1945 RVA: 0x00021EF4 File Offset: 0x000200F4
	private void StartVisualRecoil()
	{
		this._recoilBack = true;
	}

	// Token: 0x0600079A RID: 1946 RVA: 0x00021F00 File Offset: 0x00020100
	private void UpdateVisualRecoil()
	{
		bool flag = false;
		if (this._recoilBack)
		{
			flag = true;
			this._recoilMoveValue = Mathf.MoveTowards(this._recoilMoveValue, 1f, this._recoilBackSpeed * Time.deltaTime);
			if (Mathf.Approximately(this._recoilMoveValue, 1f))
			{
				this._recoilBack = false;
			}
		}
		else if (this._recoilMoveValue > 0f)
		{
			flag = true;
			this._recoilMoveValue = Mathf.MoveTowards(this._recoilMoveValue, 0f, this._recoilRecoverSpeed * Time.deltaTime);
		}
		if (flag)
		{
			base.transform.localPosition = Vector3.back * this._recoilMoveValue * this._recoilDistance;
		}
	}

	// Token: 0x0600079B RID: 1947 RVA: 0x00021FB0 File Offset: 0x000201B0
	public void SetTrigger(bool trigger, bool _triggerThisFrame, bool _releaseThisFrame)
	{
		this.triggerInput = trigger;
		this.triggerThisFrame = _triggerThisFrame;
		this.releaseThisFrame = _releaseThisFrame;
	}

	// Token: 0x0600079C RID: 1948 RVA: 0x00021FC7 File Offset: 0x000201C7
	public bool IsReloading()
	{
		return this.gunState == ItemAgent_Gun.GunStates.reloading;
	}

	// Token: 0x0600079D RID: 1949 RVA: 0x00021FD4 File Offset: 0x000201D4
	public Progress GetReloadProgress()
	{
		Progress result = default(Progress);
		if (this.IsReloading())
		{
			result.inProgress = true;
			result.total = this.ReloadTime;
			result.current = this.stateTimer;
		}
		else
		{
			result.inProgress = false;
		}
		return result;
	}

	// Token: 0x0600079E RID: 1950 RVA: 0x00022020 File Offset: 0x00020220
	public ADSAimMarker GetAimMarkerPfb()
	{
		Slot slot = base.Item.Slots.GetSlot("Scope");
		if (slot != null && slot.Content != null)
		{
			ItemSetting_Accessory component = slot.Content.GetComponent<ItemSetting_Accessory>();
			if (component.overrideAdsAimMarker)
			{
				return component.overrideAdsAimMarker;
			}
		}
		return this._gunItemSetting.adsAimMarker;
	}

	// Token: 0x040006DC RID: 1756
	private Item _bulletItem;

	// Token: 0x040006DD RID: 1757
	private static int ShootSpeedHash = "ShootSpeed".GetHashCode();

	// Token: 0x040006DE RID: 1758
	private static int ReloadTimeHash = "ReloadTime".GetHashCode();

	// Token: 0x040006DF RID: 1759
	private static int CapacityHash = "Capacity".GetHashCode();

	// Token: 0x040006E0 RID: 1760
	private static int DurabilityHash = "Durability".GetHashCode();

	// Token: 0x040006E1 RID: 1761
	private float maxDurability;

	// Token: 0x040006E2 RID: 1762
	private static int DamageHash = "Damage".GetHashCode();

	// Token: 0x040006E3 RID: 1763
	private static int BurstCountHash = "BurstCount".GetHashCode();

	// Token: 0x040006E4 RID: 1764
	private static int BulletSpeedHash = "BulletSpeed".GetHashCode();

	// Token: 0x040006E5 RID: 1765
	private static int BulletDistanceHash = "BulletDistance".GetHashCode();

	// Token: 0x040006E6 RID: 1766
	private static int PenetrateHash = "Penetrate".GetHashCode();

	// Token: 0x040006E7 RID: 1767
	private static int explosionDamageMultiplierHash = "ExplosionDamageMultiplier".GetHashCode();

	// Token: 0x040006E8 RID: 1768
	private static int CritRateHash = "CritRate".GetHashCode();

	// Token: 0x040006E9 RID: 1769
	private static int CritDamageFactorHash = "CritDamageFactor".GetHashCode();

	// Token: 0x040006EA RID: 1770
	private static int SoundRangeHash = "SoundRange".GetHashCode();

	// Token: 0x040006EB RID: 1771
	private static int ArmorPiercingHash = "ArmorPiercing".GetHashCode();

	// Token: 0x040006EC RID: 1772
	private static int ArmorBreakHash = "ArmorBreak".GetHashCode();

	// Token: 0x040006ED RID: 1773
	private static int ShotCountHash = "ShotCount".GetHashCode();

	// Token: 0x040006EE RID: 1774
	private static int ShotAngleHash = "ShotAngle".GetHashCode();

	// Token: 0x040006EF RID: 1775
	private static int ADSAimDistanceFactorHash = "ADSAimDistanceFactor".GetHashCode();

	// Token: 0x040006F0 RID: 1776
	private static int AdsTimeHash = "ADSTime".GetHashCode();

	// Token: 0x040006F1 RID: 1777
	private float scatterFactorHips = 1f;

	// Token: 0x040006F2 RID: 1778
	private float scatterFactorAds = 1f;

	// Token: 0x040006F3 RID: 1779
	private static int ScatterFactorHash = "ScatterFactor".GetHashCode();

	// Token: 0x040006F4 RID: 1780
	private static int ScatterFactorHashADS = "ScatterFactorADS".GetHashCode();

	// Token: 0x040006F5 RID: 1781
	private static int DefaultScatterHash = "DefaultScatter".GetHashCode();

	// Token: 0x040006F6 RID: 1782
	private static int DefaultScatterHashADS = "DefaultScatterADS".GetHashCode();

	// Token: 0x040006F7 RID: 1783
	private static int MaxScatterHash = "MaxScatter".GetHashCode();

	// Token: 0x040006F8 RID: 1784
	private static int MaxScatterHashADS = "MaxScatterADS".GetHashCode();

	// Token: 0x040006F9 RID: 1785
	private static int ScatterGrowHash = "ScatterGrow".GetHashCode();

	// Token: 0x040006FA RID: 1786
	private static int ScatterGrowHashADS = "ScatterGrowADS".GetHashCode();

	// Token: 0x040006FB RID: 1787
	private static int ScatterRecoverHash = "ScatterRecover".GetHashCode();

	// Token: 0x040006FC RID: 1788
	private static int ScatterRecoverHashADS = "ScatterRecoverADS".GetHashCode();

	// Token: 0x040006FD RID: 1789
	private static int RecoilVMinHash = "RecoilVMin".GetHashCode();

	// Token: 0x040006FE RID: 1790
	private static int RecoilVMaxHash = "RecoilVMax".GetHashCode();

	// Token: 0x040006FF RID: 1791
	private static int RecoilHMinHash = "RecoilHMin".GetHashCode();

	// Token: 0x04000700 RID: 1792
	private static int RecoilHMaxHash = "RecoilHMax".GetHashCode();

	// Token: 0x04000701 RID: 1793
	private static int RecoilScaleVHash = "RecoilScaleV".GetHashCode();

	// Token: 0x04000702 RID: 1794
	private static int RecoilScaleHHash = "RecoilScaleH".GetHashCode();

	// Token: 0x04000703 RID: 1795
	private static int RecoilRecoverHash = "RecoilRecover".GetHashCode();

	// Token: 0x04000704 RID: 1796
	private static int RecoilTimeHash = "RecoilTime".GetHashCode();

	// Token: 0x04000705 RID: 1797
	private static int RecoilRecoverTimeHash = "RecoilRecoverTime".GetHashCode();

	// Token: 0x04000706 RID: 1798
	private static int MoveSpeedMultiplierHash = "MoveSpeedMultiplier".GetHashCode();

	// Token: 0x04000707 RID: 1799
	private static int AdsWalkSpeedMultiplierHash = "AdsWalkSpeedMultiplier".GetHashCode();

	// Token: 0x04000708 RID: 1800
	private static int BuffChanceHash = "BuffChance".GetHashCode();

	// Token: 0x04000709 RID: 1801
	private static int bulletCritRateGainHash = "CritRateGain".GetHashCode();

	// Token: 0x0400070A RID: 1802
	private static int bulletCritDamageFactorGainHash = "CritDamageFactorGain".GetHashCode();

	// Token: 0x0400070B RID: 1803
	private static int bulletArmorPiercingGainHash = "ArmorPiercingGain".GetHashCode();

	// Token: 0x0400070C RID: 1804
	private static int BulletDamageMultiplierHash = "damageMultiplier".GetHashCode();

	// Token: 0x0400070D RID: 1805
	private static int bulletExplosionRangeHash = "ExplosionRange".GetHashCode();

	// Token: 0x0400070E RID: 1806
	private static int BulletBuffChanceMultiplierHash = "buffChanceMultiplier".GetHashCode();

	// Token: 0x0400070F RID: 1807
	private static int BulletBleedChanceHash = "bleedChance".GetHashCode();

	// Token: 0x04000710 RID: 1808
	private static int bulletExplosionDamageHash = "ExplosionDamage".GetHashCode();

	// Token: 0x04000711 RID: 1809
	private static int armorBreakGainHash = "ArmorBreakGain".GetHashCode();

	// Token: 0x04000712 RID: 1810
	private static int bulletDurabilityCostHash = "DurabilityCost".GetHashCode();

	// Token: 0x04000713 RID: 1811
	private int muzzleIndex;

	// Token: 0x04000714 RID: 1812
	public GameObject loadedVisualObject;

	// Token: 0x04000715 RID: 1813
	private float adsValue;

	// Token: 0x04000716 RID: 1814
	private Transform _mz1;

	// Token: 0x04000717 RID: 1815
	private Transform _mz2;

	// Token: 0x04000718 RID: 1816
	private bool hasMz2 = true;

	// Token: 0x04000719 RID: 1817
	[SerializeField]
	private ParticleSystem shellParticle;

	// Token: 0x0400071A RID: 1818
	private ItemSetting_Gun _gunItemSetting;

	// Token: 0x0400071B RID: 1819
	private bool triggerInput;

	// Token: 0x0400071C RID: 1820
	private bool triggerThisFrame;

	// Token: 0x0400071D RID: 1821
	private bool releaseThisFrame;

	// Token: 0x0400071E RID: 1822
	private bool triggerBuffer;

	// Token: 0x0400071F RID: 1823
	private float scatterBeforeControl;

	// Token: 0x04000723 RID: 1827
	private EventInstance? _shootSoundEvent;

	// Token: 0x04000724 RID: 1828
	private EventInstance? _reloadSoundLoopEvent;

	// Token: 0x04000725 RID: 1829
	private float stateTimer;

	// Token: 0x04000726 RID: 1830
	private int burstCounter;

	// Token: 0x04000727 RID: 1831
	private Projectile projInst;

	// Token: 0x04000728 RID: 1832
	private ItemAgent_Gun.GunStates gunState = ItemAgent_Gun.GunStates.ready;

	// Token: 0x04000729 RID: 1833
	private bool needAutoReload;

	// Token: 0x0400072A RID: 1834
	private bool loadBulletsStarted;

	// Token: 0x0400072B RID: 1835
	private float _recoilMoveValue;

	// Token: 0x0400072C RID: 1836
	private float _recoilDistance = 0.2f;

	// Token: 0x0400072D RID: 1837
	private float _recoilBackSpeed = 20f;

	// Token: 0x0400072E RID: 1838
	private float _recoilRecoverSpeed = 8f;

	// Token: 0x0400072F RID: 1839
	private bool _recoilBack;

	// Token: 0x02000468 RID: 1128
	public enum GunStates
	{
		// Token: 0x04001B3E RID: 6974
		shootCooling,
		// Token: 0x04001B3F RID: 6975
		ready,
		// Token: 0x04001B40 RID: 6976
		fire,
		// Token: 0x04001B41 RID: 6977
		burstEachShotCooling,
		// Token: 0x04001B42 RID: 6978
		empty,
		// Token: 0x04001B43 RID: 6979
		reloading
	}
}
