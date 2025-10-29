using System;
using System.Collections.Generic;
using System.Linq;
using Duckov.UI.Animations;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Duckov.UI
{
	// Token: 0x0200039B RID: 923
	public class ItemOperationMenu : ManagedUIElement
	{
		// Token: 0x17000640 RID: 1600
		// (get) Token: 0x060020B6 RID: 8374 RVA: 0x00072379 File Offset: 0x00070579
		// (set) Token: 0x060020B7 RID: 8375 RVA: 0x00072380 File Offset: 0x00070580
		public static ItemOperationMenu Instance { get; private set; }

		// Token: 0x17000641 RID: 1601
		// (get) Token: 0x060020B8 RID: 8376 RVA: 0x00072388 File Offset: 0x00070588
		private Item TargetItem
		{
			get
			{
				ItemDisplay targetDisplay = this.TargetDisplay;
				if (targetDisplay == null)
				{
					return null;
				}
				return targetDisplay.Target;
			}
		}

		// Token: 0x060020B9 RID: 8377 RVA: 0x0007239B File Offset: 0x0007059B
		protected override void Awake()
		{
			base.Awake();
			ItemOperationMenu.Instance = this;
			if (this.rectTransform == null)
			{
				this.rectTransform = base.GetComponent<RectTransform>();
			}
			this.Initialize();
		}

		// Token: 0x060020BA RID: 8378 RVA: 0x000723C9 File Offset: 0x000705C9
		protected override void OnDestroy()
		{
			base.OnDestroy();
		}

		// Token: 0x060020BB RID: 8379 RVA: 0x000723D4 File Offset: 0x000705D4
		private void Update()
		{
			if (this.fadeGroup.IsHidingInProgress)
			{
				return;
			}
			if (!this.fadeGroup.IsShown)
			{
				return;
			}
			if (!Mouse.current.leftButton.wasReleasedThisFrame && !(this.targetView == null) && this.targetView.open)
			{
				if (this.fadeGroup.IsShowingInProgress)
				{
					return;
				}
				if (!Mouse.current.rightButton.wasReleasedThisFrame)
				{
					return;
				}
			}
			base.Close();
		}

		// Token: 0x060020BC RID: 8380 RVA: 0x00072450 File Offset: 0x00070650
		private void Initialize()
		{
			this.btn_Use.onClick.AddListener(new UnityAction(this.Use));
			this.btn_Split.onClick.AddListener(new UnityAction(this.Split));
			this.btn_Dump.onClick.AddListener(new UnityAction(this.Dump));
			this.btn_Equip.onClick.AddListener(new UnityAction(this.Equip));
			this.btn_Modify.onClick.AddListener(new UnityAction(this.Modify));
			this.btn_Unload.onClick.AddListener(new UnityAction(this.Unload));
			this.btn_Wishlist.onClick.AddListener(new UnityAction(this.Wishlist));
		}

		// Token: 0x060020BD RID: 8381 RVA: 0x00072524 File Offset: 0x00070724
		private void Wishlist()
		{
			if (this.TargetItem == null)
			{
				return;
			}
			int typeID = this.TargetItem.TypeID;
			if (ItemWishlist.GetWishlistInfo(typeID).isManuallyWishlisted)
			{
				ItemWishlist.RemoveFromWishlist(typeID);
				return;
			}
			ItemWishlist.AddToWishList(this.TargetItem.TypeID);
		}

		// Token: 0x060020BE RID: 8382 RVA: 0x00072571 File Offset: 0x00070771
		private void Use()
		{
			LevelManager instance = LevelManager.Instance;
			if (instance != null)
			{
				CharacterMainControl mainCharacter = instance.MainCharacter;
				if (mainCharacter != null)
				{
					mainCharacter.UseItem(this.TargetItem);
				}
			}
			InventoryView.Hide();
			base.Close();
		}

		// Token: 0x060020BF RID: 8383 RVA: 0x0007259F File Offset: 0x0007079F
		private void Split()
		{
			SplitDialogue.SetupAndShow(this.TargetItem);
			base.Close();
		}

		// Token: 0x060020C0 RID: 8384 RVA: 0x000725B2 File Offset: 0x000707B2
		private void Dump()
		{
			LevelManager instance = LevelManager.Instance;
			if ((instance != null) ? instance.MainCharacter : null)
			{
				this.TargetItem.Drop(LevelManager.Instance.MainCharacter, true);
			}
			base.Close();
		}

		// Token: 0x060020C1 RID: 8385 RVA: 0x000725E8 File Offset: 0x000707E8
		private void Modify()
		{
			if (this.TargetItem == null)
			{
				return;
			}
			ItemCustomizeView instance = ItemCustomizeView.Instance;
			if (instance == null)
			{
				return;
			}
			List<Inventory> list = new List<Inventory>();
			LevelManager instance2 = LevelManager.Instance;
			Inventory inventory;
			if (instance2 == null)
			{
				inventory = null;
			}
			else
			{
				CharacterMainControl mainCharacter = instance2.MainCharacter;
				if (mainCharacter == null)
				{
					inventory = null;
				}
				else
				{
					Item characterItem = mainCharacter.CharacterItem;
					inventory = ((characterItem != null) ? characterItem.Inventory : null);
				}
			}
			Inventory inventory2 = inventory;
			if (inventory2)
			{
				list.Add(inventory2);
			}
			instance.Setup(this.TargetItem, list);
			instance.Open(null);
			base.Close();
		}

		// Token: 0x060020C2 RID: 8386 RVA: 0x0007266D File Offset: 0x0007086D
		private void Equip()
		{
			LevelManager instance = LevelManager.Instance;
			if (instance != null)
			{
				CharacterMainControl mainCharacter = instance.MainCharacter;
				if (mainCharacter != null)
				{
					Item characterItem = mainCharacter.CharacterItem;
					if (characterItem != null)
					{
						characterItem.TryPlug(this.TargetItem, false, null, 0);
					}
				}
			}
			base.Close();
		}

		// Token: 0x060020C3 RID: 8387 RVA: 0x000726A8 File Offset: 0x000708A8
		private void Unload()
		{
			Item targetItem = this.TargetItem;
			ItemSetting_Gun itemSetting_Gun = (targetItem != null) ? targetItem.GetComponent<ItemSetting_Gun>() : null;
			if (itemSetting_Gun == null)
			{
				return;
			}
			AudioManager.Post("SFX/Combat/Gun/unload");
			itemSetting_Gun.TakeOutAllBullets();
		}

		// Token: 0x060020C4 RID: 8388 RVA: 0x000726E3 File Offset: 0x000708E3
		protected override void OnOpen()
		{
			this.fadeGroup.Show();
		}

		// Token: 0x060020C5 RID: 8389 RVA: 0x000726F0 File Offset: 0x000708F0
		protected override void OnClose()
		{
			this.fadeGroup.Hide();
			this.displayingItem = null;
		}

		// Token: 0x060020C6 RID: 8390 RVA: 0x00072704 File Offset: 0x00070904
		public static void Show(ItemDisplay id)
		{
			if (ItemOperationMenu.Instance == null)
			{
				return;
			}
			ItemOperationMenu.Instance.MShow(id);
		}

		// Token: 0x060020C7 RID: 8391 RVA: 0x0007271F File Offset: 0x0007091F
		private void MShow(ItemDisplay targetDisplay)
		{
			if (targetDisplay == null)
			{
				return;
			}
			this.TargetDisplay = targetDisplay;
			this.targetView = targetDisplay.GetComponentInParent<View>();
			this.Setup();
			base.Open(null);
		}

		// Token: 0x060020C8 RID: 8392 RVA: 0x0007274C File Offset: 0x0007094C
		private void Setup()
		{
			if (this.TargetItem == null)
			{
				return;
			}
			this.displayingItem = this.TargetItem;
			this.icon.sprite = this.TargetItem.Icon;
			this.nameText.text = this.TargetItem.DisplayName;
			this.btn_Use.gameObject.SetActive(this.Usable);
			this.btn_Use.interactable = this.UseButtonInteractable;
			this.btn_Split.gameObject.SetActive(this.Splittable);
			this.btn_Dump.gameObject.SetActive(this.Dumpable);
			this.btn_Equip.gameObject.SetActive(this.Equipable);
			this.btn_Modify.gameObject.SetActive(this.Modifyable);
			this.btn_Unload.gameObject.SetActive(this.Unloadable);
			this.RefreshWeightText();
			this.RefreshPosition();
		}

		// Token: 0x060020C9 RID: 8393 RVA: 0x00072844 File Offset: 0x00070A44
		private void RefreshPosition()
		{
			RectTransform rectTransform = this.TargetDisplay.transform as RectTransform;
			Rect rect = rectTransform.rect;
			Vector2 min = rect.min;
			Vector2 max = rect.max;
			Vector3 point = rectTransform.localToWorldMatrix.MultiplyPoint(min);
			Vector3 point2 = rectTransform.localToWorldMatrix.MultiplyPoint(max);
			Vector3 vector = this.rectTransform.worldToLocalMatrix.MultiplyPoint(point);
			Vector3 vector2 = this.rectTransform.worldToLocalMatrix.MultiplyPoint(point2);
			Vector2[] array = new Vector2[]
			{
				new Vector2(vector.x, vector.y),
				new Vector2(vector.x, vector2.y),
				new Vector2(vector2.x, vector.y),
				new Vector2(vector2.x, vector2.y)
			};
			int num = 0;
			float num2 = float.MaxValue;
			Vector2 center = this.rectTransform.rect.center;
			for (int i = 0; i < array.Length; i++)
			{
				float sqrMagnitude = (array[i] - center).sqrMagnitude;
				if (sqrMagnitude < num2)
				{
					num = i;
					num2 = sqrMagnitude;
				}
			}
			bool flag = (num & 2) > 0;
			bool flag2 = (num & 1) > 0;
			float x = flag ? vector2.x : vector.x;
			float y = flag2 ? vector.y : vector2.y;
			this.contentRectTransform.pivot = new Vector2((float)(flag ? 0 : 1), (float)(flag2 ? 0 : 1));
			this.contentRectTransform.localPosition = new Vector2(x, y);
		}

		// Token: 0x060020CA RID: 8394 RVA: 0x00072A18 File Offset: 0x00070C18
		private void RefreshWeightText()
		{
			if (this.displayingItem == null)
			{
				return;
			}
			this.weightText.text = string.Format(this.weightTextFormat, this.displayingItem.TotalWeight);
		}

		// Token: 0x060020CB RID: 8395 RVA: 0x00072A4F File Offset: 0x00070C4F
		public void OnPointerClick(PointerEventData eventData)
		{
			base.Close();
		}

		// Token: 0x17000642 RID: 1602
		// (get) Token: 0x060020CC RID: 8396 RVA: 0x00072A57 File Offset: 0x00070C57
		private bool Usable
		{
			get
			{
				return this.TargetItem.UsageUtilities != null;
			}
		}

		// Token: 0x17000643 RID: 1603
		// (get) Token: 0x060020CD RID: 8397 RVA: 0x00072A6A File Offset: 0x00070C6A
		private bool UseButtonInteractable
		{
			get
			{
				if (this.TargetItem)
				{
					Item targetItem = this.TargetItem;
					LevelManager instance = LevelManager.Instance;
					return targetItem.IsUsable((instance != null) ? instance.MainCharacter : null);
				}
				return false;
			}
		}

		// Token: 0x17000644 RID: 1604
		// (get) Token: 0x060020CE RID: 8398 RVA: 0x00072A98 File Offset: 0x00070C98
		private bool Splittable
		{
			get
			{
				CharacterMainControl main = CharacterMainControl.Main;
				return (main == null || main.CharacterItem.Inventory.GetFirstEmptyPosition(0) >= 0) && (this.TargetItem && this.TargetItem.Stackable) && this.TargetItem.StackCount > 1;
			}
		}

		// Token: 0x17000645 RID: 1605
		// (get) Token: 0x060020CF RID: 8399 RVA: 0x00072AF4 File Offset: 0x00070CF4
		private bool Dumpable
		{
			get
			{
				if (!this.TargetItem.CanDrop)
				{
					return false;
				}
				LevelManager instance = LevelManager.Instance;
				Item item;
				if (instance == null)
				{
					item = null;
				}
				else
				{
					CharacterMainControl mainCharacter = instance.MainCharacter;
					item = ((mainCharacter != null) ? mainCharacter.CharacterItem : null);
				}
				Item y = item;
				return this.TargetItem.GetRoot() == y;
			}
		}

		// Token: 0x17000646 RID: 1606
		// (get) Token: 0x060020D0 RID: 8400 RVA: 0x00072B44 File Offset: 0x00070D44
		private bool Equipable
		{
			get
			{
				if (this.TargetItem == null)
				{
					return false;
				}
				if (this.TargetItem.PluggedIntoSlot != null)
				{
					return false;
				}
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
						flag = ((characterItem != null) ? new bool?(characterItem.Slots.Any((Slot e) => e.CanPlug(this.TargetItem))) : null);
					}
				}
				bool? flag2 = flag;
				return flag2 != null && flag2.Value;
			}
		}

		// Token: 0x17000647 RID: 1607
		// (get) Token: 0x060020D1 RID: 8401 RVA: 0x00072BDA File Offset: 0x00070DDA
		private bool Modifyable
		{
			get
			{
				return this.alwaysModifyable;
			}
		}

		// Token: 0x17000648 RID: 1608
		// (get) Token: 0x060020D2 RID: 8402 RVA: 0x00072BE7 File Offset: 0x00070DE7
		private bool Unloadable
		{
			get
			{
				return !(this.TargetItem == null) && this.TargetItem.GetComponent<ItemSetting_Gun>();
			}
		}

		// Token: 0x04001647 RID: 5703
		[SerializeField]
		private FadeGroup fadeGroup;

		// Token: 0x04001648 RID: 5704
		[SerializeField]
		private RectTransform rectTransform;

		// Token: 0x04001649 RID: 5705
		[SerializeField]
		private RectTransform contentRectTransform;

		// Token: 0x0400164A RID: 5706
		[SerializeField]
		private Image icon;

		// Token: 0x0400164B RID: 5707
		[SerializeField]
		private TextMeshProUGUI nameText;

		// Token: 0x0400164C RID: 5708
		[SerializeField]
		private TextMeshProUGUI weightText;

		// Token: 0x0400164D RID: 5709
		[SerializeField]
		private string weightTextFormat = "{0:0.#}kg";

		// Token: 0x0400164E RID: 5710
		[SerializeField]
		private Button btn_Use;

		// Token: 0x0400164F RID: 5711
		[SerializeField]
		private Button btn_Split;

		// Token: 0x04001650 RID: 5712
		[SerializeField]
		private Button btn_Dump;

		// Token: 0x04001651 RID: 5713
		[SerializeField]
		private Button btn_Equip;

		// Token: 0x04001652 RID: 5714
		[SerializeField]
		private Button btn_Modify;

		// Token: 0x04001653 RID: 5715
		[SerializeField]
		private Button btn_Unload;

		// Token: 0x04001654 RID: 5716
		[SerializeField]
		private Button btn_Wishlist;

		// Token: 0x04001655 RID: 5717
		[SerializeField]
		private bool alwaysModifyable;

		// Token: 0x04001656 RID: 5718
		private View targetView;

		// Token: 0x04001657 RID: 5719
		private ItemDisplay TargetDisplay;

		// Token: 0x04001658 RID: 5720
		private Item displayingItem;
	}
}
