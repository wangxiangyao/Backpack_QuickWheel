using System;
using System.Collections.Generic;
using System.Reflection;
using Dialogues;
using Duckov;
using Duckov.MiniMaps.UI;
using Duckov.Quests.UI;
using Duckov.UI;
using ItemStatsSystem;
using UnityEngine;
using UnityEngine.InputSystem;

// Token: 0x02000078 RID: 120
public class CharacterInputControl : MonoBehaviour
{
	// Token: 0x170000F7 RID: 247
	// (get) Token: 0x06000462 RID: 1122 RVA: 0x000142E5 File Offset: 0x000124E5
	// (set) Token: 0x06000463 RID: 1123 RVA: 0x000142EC File Offset: 0x000124EC
	public static CharacterInputControl Instance { get; private set; }

	// Token: 0x170000F8 RID: 248
	// (get) Token: 0x06000464 RID: 1124 RVA: 0x000142F4 File Offset: 0x000124F4
	private PlayerInput PlayerInput
	{
		get
		{
			return GameManager.MainPlayerInput;
		}
	}

	// Token: 0x170000F9 RID: 249
	// (get) Token: 0x06000465 RID: 1125 RVA: 0x000142FB File Offset: 0x000124FB
	private bool usingMouseAndKeyboard
	{
		get
		{
			return InputManager.InputDevice == InputManager.InputDevices.mouseKeyboard;
		}
	}

	// Token: 0x06000466 RID: 1126 RVA: 0x00014305 File Offset: 0x00012505
	private void Awake()
	{
		CharacterInputControl.Instance = this;
		this.inputActions = new CharacterInputControl.InputActionReferences(this.PlayerInput);
		this.RegisterEvents();
	}

	// Token: 0x06000467 RID: 1127 RVA: 0x00014324 File Offset: 0x00012524
	private void OnDestroy()
	{
		this.UnregisterEvent();
	}

	// Token: 0x06000468 RID: 1128 RVA: 0x0001432C File Offset: 0x0001252C
	private void RegisterEvents()
	{
		this.Bind(this.inputActions.MoveAxis, new Action<InputAction.CallbackContext>(this.OnPlayerMoveInput));
		this.Bind(this.inputActions.Run, new Action<InputAction.CallbackContext>(this.OnPlayerRunInput));
		this.Bind(this.inputActions.MousePos, new Action<InputAction.CallbackContext>(this.OnPlayerMouseMove));
		this.Bind(this.inputActions.Skill_1_StartAim, new Action<InputAction.CallbackContext>(this.OnStartCharacterSkillAim));
		this.Bind(this.inputActions.Reload, new Action<InputAction.CallbackContext>(this.OnReloadInput));
		this.Bind(this.inputActions.Interact, new Action<InputAction.CallbackContext>(this.OnInteractInput));
		this.Bind(this.inputActions.ScrollWheel, new Action<InputAction.CallbackContext>(this.OnMouseScollerInput));
		this.Bind(this.inputActions.SwitchWeapon, new Action<InputAction.CallbackContext>(this.OnSwitchWeaponInput));
		this.Bind(this.inputActions.SwitchInteractAndBulletType, new Action<InputAction.CallbackContext>(this.OnSwitchInteractAndBulletTypeInput));
		this.Bind(this.inputActions.Trigger, new Action<InputAction.CallbackContext>(this.OnPlayerTriggerInputUsingMouseKeyboard));
		this.Bind(this.inputActions.ToggleView, new Action<InputAction.CallbackContext>(this.OnToggleViewInput));
		this.Bind(this.inputActions.ToggleNightVision, new Action<InputAction.CallbackContext>(this.OnToggleNightVisionInput));
		this.Bind(this.inputActions.CancelSkill, new Action<InputAction.CallbackContext>(this.OnCancelSkillInput));
		this.Bind(this.inputActions.Dash, new Action<InputAction.CallbackContext>(this.OnDashInput));
		this.Bind(this.inputActions.ItemShortcut1, new Action<InputAction.CallbackContext>(this.OnPlayerSwitchItemAgent1));
		this.Bind(this.inputActions.ItemShortcut2, new Action<InputAction.CallbackContext>(this.OnPlayerSwitchItemAgent2));
		this.Bind(this.inputActions.ItemShortcut3, new Action<InputAction.CallbackContext>(this.OnShortCutInput3));
		this.Bind(this.inputActions.ItemShortcut4, new Action<InputAction.CallbackContext>(this.OnShortCutInput4));
		this.Bind(this.inputActions.ItemShortcut5, new Action<InputAction.CallbackContext>(this.OnShortCutInput5));
		this.Bind(this.inputActions.ItemShortcut6, new Action<InputAction.CallbackContext>(this.OnShortCutInput6));
		this.Bind(this.inputActions.ItemShortcut7, new Action<InputAction.CallbackContext>(this.OnShortCutInput7));
		this.Bind(this.inputActions.ItemShortcut8, new Action<InputAction.CallbackContext>(this.OnShortCutInput8));
		this.Bind(this.inputActions.ADS, new Action<InputAction.CallbackContext>(this.OnPlayerAdsInput));
		this.Bind(this.inputActions.UI_Inventory, new Action<InputAction.CallbackContext>(this.OnUIInventoryInput));
		this.Bind(this.inputActions.UI_Map, new Action<InputAction.CallbackContext>(this.OnUIMapInput));
		this.Bind(this.inputActions.UI_Quest, new Action<InputAction.CallbackContext>(this.OnUIQuestViewInput));
		this.Bind(this.inputActions.StopAction, new Action<InputAction.CallbackContext>(this.OnPlayerStopAction));
		this.Bind(this.inputActions.PutAway, new Action<InputAction.CallbackContext>(this.OnPutAwayInput));
		this.Bind(this.inputActions.ItemShortcut_Melee, new Action<InputAction.CallbackContext>(this.OnPlayerSwitchItemAgentMelee));
		this.Bind(this.inputActions.MouseDelta, new Action<InputAction.CallbackContext>(this.OnPlayerMouseDelta));
	}

