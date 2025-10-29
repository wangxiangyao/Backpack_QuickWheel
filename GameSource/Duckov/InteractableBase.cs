using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Duckov;
using Duckov.MasterKeys;
using Duckov.Scenes;
using Duckov.Utilities;
using ItemStatsSystem;
using ItemStatsSystem.Items;
using SodaCraft.Localizations;
using UnityEngine;
using UnityEngine.Events;

// Token: 0x020000D9 RID: 217
public class InteractableBase : MonoBehaviour, IProgress
{
	// Token: 0x060006BC RID: 1724 RVA: 0x0001E3AC File Offset: 0x0001C5AC
	public List<InteractableBase> GetInteractableList()
	{
		this._interactbleList.Clear();
		this._interactbleList.Add(this);
		if (!this.interactableGroup || this.otherInterablesInGroup.Count <= 0)
		{
			return this._interactbleList;
		}
		foreach (InteractableBase interactableBase in this.otherInterablesInGroup)
		{
			if (!(interactableBase == null) && interactableBase.gameObject.activeInHierarchy)
			{
				this._interactbleList.Add(interactableBase);
			}
		}
		return this._interactbleList;
	}

	// Token: 0x17000139 RID: 313
	// (get) Token: 0x060006BD RID: 1725 RVA: 0x0001E454 File Offset: 0x0001C654
	public float InteractTime
	{
		get
		{
			if (this.requireItem && !this.requireItemUsed)
			{
				return this.interactTime + this.unlockTime;
			}
			return this.interactTime;
		}
	}

	// Token: 0x1700013A RID: 314
	// (get) Token: 0x060006BE RID: 1726 RVA: 0x0001E47A File Offset: 0x0001C67A
	// (set) Token: 0x060006BF RID: 1727 RVA: 0x0001E49B File Offset: 0x0001C69B
	public string InteractName
	{
		get
		{
			if (this.overrideInteractName)
			{
				return this._overrideInteractNameKey.ToPlainText();
			}
			return this.defaultInteractNameKey.ToPlainText();
		}
		set
		{
			this.overrideInteractName = true;
			this._overrideInteractNameKey = value;
		}
	}

	// Token: 0x1700013B RID: 315
	// (get) Token: 0x060006C0 RID: 1728 RVA: 0x0001E4AB File Offset: 0x0001C6AB
	private bool ShowBaseInteractName
	{
		get
		{
			return this.overrideInteractName && this.ShowBaseInteractNameInspector;
		}
	}

	// Token: 0x1700013C RID: 316
	// (get) Token: 0x060006C1 RID: 1729 RVA: 0x0001E4BD File Offset: 0x0001C6BD
	protected virtual bool ShowBaseInteractNameInspector
	{
		get
		{
			return true;
		}
	}

	// Token: 0x1700013D RID: 317
	// (get) Token: 0x060006C2 RID: 1730 RVA: 0x0001E4C0 File Offset: 0x0001C6C0
	private ItemMetaData CachedMeta
	{
		get
		{
			if (this._cachedMeta == null)
			{
				this._cachedMeta = new ItemMetaData?(ItemAssetsCollection.GetMetaData(this.requireItemId));
			}
			return this._cachedMeta.Value;
		}
	}

	// Token: 0x1400002C RID: 44
	// (add) Token: 0x060006C3 RID: 1731 RVA: 0x0001E4F0 File Offset: 0x0001C6F0
	// (remove) Token: 0x060006C4 RID: 1732 RVA: 0x0001E524 File Offset: 0x0001C724
	public static event Action<InteractableBase> OnInteractStartStaticEvent;

	// Token: 0x1700013E RID: 318
	// (get) Token: 0x060006C5 RID: 1733 RVA: 0x0001E557 File Offset: 0x0001C757
	protected virtual bool ShowUnityEvents
	{
		get
		{
			return true;
		}
	}

	// Token: 0x1700013F RID: 319
	// (get) Token: 0x060006C6 RID: 1734 RVA: 0x0001E55A File Offset: 0x0001C75A
	public bool Interacting
	{
		get
		{
			return this.interactCharacter != null;
		}
	}

