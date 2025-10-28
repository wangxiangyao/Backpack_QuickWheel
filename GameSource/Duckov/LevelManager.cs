using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Duckov.MiniMaps;
using Duckov.Rules;
using Duckov.Scenes;
using Duckov.Utilities;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using Saves;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Token: 0x02000108 RID: 264
public class LevelManager : MonoBehaviour
{
	// Token: 0x170001D0 RID: 464
	// (get) Token: 0x060008D9 RID: 2265 RVA: 0x00027D71 File Offset: 0x00025F71
	public static LevelManager Instance
	{
		get
		{
			if (!LevelManager.instance)
			{
				LevelManager.SetInstance();
			}
			return LevelManager.instance;
		}
	}

	// Token: 0x170001D1 RID: 465
	// (get) Token: 0x060008DA RID: 2266 RVA: 0x00027D8C File Offset: 0x00025F8C
	public static Transform LootBoxInventoriesParent
	{
		get
		{
			if (LevelManager.Instance._lootBoxInventoriesParent == null)
			{
				GameObject gameObject = new GameObject("Loot Box Inventories");
				gameObject.transform.SetParent(LevelManager.Instance.transform);
				LevelManager.Instance._lootBoxInventoriesParent = gameObject.transform;
				LevelManager.LootBoxInventories.Clear();
			}
			return LevelManager.Instance._lootBoxInventoriesParent;
		}
	}

	// Token: 0x170001D2 RID: 466
	// (get) Token: 0x060008DB RID: 2267 RVA: 0x00027DEF File Offset: 0x00025FEF
	public static Dictionary<int, Inventory> LootBoxInventories
	{
		get
		{
			if (LevelManager.Instance._lootBoxInventories == null)
			{
				LevelManager.Instance._lootBoxInventories = new Dictionary<int, Inventory>();
			}
			return LevelManager.Instance._lootBoxInventories;
		}
	}

	// Token: 0x170001D3 RID: 467
	// (get) Token: 0x060008DC RID: 2268 RVA: 0x00027E16 File Offset: 0x00026016
	public bool IsRaidMap
	{
		get
		{
			return LevelConfig.IsRaidMap;
		}
	}

	// Token: 0x170001D4 RID: 468
	// (get) Token: 0x060008DD RID: 2269 RVA: 0x00027E1D File Offset: 0x0002601D
	public bool IsBaseLevel
	{
		get
		{
			return LevelConfig.IsBaseLevel;
		}
	}

	// Token: 0x170001D5 RID: 469
	// (get) Token: 0x060008DE RID: 2270 RVA: 0x00027E24 File Offset: 0x00026024
	public InputManager InputManager
	{
		get
		{
			return this.inputManager;
		}
	}

	// Token: 0x170001D6 RID: 470
	// (get) Token: 0x060008DF RID: 2271 RVA: 0x00027E2C File Offset: 0x0002602C
	public CharacterCreator CharacterCreator
	{
		get
		{
			return this.characterCreator;
		}
	}

	// Token: 0x170001D7 RID: 471
	// (get) Token: 0x060008E0 RID: 2272 RVA: 0x00027E34 File Offset: 0x00026034
	public ExitCreator ExitCreator
	{
		get
		{
			return this.exitCreator;
		}
	}

	// Token: 0x170001D8 RID: 472
	// (get) Token: 0x060008E1 RID: 2273 RVA: 0x00027E3C File Offset: 0x0002603C
	public ExplosionManager ExplosionManager
	{
		get
		{
			return this.explosionManager;
		}
	}

	// Token: 0x170001D9 RID: 473
	// (get) Token: 0x060008E2 RID: 2274 RVA: 0x00027E44 File Offset: 0x00026044
	private int characterItemTypeID
	{
		get
		{
			return GameplayDataSettings.ItemAssets.DefaultCharacterItemTypeID;
		}
	}

	// Token: 0x170001DA RID: 474
	// (get) Token: 0x060008E3 RID: 2275 RVA: 0x00027E50 File Offset: 0x00026050
	public CharacterMainControl MainCharacter
	{
		get
		{
			return this.mainCharacter;
		}
	}

	// Token: 0x170001DB RID: 475
	// (get) Token: 0x060008E4 RID: 2276 RVA: 0x00027E58 File Offset: 0x00026058
	public CharacterMainControl PetCharacter
	{
		get
		{
			return this.petCharacter;
		}
	}

