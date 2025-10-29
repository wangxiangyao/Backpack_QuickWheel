using System;
using Duckov.Utilities;
using ItemStatsSystem;
using LeTai.TrueShadow;
using SodaCraft.Localizations;
using SodaCraft.StringUtilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Duckov.UI
{
	// Token: 0x02000391 RID: 913
	public class ItemDetailsDisplay : MonoBehaviour
	{
		// Token: 0x17000625 RID: 1573
		// (get) Token: 0x0600201B RID: 8219 RVA: 0x000705EF File Offset: 0x0006E7EF
		private string DurabilityToolTipsFormat
		{
			get
			{
				return this.durabilityToolTipsFormatKey.ToPlainText();
			}
		}

		// Token: 0x17000626 RID: 1574
		// (get) Token: 0x0600201C RID: 8220 RVA: 0x000705FC File Offset: 0x0006E7FC
		public ItemSlotCollectionDisplay SlotCollectionDisplay
		{
			get
			{
				return this.slotCollectionDisplay;
			}
		}

		// Token: 0x17000627 RID: 1575
		// (get) Token: 0x0600201D RID: 8221 RVA: 0x00070604 File Offset: 0x0006E804
		private PrefabPool<ItemVariableEntry> VariablePool
		{
			get
			{
				if (this._variablePool == null)
				{
					this._variablePool = new PrefabPool<ItemVariableEntry>(this.variableEntryPrefab, this.propertiesParent, null, null, null, true, 10, 10000, null);
				}
				return this._variablePool;
			}
		}

		// Token: 0x17000628 RID: 1576
		// (get) Token: 0x0600201E RID: 8222 RVA: 0x00070644 File Offset: 0x0006E844
		private PrefabPool<ItemStatEntry> StatPool
		{
			get
			{
				if (this._statPool == null)
				{
					this._statPool = new PrefabPool<ItemStatEntry>(this.statEntryPrefab, this.propertiesParent, null, null, null, true, 10, 10000, null);
				}
				return this._statPool;
			}
		}

		// Token: 0x17000629 RID: 1577
		// (get) Token: 0x0600201F RID: 8223 RVA: 0x00070684 File Offset: 0x0006E884
		private PrefabPool<ItemModifierEntry> ModifierPool
		{
			get
			{
				if (this._modifierPool == null)
				{
					this._modifierPool = new PrefabPool<ItemModifierEntry>(this.modifierEntryPrefab, this.propertiesParent, null, null, null, true, 10, 10000, null);
				}
				return this._modifierPool;
			}
		}

		// Token: 0x1700062A RID: 1578
		// (get) Token: 0x06002020 RID: 8224 RVA: 0x000706C4 File Offset: 0x0006E8C4
		private PrefabPool<ItemEffectEntry> EffectPool
		{
			get
			{
				if (this._effectPool == null)
				{
					this._effectPool = new PrefabPool<ItemEffectEntry>(this.effectEntryPrefab, this.propertiesParent, null, null, null, true, 10, 10000, null);
				}
				return this._effectPool;
			}
		}

		// Token: 0x1700062B RID: 1579
		// (get) Token: 0x06002021 RID: 8225 RVA: 0x00070702 File Offset: 0x0006E902
		public Item Target
		{
			get
			{
				return this.target;
			}
		}

		// Token: 0x06002022 RID: 8226 RVA: 0x0007070C File Offset: 0x0006E90C
		internal void Setup(Item target)
		{
			this.UnregisterEvents();
			this.Clear();
			if (target == null)
			{
				return;
			}
			this.target = target;
			this.icon.sprite = target.Icon;
			ValueTuple<float, Color, bool> shadowOffsetAndColorOfQuality = GameplayDataSettings.UIStyle.GetShadowOffsetAndColorOfQuality(target.DisplayQuality);
			this.iconShadow.IgnoreCasterColor = true;
			this.iconShadow.OffsetDistance = shadowOffsetAndColorOfQuality.Item1;
			this.iconShadow.Color = shadowOffsetAndColorOfQuality.Item2;
			this.iconShadow.Inset = shadowOffsetAndColorOfQuality.Item3;
			this.displayName.text = target.DisplayName;
			this.itemID.text = string.Format("#{0}", target.TypeID);
			this.description.text = target.Description;
			this.countContainer.SetActive(target.Stackable);
			this.count.text = target.StackCount.ToString();
			this.tagsDisplay.Setup(target);
			this.usageUtilitiesDisplay.Setup(target);
			this.usableIndicator.gameObject.SetActive(target.UsageUtilities != null);
			this.RefreshDurability();
			this.slotCollectionDisplay.Setup(target, false);
			this.registeredIndicator.SetActive(target.IsRegistered());
			this.RefreshWeightText();
			this.SetupGunDisplays();
			this.SetupVariables();
			this.SetupConstants();
			this.SetupStats();
			this.SetupModifiers();
			this.SetupEffects();
			this.RegisterEvents();
		}

		// Token: 0x06002023 RID: 8227 RVA: 0x0007088B File Offset: 0x0006EA8B
		private void Awake()
		{
			this.SlotCollectionDisplay.onElementDoubleClicked += this.OnElementDoubleClicked;
		}

		// Token: 0x06002024 RID: 8228 RVA: 0x000708A4 File Offset: 0x0006EAA4
		private void OnElementDoubleClicked(ItemSlotCollectionDisplay collectionDisplay, SlotDisplay slotDisplay)
		{
			if (!collectionDisplay.Editable)
			{
				return;
			}
			Item item = slotDisplay.GetItem();
			if (item == null)
			{
				return;
			}
			ItemUtilities.SendToPlayer(item, false, PlayerStorage.Instance != null);
		}

		// Token: 0x06002025 RID: 8229 RVA: 0x000708DD File Offset: 0x0006EADD
		private void OnDestroy()
		{
			this.UnregisterEvents();
		}

		// Token: 0x06002026 RID: 8230 RVA: 0x000708E5 File Offset: 0x0006EAE5
		private void Clear()
		{
			this.tagsDisplay.Clear();
			this.VariablePool.ReleaseAll();
			this.StatPool.ReleaseAll();
			this.ModifierPool.ReleaseAll();
			this.EffectPool.ReleaseAll();
		}

		// Token: 0x06002027 RID: 8231 RVA: 0x00070920 File Offset: 0x0006EB20
		private void SetupGunDisplays()
		{
			Item item = this.Target;
			ItemSetting_Gun itemSetting_Gun = (item != null) ? item.GetComponent<ItemSetting_Gun>() : null;
			if (itemSetting_Gun == null)
			{
				this.bulletTypeDisplay.gameObject.SetActive(false);
				return;
			}
			this.bulletTypeDisplay.gameObject.SetActive(true);
			this.bulletTypeDisplay.Setup(itemSetting_Gun.TargetBulletID);
		}

		// Token: 0x06002028 RID: 8232 RVA: 0x00070980 File Offset: 0x0006EB80
		private void SetupVariables()
		{
			if (this.target.Variables == null)
			{
				return;
			}
			foreach (CustomData customData in this.target.Variables)
			{
				if (customData.Display)
				{
					ItemVariableEntry itemVariableEntry = this.VariablePool.Get(this.propertiesParent);
					itemVariableEntry.Setup(customData);
					itemVariableEntry.transform.SetAsLastSibling();
				}
			}
		}

		// Token: 0x06002029 RID: 8233 RVA: 0x00070A04 File Offset: 0x0006EC04
		private void SetupConstants()
		{
			if (this.target.Constants == null)
			{
				return;
			}
			foreach (CustomData customData in this.target.Constants)
			{
				if (customData.Display)
				{
					ItemVariableEntry itemVariableEntry = this.VariablePool.Get(this.propertiesParent);
					itemVariableEntry.Setup(customData);
					itemVariableEntry.transform.SetAsLastSibling();
				}
			}
		}

		// Token: 0x0600202A RID: 8234 RVA: 0x00070A88 File Offset: 0x0006EC88
		private void SetupStats()
		{
			if (this.target.Stats == null)
			{
				return;
			}
			foreach (Stat stat in this.target.Stats)
			{
				if (stat.Display)
				{
					ItemStatEntry itemStatEntry = this.StatPool.Get(this.propertiesParent);
					itemStatEntry.Setup(stat);
					itemStatEntry.transform.SetAsLastSibling();
				}
			}
		}

		// Token: 0x0600202B RID: 8235 RVA: 0x00070B14 File Offset: 0x0006ED14
		private void SetupModifiers()
		{
			if (this.target.Modifiers == null)
			{
				return;
			}
			foreach (ModifierDescription modifierDescription in this.target.Modifiers)
			{
				if (modifierDescription.Display)
				{
					ItemModifierEntry itemModifierEntry = this.ModifierPool.Get(this.propertiesParent);
					itemModifierEntry.Setup(modifierDescription);
					itemModifierEntry.transform.SetAsLastSibling();
				}
			}
		}

		// Token: 0x0600202C RID: 8236 RVA: 0x00070BA0 File Offset: 0x0006EDA0
		private void SetupEffects()
		{
			foreach (Effect effect in this.target.Effects)
			{
				if (effect.Display)
				{
					ItemEffectEntry itemEffectEntry = this.EffectPool.Get(this.propertiesParent);
					itemEffectEntry.Setup(effect);
					itemEffectEntry.transform.SetAsLastSibling();
				}
			}
		}

		// Token: 0x0600202D RID: 8237 RVA: 0x00070C1C File Offset: 0x0006EE1C
		private void RegisterEvents()
		{
			if (this.target == null)
			{
				return;
			}
			this.target.onDestroy += this.OnTargetDestroy;
			this.target.onChildChanged += this.OnTargetChildChanged;
			this.target.onSetStackCount += this.OnTargetSetStackCount;
			this.target.onDurabilityChanged += this.OnTargetDurabilityChanged;
		}

		// Token: 0x0600202E RID: 8238 RVA: 0x00070C94 File Offset: 0x0006EE94
		private void RefreshWeightText()
		{
			this.weightText.text = string.Format(this.weightFormat, this.target.TotalWeight);
		}

		// Token: 0x0600202F RID: 8239 RVA: 0x00070CBC File Offset: 0x0006EEBC
		private void OnTargetSetStackCount(Item item)
		{
			this.RefreshWeightText();
		}

		// Token: 0x06002030 RID: 8240 RVA: 0x00070CC4 File Offset: 0x0006EEC4
		private void OnTargetChildChanged(Item obj)
		{
			this.RefreshWeightText();
		}

		// Token: 0x06002031 RID: 8241 RVA: 0x00070CCC File Offset: 0x0006EECC
		internal void UnregisterEvents()
		{
			if (this.target == null)
			{
				return;
			}
			this.target.onDestroy -= this.OnTargetDestroy;
			this.target.onChildChanged -= this.OnTargetChildChanged;
			this.target.onSetStackCount -= this.OnTargetSetStackCount;
			this.target.onDurabilityChanged -= this.OnTargetDurabilityChanged;
		}

		// Token: 0x06002032 RID: 8242 RVA: 0x00070D44 File Offset: 0x0006EF44
		private void OnTargetDurabilityChanged(Item item)
		{
			this.RefreshDurability();
		}

		// Token: 0x06002033 RID: 8243 RVA: 0x00070D4C File Offset: 0x0006EF4C
		private void RefreshDurability()
		{
			bool useDurability = this.target.UseDurability;
			this.durabilityContainer.SetActive(useDurability);
			if (useDurability)
			{
				float durability = this.target.Durability;
				float maxDurability = this.target.MaxDurability;
				float maxDurabilityWithLoss = this.target.MaxDurabilityWithLoss;
				string lossPercentage = string.Format("{0:0}%", this.target.DurabilityLoss * 100f);
				float num = durability / maxDurability;
				this.durabilityText.text = string.Format("{0:0} / {1:0}", durability, maxDurabilityWithLoss);
				this.durabilityToolTips.text = this.DurabilityToolTipsFormat.Format(new
				{
					curDurability = durability,
					maxDurability = maxDurability,
					maxDurabilityWithLoss = maxDurabilityWithLoss,
					lossPercentage = lossPercentage
				});
				this.durabilityFill.fillAmount = num;
				this.durabilityFill.color = this.durabilityColorOverT.Evaluate(num);
				this.durabilityLoss.fillAmount = this.target.DurabilityLoss;
			}
		}

		// Token: 0x06002034 RID: 8244 RVA: 0x00070E3E File Offset: 0x0006F03E
		private void OnTargetDestroy(Item item)
		{
		}

		// Token: 0x040015DE RID: 5598
		[SerializeField]
		private Image icon;

		// Token: 0x040015DF RID: 5599
		[SerializeField]
		private TrueShadow iconShadow;

		// Token: 0x040015E0 RID: 5600
		[SerializeField]
		private TextMeshProUGUI displayName;

		// Token: 0x040015E1 RID: 5601
		[SerializeField]
		private TextMeshProUGUI itemID;

		// Token: 0x040015E2 RID: 5602
		[SerializeField]
		private TextMeshProUGUI description;

		// Token: 0x040015E3 RID: 5603
		[SerializeField]
		private GameObject countContainer;

		// Token: 0x040015E4 RID: 5604
		[SerializeField]
		private TextMeshProUGUI count;

		// Token: 0x040015E5 RID: 5605
		[SerializeField]
		private GameObject durabilityContainer;

		// Token: 0x040015E6 RID: 5606
		[SerializeField]
		private TextMeshProUGUI durabilityText;

		// Token: 0x040015E7 RID: 5607
		[SerializeField]
		private TooltipsProvider durabilityToolTips;

		// Token: 0x040015E8 RID: 5608
		[SerializeField]
		[LocalizationKey("Default")]
		private string durabilityToolTipsFormatKey = "UI_DurabilityToolTips";

		// Token: 0x040015E9 RID: 5609
		[SerializeField]
		private Image durabilityFill;

		// Token: 0x040015EA RID: 5610
		[SerializeField]
		private Image durabilityLoss;

		// Token: 0x040015EB RID: 5611
		[SerializeField]
		private Gradient durabilityColorOverT;

		// Token: 0x040015EC RID: 5612
		[SerializeField]
		private TextMeshProUGUI weightText;

		// Token: 0x040015ED RID: 5613
		[SerializeField]
		private ItemSlotCollectionDisplay slotCollectionDisplay;

		// Token: 0x040015EE RID: 5614
		[SerializeField]
		private RectTransform propertiesParent;

		// Token: 0x040015EF RID: 5615
		[SerializeField]
		private BulletTypeDisplay bulletTypeDisplay;

		// Token: 0x040015F0 RID: 5616
		[SerializeField]
		private TagsDisplay tagsDisplay;

		// Token: 0x040015F1 RID: 5617
		[SerializeField]
		private GameObject usableIndicator;

		// Token: 0x040015F2 RID: 5618
		[SerializeField]
		private UsageUtilitiesDisplay usageUtilitiesDisplay;

		// Token: 0x040015F3 RID: 5619
		[SerializeField]
		private GameObject registeredIndicator;

		// Token: 0x040015F4 RID: 5620
		[SerializeField]
		private ItemVariableEntry variableEntryPrefab;

		// Token: 0x040015F5 RID: 5621
		[SerializeField]
		private ItemStatEntry statEntryPrefab;

		// Token: 0x040015F6 RID: 5622
		[SerializeField]
		private ItemModifierEntry modifierEntryPrefab;

		// Token: 0x040015F7 RID: 5623
		[SerializeField]
		private ItemEffectEntry effectEntryPrefab;

		// Token: 0x040015F8 RID: 5624
		[SerializeField]
		private string weightFormat = "{0:0.#} kg";

		// Token: 0x040015F9 RID: 5625
		private Item target;

		// Token: 0x040015FA RID: 5626
		private PrefabPool<ItemVariableEntry> _variablePool;

		// Token: 0x040015FB RID: 5627
		private PrefabPool<ItemStatEntry> _statPool;

		// Token: 0x040015FC RID: 5628
		private PrefabPool<ItemModifierEntry> _modifierPool;

		// Token: 0x040015FD RID: 5629
		private PrefabPool<ItemEffectEntry> _effectPool;
	}
}