	// Token: 0x06000469 RID: 1129 RVA: 0x0001469F File Offset: 0x0001289F
	private void UnregisterEvent()
	{
		while (this.unbindCommands.Count > 0)
		{
			this.unbindCommands.Dequeue()();
		}
	}

	// Token: 0x0600046A RID: 1130 RVA: 0x000146C4 File Offset: 0x000128C4
	private void Bind(InputAction action, Action<InputAction.CallbackContext> method)
	{
		action.performed += method;
		action.started += method;
		action.canceled += method;
		this.unbindCommands.Enqueue(delegate
		{
			this.Unbind(action, method);
		});
	}

	// Token: 0x0600046B RID: 1131 RVA: 0x00014736 File Offset: 0x00012936
	private void Unbind(InputAction action, Action<InputAction.CallbackContext> method)
	{
		action.performed -= method;
		action.started -= method;
		action.canceled -= method;
	}

	// Token: 0x0600046C RID: 1132 RVA: 0x00014750 File Offset: 0x00012950
	private void Update()
	{
		if (!this.character)
		{
			this.character = CharacterMainControl.Main;
			if (!this.character)
			{
				return;
			}
		}
		if (this.usingMouseAndKeyboard)
		{
			this.inputManager.SetMousePosition(this.mousePos);
			this.inputManager.SetAimInputUsingMouse(this.mouseDelta);
			this.inputManager.SetTrigger(this.mouseKeyboardTriggerInput, this.mouseKeyboardTriggerInputThisFrame, this.mouseKeyboardTriggerReleaseThisFrame);
			if (this.character.skillAction.holdItemSkillKeeper.CheckSkillAndBinding())
			{
				this.inputManager.SetAimType(AimTypes.handheldSkill);
				if (this.mouseKeyboardTriggerInputThisFrame)
				{
					this.inputManager.StartItemSkillAim();
				}
				else if (this.mouseKeyboardTriggerReleaseThisFrame)
				{
					Debug.Log("Release");
					this.inputManager.ReleaseItemSkill();
				}
			}
			else
			{
				this.inputManager.SetAimType(AimTypes.normalAim);
			}
			this.UpdateScollerInput();
		}
		this.mouseKeyboardTriggerInputThisFrame = false;
		this.mouseKeyboardTriggerReleaseThisFrame = false;
	}

	// Token: 0x0600046D RID: 1133 RVA: 0x00014844 File Offset: 0x00012A44
	public void OnPlayerMoveInput(InputAction.CallbackContext context)
	{
		if (context.performed)
		{
			Vector2 moveInput = context.ReadValue<Vector2>();
			this.inputManager.SetMoveInput(moveInput);
		}
		if (context.canceled)
		{
			this.inputManager.SetMoveInput(Vector2.zero);
		}
	}

