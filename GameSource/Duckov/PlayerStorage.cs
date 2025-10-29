using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Duckov.UI;
using ItemStatsSystem;
using ItemStatsSystem.Data;
using Saves;
using SodaCraft.Localizations;
using SodaCraft.StringUtilities;
using UnityEngine;

// Token: 0x020000F8 RID: 248
public class PlayerStorage : MonoBehaviour, IInitializedQueryHandler
{
	// Token: 0x170001AE RID: 430
	// (get) Token: 0x0600082F RID: 2095 RVA: 0x00024D5A File Offset: 0x00022F5A
	// (set) Token: 0x06000830 RID: 2096 RVA: 0x00024D61 File Offset: 0x00022F61
	public static PlayerStorage Instance { get; private set; }

	// Token: 0x14000035 RID: 53
	// (add) Token: 0x06000831 RID: 2097 RVA: 0x00024D6C File Offset: 0x00022F6C
	// (remove) Token: 0x06000832 RID: 2098 RVA: 0x00024DA0 File Offset: 0x00022FA0
	public static event Action<PlayerStorage, Inventory, int> OnPlayerStorageChange;

	// Token: 0x170001AF RID: 431
	// (get) Token: 0x06000833 RID: 2099 RVA: 0x00024DD3 File Offset: 0x00022FD3
	public static Inventory Inventory
	{
		get
		{
			if (PlayerStorage.Instance == null)
			{
				return null;
			}
			return PlayerStorage.Instance.inventory;
		}
	}

	// Token: 0x170001B0 RID: 432
	// (get) Token: 0x06000834 RID: 2100 RVA: 0x00024DEE File Offset: 0x00022FEE
	public static List<ItemTreeData> IncomingItemBuffer
	{
		get
		{
			return PlayerStorageBuffer.Buffer;
		}
	}

	// Token: 0x170001B1 RID: 433
	// (get) Token: 0x06000835 RID: 2101 RVA: 0x00024DF5 File Offset: 0x00022FF5
	public InteractableLootbox InteractableLootBox
	{
		get
		{
			return this.interactable;
		}
	}

	// Token: 0x14000036 RID: 54
	// (add) Token: 0x06000836 RID: 2102 RVA: 0x00024E00 File Offset: 0x00023000
	// (remove) Token: 0x06000837 RID: 2103 RVA: 0x00024E34 File Offset: 0x00023034
	public static event Action<PlayerStorage.StorageCapacityCalculationHolder> OnRecalculateStorageCapacity;

	// Token: 0x14000037 RID: 55
	// (add) Token: 0x06000838 RID: 2104 RVA: 0x00024E68 File Offset: 0x00023068
	// (remove) Token: 0x06000839 RID: 2105 RVA: 0x00024E9C File Offset: 0x0002309C
	public static event Action OnTakeBufferItem;

	// Token: 0x14000038 RID: 56
	// (add) Token: 0x0600083A RID: 2106 RVA: 0x00024ED0 File Offset: 0x000230D0
	// (remove) Token: 0x0600083B RID: 2107 RVA: 0x00024F04 File Offset: 0x00023104
	public static event Action<Item> OnItemAddedToBuffer;

	// Token: 0x14000039 RID: 57
	// (add) Token: 0x0600083C RID: 2108 RVA: 0x00024F38 File Offset: 0x00023138
	// (remove) Token: 0x0600083D RID: 2109 RVA: 0x00024F6C File Offset: 0x0002316C
	public static event Action OnLoadingFinished;

	// Token: 0x0600083E RID: 2110 RVA: 0x00024F9F File Offset: 0x0002319F
	public static bool IsAccessableAndNotFull()
	{
		return !(PlayerStorage.Instance == null) && !(PlayerStorage.Inventory == null) && PlayerStorage.Inventory.GetFirstEmptyPosition(0) >= 0;
	}

	// Token: 0x170001B2 RID: 434
	// (get) Token: 0x0600083F RID: 2111 RVA: 0x00024FD0 File Offset: 0x000231D0
	public int DefaultCapacity
	{
		get
		{
			return this.defaultCapacity;
		}
	}

	// Token: 0x06000840 RID: 2112 RVA: 0x00024FD8 File Offset: 0x000231D8
	public static void NotifyCapacityDirty()
	{
		PlayerStorage.needRecalculateCapacity = true;
	}

