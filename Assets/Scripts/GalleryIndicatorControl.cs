using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Grain.Gallery;

public class GalleryIndicatorControl : MonoBehaviour, IGallery
{
	[SerializeField]
	private GameObject indicatorPrefab;

	[SerializeField]
	private GalleryZoomAndPan zoomAndPan;

	public string ID { get { return gameObject.name; } }

	private List<GalleryIndicator> indicators = new List<GalleryIndicator> ();

	private CanvasGroup cGroup;
	
	private void Awake()
	{
		if (zoomAndPan != null)
		{
			zoomAndPan.OnZoomIn += OnZoomIn;
			zoomAndPan.OnZoomOut += OnZoomOut;
		}

		cGroup = GetComponent<CanvasGroup>();

		if (cGroup == null)
		{
			cGroup = gameObject.AddComponent<CanvasGroup>();
		}
	}

	private void OnDisable()
	{
		Clear();
	}

	public void Append(string data)
	{
		if (string.IsNullOrEmpty(data))
			return;

		if (indicatorPrefab == null || indicatorPrefab.GetComponent<GalleryIndicator> () == null)
			return;

        int n;

		if (!int.TryParse(data, out n))
			return;

		if (n > 1) 
		{
			for (int i = 0; i < n; i++) 
			{
				GameObject go = Instantiate (indicatorPrefab, Vector3.zero, Quaternion.identity, transform) as GameObject;
				go.transform.localScale = Vector3.one;
				go.transform.localPosition = Vector3.zero;
				go.SetActive (true);

				indicators.Add (go.GetComponent<GalleryIndicator> ());
			}
		}
	}

	public void Publish(int control = -1)
	{
		if (indicators.Count < 1)
			return;

		for (int i = 0; i < indicators.Count; i++) 
		{
			if (i.Equals (control))
				indicators [i].Set (true);
			else
				indicators [i].Set (false);
		}
	}

	public void Clear()
	{
		indicators.ForEach (i => Destroy (i.gameObject));

		indicators.Clear ();
	}

	public void Resize()
    {

    }

	public virtual void OnZoomIn()
	{
		if (cGroup != null)
		{
			cGroup.alpha = 0.0f;
			cGroup.interactable = false;
			cGroup.blocksRaycasts = false;
		}
	}
	
	public virtual void OnZoomOut()
	{
		if (cGroup != null)
		{
			cGroup.alpha = 1.0f;
			cGroup.interactable = true;
			cGroup.blocksRaycasts = true;
		}
	}
}
