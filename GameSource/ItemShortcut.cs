using System;
using System.Collections.Generic;
using System.Linq;
using ItemStatsSystem;
using Saves;
using UnityEngine;

namespace Duckov
{
	// Token: 0x02000231 RID: 561
	public class ItemShortcut : MonoBehaviour
	{
		// Token: 0x17000304 RID: 772
		// (get) Token: 0x0600116C RID: 4460 RVA: 0x00043ACC File Offset: 0x00041CCC
		private static CharacterMainControl Master
		{
			get
			{
				return CharacterMainControl.Main;
			}
		}

		// Token: 0x17000305 RID: 773
		// (get) Token: 0x0600116D RID: 4461 RVA: 0x00043AD3 File Offset: 0x00041CD3
		private static Inventory MainInventory
		{
			get
			{
				if (ItemShortcut.Master == null)
				{
					return null;
				}
				if (!ItemShortcut.Master.CharacterItem)
				{
					return null;
				}
				return ItemShortcut.Master.CharacterItem.Inventory;
			}
		}

		// Token: 0x17000306 RID: 774
		// (get) Token: 0x0600116E RID: 4462 RVA: 0x00043B06 File Offset: 0x00041D06
		public static int MaxIndex
		{
			get
			{
				if (ItemShortcut.Instance == null)
				{
					return 0;
				}
				return ItemShortcut.Instance.maxIndex;
			}
		}

		// Token: 0x0600116F RID: 4463 RVA: 0x00043B24 File Offset: 0x00041D24
		private void Awake()
		{
			if (ItemShortcut.Instance == null)
			{
				ItemShortcut.Instance = this;
			}
			else
			{
				Debug.LogError("检测到多个ItemShortcut");
			}
			SavesSystem.OnCollectSaveData += this.OnCollectSaveData;
			SavesSystem.OnSetFile += this.OnSetSaveFile;
			LevelManager.OnLevelInitialized += this.OnLevelInitialized;
		}

		// Token: 0x06001170 RID: 4464 RVA: 0x00043B83 File Offset: 0x00041D83
		private void OnDestroy()
		{
			SavesSystem.OnCollectSaveData -= this.OnCollectSaveData;
			SavesSystem.OnSetFile -= this.OnSetSaveFile;
			LevelManager.OnLevelInitialized -= this.OnLevelInitialized;
		}

		// Token: 0x06001171 RID: 4465 RVA: 0x00043BB8 File Offset: 0x00041DB8
		private void Start()
		{
			this.Load();
		}

		// Token: 0x06001172 RID: 4466 RVA: 0x00043BC0 File Offset: 0x00041DC0
		private void OnLevelInitialized()
		{
			this.Load();
		}

		// Token: 0x06001173 RID: 4467 RVA: 0x00043BC8 File Offset: 0x00041DC8
		private void OnSetSaveFile()
		{
			this.Load();
		}

		// Token: 0x06001174 RID: 4468 RVA: 0x00043BD0 File Offset: 0x00041DD0
		private void OnCollectSaveData()
		{
			this.Save();
		}

		// Token: 0x06001175 RID: 4469 RVA: 0x00043BD8 File Offset: 0x00041DD8
		private void Load()
		{
			ItemShortcut.SaveData saveData = SavesSystem.Load<ItemShortcut.SaveData>("ItemShortcut_Data");
			if (saveData == null)
			{
				return;
			}
			saveData.ApplyTo(this);
		}

		// Token: 0x06001176 RID: 4470 RVA: 0x00043BF0 File Offset: 0x00041DF0
		private void Save()
		{
			ItemShortcut.SaveData saveData = new ItemShortcut.SaveData();
			saveData.Generate(this);
			SavesSystem.Save<ItemShortcut.SaveData>("ItemShortcut_Data", saveData);
		}

		// Token: 0x06001177 RID: 4471 RVA: 0x00043C18 File Offset: 0x00041E18
		public static bool IsItemValid(Item item)
		{
			return !(item == null) && !(ItemShortcut.MainInventory == null) && !(ItemShortcut.MainInventory != item.InInventory) && !item.Tags.Contains("Weapon");
		}

		// Token: 0x06001178 RID: 4472 RVA: 0x00043C68 File Offset: 0x00041E68
		private bool Set_Local(int index, Item item)
		{
			if (ItemShortcut.Master == null)
			{
				return false;
			}
			if (index < 0 || index > this.maxIndex)
			{
				return false;
			}
			if (!ItemShortcut.IsItemValid(item))
			{
				return false;
			}
			while (this.items.Count <= index)
			{
				this.items.Add(null);
			}
			while (this.itemTypes.Count <= index)
			{
				this.itemTypes.Add(-1);
			}
			this.items[index] = item;
			this.itemTypes[index] = item.TypeID;
			Action<int> onSetItem = ItemShortcut.OnSetItem;
			if (onSetItem != null)
			{
				onSetItem(index);
			}
			for (int i = 0; i < this.items.Count; i++)
			{
				if (i != index)
				{
					bool flag = false;
					if (this.items[i] == item)
					{
						this.items[i] = null;
						flag = true;
					}
					if (this.itemTypes[i] == item.TypeID)
					{
						this.itemTypes[i] = -1;
						this.items[i] = null;
						flag = true;
					}
					if (flag)
					{
						ItemShortcut.OnSetItem(i);
					}
				}
			}
			return true;
		}

