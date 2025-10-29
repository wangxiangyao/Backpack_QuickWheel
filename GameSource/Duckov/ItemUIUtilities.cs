using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Duckov.Utilities;
using ItemStatsSystem;
using UnityEngine;

namespace Duckov.UI
{
	// Token: 0x020003A6 RID: 934
	public static class ItemUIUtilities
	{
		// Token: 0x140000E6 RID: 230
		// (add) Token: 0x06002172 RID: 8562 RVA: 0x00074E24 File Offset: 0x00073024
		// (remove) Token: 0x06002173 RID: 8563 RVA: 0x00074E58 File Offset: 0x00073058
		public static event Action OnSelectionChanged;

		// Token: 0x140000E7 RID: 231
		// (add) Token: 0x06002174 RID: 8564 RVA: 0x00074E8C File Offset: 0x0007308C
		// (remove) Token: 0x06002175 RID: 8565 RVA: 0x00074EC0 File Offset: 0x000730C0
		public static event Action<Item> OnOrphanRaised;

		// Token: 0x1700065F RID: 1631
		// (get) Token: 0x06002176 RID: 8566 RVA: 0x00074EF3 File Offset: 0x000730F3
		public static ItemDisplay SelectedItemDisplayRaw
		{
			get
			{
				return ItemUIUtilities.selectedItemDisplay;
			}
		}

		// Token: 0x17000660 RID: 1632
		// (get) Token: 0x06002177 RID: 8567 RVA: 0x00074EFA File Offset: 0x000730FA
		// (set) Token: 0x06002178 RID: 8568 RVA: 0x00074F24 File Offset: 0x00073124
		public static ItemDisplay SelectedItemDisplay
		{
			get
			{
				if (ItemUIUtilities.selectedItemDisplay == null)
				{
					return null;
				}
				if (ItemUIUtilities.selectedItemDisplay.Target == null)
				{
					return null;
				}
				return ItemUIUtilities.selectedItemDisplay;
			}
			private set
			{
				ItemDisplay itemDisplay = ItemUIUtilities.selectedItemDisplay;
				if (itemDisplay != null)
				{
					itemDisplay.NotifyUnselected();
				}
				ItemUIUtilities.selectedItemDisplay = value;
				Item selectedItem = ItemUIUtilities.SelectedItem;
				if (selectedItem == null)
				{
					ItemUIUtilities.selectedItemTypeID = -1;
				}
				else
				{
					ItemUIUtilities.selectedItemTypeID = selectedItem.TypeID;
					ItemUIUtilities.cachedSelectedItemMeta = ItemAssetsCollection.GetMetaData(ItemUIUtilities.selectedItemTypeID);
					ItemUIUtilities.cacheGunSelected = selectedItem.Tags.Contains("Gun");
				}
				ItemDisplay itemDisplay2 = ItemUIUtilities.selectedItemDisplay;
				if (itemDisplay2 != null)
				{
					itemDisplay2.NotifySelected();
				}
				Action onSelectionChanged = ItemUIUtilities.OnSelectionChanged;
				if (onSelectionChanged == null)
				{
					return;
				}
				onSelectionChanged();
			}
		}

		// Token: 0x17000661 RID: 1633
		// (get) Token: 0x06002179 RID: 8569 RVA: 0x00074FAC File Offset: 0x000731AC
		public static Item SelectedItem
		{
			get
			{
				if (ItemUIUtilities.SelectedItemDisplay == null)
				{
					return null;
				}
				return ItemUIUtilities.SelectedItemDisplay.Target;
			}
		}

		// Token: 0x17000662 RID: 1634
		// (get) Token: 0x0600217A RID: 8570 RVA: 0x00074FC7 File Offset: 0x000731C7
		public static bool IsGunSelected
		{
			get
			{
				return !(ItemUIUtilities.SelectedItem == null) && ItemUIUtilities.cacheGunSelected;
			}
		}

		// Token: 0x17000663 RID: 1635
		// (get) Token: 0x0600217B RID: 8571 RVA: 0x00074FDD File Offset: 0x000731DD
		public static string SelectedItemCaliber
		{
			get
			{
				return ItemUIUtilities.cachedSelectedItemMeta.caliber;
			}
		}

		// Token: 0x140000E8 RID: 232
		// (add) Token: 0x0600217C RID: 8572 RVA: 0x00074FEC File Offset: 0x000731EC
		// (remove) Token: 0x0600217D RID: 8573 RVA: 0x00075020 File Offset: 0x00073220
		public static event Action<Item, bool> OnPutItem;

