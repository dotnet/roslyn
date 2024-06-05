// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Progress;

namespace Microsoft.CodeAnalysis;

internal readonly struct CodeAnalysisProgressWrapper
{
    internal static readonly IProgress<CodeAnalysisProgressWrapper> None = NullProgress<CodeAnalysisProgressWrapper>.Instance;

    public static CodeAnalysisProgressWrapper Description(string description)
        => throw new NotImplementedException();

    public static CodeAnalysisProgressWrapper AddIncompleteItems(int count, string? description = null)
        => throw new NotImplementedException();

    public static CodeAnalysisProgressWrapper AddCompleteItems(int count, string? description = null)
        => throw new NotImplementedException();

    internal static CodeAnalysisProgressWrapper Clear()
        => throw new NotImplementedException();

#if !CODE_STYLE
    //public static implicit operator CodeAnalysisProgressWrapper(CodeAnalysisProgress instance)
    //    => throw new NotImplementedException();

    //public static implicit operator CodeAnalysisProgress(CodeAnalysisProgressWrapper wrapper)
    //    => throw new NotImplementedException();
#endif
}
