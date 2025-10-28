using System;
using Duckov.UI;
using UnityEngine;
using UnityEngine.InputSystem;

// Token: 0x02000172 RID: 370
public class UIInputManager : MonoBehaviour
{
	// Token: 0x1700021C RID: 540
	// (get) Token: 0x06000B1C RID: 2844 RVA: 0x0002F5D9 File Offset: 0x0002D7D9
	public static UIInputManager Instance
	{
		get
		{
			return GameManager.UiInputManager;
		}
	}

	// Token: 0x14000051 RID: 81
	// (add) Token: 0x06000B1D RID: 2845 RVA: 0x0002F5E0 File Offset: 0x0002D7E0
	// (remove) Token: 0x06000B1E RID: 2846 RVA: 0x0002F614 File Offset: 0x0002D814
	public static event Action<UIInputEventData> OnNavigate;

	// Token: 0x14000052 RID: 82
	// (add) Token: 0x06000B1F RID: 2847 RVA: 0x0002F648 File Offset: 0x0002D848
	// (remove) Token: 0x06000B20 RID: 2848 RVA: 0x0002F67C File Offset: 0x0002D87C
	public static event Action<UIInputEventData> OnConfirm;

	// Token: 0x14000053 RID: 83
	// (add) Token: 0x06000B21 RID: 2849 RVA: 0x0002F6B0 File Offset: 0x0002D8B0
	// (remove) Token: 0x06000B22 RID: 2850 RVA: 0x0002F6E4 File Offset: 0x0002D8E4
	public static event Action<UIInputEventData> OnToggleIndicatorHUD;

	// Token: 0x14000054 RID: 84
	// (add) Token: 0x06000B23 RID: 2851 RVA: 0x0002F718 File Offset: 0x0002D918
	// (remove) Token: 0x06000B24 RID: 2852 RVA: 0x0002F74C File Offset: 0x0002D94C
	public static event Action<UIInputEventData> OnCancelEarly;

	// Token: 0x14000055 RID: 85
	// (add) Token: 0x06000B25 RID: 2853 RVA: 0x0002F780 File Offset: 0x0002D980
	// (remove) Token: 0x06000B26 RID: 2854 RVA: 0x0002F7B4 File Offset: 0x0002D9B4
	public static event Action<UIInputEventData> OnCancel;

	// Token: 0x14000056 RID: 86
	// (add) Token: 0x06000B27 RID: 2855 RVA: 0x0002F7E8 File Offset: 0x0002D9E8
	// (remove) Token: 0x06000B28 RID: 2856 RVA: 0x0002F81C File Offset: 0x0002DA1C
	public static event Action<UIInputEventData> OnFastPick;

	// Token: 0x14000057 RID: 87
	// (add) Token: 0x06000B29 RID: 2857 RVA: 0x0002F850 File Offset: 0x0002DA50
	// (remove) Token: 0x06000B2A RID: 2858 RVA: 0x0002F884 File Offset: 0x0002DA84
	public static event Action<UIInputEventData> OnDropItem;

	// Token: 0x14000058 RID: 88
	// (add) Token: 0x06000B2B RID: 2859 RVA: 0x0002F8B8 File Offset: 0x0002DAB8
	// (remove) Token: 0x06000B2C RID: 2860 RVA: 0x0002F8EC File Offset: 0x0002DAEC
	public static event Action<UIInputEventData> OnUseItem;

	// Token: 0x14000059 RID: 89
	// (add) Token: 0x06000B2D RID: 2861 RVA: 0x0002F920 File Offset: 0x0002DB20
	// (remove) Token: 0x06000B2E RID: 2862 RVA: 0x0002F954 File Offset: 0x0002DB54
	public static event Action<UIInputEventData> OnToggleCameraMode;

	// Token: 0x1400005A RID: 90
	// (add) Token: 0x06000B2F RID: 2863 RVA: 0x0002F988 File Offset: 0x0002DB88
	// (remove) Token: 0x06000B30 RID: 2864 RVA: 0x0002F9BC File Offset: 0x0002DBBC
	public static event Action<UIInputEventData> OnWishlistHoveringItem;

	// Token: 0x1400005B RID: 91
	// (add) Token: 0x06000B31 RID: 2865 RVA: 0x0002F9F0 File Offset: 0x0002DBF0
	// (remove) Token: 0x06000B32 RID: 2866 RVA: 0x0002FA24 File Offset: 0x0002DC24
	public static event Action<UIInputEventData> OnNextPage;

