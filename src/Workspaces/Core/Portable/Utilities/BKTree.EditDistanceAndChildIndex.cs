using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal partial class BKTree
    {
        private struct EditDistanceAndChildIndex
        {
            // The edit distance between this child and its parent.
            public readonly int EditDistance;

            // Where the child node can be found on "nodeArray"
            public readonly int ChildNodeIndexInNodeArray;

            public EditDistanceAndChildIndex(int editDistance, int childNodeIndexInNodeArray)
            {
                EditDistance = editDistance;
                ChildNodeIndexInNodeArray = childNodeIndexInNodeArray;
            }
        }
    }
}
