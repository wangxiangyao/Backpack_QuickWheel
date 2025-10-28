using System;
using DG.Tweening;
using Duckov;
using Duckov.Achievements;
using Duckov.Modding;
using Duckov.NoteIndexs;
using Duckov.Rules;
using Duckov.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

// Token: 0x0200009C RID: 156
public class GameManager : MonoBehaviour
{
	// Token: 0x1700010E RID: 270
	// (get) Token: 0x06000531 RID: 1329 RVA: 0x000176C4 File Offset: 0x000158C4
	public static GameManager Instance
	{
		get
		{
			if (!Application.isPlaying)
			{
				return null;
			}
			if (GameManager._instance == null)
			{
				GameManager._instance = UnityEngine.Object.FindObjectOfType<GameManager>();
				if (GameManager._instance)
				{
					UnityEngine.Object.DontDestroyOnLoad(GameManager._instance.gameObject);
				}
			}
			if (GameManager._instance == null)
			{
				GameObject gameObject = Resources.Load<GameObject>("GameManager");
				if (gameObject == null)
				{
					Debug.LogError("Resources中找不到GameManager的Prefab");
				}
				GameManager component = UnityEngine.Object.Instantiate<GameObject>(gameObject).GetComponent<GameManager>();
				if (component == null)
				{
					Debug.LogError("GameManager的prefab上没有GameManager组件");
					return null;
				}
				GameManager._instance = component;
				if (GameManager._instance)
				{
					UnityEngine.Object.DontDestroyOnLoad(GameManager._instance.gameObject);
				}
			}
			return GameManager._instance;
		}
	}

	// Token: 0x1700010F RID: 271
	// (get) Token: 0x06000532 RID: 1330 RVA: 0x0001777C File Offset: 0x0001597C
	public static bool Paused
	{
		get
		{
			return !(GameManager.Instance == null) && GameManager.Instance.pauseMenu.Shown;
		}
	}

	// Token: 0x17000110 RID: 272
	// (get) Token: 0x06000533 RID: 1331 RVA: 0x000177A1 File Offset: 0x000159A1
	public static AudioManager AudioManager
	{
		get
		{
			return GameManager.Instance.audioManager;
		}
	}

	// Token: 0x17000111 RID: 273
	// (get) Token: 0x06000534 RID: 1332 RVA: 0x000177AD File Offset: 0x000159AD
	public static UIInputManager UiInputManager
	{
		get
		{
			return GameManager.Instance.uiInputManager;
		}
	}

	// Token: 0x17000112 RID: 274
	// (get) Token: 0x06000535 RID: 1333 RVA: 0x000177B9 File Offset: 0x000159B9
	public static PauseMenu PauseMenu
	{
		get
		{
			return GameManager.Instance.pauseMenu;
		}
	}

	// Token: 0x17000113 RID: 275
	// (get) Token: 0x06000536 RID: 1334 RVA: 0x000177C5 File Offset: 0x000159C5
	public static GameRulesManager DifficultyManager
	{
		get
		{
			return GameManager.Instance.difficultyManager;
		}
	}

	// Token: 0x17000114 RID: 276
	// (get) Token: 0x06000537 RID: 1335 RVA: 0x000177D1 File Offset: 0x000159D1
	public static SceneLoader SceneLoader
	{
		get
		{
			return GameManager.Instance.sceneLoader;
		}
	}

	// Token: 0x17000115 RID: 277
	// (get) Token: 0x06000538 RID: 1336 RVA: 0x000177DD File Offset: 0x000159DD
	public static BlackScreen BlackScreen
	{
		get
		{
			return GameManager.Instance.blackScreen;
		}
	}

	// Token: 0x17000116 RID: 278
	// (get) Token: 0x06000539 RID: 1337 RVA: 0x000177E9 File Offset: 0x000159E9
	public static EventSystem EventSystem
	{
		get
		{
			return GameManager.Instance.eventSystem;
		}
	}

