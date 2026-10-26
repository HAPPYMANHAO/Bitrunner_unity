using System.Collections.Generic;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BitRunner.Game
{
    public sealed class FloatingWindowManager : MonoBehaviour
    {
        private readonly List<BaseDragInteraction> _drags = new List<BaseDragInteraction>();
        private readonly List<BagInventory> _extraBags = new List<BagInventory>();
        private readonly Dictionary<int, ContainerWindow> _windows = new Dictionary<int, ContainerWindow>();

        private static int _openWindowCount;

        private RectTransform _canvas;
        private ItemCatalog _catalog;
        private Dictionary<string, Color> _palette;
        private int _openIndex;

        /// <summary>打开/关闭容器窗口时通知外部（战斗搜刮界面会把这些窗口接成拖拽源）。</summary>
        public event System.Action<ContainerWindow> WindowOpened;
        public event System.Action<ContainerWindow> WindowClosed;

        public void Initialize(RectTransform canvas, ItemCatalog catalog, Dictionary<string, Color> palette)
        {
            _canvas = canvas;
            _catalog = catalog;
            _palette = palette;
        }

        /// <summary>
        /// 没有 BaseDragInteraction 的界面（战斗搜刮）用这个登记容器可能所在的背包，
        /// 否则 RefreshAll 会以为容器已消失，把刚打开的窗口立刻关掉。
        /// </summary>
        public void AttachBag(BagInventory bag)
        {
            if (bag != null && !_extraBags.Contains(bag))
            {
                _extraBags.Add(bag);
            }
        }

        public void AttachDrag(BaseDragInteraction drag)
        {
            if (drag == null || _drags.Contains(drag))
            {
                return;
            }

            _drags.Add(drag);
            foreach (ContainerWindow window in _windows.Values)
            {
                drag.AddPersistentSource(window.Source);
            }
        }

        public void DetachDrag(BaseDragInteraction drag)
        {
            if (drag == null)
            {
                return;
            }

            for (int i = _drags.Count - 1; i >= 0; i--)
            {
                if (_drags[i] == drag)
                {
                    _drags.RemoveAt(i);
                }
            }
        }

        /// <summary>当前打开的容器窗口数；设置面板据此判断 Esc 应该先关窗口还是打开自己。</summary>
        public static int OpenWindowCount
        {
            get { return _openWindowCount; }
        }

        public bool TryOpen(BagInventory bag, int instanceId)
        {
            if (bag == null || _catalog == null)
            {
                return false;
            }

            PlacedItem item;
            if (!bag.TryGet(instanceId, out item))
            {
                return false;
            }

            ItemDefinition definition;
            if (!_catalog.TryGet(item.DefinitionId, out definition))
            {
                return false;
            }

            ItemContainer container = ContainerOps.Ensure(item, definition);
            if (container == null)
            {
                return false;
            }

            ContainerWindow existing;
            if (_windows.TryGetValue(container.Id, out existing))
            {
                BringToFront(existing);
                return true;
            }

            CreateWindow(container, definition);
            return true;
        }

        /// <summary>屏幕坐标下的容器窗口（用于拖拽投放目标）。</summary>
        public ItemContainer FindContainerAt(Vector2 screen)
        {
            foreach (ContainerWindow window in _windows.Values)
            {
                BaseBagSource source = window.Source;
                if (source == null || source.Rect == null)
                {
                    continue;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(source.Rect, screen, null))
                {
                    return window.Container;
                }
            }

            return null;
        }

        public bool TryOpen(BaseBagSource source, int instanceId)
        {
            if (source == null)
            {
                return false;
            }

            PlacedItem item;
            if (!source.Bag.TryGet(instanceId, out item))
            {
                return false;
            }

            ItemDefinition definition;
            if (!_catalog.TryGet(item.DefinitionId, out definition))
            {
                return false;
            }

            ItemContainer container = ContainerOps.Ensure(item, definition);
            if (container == null)
            {
                return false;
            }

            ContainerWindow existing;
            if (_windows.TryGetValue(container.Id, out existing))
            {
                BringToFront(existing);
                return true;
            }

            CreateWindow(container, definition);
            return true;
        }

        public PlacedItem FindHoveredItem(Vector2 screen)
        {
            foreach (ContainerWindow window in _windows.Values)
            {
                BaseBagSource source = window.Source;
                if (source == null || source.Rect == null || source.Bag == null)
                {
                    continue;
                }

                Vector2 local;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(source.Rect, screen, null, out local))
                {
                    continue;
                }

                float halfWidth = source.Bag.Grid.Width * BagView.CellSize * 0.5f;
                float halfHeight = source.Bag.Grid.Height * BagView.CellSize * 0.5f;
                int x = Mathf.FloorToInt((local.x + halfWidth) / BagView.CellSize);
                int y = Mathf.FloorToInt((halfHeight - local.y) / BagView.CellSize);
                if (x < 0 || x >= source.Bag.Grid.Width || y < 0 || y >= source.Bag.Grid.Height)
                {
                    continue;
                }

                int instanceId = source.Bag.Grid.GetOccupant(x, y);
                PlacedItem item;
                if (instanceId != 0 && source.Bag.TryGet(instanceId, out item))
                {
                    return item;
                }
            }

            return null;
        }

        public void RefreshAll()
        {
            var stale = new List<int>();
            foreach (KeyValuePair<int, ContainerWindow> pair in _windows)
            {
                if (!IsContainerPresent(pair.Value.Container))
                {
                    stale.Add(pair.Key);
                    continue;
                }

                pair.Value.Refresh();
            }

            for (int i = 0; i < stale.Count; i++)
            {
                Close(stale[i]);
            }
        }

        private bool IsContainerPresent(ItemContainer container)
        {
            if (container == null)
            {
                return false;
            }

            for (int i = 0; i < _extraBags.Count; i++)
            {
                BagInventory sharedBag = _extraBags[i];
                if (sharedBag == null)
                {
                    continue;
                }

                IReadOnlyList<PlacedItem> sharedItems = sharedBag.Items;
                for (int k = 0; k < sharedItems.Count; k++)
                {
                    if (sharedItems[k].Container == container)
                    {
                        return true;
                    }
                }
            }

            for (int i = 0; i < _drags.Count; i++)
            {
                BaseDragInteraction drag = _drags[i];
                if (drag == null)
                {
                    continue;
                }

                IReadOnlyList<BaseBagSource> sources = drag.Sources;
                for (int j = 0; j < sources.Count; j++)
                {
                    BagInventory bag = sources[j] == null ? null : sources[j].Bag;
                    if (bag == null)
                    {
                        continue;
                    }

                    IReadOnlyList<PlacedItem> items = bag.Items;
                    for (int k = 0; k < items.Count; k++)
                    {
                        if (items[k].Container == container)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool IsPointerOverChrome(Vector2 screen)
        {
            foreach (ContainerWindow window in _windows.Values)
            {
                if (window.IsChromeAt(screen))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsTopmost(ContainerWindow window, Vector2 screen)
        {
            if (window == null || _canvas == null)
            {
                return false;
            }

            for (int i = _canvas.childCount - 1; i >= 0; i--)
            {
                ContainerWindow candidate = _canvas.GetChild(i).GetComponent<ContainerWindow>();
                if (candidate == null)
                {
                    continue;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(
                        (RectTransform)candidate.transform,
                        screen,
                        null))
                {
                    return candidate == window;
                }
            }

            return false;
        }

        public void BringToFront(ContainerWindow window)
        {
            if (window == null)
            {
                return;
            }

            window.transform.SetAsLastSibling();
            for (int i = 0; i < _drags.Count; i++)
            {
                if (_drags[i] != null && window.Source != null)
                {
                    _drags[i].RemovePersistentSource(window.Source.Name);
                    _drags[i].AddPersistentSource(window.Source);
                }
            }
        }

        public void Close(int containerId)
        {
            ContainerWindow window;
            if (!_windows.TryGetValue(containerId, out window))
            {
                return;
            }

            _windows.Remove(containerId);
            if (_openWindowCount > 0)
            {
                _openWindowCount--;
            }

            for (int i = 0; i < _drags.Count; i++)
            {
                if (_drags[i] != null)
                {
                    _drags[i].RemovePersistentSource(window.Source.Name);
                }
            }

            if (WindowClosed != null)
            {
                WindowClosed(window);
            }

            Destroy(window.gameObject);
        }

        public void CloseAll()
        {
            var ids = new List<int>(_windows.Keys);
            for (int i = 0; i < ids.Count; i++)
            {
                Close(ids[i]);
            }
        }

        private void CreateWindow(ItemContainer container, ItemDefinition definition)
        {
            var go = new GameObject(
                "ContainerWindow_" + container.Id,
                typeof(RectTransform),
                typeof(Image),
                typeof(ContainerWindow));
            go.transform.SetParent(_canvas, false);
            go.transform.SetAsLastSibling();

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            ContainerWindow window = go.GetComponent<ContainerWindow>();
            window.Initialize(this, container, definition, _canvas, _catalog, _palette);
            window.Refresh();

            int slot = _openIndex;
            _openIndex++;
            Vector2 position = new Vector2(-110f + (slot % 4) * 22f, 52f - (slot % 4) * 24f);
            window.SetPosition(position);
            window.ClampTo(_canvas);

            _windows[container.Id] = window;
            _openWindowCount++;
            if (WindowOpened != null)
            {
                WindowOpened(window);
            }

            for (int i = 0; i < _drags.Count; i++)
            {
                if (_drags[i] != null)
                {
                    _drags[i].AddPersistentSource(window.Source);
                }
            }
        }

        private void Update()
        {
            if (SettingsOverlay.BlocksInput)
            {
                return;
            }

            if (_windows.Count == 0 || Keyboard.current == null)
            {
                return;
            }

            if (!Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            // Esc 已被这个窗口吃掉，设置面板不要在同一帧再打开。
            SettingsOverlay.ConsumeEscape();

            ContainerWindow top = null;
            for (int i = _canvas.childCount - 1; i >= 0; i--)
            {
                ContainerWindow candidate = _canvas.GetChild(i).GetComponent<ContainerWindow>();
                if (candidate != null)
                {
                    top = candidate;
                    break;
                }
            }

            if (top != null)
            {
                Close(top.ContainerId);
            }
        }
    }
}