	// Token: 0x1400005C RID: 92
	// (add) Token: 0x06000B33 RID: 2867 RVA: 0x0002FA58 File Offset: 0x0002DC58
	// (remove) Token: 0x06000B34 RID: 2868 RVA: 0x0002FA8C File Offset: 0x0002DC8C
	public static event Action<UIInputEventData> OnPreviousPage;

	// Token: 0x1400005D RID: 93
	// (add) Token: 0x06000B35 RID: 2869 RVA: 0x0002FAC0 File Offset: 0x0002DCC0
	// (remove) Token: 0x06000B36 RID: 2870 RVA: 0x0002FAF4 File Offset: 0x0002DCF4
	public static event Action<UIInputEventData> OnLockInventoryIndex;

	// Token: 0x1400005E RID: 94
	// (add) Token: 0x06000B37 RID: 2871 RVA: 0x0002FB28 File Offset: 0x0002DD28
	// (remove) Token: 0x06000B38 RID: 2872 RVA: 0x0002FB5C File Offset: 0x0002DD5C
	public static event Action<UIInputEventData, int> OnShortcutInput;

	// Token: 0x1400005F RID: 95
	// (add) Token: 0x06000B39 RID: 2873 RVA: 0x0002FB90 File Offset: 0x0002DD90
	// (remove) Token: 0x06000B3A RID: 2874 RVA: 0x0002FBC4 File Offset: 0x0002DDC4
	public static event Action<InputAction.CallbackContext> OnInteractInputContext;

	// Token: 0x1700021D RID: 541
	// (get) Token: 0x06000B3B RID: 2875 RVA: 0x0002FBF7 File Offset: 0x0002DDF7
	public static bool Ctrl
	{
		get
		{
			return Keyboard.current != null && Keyboard.current.ctrlKey.isPressed;
		}
	}

	// Token: 0x1700021E RID: 542
	// (get) Token: 0x06000B3C RID: 2876 RVA: 0x0002FC11 File Offset: 0x0002DE11
	public static bool Alt
	{
		get
		{
			return Keyboard.current != null && Keyboard.current.altKey.isPressed;
		}
	}

	// Token: 0x1700021F RID: 543
	// (get) Token: 0x06000B3D RID: 2877 RVA: 0x0002FC2B File Offset: 0x0002DE2B
	public static bool Shift
	{
		get
		{
			return Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
		}
	}

	// Token: 0x17000220 RID: 544
	// (get) Token: 0x06000B3E RID: 2878 RVA: 0x0002FC48 File Offset: 0x0002DE48
	public static Vector2 Point
	{
		get
		{
			if (!Application.isPlaying)
			{
				return default(Vector2);
			}
			if (UIInputManager.Instance == null)
			{
				return default(Vector2);
			}
			if (UIInputManager.Instance.inputActionPoint == null)
			{
				return default(Vector2);
			}
			return UIInputManager.Instance.inputActionPoint.ReadValue<Vector2>();
		}
	}

	// Token: 0x17000221 RID: 545
	// (get) Token: 0x06000B3F RID: 2879 RVA: 0x0002FCA4 File Offset: 0x0002DEA4
	public static Vector2 MouseDelta
	{
		get
		{
			if (!Application.isPlaying)
			{
				return default(Vector2);
			}
			if (UIInputManager.Instance == null)
			{
				return default(Vector2);
			}
			if (UIInputManager.Instance.inputActionMouseDelta == null)
			{
				return default(Vector2);
			}
			return UIInputManager.Instance.inputActionMouseDelta.ReadValue<Vector2>();
		}
	}

	// Token: 0x17000222 RID: 546
	// (get) Token: 0x06000B40 RID: 2880 RVA: 0x0002FCFE File Offset: 0x0002DEFE
	public static bool WasClickedThisFrame
	{
		get
		{
			return Application.isPlaying && !(UIInputManager.Instance == null) && UIInputManager.Instance.inputActionMouseClick != null && UIInputManager.Instance.inputActionMouseClick.WasPressedThisFrame();
		}
	}

	// Token: 0x06000B41 RID: 2881 RVA: 0x0002FD38 File Offset: 0x0002DF38
	public static Ray GetPointRay()
	{
		if (UIInputManager.Instance == null)
		{
			return default(Ray);
		}
		GameCamera instance = GameCamera.Instance;
		if (instance == null)
		{
			return default(Ray);
		}
		return instance.renderCamera.ScreenPointToRay(UIInputManager.Point);
	}

