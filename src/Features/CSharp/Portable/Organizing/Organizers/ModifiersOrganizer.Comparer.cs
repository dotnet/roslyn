// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.Organizing.Organizers
{
    internal partial class ModifiersOrganizer : IComparer<SyntaxToken>
    {
        int IComparer<SyntaxToken>.Compare(SyntaxToken x, SyntaxToken y)
        {
            return GetOrdering(x) - GetOrdering(y);
        }

        private int GetOrdering(SyntaxToken token)
        {
            if (_preferredOrder.TryGetValue(token.RawKind, out var order))
            {
                return order;
            }
            else if (token.IsKind(SyntaxKind.PartialKeyword))
            {
                // 'partial' comes at the end unless explicitly specified in the code style option
                return int.MaxValue;
            }
            else
            {
                // other unspecified options come at the end except for the special case of 'partial'
                return int.MaxValue - 1;
            }
        }
    }
}
