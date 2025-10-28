using System;
using Duckov.UI.Animations;
using ItemStatsSystem;
using UnityEngine;

namespace Duckov.UI
{
	// Token: 0x020003B2 RID: 946
	public class InventoryView : View
	{
		// Token: 0x1700067C RID: 1660
		// (get) Token: 0x06002210 RID: 8720 RVA: 0x00076BBD File Offset: 0x00074DBD
		private static InventoryView Instance
		{
			get
			{
				return View.GetViewInstance<InventoryView>();
			}
		}

		// Token: 0x1700067D RID: 1661
		// (get) Token: 0x06002211 RID: 8721 RVA: 0x00076BC4 File Offset: 0x00074DC4
		private Item CharacterItem
		{
			get
			{
				LevelManager instance = LevelManager.Instance;
				if (instance == null)
				{
					return null;
				}
				CharacterMainControl mainCharacter = instance.MainCharacter;
				if (mainCharacter == null)
				{
					return null;
				}
				return mainCharacter.CharacterItem;
			}
		}

		// Token: 0x06002212 RID: 8722 RVA: 0x00076BE1 File Offset: 0x00074DE1
		protected override void Awake()
		{
			base.Awake();
		}

		// Token: 0x06002213 RID: 8723 RVA: 0x00076BEC File Offset: 0x00074DEC
		private void Update()
		{
			bool editable = true;
			this.inventoryDisplay.Editable = editable;
			this.slotDisplay.Editable = editable;
		}

		// Token: 0x06002214 RID: 8724 RVA: 0x00076C14 File Offset: 0x00074E14
		protected override void OnOpen()
		{
			this.UnregisterEvents();
			base.OnOpen();
			Item characterItem = this.CharacterItem;
			if (characterItem == null)
			{
				Debug.LogError("物品栏开启失败，角色物体不存在");
				base.Close();
				return;
			}
			base.gameObject.SetActive(true);
			this.slotDisplay.Setup(characterItem, false);
			this.inventoryDisplay.Setup(characterItem.Inventory, null, null, false, null);
			this.RegisterEvents();
			this.fadeGroup.Show();
		}

		// Token: 0x06002215 RID: 8725 RVA: 0x00076C90 File Offset: 0x00074E90
		protected override void OnClose()
		{
			this.UnregisterEvents();
			base.OnClose();
			this.fadeGroup.Hide();
			this.itemDetailsFadeGroup.Hide();
			if (SplitDialogue.Instance && SplitDialogue.Instance.isActiveAndEnabled)
			{
				SplitDialogue.Instance.Cancel();
			}
		}

		// Token: 0x06002216 RID: 8726 RVA: 0x00076CE1 File Offset: 0x00074EE1
		private void RegisterEvents()
		{
			ItemUIUtilities.OnSelectionChanged += this.OnItemSelectionChanged;
		}

		// Token: 0x06002217 RID: 8727 RVA: 0x00076CF4 File Offset: 0x00074EF4
		private void OnItemSelectionChanged()
		{
			if (ItemUIUtilities.SelectedItem != null)
			{
				this.detailsDisplay.Setup(ItemUIUtilities.SelectedItem);
				this.itemDetailsFadeGroup.Show();
				return;
			}
			this.itemDetailsFadeGroup.Hide();
		}

		// Token: 0x06002218 RID: 8728 RVA: 0x00076D2A File Offset: 0x00074F2A
		private void UnregisterEvents()
		{
			ItemUIUtilities.OnSelectionChanged -= this.OnItemSelectionChanged;
		}

		// Token: 0x06002219 RID: 8729 RVA: 0x00076D3D File Offset: 0x00074F3D
		public static void Show()
		{
			if (!LevelManager.LevelInited)
			{
				return;
			}
			LootView instance = LootView.Instance;
			if (instance != null)
			{
				instance.Show();
			}
			if (LootView.Instance == null)
			{
				Debug.Log("LOOTVIEW INSTANCE IS NULL");
			}
		}

		// Token: 0x0600221A RID: 8730 RVA: 0x00076D6E File Offset: 0x00074F6E
		public static void Hide()
		{
			LootView instance = LootView.Instance;
			if (instance == null)
			{
				return;
			}
			instance.Close();
		}

		// Token: 0x04001715 RID: 5909
		[SerializeField]
		private FadeGroup fadeGroup;

		// Token: 0x04001716 RID: 5910
		[SerializeField]
		private ItemSlotCollectionDisplay slotDisplay;

		// Token: 0x04001717 RID: 5911
		[SerializeField]
		private InventoryDisplay inventoryDisplay;

		// Token: 0x04001718 RID: 5912
		[SerializeField]
		private ItemDetailsDisplay detailsDisplay;

		// Token: 0x04001719 RID: 5913
		[SerializeField]
		private FadeGroup itemDetailsFadeGroup;
	}
}
