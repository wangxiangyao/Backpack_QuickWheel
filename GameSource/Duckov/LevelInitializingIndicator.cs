using System;
using Duckov.UI.Animations;
using TMPro;
using UnityEngine;

// Token: 0x02000163 RID: 355
public class LevelInitializingIndicator : MonoBehaviour
{
	// Token: 0x06000AD0 RID: 2768 RVA: 0x0002ED54 File Offset: 0x0002CF54
	private void Awake()
	{
		SceneLoader.onBeforeSetSceneActive += this.SceneLoader_onBeforeSetSceneActive;
		SceneLoader.onAfterSceneInitialize += this.SceneLoader_onAfterSceneInitialize;
		LevelManager.OnLevelInitializingCommentChanged += this.OnCommentChanged;
		SceneLoader.OnSetLoadingComment += this.OnSetLoadingComment;
		this.fadeGroup.SkipHide();
	}

	// Token: 0x06000AD1 RID: 2769 RVA: 0x0002EDB0 File Offset: 0x0002CFB0
	private void OnSetLoadingComment(string comment)
	{
		this.levelInitializationCommentText.text = SceneLoader.LoadingComment;
	}

	// Token: 0x06000AD2 RID: 2770 RVA: 0x0002EDC2 File Offset: 0x0002CFC2
	private void OnCommentChanged(string comment)
	{
		this.levelInitializationCommentText.text = SceneLoader.LoadingComment;
	}

	// Token: 0x06000AD3 RID: 2771 RVA: 0x0002EDD4 File Offset: 0x0002CFD4
	private void OnDestroy()
	{
		SceneLoader.onBeforeSetSceneActive -= this.SceneLoader_onBeforeSetSceneActive;
		SceneLoader.onAfterSceneInitialize -= this.SceneLoader_onAfterSceneInitialize;
		LevelManager.OnLevelInitializingCommentChanged -= this.OnCommentChanged;
		SceneLoader.OnSetLoadingComment -= this.OnSetLoadingComment;
	}

	// Token: 0x06000AD4 RID: 2772 RVA: 0x0002EE25 File Offset: 0x0002D025
	private void SceneLoader_onBeforeSetSceneActive(SceneLoadingContext obj)
	{
		this.fadeGroup.Show();
		this.levelInitializationCommentText.text = LevelManager.LevelInitializingComment;
	}

	// Token: 0x06000AD5 RID: 2773 RVA: 0x0002EE42 File Offset: 0x0002D042
	private void SceneLoader_onAfterSceneInitialize(SceneLoadingContext obj)
	{
		this.fadeGroup.Hide();
	}

	// Token: 0x04000961 RID: 2401
	[SerializeField]
	private FadeGroup fadeGroup;

	// Token: 0x04000962 RID: 2402
	[SerializeField]
	private TextMeshProUGUI levelInitializationCommentText;
}
