// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using static Microsoft.CodeAnalysis.CSharp.Symbols.FlowAnalysisAnnotations;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    [Flags]
    internal enum FlowAnalysisAnnotations
    {
        None = 0,
        NotNullWhenTrue = 1 << 0,
        NotNullWhenFalse = 1 << 1,
        EnsuresNotNull = NotNullWhenTrue | NotNullWhenFalse,
        EnsuresTrue = 1 << 2,
        EnsuresFalse = 1 << 3,
    }

    internal static class FlowAnalysisAnnotationsFacts
    {
        // For EnsuresNotNull, you should set NotNullWhenTrue and NotNullWhenFalse
        internal static FlowAnalysisAnnotations Create(bool notNullWhenTrue, bool notNullWhenFalse, bool ensuresTrue, bool ensuresFalse)
        {
            FlowAnalysisAnnotations value = None;
            if (notNullWhenFalse)
            {
                value |= NotNullWhenFalse;
            }

            if (notNullWhenTrue)
            {
                value |= NotNullWhenTrue;
            }

            if (ensuresTrue)
            {
                value |= EnsuresTrue;
            }

            if (ensuresFalse)
            {
                value |= EnsuresFalse;
            }

            return value;
        }
    }
}
