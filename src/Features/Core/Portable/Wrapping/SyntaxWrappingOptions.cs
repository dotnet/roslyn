// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.Wrapping
{
    internal abstract class SyntaxWrappingOptions
    {
        public readonly bool UseTabs;
        public readonly int TabSize;
        public readonly string NewLine;
        public readonly int WrappingColumn;
        public readonly OperatorPlacementWhenWrappingPreference OperatorPlacement;

        protected SyntaxWrappingOptions(
            bool useTabs,
            int tabSize,
            string newLine,
            int wrappingColumn,
            OperatorPlacementWhenWrappingPreference operatorPlacement)
        {
            UseTabs = useTabs;
            TabSize = tabSize;
            NewLine = newLine;
            WrappingColumn = wrappingColumn;
            OperatorPlacement = operatorPlacement;
        }
    }
}