	// Token: 0x17000117 RID: 279
	// (get) Token: 0x0600053A RID: 1338 RVA: 0x000177F5 File Offset: 0x000159F5
	public static NightVisionVisual NightVision
	{
		get
		{
			return GameManager.Instance.nightVision;
		}
	}

	// Token: 0x17000118 RID: 280
	// (get) Token: 0x0600053B RID: 1339 RVA: 0x00017801 File Offset: 0x00015A01
	public static bool BloodFxOn
	{
		get
		{
			return GameMetaData.BloodFxOn;
		}
	}

	// Token: 0x17000119 RID: 281
	// (get) Token: 0x0600053C RID: 1340 RVA: 0x00017808 File Offset: 0x00015A08
	public static PlayerInput MainPlayerInput
	{
		get
		{
			return GameManager.Instance.mainPlayerInput;
		}
	}

	// Token: 0x1700011A RID: 282
	// (get) Token: 0x0600053D RID: 1341 RVA: 0x00017814 File Offset: 0x00015A14
	public static ModManager ModManager
	{
		get
		{
			return GameManager.Instance.modManager;
		}
	}

	// Token: 0x1700011B RID: 283
	// (get) Token: 0x0600053E RID: 1342 RVA: 0x00017820 File Offset: 0x00015A20
	public static NoteIndex NoteIndex
	{
		get
		{
			return GameManager.Instance.noteIndex;
		}
	}

	// Token: 0x1700011C RID: 284
	// (get) Token: 0x0600053F RID: 1343 RVA: 0x0001782C File Offset: 0x00015A2C
	public static AchievementManager AchievementManager
	{
		get
		{
			return GameManager.Instance.achievementManager;
		}
	}

	// Token: 0x06000540 RID: 1344 RVA: 0x00017838 File Offset: 0x00015A38
	private void Awake()
	{
		if (GameManager._instance == null)
		{
			GameManager._instance = this;
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		}
		else if (GameManager._instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		DOTween.defaultTimeScaleIndependent = true;
		DebugManager.instance.enableRuntimeUI = false;
		DebugManager.instance.displayRuntimeUI = false;
	}

	// Token: 0x06000541 RID: 1345 RVA: 0x00017899 File Offset: 0x00015A99
	private void Update()
	{
		bool isEditor = Application.isEditor;
	}

	// Token: 0x06000542 RID: 1346 RVA: 0x000178A1 File Offset: 0x00015AA1
	public static void TimeTravelDetected()
	{
		Debug.Log("检测到穿越者");
	}

	// Token: 0x040004A9 RID: 1193
	private static GameManager _instance;

	// Token: 0x040004AA RID: 1194
	[SerializeField]
	private AudioManager audioManager;

	// Token: 0x040004AB RID: 1195
	[SerializeField]
	private UIInputManager uiInputManager;

	// Token: 0x040004AC RID: 1196
	[SerializeField]
	private GameRulesManager difficultyManager;

	// Token: 0x040004AD RID: 1197
	[SerializeField]
	private PauseMenu pauseMenu;

	// Token: 0x040004AE RID: 1198
	[SerializeField]
	private SceneLoader sceneLoader;

	// Token: 0x040004AF RID: 1199
	[SerializeField]
	private BlackScreen blackScreen;

	// Token: 0x040004B0 RID: 1200
	[SerializeField]
	private EventSystem eventSystem;

	// Token: 0x040004B1 RID: 1201
	[SerializeField]
	private PlayerInput mainPlayerInput;

	// Token: 0x040004B2 RID: 1202
	[SerializeField]
	private NightVisionVisual nightVision;

	// Token: 0x040004B3 RID: 1203
	[SerializeField]
	private ModManager modManager;

	// Token: 0x040004B4 RID: 1204
	[SerializeField]
	private NoteIndex noteIndex;

	// Token: 0x040004B5 RID: 1205
	[SerializeField]
	private AchievementManager achievementManager;

	// Token: 0x040004B6 RID: 1206
	public static bool newBoot;
}
