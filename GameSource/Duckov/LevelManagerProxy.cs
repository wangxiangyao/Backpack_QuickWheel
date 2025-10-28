using System;
using Duckov.Scenes;
using UnityEngine;

// Token: 0x02000113 RID: 275
public class LevelManagerProxy : MonoBehaviour
{
	// Token: 0x06000957 RID: 2391 RVA: 0x000295FB File Offset: 0x000277FB
	public void NotifyEvacuated()
	{
		LevelManager instance = LevelManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.NotifyEvacuated(new EvacuationInfo(MultiSceneCore.ActiveSubSceneID, base.transform.position));
	}
}
