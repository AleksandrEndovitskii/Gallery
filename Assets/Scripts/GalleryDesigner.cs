using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Grain.Gallery;

[RequireComponent(typeof(Gallery))]
public class GalleryDesigner : MonoBehaviour 
{
	[Header("Entries")]
	[SerializeField]
	private Transform entryPrefab;

	[SerializeField]
	private Transform entryContainer;

	[SerializeField]
	private float spacing = 0.0f;

	[SerializeField]
	private Vector2 size = new Vector2(2732, 2048);

	[SerializeField]
	private bool matchViewportSize = false;

	[HideInInspector]
	[SerializeField]
	private Gallery gallery;

	public void Awake()
	{
#if UNITY_EDITOR

#else
		Destroy(this);
#endif
	}

	public void Resize()
	{
		if (matchViewportSize && entryContainer != null && gallery != null)
		{
			Rect sDelta = entryContainer.parent.GetComponent<RectTransform>().rect;
			Vector2 vSize = new Vector2(sDelta.width, sDelta.height);

			entryContainer.GetComponent<RectTransform>().sizeDelta = vSize;

			GameObject go = null;

			for (int i = 0; i < 3; i++)
			{
				switch (i)
				{
					case 0:
						go = gallery.BelowGalleryItem.gameObject;

						go.transform.localPosition = new Vector3(-(vSize.x + spacing), 0, 0);
						go.GetComponent<RectTransform>().sizeDelta = vSize;
						go.name = "GEntry_Left";
						gallery.BelowGalleryItem = go.GetComponent<RectTransform>();
						break;
					case 1:
						go = gallery.CenterGalleryItem.gameObject;

						go.transform.localPosition = new Vector3(0, 0, 0);
						go.GetComponent<RectTransform>().sizeDelta = vSize;
						go.name = "GEntry_Center";
						gallery.CenterGalleryItem = go.GetComponent<RectTransform>();
						break;
					case 2:
						go = gallery.AboveGalleryItem.gameObject;

						go.transform.localPosition = new Vector3(vSize.x + spacing, 0, 0);
						go.GetComponent<RectTransform>().sizeDelta = vSize;
						go.name = "GEntry_Right";
						gallery.AboveGalleryItem = go.GetComponent<RectTransform>();
						break;
					default:
						break;
				}
			}

			gallery.Spacing = spacing;
			gallery.Bounds = new GalleryBounds(vSize.y + spacing, -(vSize.y + spacing), -(vSize.x + spacing), vSize.x + spacing, vSize);
			gallery.Resize();
		}
	}

    public void Append()
	{
		Delete ();

		if (entryPrefab == null || entryContainer == null)
			return;

		for (int i = 0; i < 3; i++) 
		{
			GameObject go = Instantiate (entryPrefab.gameObject, Vector3.zero, Quaternion.identity) as GameObject;

			go.transform.SetParent(entryContainer);

			switch (i)
			{
			case 0:

				go.transform.localPosition = new Vector3 (-(size.x + spacing), 0, 0);
				go.name = "GEntry_Left";
				gallery.BelowGalleryItem = go.GetComponent<RectTransform>();
				break;
			case 1:
				go.transform.localPosition = new Vector3 (0, 0, 0);
				go.name = "GEntry_Center";
				gallery.CenterGalleryItem = go.GetComponent<RectTransform>();
				break;
			case 2:
				go.transform.localPosition = new Vector3 (size.x + spacing, 0, 0);
				go.name = "GEntry_Right";
				gallery.AboveGalleryItem = go.GetComponent<RectTransform>();
				break;
			default:
				break;
			}

			go.transform.localScale = Vector3.one;
			go.SetActive (true);
		}

		gallery.Spacing = spacing;
		gallery.Bounds = new GalleryBounds(size.y + spacing, -(size.y + spacing), -(size.x + spacing), size.x + spacing, size);
	}

	public void Delete()
	{
		Set ();

		gallery.BelowGalleryItem = null;
		gallery.AboveGalleryItem = null;
		gallery.CenterGalleryItem = null;

		List<Transform> deletion = new List<Transform> ();

		foreach (Transform t in entryContainer) 
		{
			if (t != entryPrefab)
			{
				deletion.Add(t);
			}
		}

		deletion.ForEach (t => DestroyImmediate (t.gameObject));

		deletion = null;
	}

	private void Set()
	{
		if (gallery == null)
			gallery = GetComponent<Gallery> ();
	}

	[System.Serializable]
	private enum Positioning { _Bottom, _Top, _Left, _Right }
}
