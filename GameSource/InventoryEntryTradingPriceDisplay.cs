using System;
using ItemStatsSystem;
using TMPro;
using UnityEngine;

namespace Duckov.UI
{
	// Token: 0x020003A2 RID: 930
	public class InventoryEntryTradingPriceDisplay : MonoBehaviour
	{
		// Token: 0x17000656 RID: 1622
		// (get) Token: 0x06002131 RID: 8497 RVA: 0x00073E46 File Offset: 0x00072046
		// (set) Token: 0x06002132 RID: 8498 RVA: 0x00073E4E File Offset: 0x0007204E
		public bool Selling
		{
			get
			{
				return this.selling;
			}
			set
			{
				this.selling = value;
			}
		}

		// Token: 0x06002133 RID: 8499 RVA: 0x00073E57 File Offset: 0x00072057
		private void Awake()
		{
			this.master.onRefresh += this.OnRefresh;
			TradingUIUtilities.OnActiveMerchantChanged += this.OnActiveMerchantChanged;
		}

		// Token: 0x06002134 RID: 8500 RVA: 0x00073E81 File Offset: 0x00072081
		private void OnActiveMerchantChanged(IMerchant merchant)
		{
			this.Refresh();
		}

		// Token: 0x06002135 RID: 8501 RVA: 0x00073E89 File Offset: 0x00072089
		private void Start()
		{
			this.Refresh();
		}

		// Token: 0x06002136 RID: 8502 RVA: 0x00073E91 File Offset: 0x00072091
		private void OnDestroy()
		{
			if (this.master != null)
			{
				this.master.onRefresh -= this.OnRefresh;
			}
			TradingUIUtilities.OnActiveMerchantChanged -= this.OnActiveMerchantChanged;
		}

		// Token: 0x06002137 RID: 8503 RVA: 0x00073EC9 File Offset: 0x000720C9
		private void OnRefresh(InventoryEntry entry)
		{
			this.Refresh();
		}

		// Token: 0x06002138 RID: 8504 RVA: 0x00073ED4 File Offset: 0x000720D4
		private void Refresh()
		{
			InventoryEntry inventoryEntry = this.master;
			Item item = (inventoryEntry != null) ? inventoryEntry.Content : null;
			if (item != null)
			{
				this.canvasGroup.alpha = 1f;
				string text = this.GetPrice(item).ToString(this.moneyFormat);
				this.priceText.text = text;
				return;
			}
			this.canvasGroup.alpha = 0f;
		}

		// Token: 0x06002139 RID: 8505 RVA: 0x00073F40 File Offset: 0x00072140
		private int GetPrice(Item content)
		{
			if (content == null)
			{
				return 0;
			}
			int value = content.Value;
			if (TradingUIUtilities.ActiveMerchant == null)
			{
				return value;
			}
			return TradingUIUtilities.ActiveMerchant.ConvertPrice(content, this.selling);
		}

		// Token: 0x04001684 RID: 5764
		[SerializeField]
		private InventoryEntry master;

		// Token: 0x04001685 RID: 5765
		[SerializeField]
		private CanvasGroup canvasGroup;

		// Token: 0x04001686 RID: 5766
		[SerializeField]
		private TextMeshProUGUI priceText;

		// Token: 0x04001687 RID: 5767
		[SerializeField]
		private bool selling = true;

		// Token: 0x04001688 RID: 5768
		[SerializeField]
		private string moneyFormat = "n0";
	}
}
