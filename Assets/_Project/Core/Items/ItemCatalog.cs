using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class ItemCatalog
    {
        private readonly Dictionary<string, ItemDefinition> _definitions = new Dictionary<string, ItemDefinition>();

        public ItemCatalog(IEnumerable<ItemDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            foreach (ItemDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException("Item definitions cannot be null.", nameof(definitions));
                }

                if (string.IsNullOrEmpty(definition.Id))
                {
                    throw new ArgumentException("Every item definition needs an Id.", nameof(definitions));
                }

                if (definition.Shape == null)
                {
                    throw new ArgumentException("Item '" + definition.Id + "' needs a shape.", nameof(definitions));
                }

                if (_definitions.ContainsKey(definition.Id))
                {
                    throw new ArgumentException("Duplicate item id '" + definition.Id + "'.", nameof(definitions));
                }

                _definitions.Add(definition.Id, definition);
            }
        }

        public int Count
        {
            get { return _definitions.Count; }
        }

        public IEnumerable<ItemDefinition> Definitions
        {
            get { return _definitions.Values; }
        }

        public bool TryGet(string id, out ItemDefinition definition)
        {
            return _definitions.TryGetValue(id, out definition);
        }

        public ItemDefinition Get(string id)
        {
            ItemDefinition definition;
            if (!_definitions.TryGetValue(id, out definition))
            {
                throw new KeyNotFoundException("Unknown item id '" + id + "'.");
            }

            return definition;
        }
    }
}
