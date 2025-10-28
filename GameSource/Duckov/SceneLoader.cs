using System;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Duckov;
using Duckov.Scenes;
using Duckov.UI.Animations;
using Duckov.Utilities;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Token: 0x0200012B RID: 299
public class SceneLoader : MonoBehaviour
{
	// Token: 0x170001FE RID: 510
	// (get) Token: 0x060009BA RID: 2490 RVA: 0x0002A0E9 File Offset: 0x000282E9
	public static SceneLoader Instance
	{
		get
		{
			return GameManager.SceneLoader;
		}
	}

	// Token: 0x170001FF RID: 511
	// (get) Token: 0x060009BB RID: 2491 RVA: 0x0002A0F0 File Offset: 0x000282F0
	// (set) Token: 0x060009BC RID: 2492 RVA: 0x0002A0F7 File Offset: 0x000282F7
	public static bool IsSceneLoading { get; private set; }

	// Token: 0x14000048 RID: 72
	// (add) Token: 0x060009BD RID: 2493 RVA: 0x0002A100 File Offset: 0x00028300
	// (remove) Token: 0x060009BE RID: 2494 RVA: 0x0002A134 File Offset: 0x00028334
	public static event Action<SceneLoadingContext> onStartedLoadingScene;

	// Token: 0x14000049 RID: 73
	// (add) Token: 0x060009BF RID: 2495 RVA: 0x0002A168 File Offset: 0x00028368
	// (remove) Token: 0x060009C0 RID: 2496 RVA: 0x0002A19C File Offset: 0x0002839C
	public static event Action<SceneLoadingContext> onFinishedLoadingScene;

	// Token: 0x1400004A RID: 74
	// (add) Token: 0x060009C1 RID: 2497 RVA: 0x0002A1D0 File Offset: 0x000283D0
	// (remove) Token: 0x060009C2 RID: 2498 RVA: 0x0002A204 File Offset: 0x00028404
	public static event Action<SceneLoadingContext> onBeforeSetSceneActive;

	// Token: 0x1400004B RID: 75
	// (add) Token: 0x060009C3 RID: 2499 RVA: 0x0002A238 File Offset: 0x00028438
	// (remove) Token: 0x060009C4 RID: 2500 RVA: 0x0002A26C File Offset: 0x0002846C
	public static event Action<SceneLoadingContext> onAfterSceneInitialize;

	// Token: 0x17000200 RID: 512
	// (get) Token: 0x060009C5 RID: 2501 RVA: 0x0002A29F File Offset: 0x0002849F
	// (set) Token: 0x060009C6 RID: 2502 RVA: 0x0002A2C7 File Offset: 0x000284C7
	public static string LoadingComment
	{
		get
		{
			if (LevelManager.LevelInitializing)
			{
				return LevelManager.LevelInitializingComment;
			}
			if (SceneLoader.Instance != null)
			{
				return SceneLoader.Instance._loadingComment;
			}
			return null;
		}
		set
		{
			if (SceneLoader.Instance == null)
			{
				return;
			}
			SceneLoader.Instance._loadingComment = value;
			Action<string> onSetLoadingComment = SceneLoader.OnSetLoadingComment;
			if (onSetLoadingComment == null)
			{
				return;
			}
			onSetLoadingComment(value);
		}
	}

	// Token: 0x1400004C RID: 76
	// (add) Token: 0x060009C7 RID: 2503 RVA: 0x0002A2F4 File Offset: 0x000284F4
	// (remove) Token: 0x060009C8 RID: 2504 RVA: 0x0002A328 File Offset: 0x00028528
	public static event Action<string> OnSetLoadingComment;

