using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Duckov.UI.Animations;
using Duckov.Utilities;
using UnityEngine;

namespace Duckov.UI
{
	// Token: 0x020003A9 RID: 937
	public class GameplayUIManager : MonoBehaviour
	{
		// Token: 0x17000666 RID: 1638
		// (get) Token: 0x06002196 RID: 8598 RVA: 0x00075627 File Offset: 0x00073827
		public static GameplayUIManager Instance
		{
			get
			{
				return GameplayUIManager.instance;
			}
		}

		// Token: 0x17000667 RID: 1639
		// (get) Token: 0x06002197 RID: 8599 RVA: 0x0007562E File Offset: 0x0007382E
		public View ActiveView
		{
			get
			{
				return View.ActiveView;
			}
		}

		// Token: 0x06002198 RID: 8600 RVA: 0x00075638 File Offset: 0x00073838
		public static T GetViewInstance<T>() where T : View
		{
			if (GameplayUIManager.Instance == null)
			{
				return default(T);
			}
			View view;
			if (GameplayUIManager.Instance.viewDic.TryGetValue(typeof(T), out view))
			{
				return view as T;
			}
			View view2 = GameplayUIManager.Instance.views.Find((View e) => e is T);
			if (view2 == null)
			{
				return default(T);
			}
			GameplayUIManager.Instance.viewDic[typeof(T)] = view2;
			return view2 as T;
		}

		// Token: 0x06002199 RID: 8601 RVA: 0x000756EC File Offset: 0x000738EC
		private void Awake()
		{
			if (GameplayUIManager.instance == null)
			{
				GameplayUIManager.instance = this;
			}
			else
			{
				Debug.LogWarning("Duplicate Gameplay UI Manager detected!");
			}
			foreach (View view in this.views)
			{
				view.gameObject.SetActive(true);
			}
			foreach (GameObject gameObject in this.setActiveOnAwake)
			{
				if (!(gameObject == null))
				{
					gameObject.gameObject.SetActive(true);
				}
			}
		}

		// Token: 0x17000668 RID: 1640
		// (get) Token: 0x0600219A RID: 8602 RVA: 0x000757B4 File Offset: 0x000739B4
		public PrefabPool<ItemDisplay> ItemDisplayPool
		{
			get
			{
				if (this.itemDisplayPool == null)
				{
					this.itemDisplayPool = new PrefabPool<ItemDisplay>(GameplayDataSettings.UIPrefabs.ItemDisplay, base.transform, null, null, null, true, 10, 10000, null);
				}
				return this.itemDisplayPool;
			}
		}

		// Token: 0x17000669 RID: 1641
		// (get) Token: 0x0600219B RID: 8603 RVA: 0x000757F8 File Offset: 0x000739F8
		public PrefabPool<SlotDisplay> SlotDisplayPool
		{
			get
			{
				if (this.slotDisplayPool == null)
				{
					this.slotDisplayPool = new PrefabPool<SlotDisplay>(GameplayDataSettings.UIPrefabs.SlotDisplay, base.transform, null, null, null, true, 10, 10000, null);
				}
				return this.slotDisplayPool;
			}
		}

		// Token: 0x1700066A RID: 1642
		// (get) Token: 0x0600219C RID: 8604 RVA: 0x0007583C File Offset: 0x00073A3C
		public PrefabPool<InventoryEntry> InventoryEntryPool
		{
			get
			{
				if (this.inventoryEntryPool == null)
				{
					this.inventoryEntryPool = new PrefabPool<InventoryEntry>(GameplayDataSettings.UIPrefabs.InventoryEntry, base.transform, null, null, null, true, 10, 10000, null);
				}
				return this.inventoryEntryPool;
			}
		}

		// Token: 0x1700066B RID: 1643
		// (get) Token: 0x0600219D RID: 8605 RVA: 0x0007587E File Offset: 0x00073A7E
		public SplitDialogue SplitDialogue
		{
			get
			{
				return this._splitDialogue;
			}
		}

		// Token: 0x0600219E RID: 8606 RVA: 0x00075886 File Offset: 0x00073A86
		public static UniTask TemporaryHide()
		{
			if (GameplayUIManager.Instance == null)
			{
				return UniTask.CompletedTask;
			}
			GameplayUIManager.Instance.canvasGroup.blocksRaycasts = false;
			return GameplayUIManager.Instance.fadeGroup.HideAndReturnTask();
		}

		// Token: 0x0600219F RID: 8607 RVA: 0x000758BA File Offset: 0x00073ABA
		public static UniTask ReverseTemporaryHide()
		{
			if (GameplayUIManager.Instance == null)
			{
				return UniTask.CompletedTask;
			}
			GameplayUIManager.Instance.canvasGroup.blocksRaycasts = true;
			return GameplayUIManager.Instance.fadeGroup.ShowAndReturnTask();
		}

		// Token: 0x040016BE RID: 5822
		private static GameplayUIManager instance;

		// Token: 0x040016BF RID: 5823
		[SerializeField]
		private CanvasGroup canvasGroup;

		// Token: 0x040016C0 RID: 5824
		[SerializeField]
		private FadeGroup fadeGroup;

		// Token: 0x040016C1 RID: 5825
		[SerializeField]
		private List<View> views = new List<View>();

		// Token: 0x040016C2 RID: 5826
		[SerializeField]
		private List<GameObject> setActiveOnAwake;

		// Token: 0x040016C3 RID: 5827
		private Dictionary<Type, View> viewDic = new Dictionary<Type, View>();

		// Token: 0x040016C4 RID: 5828
		private PrefabPool<ItemDisplay> itemDisplayPool;

		// Token: 0x040016C5 RID: 5829
		private PrefabPool<SlotDisplay> slotDisplayPool;

		// Token: 0x040016C6 RID: 5830
		private PrefabPool<InventoryEntry> inventoryEntryPool;

		// Token: 0x040016C7 RID: 5831
		[SerializeField]
		private SplitDialogue _splitDialogue;
	}
}
