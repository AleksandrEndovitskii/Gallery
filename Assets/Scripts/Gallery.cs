using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Grain.Gallery;

public class Gallery : MonoBehaviour, IGallery, IPointerUpHandler, IPointerDownHandler, IDragHandler
{
	[SerializeField]
	private string id = "";

	[SerializeField]
	private bool publishOnAppend = false;

	[SerializeField]
	private GalleryIndicatorControl indicationController;

	[SerializeField]
	private List<GEntry> entries = new List<GEntry>();

	[Header("Control")]
	[SerializeField]
	[HideInInspector]
	private GControlDirection controlDirection = GControlDirection._Horizontal;

	[SerializeField]
	private bool containDragableArea = true;

	[SerializeField]
	[Range(0.1f, 1)]
	private float releaseDuration = 1.0f;

	[SerializeField]
	private float releaseThreshold = 50.0f;

	[Header("Data")]
	[SerializeField]
	private RectTransform below;
	[SerializeField]
	private RectTransform center;
	[SerializeField]
	private RectTransform above;

	[Header("Addressables")]
	[SerializeField]
	private bool clearAddressableMemory = true;

	[SerializeField]
	private int clearEveryCount = 5;

	[Header("Design")]
	[SerializeField]
	private float spacing = 0.0f;

	[SerializeField]
	private GalleryBounds gBounds;

	[Header("Debug Mode")]
	[SerializeField]
	private bool debugOn = false;

	public RectTransform BelowGalleryItem { get { return below; } set { below = value; } }

	public RectTransform CenterGalleryItem { get { return center; } set { center = value; } }

	public RectTransform AboveGalleryItem { get { return above; } set { above = value; } }

	public float Spacing { get { return spacing; } set { spacing = value; } }

	public GalleryBounds Bounds { get { return gBounds; } set { gBounds = value; } }

	public string ID { get { return id; } }

    public int Current {  get { return attributes.Index; } }
    
    public bool IsMoving { get; private set; }
    public bool OverrideDraggabnleArea { get; set; }

    public bool DebugOn
    {
	    get => debugOn;
	    set => debugOn = value;
    }
    
    public System.Action<int> OnRelease { get; set; }
    public System.Action<int> OnValueChanged { get; set; }
    
    public System.Action OnClear { get; set; }
    
    public System.Action<int> OnClick { get; set; }

    public List<GEntry> Entries { get { return entries; } }
    
    public FolderSource LoadType
    {
	    get
	    {
		    return loadType;
	    }
	    set
	    {
		    loadType = value;
		    
		    GalleryEntry[] temp = GetComponentsInChildren<GalleryEntry>(true);

		    for (int i = 0; i < temp.Length; i++)
		    {
			    temp[i].LoadType = value;
		    }
	    }
    }

    public bool HasAspectRatio
    {
	    get
	    {
		    GalleryBaseEntryElement[] temp;

		    if (center.localPosition == Vector3.zero)
		    {
			    temp = center.GetComponentsInChildren<GalleryBaseEntryElement>(true);
		    }
		    else if (above.localPosition == Vector3.zero)
		    {
			    temp = above.GetComponentsInChildren<GalleryBaseEntryElement>(true);
		    }
		    else
		    {
			    temp = below.GetComponentsInChildren<GalleryBaseEntryElement>(true);
		    }

		    for (int i = 0; i < temp.Length; i++)
		    {
			    if (temp[i].Tex() != null)
			    {
				    return temp[i].HasAspectRatio;
			    }
		    }

		    return false;
	    }
    }

    private FolderSource loadType;
	private string rawSource = "";
	private GalleryAttributes attributes = new GalleryAttributes();
	private bool released = false;
	private bool returning = false;
	private bool freeze = false;

	private float runningTime = 0.0f;
	private float percentage = 0.0f;

	private Vector3 belowOrigin = Vector3.zero;
	private Vector3 centerOrigin = Vector3.zero;
	private Vector3 aboveOrigin = Vector3.zero;

