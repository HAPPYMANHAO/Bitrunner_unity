using System;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BitRunner.Game
{
    public sealed class BagInteraction : MonoBehaviour
    {
        private BagInventory _inventory;
        private BagView _view;
        private RectTransform _bagRect;
        private Text _info;
        private RectTransform _discardRect;

        private bool _pressed;
        private int _pressedId;
        private Cell _pressCell;
        private Vector2 _pressScreen;

        private int _dragId;
        private Cell _grabOffset;
        private Cell _target;
        private bool _targetValid;
        private bool _dragging;
        private bool _overDiscard;

        /// <summary>战斗内锁定背包布局：保留点击使用，禁止拖动和旋转。</summary>
        public bool LockPosition;

        public Action<int> ItemClicked;
        public event Action<string> ItemDiscarded;

        public void Initialize(BagInventory inventory, BagView view, RectTransform bagRect, Text info, RectTransform discardRect = null)
        {
            _inventory = inventory;
            _view = view;
            _bagRect = bagRect;
            _info = info;
            _discardRect = discardRect;
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 screen = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame && !_pressed && !_dragging)
            {
                Press(screen);
            }

            if (_pressed && !_dragging && mouse.leftButton.isPressed)
            {
                if ((screen - _pressScreen).sqrMagnitude > 36f)
                {
                    if (LockPosition)
                    {
                        _pressed = false;
                        _pressedId = 0;
                    }
                    else
                    {
                        StartDrag();
                    }
                }
            }

            if (_dragging && mouse.leftButton.isPressed)
            {
                UpdateDrag(screen);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (_dragging)
                {
                    UpdateDrag(screen);
                    EndDrag();
                }
                else if (_pressed)
                {
                    int clickedId = _pressedId;
                    _pressed = false;
                    _pressedId = 0;
                    if (ItemClicked != null && clickedId != 0)
                    {
                        ItemClicked(clickedId);
                    }
                }

                _pressed = false;
                _pressedId = 0;
            }

            if (!_dragging && !_pressed && mouse.rightButton.wasPressedThisFrame && !LockPosition)
            {
                RotateUnderCursor(screen);
            }

            UpdateInfo(screen);
        }

        private void Press(Vector2 screen)
        {
            Cell cell;
            if (!TryPointerCell(screen, out cell))
            {
                return;
            }

            int instanceId = _inventory.Grid.GetOccupant(cell.X, cell.Y);
            if (instanceId == 0)
            {
                return;
            }

            PlacedItem item;
            if (!_inventory.TryGet(instanceId, out item))
            {
                return;
            }

            _pressed = true;
            _pressedId = instanceId;
            _pressCell = cell;
            _pressScreen = screen;
        }

        private void StartDrag()
        {
            PlacedItem item;
            if (!_inventory.TryGet(_pressedId, out item))
            {
                _pressed = false;
                return;
            }

            _dragging = true;
            _dragId = _pressedId;
            _grabOffset = new Cell(_pressCell.X - item.Origin.X, _pressCell.Y - item.Origin.Y);
            _target = item.Origin;
            _targetValid = true;
            _overDiscard = false;

            _view.SetItemAlpha(_dragId, 0.2f);
            _view.ShowGhost(item.Shape, _target, true);
        }

        private void UpdateDrag(Vector2 screen)
        {
            if (_discardRect != null
                && RectTransformUtility.RectangleContainsScreenPoint(_discardRect, screen, null))
            {
                _overDiscard = true;
                _targetValid = false;
                _view.HideGhost();
                return;
            }

            _overDiscard = false;

            Cell cell;
            if (!TryPointerCell(screen, out cell))
            {
                _targetValid = false;
                _view.HideGhost();
                return;
            }

            PlacedItem item;
            if (!_inventory.TryGet(_dragId, out item))
            {
                _targetValid = false;
                _view.HideGhost();
                return;
            }

            _target = new Cell(cell.X - _grabOffset.X, cell.Y - _grabOffset.Y);
            _targetValid = _inventory.Grid.CanPlace(item.Shape, _target, _dragId);
            _view.ShowGhost(item.Shape, _target, _targetValid);
        }

        private void EndDrag()
        {
            if (_overDiscard)
            {
                PlacedItem item;
                string definitionId = null;
                if (_inventory.TryGet(_dragId, out item))
                {
                    definitionId = item.DefinitionId;
                }

                _inventory.Remove(_dragId);
                if (definitionId != null && ItemDiscarded != null)
                {
                    ItemDiscarded(definitionId);
                }
            }
            else if (_targetValid)
            {
                string error;
                _inventory.TryMove(_dragId, _target, out error);
            }

            _dragging = false;
            _dragId = 0;
            _targetValid = false;
            _overDiscard = false;
            _view.HideGhost();
            _view.Refresh();
        }

        private void RotateUnderCursor(Vector2 screen)
        {
            Cell cell;
            if (!TryPointerCell(screen, out cell))
            {
                return;
            }

            int instanceId = _inventory.Grid.GetOccupant(cell.X, cell.Y);
            if (instanceId == 0)
            {
                return;
            }

            string error;
            if (_inventory.TryRotateRelocating(instanceId, out error))
            {
                _view.Refresh();
            }
        }

        private void UpdateInfo(Vector2 screen)
        {
            if (_info == null)
            {
                return;
            }

            if (_dragging)
            {
                _info.text = "Dragging item " + _dragId;
                return;
            }

            Cell cell;
            if (TryPointerCell(screen, out cell))
            {
                int instanceId = _inventory.Grid.GetOccupant(cell.X, cell.Y);
                if (instanceId != 0)
                {
                    PlacedItem item;
                    if (_inventory.TryGet(instanceId, out item))
                    {
                        _info.text = item.DefinitionId
                                     + "\nrot " + item.Rotation + "  cells " + item.Shape.CellCount
                                     + "\n" + item.Shape.ToPattern().Replace("\n", " / ");
                        return;
                    }
                }
            }

            _info.text = "Hover an item";
        }

        private bool TryPointerCell(Vector2 screen, out Cell cell)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_bagRect, screen, null, out local))
            {
                cell = Cell.Zero;
                return false;
            }

            float halfWidth = _inventory.Grid.Width * BagView.CellSize * 0.5f;
            float halfHeight = _inventory.Grid.Height * BagView.CellSize * 0.5f;

            int x = Mathf.FloorToInt((local.x + halfWidth) / BagView.CellSize);
            int y = Mathf.FloorToInt((halfHeight - local.y) / BagView.CellSize);

            cell = new Cell(x, y);
            return x >= 0 && x < _inventory.Grid.Width && y >= 0 && y < _inventory.Grid.Height;
        }
    }
}