	// Token: 0x170001DC RID: 476
	// (get) Token: 0x060008E5 RID: 2277 RVA: 0x00027E60 File Offset: 0x00026060
	public GameCamera GameCamera
	{
		get
		{
			return this.gameCamera;
		}
	}

	// Token: 0x170001DD RID: 477
	// (get) Token: 0x060008E6 RID: 2278 RVA: 0x00027E68 File Offset: 0x00026068
	public FogOfWarManager FogOfWarManager
	{
		get
		{
			return this.fowManager;
		}
	}

	// Token: 0x170001DE RID: 478
	// (get) Token: 0x060008E7 RID: 2279 RVA: 0x00027E70 File Offset: 0x00026070
	public TimeOfDayController TimeOfDayController
	{
		get
		{
			return this.timeOfDayController;
		}
	}

	// Token: 0x1400003F RID: 63
	// (add) Token: 0x060008E8 RID: 2280 RVA: 0x00027E78 File Offset: 0x00026078
	// (remove) Token: 0x060008E9 RID: 2281 RVA: 0x00027EAC File Offset: 0x000260AC
	public static event Action OnLevelBeginInitializing;

	// Token: 0x14000040 RID: 64
	// (add) Token: 0x060008EA RID: 2282 RVA: 0x00027EE0 File Offset: 0x000260E0
	// (remove) Token: 0x060008EB RID: 2283 RVA: 0x00027F14 File Offset: 0x00026114
	public static event Action OnLevelInitialized;

	// Token: 0x14000041 RID: 65
	// (add) Token: 0x060008EC RID: 2284 RVA: 0x00027F48 File Offset: 0x00026148
	// (remove) Token: 0x060008ED RID: 2285 RVA: 0x00027F7C File Offset: 0x0002617C
	public static event Action OnAfterLevelInitialized;

	// Token: 0x170001DF RID: 479
	// (get) Token: 0x060008EE RID: 2286 RVA: 0x00027FAF File Offset: 0x000261AF
	public AIMainBrain AIMainBrain
	{
		get
		{
			return this.aiMainBrain;
		}
	}

	// Token: 0x170001E0 RID: 480
	// (get) Token: 0x060008EF RID: 2287 RVA: 0x00027FB7 File Offset: 0x000261B7
	public static bool LevelInitializing
	{
		get
		{
			return !(LevelManager.Instance == null) && LevelManager.Instance.initingLevel;
		}
	}

	// Token: 0x170001E1 RID: 481
	// (get) Token: 0x060008F0 RID: 2288 RVA: 0x00027FD2 File Offset: 0x000261D2
	public static bool AfterInit
	{
		get
		{
			return !(LevelManager.Instance == null) && LevelManager.Instance.afterInit;
		}
	}

	// Token: 0x170001E2 RID: 482
	// (get) Token: 0x060008F1 RID: 2289 RVA: 0x00027FED File Offset: 0x000261ED
	public PetProxy PetProxy
	{
		get
		{
			return this.petProxy;
		}
	}

	// Token: 0x170001E3 RID: 483
	// (get) Token: 0x060008F2 RID: 2290 RVA: 0x00027FF5 File Offset: 0x000261F5
	public BulletPool BulletPool
	{
		get
		{
			return this.bulletPool;
		}
	}

	// Token: 0x170001E4 RID: 484
	// (get) Token: 0x060008F3 RID: 2291 RVA: 0x00027FFD File Offset: 0x000261FD
	public CustomFaceManager CustomFaceManager
	{
		get
		{
			return this.customFaceManager;
		}
	}

	// Token: 0x170001E5 RID: 485
	// (get) Token: 0x060008F4 RID: 2292 RVA: 0x00028005 File Offset: 0x00026205
	// (set) Token: 0x060008F5 RID: 2293 RVA: 0x00028020 File Offset: 0x00026220
	public static string LevelInitializingComment
	{
		get
		{
			if (LevelManager.Instance == null)
			{
				return null;
			}
			return LevelManager.Instance._levelInitializingComment;
		}
		set
		{
			if (LevelManager.Instance == null)
			{
				return;
			}
			LevelManager.Instance._levelInitializingComment = value;
			Action<string> onLevelInitializingCommentChanged = LevelManager.OnLevelInitializingCommentChanged;
			if (onLevelInitializingCommentChanged != null)
			{
				onLevelInitializingCommentChanged(value);
			}
			Debug.Log("[Level Initialization] " + value);
		}
	}

