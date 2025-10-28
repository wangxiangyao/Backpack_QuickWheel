using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Duckov.UI.Animations;
using Duckov.Utilities;
using ItemStatsSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Duckov.UI
{
	// Token: 0x0200038F RID: 911
	public class InventoryDisplay : MonoBehaviour, IPoolable
	{
		// Token: 0x1700060F RID: 1551
		// (get) Token: 0x06001FBC RID: 8124 RVA: 0x0006F08F File Offset: 0x0006D28F
		private bool shortcuts
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000610 RID: 1552
		// (get) Token: 0x06001FBD RID: 8125 RVA: 0x0006F092 File Offset: 0x0006D292
		public bool UsePages
		{
			get
			{
				return this.usePages;
			}
		}

		// Token: 0x17000611 RID: 1553
		// (get) Token: 0x06001FBE RID: 8126 RVA: 0x0006F09A File Offset: 0x0006D29A
		// (set) Token: 0x06001FBF RID: 8127 RVA: 0x0006F0A2 File Offset: 0x0006D2A2
		public bool Editable
		{
			get
			{
				return this.editable;
			}
			internal set
			{
				this.editable = value;
			}
		}

		// Token: 0x17000612 RID: 1554
		// (get) Token: 0x06001FC0 RID: 8128 RVA: 0x0006F0AB File Offset: 0x0006D2AB
		// (set) Token: 0x06001FC1 RID: 8129 RVA: 0x0006F0B3 File Offset: 0x0006D2B3
		public bool ShowOperationButtons
		{
			get
			{
				return this.showOperationButtons;
			}
			internal set
			{
				this.showOperationButtons = value;
			}
		}

		// Token: 0x17000613 RID: 1555
		// (get) Token: 0x06001FC2 RID: 8130 RVA: 0x0006F0BC File Offset: 0x0006D2BC
		// (set) Token: 0x06001FC3 RID: 8131 RVA: 0x0006F0C4 File Offset: 0x0006D2C4
		public bool Movable { get; private set; }

		// Token: 0x17000614 RID: 1556
		// (get) Token: 0x06001FC4 RID: 8132 RVA: 0x0006F0CD File Offset: 0x0006D2CD
		// (set) Token: 0x06001FC5 RID: 8133 RVA: 0x0006F0D5 File Offset: 0x0006D2D5
		public Inventory Target { get; private set; }

		// Token: 0x17000615 RID: 1557
		// (get) Token: 0x06001FC6 RID: 8134 RVA: 0x0006F0E0 File Offset: 0x0006D2E0
		private PrefabPool<InventoryEntry> EntryPool
		{
			get
			{
				if (this._entryPool == null && this.entryPrefab != null)
				{
					this._entryPool = new PrefabPool<InventoryEntry>(this.entryPrefab, this.contentLayout.transform, null, null, null, true, 10, 10000, null);
				}
				return this._entryPool;
			}
		}

		// Token: 0x140000D8 RID: 216
		// (add) Token: 0x06001FC7 RID: 8135 RVA: 0x0006F134 File Offset: 0x0006D334
		// (remove) Token: 0x06001FC8 RID: 8136 RVA: 0x0006F16C File Offset: 0x0006D36C
		public event Action<InventoryDisplay, InventoryEntry, PointerEventData> onDisplayDoubleClicked;

		// Token: 0x140000D9 RID: 217
		// (add) Token: 0x06001FC9 RID: 8137 RVA: 0x0006F1A4 File Offset: 0x0006D3A4
		// (remove) Token: 0x06001FCA RID: 8138 RVA: 0x0006F1DC File Offset: 0x0006D3DC
		public event Action onPageInfoRefreshed;

		// Token: 0x17000616 RID: 1558
		// (get) Token: 0x06001FCB RID: 8139 RVA: 0x0006F211 File Offset: 0x0006D411
		public Func<Item, bool> Func_ShouldHighlight
		{
			get
			{
				return this._func_ShouldHighlight;
			}
		}

		// Token: 0x17000617 RID: 1559
		// (get) Token: 0x06001FCC RID: 8140 RVA: 0x0006F219 File Offset: 0x0006D419
		public Func<Item, bool> Func_CanOperate
		{
			get
			{
				return this._func_CanOperate;
			}
		}

		// Token: 0x17000618 RID: 1560
		// (get) Token: 0x06001FCD RID: 8141 RVA: 0x0006F221 File Offset: 0x0006D421
		// (set) Token: 0x06001FCE RID: 8142 RVA: 0x0006F229 File Offset: 0x0006D429
		public bool ShowSortButton
		{
			get
			{
				return this.showSortButton;
			}
			internal set
			{
				this.showSortButton = value;
			}
		}

		// Token: 0x06001FCF RID: 8143 RVA: 0x0006F234 File Offset: 0x0006D434
		private void RegisterEvents()
		{
			if (this.Target == null)
			{
				return;
			}
			this.UnregisterEvents();
			this.Target.onContentChanged += this.OnTargetContentChanged;
			this.Target.onInventorySorted += this.OnTargetSorted;
			this.Target.onSetIndexLock += this.OnTargetSetIndexLock;
		}

		// Token: 0x06001FD0 RID: 8144 RVA: 0x0006F29C File Offset: 0x0006D49C
		private void UnregisterEvents()
		{
			if (this.Target == null)
			{
				return;
			}
			this.Target.onContentChanged -= this.OnTargetContentChanged;
			this.Target.onInventorySorted -= this.OnTargetSorted;
			this.Target.onSetIndexLock -= this.OnTargetSetIndexLock;
		}

		// Token: 0x06001FD1 RID: 8145 RVA: 0x0006F300 File Offset: 0x0006D500
		private void OnTargetSetIndexLock(Inventory inventory, int index)
		{
			foreach (InventoryEntry inventoryEntry in this.entries)
			{
				if (!(inventoryEntry == null) && inventoryEntry.isActiveAndEnabled && inventoryEntry.Index == index)
				{
					inventoryEntry.Refresh();
				}
			}
		}

		// Token: 0x06001FD2 RID: 8146 RVA: 0x0006F36C File Offset: 0x0006D56C
		private void OnTargetSorted(Inventory inventory)
		{
			if (this.filter == null)
			{
				using (List<InventoryEntry>.Enumerator enumerator = this.entries.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						InventoryEntry inventoryEntry = enumerator.Current;
						inventoryEntry.Refresh();
					}
					return;
				}
			}
			this.LoadEntriesTask().Forget();
		}

		// Token: 0x06001FD3 RID: 8147 RVA: 0x0006F3D0 File Offset: 0x0006D5D0
		private void OnTargetContentChanged(Inventory inventory, int position)
		{
			if (this.Target.Loading)
			{
				return;
			}
			if (this.filter != null)
			{
				this.RefreshCapacityText();
				this.LoadEntriesTask().Forget();
				return;
			}
			this.RefreshCapacityText();
			InventoryEntry inventoryEntry = this.entries.Find((InventoryEntry e) => e != null && e.Index == position);
			if (!inventoryEntry)
			{
				return;
			}
			InventoryEntry inventoryEntry2 = inventoryEntry;
			inventoryEntry2.Refresh();
			inventoryEntry2.Punch();
		}

		// Token: 0x06001FD4 RID: 8148 RVA: 0x0006F448 File Offset: 0x0006D648
		private void RefreshCapacityText()
		{
			if (this.Target == null)
			{
				return;
			}
			if (!this.capacityText)
			{
				return;
			}
			this.capacityText.text = string.Format(this.capacityTextFormat, this.Target.Capacity, this.Target.GetItemCount());
		}

		// Token: 0x06001FD5 RID: 8149 RVA: 0x0006F4A8 File Offset: 0x0006D6A8
		public void Setup(Inventory target, Func<Item, bool> funcShouldHighLight = null, Func<Item, bool> funcCanOperate = null, bool movable = false, Func<Item, bool> filter = null)
		{
			this.UnregisterEvents();
			this.Target = target;
			this.Clear();
			if (this.Target == null)
			{
				return;
			}
			if (this.Target.Loading)
			{
				return;
			}
			if (funcShouldHighLight == null)
			{
				this._func_ShouldHighlight = ((Item e) => false);
			}
			else
			{
				this._func_ShouldHighlight = funcShouldHighLight;
			}
			if (funcCanOperate == null)
			{
				this._func_CanOperate = ((Item e) => true);
			}
			else
			{
				this._func_CanOperate = funcCanOperate;
			}
			this.displayNameText.text = target.DisplayName;
			this.Movable = movable;
			this.cachedCapacity = target.Capacity;
			this.filter = filter;
			this.RefreshCapacityText();
			this.RegisterEvents();
			this.sortButton.gameObject.SetActive(this.editable && this.showSortButton);
			this.LoadEntriesTask().Forget();
		}

		// Token: 0x06001FD6 RID: 8150 RVA: 0x0006F5AC File Offset: 0x0006D7AC
		private void RefreshGridLayoutPreferredHeight()
		{
			if (this.Target == null)
			{
				this.placeHolder.gameObject.SetActive(true);
				return;
			}
			int num = this.cachedIndexesToDisplay.Count;
			if (this.usePages && num > 0)
			{
				int num2 = this.cachedSelectedPage * this.itemsEachPage;
				int num3 = Mathf.Min(num2 + this.itemsEachPage, this.cachedIndexesToDisplay.Count);
				num = Mathf.Max(0, num3 - num2);
			}
			float preferredHeight = (float)Mathf.CeilToInt((float)num / (float)this.contentLayout.constraintCount) * this.contentLayout.cellSize.y + (float)this.contentLayout.padding.top + (float)this.contentLayout.padding.bottom;
			this.gridLayoutElement.preferredHeight = preferredHeight;
			this.placeHolder.gameObject.SetActive(num <= 0);
		}

		// Token: 0x17000619 RID: 1561
		// (get) Token: 0x06001FD7 RID: 8151 RVA: 0x0006F690 File Offset: 0x0006D890
		public int MaxPage
		{
			get
			{
				return this.cachedMaxPage;
			}
		}

		// Token: 0x1700061A RID: 1562
		// (get) Token: 0x06001FD8 RID: 8152 RVA: 0x0006F698 File Offset: 0x0006D898
		public int SelectedPage
		{
			get
			{
				return this.cachedSelectedPage;
			}
		}

		// Token: 0x06001FD9 RID: 8153 RVA: 0x0006F6A0 File Offset: 0x0006D8A0
		public void SetPage(int page)
		{
			this.cachedSelectedPage = page;
			Action action = this.onPageInfoRefreshed;
			if (action != null)
			{
				action();
			}
			this.LoadEntriesTask().Forget();
		}

		// Token: 0x06001FDA RID: 8154 RVA: 0x0006F6C8 File Offset: 0x0006D8C8
		public void NextPage()
		{
			int num = this.cachedSelectedPage + 1;
			if (num >= this.cachedMaxPage)
			{
				num = 0;
			}
			this.SetPage(num);
		}

		// Token: 0x06001FDB RID: 8155 RVA: 0x0006F6F0 File Offset: 0x0006D8F0
		public void PreviousPage()
		{
			int num = this.cachedSelectedPage - 1;
			if (num < 0)
			{
				num = this.cachedMaxPage - 1;
			}
			this.SetPage(num);
		}

		// Token: 0x06001FDC RID: 8156 RVA: 0x0006F71C File Offset: 0x0006D91C
		private void CacheIndexesToDisplay()
		{
			this.cachedIndexesToDisplay.Clear();
			int i = 0;
			while (i < this.Target.Capacity)
			{
				if (this.filter == null)
				{
					goto IL_32;
				}
				Item itemAt = this.Target.GetItemAt(i);
				if (this.filter(itemAt))
				{
					goto IL_32;
				}
				IL_3E:
				i++;
				continue;
				IL_32:
				this.cachedIndexesToDisplay.Add(i);
				goto IL_3E;
			}
			int count = this.cachedIndexesToDisplay.Count;
			this.cachedMaxPage = count / this.itemsEachPage + ((count % this.itemsEachPage > 0) ? 1 : 0);
			if (this.cachedSelectedPage >= this.cachedMaxPage)
			{
				this.cachedSelectedPage = Mathf.Max(0, this.cachedMaxPage - 1);
			}
			Action action = this.onPageInfoRefreshed;
			if (action == null)
			{
				return;
			}
			action();
		}

		// Token: 0x06001FDD RID: 8157 RVA: 0x0006F7D8 File Offset: 0x0006D9D8
		private UniTask LoadEntriesTask()
		{
			InventoryDisplay.<LoadEntriesTask>d__76 <LoadEntriesTask>d__;
			<LoadEntriesTask>d__.<>t__builder = AsyncUniTaskMethodBuilder.Create();
			<LoadEntriesTask>d__.<>4__this = this;
			<LoadEntriesTask>d__.<>1__state = -1;
			<LoadEntriesTask>d__.<>t__builder.Start<InventoryDisplay.<LoadEntriesTask>d__76>(ref <LoadEntriesTask>d__);
			return <LoadEntriesTask>d__.<>t__builder.Task;
		}

		// Token: 0x06001FDE RID: 8158 RVA: 0x0006F81B File Offset: 0x0006DA1B
		public void SetFilter(Func<Item, bool> filter)
		{
			this.filter = filter;
			this.cachedSelectedPage = 0;
			this.LoadEntriesTask().Forget();
		}

		// Token: 0x06001FDF RID: 8159 RVA: 0x0006F836 File Offset: 0x0006DA36
		private void Clear()
		{
			this.EntryPool.ReleaseAll();
			this.entries.Clear();
		}

		// Token: 0x06001FE0 RID: 8160 RVA: 0x0006F84E File Offset: 0x0006DA4E
		private void Awake()
		{
			this.sortButton.onClick.AddListener(new UnityAction(this.OnSortButtonClicked));
		}

		// Token: 0x06001FE1 RID: 8161 RVA: 0x0006F86C File Offset: 0x0006DA6C
		private void OnSortButtonClicked()
		{
			if (!this.Editable)
			{
				return;
			}
			if (!this.Target)
			{
				return;
			}
			if (this.Target.Loading)
			{
				return;
			}
			this.Target.Sort();
		}

		// Token: 0x06001FE2 RID: 8162 RVA: 0x0006F89E File Offset: 0x0006DA9E
		private void OnEnable()
		{
			this.RegisterEvents();
		}

		// Token: 0x06001FE3 RID: 8163 RVA: 0x0006F8A6 File Offset: 0x0006DAA6
		private void OnDisable()
		{
			this.UnregisterEvents();
			this.activeTaskToken++;
		}

		// Token: 0x06001FE4 RID: 8164 RVA: 0x0006F8BC File Offset: 0x0006DABC
		private void Update()
		{
			if (this.Target && this.cachedCapacity != this.Target.Capacity)
			{
				this.OnCapacityChanged();
			}
		}

		// Token: 0x06001FE5 RID: 8165 RVA: 0x0006F8E4 File Offset: 0x0006DAE4
		private void OnCapacityChanged()
		{
			if (this.Target == null)
			{
				return;
			}
			this.cachedCapacity = this.Target.Capacity;
			this.RefreshCapacityText();
			this.LoadEntriesTask().Forget();
		}

		// Token: 0x06001FE6 RID: 8166 RVA: 0x0006F917 File Offset: 0x0006DB17
		public bool IsShortcut(int index)
		{
			return this.shortcuts && index >= this.shortcutsRange.x && index <= this.shortcutsRange.y;
		}

		// Token: 0x06001FE7 RID: 8167 RVA: 0x0006F944 File Offset: 0x0006DB44
		private InventoryEntry GetNewInventoryEntry()
		{
			return this.EntryPool.Get(null);
		}

		// Token: 0x06001FE8 RID: 8168 RVA: 0x0006F952 File Offset: 0x0006DB52
		internal void NotifyItemDoubleClicked(InventoryEntry inventoryEntry, PointerEventData data)
		{
			Action<InventoryDisplay, InventoryEntry, PointerEventData> action = this.onDisplayDoubleClicked;
			if (action == null)
			{
				return;
			}
			action(this, inventoryEntry, data);
		}

		// Token: 0x06001FE9 RID: 8169 RVA: 0x0006F967 File Offset: 0x0006DB67
		public void NotifyPooled()
		{
		}

		// Token: 0x06001FEA RID: 8170 RVA: 0x0006F969 File Offset: 0x0006DB69
		public void NotifyReleased()
		{
		}

		// Token: 0x06001FEB RID: 8171 RVA: 0x0006F96C File Offset: 0x0006DB6C
		public void DisableItem(Item item)
		{
			foreach (InventoryEntry inventoryEntry in from e in this.entries
			where e.Content == item
			select e)
			{
				inventoryEntry.Disabled = true;
			}
		}

		// Token: 0x06001FEC RID: 8172 RVA: 0x0006F9D8 File Offset: 0x0006DBD8
		internal bool EvaluateShouldHighlight(Item content)
		{
			if (this.Func_ShouldHighlight != null && this.Func_ShouldHighlight(content))
			{
				return true;
			}
			content == null;
			return false;
		}

		// Token: 0x06001FEE RID: 8174 RVA: 0x0006FA61 File Offset: 0x0006DC61
		[CompilerGenerated]
		private bool <LoadEntriesTask>g__TaskValid|76_0(ref InventoryDisplay.<>c__DisplayClass76_0 A_1)
		{
			return Application.isPlaying && A_1.token == this.activeTaskToken;
		}

		// Token: 0x06001FEF RID: 8175 RVA: 0x0006FA7C File Offset: 0x0006DC7C
		[CompilerGenerated]
		private List<int> <LoadEntriesTask>g__GetRange|76_1(int begin, int end_exclusive, List<int> list, ref InventoryDisplay.<>c__DisplayClass76_0 A_4)
		{
			if (begin < 0)
			{
				begin = 0;
			}
			if (end_exclusive < 0)
			{
				end_exclusive = 0;
			}
			A_4.indexes = new List<int>();
			if (end_exclusive > list.Count)
			{
				end_exclusive = list.Count;
			}
			if (begin >= end_exclusive)
			{
				return A_4.indexes;
			}
			for (int i = begin; i < end_exclusive; i++)
			{
				A_4.indexes.Add(list[i]);
			}
			return A_4.indexes;
		}

		// Token: 0x040015B0 RID: 5552
		[SerializeField]
		private InventoryEntry entryPrefab;

		// Token: 0x040015B1 RID: 5553
		[SerializeField]
		private TextMeshProUGUI displayNameText;

		// Token: 0x040015B2 RID: 5554
		[SerializeField]
		private TextMeshProUGUI capacityText;

		// Token: 0x040015B3 RID: 5555
		[SerializeField]
		private string capacityTextFormat = "({1}/{0})";

		// Token: 0x040015B4 RID: 5556
		[SerializeField]
		private FadeGroup loadingIndcator;

		// Token: 0x040015B5 RID: 5557
		[SerializeField]
		private FadeGroup contentFadeGroup;

		// Token: 0x040015B6 RID: 5558
		[SerializeField]
		private GridLayoutGroup contentLayout;

		// Token: 0x040015B7 RID: 5559
		[SerializeField]
		private LayoutElement gridLayoutElement;

		// Token: 0x040015B8 RID: 5560
		[SerializeField]
		private GameObject placeHolder;

		// Token: 0x040015B9 RID: 5561
		[SerializeField]
		private Transform entriesParent;

		// Token: 0x040015BA RID: 5562
		[SerializeField]
		private Button sortButton;

		// Token: 0x040015BB RID: 5563
		[SerializeField]
		private Vector2Int shortcutsRange = new Vector2Int(0, 3);

		// Token: 0x040015BC RID: 5564
		[SerializeField]
		private bool editable = true;

		// Token: 0x040015BD RID: 5565
		[SerializeField]
		private bool showOperationButtons = true;

		// Token: 0x040015BE RID: 5566
		[SerializeField]
		private bool showSortButton;

		// Token: 0x040015BF RID: 5567
		[SerializeField]
		private bool usePages;

		// Token: 0x040015C0 RID: 5568
		[SerializeField]
		private int itemsEachPage = 30;

		// Token: 0x040015C1 RID: 5569
		public Func<Item, bool> filter;

		// Token: 0x040015C4 RID: 5572
		[SerializeField]
		private List<InventoryEntry> entries = new List<InventoryEntry>();

		// Token: 0x040015C5 RID: 5573
		private PrefabPool<InventoryEntry> _entryPool;

		// Token: 0x040015C8 RID: 5576
		private Func<Item, bool> _func_ShouldHighlight;

		// Token: 0x040015C9 RID: 5577
		private Func<Item, bool> _func_CanOperate;

		// Token: 0x040015CA RID: 5578
		private int cachedCapacity = -1;

		// Token: 0x040015CB RID: 5579
		private int activeTaskToken;

		// Token: 0x040015CC RID: 5580
		private int cachedMaxPage = 1;

		// Token: 0x040015CD RID: 5581
		private int cachedSelectedPage;

		// Token: 0x040015CE RID: 5582
		private List<int> cachedIndexesToDisplay = new List<int>();
	}
}
