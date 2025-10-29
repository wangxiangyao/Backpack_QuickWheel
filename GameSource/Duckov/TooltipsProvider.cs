using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Duckov.UI
{
	// Token: 0x02000382 RID: 898
	public class TooltipsProvider : MonoBehaviour, ITooltipsProvider, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		// Token: 0x06001F2D RID: 7981 RVA: 0x0006D61B File Offset: 0x0006B81B
		public string GetTooltipsText()
		{
			return this.text;
		}

		// Token: 0x06001F2E RID: 7982 RVA: 0x0006D623 File Offset: 0x0006B823
		public void OnPointerEnter(PointerEventData eventData)
		{
			if (string.IsNullOrEmpty(this.text))
			{
				return;
			}
			Tooltips.NotifyEnterTooltipsProvider(this);
		}

		// Token: 0x06001F2F RID: 7983 RVA: 0x0006D639 File Offset: 0x0006B839
		public void OnPointerExit(PointerEventData eventData)
		{
			Tooltips.NotifyExitTooltipsProvider(this);
		}

		// Token: 0x06001F30 RID: 7984 RVA: 0x0006D641 File Offset: 0x0006B841
		private void OnDisable()
		{
			Tooltips.NotifyExitTooltipsProvider(this);
		}

		// Token: 0x04001552 RID: 5458
		public string text;
	}
}
