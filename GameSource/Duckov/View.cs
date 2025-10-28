using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Duckov.UI
{
	// Token: 0x020003C1 RID: 961
	public abstract class View : ManagedUIElement
	{
		// Token: 0x1700069F RID: 1695
		// (get) Token: 0x060022FA RID: 8954 RVA: 0x0007AA19 File Offset: 0x00078C19
		// (set) Token: 0x060022FB RID: 8955 RVA: 0x0007AA20 File Offset: 0x00078C20
		public static View ActiveView
		{
			get
			{
				return View._activeView;
			}
			private set
			{
				UnityEngine.Object activeView = View._activeView;
				View._activeView = value;
				if (activeView != View._activeView)
				{
					Action onActiveViewChanged = View.OnActiveViewChanged;
					if (onActiveViewChanged == null)
					{
						return;
					}
					onActiveViewChanged();
				}
			}
		}

		// Token: 0x140000EF RID: 239
		// (add) Token: 0x060022FC RID: 8956 RVA: 0x0007AA48 File Offset: 0x00078C48
		// (remove) Token: 0x060022FD RID: 8957 RVA: 0x0007AA7C File Offset: 0x00078C7C
		public static event Action OnActiveViewChanged;

		// Token: 0x060022FE RID: 8958 RVA: 0x0007AAB0 File Offset: 0x00078CB0
		protected override void Awake()
		{
			base.Awake();
			if (this.exitButton != null)
			{
				this.exitButton.onClick.AddListener(new UnityAction(base.Close));
			}
			UIInputManager.OnNavigate += this.OnNavigate;
			UIInputManager.OnConfirm += this.OnConfirm;
			UIInputManager.OnCancel += this.OnCancel;
			this.viewTabs = base.transform.parent.parent.GetComponent<ViewTabs>();
			if (this.autoClose)
			{
				base.Close();
			}
		}

		// Token: 0x060022FF RID: 8959 RVA: 0x0007AB49 File Offset: 0x00078D49
		protected override void OnDestroy()
		{
			base.OnDestroy();
			UIInputManager.OnNavigate -= this.OnNavigate;
			UIInputManager.OnConfirm -= this.OnConfirm;
			UIInputManager.OnCancel -= this.OnCancel;
		}

		// Token: 0x06002300 RID: 8960 RVA: 0x0007AB84 File Offset: 0x00078D84
		protected override void OnOpen()
		{
			this.autoClose = false;
			if (View.ActiveView != null && View.ActiveView != this)
			{
				View.ActiveView.Close();
			}
			View.ActiveView = this;
			ItemUIUtilities.Select(null);
			if (this.viewTabs != null)
			{
				this.viewTabs.Show();
			}
			if (base.gameObject == null)
			{
				Debug.LogError("GameObject不存在", base.gameObject);
			}
			InputManager.DisableInput(base.gameObject);
			AudioManager.Post(this.sfx_Open);
		}

		// Token: 0x06002301 RID: 8961 RVA: 0x0007AC16 File Offset: 0x00078E16
		protected override void OnClose()
		{
			if (View.ActiveView == this)
			{
				View.ActiveView = null;
			}
			InputManager.ActiveInput(base.gameObject);
			AudioManager.Post(this.sfx_Close);
		}

		// Token: 0x06002302 RID: 8962 RVA: 0x0007AC42 File Offset: 0x00078E42
		internal virtual void TryQuit()
		{
			base.Close();
		}

		// Token: 0x06002303 RID: 8963 RVA: 0x0007AC4A File Offset: 0x00078E4A
		public void OnNavigate(UIInputEventData eventData)
		{
			if (eventData.Used)
			{
				return;
			}
			if (View.ActiveView != this)
			{
				return;
			}
			this.OnNavigate(eventData.vector);
		}

		// Token: 0x06002304 RID: 8964 RVA: 0x0007AC6F File Offset: 0x00078E6F
		public void OnConfirm(UIInputEventData eventData)
		{
			if (eventData.Used)
			{
				return;
			}
			if (View.ActiveView != this)
			{
				return;
			}
			this.OnConfirm();
		}

		// Token: 0x06002305 RID: 8965 RVA: 0x0007AC8E File Offset: 0x00078E8E
		public void OnCancel(UIInputEventData eventData)
		{
			if (eventData.Used)
			{
				return;
			}
			if (View.ActiveView == null || View.ActiveView != this)
			{
				return;
			}
			this.OnCancel();
			if (!eventData.Used)
			{
				this.TryQuit();
				eventData.Use();
			}
		}

		// Token: 0x06002306 RID: 8966 RVA: 0x0007ACCE File Offset: 0x00078ECE
		protected virtual void OnNavigate(Vector2 vector)
		{
		}

		// Token: 0x06002307 RID: 8967 RVA: 0x0007ACD0 File Offset: 0x00078ED0
		protected virtual void OnConfirm()
		{
		}

		// Token: 0x06002308 RID: 8968 RVA: 0x0007ACD2 File Offset: 0x00078ED2
		protected virtual void OnCancel()
		{
		}

		// Token: 0x06002309 RID: 8969 RVA: 0x0007ACD4 File Offset: 0x00078ED4
		protected static T GetViewInstance<T>() where T : View
		{
			return GameplayUIManager.GetViewInstance<T>();
		}

		// Token: 0x040017C8 RID: 6088
		[HideInInspector]
		private static View _activeView;

		// Token: 0x040017CA RID: 6090
		[SerializeField]
		private ViewTabs viewTabs;

		// Token: 0x040017CB RID: 6091
		[SerializeField]
		private Button exitButton;

		// Token: 0x040017CC RID: 6092
		[SerializeField]
		private string sfx_Open;

		// Token: 0x040017CD RID: 6093
		[SerializeField]
		private string sfx_Close;

		// Token: 0x040017CE RID: 6094
		private bool autoClose = true;
	}
}
