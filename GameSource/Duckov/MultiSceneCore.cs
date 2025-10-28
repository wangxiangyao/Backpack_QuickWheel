using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Duckov.MiniMaps;
using Duckov.Utilities;
using Eflatun.SceneReference;
using Saves;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Duckov.Scenes
{
	// Token: 0x0200032C RID: 812
	public class MultiSceneCore : MonoBehaviour
	{
		// Token: 0x17000501 RID: 1281
		// (get) Token: 0x06001B6D RID: 7021 RVA: 0x00063CDC File Offset: 0x00061EDC
		// (set) Token: 0x06001B6E RID: 7022 RVA: 0x00063CE3 File Offset: 0x00061EE3
		public static MultiSceneCore Instance { get; private set; }

		// Token: 0x17000502 RID: 1282
		// (get) Token: 0x06001B6F RID: 7023 RVA: 0x00063CEB File Offset: 0x00061EEB
		public List<SubSceneEntry> SubScenes
		{
			get
			{
				return this.subScenes;
			}
		}

		// Token: 0x17000503 RID: 1283
		// (get) Token: 0x06001B70 RID: 7024 RVA: 0x00063CF4 File Offset: 0x00061EF4
		public static Scene? MainScene
		{
			get
			{
				if (MultiSceneCore.Instance == null)
				{
					return null;
				}
				return new Scene?(MultiSceneCore.Instance.gameObject.scene);
			}
		}

		// Token: 0x17000504 RID: 1284
		// (get) Token: 0x06001B71 RID: 7025 RVA: 0x00063D2C File Offset: 0x00061F2C
		public static string ActiveSubSceneID
		{
			get
			{
				if (MultiSceneCore.ActiveSubScene == null)
				{
					return null;
				}
				if (MultiSceneCore.Instance == null)
				{
					return null;
				}
				SubSceneEntry subSceneEntry = MultiSceneCore.Instance.SubScenes.Find((SubSceneEntry e) => e != null && MultiSceneCore.ActiveSubScene.Value.buildIndex == e.Info.BuildIndex);
				if (subSceneEntry == null)
				{
					return null;
				}
				return subSceneEntry.sceneID;
			}
		}

		// Token: 0x17000505 RID: 1285
		// (get) Token: 0x06001B72 RID: 7026 RVA: 0x00063D94 File Offset: 0x00061F94
		public static Scene? ActiveSubScene
		{
			get
			{
				if (MultiSceneCore.Instance == null)
				{
					return null;
				}
				if (MultiSceneCore.Instance.isLoading)
				{
					return null;
				}
				return new Scene?(MultiSceneCore.Instance.activeSubScene);
			}
		}

		// Token: 0x140000BF RID: 191
		// (add) Token: 0x06001B73 RID: 7027 RVA: 0x00063DE0 File Offset: 0x00061FE0
		// (remove) Token: 0x06001B74 RID: 7028 RVA: 0x00063E14 File Offset: 0x00062014
		public static event Action<MultiSceneCore, Scene> OnSubSceneWillBeUnloaded;

		// Token: 0x140000C0 RID: 192
		// (add) Token: 0x06001B75 RID: 7029 RVA: 0x00063E48 File Offset: 0x00062048
		// (remove) Token: 0x06001B76 RID: 7030 RVA: 0x00063E7C File Offset: 0x0006207C
		public static event Action<MultiSceneCore, Scene> OnSubSceneLoaded;

		// Token: 0x17000506 RID: 1286
		// (get) Token: 0x06001B77 RID: 7031 RVA: 0x00063EB0 File Offset: 0x000620B0
		public SceneInfoEntry SceneInfo
		{
			get
			{
				return SceneInfoCollection.GetSceneInfo(base.gameObject.scene.buildIndex);
			}
		}

		// Token: 0x17000507 RID: 1287
		// (get) Token: 0x06001B78 RID: 7032 RVA: 0x00063ED8 File Offset: 0x000620D8
		public string DisplayName
		{
			get
			{
				SceneInfoEntry sceneInfo = SceneInfoCollection.GetSceneInfo(base.gameObject.scene.buildIndex);
				if (sceneInfo == null)
				{
					return "?";
				}
				return sceneInfo.DisplayName;
			}
		}

		// Token: 0x17000508 RID: 1288
		// (get) Token: 0x06001B79 RID: 7033 RVA: 0x00063F10 File Offset: 0x00062110
		public string DisplaynameRaw
		{
			get
			{
				SceneInfoEntry sceneInfo = SceneInfoCollection.GetSceneInfo(base.gameObject.scene.buildIndex);
				if (sceneInfo == null)
				{
					return "?";
				}
				return sceneInfo.DisplayNameRaw;
			}
		}

		// Token: 0x06001B7A RID: 7034 RVA: 0x00063F48 File Offset: 0x00062148
		public static void MoveToActiveWithScene(GameObject go, int sceneBuildIndex)
		{
			if (MultiSceneCore.Instance == null)
			{
				return;
			}
			Transform setActiveWithSceneParent = MultiSceneCore.Instance.GetSetActiveWithSceneParent(sceneBuildIndex);
			go.transform.SetParent(setActiveWithSceneParent);
		}

		// Token: 0x06001B7B RID: 7035 RVA: 0x00063F7C File Offset: 0x0006217C
		public static void MoveToActiveWithScene(GameObject go)
		{
			int buildIndex = go.scene.buildIndex;
			MultiSceneCore.MoveToActiveWithScene(go, buildIndex);
		}

		// Token: 0x06001B7C RID: 7036 RVA: 0x00063FA0 File Offset: 0x000621A0
		public Transform GetSetActiveWithSceneParent(int sceneBuildIndex)
		{
			GameObject gameObject;
			if (this.setActiveWithSceneObjects.TryGetValue(sceneBuildIndex, out gameObject))
			{
				return gameObject.transform;
			}
			SceneInfoEntry sceneInfoEntry = SceneInfoCollection.GetSceneInfo(sceneBuildIndex);
			if (sceneInfoEntry == null)
			{
				sceneInfoEntry = new SceneInfoEntry();
				Debug.LogWarning(string.Format("BuildIndex {0} 的sceneInfo不存在", sceneBuildIndex));
			}
			GameObject gameObject2 = new GameObject(sceneInfoEntry.ID);
			gameObject2.transform.SetParent(base.transform);
			this.setActiveWithSceneObjects.Add(sceneBuildIndex, gameObject2);
			gameObject2.SetActive(sceneInfoEntry.IsLoaded);
			return gameObject2.transform;
		}

		// Token: 0x140000C1 RID: 193
		// (add) Token: 0x06001B7D RID: 7037 RVA: 0x00064028 File Offset: 0x00062228
		// (remove) Token: 0x06001B7E RID: 7038 RVA: 0x0006405C File Offset: 0x0006225C
		public static event Action<MultiSceneCore> OnInstanceAwake;

		// Token: 0x140000C2 RID: 194
		// (add) Token: 0x06001B7F RID: 7039 RVA: 0x00064090 File Offset: 0x00062290
		// (remove) Token: 0x06001B80 RID: 7040 RVA: 0x000640C4 File Offset: 0x000622C4
		public static event Action<MultiSceneCore> OnInstanceDestroy;

		// Token: 0x140000C3 RID: 195
		// (add) Token: 0x06001B81 RID: 7041 RVA: 0x000640F8 File Offset: 0x000622F8
		// (remove) Token: 0x06001B82 RID: 7042 RVA: 0x0006412C File Offset: 0x0006232C
		public static event Action<string> OnSetSceneVisited;

		// Token: 0x06001B83 RID: 7043 RVA: 0x00064160 File Offset: 0x00062360
		private void Awake()
		{
			if (MultiSceneCore.Instance == null)
			{
				MultiSceneCore.Instance = this;
			}
			else
			{
				Debug.LogError("Multiple Multi Scene Core detected!");
			}
			Action<MultiSceneCore> onInstanceAwake = MultiSceneCore.OnInstanceAwake;
			if (onInstanceAwake != null)
			{
				onInstanceAwake(this);
			}
			if (this.playAfterLevelInit)
			{
				if (LevelManager.AfterInit)
				{
					this.PlayStinger();
					return;
				}
				LevelManager.OnAfterLevelInitialized += this.OnAfterLevelInitialized;
			}
		}

		// Token: 0x06001B84 RID: 7044 RVA: 0x000641C4 File Offset: 0x000623C4
		private void OnDestroy()
		{
			Action<MultiSceneCore> onInstanceDestroy = MultiSceneCore.OnInstanceDestroy;
			if (onInstanceDestroy != null)
			{
				onInstanceDestroy(this);
			}
			LevelManager.OnAfterLevelInitialized -= this.OnAfterLevelInitialized;
		}

		// Token: 0x06001B85 RID: 7045 RVA: 0x000641E8 File Offset: 0x000623E8
		private void OnAfterLevelInitialized()
		{
			if (this.playAfterLevelInit)
			{
				this.PlayStinger();
			}
		}

		// Token: 0x06001B86 RID: 7046 RVA: 0x000641F8 File Offset: 0x000623F8
		public void PlayStinger()
		{
			if (!string.IsNullOrWhiteSpace(this.playStinger))
			{
				AudioManager.PlayStringer(this.playStinger);
			}
		}

		// Token: 0x06001B87 RID: 7047 RVA: 0x00064214 File Offset: 0x00062414
		private void Start()
		{
			this.CreatePointsOfInterestsForLocations();
			AudioManager.StopBGM();
			AudioManager.SetState("Level", this.levelStateName);
			if (this.SceneInfo != null && !string.IsNullOrEmpty(this.SceneInfo.ID))
			{
				MultiSceneCore.SetVisited(this.SceneInfo.ID);
			}
		}

		// Token: 0x06001B88 RID: 7048 RVA: 0x00064266 File Offset: 0x00062466
		public static void SetVisited(string sceneID)
		{
			SavesSystem.Save<bool>("MultiSceneCore_Visited_" + sceneID, true);
			Action<string> onSetSceneVisited = MultiSceneCore.OnSetSceneVisited;
			if (onSetSceneVisited == null)
			{
				return;
			}
			onSetSceneVisited(sceneID);
		}

		// Token: 0x06001B89 RID: 7049 RVA: 0x00064289 File Offset: 0x00062489
		public static bool GetVisited(string sceneID)
		{
			return SavesSystem.Load<bool>("MultiSceneCore_Visited_" + sceneID);
		}

		// Token: 0x06001B8A RID: 7050 RVA: 0x0006429C File Offset: 0x0006249C
		private void CreatePointsOfInterestsForLocations()
		{
			foreach (SubSceneEntry subSceneEntry in this.SubScenes)
			{
				foreach (SubSceneEntry.Location location in subSceneEntry.cachedLocations)
				{
					if (location.showInMap)
					{
						SimplePointOfInterest.Create(location.position, subSceneEntry.sceneID, location.DisplayNameRaw, null, true);
					}
				}
			}
		}

		// Token: 0x06001B8B RID: 7051 RVA: 0x00064348 File Offset: 0x00062548
		private void CreatePointsOfInterestsForTeleporters()
		{
			foreach (SubSceneEntry subSceneEntry in this.SubScenes)
			{
				foreach (SubSceneEntry.TeleporterInfo teleporterInfo in subSceneEntry.cachedTeleporters)
				{
					SimplePointOfInterest.Create(teleporterInfo.position, subSceneEntry.sceneID, "", GameplayDataSettings.UIStyle.DefaultTeleporterIcon, false).ScaleFactor = GameplayDataSettings.UIStyle.TeleporterIconScale;
				}
			}
		}

		// Token: 0x06001B8C RID: 7052 RVA: 0x00064400 File Offset: 0x00062600
		public void BeginLoadSubScene(SceneReference reference)
		{
			this.LoadSubScene(reference, true).Forget<bool>();
		}

		// Token: 0x17000509 RID: 1289
		// (get) Token: 0x06001B8D RID: 7053 RVA: 0x0006440F File Offset: 0x0006260F
		public bool IsLoading
		{
			get
			{
				return this.isLoading;
			}
		}

		// Token: 0x1700050A RID: 1290
		// (get) Token: 0x06001B8E RID: 7054 RVA: 0x00064418 File Offset: 0x00062618
		public static string MainSceneID
		{
			get
			{
				return SceneInfoCollection.GetSceneID(MultiSceneCore.MainScene.Value.buildIndex);
			}
		}

		// Token: 0x06001B8F RID: 7055 RVA: 0x00064440 File Offset: 0x00062640
		private SceneReference GetSubSceneReference(string sceneID)
		{
			SubSceneEntry subSceneEntry = this.subScenes.Find((SubSceneEntry e) => e.sceneID == sceneID);
			if (subSceneEntry == null)
			{
				return null;
			}
			return subSceneEntry.SceneReference;
		}

		// Token: 0x06001B90 RID: 7056 RVA: 0x00064480 File Offset: 0x00062680
		private UniTask<bool> LoadSubScene(SceneReference targetScene, bool withBlackScreen = true)
		{
			MultiSceneCore.<LoadSubScene>d__62 <LoadSubScene>d__;
			<LoadSubScene>d__.<>t__builder = AsyncUniTaskMethodBuilder<bool>.Create();
			<LoadSubScene>d__.<>4__this = this;
			<LoadSubScene>d__.targetScene = targetScene;
			<LoadSubScene>d__.withBlackScreen = withBlackScreen;
			<LoadSubScene>d__.<>1__state = -1;
			<LoadSubScene>d__.<>t__builder.Start<MultiSceneCore.<LoadSubScene>d__62>(ref <LoadSubScene>d__);
			return <LoadSubScene>d__.<>t__builder.Task;
		}

		// Token: 0x06001B91 RID: 7057 RVA: 0x000644D4 File Offset: 0x000626D4
		private void LocalOnSubSceneWillBeUnloaded(Scene scene)
		{
			this.subScenes.Find((SubSceneEntry e) => e != null && e.Info.BuildIndex == scene.buildIndex);
			Transform setActiveWithSceneParent = this.GetSetActiveWithSceneParent(scene.buildIndex);
			Debug.Log(string.Format("Setting Active False {0}  {1}", setActiveWithSceneParent.name, scene.buildIndex));
			setActiveWithSceneParent.gameObject.SetActive(false);
		}

		// Token: 0x06001B92 RID: 7058 RVA: 0x0006454C File Offset: 0x0006274C
		private void LocalOnSubSceneLoaded(Scene scene)
		{
			this.subScenes.Find((SubSceneEntry e) => e != null && e.Info.BuildIndex == scene.buildIndex);
			this.GetSetActiveWithSceneParent(scene.buildIndex).gameObject.SetActive(true);
		}

		// Token: 0x06001B93 RID: 7059 RVA: 0x0006459C File Offset: 0x0006279C
		public UniTask<bool> LoadAndTeleport(MultiSceneLocation location)
		{
			MultiSceneCore.<LoadAndTeleport>d__65 <LoadAndTeleport>d__;
			<LoadAndTeleport>d__.<>t__builder = AsyncUniTaskMethodBuilder<bool>.Create();
			<LoadAndTeleport>d__.<>4__this = this;
			<LoadAndTeleport>d__.location = location;
			<LoadAndTeleport>d__.<>1__state = -1;
			<LoadAndTeleport>d__.<>t__builder.Start<MultiSceneCore.<LoadAndTeleport>d__65>(ref <LoadAndTeleport>d__);
			return <LoadAndTeleport>d__.<>t__builder.Task;
		}

		// Token: 0x06001B94 RID: 7060 RVA: 0x000645E8 File Offset: 0x000627E8
		public UniTask<bool> LoadAndTeleport(string sceneID, Vector3 position, bool subSceneLocation = false)
		{
			MultiSceneCore.<LoadAndTeleport>d__66 <LoadAndTeleport>d__;
			<LoadAndTeleport>d__.<>t__builder = AsyncUniTaskMethodBuilder<bool>.Create();
			<LoadAndTeleport>d__.<>4__this = this;
			<LoadAndTeleport>d__.sceneID = sceneID;
			<LoadAndTeleport>d__.position = position;
			<LoadAndTeleport>d__.subSceneLocation = subSceneLocation;
			<LoadAndTeleport>d__.<>1__state = -1;
			<LoadAndTeleport>d__.<>t__builder.Start<MultiSceneCore.<LoadAndTeleport>d__66>(ref <LoadAndTeleport>d__);
			return <LoadAndTeleport>d__.<>t__builder.Task;
		}

		// Token: 0x06001B95 RID: 7061 RVA: 0x00064644 File Offset: 0x00062844
		public static void MoveToMainScene(GameObject gameObject)
		{
			if (MultiSceneCore.Instance == null)
			{
				Debug.LogError("移动到主场景失败，因为MultiSceneCore不存在");
				return;
			}
			SceneManager.MoveGameObjectToScene(gameObject, MultiSceneCore.MainScene.Value);
		}

		// Token: 0x06001B96 RID: 7062 RVA: 0x0006467C File Offset: 0x0006287C
		public void CacheLocations()
		{
		}

		// Token: 0x06001B97 RID: 7063 RVA: 0x0006467E File Offset: 0x0006287E
		public void CacheTeleporters()
		{
		}

		// Token: 0x06001B98 RID: 7064 RVA: 0x00064680 File Offset: 0x00062880
		private Vector3 GetClosestTeleporterPosition(Vector3 pos)
		{
			float num = float.MaxValue;
			Vector3 result = pos;
			foreach (SubSceneEntry subSceneEntry in this.subScenes)
			{
				foreach (SubSceneEntry.TeleporterInfo teleporterInfo in subSceneEntry.cachedTeleporters)
				{
					float magnitude = (teleporterInfo.position - pos).magnitude;
					if (magnitude < num)
					{
						num = magnitude;
						result = teleporterInfo.position;
					}
				}
			}
			return result;
		}

		// Token: 0x06001B99 RID: 7065 RVA: 0x00064738 File Offset: 0x00062938
		internal bool TryGetCachedPosition(MultiSceneLocation location, out Vector3 result)
		{
			return this.TryGetCachedPosition(location.SceneID, location.LocationName, out result);
		}

		// Token: 0x06001B9A RID: 7066 RVA: 0x00064750 File Offset: 0x00062950
		internal bool TryGetCachedPosition(string sceneID, string locationName, out Vector3 result)
		{
			result = default(Vector3);
			SubSceneEntry subSceneEntry = this.subScenes.Find((SubSceneEntry e) => e != null && e.sceneID == sceneID);
			return subSceneEntry != null && subSceneEntry.TryGetCachedPosition(locationName, out result);
		}

		// Token: 0x06001B9B RID: 7067 RVA: 0x0006479C File Offset: 0x0006299C
		internal SubSceneEntry GetSubSceneInfo(Scene scene)
		{
			return this.subScenes.Find((SubSceneEntry e) => e != null && e.Info != null && e.Info.BuildIndex == scene.buildIndex);
		}

		// Token: 0x06001B9C RID: 7068 RVA: 0x000647CD File Offset: 0x000629CD
		public SubSceneEntry GetSubSceneInfo()
		{
			return this.cachedSubsceneEntry;
		}

		// Token: 0x04001385 RID: 4997
		[SerializeField]
		private string levelStateName = "None";

		// Token: 0x04001386 RID: 4998
		[SerializeField]
		private string playStinger = "";

		// Token: 0x04001387 RID: 4999
		[SerializeField]
		private bool playAfterLevelInit;

		// Token: 0x04001388 RID: 5000
		[SerializeField]
		private List<SubSceneEntry> subScenes;

		// Token: 0x04001389 RID: 5001
		private Scene activeSubScene;

		// Token: 0x0400138A RID: 5002
		[HideInInspector]
		public List<int> usedCreatorIds = new List<int>();

		// Token: 0x0400138B RID: 5003
		[HideInInspector]
		public Dictionary<int, object> inLevelData = new Dictionary<int, object>();

		// Token: 0x0400138E RID: 5006
		[SerializeField]
		private bool teleportToRandomOnLevelInitialized;

		// Token: 0x0400138F RID: 5007
		private Dictionary<int, GameObject> setActiveWithSceneObjects = new Dictionary<int, GameObject>();

		// Token: 0x04001393 RID: 5011
		private bool isLoading;

		// Token: 0x04001394 RID: 5012
		private SubSceneEntry cachedSubsceneEntry;
	}
}