	private string belowState = "B";
	private string centerState = "C";
	private string aboveState = "A";

	private string belowCache = "B";
	private string centerCache = "C";
	private string aboveCache = "A";

	private Vector3 belowTarget = Vector3.zero;
	private Vector3 centerTarget = Vector3.zero;
	private Vector3 aboveTarget = Vector3.zero;

    private bool onReleasedCalled = false;
    private bool appendOnJump = false;

    public GameObject EntryContainer
    {
	    get { return below.parent.gameObject; }
    }
    
    public bool IsReturning { get { return returning; } }
    public float PointerPosition { get { return attributes.PointerPosition; } }
    public float CurrentPosition { get { return attributes.CurrentPosition; } }

    public bool ZoomAndPanState { get; set; }

	private GalleryDesigner m_designer;
	private int m_entryCount = 0;
    
    public virtual Texture CurrentTexture
    {
	    get
	    {
		    GalleryBaseEntryElement[] temp;

		    if (center.localPosition == Vector3.zero)
		    {
			    temp = center.GetComponentsInChildren<GalleryBaseEntryElement>(true);
		    }
		    else if (above.localPosition == Vector3.zero)
		    {
			    temp = above.GetComponentsInChildren<GalleryBaseEntryElement>(true);
		    }
		    else
		    {
			    temp = below.GetComponentsInChildren<GalleryBaseEntryElement>(true);
		    }

		    for (int i = 0; i < temp.Length; i++)
		    {
			    if (temp[i].Tex() != null)
			    {
				    return temp[i].Tex();
			    }
		    }

		    return null;
	    }
    }

    private void Awake()
	{
		m_designer = GetComponent<GalleryDesigner>();

		if (m_designer != null)
		{
			m_designer.Resize();
		}

		if (appendOnJump)
		{
			OnClick += OnClickVideo;
			OnValueChanged += OnUpdateVideo;
			return;
		}

		GalleryEntry[] temp = GetComponentsInChildren<GalleryEntry>(true);

		if (temp.Length > 0)
		{
			loadType = temp[0].LoadType;
		}

		if (below == null || center == null || above == null) 
		{
			if (debugOn)
			{
				Debug.Log ("Not all data objects exists. Turning off Gallery [" + gameObject.name + "] behaviour");
			}

			this.enabled = false;
			return;
		}

		attributes.State = "C";
		attributes.Index = 0;

		OnClick += OnClickVideo;
		OnValueChanged += OnUpdateVideo;
		
		if (indicationController != null && indicationController.GetComponent<IGallery>() == null) indicationController = null;
	}

