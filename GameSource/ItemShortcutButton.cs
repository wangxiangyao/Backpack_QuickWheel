using System;
using DG.Tweening;
using ItemStatsSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Duckov.UI
{
	// Token: 0x020003AA RID: 938
	public class ItemShortcutButton : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		// Token: 0x1700066C RID: 1644
		// (get) Token: 0x060021A1 RID: 8609 RVA: 0x0007590C File Offset: 0x00073B0C
		// (set) Token: 0x060021A2 RID: 8610 RVA: 0x00075914 File Offset: 0x00073B14
		public int Index { get; private set; }

		// Token: 0x1700066D RID: 1645
		// (get) Token: 0x060021A3 RID: 8611 RVA: 0x0007591D File Offset: 0x00073B1D
		// (set) Token: 0x060021A4 RID: 8612 RVA: 0x00075925 File Offset: 0x00073B25
		public ItemShortcutPanel Master { get; private set; }

		// Token: 0x1700066E RID: 1646
		// (get) Token: 0x060021A5 RID: 8613 RVA: 0x0007592E File Offset: 0x00073B2E
		// (set) Token: 0x060021A6 RID: 8614 RVA: 0x00075936 File Offset: 0x00073B36
		public Inventory Inventory { get; private set; }

		// Token: 0x1700066F RID: 1647
		// (get) Token: 0x060021A7 RID: 8615 RVA: 0x0007593F File Offset: 0x00073B3F
		// (set) Token: 0x060021A8 RID: 8616 RVA: 0x00075947 File Offset: 0x00073B47
		public CharacterMainControl Character { get; private set; }

		// Token: 0x17000670 RID: 1648
		// (get) Token: 0x060021A9 RID: 8617 RVA: 0x00075950 File Offset: 0x00073B50
		// (set) Token: 0x060021AA RID: 8618 RVA: 0x00075958 File Offset: 0x00073B58
		public Item TargetItem { get; private set; }

		// Token: 0x060021AB RID: 8619 RVA: 0x00075961 File Offset: 0x00073B61
		private Item GetTargetItem()
		{
			return ItemShortcut.Get(this.Index);
		}

		// Token: 0x17000671 RID: 1649
		// (get) Token: 0x060021AC RID: 8620 RVA: 0x00075970 File Offset: 0x00073B70
		private bool Interactable
		{
			get
			{
				Item targetItem = this.TargetItem;
				return ((targetItem != null) ? targetItem.UsageUtilities : null) || (this.TargetItem && this.TargetItem.HasHandHeldAgent) || (this.TargetItem && this.TargetItem.GetBool("IsSkill", false));
			}
		}

		// Token: 0x060021AD RID: 8621 RVA: 0x000759D8 File Offset: 0x00073BD8
		public void OnPointerClick(PointerEventData eventData)
		{
			if (!this.Interactable)
			{
				this.denialIndicator.color = this.denialColor;
				this.denialIndicator.DOColor(Color.clear, 0.1f);
				return;
			}
			if (this.Character && this.TargetItem && this.TargetItem.UsageUtilities && this.TargetItem.UsageUtilities.IsUsable(this.TargetItem, this.Character))
			{
				this.Character.UseItem(this.TargetItem);
				return;
			}
			if (this.Character && this.TargetItem && this.TargetItem.GetBool("IsSkill", false))
			{
				this.Character.ChangeHoldItem(this.TargetItem);
				return;
			}
			if (this.Character && this.TargetItem && this.TargetItem.HasHandHeldAgent)
			{
				this.Character.ChangeHoldItem(this.TargetItem);
				return;
			}
			this.AnimateDenial();
		}

		// Token: 0x060021AE RID: 8622 RVA: 0x00075AF1 File Offset: 0x00073CF1
		public void AnimateDenial()
		{
			this.denialIndicator.DOKill(false);
			this.denialIndicator.color = this.denialColor;
			this.denialIndicator.DOColor(Color.clear, 0.1f);
		}

		// Token: 0x060021AF RID: 8623 RVA: 0x00075B27 File Offset: 0x00073D27
		private void Awake()
		{
			ItemShortcutButton.OnRequireAnimateDenial += this.OnStaticAnimateDenial;
		}

		// Token: 0x060021B0 RID: 8624 RVA: 0x00075B3A File Offset: 0x00073D3A
		private void OnDestroy()
		{
			ItemShortcutButton.OnRequireAnimateDenial -= this.OnStaticAnimateDenial;
			this.isBeingDestroyed = true;
			this.UnregisterEvents();
		}

		// Token: 0x060021B1 RID: 8625 RVA: 0x00075B5A File Offset: 0x00073D5A
		private void OnStaticAnimateDenial(int index)
		{
			if (!base.isActiveAndEnabled)
			{
				return;
			}
			if (index == this.Index)
			{
				this.AnimateDenial();
			}
		}

		// Token: 0x140000EB RID: 235
		// (add) Token: 0x060021B2 RID: 8626 RVA: 0x00075B74 File Offset: 0x00073D74
		// (remove) Token: 0x060021B3 RID: 8627 RVA: 0x00075BA8 File Offset: 0x00073DA8
		private static event Action<int> OnRequireAnimateDenial;

		// Token: 0x060021B4 RID: 8628 RVA: 0x00075BDB File Offset: 0x00073DDB
		public static void AnimateDenial(int index)
		{
			Action<int> onRequireAnimateDenial = ItemShortcutButton.OnRequireAnimateDenial;
			if (onRequireAnimateDenial == null)
			{
				return;
			}
			onRequireAnimateDenial(index);
		}

		// Token: 0x060021B5 RID: 8629 RVA: 0x00075BF0 File Offset: 0x00073DF0
		internal void Initialize(ItemShortcutPanel itemShortcutPanel, int index)
		{
			this.UnregisterEvents();
			this.Master = itemShortcutPanel;
			this.Inventory = this.Master.Target;
			this.Index = index;
			this.Character = this.Master.Character;
			this.Refresh();
			this.RegisterEvents();
		}

		// Token: 0x060021B6 RID: 8630 RVA: 0x00075C40 File Offset: 0x00073E40
		private void Refresh()
		{
			if (this.isBeingDestroyed)
			{
				return;
			}
			this.UnregisterEvents();
			this.TargetItem = this.GetTargetItem();
			if (this.TargetItem == null)
			{
				this.SetupEmpty();
			}
			else
			{
				this.SetupItem(this.TargetItem);
			}
			this.RegisterEvents();
			this.requireRefresh = false;
		}

		// Token: 0x060021B7 RID: 8631 RVA: 0x00075C98 File Offset: 0x00073E98
		private void SetupItem(Item targetItem)
		{
			if (this.notInteractableIndicator)
			{
				this.notInteractableIndicator.gameObject.SetActive(false);
			}
			this.itemDisplay.Setup(targetItem);
			this.itemDisplay.gameObject.SetActive(true);
			this.notInteractableIndicator.gameObject.SetActive(!this.Interactable);
		}

		// Token: 0x060021B8 RID: 8632 RVA: 0x00075CF9 File Offset: 0x00073EF9
		private void SetupEmpty()
		{
			this.itemDisplay.gameObject.SetActive(false);
		}

		// Token: 0x060021B9 RID: 8633 RVA: 0x00075D0C File Offset: 0x00073F0C
		private void RegisterEvents()
		{
			ItemShortcut.OnSetItem += this.OnItemShortcutSetItem;
			if (this.Inventory != null)
			{
				this.Inventory.onContentChanged += this.OnContentChanged;
			}
			if (this.TargetItem != null)
			{
				this.TargetItem.onSetStackCount += this.OnItemStackCountChanged;
			}
		}

		// Token: 0x060021BA RID: 8634 RVA: 0x00075D74 File Offset: 0x00073F74
		private void UnregisterEvents()
		{
			ItemShortcut.OnSetItem -= this.OnItemShortcutSetItem;
			if (this.Inventory != null)
			{
				this.Inventory.onContentChanged -= this.OnContentChanged;
			}
			if (this.TargetItem != null)
			{
				this.TargetItem.onSetStackCount -= this.OnItemStackCountChanged;
			}
		}

		// Token: 0x060021BB RID: 8635 RVA: 0x00075DDC File Offset: 0x00073FDC
		private void OnItemShortcutSetItem(int obj)
		{
			this.Refresh();
		}

		// Token: 0x060021BC RID: 8636 RVA: 0x00075DE4 File Offset: 0x00073FE4
		private void OnItemStackCountChanged(Item item)
		{
			if (item != this.TargetItem)
			{
				return;
			}
			this.requireRefresh = true;
		}

		// Token: 0x060021BD RID: 8637 RVA: 0x00075DFC File Offset: 0x00073FFC
		private void OnContentChanged(Inventory inventory, int index)
		{
			this.requireRefresh = true;
		}

		// Token: 0x060021BE RID: 8638 RVA: 0x00075E08 File Offset: 0x00074008
		private void Update()
		{
			if (this.requireRefresh)
			{
				this.Refresh();
			}
			bool flag = this.TargetItem != null && this.Character.CurrentHoldItemAgent != null && this.TargetItem == this.Character.CurrentHoldItemAgent.Item;
			if (flag && !this.lastFrameUsing)
			{
				this.OnStartedUsing();
			}
			else if (!flag && this.lastFrameUsing)
			{
				this.OnStoppedUsing();
			}
			this.usingIndicator.gameObject.SetActive(flag);
		}

		// Token: 0x060021BF RID: 8639 RVA: 0x00075E98 File Offset: 0x00074098
		private void OnStartedUsing()
		{
		}

		// Token: 0x060021C0 RID: 8640 RVA: 0x00075E9A File Offset: 0x0007409A
		private void OnStoppedUsing()
		{
		}

		// Token: 0x040016C8 RID: 5832
		[SerializeField]
		private ItemDisplay itemDisplay;

		// Token: 0x040016C9 RID: 5833
		[SerializeField]
		private GameObject usingIndicator;

		// Token: 0x040016CA RID: 5834
		[SerializeField]
		private GameObject notInteractableIndicator;

		// Token: 0x040016CB RID: 5835
		[SerializeField]
		private Image denialIndicator;

		// Token: 0x040016CC RID: 5836
		[SerializeField]
		private Color denialColor;

		// Token: 0x040016D3 RID: 5843
		private bool isBeingDestroyed;

		// Token: 0x040016D4 RID: 5844
		private bool requireRefresh;

		// Token: 0x040016D5 RID: 5845
		private bool lastFrameUsing;
	}
}