	// Token: 0x14000042 RID: 66
	// (add) Token: 0x060008F6 RID: 2294 RVA: 0x0002805C File Offset: 0x0002625C
	// (remove) Token: 0x060008F7 RID: 2295 RVA: 0x00028090 File Offset: 0x00026290
	public static event Action<string> OnLevelInitializingCommentChanged;

	// Token: 0x170001E6 RID: 486
	// (get) Token: 0x060008F8 RID: 2296 RVA: 0x000280C3 File Offset: 0x000262C3
	public static bool LevelInited
	{
		get
		{
			return !(LevelManager.instance == null) && LevelManager.instance.levelInited;
		}
	}

	// Token: 0x14000043 RID: 67
	// (add) Token: 0x060008F9 RID: 2297 RVA: 0x000280E0 File Offset: 0x000262E0
	// (remove) Token: 0x060008FA RID: 2298 RVA: 0x00028114 File Offset: 0x00026314
	public static event Action<EvacuationInfo> OnEvacuated;

	// Token: 0x14000044 RID: 68
	// (add) Token: 0x060008FB RID: 2299 RVA: 0x00028148 File Offset: 0x00026348
	// (remove) Token: 0x060008FC RID: 2300 RVA: 0x0002817C File Offset: 0x0002637C
	public static event Action<DamageInfo> OnMainCharacterDead;

	// Token: 0x170001E7 RID: 487
	// (get) Token: 0x060008FD RID: 2301 RVA: 0x000281AF File Offset: 0x000263AF
	public float LevelTime
	{
		get
		{
			return Time.time - this.levelStartTime;
		}
	}

	// Token: 0x14000045 RID: 69
	// (add) Token: 0x060008FE RID: 2302 RVA: 0x000281C0 File Offset: 0x000263C0
	// (remove) Token: 0x060008FF RID: 2303 RVA: 0x000281F4 File Offset: 0x000263F4
	public static event Action OnNewGameReport;

	// Token: 0x170001E8 RID: 488
	// (get) Token: 0x06000900 RID: 2304 RVA: 0x00028227 File Offset: 0x00026427
	public static Ruleset Rule
	{
		get
		{
			return LevelManager.rule;
		}
	}

	// Token: 0x06000901 RID: 2305 RVA: 0x0002822E File Offset: 0x0002642E
	public static void RegisterWaitForInitialization<T>(T toWait) where T : class, IInitializedQueryHandler
	{
		if (toWait == null)
		{
			return;
		}
		if (toWait == null)
		{
			return;
		}
		LevelManager.waitForInitializationList.Add(toWait);
	}

	// Token: 0x06000902 RID: 2306 RVA: 0x00028252 File Offset: 0x00026452
	public static bool UnregisterWaitForInitialization<T>(T obj) where T : class
	{
		return LevelManager.waitForInitializationList.Remove(obj);
	}

	// Token: 0x06000903 RID: 2307 RVA: 0x00028264 File Offset: 0x00026464
	private void Start()
	{
		if (!SceneLoader.IsSceneLoading)
		{
			this.StartInit(default(SceneLoadingContext));
		}
		else
		{
			SceneLoader.onFinishedLoadingScene += this.StartInit;
		}
		if (!SavesSystem.Load<bool>("NewGameReported"))
		{
			SavesSystem.Save<bool>("NewGameReported", true);
			Action onNewGameReport = LevelManager.OnNewGameReport;
			if (onNewGameReport != null)
			{
				onNewGameReport();
			}
		}
		if (GameManager.newBoot)
		{
			this.OnNewBoot();
			GameManager.newBoot = false;
		}
	}

	// Token: 0x06000904 RID: 2308 RVA: 0x000282D4 File Offset: 0x000264D4
	private void OnDestroy()
	{
		SceneLoader.onFinishedLoadingScene -= this.StartInit;
		CharacterMainControl characterMainControl = this.mainCharacter;
		if (characterMainControl == null)
		{
			return;
		}
		Health health = characterMainControl.Health;
		if (health == null)
		{
			return;
		}
		health.OnDeadEvent.RemoveListener(new UnityAction<DamageInfo>(this.OnMainCharacterDie));
	}

