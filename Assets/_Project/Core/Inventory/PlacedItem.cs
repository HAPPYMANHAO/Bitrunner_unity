using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class PlacedItem
    {
        internal PlacedItem(int instanceId, string definitionId, ItemShape baseShape, int rotation, Cell origin, bool rotatable, int count)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId;
            BaseShape = baseShape;
            Rotation = rotation;
            Origin = origin;
            Rotatable = rotatable;
            Count = count > 0 ? count : 1;
        }

        public int InstanceId { get; private set; }

        public string DefinitionId { get; private set; }

        public ItemShape BaseShape { get; private set; }

        public int Rotation { get; internal set; }

        public Cell Origin { get; internal set; }

        public bool Rotatable { get; private set; }

        public int Count { get; internal set; }

        public int Durability { get; internal set; }

        /// <summary>
        /// 探索出发时就带在身上的物品标记。阵亡清算只保留带这个标记的东西。
        /// 物品在背包与容器之间移动时会被重建成新实例，所以标记必须跟着
        /// <see cref="ItemTransfer.PreserveAll"/> 一起搬运。
        /// </summary>
        public bool ExpeditionBound { get; internal set; }

        public ItemContainer Container { get; internal set; }

        public ItemShape Shape
        {
            get { return BaseShape.Rotate(Rotation); }
        }

        public List<Cell> GetOccupiedCells()
        {
            var result = new List<Cell>();
            IReadOnlyList<Cell> cells = Shape.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                result.Add(new Cell(Origin.X + cells[i].X, Origin.Y + cells[i].Y));
            }

            return result;
        }
    }
}