	// Token: 0x0600046E RID: 1134 RVA: 0x00014888 File Offset: 0x00012A88
	public void OnPlayerRunInput(InputAction.CallbackContext context)
	{
		this.runInput = false;
		if (context.started)
		{
			this.inputManager.SetRunInput(true);
			this.runInput = true;
		}
		if (context.canceled)
		{
			this.inputManager.SetRunInput(false);
			this.runInput = false;
		}
	}

	// Token: 0x0600046F RID: 1135 RVA: 0x000148D4 File Offset: 0x00012AD4
	public void OnPlayerAdsInput(InputAction.CallbackContext context)
	{
		this.adsInput = false;
		if (context.started)
		{
			this.inputManager.SetAdsInput(true);
			this.adsInput = true;
		}
		if (context.canceled)
		{
			this.inputManager.SetAdsInput(false);
			this.adsInput = false;
		}
	}

	// Token: 0x06000470 RID: 1136 RVA: 0x00014920 File Offset: 0x00012B20
	public void OnToggleViewInput(InputAction.CallbackContext context)
	{
		if (GameManager.Paused)
		{
			return;
		}
		if (context.started)
		{
			this.inputManager.ToggleView();
		}
	}

	// Token: 0x06000471 RID: 1137 RVA: 0x0001493E File Offset: 0x00012B3E
	public void OnToggleNightVisionInput(InputAction.CallbackContext context)
	{
		if (GameManager.Paused)
		{
			return;
		}
		if (context.started)
		{
			this.inputManager.ToggleNightVision();
		}
	}

	// Token: 0x06000472 RID: 1138 RVA: 0x0001495C File Offset: 0x00012B5C
	public void OnPlayerTriggerInputUsingMouseKeyboard(InputAction.CallbackContext context)
	{
		if (InputManager.InputDevice != InputManager.InputDevices.mouseKeyboard)
		{
			return;
		}
		if (context.started)
		{
			this.mouseKeyboardTriggerInputThisFrame = true;
			this.mouseKeyboardTriggerInput = true;
			this.mouseKeyboardTriggerReleaseThisFrame = false;
			return;
		}
		if (context.canceled)
		{
			this.mouseKeyboardTriggerInputThisFrame = false;
			this.mouseKeyboardTriggerInput = false;
			this.mouseKeyboardTriggerReleaseThisFrame = true;
		}
	}

	// Token: 0x06000473 RID: 1139 RVA: 0x000149AE File Offset: 0x00012BAE
	public void OnPlayerMouseMove(InputAction.CallbackContext context)
	{
		this.mousePos = context.ReadValue<Vector2>();
	}

	// Token: 0x06000474 RID: 1140 RVA: 0x000149BD File Offset: 0x00012BBD
	public void OnPlayerMouseDelta(InputAction.CallbackContext context)
	{
		this.mouseDelta = context.ReadValue<Vector2>();
	}

