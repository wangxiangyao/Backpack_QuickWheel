using System;
using Duckov.Utilities;
using UnityEngine;

// Token: 0x02000107 RID: 263
public class LevelConfig : MonoBehaviour
{
	// Token: 0x170001C8 RID: 456
	// (get) Token: 0x060008CE RID: 2254 RVA: 0x00027C4A File Offset: 0x00025E4A
	public static LevelConfig Instance
	{
		get
		{
			if (!LevelConfig.instance)
			{
				LevelConfig.SetInstance();
			}
			return LevelConfig.instance;
		}
	}

	// Token: 0x170001C9 RID: 457
	// (get) Token: 0x060008CF RID: 2255 RVA: 0x00027C62 File Offset: 0x00025E62
	public float LootBoxQualityLowPercent
	{
		get
		{
			return 1f - 1f / this.lootBoxHighQualityChanceMultiplier;
		}
	}

	// Token: 0x170001CA RID: 458
	// (get) Token: 0x060008D0 RID: 2256 RVA: 0x00027C76 File Offset: 0x00025E76
	public float LootboxItemCountMultiplier
	{
		get
		{
			return this.lootboxItemCountMultiplier;
		}
	}

	// Token: 0x170001CB RID: 459
	// (get) Token: 0x060008D1 RID: 2257 RVA: 0x00027C7E File Offset: 0x00025E7E
	public static bool IsBaseLevel
	{
		get
		{
			return LevelConfig.Instance && LevelConfig.Instance.isBaseLevel;
		}
	}

	// Token: 0x170001CC RID: 460
	// (get) Token: 0x060008D2 RID: 2258 RVA: 0x00027C98 File Offset: 0x00025E98
	public static bool IsRaidMap
	{
		get
		{
			return LevelConfig.Instance && LevelConfig.Instance.isRaidMap;
		}
	}

	// Token: 0x170001CD RID: 461
	// (get) Token: 0x060008D3 RID: 2259 RVA: 0x00027CB2 File Offset: 0x00025EB2
	public static int MinExitCount
	{
		get
		{
			if (!LevelConfig.Instance)
			{
				return 0;
			}
			return LevelConfig.Instance.minExitCount;
		}
	}

	// Token: 0x170001CE RID: 462
	// (get) Token: 0x060008D4 RID: 2260 RVA: 0x00027CCC File Offset: 0x00025ECC
	public static bool SpawnTomb
	{
		get
		{
			return !LevelConfig.Instance || LevelConfig.Instance.spawnTomb;
		}
	}

	// Token: 0x170001CF RID: 463
	// (get) Token: 0x060008D5 RID: 2261 RVA: 0x00027CE6 File Offset: 0x00025EE6
	public static int MaxExitCount
	{
		get
		{
			if (!LevelConfig.Instance)
			{
				return 0;
			}
			return LevelConfig.Instance.maxExitCount;
		}
	}

	// Token: 0x060008D6 RID: 2262 RVA: 0x00027D00 File Offset: 0x00025F00
	private void Awake()
	{
		UnityEngine.Object.Instantiate<LevelManager>(GameplayDataSettings.Prefabs.LevelManagerPrefab).transform.SetParent(base.transform);
	}

	// Token: 0x060008D7 RID: 2263 RVA: 0x00027D21 File Offset: 0x00025F21
	private static void SetInstance()
	{
		if (LevelConfig.instance)
		{
			return;
		}
		LevelConfig.instance = UnityEngine.Object.FindFirstObjectByType<LevelConfig>();
		LevelConfig.instance;
	}

	// Token: 0x04000802 RID: 2050
	private static LevelConfig instance;

	// Token: 0x04000803 RID: 2051
	[SerializeField]
	private bool isBaseLevel;

	// Token: 0x04000804 RID: 2052
	[SerializeField]
	private bool isRaidMap = true;

	// Token: 0x04000805 RID: 2053
	[SerializeField]
	private bool spawnTomb = true;

	// Token: 0x04000806 RID: 2054
	[SerializeField]
	private int minExitCount;

	// Token: 0x04000807 RID: 2055
	[SerializeField]
	private int maxExitCount;

	// Token: 0x04000808 RID: 2056
	public TimeOfDayConfig timeOfDayConfig;

	// Token: 0x04000809 RID: 2057
	[SerializeField]
	[Min(1f)]
	private float lootBoxHighQualityChanceMultiplier = 1f;

	// Token: 0x0400080A RID: 2058
	[SerializeField]
	[Range(0.1f, 10f)]
	private float lootboxItemCountMultiplier = 1f;
}
