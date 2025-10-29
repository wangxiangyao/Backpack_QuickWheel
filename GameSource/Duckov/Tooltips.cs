using System;
using Duckov.UI.Animations;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Duckov.UI
{
	// Token: 0x02000381 RID: 897
	public class Tooltips : MonoBehaviour
	{
		// Token: 0x170005F7 RID: 1527
		// (get) Token: 0x06001F22 RID: 7970 RVA: 0x0006D48A File Offset: 0x0006B68A
		// (set) Token: 0x06001F23 RID: 7971 RVA: 0x0006D491 File Offset: 0x0006B691
		public static ITooltipsProvider CurrentProvider { get; private set; }

		// Token: 0x06001F24 RID: 7972 RVA: 0x0006D499 File Offset: 0x0006B699
		public static void NotifyEnterTooltipsProvider(ITooltipsProvider provider)
		{
			Tooltips.CurrentProvider = provider;
			Action<ITooltipsProvider> onEnterProvider = Tooltips.OnEnterProvider;
			if (onEnterProvider == null)
			{
				return;
			}
			onEnterProvider(provider);
		}

		// Token: 0x06001F25 RID: 7973 RVA: 0x0006D4B1 File Offset: 0x0006B6B1
		public static void NotifyExitTooltipsProvider(ITooltipsProvider provider)
		{
			if (Tooltips.CurrentProvider != provider)
			{
				return;
			}
			Tooltips.CurrentProvider = null;
			Action<ITooltipsProvider> onExitProvider = Tooltips.OnExitProvider;
			if (onExitProvider == null)
			{
				return;
			}
			onExitProvider(provider);
		}

		// Token: 0x06001F26 RID: 7974 RVA: 0x0006D4D4 File Offset: 0x0006B6D4
		private void Awake()
		{
			if (this.rectTransform == null)
			{
				this.rectTransform = base.GetComponent<RectTransform>();
			}
			Tooltips.OnEnterProvider = (Action<ITooltipsProvider>)Delegate.Combine(Tooltips.OnEnterProvider, new Action<ITooltipsProvider>(this.DoOnEnterProvider));
			Tooltips.OnExitProvider = (Action<ITooltipsProvider>)Delegate.Combine(Tooltips.OnExitProvider, new Action<ITooltipsProvider>(this.DoOnExitProvider));
		}

		// Token: 0x06001F27 RID: 7975 RVA: 0x0006D53C File Offset: 0x0006B73C
		private void OnDestroy()
		{
			Tooltips.OnEnterProvider = (Action<ITooltipsProvider>)Delegate.Remove(Tooltips.OnEnterProvider, new Action<ITooltipsProvider>(this.DoOnEnterProvider));
			Tooltips.OnExitProvider = (Action<ITooltipsProvider>)Delegate.Remove(Tooltips.OnExitProvider, new Action<ITooltipsProvider>(this.DoOnExitProvider));
		}

		// Token: 0x06001F28 RID: 7976 RVA: 0x0006D589 File Offset: 0x0006B789
		private void Update()
		{
			if (this.contents.gameObject.activeSelf)
			{
				this.RefreshPosition();
			}
		}

		// Token: 0x06001F29 RID: 7977 RVA: 0x0006D5A3 File Offset: 0x0006B7A3
		private void DoOnExitProvider(ITooltipsProvider provider)
		{
			this.fadeGroup.Hide();
		}

		// Token: 0x06001F2A RID: 7978 RVA: 0x0006D5B0 File Offset: 0x0006B7B0
		private void DoOnEnterProvider(ITooltipsProvider provider)
		{
			this.text.text = provider.GetTooltipsText();
			this.fadeGroup.Show();
		}

		// Token: 0x06001F2B RID: 7979 RVA: 0x0006D5D0 File Offset: 0x0006B7D0
		private unsafe void RefreshPosition()
		{
			Vector2 screenPoint = *Mouse.current.position.value;
			Vector2 v;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(this.rectTransform, screenPoint, null, out v);
			this.contents.localPosition = v;
		}

		// Token: 0x0400154B RID: 5451
		[SerializeField]
		private RectTransform rectTransform;

		// Token: 0x0400154C RID: 5452
		[SerializeField]
		private RectTransform contents;

		// Token: 0x0400154D RID: 5453
		[SerializeField]
		private FadeGroup fadeGroup;

		// Token: 0x0400154E RID: 5454
		[SerializeField]
		private TextMeshProUGUI text;

		// Token: 0x04001550 RID: 5456
		private static Action<ITooltipsProvider> OnEnterProvider;

		// Token: 0x04001551 RID: 5457
		private static Action<ITooltipsProvider> OnExitProvider;
	}
}
