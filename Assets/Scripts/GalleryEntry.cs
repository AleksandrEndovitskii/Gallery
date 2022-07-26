using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Grain.Gallery;
using UnityEngine.ResourceManagement.ResourceLocations;

public class GalleryEntry : MonoBehaviour, IGallery
{
	[SerializeField]
	private GEntry entry = new GEntry();

	[SerializeField]
	private bool enableCulling = false;

	public string ID { get { return entry.id; } }
	
	public FolderSource LoadType
	{
		get
		{
			GalleryBaseEntryElement[] gElements = GetComponentsInChildren<GalleryBaseEntryElement>(true);

			if(gElements.Length > 0)
			{
				return gElements[0].LoadType;
			}

			return FolderSource._Resource;
		}
		set
		{
			GalleryBaseEntryElement[] gElements = GetComponentsInChildren<GalleryBaseEntryElement>(true);

			for (int i = 0; i < gElements.Length; i++)
			{
				gElements[i].LoadType = value;
			}
		}
	}

	private bool publish = false;
	private string cacheID = "";

	private bool m_hasLoaded = false;
	private RectTransform m_rectT;

    private void Awake()
    {
		m_rectT = GetComponent<RectTransform>();
    }

    private void OnDisable()
    {
		m_hasLoaded = false;
	}

    private void Update()
    {
		if (!enableCulling || !publish) return;

		if(m_rectT)
        {
			if (IsRectTransformCulled(m_rectT))
			{
				if (!m_hasLoaded)
				{
					m_hasLoaded = true;

					List<IGalleryElement> gElements = GalleryUtils.GetInterfaces<IGalleryElement>(gameObject);

					for (int i = 0; i < gElements.Count; i++)
					{
						gElements[i].Recieve(entry.elements.FirstOrDefault(e => e.id.Equals(gElements[i].Reference)).data);
					}
				}
			}
			else
			{
				if (m_hasLoaded)
				{
					m_hasLoaded = false;

					List<IGalleryElement> gElements = GalleryUtils.GetInterfaces<IGalleryElement>(gameObject);

					for (int i = 0; i < gElements.Count; i++)
					{
						gElements[i].Clear();
					}
				}
			}
		}
    }

    public void Append(GEntry entry)
    {
	    if (entry == null)
		    return;

	    this.entry = entry;

	    if(!enableCulling)
	    {
		    publish = (cacheID.Equals(ID)) ? false : true;
	    }
	    else
	    {
		    publish = true;
	    }

	    cacheID = ID;
    }
    public void Append(GEntryGroup eGroup)
    {
	    throw new System.NotImplementedException();
    }

    public void Publish(int control = -1)
	{
		if (!publish)
			return;

		if(!enableCulling)
		{
			List<IGalleryElement> gElements = GalleryUtils.GetInterfaces<IGalleryElement>(gameObject);

			for (int i = 0; i < gElements.Count; i++)
			{
				if (entry.ResourceLocation == null)
				{
					var firstOrDefault = entry.elements.FirstOrDefault(e => e.id.Equals(gElements[i].Reference));
					if (firstOrDefault == null)
					{
						continue;
					}
					gElements[i].Recieve(firstOrDefault.data);
				}
				else
				{
					gElements[i].Recieve(entry.ResourceLocation);
				}
			}
		}
	}

	public void Clear()
	{
		cacheID = "";
		entry.id = "";
		m_hasLoaded = false;

		List<IGalleryElement> gElements = GalleryUtils.GetInterfaces<IGalleryElement>(gameObject);

        for (int i = 0; i < gElements.Count; i++)
        {
            gElements[i].Clear();
        }

        entry.elements.Clear();
		publish = false;
	}

	public void Resize()
	{
		List<IGalleryElement> gElements = GalleryUtils.GetInterfaces<IGalleryElement>(gameObject);

		for (int i = 0; i < gElements.Count; i++)
		{
			gElements[i].Resize();
		}
	}

	private bool IsRectTransformCulled(RectTransform elem)
	{
		Vector3[] v = new Vector3[4];
		elem.GetWorldCorners(v);

		float maxY = Mathf.Max(v[0].y, v[1].y, v[2].y, v[3].y);
		float minY = Mathf.Min(v[0].y, v[1].y, v[2].y, v[3].y);

		float maxX = Mathf.Max(v[0].x, v[1].x, v[2].x, v[3].x);
		float minX = Mathf.Min(v[0].x, v[1].x, v[2].x, v[3].x);

		if (maxY < 0 || minY > Screen.height || maxX < 0 || minX > Screen.width)
		{
			return false;
		}

		return true;
	}
}