		// Token: 0x06001179 RID: 4473 RVA: 0x00043D84 File Offset: 0x00041F84
		private Item Get_Local(int index)
		{
			if (index >= this.items.Count)
			{
				return null;
			}
			Item item = this.items[index];
			if (item == null)
			{
				item = ItemShortcut.MainInventory.Find(this.itemTypes[index]);
				if (item != null)
				{
					this.items[index] = item;
				}
			}
			if (!ItemShortcut.IsItemValid(item))
			{
				this.SetDirty(index);
				return null;
			}
			return item;
		}

		// Token: 0x0600117A RID: 4474 RVA: 0x00043DF6 File Offset: 0x00041FF6
		private void SetDirty(int index)
		{
			this.dirtyIndexes.Add(index);
		}

		// Token: 0x0600117B RID: 4475 RVA: 0x00043E08 File Offset: 0x00042008
		private void Update()
		{
			if (this.dirtyIndexes.Count > 0)
			{
				foreach (int num in this.dirtyIndexes.ToArray<int>())
				{
					if (num < this.items.Count && !ItemShortcut.IsItemValid(this.items[num]))
					{
						this.items[num] = null;
						Action<int> onSetItem = ItemShortcut.OnSetItem;
						if (onSetItem != null)
						{
							onSetItem(num);
						}
					}
				}
				this.dirtyIndexes.Clear();
			}
		}

		// Token: 0x14000078 RID: 120
		// (add) Token: 0x0600117C RID: 4476 RVA: 0x00043E8C File Offset: 0x0004208C
		// (remove) Token: 0x0600117D RID: 4477 RVA: 0x00043EC0 File Offset: 0x000420C0
		public static event Action<int> OnSetItem;

		// Token: 0x0600117E RID: 4478 RVA: 0x00043EF3 File Offset: 0x000420F3
		public static Item Get(int index)
		{
			if (ItemShortcut.Instance == null)
			{
				return null;
			}
			return ItemShortcut.Instance.Get_Local(index);
		}

		// Token: 0x0600117F RID: 4479 RVA: 0x00043F0F File Offset: 0x0004210F
		public static bool Set(int index, Item item)
		{
			return !(ItemShortcut.Instance == null) && ItemShortcut.Instance.Set_Local(index, item);
		}

		// Token: 0x04000D89 RID: 3465
		public static ItemShortcut Instance;

		// Token: 0x04000D8A RID: 3466
		[SerializeField]
		private int maxIndex = 3;

		// Token: 0x04000D8B RID: 3467
		[SerializeField]
		private List<Item> items = new List<Item>();

		// Token: 0x04000D8C RID: 3468
		[SerializeField]
		private List<int> itemTypes = new List<int>();

		// Token: 0x04000D8D RID: 3469
		private const string SaveKey = "ItemShortcut_Data";

		// Token: 0x04000D8E RID: 3470
		private HashSet<int> dirtyIndexes = new HashSet<int>();

		// Token: 0x02000529 RID: 1321
		[Serializable]
		private class SaveData
		{
			// Token: 0x17000756 RID: 1878
			// (get) Token: 0x060027BB RID: 10171 RVA: 0x000912D3 File Offset: 0x0008F4D3
			public int Count
			{
				get
				{
					return this.inventoryIndexes.Count;
				}
			}

			// Token: 0x060027BC RID: 10172 RVA: 0x000912E0 File Offset: 0x0008F4E0
			public void Generate(ItemShortcut shortcut)
			{
				this.inventoryIndexes.Clear();
				Inventory mainInventory = ItemShortcut.MainInventory;
				if (mainInventory == null)
				{
					return;
				}
				for (int i = 0; i < shortcut.items.Count; i++)
				{
					Item item = shortcut.items[i];
					int index = mainInventory.GetIndex(item);
					this.inventoryIndexes.Add(index);
				}
			}

			// Token: 0x060027BD RID: 10173 RVA: 0x00091340 File Offset: 0x0008F540
			public void ApplyTo(ItemShortcut shortcut)
			{
				Inventory mainInventory = ItemShortcut.MainInventory;
				if (mainInventory == null)
				{
					return;
				}
				for (int i = 0; i < this.inventoryIndexes.Count; i++)
				{
					int num = this.inventoryIndexes[i];
					if (num >= 0)
					{
						Item itemAt = mainInventory.GetItemAt(num);
						shortcut.Set_Local(i, itemAt);
					}
				}
			}

			// Token: 0x04001E65 RID: 7781
			[SerializeField]
			internal List<int> inventoryIndexes = new List<int>();
		}
	}
}