	// Token: 0x06000905 RID: 2309 RVA: 0x00028312 File Offset: 0x00026512
	private void OnNewBoot()
	{
		Debug.Log("New boot");
		GameClock.Instance.StepTimeTil(new TimeSpan(7, 0, 0));
	}

	// Token: 0x06000906 RID: 2310 RVA: 0x00028330 File Offset: 0x00026530
	private void StartInit(SceneLoadingContext context)
	{
		this.InitLevel(context).Forget();
	}

	// Token: 0x06000907 RID: 2311 RVA: 0x0002834C File Offset: 0x0002654C
	private UniTaskVoid InitLevel(SceneLoadingContext context)
	{
		LevelManager.<InitLevel>d__114 <InitLevel>d__;
		<InitLevel>d__.<>t__builder = AsyncUniTaskVoidMethodBuilder.Create();
		<InitLevel>d__.<>4__this = this;
		<InitLevel>d__.context = context;
		<InitLevel>d__.<>1__state = -1;
		<InitLevel>d__.<>t__builder.Start<LevelManager.<InitLevel>d__114>(ref <InitLevel>d__);
		return <InitLevel>d__.<>t__builder.Task;
	}

	// Token: 0x06000908 RID: 2312 RVA: 0x00028398 File Offset: 0x00026598
	private UniTask CreateMate()
	{
		LevelManager.<CreateMate>d__115 <CreateMate>d__;
		<CreateMate>d__.<>t__builder = AsyncUniTaskMethodBuilder.Create();
		<CreateMate>d__.<>4__this = this;
		<CreateMate>d__.<>1__state = -1;
		<CreateMate>d__.<>t__builder.Start<LevelManager.<CreateMate>d__115>(ref <CreateMate>d__);
		return <CreateMate>d__.<>t__builder.Task;
	}

	// Token: 0x06000909 RID: 2313 RVA: 0x000283DC File Offset: 0x000265DC
	private UniTask WaitForOtherInitialization()
	{
		LevelManager.<WaitForOtherInitialization>d__116 <WaitForOtherInitialization>d__;
		<WaitForOtherInitialization>d__.<>t__builder = AsyncUniTaskMethodBuilder.Create();
		<WaitForOtherInitialization>d__.<>1__state = -1;
		<WaitForOtherInitialization>d__.<>t__builder.Start<LevelManager.<WaitForOtherInitialization>d__116>(ref <WaitForOtherInitialization>d__);
		return <WaitForOtherInitialization>d__.<>t__builder.Task;
	}

	// Token: 0x0600090A RID: 2314 RVA: 0x00028418 File Offset: 0x00026618
	private void HandleRaidInitialization()
	{
		RaidUtilities.RaidInfo currentRaid = RaidUtilities.CurrentRaid;
		if (this.IsRaidMap)
		{
			if (currentRaid.ended)
			{
				RaidUtilities.NewRaid();
				this.isNewRaidLevel = true;
				return;
			}
		}
		else if (this.IsBaseLevel && !currentRaid.ended)
		{
			RaidUtilities.NotifyEnd();
		}
	}

	// Token: 0x0600090B RID: 2315 RVA: 0x00028460 File Offset: 0x00026660
	public void RefreshMainCharacterFace()
	{
		if (this.mainCharacter.characterModel.CustomFace)
		{
			CustomFaceSettingData saveData = this.customFaceManager.LoadMainCharacterSetting();
			this.mainCharacter.characterModel.CustomFace.LoadFromData(saveData);
		}
	}

	// Token: 0x0600090C RID: 2316 RVA: 0x000284A8 File Offset: 0x000266A8
	private UniTask CreateMainCharacterAsync(Vector3 position, Quaternion rotation)
	{
		LevelManager.<CreateMainCharacterAsync>d__119 <CreateMainCharacterAsync>d__;
		<CreateMainCharacterAsync>d__.<>t__builder = AsyncUniTaskMethodBuilder.Create();
		<CreateMainCharacterAsync>d__.<>4__this = this;
		<CreateMainCharacterAsync>d__.position = position;
		<CreateMainCharacterAsync>d__.rotation = rotation;
		<CreateMainCharacterAsync>d__.<>1__state = -1;
		<CreateMainCharacterAsync>d__.<>t__builder.Start<LevelManager.<CreateMainCharacterAsync>d__119>(ref <CreateMainCharacterAsync>d__);
		return <CreateMainCharacterAsync>d__.<>t__builder.Task;
	}

