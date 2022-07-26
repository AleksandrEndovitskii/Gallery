using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class GalleryIndicator : MonoBehaviour, IPointerClickHandler
{
	[SerializeField]
	private UnityEvent onTrue = new UnityEvent ();

	[SerializeField]
	private UnityEvent onFalse = new UnityEvent ();

	public void Set(bool state)
	{
		if (state)
			onTrue.Invoke ();
		else
			onFalse.Invoke ();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		OnClick();
	}

	public void OnClick()
	{
		Gallery g = GetComponentInParent<Gallery>();

		if (g != null)
		{
			g.Jump(transform.GetSiblingIndex() - 1);
		}
	}
}
