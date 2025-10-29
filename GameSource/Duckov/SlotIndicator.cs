using System;
using Duckov.Utilities;
using ItemStatsSystem.Items;
using UnityEngine;

namespace Duckov.UI
{
	// Token: 0x0200039F RID: 927
	public class SlotIndicator : MonoBehaviour, IPoolable
	{
		// Token: 0x17000654 RID: 1620
		// (get) Token: 0x0600211A RID: 8474 RVA: 0x00073BCB File Offset: 0x00071DCB
		// (set) Token: 0x0600211B RID: 8475 RVA: 0x00073BD3 File Offset: 0x00071DD3
		public Slot Target { get; private set; }

		// Token: 0x0600211C RID: 8476 RVA: 0x00073BDC File Offset: 0x00071DDC
		public void Setup(Slot target)
		{
			this.UnregisterEvents();
			this.Target = target;
			this.RegisterEvents();
			this.Refresh();
		}

		// Token: 0x0600211D RID: 8477 RVA: 0x00073BF7 File Offset: 0x00071DF7
		private void RegisterEvents()
		{
			if (this.Target == null)
			{
				return;
			}
			this.UnregisterEvents();
			this.Target.onSlotContentChanged += this.OnSlotContentChanged;
		}

		// Token: 0x0600211E RID: 8478 RVA: 0x00073C1F File Offset: 0x00071E1F
		private void UnregisterEvents()
		{
			if (this.Target == null)
			{
				return;
			}
			this.Target.onSlotContentChanged -= this.OnSlotContentChanged;
		}

		// Token: 0x0600211F RID: 8479 RVA: 0x00073C41 File Offset: 0x00071E41
		private void OnSlotContentChanged(Slot slot)
		{
			if (slot != this.Target)
			{
				Debug.LogError("Slot内容改变事件触发了，但它来自别的Slot。这说明Slot Indicator注册的事件发生了泄露，请检查代码。");
				return;
			}
			this.Refresh();
		}

		// Token: 0x06002120 RID: 8480 RVA: 0x00073C5D File Offset: 0x00071E5D
		private void Refresh()
		{
			if (this.contentIndicator == null)
			{
				return;
			}
			if (this.Target == null)
			{
				return;
			}
			this.contentIndicator.SetActive(this.Target.Content);
		}

		// Token: 0x06002121 RID: 8481 RVA: 0x00073C92 File Offset: 0x00071E92
		public void NotifyPooled()
		{
			this.RegisterEvents();
			this.Refresh();
		}

		// Token: 0x06002122 RID: 8482 RVA: 0x00073CA0 File Offset: 0x00071EA0
		public void NotifyReleased()
		{
			this.UnregisterEvents();
			this.Target = null;
			this.contentIndicator.SetActive(false);
		}

		// Token: 0x06002123 RID: 8483 RVA: 0x00073CBB File Offset: 0x00071EBB
		private void OnEnable()
		{
			this.RegisterEvents();
			this.Refresh();
		}

		// Token: 0x06002124 RID: 8484 RVA: 0x00073CC9 File Offset: 0x00071EC9
		private void OnDisable()
		{
			this.UnregisterEvents();
		}

		// Token: 0x0400167E RID: 5758
		[SerializeField]
		private GameObject contentIndicator;
	}
}