	// Token: 0x17000140 RID: 320
	// (get) Token: 0x060006C7 RID: 1735 RVA: 0x0001E568 File Offset: 0x0001C768
	// (set) Token: 0x060006C8 RID: 1736 RVA: 0x0001E570 File Offset: 0x0001C770
	public bool MarkerActive
	{
		get
		{
			return this.interactMarkerVisible;
		}
		set
		{
			if (!base.enabled)
			{
				return;
			}
			this.interactMarkerVisible = value;
			if (value)
			{
				this.ActiveMarker();
				return;
			}
			if (this.markerObject)
			{
				this.markerObject.gameObject.SetActive(false);
			}
		}
	}

	// Token: 0x060006C9 RID: 1737 RVA: 0x0001E5AC File Offset: 0x0001C7AC
	protected virtual void Awake()
	{
		this.requireItemDataKeyCached = this.GetKey();
		if (this.interactCollider == null)
		{
			this.interactCollider = base.GetComponent<Collider>();
			if (this.interactCollider == null)
			{
				this.interactCollider = base.gameObject.AddComponent<BoxCollider>();
				this.interactCollider.enabled = false;
			}
		}
		if (this.interactCollider != null)
		{
			this.interactCollider.gameObject.layer = LayerMask.NameToLayer("Interactable");
		}
		foreach (InteractableBase interactableBase in this.otherInterablesInGroup)
		{
			if (interactableBase)
			{
				interactableBase.MarkerActive = false;
				interactableBase.transform.position = base.transform.position;
				interactableBase.transform.rotation = base.transform.rotation;
				interactableBase.interactMarkerOffset = this.interactMarkerOffset;
			}
		}
		this._interactbleList = new List<InteractableBase>();
	}

	// Token: 0x060006CA RID: 1738 RVA: 0x0001E6C4 File Offset: 0x0001C8C4
	protected virtual void Start()
	{
		object obj;
		if (this.requireItem && MultiSceneCore.Instance && MultiSceneCore.Instance.inLevelData.TryGetValue(this.requireItemDataKeyCached, out obj) && obj is bool)
		{
			bool flag = (bool)obj;
			if (flag)
			{
				this.requireItem = false;
				this.requireItemUsed = true;
				UnityEvent onRequiredItemUsedEvent = this.OnRequiredItemUsedEvent;
				if (onRequiredItemUsedEvent != null)
				{
					onRequiredItemUsedEvent.Invoke();
				}
			}
		}
		this.MarkerActive = this.interactMarkerVisible;
	}

	// Token: 0x060006CB RID: 1739 RVA: 0x0001E73C File Offset: 0x0001C93C
	private void ActiveMarker()
	{
		if (this.markerObject)
		{
			if (!this.markerObject.gameObject.activeInHierarchy)
			{
				this.markerObject.gameObject.SetActive(true);
			}
			return;
		}
		this.markerObject = UnityEngine.Object.Instantiate<InteractMarker>(GameplayDataSettings.Prefabs.InteractMarker, base.transform);
		this.markerObject.transform.localPosition = this.interactMarkerOffset;
		this.CheckInteractable();
	}

	// Token: 0x060006CC RID: 1740 RVA: 0x0001E7B2 File Offset: 0x0001C9B2
	public void SetMarkerUsed()
	{
		if (!this.markerObject)
		{
			return;
		}
		this.markerObject.MarkAsUsed();
	}

