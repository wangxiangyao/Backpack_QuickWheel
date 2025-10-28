using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Duckov;
using Duckov.Scenes;
using Duckov.Utilities;
using ItemStatsSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

// Token: 0x020000EB RID: 235
public static class ItemExtensions
{
	// Token: 0x060007C9 RID: 1993 RVA: 0x00022EA8 File Offset: 0x000210A8
	private static ItemAgent CreatePickupAgent(this Item itemInstance, Vector3 pos)
	{
		if (itemInstance.ActiveAgent != null)
		{
			Debug.LogError("创建pickup agent失败,已有agent:" + itemInstance.ActiveAgent.name);
			return null;
		}
		ItemAgent itemAgent = itemInstance.AgentUtilities.GetPrefab(ItemExtensions.PickupHash);
		if (itemAgent == null)
		{
			itemAgent = GameplayDataSettings.Prefabs.PickupAgentPrefab;
		}
		ItemAgent itemAgent2 = itemInstance.AgentUtilities.CreateAgent(itemAgent, ItemAgent.AgentTypes.pickUp);
		itemAgent2.transform.position = pos;
		return itemAgent2;
	}

	// Token: 0x060007CA RID: 1994 RVA: 0x00022F20 File Offset: 0x00021120
	public static ItemAgent CreateHandheldAgent(this Item itemInstance)
	{
		if (itemInstance.ActiveAgent != null)
		{
			Debug.LogError("创建pickup agent失败,已有agent");
			return null;
		}
		ItemAgent itemAgent = itemInstance.AgentUtilities.GetPrefab(ItemExtensions.HandheldHash);
		if (itemAgent == null)
		{
			itemAgent = GameplayDataSettings.Prefabs.HandheldAgentPrefab;
		}
		return itemInstance.AgentUtilities.CreateAgent(itemAgent, ItemAgent.AgentTypes.handheld);
	}

