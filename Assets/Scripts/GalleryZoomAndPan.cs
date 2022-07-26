using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Grain.Gallery;

[RequireComponent(typeof(Image))]
public class GalleryZoomAndPan : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField]
    private float minZoom = 1.0f;

    [SerializeField]
    private float maxZoom = 10;

    [SerializeField]
    private float zoomSpeed = 10f;

    [SerializeField]
    private bool interactive = true;

    [SerializeField]
    private Gallery syncWith;

    [SerializeField]
    private RectTransform content;

    [SerializeField]
    private RawImage output;

    [Header("Debug Mode")]
    [SerializeField]
    private bool debugOn = false;

    public System.Action OnZoomIn { get; set; }
    public System.Action OnZoomOut { get; set; }
    
    public float Max { get { return maxZoom; } }
    public float Min { get { return minZoom; } }
    public float CurrentZoom { get { return currentZoom; } }
    public bool Pinching { get { return isPinching; } }
    public bool Closing { get { return isClosing; } }
    public RectTransform Content { get { return content; } }
    
    public bool DebugOn
    {
        get => debugOn;
        set => debugOn = value;
    }
    
    public bool IsInteractive 
    { 
        get { return interactive; }
        set { interactive = value; }
    }
    
    private float currentZoom = 1;
    private bool isPinching = false;
    private float startPinchDist;
    private float startPinchZoom;
    private Vector2 startPinchCenterPosition;
    private Vector2 startPinchScreenPosition;
    private float mouseWheelSensitivity = 1;
    
    private bool isClosing = false;
    private bool zoomState = false;
    private CanvasGroup cGroup;
    private AspectRatioFitter ratio;
    private Rect touchArea;

    private void Awake()
    {
        Input.multiTouchEnabled = true;
        
        if (syncWith != null)
        {
            syncWith.OnValueChanged += OnValueChanged;
            syncWith.OnClear += ResetThis;
        }

        if (output != null)
        {
            ratio = output.GetComponentInChildren<AspectRatioFitter>(true);

            if (ratio == null)
            {
                ratio = output.gameObject.AddComponent<AspectRatioFitter>();
            }
            
            ratio.enabled = false;
            ratio.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ratio.aspectRatio = 1.0f;
        }
        
        Initialise();
    }

    private void OnEnable()
    {
        if (syncWith != null)
        {
            CanvasGroup[] all = syncWith.GetComponentsInChildren<CanvasGroup>(true);
            
            if (all.Length <= 0)
            {
                cGroup = syncWith.gameObject.AddComponent<CanvasGroup>();
            }
            else
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if(all[i].gameObject.name.Equals(syncWith.name))
                    {
                        cGroup = all[i];
                        break;
                    }
                }

                if (cGroup == null)
                {
                    cGroup = syncWith.gameObject.AddComponent<CanvasGroup>();
                }
            }
            
            cGroup.alpha = 0.0f;
            cGroup.blocksRaycasts = false;
            cGroup.interactable = false;
        }
    }

    private void OnDisable()
    {
        if (syncWith != null)
        {
            if (cGroup != null)
            {
                DestroyImmediate(cGroup);
                Resources.UnloadAsset(cGroup);
            }
        }

        if (output != null)
        {
            output.texture = null;

            if (!syncWith.LoadType.Equals(FolderSource._Resource))
            {
                DestroyImmediate(output.texture);
            }

            output.CrossFadeAlpha(0.0f, 0.0f, true);
        }
        
        Initialise();
    }
    
    private void Update()
    {
        if (syncWith != null && cGroup != null)
        {
            if (syncWith.IsMoving)
            {
                if (cGroup.alpha < 1.0f)
                {
                    cGroup.alpha = 1.0f;
                }

                return;
            }
            else
            {
                if (cGroup.alpha > 0.0f)
                {
                    cGroup.alpha = 0.0f;
                    isPinching = false;
                }
            }
        }

        if (interactive && content != null && output.texture != null)
        {
            if (Input.touchCount == 2)
            {
                if (!isPinching)
                {
                    isPinching = true;

                    OnPinchStart();
                }

                if (touchArea != null)
                {
                    if (touchArea.Contains(Input.touches[0].position) && touchArea.Contains(Input.touches[1].position))
                    {
                        OnPinch();
                    }
                }
                else
                {
                    OnPinch();
                }

                return;
            }
            else
            {
                isPinching = false;
            }

            float scrollWheelInput = Input.GetAxis("Mouse ScrollWheel");
            
            if (!scrollWheelInput.Equals(0.0f))
            {
                bool scroll;

                if (touchArea != null)
                {
                    if (touchArea.Contains(Input.mousePosition))
                    {
                        scroll = true;
                    }
                    else
                    {
                        scroll = false;
                    }
                }
                else
                {
                    scroll = true;
                }

                if (scroll)
                {
                    if (Mathf.Abs(scrollWheelInput) > float.Epsilon)
                    {
                        currentZoom *= 1 + scrollWheelInput * mouseWheelSensitivity;
                        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

                        startPinchScreenPosition = (Vector2)Input.mousePosition;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(content, startPinchScreenPosition, null, out startPinchCenterPosition);
                        Vector2 pivotPosition = new Vector3(content.pivot.x * content.rect.size.x, content.pivot.y * content.rect.size.y);
                        Vector2 posFromBottomLeft = pivotPosition + startPinchCenterPosition;
                        SetPivot(content, new Vector2(posFromBottomLeft.x / content.rect.width, posFromBottomLeft.y / content.rect.height));

                        if (Mathf.Abs(content.localScale.x - currentZoom) > 0.0f)
                            content.localScale = Vector3.Lerp(content.localScale, Vector3.one * currentZoom, zoomSpeed * Time.deltaTime);
                    }
                }
            }

            if (currentZoom <= minZoom)
            {
                if (!content.localPosition.Equals(Vector3.zero))
                {
                    content.localPosition = Vector3.Lerp(content.localPosition, Vector3.zero, Time.deltaTime * 10);
                }

                if (!content.pivot.Equals(new Vector2(0.5f, 0.5f)))
                {
                    content.pivot = Vector2.Lerp(content.pivot, new Vector2(0.5f, 0.5f), Time.deltaTime * 10);
                }

                if (!content.localScale.Equals(Vector3.one))
                {
                    content.localScale = Vector3.Lerp(content.localScale, Vector3.one, zoomSpeed * Time.deltaTime);
                }
                
                if (zoomState)
                {
                    if (debugOn)
                    {
                        Debug.Log("Gallery zoom and pan zoomed out");
                    }
                    
                    zoomState = false;
                    
                    if (OnZoomOut != null)
                    {
                        OnZoomOut.Invoke();
                    }
                }
            }
            else if (content.localScale.x < 1.0f)
            {
                
            }
            else
            {
                if (!zoomState)
                {
                    if (debugOn)
                    {
                        Debug.Log("Gallery zoom and pan zoomed in");
                    }
                    
                    zoomState = true;

                    if (OnZoomIn != null)
                    {
                        OnZoomIn.Invoke();
                    }
                }
            }
        }
    }
     
     public void OnPointerDown(PointerEventData eventData)
     {
         if (!Application.isEditor)
         {
             if (!Input.touchCount.Equals(1)) return;
         }

         if (isClosing || isPinching || content.localScale.x > 1.01f)
         {
             if (syncWith != null)
             {
                 if (syncWith.OnClick != null)
                 {
                     syncWith.OnClick(syncWith.Current);
                 }
             }
             
             return;
         }

         if(syncWith != null)
         {
             syncWith.OverrideDraggabnleArea = true;
             syncWith.OnPointerDown(eventData);
         }
     }

     public void OnPointerUp(PointerEventData eventData)
     {
         if (!Application.isEditor)
         {
             if (!Input.touchCount.Equals(1)) return;
         }

         if (isClosing || isPinching || content.localScale.x > 1.01f) return;

         if (syncWith != null)
         {
             syncWith.OverrideDraggabnleArea = false;
             syncWith.OnPointerUp(eventData);
         }
     }

     public void OnDrag(PointerEventData eventData)
     {
         if(!Application.isEditor)
         {
             if (!Input.touchCount.Equals(1)) return;
         }

         if (content != null)
         {
             if (isClosing || isPinching || content.localScale.x > 1.01f)
             {
                 content.localPosition += new Vector3(eventData.delta.x, eventData.delta.y);

                 float x = Mathf.Clamp(content.localPosition.x, 0 - (content.rect.width * content.localScale.x / 2), 0 + (content.rect.width * content.localScale.y / 2));
                 float y = Mathf.Clamp(content.localPosition.y, 0 - (content.rect.height * content.localScale.x / 2), 0 + (content.rect.height * content.localScale.y / 2));

                 content.localPosition = new Vector3(x, y, 0.0f);

                 return;
             }
         }

         if (syncWith != null)
         {
             syncWith.OnDrag(eventData);
         }
     }

     public void ResetThis()
     {
         Initialise();
     }

     public void OnValueChanged(int val)
     {
         if (output != null && syncWith != null)
         {
             output.CrossFadeAlpha(0.0f, 0.0f, true);
             output.texture = null;
             
             if (!syncWith.LoadType.Equals(FolderSource._Resource))
             {
                 DestroyImmediate(output.texture);
             }

             if (gameObject.activeInHierarchy) StartCoroutine(Wait());
         }
     }

     private IEnumerator Wait()
     {
         while (syncWith.CurrentTexture == null)
         {
             yield return 0;
         }
         
         output.texture = syncWith.CurrentTexture;
         output.SetNativeSize();
             
         if(ratio != null)
         {
             if (syncWith.HasAspectRatio)
             {
                 float texWidth = output.texture.width;
                 float texHeight = output.texture.height;
                 float aspectRatio = texWidth / texHeight;
                 ratio.aspectRatio = aspectRatio;
                 ratio.enabled = true;
             }
             else
             {
                 ratio.enabled = false;
             }
         }
         
         if (syncWith.ZoomAndPanState)
         {
             output.CrossFadeAlpha(1.0f, 0.5f, true);
         }
         else
         {
             output.CrossFadeAlpha(1.0f, 0.0f, true);
         }

         syncWith.ZoomAndPanState = false;

         if (cGroup != null)
         {
             cGroup.alpha = 0.0f;
         }
     }

     private void Initialise()
     {
         currentZoom = 1.0f;

         if (content != null)
         {
             //need to get the viewport size eventually
             content.sizeDelta = new Vector2(syncWith.Bounds.Delta.x, syncWith.Bounds.Delta.y);
             
             content.localScale = Vector3.one;
             content.localPosition = Vector3.zero;
             content.pivot = new Vector2(0.5f, 0.5f);
         }

         if (output != null)
         {
             output.CrossFadeAlpha(0.0f, 0.0f, true);
             output.texture = null;
         }

        UpdateTouchArea();
    }

    public void UpdateTouchArea()
    {
        if (content != null)
        {
            RectTransform rect = content.parent.GetComponent<RectTransform>();
            touchArea = new Rect((0 + rect.position.x) - (rect.rect.width / 2), (0 + rect.position.y) - (rect.rect.height / 2), rect.rect.width, rect.rect.height);
        }
    }
     
    private void OnPinchStart()
    {
        Vector2 pos1 = Input.touches[0].position;
        Vector2 pos2 = Input.touches[1].position;

        startPinchDist = Distance(pos1, pos2) * content.localScale.x;
        startPinchZoom = currentZoom;
        startPinchScreenPosition = (pos1 + pos2) / 2;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(content, startPinchScreenPosition, null, out startPinchCenterPosition);

        Vector2 pivotPosition = new Vector3(content.pivot.x * content.rect.size.x, content.pivot.y * content.rect.size.y);
        Vector2 posFromBottomLeft = pivotPosition + startPinchCenterPosition;

        SetPivot(content, new Vector2(posFromBottomLeft.x / content.rect.width, posFromBottomLeft.y / content.rect.height));
    }

    private void OnPinch()
    {
        float currentPinchDist = Distance(Input.touches[0].position, Input.touches[1].position) * content.localScale.x;
        currentZoom = (currentPinchDist / startPinchDist) * startPinchZoom;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

        if (Mathf.Abs(content.localScale.x - currentZoom) > 0.0f)
            content.localScale = Vector3.Lerp(content.localScale, Vector3.one * currentZoom, zoomSpeed * Time.deltaTime);
    }

    private float Distance(Vector2 pos1, Vector2 pos2)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(content, pos1, null, out pos1);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(content, pos2, null, out pos2);
        return Vector2.Distance(pos1, pos2);
    }

    private void SetPivot(RectTransform rectTransform, Vector2 pivot)
    {
        if (rectTransform == null) return;

        Vector2 size = rectTransform.rect.size;
        Vector2 deltaPivot = rectTransform.pivot - pivot;
        Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y) * rectTransform.localScale.x;
        rectTransform.pivot = pivot;
        rectTransform.localPosition -= deltaPosition;
    }
}
