using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Whisperer
{
    public sealed class DeskLetterDragManipulator : Manipulator
    {
        readonly VisualElement dragHandle;
        readonly Action<Vector2> onPositionChanged;
        bool isDragging;
        int activePointerId = -1;
        Vector2 dragOffset;

        public DeskLetterDragManipulator(VisualElement dragHandle, Action<Vector2> onPositionChanged = null)
        {
            this.dragHandle = dragHandle ?? throw new ArgumentNullException(nameof(dragHandle));
            this.onPositionChanged = onPositionChanged;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            dragHandle.RegisterCallback<PointerDownEvent>(OnPointerDown);
            dragHandle.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            dragHandle.RegisterCallback<PointerUpEvent>(OnPointerUp);
            dragHandle.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            dragHandle.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            dragHandle.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            dragHandle.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            dragHandle.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (target == null || evt.button != 0 || isDragging)
            {
                return;
            }

            VisualElement parent = target.parent;
            if (parent == null)
            {
                return;
            }

            activePointerId = evt.pointerId;
            isDragging = true;
            dragHandle.CapturePointer(activePointerId);

            Vector2 pointerInParent = parent.WorldToLocal(evt.position);
            Vector2 targetPosition = new Vector2(
                target.worldBound.x - parent.worldBound.x,
                target.worldBound.y - parent.worldBound.y);
            dragOffset = pointerInParent - targetPosition;
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!isDragging || evt.pointerId != activePointerId)
            {
                return;
            }

            MoveTarget(evt.position);
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!isDragging || evt.pointerId != activePointerId)
            {
                return;
            }

            ReleasePointer();
            evt.StopPropagation();
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId != activePointerId)
            {
                return;
            }

            ReleasePointer();
        }

        void MoveTarget(Vector2 pointerPosition)
        {
            if (target == null)
            {
                return;
            }

            VisualElement parent = target.parent;
            if (parent == null)
            {
                return;
            }

            Vector2 pointerInParent = parent.WorldToLocal(pointerPosition);
            float maxLeft = Mathf.Max(0f, parent.contentRect.width - target.resolvedStyle.width);
            float maxTop = Mathf.Max(0f, parent.contentRect.height - target.resolvedStyle.height);
            float left = Mathf.Clamp(pointerInParent.x - dragOffset.x, 0f, maxLeft);
            float top = Mathf.Clamp(pointerInParent.y - dragOffset.y, 0f, maxTop);

            target.style.left = left;
            target.style.top = top;
            target.style.right = StyleKeyword.Auto;
            target.style.bottom = StyleKeyword.Auto;
            onPositionChanged?.Invoke(new Vector2(left, top));
        }

        void ReleasePointer()
        {
            if (dragHandle.HasPointerCapture(activePointerId))
            {
                dragHandle.ReleasePointer(activePointerId);
            }

            isDragging = false;
            activePointerId = -1;
        }
    }
}
