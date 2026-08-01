using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using Pixelplacement;

/// <summary>
/// UI cursor visual + world Physics2D picking for Interactables.
/// </summary>
public class GameCursor : ManagedBehaviour
{
    public static GameCursor instance;

    public Interactable CurrentInteractable { get; private set; }
    private Interactable cursorDownInteractable;

    public ReferenceSetToggle DisableMovement = new ReferenceSetToggle();
    public ReferenceSetToggle DisableInput = new ReferenceSetToggle();

    [Header("UI Cursor")]
    [Tooltip("UI cursor RectTransform (follows mouse on screen)")]
    [SerializeField]
    private RectTransform cursorRect;

    [Tooltip("Scaled on click; defaults to cursorRect")]
    [SerializeField]
    private Transform cursorScaleTarget;

    [SerializeField]
    private float defaultCursorScale = 1f;

    [SerializeField]
    private float pressedCursorScale = 0.85f;

    [Header("Legacy (optional)")]
    [FormerlySerializedAs("cursorRenderer")]
    [SerializeField]
    private SpriteRenderer legacyCursorRenderer;

    [Tooltip("When true, ignore world Interactables while pointer is over UI")]
    [SerializeField]
    private bool blockWorldPickOverUI = false;

    private List<string> excludedLayers = new();
    private Canvas cursorCanvas;

    protected override void ManagedInitialize()
    {
        instance = this;

        if (cursorScaleTarget == null && cursorRect != null)
            cursorScaleTarget = cursorRect;

        if (cursorRect != null)
            cursorCanvas = cursorRect.GetComponentInParent<Canvas>();

        if (legacyCursorRenderer != null)
            legacyCursorRenderer.enabled = cursorRect == null;
    }

    public override void ManagedUpdate()
    {
        UpdateMainInput();
        UpdateDragInput();
        UpdateVisuals();
    }

    private void UpdateMainInput()
    {
        Vector3 worldPos = GetWorldPointerPosition();
        bool overUI = blockWorldPickOverUI && IsPointerOverUI();

        CurrentInteractable = overUI
            ? ClearCurrentInteractable(CurrentInteractable)
            : UpdateCurrentInteractable(CurrentInteractable, excludedLayers.ToArray(), worldPos);

        if (!DisableInput.True)
        {
            if (CurrentInteractable != null)
            {
                CurrentInteractable.CursorStay();
            }

            if (CurrentInteractable != null)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    CurrentInteractable.CursorSelectStart();
                    cursorDownInteractable = CurrentInteractable;
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    CurrentInteractable.CursorSelectEnd();
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    CurrentInteractable.CursorAltSelectStart();
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    CurrentInteractable.CursorAltSelectEnd();
                }
                else if (Input.mouseScrollDelta.magnitude != 0f)
                {
                    CurrentInteractable.CursorScroll(Input.mouseScrollDelta.magnitude);
                }
            }
        }
    }

    private void UpdateDragInput()
    {
        if (cursorDownInteractable != null && !DisableInput.True)
        {
            if (CurrentInteractable != cursorDownInteractable)
            {
                cursorDownInteractable.CursorDragOff();
                cursorDownInteractable = null;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            cursorDownInteractable = null;
        }
    }

    private Interactable ClearCurrentInteractable(Interactable current)
    {
        if (current != null && current.CollisionEnabled)
            current.CursorExit();
        return null;
    }

    private Interactable UpdateCurrentInteractable(Interactable current, string[] excludeLayers, Vector3 worldPos)
    {
        var hitInteractable = RaycastForInteractable(~LayerMask.GetMask(excludeLayers), worldPos);

        if (hitInteractable != current)
        {
            if (current != null)
            {
                if (current.CollisionEnabled)
                {
                    current.CursorExit();
                }
            }

            if (hitInteractable != null && !DisableInput.True)
            {
                hitInteractable.CursorEnter();
            }
            else
            {
                return null;
            }
        }

        return hitInteractable;
    }

    private void UpdateVisuals()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        if (cursorRect != null)
        {
            FollowMouseOnCanvas(cursorRect);
        }
        else if (legacyCursorRenderer != null)
        {
            Vector3 cursorPos = GetWorldPointerPosition();
            transform.position = new Vector3(cursorPos.x, cursorPos.y, cursorPos.z);
        }

        Transform scaleTarget = cursorScaleTarget != null
            ? cursorScaleTarget
            : (cursorRect != null ? cursorRect : (legacyCursorRenderer != null ? legacyCursorRenderer.transform : null));

        if (scaleTarget == null)
            return;

        if (Input.GetMouseButtonUp(0))
        {
            Tween.LocalScale(scaleTarget, Vector2.one * defaultCursorScale, 0.05f, 0f, Tween.EaseInOut);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Tween.LocalScale(scaleTarget, Vector2.one * pressedCursorScale, 0.05f, 0f, Tween.EaseInOut);
            if (AudioController.Instance != null)
                AudioController.Instance.PlaySound2D("grassClick", 0.8f);
        }
    }

    private void FollowMouseOnCanvas(RectTransform rect)
    {
        RectTransform parent = rect.parent as RectTransform;
        if (parent == null)
            return;

        Camera eventCam = null;
        if (cursorCanvas != null && cursorCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCam = cursorCanvas.worldCamera != null ? cursorCanvas.worldCamera : Camera.main;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, Input.mousePosition, eventCam, out Vector2 local))
        {
            rect.anchoredPosition = local;
        }
    }

    private Vector3 GetWorldPointerPosition()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return transform.position;

        Vector3 p = cam.ScreenToWorldPoint(Input.mousePosition);
        return new Vector3(p.x, p.y, 0f);
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private Interactable RaycastForInteractable(int layerMask, Vector3 cursorPosition)
    {
        Interactable hitInteractable = null;

        var rayHits = Physics2D.RaycastAll(cursorPosition, Vector2.zero, 1000f, layerMask);
        var hitInteractables = GetInteractablesFromRayHits(rayHits);

        if (hitInteractables.Count > 0)
        {
            hitInteractables.Sort((Interactable a, Interactable b) =>
            {
                return a.CompareInteractionSortOrder(b);
            });
            hitInteractable = hitInteractables[0];
        }

        return hitInteractable;
    }

    private List<Interactable> GetInteractablesFromRayHits(RaycastHit2D[] rayHits)
    {
        var hitInteractables = new List<Interactable>();
        for (int i = 0; i < rayHits.Length; i++)
        {
            var interactable = rayHits[i].transform.GetComponent<Interactable>();
            if (interactable != null)
            {
                hitInteractables.Add(interactable);
            }
        }
        return hitInteractables;
    }
}
