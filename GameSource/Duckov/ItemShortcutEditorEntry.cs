using System;
using Duckov.Utilities;
using ItemStatsSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Duckov.UI
{
	// Token: 0x020003AB RID: 939
	public class ItemShortcutEditorEntry : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IItemDragSource, IBeginDragHandler, IEndDragHandler, IDragHandler
	{
		// Token: 0x17000672 RID: 1650
		// (get) Token: 0x060021C2 RID: 8642 RVA: 0x00075EA4 File Offset: 0x000740A4
		private Item TargetItem
		{
			get
			{
				return ItemShortcut.Get(this.index);
				
			}
		}

		// Token: 0x060021C3 RID: 8643 RVA: 0x00075EB4 File Offset: 0x000740B4
		private void Awake()
		{
			this.itemDisplay.onPointerClick += this.OnItemDisplayClicked;
			this.itemDisplay.onReceiveDrop += this.OnDrop;
			ItemShortcut.OnSetItem += this.OnSetItem;
			this.hoveringIndicator.SetActive(false);
		}

		// Token: 0x060021C4 RID: 8644 RVA: 0x00075F0D File Offset: 0x0007410D
		private void OnSetItem(int index)
		{
			if (index == this.index)
			{
				this.Refresh();
			}
		}

		// Token: 0x060021C5 RID: 8645 RVA: 0x00075F1E File Offset: 0x0007411E
		private void OnItemDisplayClicked(ItemDisplay display, PointerEventData data)
		{
			this.OnPointerClick(data);
			data.Use();
		}

		// Token: 0x060021C6 RID: 8646 RVA: 0x00075F2D File Offset: 0x0007412D
		public void OnPointerClick(PointerEventData eventData)
		{
			if (ItemUIUtilities.SelectedItem != null && ItemShortcut.Set(this.index, ItemUIUtilities.SelectedItem))
			{
				this.Refresh();
			}
		}

		// Token: 0x060021C7 RID: 8647 RVA: 0x00075F54 File Offset: 0x00074154
		internal void Refresh()
		{
			this.UnregisterEvents();
			if (this.displayingItem != this.TargetItem)
			{
				this.itemDisplay.Punch();
			}
			this.displayingItem = this.TargetItem;
			this.itemDisplay.Setup(this.displayingItem);
			this.itemDisplay.ShowOperationButtons = false;
			this.RegisterEvents();
		}

		// Token: 0x060021C8 RID: 8648 RVA: 0x00075FB4 File Offset: 0x000741B4
		private void RegisterEvents()
		{
			if (this.displayingItem != null)
			{
				this.displayingItem.onParentChanged += this.OnTargetParentChanged;
				this.displayingItem.onSetStackCount += this.OnTargetStackCountChanged;
			}
		}

		// Token: 0x060021C9 RID: 8649 RVA: 0x00075FF2 File Offset: 0x000741F2
		private void UnregisterEvents()
		{
			if (this.displayingItem != null)
			{
				this.displayingItem.onParentChanged -= this.OnTargetParentChanged;
				this.displayingItem.onSetStackCount -= this.OnTargetStackCountChanged;
			}
		}

		// Token: 0x060021CA RID: 8650 RVA: 0x00076030 File Offset: 0x00074230
		private void OnTargetStackCountChanged(Item item)
		{
			this.SetDirty();
		}

		// Token: 0x060021CB RID: 8651 RVA: 0x00076038 File Offset: 0x00074238
		private void OnTargetParentChanged(Item item)
		{
			this.SetDirty();
		}

		// Token: 0x060021CC RID: 8652 RVA: 0x00076040 File Offset: 0x00074240
		private void SetDirty()
		{
			this.dirty = true;
		}

		// Token: 0x060021CD RID: 8653 RVA: 0x00076049 File Offset: 0x00074249
		private void Update()
		{
			if (this.dirty)
			{
				this.Refresh();
			}
		}

		// Token: 0x060021CE RID: 8654 RVA: 0x00076059 File Offset: 0x00074259
		private void OnDestroy()
		{
			this.UnregisterEvents();
			ItemShortcut.OnSetItem -= this.OnSetItem;
		}

		// Token: 0x060021CF RID: 8655 RVA: 0x00076074 File Offset: 0x00074274
		internal void Setup(int i)
		{
			this.index = i;
			this.Refresh();
			InputActionReference inputActionRef = InputActionReference.Create(GameplayDataSettings.InputActions[string.Format("Character/ItemShortcut{0}", i + 3)]);
			this.indicator.Setup(inputActionRef, -1);
		}

		// Token: 0x060021D0 RID: 8656 RVA: 0x000760C0 File Offset: 0x000742C0
		public void OnDrop(PointerEventData eventData)
		{
			eventData.Use();
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
			if (!item.IsInPlayerCharacter())
			{
				ItemUtilities.SendToPlayer(item, false, false);
			}
			if (ItemShortcut.Set(this.index, item))
			{
				this.Refresh();
				AudioManager.Post("UI/click");
			}
		}

		// Token: 0x060021D1 RID: 8657 RVA: 0x00076131 File Offset: 0x00074331
		public void OnPointerEnter(PointerEventData eventData)
		{
			this.hoveringIndicator.SetActive(true);
		}

		// Token: 0x060021D2 RID: 8658 RVA: 0x0007613F File Offset: 0x0007433F
		public void OnPointerExit(PointerEventData eventData)
		{
			this.hoveringIndicator.SetActive(false);
		}

		// Token: 0x060021D3 RID: 8659 RVA: 0x0007614D File Offset: 0x0007434D
		public bool IsEditable()
		{
			return this.TargetItem != null;
		}

		// Token: 0x060021D4 RID: 8660 RVA: 0x0007615B File Offset: 0x0007435B
		public Item GetItem()
		{
			return this.TargetItem;
		}

		// Token: 0x060021D5 RID: 8661 RVA: 0x00076163 File Offset: 0x00074363
		public void OnDrag(PointerEventData eventData)
		{
		}

		// Token: 0x040016D6 RID: 5846
		[SerializeField]
		private ItemDisplay itemDisplay;

		// Token: 0x040016D7 RID: 5847
		[SerializeField]
		private GameObject hoveringIndicator;

		// Token: 0x040016D8 RID: 5848
		[SerializeField]
		private int index;

		// Token: 0x040016D9 RID: 5849
		[SerializeField]
		private InputIndicator indicator;

		// Token: 0x040016DA RID: 5850
		private Item displayingItem;

		// Token: 0x040016DB RID: 5851
		private bool dirty;
	}
}