	private void Update()
	{
		if (released) 
		{
			runningTime += Time.deltaTime;
			percentage = runningTime / releaseDuration;

			if (below != null) 
			{
				below.localPosition = new Vector3 (Ease (belowOrigin.x, belowTarget.x, percentage),
					Ease (belowOrigin.y, belowTarget.y, percentage),
				0.0f);
			}

			if (center != null) 
			{
				center.localPosition = new Vector3 (Ease (centerOrigin.x, centerTarget.x, percentage),
					Ease (centerOrigin.y, centerTarget.y, percentage),
					0.0f);
			}

			if (above != null) 
			{
				above.localPosition = new Vector3 (Ease (aboveOrigin.x, aboveTarget.x, percentage),
					Ease (belowOrigin.y, aboveTarget.y, percentage),
					0.0f);
			}

            if (!onReleasedCalled)
            {
	            if (OnRelease != null)
	            {
		            if (entries.Count > 0)
		            {
			            OnRelease.Invoke(attributes.Index);
		            }
	            }
	            
                onReleasedCalled = true;
            }

            if (percentage >= 1.0f) 
			{
				released = false;

				if (below != null) 
				{
					if (belowState.Equals ("C"))
						below.localPosition = Vector3.zero;
					else if(belowState.Equals("A"))
						below.localPosition = new Vector3 ((((int)controlDirection > 0) ? gBounds.Right : 0), (((int)controlDirection < 1) ? gBounds.Top : 0), 0);
					else
						below.localPosition = new Vector3 ((((int)controlDirection > 0) ? gBounds.Left : 0), (((int)controlDirection < 1) ? gBounds.Bottom : 0), 0);
				}

				if (center != null) 
				{
					if (centerState.Equals ("C"))
						center.localPosition = Vector3.zero;
					else if(centerState.Equals("A"))
						center.localPosition = new Vector3 ((((int)controlDirection > 0) ? gBounds.Right : 0), (((int)controlDirection < 1) ? gBounds.Top : 0), 0);
					else
						center.localPosition = new Vector3 ((((int)controlDirection > 0) ? gBounds.Left : 0), (((int)controlDirection < 1) ? gBounds.Bottom : 0), 0);
				}

				if (above != null) 
				{
					if (aboveState.Equals ("C"))
						above.localPosition = Vector3.zero;
					else if(aboveState.Equals("A"))
						above.localPosition = new Vector3 ((((int)controlDirection > 0) ? gBounds.Right : 0), (((int)controlDirection < 1) ? gBounds.Top : 0), 0);
					else
						above.localPosition = new Vector3 ((((int)controlDirection > 0) ? gBounds.Left : 0), (((int)controlDirection < 1) ? gBounds.Bottom : 0), 0);
				}

                if (entries.Count > 1 && !returning) 
				{
					attributes.Index = (attributes.State.Equals ("B")) ? attributes.Index - 1 : attributes.Index + 1;

					if (attributes.Index < 0)
						attributes.Index = entries.Count - 1;
					else if (attributes.Index > entries.Count - 1)
						attributes.Index = 0;
					
                    attributes.Previous = ((attributes.Index - 1) < 0) ? entries.Count - 1 : attributes.Index - 1;
					attributes.Next = ((attributes.Index + 1) > entries.Count - 1) ? 0 : attributes.Index + 1;

					if (debugOn)
					{
						Debug.Log("Gallery [" + gameObject.name + "]indexes are: Indes = " + attributes.Index + ", Previous = " + attributes.Previous + ", Next = " + attributes.Next);
					}

                    onReleasedCalled = false;

                    Publish(1);

                    IsMoving = false;
				}
                
                if (returning)
                {
	                if (indicationController != null)
		                indicationController.GetComponent<IGallery>().Publish(attributes.Index);
                }
                
				returning = false;
                
			}
		}
	}

	public void Resize()
    {
		GalleryEntry e;

		if (below != null)
		{
			e = below.GetComponent<GalleryEntry>();
			e.Resize();
		}

		if (center != null)
		{
			e = center.GetComponent<GalleryEntry>();
			e.Resize();
		}

		if (above != null)
		{
			e = above.GetComponent<GalleryEntry>();
			e.Resize();
		}
	}

	public void Append(GEntryGroup eGroup)
	{
		if (eGroup == null)
		{
			return;
		}
		
		Clear();

		rawSource = JsonUtility.ToJson(eGroup);

		if (eGroup != null) 
		{
			if(string.IsNullOrEmpty(eGroup.id) || !eGroup.id.Equals(ID))
			{
				return;
			}

			eGroup.entries.ForEach(e => entries.Add(e));
		}

		if (debugOn)
		{
			Debug.Log("Data appended on Gallery [" + gameObject.name + "]");
		}

		if (indicationController != null)
		{
			indicationController.Append(entries.Count);
		}

		freeze = (entries.Count > 1) ? false : true;
			
		ZoomAndPanState = true;

		if (publishOnAppend) Publish();

		Resources.UnloadUnusedAssets();
	}
	public void Append(GEntry gEntry)
	{
		throw new System.NotImplementedException();
	}