	// Token: 0x0600090D RID: 2317 RVA: 0x000284FC File Offset: 0x000266FC
	private void SetCharacterItemsInspected()
	{
		foreach (Slot slot in this.mainCharacter.CharacterItem.Slots)
		{
			if (slot.Content != null)
			{
				slot.Content.Inspected = true;
			}
		}
		foreach (Item item in this.mainCharacter.CharacterItem.Inventory)
		{
			if (item != null)
			{
				item.Inspected = true;
			}
		}
		foreach (Item item2 in this.petProxy.Inventory)
		{
			if (item2 != null)
			{
				item2.Inspected = true;
			}
		}
	}

	// Token: 0x0600090E RID: 2318 RVA: 0x00028604 File Offset: 0x00026804
	private static void SetInstance()
	{
		if (LevelManager.instance)
		{
			return;
		}
		LevelManager.instance = UnityEngine.Object.FindFirstObjectByType<LevelManager>();
		LevelManager.instance;
	}

	// Token: 0x0600090F RID: 2319 RVA: 0x00028628 File Offset: 0x00026828
	private UniTask<Item> LoadOrCreateCharacterItemInstance()
	{
		LevelManager.<LoadOrCreateCharacterItemInstance>d__122 <LoadOrCreateCharacterItemInstance>d__;
		<LoadOrCreateCharacterItemInstance>d__.<>t__builder = AsyncUniTaskMethodBuilder<Item>.Create();
		<LoadOrCreateCharacterItemInstance>d__.<>4__this = this;
		<LoadOrCreateCharacterItemInstance>d__.<>1__state = -1;
		<LoadOrCreateCharacterItemInstance>d__.<>t__builder.Start<LevelManager.<LoadOrCreateCharacterItemInstance>d__122>(ref <LoadOrCreateCharacterItemInstance>d__);
		return <LoadOrCreateCharacterItemInstance>d__.<>t__builder.Task;
	}

	// Token: 0x06000910 RID: 2320 RVA: 0x0002866B File Offset: 0x0002686B
	public void NotifyEvacuated(EvacuationInfo info)
	{
		this.mainCharacter.Health.SetInvincible(true);
		Action<EvacuationInfo> onEvacuated = LevelManager.OnEvacuated;
		if (onEvacuated != null)
		{
			onEvacuated(info);
		}
		this.SaveMainCharacter();
		SavesSystem.CollectSaveData();
		SavesSystem.SaveFile(true);
	}

	// Token: 0x06000911 RID: 2321 RVA: 0x000286A0 File Offset: 0x000268A0
	public void NotifySaveBeforeLoadScene(bool saveToFile)
	{
		this.SaveMainCharacter();
		SavesSystem.CollectSaveData();
		if (saveToFile)
		{
			SavesSystem.SaveFile(true);
		}
	}

	// Token: 0x06000912 RID: 2322 RVA: 0x000286B8 File Offset: 0x000268B8
	private void OnMainCharacterDie(DamageInfo dmgInfo)
	{
		if (this.dieTask)
		{
			return;
		}
		this.dieTask = true;
		this.CharacterDieTask(dmgInfo).Forget();
		Action<DamageInfo> onMainCharacterDead = LevelManager.OnMainCharacterDead;
		if (onMainCharacterDead == null)
		{
			return;
		}
		onMainCharacterDead(dmgInfo);
	}

	// Token: 0x06000913 RID: 2323 RVA: 0x000286F4 File Offset: 0x000268F4
	private UniTaskVoid CharacterDieTask(DamageInfo dmgInfo)
	{
		LevelManager.<CharacterDieTask>d__127 <CharacterDieTask>d__;
		<CharacterDieTask>d__.<>t__builder = AsyncUniTaskVoidMethodBuilder.Create();
		<CharacterDieTask>d__.<>4__this = this;
		<CharacterDieTask>d__.dmgInfo = dmgInfo;
		<CharacterDieTask>d__.<>1__state = -1;
		<CharacterDieTask>d__.<>t__builder.Start<LevelManager.<CharacterDieTask>d__127>(ref <CharacterDieTask>d__);
		return <CharacterDieTask>d__.<>t__builder.Task;
	}