	// Token: 0x060009C9 RID: 2505 RVA: 0x0002A35C File Offset: 0x0002855C
	private void Awake()
	{
		if (SceneLoader.Instance != this)
		{
			Debug.LogError(base.gameObject.scene.name + " 场景中出现了应当删除的Scene Loader");
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		this.pointerClickEventRecevier.onPointerClick.AddListener(new UnityAction<PointerEventData>(this.NotifyPointerClick));
		this.pointerClickEventRecevier.gameObject.SetActive(false);
		this.content.Hide();
	}

	// Token: 0x060009CA RID: 2506 RVA: 0x0002A3DC File Offset: 0x000285DC
	public UniTask LoadScene(string sceneID, MultiSceneLocation location, SceneReference overrideCurtainScene = null, bool clickToConinue = false, bool notifyEvacuation = false, bool doCircleFade = true, bool saveToFile = true, bool hideTips = false)
	{
		SceneLoader.<LoadScene>d__39 <LoadScene>d__;
		<LoadScene>d__.<>t__builder = AsyncUniTaskMethodBuilder.Create();
		<LoadScene>d__.<>4__this = this;
		<LoadScene>d__.sceneID = sceneID;
		<LoadScene>d__.location = location;
		<LoadScene>d__.overrideCurtainScene = overrideCurtainScene;
		<LoadScene>d__.clickToConinue = clickToConinue;
		<LoadScene>d__.notifyEvacuation = notifyEvacuation;
		<LoadScene>d__.doCircleFade = doCircleFade;
		<LoadScene>d__.saveToFile = saveToFile;
		<LoadScene>d__.hideTips = hideTips;
		<LoadScene>d__.<>1__state = -1;
		<LoadScene>d__.<>t__builder.Start<SceneLoader.<LoadScene>d__39>(ref <LoadScene>d__);
		return <LoadScene>d__.<>t__builder.Task;
	}

	// Token: 0x060009CB RID: 2507 RVA: 0x0002A464 File Offset: 0x00028664
	public UniTask LoadScene(string sceneID, SceneReference overrideCurtainScene = null, bool clickToConinue = false, bool notifyEvacuation = false, bool doCircleFade = true, bool useLocation = false, MultiSceneLocation location = default(MultiSceneLocation), bool saveToFile = true, bool hideTips = false)
	{
		SceneLoader.<LoadScene>d__40 <LoadScene>d__;
		<LoadScene>d__.<>t__builder = AsyncUniTaskMethodBuilder.Create();
		<LoadScene>d__.<>4__this = this;
		<LoadScene>d__.sceneID = sceneID;
		<LoadScene>d__.overrideCurtainScene = overrideCurtainScene;
		<LoadScene>d__.clickToConinue = clickToConinue;
		<LoadScene>d__.notifyEvacuation = notifyEvacuation;
		<LoadScene>d__.doCircleFade = doCircleFade;
		<LoadScene>d__.useLocation = useLocation;
		<LoadScene>d__.location = location;
		<LoadScene>d__.saveToFile = saveToFile;
		<LoadScene>d__.hideTips = hideTips;
		<LoadScene>d__.<>1__state = -1;
		<LoadScene>d__.<>t__builder.Start<SceneLoader.<LoadScene>d__40>(ref <LoadScene>d__);
		return <LoadScene>d__.<>t__builder.Task;
	}

	// Token: 0x17000201 RID: 513
	// (get) Token: 0x060009CC RID: 2508 RVA: 0x0002A4F5 File Offset: 0x000286F5
	// (set) Token: 0x060009CD RID: 2509 RVA: 0x0002A4FC File Offset: 0x000286FC
	public static bool HideTips { get; private set; }

	// Token: 0x060009CE RID: 2510 RVA: 0x0002A504 File Offset: 0x00028704
	public UniTask LoadScene(SceneReference sceneReference, SceneReference overrideCurtainScene = null, bool clickToConinue = false, bool notifyEvacuation = false, bool doCircleFade = true, bool useLocation = false, MultiSceneLocation location = default(MultiSceneLocation), bool saveToFile = true, bool hideTips = false)
	{
		SceneLoader.<LoadScene>d__45 <LoadScene>d__;
		<LoadScene>d__.<>t__builder = AsyncUniTaskMethodBuilder.Create();
		<LoadScene>d__.<>4__this = this;
		<LoadScene>d__.sceneReference = sceneReference;
		<LoadScene>d__.overrideCurtainScene = overrideCurtainScene;
		<LoadScene>d__.clickToConinue = clickToConinue;
		<LoadScene>d__.notifyEvacuation = notifyEvacuation;
		<LoadScene>d__.doCircleFade = doCircleFade;
		<LoadScene>d__.useLocation = useLocation;
		<LoadScene>d__.location = location;
		<LoadScene>d__.saveToFile = saveToFile;
		<LoadScene>d__.hideTips = hideTips;
		<LoadScene>d__.<>1__state = -1;
		<LoadScene>d__.<>t__builder.Start<SceneLoader.<LoadScene>d__45>(ref <LoadScene>d__);
		return <LoadScene>d__.<>t__builder.Task;
	}

	// Token: 0x060009CF RID: 2511 RVA: 0x0002A598 File Offset: 0x00028798
	public void LoadTarget()
	{
		this.LoadScene(this.target, null, false, false, true, false, default(MultiSceneLocation), true, false).Forget();
	}

	// Token: 0x060009D0 RID: 2512 RVA: 0x0002A5C8 File Offset: 0x000287C8
	public UniTask LoadBaseScene(SceneReference overrideCurtainScene = null, bool doCircleFade = true)
	{
		SceneLoader.<LoadBaseScene>d__47 <LoadBaseScene>d__;
		<LoadBaseScene>d__.<>t__builder = AsyncUniTaskMethodBuilder.Create();
		<LoadBaseScene>d__.<>4__this = this;
		<LoadBaseScene>d__.overrideCurtainScene = overrideCurtainScene;
		<LoadBaseScene>d__.doCircleFade = doCircleFade;
		<LoadBaseScene>d__.<>1__state = -1;
		<LoadBaseScene>d__.<>t__builder.Start<SceneLoader.<LoadBaseScene>d__47>(ref <LoadBaseScene>d__);
		return <LoadBaseScene>d__.<>t__builder.Task;
	}

	// Token: 0x060009D1 RID: 2513 RVA: 0x0002A61B File Offset: 0x0002881B
	public void NotifyPointerClick(PointerEventData eventData)
	{
		this.clicked = true;
		AudioManager.Post("UI/sceneloader_click");
	}

	// Token: 0x060009D2 RID: 2514 RVA: 0x0002A62F File Offset: 0x0002882F
	internal static void StaticLoadSingle(SceneReference sceneReference)
	{
		SceneManager.LoadScene(sceneReference.Name, LoadSceneMode.Single);
	}

	// Token: 0x060009D3 RID: 2515 RVA: 0x0002A63D File Offset: 0x0002883D
	internal static void StaticLoadSingle(string sceneID)
	{
		SceneManager.LoadScene(SceneInfoCollection.GetBuildIndex(sceneID), LoadSceneMode.Single);
	}

	// Token: 0x060009D4 RID: 2516 RVA: 0x0002A64C File Offset: 0x0002884C
	public static void LoadMainMenu(bool circleFade = true)
	{
		if (SceneLoader.Instance)
		{
			SceneLoader.Instance.LoadScene(GameplayDataSettings.SceneManagement.MainMenuScene, null, false, false, circleFade, false, default(MultiSceneLocation), true, false).Forget();
		}
	}

	// Token: 0x060009D6 RID: 2518 RVA: 0x0002A6AC File Offset: 0x000288AC
	[CompilerGenerated]
	internal static float <LoadScene>g__TimeSinceLoadingStarted|45_0(ref SceneLoader.<>c__DisplayClass45_0 A_0)
	{
		return Time.unscaledTime - A_0.timeWhenLoadingStarted;
	}

	// Token: 0x04000884 RID: 2180
	public SceneReference defaultCurtainScene;

	// Token: 0x04000885 RID: 2181
	[SerializeField]
	private OnPointerClick pointerClickEventRecevier;

	// Token: 0x04000886 RID: 2182
	[SerializeField]
	private float minimumLoadingTime = 1f;

	// Token: 0x04000887 RID: 2183
	[SerializeField]
	private float waitAfterSceneLoaded = 1f;

	// Token: 0x04000888 RID: 2184
	[SerializeField]
	private FadeGroup content;

	// Token: 0x04000889 RID: 2185
	[SerializeField]
	private FadeGroup loadingIndicator;

	// Token: 0x0400088A RID: 2186
	[SerializeField]
	private FadeGroup clickIndicator;

	// Token: 0x0400088B RID: 2187
	[SerializeField]
	private AnimationCurve fadeCurve1;

	// Token: 0x0400088C RID: 2188
	[SerializeField]
	private AnimationCurve fadeCurve2;

	// Token: 0x0400088D RID: 2189
	[SerializeField]
	private AnimationCurve fadeCurve3;

	// Token: 0x0400088E RID: 2190
	[SerializeField]
	private AnimationCurve fadeCurve4;

	// Token: 0x04000894 RID: 2196
	private string _loadingComment;

	// Token: 0x04000896 RID: 2198
	[SerializeField]
	private SceneReference target;

	// Token: 0x04000897 RID: 2199
	private bool clicked;
}