	// Token: 0x060006CD RID: 1741 RVA: 0x0001E7D0 File Offset: 0x0001C9D0
	public bool StartInteract(CharacterMainControl _interactCharacter)
	{
		if (!_interactCharacter)
		{
			return false;
		}
		if (this.requireItem && !this.TryGetRequiredItem(_interactCharacter).Item1)
		{
			return false;
		}
		if (this.interactCharacter == _interactCharacter)
		{
			return false;
		}
		if (!this.CheckInteractable())
		{
			return false;
		}
		if (this.requireItem && this.whenToUseRequireItem == InteractableBase.WhenToUseRequireItemTypes.OnStartInteract && !this.UseRequiredItem(_interactCharacter))
		{
			this.StopInteract();
			return false;
		}
		this.interactCharacter = _interactCharacter;
		this.interactTimer = 0f;
		this.timeOut = false;
		UnityEvent<CharacterMainControl, InteractableBase> onInteractStartEvent = this.OnInteractStartEvent;
		if (onInteractStartEvent != null)
		{
			onInteractStartEvent.Invoke(_interactCharacter, this);
		}
		Action<InteractableBase> onInteractStartStaticEvent = InteractableBase.OnInteractStartStaticEvent;
		if (onInteractStartStaticEvent != null)
		{
			onInteractStartStaticEvent(this);
		}
		try
		{
			this.OnInteractStart(_interactCharacter);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			if (CharacterMainControl.Main)
			{
				CharacterMainControl.Main.PopText("OnInteractStart开始失败，Log Error", -1f);
			}
			return false;
		}
		return true;
	}

	// Token: 0x060006CE RID: 1742 RVA: 0x0001E8C0 File Offset: 0x0001CAC0
	public InteractableBase GetInteractableInGroup(int index)
	{
		if (index == 0)
		{
			return this;
		}
		List<InteractableBase> interactableList = this.GetInteractableList();
		if (index >= interactableList.Count)
		{
			return null;
		}
		return interactableList[index];
	}

	// Token: 0x060006CF RID: 1743 RVA: 0x0001E8EB File Offset: 0x0001CAEB
	public void InternalStopInteract()
	{
		this.interactCharacter = null;
		this.lastStopTime = Time.time;
		this.OnInteractStop();
	}

	// Token: 0x060006D0 RID: 1744 RVA: 0x0001E908 File Offset: 0x0001CB08
	public void StopInteract()
	{
		CharacterMainControl characterMainControl = this.interactCharacter;
		if (characterMainControl && characterMainControl.interactAction.Running && characterMainControl.interactAction.InteractingTarget == this)
		{
			this.interactCharacter.interactAction.StopAction();
			return;
		}
		this.InternalStopInteract();
	}

	// Token: 0x060006D1 RID: 1745 RVA: 0x0001E95C File Offset: 0x0001CB5C
	public void UpdateInteract(CharacterMainControl _interactCharacter, float deltaTime)
	{
		this.interactTimer += deltaTime;
		this.OnUpdate(_interactCharacter, deltaTime);
		if (!this.timeOut && this.interactTimer >= this.InteractTime)
		{
			if (this.requireItem && this.whenToUseRequireItem == InteractableBase.WhenToUseRequireItemTypes.OnTimeOut && !this.UseRequiredItem(_interactCharacter))
			{
				this.StopInteract();
				return;
			}
			if (this.requireItem && this.whenToUseRequireItem == InteractableBase.WhenToUseRequireItemTypes.None && !this.requireItemUsed)
			{
				this.requireItemUsed = true;
				UnityEvent onRequiredItemUsedEvent = this.OnRequiredItemUsedEvent;
				if (onRequiredItemUsedEvent != null)
				{
					onRequiredItemUsedEvent.Invoke();
				}
				if (MultiSceneCore.Instance)
				{
					MultiSceneCore.Instance.inLevelData[this.requireItemDataKeyCached] = true;
					Debug.Log("设置使用过物品为true");
				}
			}
			this.timeOut = true;
			this.OnTimeOut();
			UnityEvent<CharacterMainControl, InteractableBase> onInteractTimeoutEvent = this.OnInteractTimeoutEvent;
			if (onInteractTimeoutEvent != null)
			{
				onInteractTimeoutEvent.Invoke(_interactCharacter, this);
			}
			if (this.finishWhenTimeOut)
			{
				this.FinishInteract(_interactCharacter);
			}
		}
	}

