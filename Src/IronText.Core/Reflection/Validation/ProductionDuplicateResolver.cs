﻿using IronText.Collections;

namespace IronText.Reflection.Validation
{
    class ProductionDuplicateResolver : IDuplicateResolver<Production>
    {
        private static IDuplicateResolver<Production> _instance;

        public static IDuplicateResolver<Production> Instance
        {
            get
            {
                return _instance ?? (_instance = new ProductionDuplicateResolver());
            }
        }

        public Production Resolve(Production existingItem, Production newItem)
        {
            if (existingItem.ChildComponents.Length > newItem.ChildComponents.Length)
            {
                return newItem;
            }

            return existingItem;
        }
    }
}