	// Token: 0x06000475 RID: 1141 RVA: 0x000149CC File Offset: 0x00012BCC
	public void OnPlayerStopAction(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			this.inputManager.StopAction();
		}
	}

	// Token: 0x06000476 RID: 1142 RVA: 0x000149E2 File Offset: 0x00012BE2
	public void OnPlayerSwitchItemAgent1(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			this.inputManager.SwitchItemAgent(1);
		}
	}

	// Token: 0x06000477 RID: 1143 RVA: 0x000149F9 File Offset: 0x00012BF9
	public void OnPlayerSwitchItemAgent2(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			this.inputManager.SwitchItemAgent(2);
		}
	}

	// Token: 0x06000478 RID: 1144 RVA: 0x00014A10 File Offset: 0x00012C10
	public void OnPlayerSwitchItemAgentMelee(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			this.inputManager.SwitchItemAgent(3);
		}
	}

	// Token: 0x06000479 RID: 1145 RVA: 0x00014A27 File Offset: 0x00012C27
	public void OnStartCharacterSkillAim(InputAction.CallbackContext context)
	{
		this.inputManager.StartCharacterSkillAim();
	}

	// Token: 0x0600047A RID: 1146 RVA: 0x00014A34 File Offset: 0x00012C34
	public void OnCharacterSkillRelease()
	{
		this.inputManager.ReleaseCharacterSkill();
	}

	// Token: 0x0600047B RID: 1147 RVA: 0x00014A41 File Offset: 0x00012C41
	public void OnReloadInput(InputAction.CallbackContext context)
	{
		if (!context.performed)
		{
			return;
		}
		CharacterMainControl main = CharacterMainControl.Main;
		if (main == null)
		{
			return;
		}
		main.TryToReload(null);
	}

	// Token: 0x0600047C RID: 1148 RVA: 0x00014A60 File Offset: 0x00012C60
	public void OnUIInventoryInput(InputAction.CallbackContext context)
	{
		if (!context.performed)
		{
			return;
		}
		if (GameManager.Paused)
		{
			return;
		}
		if (DialogueUI.Active)
		{
			return;
		}
		if (SceneLoader.IsSceneLoading)
		{
			return;
		}
		if (!(View.ActiveView == null))
		{
			View.ActiveView.TryQuit();
			return;
		}
		if (LevelManager.Instance.IsBaseLevel)
		{
			PlayerStorage.Instance.InteractableLootBox.InteractWithMainCharacter();
			return;
		}
		InventoryView.Show();
	}

	// Token: 0x0600047D RID: 1149 RVA: 0x00014AC8 File Offset: 0x00012CC8
	public void OnUIQuestViewInput(InputAction.CallbackContext context)
	{
		if (!context.performed)
		{
			return;
		}
		if (GameManager.Paused)
		{
			return;
		}
		if (DialogueUI.Active)
		{
			return;
		}
		if (View.ActiveView == null)
		{
			QuestView.Show();
			return;
		}
		if (View.ActiveView is QuestView)
		{
			View.ActiveView.TryQuit();
		}
	}

	// Token: 0x0600047E RID: 1150 RVA: 0x00014B18 File Offset: 0x00012D18
	public void OnDashInput(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			this.inputManager.Dash();
		}
	}

	// Token: 0x0600047F RID: 1151 RVA: 0x00014B30 File Offset: 0x00012D30
	public void OnUIMapInput(InputAction.CallbackContext context)
	{
		if (!context.performed)
		{
			return;
		}
		if (GameManager.Paused)
		{
			return;
		}
		if (SceneLoader.IsSceneLoading)
		{
			return;
		}
		if (View.ActiveView == null)
		{
			MiniMapView.Show();
			return;
		}
		MiniMapView miniMapView = View.ActiveView as MiniMapView;
		if (miniMapView != null)
		{
			miniMapView.Close();
		}
	}

	// Token: 0x06000480 RID: 1152 RVA: 0x00014B7E File Offset: 0x00012D7E
	public void OnCancelSkillInput(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			this.inputManager.CancleSkill();
		}
	}

	// Token: 0x06000481 RID: 1153 RVA: 0x00014B95 File Offset: 0x00012D95
	public void OnInteractInput(InputAction.CallbackContext context)
	{
		if (!context.performed)
		{
			return;
		}
		this.inputManager.Interact();
	}

	// Token: 0x06000482 RID: 1154 RVA: 0x00014BAC File Offset: 0x00012DAC
	public void OnPutAwayInput(InputAction.CallbackContext context)
	{
		if (!context.performed)
		{
			return;
		}
		this.inputManager.PutAway();
	}

	// Token: 0x06000483 RID: 1155 RVA: 0x00014BC3 File Offset: 0x00012DC3
	public void OnMouseScollerInput(InputAction.CallbackContext context)
	{
		if (context.performed)
		{
			this.scollY = context.ReadValue<Vector2>().y;
		}
	}

	// Token: 0x06000484 RID: 1156 RVA: 0x00014BE0 File Offset: 0x00012DE0
	private void UpdateScollerInput()
	{
		if (Mathf.Abs(this.scollY) > 0.5f && (float)this.scollYZeroFrames > 3f)
		{
			if (ScrollWheelBehaviour.CurrentBehaviour == ScrollWheelBehaviour.Behaviour.AmmoAndInteract)
			{
				this.inputManager.SetSwitchInteractInput((this.scollY > 0f) ? 1 : -1);
				this.inputManager.SetSwitchBulletTypeInput((this.scollY > 0f) ? 1 : -1);
			}
			else
			{
				this.inputManager.SetSwitchWeaponInput((this.scollY > 0f) ? 1 : -1);
			}
		}
		if (Mathf.Abs(this.scollY) < 0.5f)
		{
			this.scollYZeroFrames++;
			return;
		}
		this.scollYZeroFrames = 0;
	}

	// Token: 0x06000485 RID: 1157 RVA: 0x00014C94 File Offset: 0x00012E94
	public void OnSwitchWeaponInput(InputAction.CallbackContext context)
	{
		if (context.performed)
		{
			float num = context.ReadValue<float>();
			this.inputManager.SetSwitchWeaponInput((num > 0f) ? -1 : 1);
		}
	}

	// Token: 0x06000486 RID: 1158 RVA: 0x00014CCC File Offset: 0x00012ECC
	public void OnSwitchInteractAndBulletTypeInput(InputAction.CallbackContext context)
	{
		if (context.performed)
		{
			float num = context.ReadValue<float>();
			this.inputManager.SetSwitchInteractInput((num > 0f) ? -1 : 1);
			this.inputManager.SetSwitchBulletTypeInput((num > 0f) ? -1 : 1);
		}
	}

	// Token: 0x06000487 RID: 1159 RVA: 0x00014D18 File Offset: 0x00012F18
	private void ShortCutInput(int index)
	{
		if (View.ActiveView != null)
		{
			UIInputManager.NotifyShortcutInput(index - 3);
			return;
		}
		Item item = ItemShortcut.Get(index - 3);
		if (item == null)
		{
			return;
		}
		if (!this.character)
		{
			return;
		}
		if (item && item.UsageUtilities && item.UsageUtilities.IsUsable(item, this.character))
		{
			this.character.UseItem(item);
			return;
		}
		if (item && item.GetBool("IsSkill", false))
		{
			this.character.ChangeHoldItem(item);
			return;
		}
		if (item && item.HasHandHeldAgent)
		{
			Debug.Log("has hand held");
			this.character.ChangeHoldItem(item);
		}
	}

	// Token: 0x06000488 RID: 1160 RVA: 0x00014DDD File Offset: 0x00012FDD
	public void OnShortCutInput3(InputAction.CallbackContext context)
	{
		if (!context.performed)
		{
			return;
		}
		this.ShortCutInput(3);
	}

	// Token: 0x06000489 RID: 1161 RVA: 0x00014DF0 File Offset: 0x00012FF0
	public void OnShortCutInput4(InputAction.CallbackContext context)
	{
		if (!context.performed)
		{
			return;
		}
		this.ShortCutInput(4);
	}

	// Token: 0x0600048A RID: 1162 RVA: 0x00014E03 File Offset: 0x00013003
	public void OnShortCutInput5(InputAction.CallbackContext context)
	{
		if (!context.performed)
		{
			return;
		}
		this.ShortCutInput(5);
	}

	// Token: 0x0600048B RID: 1163 RVA: 0x00014E16 File Offset: 0x00013016
	public void OnShortCutInput6(InputAction.CallbackContext context)
	{
		if (!context.performed)
		{
			return;
		}
		this.ShortCutInput(6);
	}

	// Token: 0x0600048C RID: 1164 RVA: 0x00014E29 File Offset: 0x00013029
	public void OnShortCutInput7(InputAction.CallbackContext context)
	{
		if (!context.performed)
		{
			return;
		}
		this.ShortCutInput(7);
	}

	// Token: 0x0600048D RID: 1165 RVA: 0x00014E3C File Offset: 0x0001303C
	public void OnShortCutInput8(InputAction.CallbackContext context)
	{
		if (!context.performed)
		{
			return;
		}
		this.ShortCutInput(8);
	}

	// Token: 0x0600048E RID: 1166 RVA: 0x00014E50 File Offset: 0x00013050
	internal static InputAction GetInputAction(string name)
	{
		if (CharacterInputControl.Instance == null)
		{
			return null;
		}
		InputAction result;
		try
		{
			result = CharacterInputControl.Instance.PlayerInput.actions[name];
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			Debug.LogError("查找 Input Action " + name + " 时发生错误, 返回null");
			result = null;
		}
		return result;
	}

	// Token: 0x0600048F RID: 1167 RVA: 0x00014EB4 File Offset: 0x000130B4
	public static bool GetChangeBulletTypeWasPressed()
	{
		return CharacterInputControl.Instance.inputActions.SwitchBulletType.WasPressedThisFrame();
	}

	// Token: 0x040003CA RID: 970
	public InputManager inputManager;

	// Token: 0x040003CB RID: 971
	private bool runInput;

	// Token: 0x040003CC RID: 972
	private bool adsInput;

	// Token: 0x040003CD RID: 973
	private bool aimDown;

	// Token: 0x040003CE RID: 974
	private Vector2 mousePos;

	// Token: 0x040003CF RID: 975
	private Vector2 mouseDelta;

	// Token: 0x040003D0 RID: 976
	private bool mouseKeyboardTriggerInput;

	// Token: 0x040003D1 RID: 977
	private bool mouseKeyboardTriggerReleaseThisFrame;

	// Token: 0x040003D2 RID: 978
	private bool mouseKeyboardTriggerInputThisFrame;

	// Token: 0x040003D3 RID: 979
	private CharacterMainControl character;

	// Token: 0x040003D4 RID: 980
	private CharacterInputControl.InputActionReferences inputActions;

	// Token: 0x040003D5 RID: 981
	private Queue<Action> unbindCommands = new Queue<Action>();

	// Token: 0x040003D6 RID: 982
	private float scollY;

	// Token: 0x040003D7 RID: 983
	private int scollYZeroFrames;

	// Token: 0x0200043B RID: 1083
	private class InputActionReferences
	{
		// Token: 0x0600262F RID: 9775 RVA: 0x00084064 File Offset: 0x00082264
		public InputActionReferences(PlayerInput playerInput)
		{
			InputActionAsset actions = playerInput.actions;
			Type typeFromHandle = typeof(CharacterInputControl.InputActionReferences);
			Type typeFromHandle2 = typeof(InputAction);
			FieldInfo[] fields = typeFromHandle.GetFields();
			foreach (FieldInfo fieldInfo in fields)
			{
				if (fieldInfo.FieldType != typeFromHandle2)
				{
					Debug.LogError(fieldInfo.FieldType.Name);
				}
				else
				{
					InputAction inputAction = actions[fieldInfo.Name];
					if (inputAction == null)
					{
						Debug.LogError("找不到名为 " + fieldInfo.Name + " 的input action");
					}
					else
					{
						fieldInfo.SetValue(this, inputAction);
					}
				}
			}
			foreach (FieldInfo fieldInfo2 in fields)
			{
				if (!(fieldInfo2.FieldType != typeFromHandle2))
				{
					fieldInfo2.GetValue(this);
				}
			}
		}

		// Token: 0x04001A4F RID: 6735
		public InputAction MoveAxis;

		// Token: 0x04001A50 RID: 6736
		public InputAction Run;

		// Token: 0x04001A51 RID: 6737
		public InputAction Aim;

		// Token: 0x04001A52 RID: 6738
		public InputAction MousePos;

		// Token: 0x04001A53 RID: 6739
		public InputAction ItemShortcut1;

		// Token: 0x04001A54 RID: 6740
		public InputAction ItemShortcut2;

		// Token: 0x04001A55 RID: 6741
		public InputAction Skill_1_StartAim;

		// Token: 0x04001A56 RID: 6742
		public InputAction Reload;

		// Token: 0x04001A57 RID: 6743
		public InputAction UI_Inventory;

		// Token: 0x04001A58 RID: 6744
		public InputAction UI_Map;

		// Token: 0x04001A59 RID: 6745
		public InputAction Interact;

		// Token: 0x04001A5A RID: 6746
		public InputAction ScrollWheel;

		// Token: 0x04001A5B RID: 6747
		public InputAction SwitchWeapon;

		// Token: 0x04001A5C RID: 6748
		public InputAction SwitchInteractAndBulletType;

		// Token: 0x04001A5D RID: 6749
		public InputAction Trigger;

		// Token: 0x04001A5E RID: 6750
		public InputAction ToggleView;

		// Token: 0x04001A5F RID: 6751
		public InputAction ToggleNightVision;

		// Token: 0x04001A60 RID: 6752
		public InputAction CancelSkill;

		// Token: 0x04001A61 RID: 6753
		public InputAction Dash;

		// Token: 0x04001A62 RID: 6754
		public InputAction ItemShortcut3;

		// Token: 0x04001A63 RID: 6755
		public InputAction ItemShortcut4;

		// Token: 0x04001A64 RID: 6756
		public InputAction ItemShortcut5;

		// Token: 0x04001A65 RID: 6757
		public InputAction ItemShortcut6;

		// Token: 0x04001A66 RID: 6758
		public InputAction ItemShortcut7;

		// Token: 0x04001A67 RID: 6759
		public InputAction ItemShortcut8;

		// Token: 0x04001A68 RID: 6760
		public InputAction ADS;

		// Token: 0x04001A69 RID: 6761
		public InputAction UI_Quest;

		// Token: 0x04001A6A RID: 6762
		public InputAction StopAction;

		// Token: 0x04001A6B RID: 6763
		public InputAction PutAway;

		// Token: 0x04001A6C RID: 6764
		public InputAction ItemShortcut_Melee;

		// Token: 0x04001A6D RID: 6765
		public InputAction MouseDelta;

		// Token: 0x04001A6E RID: 6766
		public InputAction SwitchBulletType;
	}
}