		// Token: 0x0600217E RID: 8574 RVA: 0x00075053 File Offset: 0x00073253
		public static void Select(ItemDisplay itemDisplay)
		{
			ItemUIUtilities.SelectedItemDisplay = itemDisplay;
		}

		// Token: 0x0600217F RID: 8575 RVA: 0x0007505B File Offset: 0x0007325B
		public static void RaiseOrphan(Item orphan)
		{
			if (orphan == null)
			{
				return;
			}
			Action<Item> onOrphanRaised = ItemUIUtilities.OnOrphanRaised;
			if (onOrphanRaised != null)
			{
				onOrphanRaised(orphan);
			}
			Debug.LogWarning(string.Format("游戏中出现了孤儿Item {0}。", orphan));
		}

		// Token: 0x06002180 RID: 8576 RVA: 0x00075088 File Offset: 0x00073288
		public static void NotifyPutItem(Item item, bool pickup = false)
		{
			Action<Item, bool> onPutItem = ItemUIUtilities.OnPutItem;
			if (onPutItem == null)
			{
				return;
			}
			onPutItem(item, pickup);
		}

		// Token: 0x06002181 RID: 8577 RVA: 0x0007509C File Offset: 0x0007329C
		public static string GetPropertiesDisplayText(this Item item)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (item.Variables != null)
			{
				foreach (CustomData customData in item.Variables)
				{
					if (customData.Display)
					{
						stringBuilder.AppendLine(customData.DisplayName + "\t" + customData.GetValueDisplayString(""));
					}
				}
			}
			if (item.Constants != null)
			{
				foreach (CustomData customData2 in item.Constants)
				{
					if (customData2.Display)
					{
						stringBuilder.AppendLine(customData2.DisplayName + "\t" + customData2.GetValueDisplayString(""));
					}
				}
			}
			if (item.Stats != null)
			{
				foreach (Stat stat in item.Stats)
				{
					if (stat.Display)
					{
						stringBuilder.AppendLine(string.Format("{0}\t{1}", stat.DisplayName, stat.Value));
					}
				}
			}
			if (item.Modifiers != null)
			{
				foreach (ModifierDescription modifierDescription in item.Modifiers)
				{
					if (modifierDescription.Display)
					{
						stringBuilder.AppendLine(modifierDescription.DisplayName + "\t" + modifierDescription.GetDisplayValueString("0.##"));
					}
				}
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06002182 RID: 8578 RVA: 0x00075274 File Offset: 0x00073474
		[return: TupleElementNames(new string[]
		{
			"name",
			"value",
			"polarity"
		})]
		public static List<ValueTuple<string, string, Polarity>> GetPropertyValueTextPair(this Item item)
		{
			List<ValueTuple<string, string, Polarity>> list = new List<ValueTuple<string, string, Polarity>>();
			if (item.Variables != null)
			{
				foreach (CustomData customData in item.Variables)
				{
					if (customData.Display)
					{
						list.Add(new ValueTuple<string, string, Polarity>(customData.DisplayName, customData.GetValueDisplayString(""), Polarity.Neutral));
					}
				}
			}
			if (item.Constants != null)
			{
				foreach (CustomData customData2 in item.Constants)
				{
					if (customData2.Display)
					{
						list.Add(new ValueTuple<string, string, Polarity>(customData2.DisplayName, customData2.GetValueDisplayString(""), Polarity.Neutral));
					}
				}
			}
			if (item.Stats != null)
			{
				foreach (Stat stat in item.Stats)
				{
					if (stat.Display)
					{
						list.Add(new ValueTuple<string, string, Polarity>(stat.DisplayName, stat.Value.ToString(), Polarity.Neutral));
					}
				}
			}
			if (item.Modifiers != null)
			{
				foreach (ModifierDescription modifierDescription in item.Modifiers)
				{
					if (modifierDescription.Display)
					{
						Polarity polarity = StatInfoDatabase.GetPolarity(modifierDescription.Key);
						if (modifierDescription.Value < 0f)
						{
							polarity = -polarity;
						}
						list.Add(new ValueTuple<string, string, Polarity>(modifierDescription.DisplayName, modifierDescription.GetDisplayValueString("0.##"), polarity));
					}
				}
			}
			return list;
		}

		// Token: 0x040016B5 RID: 5813
		private static ItemDisplay selectedItemDisplay;

		// Token: 0x040016B6 RID: 5814
		private static bool cacheGunSelected;

		// Token: 0x040016B7 RID: 5815
		private static int selectedItemTypeID;

		// Token: 0x040016B8 RID: 5816
		private static ItemMetaData cachedSelectedItemMeta;
	}
}
