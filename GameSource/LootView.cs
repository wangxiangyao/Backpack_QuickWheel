using System;
using System.Collections.Generic;
using Duckov.UI.Animations;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using SodaCraft.StringUtilities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Duckov.UI
{
	// Token: 0x020003B7 RID: 951
	public class LootView : View
	{
		// Token: 0x1700068B RID: 1675
		// (get) Token: 0x0600226F RID: 8815 RVA: 0x00078552 File Offset: 0x00076752
		public static LootView Instance
		{
			get
			{
				return View.GetViewInstance<LootView>();
			}
		}

		// Token: 0x1700068C RID: 1676
		// (get) Token: 0x06002270 RID: 8816 RVA: 0x00078559 File Offset: 0x00076759
		private CharacterMainControl Character
		{
			get
			{
				return LevelManager.Instance.MainCharacter;
			}
		}

		// Token: 0x1700068D RID: 1677
		// (get) Token: 0x06002271 RID: 8817 RVA: 0x00078565 File Offset: 0x00076765
		private Item CharacterItem
		{
			get
			{
				if (this.Character == null)
				{
					return null;
				}
				return this.Character.CharacterItem;
			}
		}

		// Token: 0x1700068E RID: 1678
		// (get) Token: 0x06002272 RID: 8818 RVA: 0x00078582 File Offset: 0x00076782
		public Inventory TargetInventory
		{
			get
			{
				if (this.targetLootBox != null)
				{
					return this.targetLootBox.Inventory;
				}
				if (this.targetInventory)
				{
					return this.targetInventory;
				}
				return null;
			}
		}

		// Token: 0x06002273 RID: 8819 RVA: 0x000785B3 File Offset: 0x000767B3
		public static bool HasInventoryEverBeenLooted(Inventory inventory)
		{
			return !(LootView.Instance == null) && LootView.Instance.lootedInventories != null && !(inventory == null) && LootView.Instance.lootedInventories.Contains(inventory);
		}

		// Token: 0x06002274 RID: 8820 RVA: 0x000785F0 File Offset: 0x000767F0
		protected override void Awake()
		{
			base.Awake();
			InteractableLootbox.OnStartLoot += this.OnStartLoot;
			this.pickAllButton.onClick.AddListener(new UnityAction(this.OnPickAllButtonClicked));
			CharacterMainControl.OnMainCharacterStartUseItem += this.OnMainCharacterStartUseItem;
			LevelManager.OnMainCharacterDead += this.OnMainCharacterDead;
			this.storeAllButton.onClick.AddListener(new UnityAction(this.OnStoreAllButtonClicked));
		}

		// Token: 0x06002275 RID: 8821 RVA: 0x00078670 File Offset: 0x00076870
		private void OnStoreAllButtonClicked()
		{
			if (this.TargetInventory == null)
			{
				return;
			}
			if (this.TargetInventory != PlayerStorage.Inventory)
			{
				return;
			}
			if (this.CharacterItem == null)
			{
				return;
			}
			Inventory inventory = this.CharacterItem.Inventory;
			if (inventory == null)
			{
				return;
			}
			int lastItemPosition = inventory.GetLastItemPosition();
			for (int i = 0; i <= lastItemPosition; i++)
			{
				if (!inventory.lockedIndexes.Contains(i))
				{
					Item itemAt = inventory.GetItemAt(i);
					if (!(itemAt == null))
					{
						if (!this.TargetInventory.AddAndMerge(itemAt, 0))
						{
							break;
						}
						if (i == 0)
						{
							AudioManager.PlayPutItemSFX(itemAt, false);
						}
					}
				}
			}
		}

		// Token: 0x06002276 RID: 8822 RVA: 0x0007870F File Offset: 0x0007690F
		protected override void OnDestroy()
		{
			this.UnregisterEvents();
			InteractableLootbox.OnStartLoot -= this.OnStartLoot;
			LevelManager.OnMainCharacterDead -= this.OnMainCharacterDead;
			base.OnDestroy();
		}

		// Token: 0x06002277 RID: 8823 RVA: 0x0007873F File Offset: 0x0007693F
		private void OnMainCharacterStartUseItem(Item _item)
		{
			if (base.open)
			{
				base.Close();
			}
		}

		// Token: 0x06002278 RID: 8824 RVA: 0x0007874F File Offset: 0x0007694F
		private void OnMainCharacterDead(DamageInfo dmgInfo)
		{
			if (base.open)
			{
				base.Close();
			}
		}

		// Token: 0x06002279 RID: 8825 RVA: 0x0007875F File Offset: 0x0007695F
		private void OnEnable()
		{
			this.RegisterEvents();
		}

		// Token: 0x0600227A RID: 8826 RVA: 0x00078767 File Offset: 0x00076967
		private void OnDisable()
		{
			this.UnregisterEvents();
			InteractableLootbox interactableLootbox = this.targetLootBox;
			if (interactableLootbox != null)
			{
				interactableLootbox.StopInteract();
			}
			this.targetLootBox = null;
		}

		// Token: 0x0600227B RID: 8827 RVA: 0x00078787 File Offset: 0x00076987
		public void Show()
		{
			base.Open(null);
		}

		// Token: 0x0600227C RID: 8828 RVA: 0x00078790 File Offset: 0x00076990
		private void OnStartLoot(InteractableLootbox lootbox)
		{
			this.targetLootBox = lootbox;
			if (this.targetLootBox == null || this.targetLootBox.Inventory == null)
			{
				Debug.LogError("Target loot box could not be found");
				return;
			}
			base.Open(null);
			if (this.TargetInventory != null)
			{
				this.lootedInventories.Add(this.TargetInventory);
			}
		}

		// Token: 0x0600227D RID: 8829 RVA: 0x000787F7 File Offset: 0x000769F7
		private void OnStopLoot(InteractableLootbox lootbox)
		{
			if (lootbox == this.targetLootBox)
			{
				this.targetLootBox = null;
				base.Close();
			}
		}

		// Token: 0x0600227E RID: 8830 RVA: 0x00078814 File Offset: 0x00076A14
		public static void LootItem(Item item)
		{
			if (item == null)
			{
				return;
			}
			if (LootView.Instance == null)
			{
				return;
			}
			LootView.Instance.targetInventory = item.Inventory;
			LootView.Instance.Open(null);
		}

		// Token: 0x0600227F RID: 8831 RVA: 0x0007884C File Offset: 0x00076A4C
		protected override void OnOpen()
		{
			base.OnOpen();
			this.UnregisterEvents();
			base.gameObject.SetActive(true);
			this.characterSlotCollectionDisplay.Setup(this.CharacterItem, true);
			if (PetProxy.PetInventory)
			{
				this.petInventoryDisplay.gameObject.SetActive(true);
				this.petInventoryDisplay.Setup(PetProxy.PetInventory, null, null, false, null);
			}
			else
			{
				this.petInventoryDisplay.gameObject.SetActive(false);
			}
			this.characterInventoryDisplay.Setup(this.CharacterItem.Inventory, null, null, true, null);
			if (this.targetLootBox != null)
			{
				this.lootTargetInventoryDisplay.ShowSortButton = this.targetLootBox.ShowSortButton;
				this.lootTargetInventoryDisplay.Setup(this.TargetInventory, null, null, true, null);
				this.lootTargetDisplayName.text = this.TargetInventory.DisplayName;
				if (this.TargetInventory.GetComponent<InventoryFilterProvider>())
				{
					this.lootTargetFilterDisplay.gameObject.SetActive(true);
					this.lootTargetFilterDisplay.Setup(this.lootTargetInventoryDisplay);
					this.lootTargetFilterDisplay.Select(0);
				}
				else
				{
					this.lootTargetFilterDisplay.gameObject.SetActive(false);
				}
				this.lootTargetFadeGroup.Show();
			}
			else if (this.targetInventory != null)
			{
				this.lootTargetInventoryDisplay.ShowSortButton = false;
				this.lootTargetInventoryDisplay.Setup(this.TargetInventory, null, null, true, null);
				this.lootTargetFadeGroup.Show();
				this.lootTargetFilterDisplay.gameObject.SetActive(false);
			}
			else
			{
				this.lootTargetFadeGroup.SkipHide();
			}
			bool active = this.TargetInventory != null && this.TargetInventory == PlayerStorage.Inventory;
			this.storeAllButton.gameObject.SetActive(active);
			this.fadeGroup.Show();
			this.RefreshDetails();
			this.RefreshPickAllButton();
			this.RegisterEvents();
			this.RefreshCapacityText();
		}

		// Token: 0x06002280 RID: 8832 RVA: 0x00078A44 File Offset: 0x00076C44
		protected override void OnClose()
		{
			base.OnClose();
			this.fadeGroup.Hide();
			this.detailsFadeGroup.Hide();
			InteractableLootbox interactableLootbox = this.targetLootBox;
			if (interactableLootbox != null)
			{
				interactableLootbox.StopInteract();
			}
			this.targetLootBox = null;
			this.targetInventory = null;
			if (SplitDialogue.Instance && SplitDialogue.Instance.isActiveAndEnabled)
			{
				SplitDialogue.Instance.Cancel();
			}
			this.UnregisterEvents();
		}

		// Token: 0x06002281 RID: 8833 RVA: 0x00078AB4 File Offset: 0x00076CB4
		private void OnTargetInventoryContentChanged(Inventory inventory, int arg2)
		{
			this.RefreshPickAllButton();
			this.RefreshCapacityText();
		}

		// Token: 0x06002282 RID: 8834 RVA: 0x00078AC4 File Offset: 0x00076CC4
		private void RefreshCapacityText()
		{
			if (this.targetLootBox != null)
			{
				this.lootTargetCapacityText.text = this.lootTargetCapacityTextFormat.Format(new
				{
					itemCount = this.TargetInventory.GetItemCount(),
					capacity = this.TargetInventory.Capacity
				});
			}
		}

		// Token: 0x06002283 RID: 8835 RVA: 0x00078B10 File Offset: 0x00076D10
		private void RegisterEvents()
		{
			this.UnregisterEvents();
			ItemUIUtilities.OnSelectionChanged += this.OnSelectionChanged;
			this.lootTargetInventoryDisplay.onDisplayDoubleClicked += this.OnLootTargetItemDoubleClicked;
			this.characterInventoryDisplay.onDisplayDoubleClicked += this.OnCharacterInventoryItemDoubleClicked;
			this.petInventoryDisplay.onDisplayDoubleClicked += this.OnCharacterInventoryItemDoubleClicked;
			this.characterSlotCollectionDisplay.onElementDoubleClicked += this.OnCharacterSlotItemDoubleClicked;
			if (this.TargetInventory)
			{
				this.TargetInventory.onContentChanged += this.OnTargetInventoryContentChanged;
			}
			UIInputManager.OnNextPage += this.OnNextPage;
			UIInputManager.OnPreviousPage += this.OnPreviousPage;
		}

		// Token: 0x06002284 RID: 8836 RVA: 0x00078BD6 File Offset: 0x00076DD6
		private void OnPreviousPage(UIInputEventData data)
		{
			if (this.TargetInventory == null)
			{
				return;
			}
			if (!this.lootTargetInventoryDisplay.UsePages)
			{
				return;
			}
			this.lootTargetInventoryDisplay.PreviousPage();
		}

		// Token: 0x06002285 RID: 8837 RVA: 0x00078C00 File Offset: 0x00076E00
		private void OnNextPage(UIInputEventData data)
		{
			if (this.TargetInventory == null)
			{
				return;
			}
			if (!this.lootTargetInventoryDisplay.UsePages)
			{
				return;
			}
			this.lootTargetInventoryDisplay.NextPage();
		}

		// Token: 0x06002286 RID: 8838 RVA: 0x00078C2C File Offset: 0x00076E2C
		private void UnregisterEvents()
		{
			ItemUIUtilities.OnSelectionChanged -= this.OnSelectionChanged;
			if (this.lootTargetInventoryDisplay)
			{
				this.lootTargetInventoryDisplay.onDisplayDoubleClicked -= this.OnLootTargetItemDoubleClicked;
			}
			if (this.characterInventoryDisplay)
			{
				this.characterInventoryDisplay.onDisplayDoubleClicked -= this.OnCharacterInventoryItemDoubleClicked;
			}
			if (this.petInventoryDisplay)
			{
				this.petInventoryDisplay.onDisplayDoubleClicked -= this.OnCharacterInventoryItemDoubleClicked;
			}
			if (this.characterSlotCollectionDisplay)
			{
				this.characterSlotCollectionDisplay.onElementDoubleClicked -= this.OnCharacterSlotItemDoubleClicked;
			}
			if (this.TargetInventory)
			{
				this.TargetInventory.onContentChanged -= this.OnTargetInventoryContentChanged;
			}
			UIInputManager.OnNextPage -= this.OnNextPage;
			UIInputManager.OnPreviousPage -= this.OnPreviousPage;
		}

		// Token: 0x06002287 RID: 8839 RVA: 0x00078D20 File Offset: 0x00076F20
		private void OnCharacterSlotItemDoubleClicked(ItemSlotCollectionDisplay collectionDisplay, SlotDisplay slotDisplay)
		{
			if (slotDisplay == null)
			{
				return;
			}
			Slot target = slotDisplay.Target;
			if (target == null)
			{
				return;
			}
			Item content = target.Content;
			if (content == null)
			{
				return;
			}
			if (this.TargetInventory == null)
			{
				return;
			}
			if (content.Sticky && !this.TargetInventory.AcceptSticky)
			{
				return;
			}
			AudioManager.PlayPutItemSFX(content, false);
			content.Detach();
			if (this.TargetInventory.AddAndMerge(content, 0))
			{
				this.RefreshDetails();
				return;
			}
			Item x;
			if (!target.Plug(content, out x))
			{
				Debug.LogError("Failed plugging back!");
			}
			if (x != null)
			{
				Debug.Log("Unplugged item should be null!");
			}
			this.RefreshDetails();
		}

		// Token: 0x06002288 RID: 8840 RVA: 0x00078DCC File Offset: 0x00076FCC
		private void OnCharacterInventoryItemDoubleClicked(InventoryDisplay display, InventoryEntry entry, PointerEventData data)
		{
			Item content = entry.Content;
			if (content == null)
			{
				return;
			}
			Inventory inInventory = content.InInventory;
			if (this.TargetInventory == null)
			{
				return;
			}
			if (content.Sticky && !this.TargetInventory.AcceptSticky)
			{
				return;
			}
			AudioManager.PlayPutItemSFX(content, false);
			content.Detach();
			if (this.TargetInventory.AddAndMerge(content, 0))
			{
				this.RefreshDetails();
				return;
			}
			if (!inInventory.AddAndMerge(content, 0))
			{
				Debug.LogError("Failed sending back item");
			}
			this.RefreshDetails();
		}

		// Token: 0x06002289 RID: 8841 RVA: 0x00078E53 File Offset: 0x00077053
		private void OnSelectionChanged()
		{
			this.RefreshDetails();
		}

		// Token: 0x0600228A RID: 8842 RVA: 0x00078E5B File Offset: 0x0007705B
		private void RefreshDetails()
		{
			if (ItemUIUtilities.SelectedItem != null)
			{
				this.detailsFadeGroup.Show();
				this.detailsDisplay.Setup(ItemUIUtilities.SelectedItem);
				return;
			}
			this.detailsFadeGroup.Hide();
		}

		// Token: 0x0600228B RID: 8843 RVA: 0x00078E94 File Offset: 0x00077094
		private void OnLootTargetItemDoubleClicked(InventoryDisplay display, InventoryEntry entry, PointerEventData data)
		{
			Item item = entry.Item;
			if (item == null)
			{
				return;
			}
			if (!item.IsInPlayerCharacter())
			{
				if (this.targetLootBox != null && this.targetLootBox.needInspect && !item.Inspected)
				{
					data.Use();
					return;
				}
				data.Use();
				bool flag = false;
				LevelManager instance = LevelManager.Instance;
				bool? flag2;
				if (instance == null)
				{
					flag2 = null;
				}
				else
				{
					CharacterMainControl mainCharacter = instance.MainCharacter;
					if (mainCharacter == null)
					{
						flag2 = null;
					}
					else
					{
						Item characterItem = mainCharacter.CharacterItem;
						flag2 = ((characterItem != null) ? new bool?(characterItem.TryPlug(item, true, null, 0)) : null);
					}
				}
				bool? flag3 = flag2;
				flag |= flag3.Value;
				if (flag3 == null || !flag3.Value)
				{
					flag |= ItemUtilities.SendToPlayerCharacterInventory(item, false);
				}
				if (flag)
				{
					AudioManager.PlayPutItemSFX(item, false);
					this.RefreshDetails();
				}
			}
		}

		// Token: 0x0600228C RID: 8844 RVA: 0x00078F70 File Offset: 0x00077170
		private void RefreshPickAllButton()
		{
			if (this.TargetInventory == null)
			{
				return;
			}
			this.pickAllButton.gameObject.SetActive(false);
			bool interactable = this.TargetInventory.GetItemCount() > 0;
			this.pickAllButton.interactable = interactable;
		}

		// Token: 0x0600228D RID: 8845 RVA: 0x00078FB8 File Offset: 0x000771B8
		private void OnPickAllButtonClicked()
		{
			if (this.TargetInventory == null)
			{
				return;
			}
			List<Item> list = new List<Item>();
			list.AddRange(this.TargetInventory);
			foreach (Item item in list)
			{
				if (!(item == null) && (!this.targetLootBox.needInspect || item.Inspected))
				{
					LevelManager instance = LevelManager.Instance;
					bool? flag;
					if (instance == null)
					{
						flag = null;
					}
					else
					{
						CharacterMainControl mainCharacter = instance.MainCharacter;
						if (mainCharacter == null)
						{
							flag = null;
						}
						else
						{
							Item characterItem = mainCharacter.CharacterItem;
							flag = ((characterItem != null) ? new bool?(characterItem.TryPlug(item, true, null, 0)) : null);
						}
					}
					bool? flag2 = flag;
					if (flag2 == null || !flag2.Value)
					{
						ItemUtilities.SendToPlayerCharacterInventory(item, false);
					}
				}
			}
			AudioManager.Post("UI/confirm");
		}

		// Token: 0x04001760 RID: 5984
		[SerializeField]
		private ItemSlotCollectionDisplay characterSlotCollectionDisplay;

		// Token: 0x04001761 RID: 5985
		[SerializeField]
		private InventoryDisplay characterInventoryDisplay;

		// Token: 0x04001762 RID: 5986
		[SerializeField]
		private InventoryDisplay petInventoryDisplay;

		// Token: 0x04001763 RID: 5987
		[SerializeField]
		private InventoryDisplay lootTargetInventoryDisplay;

		// Token: 0x04001764 RID: 5988
		[SerializeField]
		private InventoryFilterDisplay lootTargetFilterDisplay;

		// Token: 0x04001765 RID: 5989
		[SerializeField]
		private FadeGroup fadeGroup;

		// Token: 0x04001766 RID: 5990
		[SerializeField]
		private Button pickAllButton;

		// Token: 0x04001767 RID: 5991
		[SerializeField]
		private TextMeshProUGUI lootTargetDisplayName;

		// Token: 0x04001768 RID: 5992
		[SerializeField]
		private TextMeshProUGUI lootTargetCapacityText;

		// Token: 0x04001769 RID: 5993
		[SerializeField]
		private string lootTargetCapacityTextFormat = "({itemCount}/{capacity})";

		// Token: 0x0400176A RID: 5994
		[SerializeField]
		private Button storeAllButton;

		// Token: 0x0400176B RID: 5995
		[SerializeField]
		private FadeGroup lootTargetFadeGroup;

		// Token: 0x0400176C RID: 5996
		[SerializeField]
		private ItemDetailsDisplay detailsDisplay;

		// Token: 0x0400176D RID: 5997
		[SerializeField]
		private FadeGroup detailsFadeGroup;

		// Token: 0x0400176E RID: 5998
		[SerializeField]
		private InteractableLootbox targetLootBox;

		// Token: 0x0400176F RID: 5999
		private Inventory targetInventory;

		// Token: 0x04001770 RID: 6000
		private HashSet<Inventory> lootedInventories = new HashSet<Inventory>();
	}
}
