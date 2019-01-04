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
        AssertsTrue = 1 << 2,
        AssertsFalse = 1 << 3,
    }

    internal static class FlowAnalysisAnnotationsFacts
    {
        /// <summary>
        /// For EnsuresNotNull, you should set NotNullWhenTrue and NotNullWhenFalse.
        /// </summary>
        internal static FlowAnalysisAnnotations Create(bool notNullWhenTrue, bool notNullWhenFalse, bool assertsTrue, bool assertsFalse)
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

            if (assertsTrue)
            {
                value |= AssertsTrue;
            }

            if (assertsFalse)
            {
                value |= AssertsFalse;
            }

            return value;
        }
    }
}
