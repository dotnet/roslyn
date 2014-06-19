// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp
{
    internal class LocalSlotManager
    {
        private Dictionary<LocalSymbol, int> slots = new Dictionary<LocalSymbol, int>();

        internal int EnsureSlot(LocalSymbol local)
        {
            if (!slots.ContainsKey(local))
            {
                slots[local] = slots.Count;
            }

            return slots[local];
        }

        public int NumSlots
        {
            get { return this.slots.Count; }
        }

        public LocalSymbol LocalAtSlot(int i)
        {
            return this.slots
                    .Where(x => x.Value == i)
                    .Select(x => x.Key)
                    .Single();
        }
    }
}