	// Token: 0x06000914 RID: 2324 RVA: 0x0002873F File Offset: 0x0002693F
	internal void SaveMainCharacter()
	{
		this.mainCharacter.CharacterItem.Save("MainCharacterItemData");
		SavesSystem.Save<float>("MainCharacterHealth", this.MainCharacter.Health.CurrentHealth);
	}

	// Token: 0x06000915 RID: 2325 RVA: 0x00028770 File Offset: 0x00026970
	[return: TupleElementNames(new string[]
	{
		"sceneID",
		"locationData"
	})]
	private ValueTuple<string, SubSceneEntry.Location> GetPlayerStartLocation()
	{
		List<ValueTuple<string, SubSceneEntry.Location>> list = new List<ValueTuple<string, SubSceneEntry.Location>>();
		string text = "StartPoints";
		if (LevelManager.loadLevelBeaconIndex > 0)
		{
			text = text + "_" + LevelManager.loadLevelBeaconIndex.ToString();
			LevelManager.loadLevelBeaconIndex = 0;
		}
		foreach (SubSceneEntry subSceneEntry in MultiSceneCore.Instance.SubScenes)
		{
			foreach (SubSceneEntry.Location location in subSceneEntry.cachedLocations)
			{
				if (this.IsPathCompatible(location, text))
				{
					list.Add(new ValueTuple<string, SubSceneEntry.Location>(subSceneEntry.sceneID, location));
				}
			}
		}
		if (list.Count == 0)
		{
			text = "StartPoints";
			foreach (SubSceneEntry subSceneEntry2 in MultiSceneCore.Instance.SubScenes)
			{
				foreach (SubSceneEntry.Location location2 in subSceneEntry2.cachedLocations)
				{
					if (this.IsPathCompatible(location2, text))
					{
						list.Add(new ValueTuple<string, SubSceneEntry.Location>(subSceneEntry2.sceneID, location2));
					}
				}
			}
		}
		return list.GetRandom<ValueTuple<string, SubSceneEntry.Location>>();
	}

	// Token: 0x06000916 RID: 2326 RVA: 0x00028900 File Offset: 0x00026B00
	private void CreateMainCharacterMapElement()
	{
		if (MultiSceneCore.Instance != null)
		{
			SimplePointOfInterest simplePointOfInterest = this.mainCharacter.gameObject.AddComponent<SimplePointOfInterest>();
			simplePointOfInterest.Color = this.characterMapIconColor;
			simplePointOfInterest.ShadowColor = this.characterMapShadowColor;
			simplePointOfInterest.ShadowDistance = 0f;
			simplePointOfInterest.Setup(this.characterMapIcon, "You", true, null);
		}
	}

	// Token: 0x06000917 RID: 2327 RVA: 0x0002895F File Offset: 0x00026B5F
	private void OnSubSceneLoaded()
	{
	}

	// Token: 0x06000918 RID: 2328 RVA: 0x00028964 File Offset: 0x00026B64
	private bool IsPathCompatible(SubSceneEntry.Location location, string keyWord)
	{
		string path = location.path;
		int num = path.IndexOf('/');
		return num != -1 && path.Substring(0, num) == keyWord;
	}

	// Token: 0x06000919 RID: 2329 RVA: 0x00028998 File Offset: 0x00026B98
	public void TestTeleport()
	{
		MultiSceneCore.Instance.LoadAndTeleport(this.testTeleportTarget).Forget<bool>();
	}

	// Token: 0x0600091A RID: 2330 RVA: 0x000289B0 File Offset: 0x00026BB0
	private LevelManager.LevelInfo mGetInfo()
	{
		Scene? activeSubScene = MultiSceneCore.ActiveSubScene;
		string activeSubSceneID = (activeSubScene != null) ? activeSubScene.Value.name : "";
		return new LevelManager.LevelInfo
		{
			isBaseLevel = this.IsBaseLevel,
			sceneName = base.gameObject.scene.name,
			activeSubSceneID = activeSubSceneID
		};
	}

	// Token: 0x0600091B RID: 2331 RVA: 0x00028A1C File Offset: 0x00026C1C
	public static LevelManager.LevelInfo GetCurrentLevelInfo()
	{
		if (LevelManager.Instance == null)
		{
			return default(LevelManager.LevelInfo);
		}
		return LevelManager.Instance.mGetInfo();
	}

