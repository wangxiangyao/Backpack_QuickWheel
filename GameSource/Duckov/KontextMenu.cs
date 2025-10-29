using System;
using System.Collections.Generic;
using Duckov.UI.Animations;
using Duckov.Utilities;
using UnityEngine;

namespace Duckov.UI
{
	// Token: 0x020003C7 RID: 967
	public class KontextMenu : MonoBehaviour
	{
		// Token: 0x170006AD RID: 1709
		// (get) Token: 0x06002342 RID: 9026 RVA: 0x0007B75C File Offset: 0x0007995C
		private Transform ContentRoot
		{
			get
			{
				return base.transform;
			}
		}

		// Token: 0x170006AE RID: 1710
		// (get) Token: 0x06002343 RID: 9027 RVA: 0x0007B764 File Offset: 0x00079964
		private PrefabPool<KontextMenuEntry> EntryPool
		{
			get
			{
				if (this._entryPool == null)
				{
					this._entryPool = new PrefabPool<KontextMenuEntry>(this.entryPrefab, this.ContentRoot, null, null, null, true, 10, 10000, null);
				}
				return this._entryPool;
			}
		}

		// Token: 0x06002344 RID: 9028 RVA: 0x0007B7A2 File Offset: 0x000799A2
		private void Awake()
		{
			if (KontextMenu.instance == null)
			{
				KontextMenu.instance = this;
			}
			this.rectTransform = (base.transform as RectTransform);
		}

		// Token: 0x06002345 RID: 9029 RVA: 0x0007B7C8 File Offset: 0x000799C8
		private void OnDestroy()
		{
		}

		// Token: 0x06002346 RID: 9030 RVA: 0x0007B7CC File Offset: 0x000799CC
		private void Update()
		{
			if (this.watchRectTransform)
			{
				if ((this.cachedTransformPosition - this.watchRectTransform.position).magnitude > this.positionMoveCloseThreshold)
				{
					KontextMenu.Hide(null);
					return;
				}
			}
			else if (this.isWatchingRectTransform)
			{
				KontextMenu.Hide(null);
			}
		}

		// Token: 0x06002347 RID: 9031 RVA: 0x0007B824 File Offset: 0x00079A24
		public void InstanceShow(object target, RectTransform targetRectTransform, params KontextMenuDataEntry[] entries)
		{
			this.target = target;
			this.watchRectTransform = targetRectTransform;
			this.isWatchingRectTransform = true;
			this.cachedTransformPosition = this.watchRectTransform.position;
			Vector3[] array = new Vector3[4];
			targetRectTransform.GetWorldCorners(array);
			float num = Mathf.Min(new float[]
			{
				array[0].x,
				array[1].x,
				array[2].x,
				array[3].x
			});
			float num2 = Mathf.Max(new float[]
			{
				array[0].x,
				array[1].x,
				array[2].x,
				array[3].x
			});
			float num3 = Mathf.Min(new float[]
			{
				array[0].y,
				array[1].y,
				array[2].y,
				array[3].y
			});
			float num4 = Mathf.Max(new float[]
			{
				array[0].y,
				array[1].y,
				array[2].y,
				array[3].y
			});
			float num5 = num;
			float num6 = (float)Screen.width - num2;
			float num7 = num3;
			float num8 = (float)Screen.height - num4;
			float x = (num5 > num6) ? num : num2;
			float y = (num7 > num8) ? num3 : num4;
			Vector2 vector = new Vector2(x, y);
			if (entries.Length < 1)
			{
				this.InstanceHide();
				return;
			}
			Vector2 vector2 = new Vector2(vector.x / (float)Screen.width, vector.y / (float)Screen.height);
			float x2 = (float)((vector2.x < 0.5f) ? 0 : 1);
			float y2 = (float)((vector2.y < 0.5f) ? 0 : 1);
			this.rectTransform.pivot = new Vector2(x2, y2);
			base.gameObject.SetActive(true);
			this.fadeGroup.SkipHide();
			this.Setup(entries);
			this.fadeGroup.Show();
			base.transform.position = vector;
		}

