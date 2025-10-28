using System;
using Duckov.Utilities;
using ItemStatsSystem;
using SodaCraft.Localizations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Duckov.UI
{
	// Token: 0x02000390 RID: 912
	public class InventoryEntry : MonoBehaviour, IPoolable, IPointerClickHandler, IEventSystemHandler, IDropHandler, IItemDragSource, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
	{
		// Token: 0x1700061B RID: 1563
		// (get) Token: 0x06001FF0 RID: 8176 RVA: 0x0006FAE6 File Offset: 0x0006DCE6
		// (set) Token: 0x06001FF1 RID: 8177 RVA: 0x0006FAEE File Offset: 0x0006DCEE
		public InventoryDisplay Master { get; private set; }

		// Token: 0x1700061C RID: 1564
		// (get) Token: 0x06001FF2 RID: 8178 RVA: 0x0006FAF7 File Offset: 0x0006DCF7
		public int Index
		{
			get
			{
				return this.index;
			}
		}

		// Token: 0x1700061D RID: 1565
		// (get) Token: 0x06001FF3 RID: 8179 RVA: 0x0006FAFF File Offset: 0x0006DCFF
		// (set) Token: 0x06001FF4 RID: 8180 RVA: 0x0006FB07 File Offset: 0x0006DD07
		public bool Disabled
		{
			get
			{
				return this.disabled;
			}
			set
			{
				this.disabled = value;
				this.Refresh();
			}
		}

		// Token: 0x1700061E RID: 1566
		// (get) Token: 0x06001FF5 RID: 8181 RVA: 0x0006FB18 File Offset: 0x0006DD18
		public Item Content
		{
			get
			{
				InventoryDisplay master = this.Master;
				Inventory inventory = (master != null) ? master.Target : null;
				if (inventory == null)
				{
					return null;
				}
				if (this.index >= inventory.Capacity)
				{
					return null;
				}
				InventoryDisplay master2 = this.Master;
				if (master2 == null)
				{
					return null;
				}
				Inventory target = master2.Target;
				if (target == null)
				{
					return null;
				}
				return target.GetItemAt(this.index);
			}
		}

		// Token: 0x1700061F RID: 1567
		// (get) Token: 0x06001FF6 RID: 8182 RVA: 0x0006FB78 File Offset: 0x0006DD78
		public bool ShouldHighlight
		{
			get
			{
				return !(this.Master == null) && !(this.Content == null) && (this.Master.EvaluateShouldHighlight(this.Content) || (this.Editable && ItemUIUtilities.IsGunSelected && !this.cacheContentIsGun && this.IsCaliberMatchItemSelected()));
			}
		}

		// Token: 0x06001FF7 RID: 8183 RVA: 0x0006FBD9 File Offset: 0x0006DDD9
		private bool IsCaliberMatchItemSelected()
		{
			return !(this.Content == null) && ItemUIUtilities.SelectedItemCaliber == this.cachedMeta.caliber;
		}

		// Token: 0x17000620 RID: 1568
		// (get) Token: 0x06001FF8 RID: 8184 RVA: 0x0006FC00 File Offset: 0x0006DE00
		public bool CanOperate
		{
			get
			{
				return !(this.Master == null) && this.Master.Func_CanOperate(this.Content);
			}
		}

		// Token: 0x17000621 RID: 1569
		// (get) Token: 0x06001FF9 RID: 8185 RVA: 0x0006FC28 File Offset: 0x0006DE28
		public bool Editable
		{
			get
			{
				return !(this.Master == null) && this.Master.Editable && this.CanOperate;
			}
		}

		// Token: 0x17000622 RID: 1570
		// (get) Token: 0x06001FFA RID: 8186 RVA: 0x0006FC4F File Offset: 0x0006DE4F
		public bool Movable
		{
			get
			{
				return !(this.Master == null) && this.Master.Movable;
			}
		}

		// Token: 0x140000DA RID: 218
		// (add) Token: 0x06001FFB RID: 8187 RVA: 0x0006FC6C File Offset: 0x0006DE6C
		// (remove) Token: 0x06001FFC RID: 8188 RVA: 0x0006FCA4 File Offset: 0x0006DEA4
		public event Action<InventoryEntry> onRefresh;

		// Token: 0x06001FFD RID: 8189 RVA: 0x0006FCDC File Offset: 0x0006DEDC
		private void Awake()
		{
			this.itemDisplay.onPointerClick += this.OnItemDisplayPointerClicked;
			this.itemDisplay.onDoubleClicked += this.OnDisplayDoubleClicked;
			this.itemDisplay.onReceiveDrop += this.OnDrop;
			GameObject gameObject = this.hoveringIndicator;
			if (gameObject != null)
			{
				gameObject.SetActive(false);
			}
			UIInputManager.OnFastPick += this.OnFastPick;
			UIInputManager.OnDropItem += this.OnDropItemButton;
			UIInputManager.OnUseItem += this.OnUseItemButton;
		}

		// Token: 0x06001FFE RID: 8190 RVA: 0x0006FD74 File Offset: 0x0006DF74
		private void OnEnable()
		{
			ItemUIUtilities.OnSelectionChanged += this.OnSelectionChanged;
			UIInputManager.OnLockInventoryIndex += this.OnInputLockInventoryIndex;
			UIInputManager.OnShortcutInput += this.OnShortcutInput;
		}

		// Token: 0x06001FFF RID: 8191 RVA: 0x0006FDAC File Offset: 0x0006DFAC
		private void OnDisable()
		{
			this.hovering = false;
			GameObject gameObject = this.hoveringIndicator;
			if (gameObject != null)
			{
				gameObject.SetActive(false);
			}
			ItemUIUtilities.OnSelectionChanged -= this.OnSelectionChanged;
			UIInputManager.OnLockInventoryIndex -= this.OnInputLockInventoryIndex;
			UIInputManager.OnShortcutInput -= this.OnShortcutInput;
		}

		// Token: 0x06002000 RID: 8192 RVA: 0x0006FE05 File Offset: 0x0006E005
		private void OnShortcutInput(UIInputEventData data, int shortcutIndex)
		{
			if (!this.hovering)
			{
				return;
			}
			if (this.Item == null)
			{
				return;
			}
			ItemShortcut.Set(shortcutIndex, this.Item);
			ItemUIUtilities.NotifyPutItem(this.Item, false);
		}

		// Token: 0x06002001 RID: 8193 RVA: 0x0006FE38 File Offset: 0x0006E038
		private void OnInputLockInventoryIndex(UIInputEventData data)
		{
			if (!this.hovering)
			{
				return;
			}
			this.ToggleLock();
		}

		// Token: 0x06002002 RID: 8194 RVA: 0x0006FE49 File Offset: 0x0006E049
		private void OnSelectionChanged()
		{
			this.highlightIndicator.SetActive(this.ShouldHighlight);
			if (ItemUIUtilities.SelectedItemDisplay == this.itemDisplay)
			{
				this.Refresh();
			}
		}

		// Token: 0x06002003 RID: 8195 RVA: 0x0006FE74 File Offset: 0x0006E074
		private void OnDestroy()
		{
			UIInputManager.OnFastPick -= this.OnFastPick;
			UIInputManager.OnDropItem -= this.OnDropItemButton;
			UIInputManager.OnUseItem -= this.OnUseItemButton;
			if (this.itemDisplay != null)
			{
				this.itemDisplay.onPointerClick -= this.OnItemDisplayPointerClicked;
				this.itemDisplay.onDoubleClicked -= this.OnDisplayDoubleClicked;
				this.itemDisplay.onReceiveDrop -= this.OnDrop;
			}
		}

		// Token: 0x06002004 RID: 8196 RVA: 0x0006FF08 File Offset: 0x0006E108
		private void OnFastPick(UIInputEventData data)
		{
			if (data.Used)
			{
				return;
			}
			if (!base.isActiveAndEnabled)
			{
				return;
			}
			if (!this.hovering)
			{
				return;
			}
			this.Master.NotifyItemDoubleClicked(this, new PointerEventData(EventSystem.current));
			data.Use();
		}

		// Token: 0x06002005 RID: 8197 RVA: 0x0006FF44 File Offset: 0x0006E144
		private void OnDropItemButton(UIInputEventData data)
		{
			if (!base.isActiveAndEnabled)
			{
				return;
			}
			if (!this.hovering)
			{
				return;
			}
			if (this.Item == null)
			{
				return;
			}
			if (!this.Item.CanDrop)
			{
				return;
			}
			if (this.CanOperate)
			{
				this.Item.Drop(CharacterMainControl.Main, true);
			}
		}

		// Token: 0x06002006 RID: 8198 RVA: 0x0006FF9C File Offset: 0x0006E19C
		private void OnUseItemButton(UIInputEventData data)
		{
			if (!base.isActiveAndEnabled)
			{
				return;
			}
			if (!this.hovering)
			{
				return;
			}
			if (this.Item == null)
			{
				return;
			}
			if (!this.Item.IsUsable(CharacterMainControl.Main))
			{
				return;
			}
			if (this.CanOperate)
			{
				CharacterMainControl.Main.UseItem(this.Item);
			}
		}

		// Token: 0x06002007 RID: 8199 RVA: 0x0006FFF8 File Offset: 0x0006E1F8
		private void OnItemDisplayPointerClicked(ItemDisplay display, PointerEventData data)
		{
			if (!base.isActiveAndEnabled)
			{
				return;
			}
			if (this.disabled || !this.CanOperate)
			{
				data.Use();
				return;
			}
			if (!this.Editable)
			{
				return;
			}
			if (data.button == PointerEventData.InputButton.Left)
			{
				if (this.Content == null)
				{
					return;
				}
				if (Keyboard.current != null && Keyboard.current.altKey.isPressed)
				{
					data.Use();
					if (ItemUIUtilities.SelectedItem != null)
					{
						ItemUIUtilities.SelectedItem.TryPlug(this.Content, false, null, 0);
					}
					CharacterMainControl.Main.CharacterItem.TryPlug(this.Content, false, null, 0);
					return;
				}
				if (ItemUIUtilities.SelectedItem == null)
				{
					return;
				}
				if (this.Content.Stackable && ItemUIUtilities.SelectedItem != this.Content && ItemUIUtilities.SelectedItem.TypeID == this.Content.TypeID)
				{
					ItemUIUtilities.SelectedItem.CombineInto(this.Content);
					return;
				}
			}
			else if (data.button == PointerEventData.InputButton.Right && this.Editable && this.Content != null)
			{
				ItemOperationMenu.Show(this.itemDisplay);
			}
		}

		// Token: 0x06002008 RID: 8200 RVA: 0x00070120 File Offset: 0x0006E320
		private void OnDisplayDoubleClicked(ItemDisplay display, PointerEventData data)
		{
			this.Master.NotifyItemDoubleClicked(this, data);
		}

		// Token: 0x06002009 RID: 8201 RVA: 0x0007012F File Offset: 0x0006E32F
		public void Setup(InventoryDisplay master, int index, bool disabled = false)
		{
			this.Master = master;
			this.index = index;
			this.disabled = disabled;
			this.Refresh();
		}

		// Token: 0x0600200A RID: 8202 RVA: 0x0007014C File Offset: 0x0006E34C
		internal void Refresh()
		{
			Item content = this.Content;
			if (content != null)
			{
				this.cachedMeta = ItemAssetsCollection.GetMetaData(content.TypeID);
				this.cacheContentIsGun = content.Tags.Contains("Gun");
			}
			else
			{
				this.cacheContentIsGun = false;
				this.cachedMeta = default(ItemMetaData);
			}
			this.itemDisplay.Setup(content);
			this.itemDisplay.CanDrop = this.CanOperate;
			this.itemDisplay.Movable = this.Movable;
			this.itemDisplay.Editable = (this.Editable && this.CanOperate);
			this.itemDisplay.CanLockSort = true;
			if (!this.Master.Target.NeedInspection && content != null)
			{
				content.Inspected = true;
			}
			this.itemDisplay.ShowOperationButtons = this.Master.ShowOperationButtons;
			this.shortcutIndicator.gameObject.SetActive(this.Master.IsShortcut(this.index));
			this.disabledIndicator.SetActive(this.disabled || !this.CanOperate);
			this.highlightIndicator.SetActive(this.ShouldHighlight);
			bool active = this.Master.Target.IsIndexLocked(this.Index);
			this.lockIndicator.SetActive(active);
			Action<InventoryEntry> action = this.onRefresh;
			if (action == null)
			{
				return;
			}
			action(this);
		}

		// Token: 0x17000623 RID: 1571
		// (get) Token: 0x0600200B RID: 8203 RVA: 0x000702B8 File Offset: 0x0006E4B8
		public static PrefabPool<InventoryEntry> Pool
		{
			get
			{
				return GameplayUIManager.Instance.InventoryEntryPool;
			}
		}

		// Token: 0x17000624 RID: 1572
		// (get) Token: 0x0600200C RID: 8204 RVA: 0x000702C4 File Offset: 0x0006E4C4
		public Item Item
		{
			get
			{
				if (this.itemDisplay != null && this.itemDisplay.isActiveAndEnabled)
				{
					return this.itemDisplay.Target;
				}
				return null;
			}
		}

		// Token: 0x0600200D RID: 8205 RVA: 0x000702EE File Offset: 0x0006E4EE
		public static InventoryEntry Get()
		{
			return InventoryEntry.Pool.Get(null);
		}

		// Token: 0x0600200E RID: 8206 RVA: 0x000702FB File Offset: 0x0006E4FB
		public static void Release(InventoryEntry item)
		{
			InventoryEntry.Pool.Release(item);
		}

		// Token: 0x0600200F RID: 8207 RVA: 0x00070308 File Offset: 0x0006E508
		public void NotifyPooled()
		{
		}

		// Token: 0x06002010 RID: 8208 RVA: 0x0007030A File Offset: 0x0006E50A
		public void NotifyReleased()
		{
			this.Master = null;
		}

		// Token: 0x06002011 RID: 8209 RVA: 0x00070314 File Offset: 0x0006E514
		public void OnPointerClick(PointerEventData eventData)
		{
			this.Punch();
			if (eventData.button == PointerEventData.InputButton.Left)
			{
				this.lastClickTime = eventData.clickTime;
				if (this.Editable)
				{
					Item selectedItem = ItemUIUtilities.SelectedItem;
					if (!(selectedItem == null))
					{
						if (this.Content != null)
						{
							Debug.Log(string.Format("{0}(Inventory) 的 {1} 已经有物品。操作已取消。", this.Master.Target.name, this.index));
						}
						else
						{
							eventData.Use();
							selectedItem.Detach();
							this.Master.Target.AddAt(selectedItem, this.index);
							ItemUIUtilities.NotifyPutItem(selectedItem, false);
						}
					}
				}
				this.lastClickTime = eventData.clickTime;
			}
		}

		// Token: 0x06002012 RID: 8210 RVA: 0x000703C6 File Offset: 0x0006E5C6
		internal void Punch()
		{
			this.itemDisplay.Punch();
		}

		// Token: 0x06002013 RID: 8211 RVA: 0x000703D3 File Offset: 0x0006E5D3
		public void OnDrag(PointerEventData eventData)
		{
		}

		// Token: 0x06002014 RID: 8212 RVA: 0x000703D8 File Offset: 0x0006E5D8
		public void OnDrop(PointerEventData eventData)
		{
			if (eventData.used)
			{
				return;
			}
			if (!this.Editable)
			{
				return;
			}
			if (eventData.button != PointerEventData.InputButton.Left)
			{
				return;
			}
			IItemDragSource component = eventData.pointerDrag.gameObject.GetComponent<IItemDragSource>();
			if (component == null)
			{
				return;
			}
			if (!component.IsEditable())
			{
				return;
			}
			Item item = component.GetItem();
			if (item == null)
			{
				return;
			}
			if (item.Sticky && !this.Master.Target.AcceptSticky)
			{
				return;
			}
			if (Keyboard.current != null && Keyboard.current.ctrlKey.isPressed)
			{
				if (this.Content != null)
				{
					NotificationText.Push("UI_Inventory_TargetOccupiedCannotSplit".ToPlainText());
					return;
				}
				Debug.Log("SPLIT");
				SplitDialogue.SetupAndShow(item, this.Master.Target, this.index);
				return;
			}
			else
			{
				ItemUIUtilities.NotifyPutItem(item, false);
				if (this.Content == null)
				{
					item.Detach();
					this.Master.Target.AddAt(item, this.index);
					return;
				}
				if (this.Content.TypeID == item.TypeID && this.Content.Stackable)
				{
					this.Content.Combine(item);
					return;
				}
				Inventory inInventory = item.InInventory;
				Inventory target = this.Master.Target;
				if (inInventory != null)
				{
					int atPosition = inInventory.GetIndex(item);
					int atPosition2 = this.index;
					Item content = this.Content;
					if (content != item)
					{
						item.Detach();
						content.Detach();
						inInventory.AddAt(content, atPosition);
						target.AddAt(item, atPosition2);
					}
				}
				return;
			}
		}

		// Token: 0x06002015 RID: 8213 RVA: 0x00070567 File Offset: 0x0006E767
		public bool IsEditable()
		{
			return !(this.Content == null) && !this.Content.NeedInspection && this.Editable;
		}

		// Token: 0x06002016 RID: 8214 RVA: 0x0007058E File Offset: 0x0006E78E
		public Item GetItem()
		{
			return this.Content;
		}

		// Token: 0x06002017 RID: 8215 RVA: 0x00070596 File Offset: 0x0006E796
		public void OnPointerEnter(PointerEventData eventData)
		{
			this.hovering = true;
			GameObject gameObject = this.hoveringIndicator;
			if (gameObject == null)
			{
				return;
			}
			gameObject.SetActive(this.Editable);
		}

		// Token: 0x06002018 RID: 8216 RVA: 0x000705B5 File Offset: 0x0006E7B5
		public void OnPointerExit(PointerEventData eventData)
		{
			this.hovering = false;
			GameObject gameObject = this.hoveringIndicator;
			if (gameObject == null)
			{
				return;
			}
			gameObject.SetActive(false);
		}

		// Token: 0x06002019 RID: 8217 RVA: 0x000705CF File Offset: 0x0006E7CF
		public void ToggleLock()
		{
			this.Master.Target.ToggleLockIndex(this.Index);
		}

		// Token: 0x040015CF RID: 5583
		[SerializeField]
		private ItemDisplay itemDisplay;

		// Token: 0x040015D0 RID: 5584
		[SerializeField]
		private GameObject shortcutIndicator;

		// Token: 0x040015D1 RID: 5585
		[SerializeField]
		private GameObject disabledIndicator;

		// Token: 0x040015D2 RID: 5586
		[SerializeField]
		private GameObject hoveringIndicator;

		// Token: 0x040015D3 RID: 5587
		[SerializeField]
		private GameObject highlightIndicator;

		// Token: 0x040015D4 RID: 5588
		[SerializeField]
		private GameObject lockIndicator;

		// Token: 0x040015D6 RID: 5590
		[SerializeField]
		private int index;

		// Token: 0x040015D7 RID: 5591
		[SerializeField]
		private bool disabled;

		// Token: 0x040015D9 RID: 5593
		private bool cacheContentIsGun;

		// Token: 0x040015DA RID: 5594
		private ItemMetaData cachedMeta;

		// Token: 0x040015DB RID: 5595
		public const float doubleClickTimeThreshold = 0.3f;

		// Token: 0x040015DC RID: 5596
		private float lastClickTime;

		// Token: 0x040015DD RID: 5597
		private bool hovering;
	}
}