	public void Publish(int control = -1)
	{
		if (entries.Count < 1)
			return;

		if (clearEveryCount < 0)
		{
			clearAddressableMemory = false;
		}

		int n = 0;
		GalleryEntry e;

		if (below != null && !freeze) 
		{	
			e = below.GetComponent<GalleryEntry> ();

			if (control <= 0) 
			{
				n = ((attributes.Index - 1) < 0) ? entries.Count - 1 : attributes.Index - 1;

				e.Append(entries[n]);

				e.Publish ();
			} 
			else 
			{
				if (belowCache.Equals ("B") && belowState.Equals ("A"))
				{
					e.Append(entries[attributes.Next]);

					e.Publish ();
				} else if (belowCache.Equals ("A") && belowState.Equals ("B"))
				{
					e.Append(entries[attributes.Previous]);

					e.Publish ();
				} 
			}
		}

		if (center != null) 
		{	
			e = center.GetComponent<GalleryEntry>();

			if (control <= 0) 
			{
				e.Append(entries[attributes.Index]);

				e.Publish ();
			} 
			else 
			{
				if (centerCache.Equals ("B") && centerState.Equals ("A"))
				{
					e.Append(entries[attributes.Next]);

					e.Publish();
				} 
				else if (centerCache.Equals ("A") && centerState.Equals ("B"))
				{
					e.Append(entries[attributes.Previous]);

					e.Publish();
				}
			}
		}

		if (above != null && !freeze) 
		{	
			e = above.GetComponent<GalleryEntry>();

			if (control <= 0) 
			{
				n = ((attributes.Index + 1) >= entries.Count) ? 0 : attributes.Index + 1;
				e.Append(entries[n]);

				e.Publish ();
			} 
			else 
			{
				if (aboveCache.Equals ("B") && aboveState.Equals ("A"))
				{
					e.Append(entries[attributes.Next]);

					e.Publish();
				} 
				else if (aboveCache.Equals ("A") && aboveState.Equals ("B"))
				{
					e.Append(entries[attributes.Previous]);

					e.Publish();
				}
			}
		}

		if (indicationController != null)
		{
			indicationController.Publish (attributes.Index);
		}

		if (OnValueChanged != null)
		{
			OnValueChanged.Invoke(attributes.Index);
		}

		IsMoving = false;

		if (debugOn)
		{
			Debug.Log("Published Gallery [" + gameObject.name + "]");
		}

		m_entryCount++;

		if(clearAddressableMemory)
        {
			if(m_entryCount >= clearEveryCount)
            {
				if (debugOn)
				{
					Debug.Log("Clearing unused assets from Gallery [" + gameObject.name + "]");
				}

				Resources.UnloadUnusedAssets();
				m_entryCount = 0;
            }
        }
	}

	public void Clear()
	{
		rawSource = "";

		belowState = "B";
		centerState = "C";
		aboveState = "A";

		belowCache = "B";
		centerCache = "C";
		aboveCache = "A";

		attributes.Index = 0;
		
		attributes.State = "C";

		m_entryCount = 0;

		IsMoving = false;

		if (below != null) 
		{	
			below.GetComponent<GalleryEntry> ().Clear();

			below.localPosition = new Vector3 ((((int)controlDirection > 0) ? gBounds.Left : 0), (((int)controlDirection < 1) ? gBounds.Bottom : 0), 0);
		}

		if (center != null) 
		{	
			center.GetComponent<GalleryEntry> ().Clear();

			center.localPosition = Vector3.zero;
		}

		if (above != null) 
		{	
			above.GetComponent<GalleryEntry>().Clear();

			above.localPosition = new Vector3 ((((int)controlDirection > 0) ? gBounds.Right : 0), (((int)controlDirection < 1) ? gBounds.Top : 0), 0);
		}

		entries.Clear();

		if (indicationController != null)
		{
			indicationController.Clear ();
		}

		if (OnClear != null)
		{
			OnClear.Invoke();
		}

		if (debugOn)
		{
			Debug.Log("Cleared Gallery [" + gameObject.name + "]");
		}
	}
	