	// Token: 0x060006D2 RID: 1746 RVA: 0x0001EA4C File Offset: 0x0001CC4C
	public void FinishInteract(CharacterMainControl _interactCharacter)
	{
		if (this.requireItem && this.whenToUseRequireItem == InteractableBase.WhenToUseRequireItemTypes.OnFinshed && !this.UseRequiredItem(_interactCharacter))
		{
			this.StopInteract();
			return;
		}
		try
		{
			this.OnInteractFinished();
			UnityEvent<CharacterMainControl, InteractableBase> onInteractFinishedEvent = this.OnInteractFinishedEvent;
			if (onInteractFinishedEvent != null)
			{
				onInteractFinishedEvent.Invoke(_interactCharacter, this);
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		this.StopInteract();
		if (this.disableOnFinish)
		{
			base.enabled = false;
			if (this.markerObject)
			{
				this.markerObject.gameObject.SetActive(false);
			}
			if (this.interactCollider)
			{
				this.interactCollider.enabled = false;
			}
		}
	}

	// Token: 0x060006D3 RID: 1747 RVA: 0x0001EAFC File Offset: 0x0001CCFC
	protected virtual void OnUpdate(CharacterMainControl _interactCharacter, float deltaTime)
	{
	}

	// Token: 0x060006D4 RID: 1748 RVA: 0x0001EAFE File Offset: 0x0001CCFE
	protected virtual void OnTimeOut()
	{
	}

	// Token: 0x060006D5 RID: 1749 RVA: 0x0001EB00 File Offset: 0x0001CD00
	private bool UseRequiredItem(CharacterMainControl interactCharacter)
	{
		Debug.Log("尝试使用");
		ValueTuple<bool, Item> valueTuple = this.TryGetRequiredItem(interactCharacter);
		Item item = valueTuple.Item2;
		if (!valueTuple.Item1 || valueTuple.Item2 == null)
		{
			return false;
		}
		if (item.UseDurability)
		{
			Debug.Log("尝试消耗耐久");
			item.Durability -= 1f;
			if (item.Durability <= 0f)
			{
				item.Detach();
				item.DestroyTree();
			}
		}
		else if (!item.Stackable)
		{
			Debug.Log("尝试直接消耗掉");
			item.Detach();
			item.DestroyTree();
		}
		else
		{
			Debug.Log("尝试消耗堆叠");
			item.StackCount--;
		}
		if (this.requireOnce)
		{
			this.requireItem = false;
			this.requireItemUsed = true;
			UnityEvent onRequiredItemUsedEvent = this.OnRequiredItemUsedEvent;
			if (onRequiredItemUsedEvent != null)
			{
				onRequiredItemUsedEvent.Invoke();
			}
			if (MultiSceneCore.Instance)
			{
				MultiSceneCore.Instance.inLevelData[this.requireItemDataKeyCached] = true;
				Debug.Log("设置使用过物品为true");
			}
		}
		return true;
	}

	// Token: 0x060006D6 RID: 1750 RVA: 0x0001EC10 File Offset: 0x0001CE10
	public bool CheckInteractable()
	{
		if (this.interactCharacter != null)
		{
			if (!(this.interactCharacter.interactAction.InteractingTarget != this))
			{
				return false;
			}
			this.StopInteract();
		}
		return (Time.time - this.lastStopTime >= this.coolTime || this.coolTime <= 0f || this.lastStopTime <= 0f) && this.IsInteractable();
	}

	// Token: 0x060006D7 RID: 1751 RVA: 0x0001EC83 File Offset: 0x0001CE83
	protected virtual bool IsInteractable()
	{
		return true;
	}

	// Token: 0x060006D8 RID: 1752 RVA: 0x0001EC86 File Offset: 0x0001CE86
	protected virtual void OnInteractStart(CharacterMainControl interactCharacter)
	{
	}

	// Token: 0x060006D9 RID: 1753 RVA: 0x0001EC88 File Offset: 0x0001CE88
	protected virtual void OnInteractStop()
	{
	}

	// Token: 0x060006DA RID: 1754 RVA: 0x0001EC8A File Offset: 0x0001CE8A
	protected virtual void OnInteractFinished()
	{
	}

	// Token: 0x060006DB RID: 1755 RVA: 0x0001EC8C File Offset: 0x0001CE8C
	public string GetInteractName()
	{
		if (this.overrideInteractName)
		{
			return this.InteractName;
		}
		return "UI_Interact".ToPlainText();
	}

	// Token: 0x060006DC RID: 1756 RVA: 0x0001ECA8 File Offset: 0x0001CEA8
	public string GetRequiredItemName()
	{
		if (!this.requireItem)
		{
			return null;
		}
		return this.CachedMeta.DisplayName;
	}

	// Token: 0x060006DD RID: 1757 RVA: 0x0001ECCD File Offset: 0x0001CECD
	public Sprite GetRequireditemIcon()
	{
		if (!this.requireItem)
		{
			return null;
		}
		return this.CachedMeta.icon;
	}

	// Token: 0x060006DE RID: 1758 RVA: 0x0001ECE4 File Offset: 0x0001CEE4
	protected virtual void OnDestroy()
	{
		if (this.Interacting)
		{
			this.StopInteract();
		}
	}

	// Token: 0x060006DF RID: 1759 RVA: 0x0001ECF4 File Offset: 0x0001CEF4
	public virtual Progress GetProgress()
	{
		Progress result = default(Progress);
		if (this.Interacting && this.InteractTime > 0f)
		{
			result.inProgress = true;
			result.total = this.InteractTime;
			result.current = this.interactTimer;
		}
		else
		{
			result.inProgress = false;
		}
		return result;
	}

	// Token: 0x060006E0 RID: 1760 RVA: 0x0001ED4C File Offset: 0x0001CF4C
	[return: TupleElementNames(new string[]
	{
		"hasItem",
		"ItemInstance"
	})]
	public ValueTuple<bool, Item> TryGetRequiredItem(CharacterMainControl fromCharacter)
	{
		if (!this.requireItem)
		{
			return new ValueTuple<bool, Item>(false, null);
		}
		if (!fromCharacter)
		{
			return new ValueTuple<bool, Item>(false, null);
		}
		if (MasterKeysManager.IsActive(this.requireItemId))
		{
			return new ValueTuple<bool, Item>(true, null);
		}
		foreach (Slot slot in fromCharacter.CharacterItem.Slots)
		{
			if (slot.Content && slot.Content.TypeID == this.requireItemId)
			{
				return new ValueTuple<bool, Item>(true, slot.Content);
			}
		}
		foreach (Item item in fromCharacter.CharacterItem.Inventory)
		{
			if (item.TypeID == this.requireItemId)
			{
				return new ValueTuple<bool, Item>(true, item);
			}
			if (item.Slots != null && item.Slots.Count > 0)
			{
				foreach (Slot slot2 in item.Slots)
				{
					if (slot2.Content != null && slot2.Content.TypeID == this.requireItemId)
					{
						return new ValueTuple<bool, Item>(true, slot2.Content);
					}
				}
			}
		}
		foreach (Item item2 in LevelManager.Instance.PetProxy.Inventory)
		{
			if (item2.TypeID == this.requireItemId)
			{
				return new ValueTuple<bool, Item>(true, item2);
			}
			if (item2.Slots && item2.Slots.Count > 0)
			{
				foreach (Slot slot3 in item2.Slots)
				{
					if (slot3.Content != null && slot3.Content.TypeID == this.requireItemId)
					{
						return new ValueTuple<bool, Item>(true, slot3.Content);
					}
				}
			}
		}
		return new ValueTuple<bool, Item>(false, null);
	}

	// Token: 0x060006E1 RID: 1761 RVA: 0x0001EFDC File Offset: 0x0001D1DC
	private int GetKey()
	{
		if (this.overrideItemUsedKey)
		{
			return this.overrideItemUsedSaveKey.GetHashCode();
		}
		Vector3 vector = base.transform.position * 10f;
		int x = Mathf.RoundToInt(vector.x);
		int y = Mathf.RoundToInt(vector.y);
		int z = Mathf.RoundToInt(vector.z);
		Vector3Int vector3Int = new Vector3Int(x, y, z);
		return string.Format("Intact_{0}", vector3Int).GetHashCode();
	}

	// Token: 0x060006E2 RID: 1762 RVA: 0x0001F054 File Offset: 0x0001D254
	public void InteractWithMainCharacter()
	{
		CharacterMainControl main = CharacterMainControl.Main;
		if (main == null)
		{
			return;
		}
		main.Interact(this);
	}

	// Token: 0x060006E3 RID: 1763 RVA: 0x0001F066 File Offset: 0x0001D266
	private void OnDrawGizmos()
	{
		if (!this.interactMarkerVisible)
		{
			return;
		}
		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere(base.transform.TransformPoint(this.interactMarkerOffset), 0.1f);
	}

	// Token: 0x04000679 RID: 1657
	public bool interactableGroup;

	// Token: 0x0400067A RID: 1658
	[SerializeField]
	private List<InteractableBase> otherInterablesInGroup;

	// Token: 0x0400067B RID: 1659
	public bool zoomIn = true;

	// Token: 0x0400067C RID: 1660
	private List<InteractableBase> _interactbleList = new List<InteractableBase>();

	// Token: 0x0400067D RID: 1661
	[SerializeField]
	private float interactTime;

	// Token: 0x0400067E RID: 1662
	public bool finishWhenTimeOut = true;

	// Token: 0x0400067F RID: 1663
	private float interactTimer;

	// Token: 0x04000680 RID: 1664
	public Vector3 interactMarkerOffset;

	// Token: 0x04000681 RID: 1665
	public bool overrideInteractName;

	// Token: 0x04000682 RID: 1666
	[LocalizationKey("Default")]
	private string defaultInteractNameKey = "UI_Interact";

	// Token: 0x04000683 RID: 1667
	[LocalizationKey("Interact")]
	public string _overrideInteractNameKey;

	// Token: 0x04000684 RID: 1668
	public Collider interactCollider;

	// Token: 0x04000685 RID: 1669
	public bool requireItem;

	// Token: 0x04000686 RID: 1670
	public bool requireOnce = true;

	// Token: 0x04000687 RID: 1671
	[ItemTypeID]
	public int requireItemId;

	// Token: 0x04000688 RID: 1672
	public float unlockTime;

	// Token: 0x04000689 RID: 1673
	public bool overrideItemUsedKey;

	// Token: 0x0400068A RID: 1674
	public string overrideItemUsedSaveKey;

	// Token: 0x0400068B RID: 1675
	public InteractableBase.WhenToUseRequireItemTypes whenToUseRequireItem;

	// Token: 0x0400068C RID: 1676
	public UnityEvent OnRequiredItemUsedEvent;

	// Token: 0x0400068D RID: 1677
	private int requireItemDataKeyCached;

	// Token: 0x0400068E RID: 1678
	private bool requireItemUsed;

	// Token: 0x0400068F RID: 1679
	private ItemMetaData? _cachedMeta;

	// Token: 0x04000690 RID: 1680
	public UnityEvent<CharacterMainControl, InteractableBase> OnInteractStartEvent;

	// Token: 0x04000691 RID: 1681
	public UnityEvent<CharacterMainControl, InteractableBase> OnInteractTimeoutEvent;

	// Token: 0x04000692 RID: 1682
	public UnityEvent<CharacterMainControl, InteractableBase> OnInteractFinishedEvent;

	// Token: 0x04000694 RID: 1684
	public bool disableOnFinish;

	// Token: 0x04000695 RID: 1685
	public float coolTime;

	// Token: 0x04000696 RID: 1686
	private float lastStopTime = -1f;

	// Token: 0x04000697 RID: 1687
	protected CharacterMainControl interactCharacter;

	// Token: 0x04000698 RID: 1688
	private bool timeOut;

	// Token: 0x04000699 RID: 1689
	[SerializeField]
	private bool interactMarkerVisible = true;

	// Token: 0x0400069A RID: 1690
	private InteractMarker markerObject;

	// Token: 0x02000463 RID: 1123
	public enum WhenToUseRequireItemTypes
	{
		// Token: 0x04001B2A RID: 6954
		None,
		// Token: 0x04001B2B RID: 6955
		OnFinshed,
		// Token: 0x04001B2C RID: 6956
		OnTimeOut,
		// Token: 0x04001B2D RID: 6957
		OnStartInteract
	}
}
