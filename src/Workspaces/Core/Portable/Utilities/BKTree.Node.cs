using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal partial class BKTree
    {
        private struct Node
        {
            // The string this node corresponds to.  Stored in char[] format so we can easily compute
            // edit distances on it.
            public readonly char[] LowerCaseCharacters;

            // How many children this node has.
            public readonly int ChildCount;

            // Where the children can be found in "editDistanceArray".
            public readonly int FirstChildIndexInEditDistanceArray;

            public Node(char[] lowerCaseCharacters, int childCount, int firstChildIndexInEditDistanceArray)
            {
                LowerCaseCharacters = lowerCaseCharacters;
                ChildCount = childCount;
                FirstChildIndexInEditDistanceArray = firstChildIndexInEditDistanceArray;
            }
        }
    }
}