	// Token: 0x0400080B RID: 2059
	private Transform _lootBoxInventoriesParent;

	// Token: 0x0400080C RID: 2060
	private Dictionary<int, Inventory> _lootBoxInventories;

	// Token: 0x0400080D RID: 2061
	[SerializeField]
	private Transform defaultStartPos;

	// Token: 0x0400080E RID: 2062
	private static LevelManager instance;

	// Token: 0x0400080F RID: 2063
	[SerializeField]
	private InputManager inputManager;

	// Token: 0x04000810 RID: 2064
	[SerializeField]
	private CharacterCreator characterCreator;

	// Token: 0x04000811 RID: 2065
	[SerializeField]
	private ExitCreator exitCreator;

	// Token: 0x04000812 RID: 2066
	[SerializeField]
	private ExplosionManager explosionManager;

	// Token: 0x04000813 RID: 2067
	[SerializeField]
	private CharacterModel characterModel;

	// Token: 0x04000814 RID: 2068
	private CharacterMainControl mainCharacter;

	// Token: 0x04000815 RID: 2069
	private CharacterMainControl petCharacter;

	// Token: 0x04000816 RID: 2070
	[SerializeField]
	private GameCamera gameCamera;

	// Token: 0x04000817 RID: 2071
	[SerializeField]
	private FogOfWarManager fowManager;

	// Token: 0x04000818 RID: 2072
	[SerializeField]
	private TimeOfDayController timeOfDayController;

	// Token: 0x0400081C RID: 2076
	[SerializeField]
	private AIMainBrain aiMainBrain;

	// Token: 0x0400081D RID: 2077
	[SerializeField]
	private CharacterRandomPreset matePreset;

	// Token: 0x0400081E RID: 2078
	private bool initingLevel;

	// Token: 0x0400081F RID: 2079
	private bool isNewRaidLevel;

	// Token: 0x04000820 RID: 2080
	private bool afterInit;

	// Token: 0x04000821 RID: 2081
	[SerializeField]
	private CharacterRandomPreset petPreset;

	// Token: 0x04000822 RID: 2082
	[SerializeField]
	private Sprite characterMapIcon;

	// Token: 0x04000823 RID: 2083
	[SerializeField]
	private Color characterMapIconColor;

	// Token: 0x04000824 RID: 2084
	[SerializeField]
	private Color characterMapShadowColor;

	// Token: 0x04000825 RID: 2085
	[SerializeField]
	private MultiSceneLocation testTeleportTarget;

	// Token: 0x04000826 RID: 2086
	[SerializeField]
	public SkillBase defaultSkill;

	// Token: 0x04000827 RID: 2087
	[SerializeField]
	private PetProxy petProxy;

	// Token: 0x04000828 RID: 2088
	[SerializeField]
	private CustomFaceManager customFaceManager;

	// Token: 0x04000829 RID: 2089
	[SerializeField]
	private BulletPool bulletPool;

	// Token: 0x0400082A RID: 2090
	private string _levelInitializingComment = "";

	// Token: 0x0400082B RID: 2091
	public static int loadLevelBeaconIndex = 0;

	// Token: 0x0400082D RID: 2093
	private bool levelInited;

	// Token: 0x0400082E RID: 2094
	public const string MainCharacterItemSaveKey = "MainCharacterItemData";

	// Token: 0x0400082F RID: 2095
	public const string MainCharacterHealthSaveKey = "MainCharacterHealth";

	// Token: 0x04000832 RID: 2098
	private float levelStartTime = -0.1f;

	// Token: 0x04000834 RID: 2100
	private static Ruleset rule;

	// Token: 0x04000835 RID: 2101
	private static List<object> waitForInitializationList = new List<object>();

	// Token: 0x04000836 RID: 2102
	private bool dieTask;

	// Token: 0x0200048A RID: 1162
	[Serializable]
	public struct LevelInfo
	{
		// Token: 0x04001BB6 RID: 7094
		public bool isBaseLevel;

		// Token: 0x04001BB7 RID: 7095
		public string sceneName;

		// Token: 0x04001BB8 RID: 7096
		public string activeSubSceneID;
	}
}