	// Token: 0x06000841 RID: 2113 RVA: 0x00024FE0 File Offset: 0x000231E0
	private void Awake()
	{
		if (PlayerStorage.Instance == null)
		{
			PlayerStorage.Instance = this;
		}
		if (PlayerStorage.Instance != this)
		{
			Debug.LogError("发现了多个Player Storage!");
			return;
		}
		if (this.interactable == null)
		{
			this.interactable = base.GetComponent<InteractableLootbox>();
		}
		this.inventory.onContentChanged += this.OnInventoryContentChanged;
		SavesSystem.OnCollectSaveData += this.SavesSystem_OnCollectSaveData;
		LevelManager.RegisterWaitForInitialization<PlayerStorage>(this);
	}

	// Token: 0x06000842 RID: 2114 RVA: 0x00025060 File Offset: 0x00023260
	private void Start()
	{
		this.Load().Forget();
	}

	// Token: 0x06000843 RID: 2115 RVA: 0x0002506D File Offset: 0x0002326D
	private void OnDestroy()
	{
		this.inventory.onContentChanged -= this.OnInventoryContentChanged;
		SavesSystem.OnCollectSaveData -= this.SavesSystem_OnCollectSaveData;
		LevelManager.UnregisterWaitForInitialization<PlayerStorage>(this);
	}

	// Token: 0x06000844 RID: 2116 RVA: 0x0002509E File Offset: 0x0002329E
	private void SavesSystem_OnSetFile()
	{
		this.Load().Forget();
	}

	// Token: 0x06000845 RID: 2117 RVA: 0x000250AB File Offset: 0x000232AB
	private void SavesSystem_OnCollectSaveData()
	{
		this.Save();
	}

	// Token: 0x06000846 RID: 2118 RVA: 0x000250B3 File Offset: 0x000232B3
	private void OnInventoryContentChanged(Inventory inventory, int index)
	{
		Action<PlayerStorage, Inventory, int> onPlayerStorageChange = PlayerStorage.OnPlayerStorageChange;
		if (onPlayerStorageChange == null)
		{
			return;
		}
		onPlayerStorageChange(this, inventory, index);
	}

	// Token: 0x06000847 RID: 2119 RVA: 0x000250C8 File Offset: 0x000232C8
	public static void Push(Item item, bool toBufferDirectly = false)
	{
		if (item == null)
		{
			return;
		}
		if (!toBufferDirectly && PlayerStorage.Inventory != null)
		{
			if (item.Stackable)
			{
				Func<Item, bool> <>9__0;
				while (item.StackCount > 0)
				{
					IEnumerable<Item> source = PlayerStorage.Inventory;
					Func<Item, bool> predicate;
					if ((predicate = <>9__0) == null)
					{
						predicate = (<>9__0 = ((Item e) => e.TypeID == item.TypeID && e.MaxStackCount > e.StackCount));
					}
					Item item2 = source.FirstOrDefault(predicate);
					if (item2 == null)
					{
						break;
					}
					item2.Combine(item);
				}
			}
			if (item != null && item.StackCount > 0)
			{
				int firstEmptyPosition = PlayerStorage.Inventory.GetFirstEmptyPosition(0);
				if (firstEmptyPosition >= 0)
				{
					PlayerStorage.Inventory.AddAt(item, firstEmptyPosition);
					return;
				}
			}
		}
		NotificationText.Push("PlayerStorage_Notification_ItemAddedToBuffer".ToPlainText().Format(new
		{
			displayName = item.DisplayName
		}));
		PlayerStorage.IncomingItemBuffer.Add(ItemTreeData.FromItem(item));
		item.Detach();
		item.DestroyTree();
		Action<Item> onItemAddedToBuffer = PlayerStorage.OnItemAddedToBuffer;
		if (onItemAddedToBuffer == null)
		{
			return;
		}
		onItemAddedToBuffer(item);
	}

	// Token: 0x06000848 RID: 2120 RVA: 0x00025206 File Offset: 0x00023406
	private void Save()
	{
		if (PlayerStorage.Loading)
		{
			return;
		}
		this.inventory.Save("PlayerStorage");
	}

