using System;
using Duckov.UI.Animations;
using ItemStatsSystem;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Duckov.UI
{
	// Token: 0x0200037C RID: 892
	public class ItemHoveringUI : MonoBehaviour
	{
		// Token: 0x170005F0 RID: 1520
		// (get) Token: 0x06001EE6 RID: 7910 RVA: 0x0006C8DC File Offset: 0x0006AADC
		// (set) Token: 0x06001EE7 RID: 7911 RVA: 0x0006C8E3 File Offset: 0x0006AAE3
		public static ItemHoveringUI Instance { get; private set; }

		// Token: 0x170005F1 RID: 1521
		// (get) Token: 0x06001EE8 RID: 7912 RVA: 0x0006C8EB File Offset: 0x0006AAEB
		public RectTransform LayoutParent
		{
			get
			{
				return this.layoutParent;
			}
		}

		// Token: 0x140000D6 RID: 214
		// (add) Token: 0x06001EE9 RID: 7913 RVA: 0x0006C8F4 File Offset: 0x0006AAF4
		// (remove) Token: 0x06001EEA RID: 7914 RVA: 0x0006C928 File Offset: 0x0006AB28
		public static event Action<ItemHoveringUI, ItemMetaData> onSetupMeta;

		// Token: 0x140000D7 RID: 215
		// (add) Token: 0x06001EEB RID: 7915 RVA: 0x0006C95C File Offset: 0x0006AB5C
		// (remove) Token: 0x06001EEC RID: 7916 RVA: 0x0006C990 File Offset: 0x0006AB90
		public static event Action<ItemHoveringUI, Item> onSetupItem;

		// Token: 0x170005F2 RID: 1522
		// (get) Token: 0x06001EED RID: 7917 RVA: 0x0006C9C3 File Offset: 0x0006ABC3
		// (set) Token: 0x06001EEE RID: 7918 RVA: 0x0006C9CA File Offset: 0x0006ABCA
		public static int DisplayingItemID { get; private set; }

		// Token: 0x170005F3 RID: 1523
		// (get) Token: 0x06001EEF RID: 7919 RVA: 0x0006C9D2 File Offset: 0x0006ABD2
		public static bool Shown
		{
			get
			{
				return !(ItemHoveringUI.Instance == null) && ItemHoveringUI.Instance.fadeGroup.IsShown;
			}
		}

		// Token: 0x06001EF0 RID: 7920 RVA: 0x0006C9F4 File Offset: 0x0006ABF4
		private void Awake()
		{
			ItemHoveringUI.Instance = this;
			if (this.rectTransform == null)
			{
				this.rectTransform = base.GetComponent<RectTransform>();
			}
			ItemDisplay.OnPointerEnterItemDisplay += this.OnPointerEnterItemDisplay;
			ItemDisplay.OnPointerExitItemDisplay += this.OnPointerExitItemDisplay;
			ItemAmountDisplay.OnMouseEnter += this.OnMouseEnterItemAmountDisplay;
			ItemAmountDisplay.OnMouseExit += this.OnMouseExitItemAmountDisplay;
			ItemMetaDisplay.OnMouseEnter += this.OnMouseEnterMetaDisplay;
			ItemMetaDisplay.OnMouseExit += this.OnMouseExitMetaDisplay;
		}

		// Token: 0x06001EF1 RID: 7921 RVA: 0x0006CA88 File Offset: 0x0006AC88
		private void OnDestroy()
		{
			ItemDisplay.OnPointerEnterItemDisplay -= this.OnPointerEnterItemDisplay;
			ItemDisplay.OnPointerExitItemDisplay -= this.OnPointerExitItemDisplay;
			ItemAmountDisplay.OnMouseEnter -= this.OnMouseEnterItemAmountDisplay;
			ItemAmountDisplay.OnMouseExit -= this.OnMouseExitItemAmountDisplay;
			ItemMetaDisplay.OnMouseEnter -= this.OnMouseEnterMetaDisplay;
			ItemMetaDisplay.OnMouseExit -= this.OnMouseExitMetaDisplay;
		}

		// Token: 0x06001EF2 RID: 7922 RVA: 0x0006CAFB File Offset: 0x0006ACFB
		private void OnMouseExitMetaDisplay(ItemMetaDisplay display)
		{
			if (this.target == display)
			{
				this.Hide();
			}
		}

		// Token: 0x06001EF3 RID: 7923 RVA: 0x0006CB11 File Offset: 0x0006AD11
		private void OnMouseEnterMetaDisplay(ItemMetaDisplay display)
		{
			this.SetupAndShowMeta<ItemMetaDisplay>(display);
		}

		// Token: 0x06001EF4 RID: 7924 RVA: 0x0006CB1A File Offset: 0x0006AD1A
		private void OnMouseExitItemAmountDisplay(ItemAmountDisplay display)
		{
			if (this.target == display)
			{
				this.Hide();
			}
		}

		// Token: 0x06001EF5 RID: 7925 RVA: 0x0006CB30 File Offset: 0x0006AD30
		private void OnMouseEnterItemAmountDisplay(ItemAmountDisplay display)
		{
			this.SetupAndShowMeta<ItemAmountDisplay>(display);
		}

		// Token: 0x06001EF6 RID: 7926 RVA: 0x0006CB39 File Offset: 0x0006AD39
		private void OnPointerExitItemDisplay(ItemDisplay display)
		{
			if (this.target == display)
			{
				this.Hide();
			}
		}

		// Token: 0x06001EF7 RID: 7927 RVA: 0x0006CB4F File Offset: 0x0006AD4F
		private void OnPointerEnterItemDisplay(ItemDisplay display)
		{
			this.SetupAndShow(display);
		}

		// Token: 0x06001EF8 RID: 7928 RVA: 0x0006CB58 File Offset: 0x0006AD58
		private void SetupAndShow(ItemDisplay display)
		{
			if (display == null)
			{
				return;
			}
			Item item = display.Target;
			if (item == null)
			{
				return;
			}
			if (item.NeedInspection)
			{
				return;
			}
			this.registeredIndicator.SetActive(false);
			this.target = display;
			this.itemName.text = (item.DisplayName ?? "");
			this.itemDescription.text = (item.Description ?? "");
			this.weightDisplay.gameObject.SetActive(true);
			this.weightDisplay.text = string.Format("{0:0.#} kg", item.TotalWeight);
			this.itemID.text = string.Format("#{0}", item.TypeID);
			ItemHoveringUI.DisplayingItemID = item.TypeID;
			this.itemProperties.gameObject.SetActive(true);
			this.itemProperties.Setup(item);
			this.interactionIndicatorsContainer.SetActive(true);
			this.interactionIndicator_Menu.SetActive(display.ShowOperationButtons);
			this.interactionIndicator_Move.SetActive(display.Movable);
			this.interactionIndicator_Drop.SetActive(display.CanDrop);
			this.interactionIndicator_Use.SetActive(display.CanUse);
			this.interactionIndicator_Split.SetActive(display.CanSplit);
			this.interactionIndicator_LockSort.SetActive(display.CanLockSort);
			this.interactionIndicator_Shortcut.SetActive(display.CanSetShortcut);
			this.usageUtilitiesDisplay.Setup(item);
			this.SetupWishlistInfos(item.TypeID);
			this.SetupBulletDisplay();
			try
			{
				Action<ItemHoveringUI, Item> action = ItemHoveringUI.onSetupItem;
				if (action != null)
				{
					action(this, item);
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			this.RefreshPosition();
			this.SetupRegisteredInfo(item);
			this.fadeGroup.Show();
		}

		// Token: 0x06001EF9 RID: 7929 RVA: 0x0006CD30 File Offset: 0x0006AF30
		private void SetupRegisteredInfo(Item item)
		{
			if (item == null)
			{
				return;
			}
			if (item.IsRegistered())
			{
				this.registeredIndicator.SetActive(true);
			}
		}

		// Token: 0x06001EFA RID: 7930 RVA: 0x0006CD50 File Offset: 0x0006AF50
		private void SetupAndShowMeta<T>(T dataProvider) where T : MonoBehaviour, IItemMetaDataProvider
		{
			if (dataProvider == null)
			{
				return;
			}
			this.registeredIndicator.SetActive(false);
			this.target = dataProvider;
			ItemMetaData metaData = dataProvider.GetMetaData();
			this.itemName.text = metaData.DisplayName;
			this.itemID.text = string.Format("{0}", metaData.id);
			ItemHoveringUI.DisplayingItemID = metaData.id;
			this.itemDescription.text = metaData.Description;
			this.interactionIndicatorsContainer.SetActive(true);
			this.weightDisplay.gameObject.SetActive(false);
			this.bulletTypeDisplay.gameObject.SetActive(false);
			this.itemProperties.gameObject.SetActive(false);
			this.interactionIndicator_Menu.gameObject.SetActive(false);
			this.interactionIndicator_Move.gameObject.SetActive(false);
			this.interactionIndicator_Drop.gameObject.SetActive(false);
			this.interactionIndicator_Use.gameObject.SetActive(false);
			this.usageUtilitiesDisplay.gameObject.SetActive(false);
			this.interactionIndicator_Split.SetActive(false);
			this.interactionIndicator_Shortcut.SetActive(false);
			this.SetupWishlistInfos(metaData.id);
			try
			{
				Action<ItemHoveringUI, ItemMetaData> action = ItemHoveringUI.onSetupMeta;
				if (action != null)
				{
					action(this, metaData);
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			this.RefreshPosition();
			this.fadeGroup.Show();
		}

		// Token: 0x06001EFB RID: 7931 RVA: 0x0006CED4 File Offset: 0x0006B0D4
		private void SetupBulletDisplay()
		{
			ItemDisplay itemDisplay = this.target as ItemDisplay;
			if (itemDisplay == null)
			{
				return;
			}
			ItemSetting_Gun component = itemDisplay.Target.GetComponent<ItemSetting_Gun>();
			if (component == null)
			{
				this.bulletTypeDisplay.gameObject.SetActive(false);
				return;
			}
			this.bulletTypeDisplay.gameObject.SetActive(true);
			this.bulletTypeDisplay.Setup(component.TargetBulletID);
		}

		// Token: 0x06001EFC RID: 7932 RVA: 0x0006CF40 File Offset: 0x0006B140
		private unsafe void RefreshPosition()
		{
			Vector2 screenPoint = *Mouse.current.position.value;
			Vector2 vector;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(this.rectTransform, screenPoint, null, out vector);
			float xMax = this.contents.rect.xMax;
			float yMin = this.contents.rect.yMin;
			float b = this.rectTransform.rect.xMax - xMax;
			float b2 = this.rectTransform.rect.yMin - yMin;
			vector.x = Mathf.Min(vector.x, b);
			vector.y = Mathf.Max(vector.y, b2);
			this.contents.anchoredPosition = vector;
		}

		// Token: 0x06001EFD RID: 7933 RVA: 0x0006D000 File Offset: 0x0006B200
		private void Hide()
		{
			this.fadeGroup.Hide();
			ItemHoveringUI.DisplayingItemID = -1;
		}

		// Token: 0x06001EFE RID: 7934 RVA: 0x0006D014 File Offset: 0x0006B214
		private void Update()
		{
			if (this.fadeGroup.IsShown)
			{
				if (this.target == null || !this.target.isActiveAndEnabled)
				{
					this.Hide();
				}
				ItemDisplay itemDisplay = this.target as ItemDisplay;
				if (itemDisplay != null && itemDisplay.Target == null)
				{
					this.Hide();
				}
			}
			this.RefreshPosition();
		}

		// Token: 0x06001EFF RID: 7935 RVA: 0x0006D078 File Offset: 0x0006B278
		private void SetupWishlistInfos(int itemTypeID)
		{
			ItemWishlist.WishlistInfo wishlistInfo = ItemWishlist.GetWishlistInfo(itemTypeID);
			bool isManuallyWishlisted = wishlistInfo.isManuallyWishlisted;
			bool isBuildingRequired = wishlistInfo.isBuildingRequired;
			bool isQuestRequired = wishlistInfo.isQuestRequired;
			bool active = isManuallyWishlisted || isBuildingRequired || isQuestRequired;
			this.wishlistIndicator.SetActive(isManuallyWishlisted);
			this.buildingIndicator.SetActive(isBuildingRequired);
			this.questIndicator.SetActive(isQuestRequired);
			this.wishlistInfoParent.SetActive(active);
		}

		// Token: 0x06001F00 RID: 7936 RVA: 0x0006D0D5 File Offset: 0x0006B2D5
		internal static void NotifyRefreshWishlistInfo()
		{
			if (ItemHoveringUI.Instance == null)
			{
				return;
			}
			ItemHoveringUI.Instance.SetupWishlistInfos(ItemHoveringUI.DisplayingItemID);
		}

		// Token: 0x0400151F RID: 5407
		[SerializeField]
		private RectTransform rectTransform;

		// Token: 0x04001520 RID: 5408
		[SerializeField]
		private RectTransform layoutParent;

		// Token: 0x04001521 RID: 5409
		[SerializeField]
		private RectTransform contents;

		// Token: 0x04001522 RID: 5410
		[SerializeField]
		private FadeGroup fadeGroup;

		// Token: 0x04001523 RID: 5411
		[SerializeField]
		private TextMeshProUGUI itemName;

		// Token: 0x04001524 RID: 5412
		[SerializeField]
		private TextMeshProUGUI weightDisplay;

		// Token: 0x04001525 RID: 5413
		[SerializeField]
		private TextMeshProUGUI itemDescription;

		// Token: 0x04001526 RID: 5414
		[SerializeField]
		private TextMeshProUGUI itemID;

		// Token: 0x04001527 RID: 5415
		[SerializeField]
		private ItemPropertiesDisplay itemProperties;

		// Token: 0x04001528 RID: 5416
		[SerializeField]
		private BulletTypeDisplay bulletTypeDisplay;

		// Token: 0x04001529 RID: 5417
		[SerializeField]
		private UsageUtilitiesDisplay usageUtilitiesDisplay;

		// Token: 0x0400152A RID: 5418
		[SerializeField]
		private GameObject interactionIndicatorsContainer;

		// Token: 0x0400152B RID: 5419
		[SerializeField]
		private GameObject interactionIndicator_Move;

		// Token: 0x0400152C RID: 5420
		[SerializeField]
		private GameObject interactionIndicator_Menu;

		// Token: 0x0400152D RID: 5421
		[SerializeField]
		private GameObject interactionIndicator_Drop;

		// Token: 0x0400152E RID: 5422
		[SerializeField]
		private GameObject interactionIndicator_Use;

		// Token: 0x0400152F RID: 5423
		[SerializeField]
		private GameObject interactionIndicator_Split;

		// Token: 0x04001530 RID: 5424
		[SerializeField]
		private GameObject interactionIndicator_LockSort;

		// Token: 0x04001531 RID: 5425
		[SerializeField]
		private GameObject interactionIndicator_Shortcut;

		// Token: 0x04001532 RID: 5426
		[SerializeField]
		private GameObject wishlistInfoParent;

		// Token: 0x04001533 RID: 5427
		[SerializeField]
		private GameObject wishlistIndicator;

		// Token: 0x04001534 RID: 5428
		[SerializeField]
		private GameObject buildingIndicator;

		// Token: 0x04001535 RID: 5429
		[SerializeField]
		private GameObject questIndicator;

		// Token: 0x04001536 RID: 5430
		[SerializeField]
		private GameObject registeredIndicator;

		// Token: 0x0400153A RID: 5434
		private MonoBehaviour target;
	}
}
