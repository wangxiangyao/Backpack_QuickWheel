using System;
using DG.Tweening;
using Duckov.Utilities;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using LeTai.TrueShadow;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Duckov.UI
{
	// Token: 0x0200039A RID: 922
	public class ItemDisplay : MonoBehaviour, IPoolable, IPointerClickHandler, IEventSystemHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IDropHandler
	{
		// Token: 0x17000631 RID: 1585
		// (get) Token: 0x06002071 RID: 8305 RVA: 0x0007150E File Offset: 0x0006F70E
		private Sprite FallbackIcon
		{
			get
			{
				return GameplayDataSettings.UIStyle.FallbackItemIcon;
			}
		}

		// Token: 0x17000632 RID: 1586
		// (get) Token: 0x06002072 RID: 8306 RVA: 0x0007151A File Offset: 0x0006F71A
		// (set) Token: 0x06002073 RID: 8307 RVA: 0x00071522 File Offset: 0x0006F722
		public Item Target { get; private set; }

		// Token: 0x17000633 RID: 1587
		// (get) Token: 0x06002074 RID: 8308 RVA: 0x0007152B File Offset: 0x0006F72B
		// (set) Token: 0x06002075 RID: 8309 RVA: 0x00071533 File Offset: 0x0006F733
		internal Action releaseAction { get; set; }

		// Token: 0x140000DB RID: 219
		// (add) Token: 0x06002076 RID: 8310 RVA: 0x0007153C File Offset: 0x0006F73C
		// (remove) Token: 0x06002077 RID: 8311 RVA: 0x00071574 File Offset: 0x0006F774
		internal event Action<ItemDisplay, PointerEventData> onDoubleClicked;

		// Token: 0x140000DC RID: 220
		// (add) Token: 0x06002078 RID: 8312 RVA: 0x000715AC File Offset: 0x0006F7AC
		// (remove) Token: 0x06002079 RID: 8313 RVA: 0x000715E4 File Offset: 0x0006F7E4
		public event Action<PointerEventData> onReceiveDrop;

		// Token: 0x17000634 RID: 1588
		// (get) Token: 0x0600207A RID: 8314 RVA: 0x00071619 File Offset: 0x0006F819
		public bool Selected
		{
			get
			{
				return ItemUIUtilities.SelectedItemDisplay == this;
			}
		}

		// Token: 0x17000635 RID: 1589
		// (get) Token: 0x0600207B RID: 8315 RVA: 0x00071628 File Offset: 0x0006F828
		private PrefabPool<SlotIndicator> SlotIndicatorPool
		{
			get
			{
				if (this._slotIndicatorPool == null)
				{
					if (this.slotIndicatorTemplate == null)
					{
						Debug.LogError("SI is null", base.gameObject);
					}
					this._slotIndicatorPool = new PrefabPool<SlotIndicator>(this.slotIndicatorTemplate, null, null, null, null, true, 10, 10000, null);
				}
				return this._slotIndicatorPool;
			}
		}

		// Token: 0x140000DD RID: 221
		// (add) Token: 0x0600207C RID: 8316 RVA: 0x00071680 File Offset: 0x0006F880
		// (remove) Token: 0x0600207D RID: 8317 RVA: 0x000716B4 File Offset: 0x0006F8B4
		public static event Action<ItemDisplay> OnPointerEnterItemDisplay;

		// Token: 0x140000DE RID: 222
		// (add) Token: 0x0600207E RID: 8318 RVA: 0x000716E8 File Offset: 0x0006F8E8
		// (remove) Token: 0x0600207F RID: 8319 RVA: 0x0007171C File Offset: 0x0006F91C
		public static event Action<ItemDisplay> OnPointerExitItemDisplay;

		// Token: 0x06002080 RID: 8320 RVA: 0x00071750 File Offset: 0x0006F950
		public void Setup(Item target)
		{
			this.UnregisterEvents();
			this.Target = target;
			this.Clear();
			this.slotIndicatorTemplate.gameObject.SetActive(false);
			if (target == null)
			{
				this.SetupEmpty();
			}
			else
			{
				this.icon.color = Color.white;
				this.icon.sprite = target.Icon;
				if (this.icon.sprite == null)
				{
					this.icon.sprite = this.FallbackIcon;
				}
				this.icon.gameObject.SetActive(true);
				ValueTuple<float, Color, bool> shadowOffsetAndColorOfQuality = GameplayDataSettings.UIStyle.GetShadowOffsetAndColorOfQuality(target.DisplayQuality);
				this.displayQualityShadow.OffsetDistance = shadowOffsetAndColorOfQuality.Item1;
				this.displayQualityShadow.Color = shadowOffsetAndColorOfQuality.Item2;
				this.displayQualityShadow.Inset = shadowOffsetAndColorOfQuality.Item3;
				bool stackable = this.Target.Stackable;
				this.countGameObject.SetActive(stackable);
				this.nameText.text = this.Target.DisplayName;
				if (target.Slots != null)
				{
					foreach (Slot target2 in target.Slots)
					{
						this.SlotIndicatorPool.Get(null).Setup(target2);
					}
				}
			}
			this.Refresh();
			if (base.isActiveAndEnabled)
			{
				this.RegisterEvents();
			}
		}

		// Token: 0x140000DF RID: 223
		// (add) Token: 0x06002081 RID: 8321 RVA: 0x000718CC File Offset: 0x0006FACC
		// (remove) Token: 0x06002082 RID: 8322 RVA: 0x00071904 File Offset: 0x0006FB04
		public event Action<ItemDisplay, PointerEventData> onPointerClick;

		// Token: 0x06002083 RID: 8323 RVA: 0x0007193C File Offset: 0x0006FB3C
		private void RegisterEvents()
		{
			this.UnregisterEvents();
			ItemUIUtilities.OnSelectionChanged += this.OnItemUtilitiesSelectionChanged;
			ItemWishlist.OnWishlistChanged += this.OnWishlistChanged;
			if (this.Target == null)
			{
				return;
			}
			this.Target.onDestroy += this.OnTargetDestroy;
			this.Target.onSetStackCount += this.OnTargetSetStackCount;
			this.Target.onInspectionStateChanged += this.OnTargetInspectionStateChanged;
			this.Target.onDurabilityChanged += this.OnTargetDurabilityChanged;
		}

		// Token: 0x06002084 RID: 8324 RVA: 0x000719DC File Offset: 0x0006FBDC
		private void UnregisterEvents()
		{
			ItemUIUtilities.OnSelectionChanged -= this.OnItemUtilitiesSelectionChanged;
			ItemWishlist.OnWishlistChanged -= this.OnWishlistChanged;
			if (this.Target == null)
			{
				return;
			}
			this.Target.onDestroy -= this.OnTargetDestroy;
			this.Target.onSetStackCount -= this.OnTargetSetStackCount;
			this.Target.onInspectionStateChanged -= this.OnTargetInspectionStateChanged;
			this.Target.onDurabilityChanged -= this.OnTargetDurabilityChanged;
		}

		// Token: 0x06002085 RID: 8325 RVA: 0x00071A76 File Offset: 0x0006FC76
		private void OnWishlistChanged(int type)
		{
			if (this.Target == null)
			{
				return;
			}
			if (this.Target.TypeID == type)
			{
				this.RefreshWishlistInfo();
			}
		}

		// Token: 0x06002086 RID: 8326 RVA: 0x00071A9B File Offset: 0x0006FC9B
		private void OnTargetDurabilityChanged(Item item)
		{
			this.Refresh();
		}

		// Token: 0x06002087 RID: 8327 RVA: 0x00071AA3 File Offset: 0x0006FCA3
		private void OnTargetDestroy(Item item)
		{
		}

		// Token: 0x06002088 RID: 8328 RVA: 0x00071AA5 File Offset: 0x0006FCA5
		private void OnTargetSetStackCount(Item item)
		{
			if (item != this.Target)
			{
				Debug.LogError("触发事件的Item不匹配!");
			}
			this.Refresh();
		}

		// Token: 0x06002089 RID: 8329 RVA: 0x00071AC5 File Offset: 0x0006FCC5
		private void OnItemUtilitiesSelectionChanged()
		{
			this.Refresh();
		}

		// Token: 0x0600208A RID: 8330 RVA: 0x00071ACD File Offset: 0x0006FCCD
		private void OnTargetInspectionStateChanged(Item item)
		{
			this.Refresh();
			this.Punch();
		}

		// Token: 0x0600208B RID: 8331 RVA: 0x00071ADB File Offset: 0x0006FCDB
		private void Clear()
		{
			this.SlotIndicatorPool.ReleaseAll();
		}

		// Token: 0x0600208C RID: 8332 RVA: 0x00071AE8 File Offset: 0x0006FCE8
		private void SetupEmpty()
		{
			this.icon.sprite = EmptySprite.Get();
			this.icon.color = Color.clear;
			this.countText.text = string.Empty;
			this.nameText.text = string.Empty;
			this.durabilityFill.fillAmount = 0f;
			this.durabilityLoss.fillAmount = 0f;
			this.durabilityZeroIndicator.gameObject.SetActive(false);
		}

		// Token: 0x0600208D RID: 8333 RVA: 0x00071B68 File Offset: 0x0006FD68
		private void Refresh()
		{
			if (this == null)
			{
				Debug.Log("NULL");
				return;
			}
			if (this.isBeingDestroyed)
			{
				return;
			}
			if (this.Target == null)
			{
				this.HideMainContentAndDisableControl();
				this.HideInspectionElements();
				if (ItemUIUtilities.SelectedItemDisplayRaw == this)
				{
					ItemUIUtilities.Select(null);
				}
			}
			else if (this.Target.NeedInspection)
			{
				this.HideMainContentAndDisableControl();
				this.ShowInspectionElements();
			}
			else
			{
				this.HideInspectionElements();
				this.ShowMainContentAndEnableControl();
			}
			this.selectionIndicator.gameObject.SetActive(this.Selected);
			this.RefreshWishlistInfo();
		}

		// Token: 0x0600208E RID: 8334 RVA: 0x00071C04 File Offset: 0x0006FE04
		private void RefreshWishlistInfo()
		{
			if (this.Target == null || this.Target.NeedInspection)
			{
				this.wishlistedIndicator.SetActive(false);
				this.questRequiredIndicator.SetActive(false);
				this.buildingRequiredIndicator.SetActive(false);
				return;
			}
			ItemWishlist.WishlistInfo wishlistInfo = ItemWishlist.GetWishlistInfo(this.Target.TypeID);
			this.wishlistedIndicator.SetActive(wishlistInfo.isManuallyWishlisted);
			this.questRequiredIndicator.SetActive(wishlistInfo.isQuestRequired);
			this.buildingRequiredIndicator.SetActive(wishlistInfo.isBuildingRequired);
		}

		// Token: 0x0600208F RID: 8335 RVA: 0x00071C98 File Offset: 0x0006FE98
		private void HideMainContentAndDisableControl()
		{
			this.mainContentShown = false;
			if (this.mainContentShown && ItemUIUtilities.SelectedItemDisplay == this)
			{
				ItemUIUtilities.Select(null);
			}
			this.interactionEventReceiver.raycastTarget = false;
			this.icon.gameObject.SetActive(false);
			this.countGameObject.SetActive(false);
			this.durabilityGameObject.SetActive(false);
			this.durabilityZeroIndicator.gameObject.SetActive(false);
			this.nameContainer.SetActive(false);
			this.slotIndicatorContainer.SetActive(false);
		}

		// Token: 0x06002090 RID: 8336 RVA: 0x00071D28 File Offset: 0x0006FF28
		private void ShowMainContentAndEnableControl()
		{
			this.mainContentShown = true;
			this.interactionEventReceiver.raycastTarget = true;
			this.icon.gameObject.SetActive(true);
			this.nameContainer.SetActive(true);
			this.countText.text = (this.Target.Stackable ? this.Target.StackCount.ToString() : string.Empty);
			bool useDurability = this.Target.UseDurability;
			if (useDurability)
			{
				float num = this.Target.Durability / this.Target.MaxDurability;
				this.durabilityFill.fillAmount = num;
				this.durabilityFill.color = this.durabilityFillColorOverT.Evaluate(num);
				this.durabilityZeroIndicator.SetActive(this.Target.Durability <= 0f);
				this.durabilityLoss.fillAmount = this.Target.DurabilityLoss;
			}
			else
			{
				this.durabilityZeroIndicator.gameObject.SetActive(false);
			}
			this.countGameObject.SetActive(this.Target.Stackable);
			this.durabilityGameObject.SetActive(useDurability);
			this.slotIndicatorContainer.SetActive(true);
		}

		// Token: 0x06002091 RID: 8337 RVA: 0x00071E58 File Offset: 0x00070058
		private void ShowInspectionElements()
		{
			this.inspectionElementRoot.gameObject.SetActive(true);
			bool inspecting = this.Target.Inspecting;
			if (this.inspectingElement)
			{
				this.inspectingElement.SetActive(inspecting);
			}
			if (this.notInspectingElement)
			{
				this.notInspectingElement.SetActive(!inspecting);
			}
		}

		// Token: 0x06002092 RID: 8338 RVA: 0x00071EB7 File Offset: 0x000700B7
		private void HideInspectionElements()
		{
			this.inspectionElementRoot.gameObject.SetActive(false);
		}

		// Token: 0x06002093 RID: 8339 RVA: 0x00071ECA File Offset: 0x000700CA
		private void OnEnable()
		{
			this.RegisterEvents();
		}

		// Token: 0x06002094 RID: 8340 RVA: 0x00071ED2 File Offset: 0x000700D2
		private void OnDisable()
		{
			ItemUIUtilities.OnSelectionChanged -= this.OnItemUtilitiesSelectionChanged;
			if (this.Selected)
			{
				ItemUIUtilities.Select(null);
			}
			this.UnregisterEvents();
		}

		// Token: 0x06002095 RID: 8341 RVA: 0x00071EF9 File Offset: 0x000700F9
		private void OnDestroy()
		{
			this.UnregisterEvents();
			ItemUIUtilities.OnSelectionChanged -= this.OnItemUtilitiesSelectionChanged;
			this.isBeingDestroyed = true;
		}

		// Token: 0x17000636 RID: 1590
		// (get) Token: 0x06002096 RID: 8342 RVA: 0x00071F19 File Offset: 0x00070119
		public static PrefabPool<ItemDisplay> Pool
		{
			get
			{
				return GameplayUIManager.Instance.ItemDisplayPool;
			}
		}

		// Token: 0x17000637 RID: 1591
		// (get) Token: 0x06002097 RID: 8343 RVA: 0x00071F25 File Offset: 0x00070125
		// (set) Token: 0x06002098 RID: 8344 RVA: 0x00071F2D File Offset: 0x0007012D
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

		// Token: 0x17000638 RID: 1592
		// (get) Token: 0x06002099 RID: 8345 RVA: 0x00071F36 File Offset: 0x00070136
		// (set) Token: 0x0600209A RID: 8346 RVA: 0x00071F3E File Offset: 0x0007013E
		public bool Editable { get; set; }

		// Token: 0x17000639 RID: 1593
		// (get) Token: 0x0600209B RID: 8347 RVA: 0x00071F47 File Offset: 0x00070147
		// (set) Token: 0x0600209C RID: 8348 RVA: 0x00071F4F File Offset: 0x0007014F
		public bool Movable { get; set; }

		// Token: 0x1700063A RID: 1594
		// (get) Token: 0x0600209D RID: 8349 RVA: 0x00071F58 File Offset: 0x00070158
		// (set) Token: 0x0600209E RID: 8350 RVA: 0x00071F60 File Offset: 0x00070160
		public bool CanDrop { get; set; }

		// Token: 0x1700063B RID: 1595
		// (get) Token: 0x0600209F RID: 8351 RVA: 0x00071F69 File Offset: 0x00070169
		// (set) Token: 0x060020A0 RID: 8352 RVA: 0x00071F71 File Offset: 0x00070171
		public bool IsStockshopSample { get; set; }

		// Token: 0x1700063C RID: 1596
		// (get) Token: 0x060020A1 RID: 8353 RVA: 0x00071F7A File Offset: 0x0007017A
		public bool CanUse
		{
			get
			{
				return !(this.Target == null) && this.Editable && this.Target.IsUsable(CharacterMainControl.Main);
			}
		}

		// Token: 0x1700063D RID: 1597
		// (get) Token: 0x060020A2 RID: 8354 RVA: 0x00071FAB File Offset: 0x000701AB
		public bool CanSplit
		{
			get
			{
				return !(this.Target == null) && this.Editable && (this.Movable && this.Target.StackCount > 1);
			}
		}

		// Token: 0x1700063E RID: 1598
		// (get) Token: 0x060020A3 RID: 8355 RVA: 0x00071FE0 File Offset: 0x000701E0
		// (set) Token: 0x060020A4 RID: 8356 RVA: 0x00071FE8 File Offset: 0x000701E8
		public bool CanLockSort { get; internal set; }

		// Token: 0x1700063F RID: 1599
		// (get) Token: 0x060020A5 RID: 8357 RVA: 0x00071FF1 File Offset: 0x000701F1
		public bool CanSetShortcut
		{
			get
			{
				return !(this.Target == null) && this.showOperationButtons && ItemShortcut.IsItemValid(this.Target);
			}
		}

		// Token: 0x060020A6 RID: 8358 RVA: 0x0007201D File Offset: 0x0007021D
		public static ItemDisplay Get()
		{
			return ItemDisplay.Pool.Get(null);
		}

		// Token: 0x060020A7 RID: 8359 RVA: 0x0007202A File Offset: 0x0007022A
		public static void Release(ItemDisplay item)
		{
			ItemDisplay.Pool.Release(item);
		}

		// Token: 0x060020A8 RID: 8360 RVA: 0x00072037 File Offset: 0x00070237
		public void NotifyPooled()
		{
		}

		// Token: 0x060020A9 RID: 8361 RVA: 0x00072039 File Offset: 0x00070239
		public void NotifyReleased()
		{
			this.UnregisterEvents();
			this.Target = null;
			this.SetupEmpty();
		}

		// Token: 0x060020AA RID: 8362 RVA: 0x0007204E File Offset: 0x0007024E
		[ContextMenu("Select")]
		private void Select()
		{
			ItemUIUtilities.Select(this);
		}

		// Token: 0x060020AB RID: 8363 RVA: 0x00072056 File Offset: 0x00070256
		public void NotifySelected()
		{
		}

		// Token: 0x060020AC RID: 8364 RVA: 0x00072058 File Offset: 0x00070258
		public void NotifyUnselected()
		{
			KontextMenu.Hide(this);
		}

		// Token: 0x060020AD RID: 8365 RVA: 0x00072060 File Offset: 0x00070260
		public void OnPointerClick(PointerEventData eventData)
		{
			Action<ItemDisplay, PointerEventData> action = this.onPointerClick;
			if (action != null)
			{
				action(this, eventData);
			}
			if (!eventData.used && eventData.button == PointerEventData.InputButton.Left)
			{
				if (eventData.clickTime - this.lastClickTime <= 0.3f && !this.doubleClickInvoked)
				{
					this.doubleClickInvoked = true;
					Action<ItemDisplay, PointerEventData> action2 = this.onDoubleClicked;
					if (action2 != null)
					{
						action2(this, eventData);
					}
				}
				if (!eventData.used && (!this.Target || !this.Target.NeedInspection))
				{
					if (ItemUIUtilities.SelectedItemDisplay != this)
					{
						this.Select();
						eventData.Use();
					}
					else
					{
						ItemUIUtilities.Select(null);
						eventData.Use();
					}
				}
			}
			if (eventData.clickTime - this.lastClickTime > 0.3f)
			{
				this.doubleClickInvoked = false;
			}
			this.lastClickTime = eventData.clickTime;
			this.Punch();
		}

		// Token: 0x060020AE RID: 8366 RVA: 0x00072140 File Offset: 0x00070340
		public void Punch()
		{
			this.selectionIndicator.transform.DOKill(false);
			this.icon.transform.DOKill(false);
			this.backgroundRing.transform.DOKill(false);
			this.selectionIndicator.transform.localScale = Vector3.one;
			this.icon.transform.localScale = Vector3.one;
			this.backgroundRing.transform.localScale = Vector3.one;
			this.selectionIndicator.transform.DOPunchScale(Vector3.one * this.selectionRingPunchScale, this.punchDuration, 10, 1f);
			this.icon.transform.DOPunchScale(Vector3.one * this.iconPunchScale, this.punchDuration, 10, 1f);
			this.backgroundRing.transform.DOPunchScale(Vector3.one * this.backgroundRingPunchScale, this.punchDuration, 10, 1f);
		}

		// Token: 0x060020AF RID: 8367 RVA: 0x0007224C File Offset: 0x0007044C
		public void OnPointerDown(PointerEventData eventData)
		{
		}

		// Token: 0x060020B0 RID: 8368 RVA: 0x0007224E File Offset: 0x0007044E
		public void OnPointerUp(PointerEventData eventData)
		{
		}

		// Token: 0x060020B1 RID: 8369 RVA: 0x00072250 File Offset: 0x00070450
		public void OnPointerExit(PointerEventData eventData)
		{
			if (this.Target == null)
			{
				return;
			}
			Action<ItemDisplay> onPointerExitItemDisplay = ItemDisplay.OnPointerExitItemDisplay;
			if (onPointerExitItemDisplay == null)
			{
				return;
			}
			onPointerExitItemDisplay(this);
		}

		// Token: 0x060020B2 RID: 8370 RVA: 0x00072271 File Offset: 0x00070471
		public void OnPointerEnter(PointerEventData eventData)
		{
			if (this.Target == null)
			{
				return;
			}
			Action<ItemDisplay> onPointerEnterItemDisplay = ItemDisplay.OnPointerEnterItemDisplay;
			if (onPointerEnterItemDisplay == null)
			{
				return;
			}
			onPointerEnterItemDisplay(this);
		}

		// Token: 0x060020B3 RID: 8371 RVA: 0x00072292 File Offset: 0x00070492
		public void OnDrop(PointerEventData eventData)
		{
			this.HandleDirectDrop(eventData);
			if (eventData.used)
			{
				return;
			}
			Action<PointerEventData> action = this.onReceiveDrop;
			if (action == null)
			{
				return;
			}
			action(eventData);
		}

		// Token: 0x060020B4 RID: 8372 RVA: 0x000722B8 File Offset: 0x000704B8
		private void HandleDirectDrop(PointerEventData eventData)
		{
			if (this.Target == null)
			{
				return;
			}
			if (eventData.button != PointerEventData.InputButton.Left)
			{
				return;
			}
			if (this.IsStockshopSample)
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
			if (!this.Target.TryPlug(item, false, null, 0))
			{
				return;
			}
			ItemUIUtilities.NotifyPutItem(item, false);
			eventData.Use();
		}

		// Token: 0x04001619 RID: 5657
		[SerializeField]
		private Image icon;

		// Token: 0x0400161A RID: 5658
		[SerializeField]
		private TrueShadow displayQualityShadow;

		// Token: 0x0400161B RID: 5659
		[SerializeField]
		private GameObject countGameObject;

		// Token: 0x0400161C RID: 5660
		[SerializeField]
		private TextMeshProUGUI countText;

		// Token: 0x0400161D RID: 5661
		[SerializeField]
		private GameObject selectionIndicator;

		// Token: 0x0400161E RID: 5662
		[SerializeField]
		private Graphic interactionEventReceiver;

		// Token: 0x0400161F RID: 5663
		[SerializeField]
		private GameObject backgroundRing;

		// Token: 0x04001620 RID: 5664
		[SerializeField]
		private GameObject inspectionElementRoot;

		// Token: 0x04001621 RID: 5665
		[SerializeField]
		private GameObject inspectingElement;

		// Token: 0x04001622 RID: 5666
		[SerializeField]
		private GameObject notInspectingElement;

		// Token: 0x04001623 RID: 5667
		[SerializeField]
		private GameObject nameContainer;

		// Token: 0x04001624 RID: 5668
		[SerializeField]
		private TextMeshProUGUI nameText;

		// Token: 0x04001625 RID: 5669
		[SerializeField]
		private GameObject durabilityGameObject;

		// Token: 0x04001626 RID: 5670
		[SerializeField]
		private Image durabilityFill;

		// Token: 0x04001627 RID: 5671
		[SerializeField]
		private Gradient durabilityFillColorOverT;

		// Token: 0x04001628 RID: 5672
		[SerializeField]
		private GameObject durabilityZeroIndicator;

		// Token: 0x04001629 RID: 5673
		[SerializeField]
		private Image durabilityLoss;

		// Token: 0x0400162A RID: 5674
		[SerializeField]
		private GameObject slotIndicatorContainer;

		// Token: 0x0400162B RID: 5675
		[SerializeField]
		private SlotIndicator slotIndicatorTemplate;

		// Token: 0x0400162C RID: 5676
		[SerializeField]
		private GameObject wishlistedIndicator;

		// Token: 0x0400162D RID: 5677
		[SerializeField]
		private GameObject questRequiredIndicator;

		// Token: 0x0400162E RID: 5678
		[SerializeField]
		private GameObject buildingRequiredIndicator;

		// Token: 0x0400162F RID: 5679
		[SerializeField]
		[Range(0f, 1f)]
		private float punchDuration = 0.2f;

		// Token: 0x04001630 RID: 5680
		[SerializeField]
		[Range(-1f, 1f)]
		private float selectionRingPunchScale = 0.1f;

		// Token: 0x04001631 RID: 5681
		[SerializeField]
		[Range(-1f, 1f)]
		private float backgroundRingPunchScale = 0.2f;

		// Token: 0x04001632 RID: 5682
		[SerializeField]
		[Range(-1f, 1f)]
		private float iconPunchScale = 0.1f;

		// Token: 0x04001637 RID: 5687
		public const float doubleClickTimeThreshold = 0.3f;

		// Token: 0x04001638 RID: 5688
		private PrefabPool<SlotIndicator> _slotIndicatorPool;

		// Token: 0x0400163C RID: 5692
		private bool mainContentShown = true;

		// Token: 0x0400163D RID: 5693
		private bool isBeingDestroyed;

		// Token: 0x0400163E RID: 5694
		[SerializeField]
		private bool showOperationButtons = true;

		// Token: 0x04001644 RID: 5700
		private float lastClickTime;

		// Token: 0x04001645 RID: 5701
		private bool doubleClickInvoked;
	}
}
