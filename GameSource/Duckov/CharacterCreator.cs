using System;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Duckov.Utilities;
using ItemStatsSystem;
using UnityEngine;

// Token: 0x020000FF RID: 255
public class CharacterCreator : MonoBehaviour
{
	// Token: 0x170001B9 RID: 441
	// (get) Token: 0x06000876 RID: 2166 RVA: 0x00025D1C File Offset: 0x00023F1C
	public CharacterMainControl characterPfb
	{
		get
		{
			return GameplayDataSettings.Prefabs.CharacterPrefab;
		}
	}

	// Token: 0x06000877 RID: 2167 RVA: 0x00025D28 File Offset: 0x00023F28
	public UniTask<CharacterMainControl> CreateCharacter(Item itemInstance, CharacterModel modelPrefab, Vector3 pos, Quaternion rotation)
	{
		CharacterCreator.<CreateCharacter>d__2 <CreateCharacter>d__;
		<CreateCharacter>d__.<>t__builder = AsyncUniTaskMethodBuilder<CharacterMainControl>.Create();
		<CreateCharacter>d__.<>4__this = this;
		<CreateCharacter>d__.itemInstance = itemInstance;
		<CreateCharacter>d__.modelPrefab = modelPrefab;
		<CreateCharacter>d__.pos = pos;
		<CreateCharacter>d__.rotation = rotation;
		<CreateCharacter>d__.<>1__state = -1;
		<CreateCharacter>d__.<>t__builder.Start<CharacterCreator.<CreateCharacter>d__2>(ref <CreateCharacter>d__);
		return <CreateCharacter>d__.<>t__builder.Task;
	}

	// Token: 0x06000878 RID: 2168 RVA: 0x00025D8C File Offset: 0x00023F8C
	public UniTask<Item> LoadOrCreateCharacterItemInstance(int itemTypeID)
	{
		CharacterCreator.<LoadOrCreateCharacterItemInstance>d__3 <LoadOrCreateCharacterItemInstance>d__;
		<LoadOrCreateCharacterItemInstance>d__.<>t__builder = AsyncUniTaskMethodBuilder<Item>.Create();
		<LoadOrCreateCharacterItemInstance>d__.itemTypeID = itemTypeID;
		<LoadOrCreateCharacterItemInstance>d__.<>1__state = -1;
		<LoadOrCreateCharacterItemInstance>d__.<>t__builder.Start<CharacterCreator.<LoadOrCreateCharacterItemInstance>d__3>(ref <LoadOrCreateCharacterItemInstance>d__);
		return <LoadOrCreateCharacterItemInstance>d__.<>t__builder.Task;
	}
}