		// Token: 0x06002348 RID: 9032 RVA: 0x0007BA68 File Offset: 0x00079C68
		public void InstanceShow(object target, Vector2 screenPoint, params KontextMenuDataEntry[] entries)
		{
			this.target = target;
			this.watchRectTransform = null;
			this.isWatchingRectTransform = false;
			if (entries.Length < 1)
			{
				this.InstanceHide();
				return;
			}
			Vector2 vector = new Vector2(screenPoint.x / (float)Screen.width, screenPoint.y / (float)Screen.height);
			float x = (float)((vector.x < 0.5f) ? 0 : 1);
			float y = (float)((vector.y < 0.5f) ? 0 : 1);
			this.rectTransform.pivot = new Vector2(x, y);
			base.gameObject.SetActive(true);
			this.fadeGroup.SkipHide();
			this.Setup(entries);
			this.fadeGroup.Show();
			base.transform.position = screenPoint;
		}

		// Token: 0x06002349 RID: 9033 RVA: 0x0007BB28 File Offset: 0x00079D28
		private void Clear()
		{
			this.EntryPool.ReleaseAll();
			List<GameObject> list = new List<GameObject>();
			for (int i = 0; i < this.ContentRoot.childCount; i++)
			{
				Transform child = this.ContentRoot.GetChild(i);
				if (child.gameObject.activeSelf)
				{
					list.Add(child.gameObject);
				}
			}
			foreach (GameObject obj in list)
			{
				UnityEngine.Object.Destroy(obj);
			}
		}

		// Token: 0x0600234A RID: 9034 RVA: 0x0007BBC0 File Offset: 0x00079DC0
		private void Setup(IEnumerable<KontextMenuDataEntry> entries)
		{
			this.Clear();
			int num = 0;
			foreach (KontextMenuDataEntry kontextMenuDataEntry in entries)
			{
				if (kontextMenuDataEntry != null)
				{
					KontextMenuEntry kontextMenuEntry = this.EntryPool.Get(this.ContentRoot);
					num++;
					kontextMenuEntry.Setup(this, num, kontextMenuDataEntry);
					kontextMenuEntry.transform.SetAsLastSibling();
				}
			}
		}

		// Token: 0x0600234B RID: 9035 RVA: 0x0007BC34 File Offset: 0x00079E34
		public void InstanceHide()
		{
			this.target = null;
			this.watchRectTransform = null;
			this.fadeGroup.Hide();
		}

		// Token: 0x0600234C RID: 9036 RVA: 0x0007BC4F File Offset: 0x00079E4F
		public static void Show(object target, RectTransform watchRectTransform, params KontextMenuDataEntry[] entries)
		{
			if (KontextMenu.instance == null)
			{
				return;
			}
			KontextMenu.instance.InstanceShow(target, watchRectTransform, entries);
		}

		// Token: 0x0600234D RID: 9037 RVA: 0x0007BC6C File Offset: 0x00079E6C
		public static void Show(object target, Vector2 position, params KontextMenuDataEntry[] entries)
		{
			if (KontextMenu.instance == null)
			{
				return;
			}
			KontextMenu.instance.InstanceShow(target, position, entries);
		}

		// Token: 0x0600234E RID: 9038 RVA: 0x0007BC89 File Offset: 0x00079E89
		public static void Hide(object target)
		{
			if (KontextMenu.instance == null)
			{
				return;
			}
			if (target != null && target != KontextMenu.instance.target)
			{
				return;
			}
			if (KontextMenu.instance.fadeGroup.IsHidingInProgress)
			{
				return;
			}
			KontextMenu.instance.InstanceHide();
		}

		// Token: 0x040017F5 RID: 6133
		private static KontextMenu instance;

		// Token: 0x040017F6 RID: 6134
		private RectTransform rectTransform;

		// Token: 0x040017F7 RID: 6135
		[SerializeField]
		private KontextMenuEntry entryPrefab;

		// Token: 0x040017F8 RID: 6136
		[SerializeField]
		private FadeGroup fadeGroup;

		// Token: 0x040017F9 RID: 6137
		[SerializeField]
		private float positionMoveCloseThreshold = 10f;

		// Token: 0x040017FA RID: 6138
		private object target;

		// Token: 0x040017FB RID: 6139
		private bool isWatchingRectTransform;

		// Token: 0x040017FC RID: 6140
		private RectTransform watchRectTransform;

		// Token: 0x040017FD RID: 6141
		private Vector3 cachedTransformPosition;

		// Token: 0x040017FE RID: 6142
		private PrefabPool<KontextMenuEntry> _entryPool;
	}
}
