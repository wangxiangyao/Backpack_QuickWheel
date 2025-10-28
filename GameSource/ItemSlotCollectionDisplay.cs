
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Duckov.Utilities;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using UnityEngine;

namespace Duckov.UI
{
	// Token: 0x0200039C RID: 924
	public class ItemSlotCollectionDisplay : MonoBehaviour
	{
		// Token: 0x17000649 RID: 1609
		// (get) Token: 0x060020D5 RID: 8405 RVA: 0x00072C2F File Offset: 0x00070E2F
		// (set) Token: 0x060020D6 RID: 8406 RVA: 0x00072C37 File Offset: 0x00070E37
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

		// Token: 0x1700064A RID: 1610
		// (get) Token: 0x060020D7 RID: 8407 RVA: 0x00072C40 File Offset: 0x00070E40
		// (set) Token: 0x060020D8 RID: 8408 RVA: 0x00072C48 File Offset: 0x00070E48
		public bool ContentSelectable
		{
			get
			{
				return this.contentSelectable;
			}
			set
			{
				this.contentSelectable = value;
			}
		}

		// Token: 0x1700064B RID: 1611
		// (get) Token: 0x060020D9 RID: 8409 RVA: 0x00072C51 File Offset: 0x00070E51
		public bool ShowOperationMenu
		{
			get
			{
				return this.showOperationMenu;
			}
		}

		// Token: 0x1700064C RID: 1612
		// (get) Token: 0x060020DA RID: 8410 RVA: 0x00072C59 File Offset: 0x00070E59
		// (set) Token: 0x060020DB RID: 8411 RVA: 0x00072C61 File Offset: 0x00070E61
		public bool Movable { get; private set; }

		// Token: 0x1700064D RID: 1613
		// (get) Token: 0x060020DC RID: 8412 RVA: 0x00072C6A File Offset: 0x00070E6A
		// (set) Token: 0x060020DD RID: 8413 RVA: 0x00072C72 File Offset: 0x00070E72
		public Item Target { get; private set; }

		// Token: 0x140000E0 RID: 224
		// (add) Token: 0x060020DE RID: 8414 RVA: 0x00072C7C File Offset: 0x00070E7C
		// (remove) Token: 0x060020DF RID: 8415 RVA: 0x00072CB4 File Offset: 0x00070EB4
		public event Action<ItemSlotCollectionDisplay, SlotDisplay> onElementClicked;

		// Token: 0x140000E1 RID: 225
		// (add) Token: 0x060020E0 RID: 8416 RVA: 0x00072CEC File Offset: 0x00070EEC
		// (remove) Token: 0x060020E1 RID: 8417 RVA: 0x00072D24 File Offset: 0x00070F24
		public event Action<ItemSlotCollectionDisplay, SlotDisplay> onElementDoubleClicked;

		// Token: 0x060020E2 RID: 8418 RVA: 0x00072D5C File Offset: 0x00070F5C
		public void Setup(Item target, bool movable = false)
		{
			this.Target = target;
			this.Clear();
			if (this.Target == null)
			{
				return;
			}
			if (this.Target.Slots == null)
			{
				return;
			}
			this.Movable = movable;
			for (int i = 0; i < this.Target.Slots.Count; i++)
			{
				Slot slot = this.Target.Slots[i];
				if (slot != null)
				{
					SlotDisplay slotDisplay = SlotDisplay.Get();
					slotDisplay.onSlotDisplayClicked += this.OnSlotDisplayClicked;
					slotDisplay.onSlotDisplayDoubleClicked += this.OnSlotDisplayDoubleClicked;
					slotDisplay.ShowOperationMenu = this.ShowOperationMenu;
					slotDisplay.Setup(slot);
					slotDisplay.Editable = this.editable;
					slotDisplay.ContentSelectable = this.contentSelectable;
					slotDisplay.transform.SetParent(this.entriesParent, false);
					slotDisplay.Movable = this.Movable;
					this.slots.Add(slotDisplay);
				}
			}
		}

		// Token: 0x060020E3 RID: 8419 RVA: 0x00072E55 File Offset: 0x00071055
		private void OnSlotDisplayDoubleClicked(SlotDisplay display)
		{
			Action<ItemSlotCollectionDisplay, SlotDisplay> action = this.onElementDoubleClicked;
			if (action == null)
			{
				return;
			}
			action(this, display);
		}

		// Token: 0x060020E4 RID: 8420 RVA: 0x00072E6C File Offset: 0x0007106C
		private void Clear()
		{
			foreach (SlotDisplay slotDisplay in this.slots)
			{
				slotDisplay.onSlotDisplayClicked -= this.OnSlotDisplayClicked;
				SlotDisplay.Release(slotDisplay);
			}
			this.slots.Clear();
			this.entriesParent.DestroyAllChildren();
		}

		// Token: 0x060020E5 RID: 8421 RVA: 0x00072EE4 File Offset: 0x000710E4
		private void OnSlotDisplayClicked(SlotDisplay display)
		{
			Action<ItemSlotCollectionDisplay, SlotDisplay> action = this.onElementClicked;
			if (action != null)
			{
				action(this, display);
			}
			if (!this.editable && this.notifyNotEditable)
			{
				this.ShowNotEditableIndicator().Forget();
			}
		}

		// Token: 0x060020E6 RID: 8422 RVA: 0x00072F14 File Offset: 0x00071114
		private UniTask ShowNotEditableIndicator()
		{
			ItemSlotCollectionDisplay.<ShowNotEditableIndicator>d__36 <ShowNotEditableIndicator>d__;
			<ShowNotEditableIndicator>d__.<>t__builder = AsyncUniTaskMethodBuilder.Create();
			<ShowNotEditableIndicator>d__.<>4__this = this;
			<ShowNotEditableIndicator>d__.<>1__state = -1;
			<ShowNotEditableIndicator>d__.<>t__builder.Start<ItemSlotCollectionDisplay.<ShowNotEditableIndicator>d__36>(ref <ShowNotEditableIndicator>d__);
			return <ShowNotEditableIndicator>d__.<>t__builder.Task;
		}

		// Token: 0x060020E8 RID: 8424 RVA: 0x00072F95 File Offset: 0x00071195
		[CompilerGenerated]
		private bool <ShowNotEditableIndicator>g__TokenChanged|36_0(ref ItemSlotCollectionDisplay.<>c__DisplayClass36_0 A_1)
		{
			return A_1.token != this.currentToken;
		}

		// Token: 0x04001659 RID: 5721
		[SerializeField]
		private Transform entriesParent;

		// Token: 0x0400165A RID: 5722
		[SerializeField]
		private CanvasGroup notEditableIndicator;

		// Token: 0x0400165B RID: 5723
		[SerializeField]
		private bool editable = true;

		// Token: 0x0400165C RID: 5724
		[SerializeField]
		private bool contentSelectable = true;

		// Token: 0x0400165D RID: 5725
		[SerializeField]
		private bool showOperationMenu = true;

		// Token: 0x0400165E RID: 5726
		[SerializeField]
		private bool notifyNotEditable;

		// Token: 0x0400165F RID: 5727
		[SerializeField]
		private float fadeDuration = 1f;

		// Token: 0x04001660 RID: 5728
		[SerializeField]
		private float sustainDuration = 1f;

		// Token: 0x04001663 RID: 5731
		private List<SlotDisplay> slots = new List<SlotDisplay>();

		// Token: 0x04001666 RID: 5734
		private int currentToken;
	}
}