	// Token: 0x06000B42 RID: 2882 RVA: 0x0002FD8C File Offset: 0x0002DF8C
	private void Awake()
	{
		if (UIInputManager.Instance != this)
		{
			return;
		}
		InputActionAsset actions = GameManager.MainPlayerInput.actions;
		this.inputActionNavigate = actions["UI_Navigate"];
		this.inputActionConfirm = actions["UI_Confirm"];
		this.inputActionCancel = actions["UI_Cancel"];
		this.inputActionPoint = actions["Point"];
		this.inputActionFastPick = actions["Interact"];
		this.inputActionDropItem = actions["UI_Item_Drop"];
		this.inputActionUseItem = actions["UI_Item_use"];
		this.inputActionToggleIndicatorHUD = actions["UI_ToggleIndicatorHUD"];
		this.inputActionToggleCameraMode = actions["UI_ToggleCameraMode"];
		this.inputActionWishlistHoveringItem = actions["UI_WishlistHoveringItem"];
		this.inputActionNextPage = actions["UI_NextPage"];
		this.inputActionPreviousPage = actions["UI_PreviousPage"];
		this.inputActionLockInventoryIndex = actions["UI_LockInventoryIndex"];
		this.inputActionMouseDelta = actions["MouseDelta"];
		this.inputActionMouseClick = actions["Click"];
		this.inputActionInteract = actions["Interact"];
		this.Bind(this.inputActionNavigate, new Action<InputAction.CallbackContext>(this.OnInputActionNavigate));
		this.Bind(this.inputActionConfirm, new Action<InputAction.CallbackContext>(this.OnInputActionConfirm));
		this.Bind(this.inputActionCancel, new Action<InputAction.CallbackContext>(this.OnInputActionCancel));
		this.Bind(this.inputActionFastPick, new Action<InputAction.CallbackContext>(this.OnInputActionFastPick));
		this.Bind(this.inputActionDropItem, new Action<InputAction.CallbackContext>(this.OnInputActionDropItem));
		this.Bind(this.inputActionUseItem, new Action<InputAction.CallbackContext>(this.OnInputActionUseItem));
		this.Bind(this.inputActionToggleIndicatorHUD, new Action<InputAction.CallbackContext>(this.OnInputActionToggleIndicatorHUD));
		this.Bind(this.inputActionToggleCameraMode, new Action<InputAction.CallbackContext>(this.OnInputActionToggleCameraMode));
		this.Bind(this.inputActionWishlistHoveringItem, new Action<InputAction.CallbackContext>(this.OnInputWishlistHoveringItem));
		this.Bind(this.inputActionNextPage, new Action<InputAction.CallbackContext>(this.OnInputActionNextPage));
		this.Bind(this.inputActionPreviousPage, new Action<InputAction.CallbackContext>(this.OnInputActionPrevioursPage));
		this.Bind(this.inputActionLockInventoryIndex, new Action<InputAction.CallbackContext>(this.OnInputActionLockInventoryIndex));
		this.Bind(this.inputActionInteract, new Action<InputAction.CallbackContext>(this.OnInputActionInteract));
	}

	// Token: 0x06000B43 RID: 2883 RVA: 0x0002FFFC File Offset: 0x0002E1FC
	private void OnDestroy()
	{
		this.UnBind(this.inputActionNavigate, new Action<InputAction.CallbackContext>(this.OnInputActionNavigate));
		this.UnBind(this.inputActionConfirm, new Action<InputAction.CallbackContext>(this.OnInputActionConfirm));
		this.UnBind(this.inputActionCancel, new Action<InputAction.CallbackContext>(this.OnInputActionCancel));
		this.UnBind(this.inputActionFastPick, new Action<InputAction.CallbackContext>(this.OnInputActionFastPick));
		this.UnBind(this.inputActionUseItem, new Action<InputAction.CallbackContext>(this.OnInputActionUseItem));
		this.UnBind(this.inputActionToggleIndicatorHUD, new Action<InputAction.CallbackContext>(this.OnInputActionToggleIndicatorHUD));
		this.UnBind(this.inputActionToggleCameraMode, new Action<InputAction.CallbackContext>(this.OnInputActionToggleCameraMode));
		this.UnBind(this.inputActionWishlistHoveringItem, new Action<InputAction.CallbackContext>(this.OnInputWishlistHoveringItem));
		this.UnBind(this.inputActionNextPage, new Action<InputAction.CallbackContext>(this.OnInputActionNextPage));
		this.UnBind(this.inputActionPreviousPage, new Action<InputAction.CallbackContext>(this.OnInputActionPrevioursPage));
		this.UnBind(this.inputActionLockInventoryIndex, new Action<InputAction.CallbackContext>(this.OnInputActionLockInventoryIndex));
		this.UnBind(this.inputActionInteract, new Action<InputAction.CallbackContext>(this.OnInputActionInteract));
	}