	public void OnPointerDown(PointerEventData eventData)
	{
		if (released || freeze)
			return;

		attributes.PointerPosition = ((int)controlDirection < 1) ? eventData.delta.x : eventData.delta.y;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (released || freeze)
			return;
		
		if(attributes.PointerPosition.Equals(attributes.CurrentPosition))
		{
			if (OnClick != null)
			{
				if (entries.Count > 0)
				{
					OnClick.Invoke(attributes.Index);
				}
			}
			
			return;
		}
		
		attributes.State = Identify();

		bool change = ((attributes.PointerPosition + Mathf.Abs(attributes.CurrentPosition)) > releaseThreshold) ? true : false;

		if (!change) 
		{
			Return();
		} 
		else 
		{
			if (attributes.State.Equals ("A")) {
				if (below != null) {

					belowCache = belowState;

					if (belowState.Equals ("A")) {
						belowTarget = Vector3.zero;
						belowState = "C";
					} else if (belowState.Equals ("C")) {
						belowTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Left : 0), (((int)controlDirection > 1) ? gBounds.Bottom : 0), 0);
						belowState = "B";
					} else {
						belowTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Left * 2 : 0), (((int)controlDirection > 1) ? gBounds.Bottom * 2 : 0), 0);
						belowState = "A";
					}
				}

				if (center != null) {

					centerCache = centerState;

					if (centerState.Equals ("A")) {
						centerTarget = Vector3.zero;
						centerState = "C";
					} else if (centerState.Equals ("C")) {
						centerTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Left : 0), (((int)controlDirection > 1) ? gBounds.Bottom : 0), 0);
						centerState = "B";
					} else {
						centerTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Left * 2 : 0), (((int)controlDirection > 1) ? gBounds.Bottom * 2 : 0), 0);
						centerState = "A";
					}
				}

				if (above != null) {

					aboveCache = aboveState;

					if (aboveState.Equals ("A")) {
						aboveTarget = Vector3.zero;
						aboveState = "C";
					} else if (aboveState.Equals ("C")) {
						aboveTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Left : 0), (((int)controlDirection > 1) ? gBounds.Bottom : 0), 0);
						aboveState = "B";
					} else {
						aboveTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Left * 2 : 0), (((int)controlDirection > 1) ? gBounds.Bottom * 2 : 0), 0);
						aboveState = "A";
					}
				}
			} 
			else 
			{
				if (below != null) {
					
					belowCache = belowState;

					if (belowState.Equals ("B")) {
						belowTarget = Vector3.zero;
						belowState = "C";
					} else if (belowState.Equals ("C")) {
						belowTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Right : 0), (((int)controlDirection > 1) ? gBounds.Top : 0), 0);
						belowState = "A";
					} else {
						belowTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Right * 2 : 0), (((int)controlDirection > 1) ? gBounds.Top * 2 : 0), 0);
						belowState = "B";
					}
				}

				if (center != null) {

					centerCache = centerState;

					if (centerState.Equals ("B")) {
						centerTarget = Vector3.zero;
						centerState = "C";
					} else if (centerState.Equals ("C")) {
						centerTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Right : 0), (((int)controlDirection > 1) ? gBounds.Top : 0), 0);
						centerState = "A";
					} else {
						centerTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Right * 2 : 0), (((int)controlDirection > 1) ? gBounds.Top * 2 : 0), 0);
						centerState = "B";
					}
				}

				if (above != null) {

					aboveCache = aboveState;

					if (aboveState.Equals ("B")) {
						aboveTarget = Vector3.zero;
						aboveState = "C";
					} else if (aboveState.Equals ("C")) {
						aboveTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Right : 0), (((int)controlDirection > 1) ? gBounds.Top : 0), 0);
						aboveState = "A";
					} else {
						aboveTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Right * 2 : 0), (((int)controlDirection > 1) ? gBounds.Top * 2 : 0), 0);
						aboveState = "B";
					}
				}

			}
		}

		runningTime = 0.0f;
		percentage = 0.0f;

		attributes.CurrentPosition = 0.0f;

		SetOrigin();

		IsMoving = true;

		released = true;
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (released || freeze)
			return;

		if (!OverrideDraggabnleArea)
		{
			if (containDragableArea && !eventData.hovered.Contains (gameObject)) 
			{
				runningTime = 0.0f;
				percentage = 0.0f;

				attributes.CurrentPosition = 0.0f;

				Return();
				SetOrigin();

				released = true;
				return;
			}
		}

		attributes.CurrentPosition += ((int)controlDirection > 0) ? eventData.delta.x : eventData.delta.y;

		if (below != null) 
		{
			if (belowState.Equals ("B"))
				below.localPosition = new Vector3 ((((int)controlDirection > 0) ? attributes.CurrentPosition - (below.sizeDelta.x + spacing) : 0), (((int)controlDirection < 1) ? attributes.CurrentPosition - (below.sizeDelta.y + spacing) : 0), 0);
			else if(belowState.Equals("C"))
				below.localPosition = new Vector3 (attributes.CurrentPosition, 0, 0);
			else
				below.localPosition = new Vector3 ((((int)controlDirection > 0) ? attributes.CurrentPosition + (below.sizeDelta.x + spacing) : 0), (((int)controlDirection < 1) ? attributes.CurrentPosition + (below.sizeDelta.y + spacing) : 0), 0);
		}

		if (center != null) 
		{
			if(centerState.Equals("C"))
				center.localPosition = new Vector3 (attributes.CurrentPosition, 0, 0);
			else if(centerState.Equals("B"))
				center.localPosition = new Vector3 ((((int)controlDirection > 0) ? attributes.CurrentPosition - (center.sizeDelta.x + spacing) : 0), (((int)controlDirection < 1) ? attributes.CurrentPosition - (center.sizeDelta.y + spacing) : 0), 0);
			else
				center.localPosition = new Vector3 ((((int)controlDirection > 0) ? attributes.CurrentPosition + (center.sizeDelta.x + spacing) : 0), (((int)controlDirection < 1) ? attributes.CurrentPosition + (center.sizeDelta.y + spacing) : 0), 0);
		}

		if (above != null) 
		{
			if(aboveState.Equals("A"))
				above.localPosition = new Vector3 ((((int)controlDirection > 0) ? attributes.CurrentPosition + (above.sizeDelta.x + spacing) : 0), (((int)controlDirection < 1) ? attributes.CurrentPosition + (above.sizeDelta.y + spacing) : 0), 0);
			else if(aboveState.Equals("C"))
				above.localPosition = new Vector3 (attributes.CurrentPosition, 0, 0);
			else
				above.localPosition = new Vector3 ((((int)controlDirection > 0) ? attributes.CurrentPosition - (above.sizeDelta.x + spacing) : 0), (((int)controlDirection < 1) ? attributes.CurrentPosition - (above.sizeDelta.y + spacing) : 0), 0);
		}

		IsMoving = true;
	}
	
	public void Next()
	{
		attributes.CurrentPosition = 10 + releaseThreshold;

		OnPointerUp(null);

		IsMoving = true;
	}

	public void Previous()
	{
		attributes.CurrentPosition = -(10 + releaseThreshold);

		OnPointerUp(null);

		IsMoving = true;
	}

	public void Jump(int val)
	{
		released = false;
		appendOnJump = true;

		belowState = "B";
		centerState = "C";
		aboveState = "A";

		belowCache = "B";
		centerCache = "C";
		aboveCache = "A";

		attributes.State = "C";
		attributes.CurrentPosition = 0.0f;

		attributes.Index = val;
		attributes.Previous = ((attributes.Index - 1) < 0) ? entries.Count - 1 : attributes.Index - 1;
		attributes.Next = ((attributes.Index + 1) > entries.Count - 1) ? 0 : attributes.Index + 1;


		if (below != null)
		{
			below.GetComponent<GalleryEntry>().Clear();

			below.localPosition = new Vector3((((int) controlDirection > 0) ? gBounds.Left : 0),
				(((int) controlDirection < 1) ? gBounds.Bottom : 0), 0);
		}

		if (center != null)
		{
			center.GetComponent<GalleryEntry>().Clear();

			center.localPosition = Vector3.zero;
		}

		if (above != null)
		{
			above.GetComponent<GalleryEntry>().Clear();

			above.localPosition = new Vector3((((int) controlDirection > 0) ? gBounds.Right : 0),
				(((int) controlDirection < 1) ? gBounds.Top : 0), 0);
		}

		if(gameObject.activeInHierarchy) Publish(0);
	}
	
	private string Identify()
	{
		return (attributes.CurrentPosition < 0) ? "A" : "B";
	}

	private float Ease(float start, float end, float val)
	{
		end -= start;
		return -end * val * (val - 2) + start;
	}

	private void Return()
	{
		if (below != null) 
		{
			if (belowState.Equals ("C"))
				belowTarget = Vector3.zero;
			else if (belowState.Equals ("B"))
				belowTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Left : 0), (((int)controlDirection > 1) ? gBounds.Bottom : 0), 0);
			else 
				belowTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Right : 0), (((int)controlDirection > 1) ? gBounds.Top : 0), 0);
		}

		if (center != null)
		{
			if (centerState.Equals ("C"))
				centerTarget = Vector3.zero;
			else if (centerState.Equals ("B"))
				centerTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Left : 0), (((int)controlDirection > 1) ? gBounds.Bottom : 0), 0);
			else 
				centerTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Right : 0), (((int)controlDirection > 1) ? gBounds.Top : 0), 0);
		}

		if (above != null)
		{
			if (aboveState.Equals ("C"))
				aboveTarget = Vector3.zero;
			else if (aboveState.Equals ("B"))
				aboveTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Left : 0), (((int)controlDirection > 1) ? gBounds.Bottom : 0), 0);
			else 
				aboveTarget = new Vector3 ((((int)controlDirection > 0) ? gBounds.Right : 0), (((int)controlDirection > 1) ? gBounds.Top : 0), 0);
		}

		returning = true;
	}

	private void SetOrigin()
	{
		if (below != null)
			belowOrigin = new Vector3 (below.localPosition.x, below.localPosition.y, below.localPosition.z);

		if (center != null)
			centerOrigin = new Vector3 (center.localPosition.x, center.localPosition.y, center.localPosition.z);

		if (above != null)
			aboveOrigin = new Vector3 (above.localPosition.x, above.localPosition.y, above.localPosition.z);
	}
	
	private void OnClickVideo(int n)
	{
		GalleryEntry[] all = GetComponentsInChildren<GalleryEntry>();

		for(int i = 0; i < all.Length; i++)
		{
			if (!all[i].gameObject.activeInHierarchy) continue;
			else if (all[i].transform.localPosition.x > 1 || all[i].transform.localPosition.x < -1)
			{

			}
			else
			{
				GalleryEntryVideoElement[] vid = all[i].gameObject.GetComponentsInChildren<GalleryEntryVideoElement>();

				if (vid.Length > 0)
				{
					if (vid[0] != null)
					{
						if (vid[0].VideoExists)
						{
							vid[0].OnClick();
						}
					}
				}
			}
		}
	}

	private void OnUpdateVideo(int n)
	{
		GalleryEntry[] all = GetComponentsInChildren<GalleryEntry>();
		
		for (int i = 0; i < all.Length; i++)
		{
			if (!all[i].gameObject.activeInHierarchy) continue;
			else
			{
				GalleryEntryVideoElement[] vid = all[i].gameObject.GetComponentsInChildren<GalleryEntryVideoElement>();
				
				if (vid.Length > 0)
				{
					if (vid[0] != null)
					{
						if (vid[0].VideoExists)
						{
							if(all[i].ID.Equals(entries[n].id))
							{
								vid[0].Refresh();
							}
							else
							{
								vid[0].Refresh(-1);
							}
						}
					}
				}
			}
		}
	}
}
