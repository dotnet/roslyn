// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal sealed class ConversionGroup
    {
        internal ConversionGroup(TypeSymbolWithAnnotations explicitType = null)
        {
            ExplicitType = explicitType;
        }

        internal readonly TypeSymbolWithAnnotations ExplicitType;

#if DEBUG
        private static int _nextId;
        private readonly int _id = _nextId++;

        internal string GetDebuggerDisplay()
        {
            return "#" + _id;
        }
#endif
    }
}