	// Token: 0x060007CB RID: 1995 RVA: 0x00022F7C File Offset: 0x0002117C
	public static DuckovItemAgent Drop(this Item item, Vector3 pos, bool createRigidbody, Vector3 dropDirection, float randomAngle)
	{
		if (item == null)
		{
			Debug.Log("尝试丢弃不存在的物体");
			return null;
		}
		item.Detach();
		if (MultiSceneCore.MainScene != null)
		{
			item.gameObject.transform.SetParent(null);
			SceneManager.MoveGameObjectToScene(item.gameObject, MultiSceneCore.MainScene.Value);
		}
		ItemAgent itemAgent = item.CreatePickupAgent(pos);
		if (MultiSceneCore.Instance)
		{
			if (itemAgent == null)
			{
				Debug.Log("创建的agent是null");
			}
			MultiSceneCore.MoveToActiveWithScene(itemAgent.gameObject, SceneManager.GetActiveScene().buildIndex);
		}
		InteractablePickup component = itemAgent.GetComponent<InteractablePickup>();
		if (createRigidbody && component != null)
		{
			component.Throw(dropDirection, randomAngle);
		}
		else
		{
			component.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(-randomAngle, randomAngle) * 0.5f, 0f);
		}
		return itemAgent as DuckovItemAgent;
	}

	// Token: 0x060007CC RID: 1996 RVA: 0x0002306C File Offset: 0x0002126C
	public static void Drop(this Item item, CharacterMainControl character, bool createRigidbody)
	{
		if (item == null)
		{
			return;
		}
		(UnityEngine.Random.insideUnitSphere * 1f).y = 0f;
		item.Drop(character.transform.position, createRigidbody, character.CurrentAimDirection, 45f);
		if (character.IsMainCharacter && LevelManager.LevelInited)
		{
			AudioManager.Post("SFX/Item/put_" + item.SoundKey, character.gameObject);
		}
	}

	// Token: 0x060007CD RID: 1997 RVA: 0x000230E8 File Offset: 0x000212E8
	public static UniTask<List<Item>> GetItemsOfAmount(this Inventory inventory, int itemTypeID, int amount)
	{
		ItemExtensions.<GetItemsOfAmount>d__6 <GetItemsOfAmount>d__;
		<GetItemsOfAmount>d__.<>t__builder = AsyncUniTaskMethodBuilder<List<Item>>.Create();
		<GetItemsOfAmount>d__.inventory = inventory;
		<GetItemsOfAmount>d__.itemTypeID = itemTypeID;
		<GetItemsOfAmount>d__.amount = amount;
		<GetItemsOfAmount>d__.<>1__state = -1;
		<GetItemsOfAmount>d__.<>t__builder.Start<ItemExtensions.<GetItemsOfAmount>d__6>(ref <GetItemsOfAmount>d__);
		return <GetItemsOfAmount>d__.<>t__builder.Task;
	}

	// Token: 0x060007CE RID: 1998 RVA: 0x0002313C File Offset: 0x0002133C
	public static bool TryFindItemsOfAmount(this IEnumerable<Inventory> inventories, int itemTypeID, int requiredAmount, out List<Item> result)
	{
		result = new List<Item>();
		int num = 0;
		foreach (Inventory inventory in inventories)
		{
			foreach (Item item in inventory)
			{
				if (item.TypeID == itemTypeID)
				{
					result.Add(item);
					num += item.StackCount;
					if (num >= requiredAmount)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	// Token: 0x060007CF RID: 1999 RVA: 0x000231DC File Offset: 0x000213DC
	public static void ConsumeItemsOfAmount(this IEnumerable<Item> itemsToBeConsumed, int amount)
	{
		List<Item> list = new List<Item>();
		int num = 0;
		foreach (Item item in itemsToBeConsumed)
		{
			list.Add(item);
			num += item.StackCount;
			if (num >= amount)
			{
				break;
			}
		}
		num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			Item item2 = list[i];
			int num2 = amount - num;
			if (num2 < item2.StackCount)
			{
				item2.StackCount -= num2;
				return;
			}
			item2.Detach();
			item2.DestroyTree();
		}
	}

	// Token: 0x060007D0 RID: 2000 RVA: 0x00023290 File Offset: 0x00021490
	private static bool TryMerge(IEnumerable<Item> itemsOfSameTypeID, out List<Item> result)
	{
		result = null;
		List<Item> list = itemsOfSameTypeID.ToList<Item>();
		list.RemoveAll((Item e) => e == null);
		if (list.Count <= 0)
		{
			return false;
		}
		int typeID = list[0].TypeID;
		foreach (Item item in list)
		{
			if (typeID != item.TypeID)
			{
				Debug.LogError("尝试融合的Item具有不同的TypeID,已取消");
				return false;
			}
		}
		if (!list[0].Stackable)
		{
			Debug.LogError("此类物品不可堆叠，已取消");
			return false;
		}
		result = new List<Item>();
		Stack<Item> stack = new Stack<Item>(list);
		Item item2 = null;
		while (stack.Count > 0)
		{
			if (item2 == null)
			{
				item2 = stack.Pop();
			}
			if (stack.Count <= 0)
			{
				result.Add(item2);
				break;
			}
			Item item3 = null;
			while (item2.StackCount < item2.MaxStackCount && stack.Count > 0)
			{
				item3 = stack.Pop();
				item2.Combine(item3);
			}
			result.Add(item2);
			if (item3 != null && item3.StackCount > 0)
			{
				if (stack.Count <= 0)
				{
					result.Add(item3);
					break;
				}
				item2 = item3;
			}
			else
			{
				item2 = null;
			}
		}
		return true;
	}

	// Token: 0x04000751 RID: 1873
	public static readonly int PickupHash = "Pickup".GetHashCode();

	// Token: 0x04000752 RID: 1874
	public static readonly int HandheldHash = "Handheld".GetHashCode();
}
