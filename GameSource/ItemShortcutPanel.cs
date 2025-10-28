using System;
using ItemStatsSystem;
using UnityEngine;

namespace Duckov.UI
{
	// Token: 0x020003AD RID: 941
	public class ItemShortcutPanel : MonoBehaviour
	{
		// Token: 0x17000674 RID: 1652
		// (get) Token: 0x060021DB RID: 8667 RVA: 0x00076230 File Offset: 0x00074430
		// (set) Token: 0x060021DC RID: 8668 RVA: 0x00076238 File Offset: 0x00074438
		public Inventory Target { get; private set; }

		// Token: 0x17000675 RID: 1653
		// (get) Token: 0x060021DD RID: 8669 RVA: 0x00076241 File Offset: 0x00074441
		// (set) Token: 0x060021DE RID: 8670 RVA: 0x00076249 File Offset: 0x00074449
		public CharacterMainControl Character { get; internal set; }

		// Token: 0x060021DF RID: 8671 RVA: 0x00076252 File Offset: 0x00074452
		private void Awake()
		{
			LevelManager.OnLevelInitialized += this.OnLevelInitialized;
			if (LevelManager.LevelInited)
			{
				this.Initialize();
			}
		}

		// Token: 0x060021E0 RID: 8672 RVA: 0x00076272 File Offset: 0x00074472
		private void OnDestroy()
		{
			LevelManager.OnLevelInitialized -= this.OnLevelInitialized;
		}

		// Token: 0x060021E1 RID: 8673 RVA: 0x00076285 File Offset: 0x00074485
		private void OnLevelInitialized()
		{
			this.Initialize();
		}

		// Token: 0x060021E2 RID: 8674 RVA: 0x00076290 File Offset: 0x00074490
		private void Initialize()
		{
			LevelManager instance = LevelManager.Instance;
			this.Character = ((instance != null) ? instance.MainCharacter : null);
			if (this.Character == null)
			{
				return;
			}
			LevelManager instance2 = LevelManager.Instance;
			Inventory target;
			if (instance2 == null)
			{
				target = null;
			}
			else
			{
				CharacterMainControl mainCharacter = instance2.MainCharacter;
				if (mainCharacter == null)
				{
					target = null;
				}
				else
				{
					Item characterItem = mainCharacter.CharacterItem;
					target = ((characterItem != null) ? characterItem.Inventory : null);
				}
			}
			this.Target = target;
			if (this.Target == null)
			{
				return;
			}
			for (int i = 0; i < this.buttons.Length; i++)
			{
				ItemShortcutButton itemShortcutButton = this.buttons[i];
				if (!(itemShortcutButton == null))
				{
					itemShortcutButton.Initialize(this, i);
				}
			}
			this.initialized = true;
		}

		// Token: 0x040016DE RID: 5854
		[SerializeField]
		private ItemShortcutButton[] buttons;

		// Token: 0x040016E1 RID: 5857
		private bool initialized;
	}
}
