using System;
using System.Collections.Generic;
using ItemStatsSystem;
using UnityEngine;
using UnityEngine.Events;

// Token: 0x020000E3 RID: 227
public class DuckovItemAgent : ItemAgent
{
	// Token: 0x1700014A RID: 330
	// (get) Token: 0x06000727 RID: 1831 RVA: 0x00020133 File Offset: 0x0001E333
	public CharacterMainControl Holder
	{
		get
		{
			return this.holder;
		}
	}

	// Token: 0x1700014B RID: 331
	// (get) Token: 0x06000728 RID: 1832 RVA: 0x0002013C File Offset: 0x0001E33C
	private Dictionary<string, Transform> SocketsDic
	{
		get
		{
			if (this._socketsDic == null)
			{
				this._socketsDic = new Dictionary<string, Transform>();
				foreach (Transform transform in this.socketsList)
				{
					this._socketsDic.Add(transform.name, transform);
				}
			}
			return this._socketsDic;
		}
	}

	// Token: 0x06000729 RID: 1833 RVA: 0x000201B4 File Offset: 0x0001E3B4
	public Transform GetSocket(string socketName, bool createNew)
	{
		Transform transform;
		bool flag = this.SocketsDic.TryGetValue(socketName, out transform);
		if (flag && transform == null)
		{
			this.SocketsDic.Remove(socketName);
		}
		if (!flag && createNew)
		{
			transform = new GameObject(socketName).transform;
			transform.SetParent(base.transform);
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			this.SocketsDic.Add(socketName, transform);
		}
		return transform;
	}

	// Token: 0x0600072A RID: 1834 RVA: 0x0002022B File Offset: 0x0001E42B
	public void SetHolder(CharacterMainControl _holder)
	{
		this.holder = _holder;
		if (this.setActiveIfMainCharacter)
		{
			this.setActiveIfMainCharacter.SetActive(_holder.IsMainCharacter);
		}
	}

	// Token: 0x0600072B RID: 1835 RVA: 0x00020252 File Offset: 0x0001E452
	public CharacterMainControl GetHolder()
	{
		return this.holder;
	}

	// Token: 0x0600072C RID: 1836 RVA: 0x0002025A File Offset: 0x0001E45A
	protected override void OnInitialize()
	{
		base.OnInitialize();
		this.InitInterfaces();
		UnityEvent onInitializdEvent = this.OnInitializdEvent;
		if (onInitializdEvent == null)
		{
			return;
		}
		onInitializdEvent.Invoke();
	}

	// Token: 0x0600072D RID: 1837 RVA: 0x00020278 File Offset: 0x0001E478
	private void InitInterfaces()
	{
		this.usableInterface = (this as IAgentUsable);
	}

	// Token: 0x1700014C RID: 332
	// (get) Token: 0x0600072E RID: 1838 RVA: 0x00020286 File Offset: 0x0001E486
	public IAgentUsable UsableInterface
	{
		get
		{
			return this.usableInterface;
		}
	}

	// Token: 0x040006D1 RID: 1745
	public HandheldSocketTypes handheldSocket = HandheldSocketTypes.normalHandheld;

	// Token: 0x040006D2 RID: 1746
	public HandheldAnimationType handAnimationType = HandheldAnimationType.normal;

	// Token: 0x040006D3 RID: 1747
	private CharacterMainControl holder;

	// Token: 0x040006D4 RID: 1748
	public UnityEvent OnInitializdEvent;

	// Token: 0x040006D5 RID: 1749
	[SerializeField]
	private List<Transform> socketsList = new List<Transform>();

	// Token: 0x040006D6 RID: 1750
	public GameObject setActiveIfMainCharacter;

	// Token: 0x040006D7 RID: 1751
	private Dictionary<string, Transform> _socketsDic;

	// Token: 0x040006D8 RID: 1752
	private IAgentUsable usableInterface;
}
