using System;
using Duckov.Utilities;
using UnityEngine;

namespace Duckov.UI
{
	// Token: 0x020003AC RID: 940
	public class ItemShortcutEditorPanel : MonoBehaviour
	{
		// Token: 0x17000673 RID: 1651
		// (get) Token: 0x060021D7 RID: 8663 RVA: 0x00076170 File Offset: 0x00074370
		private PrefabPool<ItemShortcutEditorEntry> EntryPool
		{
			get
			{
				if (this._entryPool == null)
				{
					this._entryPool = new PrefabPool<ItemShortcutEditorEntry>(this.entryTemplate, this.entryTemplate.transform.parent, null, null, null, true, 10, 10000, null);
					this.entryTemplate.gameObject.SetActive(false);
				}
				return this._entryPool;
			}
		}

		// Token: 0x060021D8 RID: 8664 RVA: 0x000761C9 File Offset: 0x000743C9
		private void OnEnable()
		{
			this.Setup();
		}

		// Token: 0x060021D9 RID: 8665 RVA: 0x000761D4 File Offset: 0x000743D4
		private void Setup()
		{
			this.EntryPool.ReleaseAll();
			for (int i = 0; i <= ItemShortcut.MaxIndex; i++)
			{
				ItemShortcutEditorEntry itemShortcutEditorEntry = this.EntryPool.Get(this.entryTemplate.transform.parent);
				itemShortcutEditorEntry.Setup(i);
				itemShortcutEditorEntry.transform.SetAsLastSibling();
			}
		}

		// Token: 0x040016DC RID: 5852
		[SerializeField]
		private ItemShortcutEditorEntry entryTemplate;

		// Token: 0x040016DD RID: 5853
		private PrefabPool<ItemShortcutEditorEntry> _entryPool;
	}
}