	// Token: 0x06000B44 RID: 2884 RVA: 0x00030129 File Offset: 0x0002E329
	private void OnInputActionInteract(InputAction.CallbackContext context)
	{
		Action<InputAction.CallbackContext> onInteractInputContext = UIInputManager.OnInteractInputContext;
		if (onInteractInputContext == null)
		{
			return;
		}
		onInteractInputContext(context);
	}

	// Token: 0x06000B45 RID: 2885 RVA: 0x0003013B File Offset: 0x0002E33B
	private void OnInputActionLockInventoryIndex(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			Action<UIInputEventData> onLockInventoryIndex = UIInputManager.OnLockInventoryIndex;
			if (onLockInventoryIndex == null)
			{
				return;
			}
			onLockInventoryIndex(new UIInputEventData());
		}
	}

	// Token: 0x06000B46 RID: 2886 RVA: 0x0003015A File Offset: 0x0002E35A
	private void OnInputActionNextPage(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			Action<UIInputEventData> onNextPage = UIInputManager.OnNextPage;
			if (onNextPage == null)
			{
				return;
			}
			onNextPage(new UIInputEventData());
		}
	}

	// Token: 0x06000B47 RID: 2887 RVA: 0x00030179 File Offset: 0x0002E379
	private void OnInputActionPrevioursPage(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			Action<UIInputEventData> onPreviousPage = UIInputManager.OnPreviousPage;
			if (onPreviousPage == null)
			{
				return;
			}
			onPreviousPage(new UIInputEventData());
		}
	}

	// Token: 0x06000B48 RID: 2888 RVA: 0x00030198 File Offset: 0x0002E398
	private void OnInputWishlistHoveringItem(InputAction.CallbackContext context)
	{
		if (!context.started)
		{
			return;
		}
		Action<UIInputEventData> onWishlistHoveringItem = UIInputManager.OnWishlistHoveringItem;
		if (onWishlistHoveringItem == null)
		{
			return;
		}
		onWishlistHoveringItem(new UIInputEventData());
	}

	// Token: 0x06000B49 RID: 2889 RVA: 0x000301B8 File Offset: 0x0002E3B8
	private void OnInputActionToggleCameraMode(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			Action<UIInputEventData> onToggleCameraMode = UIInputManager.OnToggleCameraMode;
			if (onToggleCameraMode == null)
			{
				return;
			}
			onToggleCameraMode(new UIInputEventData());
		}
	}

	// Token: 0x06000B4A RID: 2890 RVA: 0x000301D7 File Offset: 0x0002E3D7
	private void OnInputActionDropItem(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			Action<UIInputEventData> onDropItem = UIInputManager.OnDropItem;
			if (onDropItem == null)
			{
				return;
			}
			onDropItem(new UIInputEventData());
		}
	}

	// Token: 0x06000B4B RID: 2891 RVA: 0x000301F6 File Offset: 0x0002E3F6
	private void OnInputActionUseItem(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			Action<UIInputEventData> onUseItem = UIInputManager.OnUseItem;
			if (onUseItem == null)
			{
				return;
			}
			onUseItem(new UIInputEventData());
		}
	}

	// Token: 0x06000B4C RID: 2892 RVA: 0x00030215 File Offset: 0x0002E415
	private void OnInputActionFastPick(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			Action<UIInputEventData> onFastPick = UIInputManager.OnFastPick;
			if (onFastPick == null)
			{
				return;
			}
			onFastPick(new UIInputEventData());
		}
	}

	// Token: 0x06000B4D RID: 2893 RVA: 0x00030234 File Offset: 0x0002E434
	private void OnInputActionCancel(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			UIInputEventData uiinputEventData = new UIInputEventData
			{
				cancel = true
			};
			Action<UIInputEventData> onCancelEarly = UIInputManager.OnCancelEarly;
			if (onCancelEarly != null)
			{
				onCancelEarly(uiinputEventData);
			}
			if (uiinputEventData.Used)
			{
				return;
			}
			Action<UIInputEventData> onCancel = UIInputManager.OnCancel;
			if (onCancel != null)
			{
				onCancel(uiinputEventData);
			}
			if (uiinputEventData.Used)
			{
				return;
			}
			if (LevelManager.Instance != null && View.ActiveView == null)
			{
				PauseMenu.Toggle();
			}
		}
	}

	// Token: 0x06000B4E RID: 2894 RVA: 0x000302AA File Offset: 0x0002E4AA
	private void OnInputActionConfirm(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			Action<UIInputEventData> onConfirm = UIInputManager.OnConfirm;
			if (onConfirm == null)
			{
				return;
			}
			onConfirm(new UIInputEventData
			{
				confirm = true
			});
		}
	}

	// Token: 0x06000B4F RID: 2895 RVA: 0x000302D0 File Offset: 0x0002E4D0
	private void OnInputActionNavigate(InputAction.CallbackContext context)
	{
		Vector2 vector = context.ReadValue<Vector2>();
		Action<UIInputEventData> onNavigate = UIInputManager.OnNavigate;
		if (onNavigate == null)
		{
			return;
		}
		onNavigate(new UIInputEventData
		{
			vector = vector
		});
	}

	// Token: 0x06000B50 RID: 2896 RVA: 0x00030300 File Offset: 0x0002E500
	private void OnInputActionToggleIndicatorHUD(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			Action<UIInputEventData> onToggleIndicatorHUD = UIInputManager.OnToggleIndicatorHUD;
			if (onToggleIndicatorHUD == null)
			{
				return;
			}
			onToggleIndicatorHUD(new UIInputEventData());
		}
	}

	// Token: 0x06000B51 RID: 2897 RVA: 0x0003031F File Offset: 0x0002E51F
	private void Bind(InputAction inputAction, Action<InputAction.CallbackContext> action)
	{
		inputAction.Enable();
		inputAction.started += action;
		inputAction.performed += action;
		inputAction.canceled += action;
	}

	// Token: 0x06000B52 RID: 2898 RVA: 0x0003033C File Offset: 0x0002E53C
	private void UnBind(InputAction inputAction, Action<InputAction.CallbackContext> action)
	{
		if (inputAction != null)
		{
			inputAction.started -= action;
			inputAction.performed -= action;
			inputAction.canceled -= action;
		}
	}

	// Token: 0x06000B53 RID: 2899 RVA: 0x00030356 File Offset: 0x0002E556
	internal static void NotifyShortcutInput(int index)
	{
		UIInputManager.OnShortcutInput(new UIInputEventData
		{
			confirm = true
		}, index);
	}

	// Token: 0x04000989 RID: 2441
	private static bool instantiated;

	// Token: 0x0400098A RID: 2442
	private InputAction inputActionNavigate;

	// Token: 0x0400098B RID: 2443
	private InputAction inputActionConfirm;

	// Token: 0x0400098C RID: 2444
	private InputAction inputActionCancel;

	// Token: 0x0400098D RID: 2445
	private InputAction inputActionPoint;

	// Token: 0x0400098E RID: 2446
	private InputAction inputActionMouseDelta;

	// Token: 0x0400098F RID: 2447
	private InputAction inputActionMouseClick;

	// Token: 0x04000990 RID: 2448
	private InputAction inputActionFastPick;

	// Token: 0x04000991 RID: 2449
	private InputAction inputActionDropItem;

	// Token: 0x04000992 RID: 2450
	private InputAction inputActionUseItem;

	// Token: 0x04000993 RID: 2451
	private InputAction inputActionToggleIndicatorHUD;

	// Token: 0x04000994 RID: 2452
	private InputAction inputActionToggleCameraMode;

	// Token: 0x04000995 RID: 2453
	private InputAction inputActionWishlistHoveringItem;

	// Token: 0x04000996 RID: 2454
	private InputAction inputActionNextPage;

	// Token: 0x04000997 RID: 2455
	private InputAction inputActionPreviousPage;

	// Token: 0x04000998 RID: 2456
	private InputAction inputActionLockInventoryIndex;

	// Token: 0x04000999 RID: 2457
	private InputAction inputActionInteract;
}
