using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ItemStatsSystem;
using Saves;
using UnityEngine;

// Token: 0x0200010B RID: 267
public class PetProxy : MonoBehaviour
{
	// Token: 0x170001E9 RID: 489
	// (get) Token: 0x06000920 RID: 2336 RVA: 0x00028A92 File Offset: 0x00026C92
	public static PetProxy Instance
	{
		get
		{
			if (LevelManager.Instance == null)
			{
				return null;
			}
			return LevelManager.Instance.PetProxy;
		}
	}

	// Token: 0x170001EA RID: 490
	// (get) Token: 0x06000921 RID: 2337 RVA: 0x00028AAD File Offset: 0x00026CAD
	public static Inventory PetInventory
	{
		get
		{
			if (PetProxy.Instance == null)
			{
				return null;
			}
			return PetProxy.Instance.Inventory;
		}
	}

	// Token: 0x170001EB RID: 491
	// (get) Token: 0x06000922 RID: 2338 RVA: 0x00028AC8 File Offset: 0x00026CC8
	public Inventory Inventory
	{
		get
		{
			return this.inventory;
		}
	}

	// Token: 0x06000923 RID: 2339 RVA: 0x00028AD0 File Offset: 0x00026CD0
	private void Start()
	{
		SavesSystem.OnCollectSaveData += this.OnCollectSaveData;
		ItemSavesUtilities.LoadInventory("Inventory_Safe", this.inventory).Forget();
	}

	// Token: 0x06000924 RID: 2340 RVA: 0x00028AF8 File Offset: 0x00026CF8
	private void OnDestroy()
	{
		SavesSystem.OnCollectSaveData -= this.OnCollectSaveData;
	}

	// Token: 0x06000925 RID: 2341 RVA: 0x00028B0B File Offset: 0x00026D0B
	private void OnCollectSaveData()
	{
		this.inventory.Save("Inventory_Safe");
	}

	// Token: 0x06000926 RID: 2342 RVA: 0x00028B20 File Offset: 0x00026D20
	public void DestroyItemInBase()
	{
		if (!this.Inventory)
		{
			return;
		}
		List<Item> list = new List<Item>();
		foreach (Item item in this.Inventory)
		{
			list.Add(item);
		}
		foreach (Item item2 in list)
		{
			if (item2.Tags.Contains("DestroyInBase"))
			{
				item2.DestroyTree();
			}
		}
	}

	// Token: 0x06000927 RID: 2343 RVA: 0x00028BD4 File Offset: 0x00026DD4
	private void Update()
	{
		if (!LevelManager.LevelInited)
		{
			return;
		}
		if (LevelManager.Instance.PetCharacter == null)
		{
			return;
		}
		base.transform.position = LevelManager.Instance.PetCharacter.transform.position;
		if (this.checkTimer > 0f)
		{
			this.checkTimer -= Time.unscaledDeltaTime;
			return;
		}
		if (CharacterMainControl.Main.PetCapcity != this.inventory.Capacity)
		{
			this.inventory.SetCapacity(CharacterMainControl.Main.PetCapcity);
		}
		this.checkTimer = 1f;
	}

	// Token: 0x04000839 RID: 2105
	[SerializeField]
	private Inventory inventory;

	// Token: 0x0400083A RID: 2106
	private float checkTimer = 0.02f;
}
