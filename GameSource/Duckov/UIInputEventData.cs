using System;
using UnityEngine;

// Token: 0x02000173 RID: 371
public class UIInputEventData
{
	// Token: 0x17000223 RID: 547
	// (get) Token: 0x06000B55 RID: 2901 RVA: 0x00030377 File Offset: 0x0002E577
	public bool Used
	{
		get
		{
			return this.used;
		}
	}

	// Token: 0x06000B56 RID: 2902 RVA: 0x0003037F File Offset: 0x0002E57F
	public void Use()
	{
		this.used = true;
	}

	// Token: 0x040009A9 RID: 2473
	private bool used;

	// Token: 0x040009AA RID: 2474
	public Vector2 vector;

	// Token: 0x040009AB RID: 2475
	public bool confirm;

	// Token: 0x040009AC RID: 2476
	public bool cancel;
}
