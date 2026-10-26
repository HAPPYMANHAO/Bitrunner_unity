using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public readonly struct ItemPair
    {
        public readonly int A;
        public readonly int B;

        public ItemPair(int a, int b)
        {
            A = a < b ? a : b;
            B = a < b ? b : a;
        }
    }

    public sealed class BagInventory
    {
        private readonly Dictionary<int, PlacedItem> _items = new Dictionary<int, PlacedItem>();
        private int _nextInstanceId = 1;

        public BagInventory(int width, int height)
            : this(new BagGrid(width, height))
        {
        }

        public BagInventory(BagGrid grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            Grid = grid;
        }

        public BagGrid Grid { get; private set; }

        /// <summary>
        /// 可选修改守卫：返回 null 表示允许，返回文案表示拒绝。
        /// 通讯中枢用它把被任务锁定的上行链路钉在格子里，防止绕过 UI 直接改背包。
        /// </summary>
        public Func<int, string> ItemModificationGuard;

        public int Count
        {
            get { return _items.Count; }
        }

        public int Capacity
        {
            get { return Grid.Capacity; }
        }

        public int OccupiedCellCount
        {
            get { return Grid.GetOccupiedCount(); }
        }

        public int FreeCellCount
        {
            get { return Grid.Capacity - OccupiedCellCount; }
        }

        public IReadOnlyList<PlacedItem> Items
        {
            get
            {
                var list = new List<PlacedItem>(_items.Values);
                list.Sort(CompareItems);
                return list;
            }
        }

        public bool TryGet(int instanceId, out PlacedItem item)
        {
            return _items.TryGetValue(instanceId, out item);
        }

        /// <summary>
        /// 原地换尺寸（仓储设备 / 保险网络中枢扩容用）。
        ///
        /// 物品实例本身不动，只把占位重排到新网格上——容器内容物、耐久、绑定标记全部原样保留。
        /// 刻意做成「原地」而不是返回新对象：<see cref="ItemBitWallet"/>、`BagView`、拖拽源
        /// 都持有这个实例的引用，换对象会让比特写进旧仓库、界面继续显示旧格子。
        ///
        /// 新格子放不下任何一件物品时整体回滚并返回 false（绝不静默丢东西）。
        /// </summary>
        public bool ResizeTo(int width, int height)
        {
            if (width == Grid.Width && height == Grid.Height)
            {
                return true;
            }

            var next = new BagGrid(width, height);
            var items = new List<PlacedItem>(_items.Values);

            // 大件优先：先放 2×2 再放 1×1，否则小件会把位置占碎，大件反而无处落脚。
            items.Sort(CompareByFootprint);

            var previous = new List<Cell>(items.Count);
            var previousRotation = new List<int>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                previous.Add(items[i].Origin);
                previousRotation.Add(items[i].Rotation);
                if (TryPlaceInto(next, items[i]))
                {
                    continue;
                }

                for (int j = i; j >= 0; j--)
                {
                    items[j].Origin = previous[j];
                    items[j].Rotation = previousRotation[j];
                }

                return false;
            }

            Grid = next;
            return true;
        }

        private static int CompareByFootprint(PlacedItem left, PlacedItem right)
        {
            int cells = right.BaseShape.CellCount.CompareTo(left.BaseShape.CellCount);
            return cells != 0 ? cells : left.InstanceId.CompareTo(right.InstanceId);
        }

        private static bool TryPlaceInto(BagGrid grid, PlacedItem item)
        {
            // 原位置原朝向能用就完全不动，扩容时绝大多数物品走这条分支。
            if (grid.CanPlace(item.Shape, item.Origin))
            {
                grid.Place(item.InstanceId, item.Shape, item.Origin);
                return true;
            }

            int rotations = item.Rotatable ? 4 : 1;
            for (int rotation = 0; rotation < rotations; rotation++)
            {
                ItemShape shape = item.BaseShape.Rotate(rotation);
                if (shape.Width > grid.Width || shape.Height > grid.Height)
                {
                    continue;
                }

                for (int y = 0; y <= grid.Height - shape.Height; y++)
                {
                    for (int x = 0; x <= grid.Width - shape.Width; x++)
                    {
                        var origin = new Cell(x, y);
                        if (!grid.CanPlace(shape, origin))
                        {
                            continue;
                        }

                        item.Rotation = rotation;
                        item.Origin = origin;
                        grid.Place(item.InstanceId, shape, origin);
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryAdd(string definitionId, ItemShape baseShape, out PlacedItem item)
        {
            return TryAdd(definitionId, baseShape, true, out item);
        }

        public bool TryAdd(string definitionId, ItemShape baseShape, bool rotatable, out PlacedItem item)
        {
            return TryAdd(definitionId, baseShape, rotatable, 1, out item);
        }

        public bool TryAdd(string definitionId, ItemShape baseShape, bool rotatable, int count, out PlacedItem item)
        {
            if (definitionId == null)
            {
                throw new ArgumentNullException(nameof(definitionId));
            }

            if (baseShape == null)
            {
                throw new ArgumentNullException(nameof(baseShape));
            }

            if (count <= 0)
            {
                count = 1;
            }

            int rotations = rotatable ? 4 : 1;
            for (int rotation = 0; rotation < rotations; rotation++)
            {
                ItemShape shape = baseShape.Rotate(rotation);
                for (int y = 0; y <= Grid.Height - shape.Height; y++)
                {
                    for (int x = 0; x <= Grid.Width - shape.Width; x++)
                    {
                        var origin = new Cell(x, y);
                        if (!Grid.CanPlace(shape, origin))
                        {
                            continue;
                        }

                        item = Commit(definitionId, baseShape, rotation, origin, rotatable, count);
                        return true;
                    }
                }
            }

            item = null;
            return false;
        }

        public bool TryAddOrStack(
            string definitionId,
            ItemShape baseShape,
            bool rotatable,
            int count,
            int maxStack,
            out PlacedItem item,
            out string error)
        {
            item = null;
            error = null;
            if (count <= 0)
            {
                count = 1;
            }

            if (maxStack > 1)
            {
                IReadOnlyList<PlacedItem> items = Items;
                for (int i = 0; i < items.Count; i++)
                {
                    PlacedItem existing = items[i];
                    if (existing.DefinitionId == definitionId
                        && existing.Count + count <= maxStack)
                    {
                        existing.Count += count;
                        item = existing;
                        return true;
                    }
                }
            }

            if (TryAdd(definitionId, baseShape, rotatable, count, out item))
            {
                return true;
            }

            error = "目标空间不足。";
            return false;
        }

        public int AddAmount(string definitionId, ItemShape baseShape, int amount, int maxStack)
        {
            if (amount <= 0 || definitionId == null || baseShape == null)
            {
                return 0;
            }

            if (maxStack < 1)
            {
                maxStack = 1;
            }

            int added = 0;
            if (maxStack > 1)
            {
                IReadOnlyList<PlacedItem> items = Items;
                for (int i = 0; i < items.Count && added < amount; i++)
                {
                    PlacedItem existing = items[i];
                    if (existing.DefinitionId != definitionId || existing.Count >= maxStack)
                    {
                        continue;
                    }

                    int room = maxStack - existing.Count;
                    int take = Math.Min(room, amount - added);
                    existing.Count += take;
                    added += take;
                }
            }

            while (added < amount)
            {
                int chunk = Math.Min(maxStack, amount - added);
                PlacedItem placed;
                if (!TryAdd(definitionId, baseShape, true, chunk, out placed))
                {
                    break;
                }

                added += chunk;
            }

            return added;
        }

        public int CountOf(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
            {
                return 0;
            }

            int total = 0;
            IReadOnlyList<PlacedItem> items = Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].DefinitionId == definitionId)
                {
                    total += items[i].Count;
                }
            }

            return total;
        }

        public bool TryRemoveAmount(string definitionId, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (amount == 0)
            {
                return true;
            }

            if (CountOf(definitionId) < amount)
            {
                return false;
            }

            int remaining = amount;
            IReadOnlyList<PlacedItem> items = Items;
            if (ItemModificationGuard != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].DefinitionId != definitionId)
                    {
                        continue;
                    }

                    string guardError;
                    if (!CheckModifiable(items[i].InstanceId, out guardError))
                    {
                        return false;
                    }
                }
            }

            for (int i = 0; i < items.Count && remaining > 0; i++)
            {
                PlacedItem item = items[i];
                if (item.DefinitionId != definitionId)
                {
                    continue;
                }

                int take = Math.Min(item.Count, remaining);
                item.Count -= take;
                remaining -= take;
                if (item.Count <= 0)
                {
                    Remove(item.InstanceId);
                }
            }

            return remaining == 0;
        }

        public bool TryRemoveAmount(int instanceId, int amount, out int removed)
        {
            removed = 0;
            PlacedItem item;
            if (!TryGet(instanceId, out item) || amount <= 0)
            {
                return false;
            }

            string error;
            if (!CheckModifiable(instanceId, out error))
            {
                return false;
            }

            removed = Math.Min(item.Count, amount);
            item.Count -= removed;
            if (item.Count <= 0)
            {
                Remove(instanceId);
            }

            return removed > 0;
        }

        public bool CanStackAt(string definitionId, ItemShape baseShape, Cell origin, int rotation, int count, int maxStack)
        {
            if (count <= 0 || maxStack <= 1 || definitionId == null || baseShape == null || !Grid.InBounds(origin.X, origin.Y))
            {
                return false;
            }

            int occupant = Grid.GetOccupant(origin.X, origin.Y);
            if (occupant == 0)
            {
                return Grid.CanPlace(baseShape.Rotate(rotation), origin);
            }

            PlacedItem existing;
            return _items.TryGetValue(occupant, out existing)
                   && existing.DefinitionId == definitionId
                   && existing.Count + count <= maxStack;
        }

        public bool TryStackAt(string definitionId, ItemShape baseShape, Cell origin, int rotation, int count, int maxStack, out PlacedItem item, out string error)
        {
            if (CanStackAt(definitionId, baseShape, origin, rotation, count, maxStack))
            {
                int occupant = Grid.GetOccupant(origin.X, origin.Y);
                if (occupant != 0)
                {
                    _items.TryGetValue(occupant, out item);
                    item.Count += count;
                    error = null;
                    return true;
                }
            }

            return TryAddAt(definitionId, baseShape, origin, rotation, true, count, out item, out error);
        }

        public bool TryAddAt(string definitionId, ItemShape baseShape, Cell origin, int rotation, bool rotatable, out PlacedItem item, out string error)
        {
            return TryAddAt(definitionId, baseShape, origin, rotation, rotatable, 1, out item, out error);
        }

        public bool TryAddAt(string definitionId, ItemShape baseShape, Cell origin, int rotation, bool rotatable, int count, out PlacedItem item, out string error)
        {
            if (definitionId == null)
            {
                throw new ArgumentNullException(nameof(definitionId));
            }

            if (baseShape == null)
            {
                throw new ArgumentNullException(nameof(baseShape));
            }

            item = null;

            if (count <= 0)
            {
                count = 1;
            }

            if (rotation < 0 || rotation > 3)
            {
                error = "Rotation must be between 0 and 3.";
                return false;
            }

            if (!rotatable && rotation != 0)
            {
                error = "Item cannot be rotated.";
                return false;
            }

            ItemShape shape = baseShape.Rotate(rotation);
            if (!Grid.CanPlace(shape, origin))
            {
                error = "Shape does not fit at " + origin + ".";
                return false;
            }

            item = Commit(definitionId, baseShape, rotation, origin, rotatable, count);
            error = null;
            return true;
        }

        public bool TryMove(int instanceId, Cell newOrigin, out string error)
        {
            PlacedItem item;
            if (!_items.TryGetValue(instanceId, out item))
            {
                error = "Unknown item " + instanceId + ".";
                return false;
            }

            if (!CheckModifiable(instanceId, out error))
            {
                return false;
            }

            if (item.Origin == newOrigin)
            {
                error = null;
                return true;
            }

            ItemShape shape = item.Shape;
            if (!Grid.CanPlace(shape, newOrigin, instanceId))
            {
                error = "Shape does not fit at " + newOrigin + ".";
                return false;
            }

            Grid.Clear(instanceId);
            Grid.Place(instanceId, shape, newOrigin);
            item.Origin = newOrigin;
            error = null;
            return true;
        }

        public bool TryTransform(int instanceId, Cell newOrigin, int newRotation, out string error)
        {
            PlacedItem item;
            if (!_items.TryGetValue(instanceId, out item))
            {
                error = "Unknown item " + instanceId + ".";
                return false;
            }

            if (!CheckModifiable(instanceId, out error))
            {
                return false;
            }

            if (newRotation < 0 || newRotation > 3)
            {
                error = "Rotation must be between 0 and 3.";
                return false;
            }

            if (!item.Rotatable && newRotation != 0)
            {
                error = "Item cannot be rotated.";
                return false;
            }

            ItemShape shape = item.BaseShape.Rotate(newRotation);
            if (!Grid.CanPlace(shape, newOrigin, instanceId))
            {
                error = "Shape does not fit at " + newOrigin + ".";
                return false;
            }

            if (item.Origin == newOrigin && item.Rotation == newRotation)
            {
                error = null;
                return true;
            }

            Grid.Clear(instanceId);
            Grid.Place(instanceId, shape, newOrigin);
            item.Origin = newOrigin;
            item.Rotation = newRotation;
            error = null;
            return true;
        }

        public bool TryRotate(int instanceId, out string error)
        {
            PlacedItem item;
            if (!_items.TryGetValue(instanceId, out item))
            {
                error = "Unknown item " + instanceId + ".";
                return false;
            }

            if (!CheckModifiable(instanceId, out error))
            {
                return false;
            }

            if (!item.Rotatable)
            {
                error = "Item cannot be rotated.";
                return false;
            }

            int rotation = (item.Rotation + 1) % 4;
            ItemShape shape = item.BaseShape.Rotate(rotation);
            if (!Grid.CanPlace(shape, item.Origin, instanceId))
            {
                error = "Rotated shape does not fit at " + item.Origin + ".";
                return false;
            }

            Grid.Clear(instanceId);
            Grid.Place(instanceId, shape, item.Origin);
            item.Rotation = rotation;
            error = null;
            return true;
        }

        /// <summary>
        /// 右键旋转：优先原地旋转；原地放不下时，在包内找离原位置最近的空位放下旋转后的形状。
        /// 战利品包和战斗背包经常是满的，原地旋转几乎必然失败。
        /// </summary>
        public bool TryRotateRelocating(int instanceId, out string error)
        {
            PlacedItem item;
            if (!_items.TryGetValue(instanceId, out item))
            {
                error = "Unknown item " + instanceId + ".";
                return false;
            }

            if (!CheckModifiable(instanceId, out error))
            {
                return false;
            }

            if (!item.Rotatable)
            {
                error = "Item cannot be rotated.";
                return false;
            }

            int rotation = (item.Rotation + 1) % 4;
            ItemShape shape = item.BaseShape.Rotate(rotation);
            if (Grid.CanPlace(shape, item.Origin, instanceId))
            {
                Grid.Clear(instanceId);
                Grid.Place(instanceId, shape, item.Origin);
                item.Rotation = rotation;
                error = null;
                return true;
            }

            Cell origin = FindNearestOrigin(shape, item.Origin, instanceId);
            if (origin.X < 0)
            {
                error = "空间不足，无法旋转。";
                return false;
            }

            Grid.Clear(instanceId);
            Grid.Place(instanceId, shape, origin);
            item.Rotation = rotation;
            item.Origin = origin;
            error = null;
            return true;
        }

        private Cell FindNearestOrigin(ItemShape shape, Cell around, int ignoreId)
        {
            Cell best = new Cell(-1, -1);
            int bestDistance = int.MaxValue;

            for (int y = 0; y < Grid.Height; y++)
            {
                for (int x = 0; x < Grid.Width; x++)
                {
                    var origin = new Cell(x, y);
                    if (!Grid.CanPlace(shape, origin, ignoreId))
                    {
                        continue;
                    }

                    int dx = x - around.X;
                    int dy = y - around.Y;
                    int distance = dx * dx + dy * dy;
                    if (distance < bestDistance)
                    {
                        best = origin;
                        bestDistance = distance;
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// 存档恢复专用：按**原实例 ID、原坐标、原朝向**精确放回，并跳过
        /// <see cref="ItemModificationGuard"/>——读档时通讯中枢还没有锁，也不该被锁住。
        ///
        /// 与 <see cref="TryAddAt"/> 的差别：这里的实例 ID 是外部给定的，
        /// 因为通讯锁、商人报价都用实例 ID 指代物品，重新编号会让它们全部失效。
        /// 内部计数器会抬到最大实例 ID 之上，之后新增的物品不会撞号。
        /// </summary>
        internal bool TryRestoreItem(
            int instanceId,
            string definitionId,
            ItemShape baseShape,
            int rotation,
            bool rotatable,
            Cell origin,
            int count,
            int durability,
            bool expeditionBound,
            out PlacedItem item,
            out string error)
        {
            item = null;

            if (instanceId <= 0)
            {
                error = "实例 ID 非法（" + instanceId + "）。";
                return false;
            }

            if (definitionId == null || baseShape == null)
            {
                error = "物品定义缺失。";
                return false;
            }

            if (_items.ContainsKey(instanceId))
            {
                error = "实例 ID " + instanceId + " 重复。";
                return false;
            }

            if (rotation < 0 || rotation > 3)
            {
                error = "旋转值非法（" + rotation + "）。";
                return false;
            }

            if (!rotatable && rotation != 0)
            {
                error = "该物品不可旋转，但存档里带了旋转值 " + rotation + "。";
                return false;
            }

            if (count <= 0)
            {
                count = 1;
            }

            ItemShape shape = baseShape.Rotate(rotation);
            if (!Grid.CanPlace(shape, origin))
            {
                error = "物品 " + definitionId + " 在 " + origin + " 无法放下（越界或重叠）。";
                return false;
            }

            Grid.Place(instanceId, shape, origin);
            var placed = new PlacedItem(instanceId, definitionId, baseShape, rotation, origin, rotatable, count);
            placed.Durability = durability;
            placed.ExpeditionBound = expeditionBound;
            _items.Add(instanceId, placed);

            if (instanceId >= _nextInstanceId)
            {
                _nextInstanceId = instanceId + 1;
            }

            item = placed;
            error = null;
            return true;
        }

        public bool Remove(int instanceId)
        {
            if (!_items.ContainsKey(instanceId))
            {
                return false;
            }

            string error;
            if (!CheckModifiable(instanceId, out error))
            {
                return false;
            }

            Grid.Clear(instanceId);
            _items.Remove(instanceId);
            return true;
        }

        public IReadOnlyList<ItemPair> GetAdjacentPairs()
        {
            var seen = new HashSet<long>();
            var pairs = new List<ItemPair>();

            for (int y = 0; y < Grid.Height; y++)
            {
                for (int x = 0; x < Grid.Width; x++)
                {
                    int first = Grid.GetOccupant(x, y);
                    if (first == 0)
                    {
                        continue;
                    }

                    if (x + 1 < Grid.Width)
                    {
                        AddPair(first, Grid.GetOccupant(x + 1, y), seen, pairs);
                    }

                    if (y + 1 < Grid.Height)
                    {
                        AddPair(first, Grid.GetOccupant(x, y + 1), seen, pairs);
                    }
                }
            }

            pairs.Sort(ComparePairs);
            return pairs;
        }

        public bool AreAdjacent(int firstId, int secondId)
        {
            if (firstId == secondId)
            {
                return false;
            }

            IReadOnlyList<ItemPair> pairs = GetAdjacentPairs();
            for (int i = 0; i < pairs.Count; i++)
            {
                if ((pairs[i].A == firstId && pairs[i].B == secondId)
                    || (pairs[i].A == secondId && pairs[i].B == firstId))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddPair(int first, int second, HashSet<long> seen, List<ItemPair> pairs)
        {
            if (second == 0 || second == first)
            {
                return;
            }

            int low = first < second ? first : second;
            int high = first < second ? second : first;
            long key = ((long)low << 32) | (uint)high;
            if (seen.Add(key))
            {
                pairs.Add(new ItemPair(low, high));
            }
        }

        private bool CheckModifiable(int instanceId, out string error)
        {
            if (ItemModificationGuard != null)
            {
                string reason = ItemModificationGuard(instanceId);
                if (!string.IsNullOrEmpty(reason))
                {
                    error = reason;
                    return false;
                }
            }

            error = null;
            return true;
        }

        private PlacedItem Commit(string definitionId, ItemShape baseShape, int rotation, Cell origin, bool rotatable, int count)
        {
            int instanceId = _nextInstanceId;
            _nextInstanceId++;

            ItemShape shape = baseShape.Rotate(rotation);
            Grid.Place(instanceId, shape, origin);

            var item = new PlacedItem(instanceId, definitionId, baseShape, rotation, origin, rotatable, count);
            _items.Add(instanceId, item);
            return item;
        }

        private static int CompareItems(PlacedItem a, PlacedItem b)
        {
            return a.InstanceId.CompareTo(b.InstanceId);
        }

        private static int ComparePairs(ItemPair a, ItemPair b)
        {
            if (a.A != b.A)
            {
                return a.A.CompareTo(b.A);
            }

            return a.B.CompareTo(b.B);
        }
    }
}