	// Token: 0x170001B3 RID: 435
	// (get) Token: 0x06000849 RID: 2121 RVA: 0x00025220 File Offset: 0x00023420
	// (set) Token: 0x0600084A RID: 2122 RVA: 0x00025227 File Offset: 0x00023427
	public static bool Loading { get; private set; }

	// Token: 0x170001B4 RID: 436
	// (get) Token: 0x0600084B RID: 2123 RVA: 0x0002522F File Offset: 0x0002342F
	// (set) Token: 0x0600084C RID: 2124 RVA: 0x00025236 File Offset: 0x00023436
	public static bool TakingItem { get; private set; }

	// Token: 0x0600084D RID: 2125 RVA: 0x00025240 File Offset: 0x00023440
	private UniTask Load()
	{
		PlayerStorage.<Load>d__52 <Load>d__;
		<Load>d__.<>t__builder = AsyncUniTaskMethodBuilder.Create();
		<Load>d__.<>4__this = this;
		<Load>d__.<>1__state = -1;
		<Load>d__.<>t__builder.Start<PlayerStorage.<Load>d__52>(ref <Load>d__);
		return <Load>d__.<>t__builder.Task;
	}

	// Token: 0x0600084E RID: 2126 RVA: 0x00025283 File Offset: 0x00023483
	private void Update()
	{
		if (PlayerStorage.needRecalculateCapacity)
		{
			PlayerStorage.RecalculateStorageCapacity();
		}
	}

	// Token: 0x0600084F RID: 2127 RVA: 0x00025294 File Offset: 0x00023494
	public static int RecalculateStorageCapacity()
	{
		if (PlayerStorage.Instance == null)
		{
			return 0;
		}
		PlayerStorage.StorageCapacityCalculationHolder storageCapacityCalculationHolder = new PlayerStorage.StorageCapacityCalculationHolder();
		storageCapacityCalculationHolder.capacity = PlayerStorage.Instance.DefaultCapacity;
		Action<PlayerStorage.StorageCapacityCalculationHolder> onRecalculateStorageCapacity = PlayerStorage.OnRecalculateStorageCapacity;
		if (onRecalculateStorageCapacity != null)
		{
			onRecalculateStorageCapacity(storageCapacityCalculationHolder);
		}
		int capacity = storageCapacityCalculationHolder.capacity;
		PlayerStorage.Instance.SetCapacity(capacity);
		PlayerStorage.needRecalculateCapacity = false;
		return capacity;
	}

	// Token: 0x06000850 RID: 2128 RVA: 0x000252F0 File Offset: 0x000234F0
	private void SetCapacity(int capacity)
	{
		this.inventory.SetCapacity(capacity);
	}

	// Token: 0x06000851 RID: 2129 RVA: 0x00025300 File Offset: 0x00023500
	public static UniTask TakeBufferItem(int index)
	{
		PlayerStorage.<TakeBufferItem>d__56 <TakeBufferItem>d__;
		<TakeBufferItem>d__.<>t__builder = AsyncUniTaskMethodBuilder.Create();
		<TakeBufferItem>d__.index = index;
		<TakeBufferItem>d__.<>1__state = -1;
		<TakeBufferItem>d__.<>t__builder.Start<PlayerStorage.<TakeBufferItem>d__56>(ref <TakeBufferItem>d__);
		return <TakeBufferItem>d__.<>t__builder.Task;
	}

	// Token: 0x06000852 RID: 2130 RVA: 0x00025343 File Offset: 0x00023543
	public bool HasInitialized()
	{
		return this.initialized;
	}

	// Token: 0x0400077F RID: 1919
	[SerializeField]
	private Inventory inventory;

	// Token: 0x04000780 RID: 1920
	[SerializeField]
	private InteractableLootbox interactable;

	// Token: 0x04000785 RID: 1925
	[SerializeField]
	private int defaultCapacity = 32;

	// Token: 0x04000786 RID: 1926
	private static bool needRecalculateCapacity;

	// Token: 0x04000787 RID: 1927
	private const string inventorySaveKey = "PlayerStorage";

	// Token: 0x0400078A RID: 1930
	private bool initialized;

	// Token: 0x0200047C RID: 1148
	public class StorageCapacityCalculationHolder
	{
		// Token: 0x04001B7C RID: 7036
		public int capacity;
	}
}
