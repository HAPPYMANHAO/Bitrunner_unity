using System.Threading;

namespace BitRunner.Core
{
    public sealed class ItemContainer
    {
        private static int _nextId;

        public ItemContainer(ContainerSpec spec)
        {
            Spec = spec;
            Id = Interlocked.Increment(ref _nextId);
            Bag = new BagInventory(spec.Width, spec.Height);
        }

        public int Id { get; private set; }

        public ContainerSpec Spec { get; private set; }

        public BagInventory Bag { get; private set; }

        public int FreeCellCount
        {
            get { return Bag.FreeCellCount; }
        }

        public bool CanAccept(ItemDefinition definition)
        {
            return Spec != null && Spec.Accepts(definition);
        }
    }
}
