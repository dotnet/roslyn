// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

internal readonly struct FixAllContextWrapper
{
    public IProgress<CodeAnalysisProgressWrapper> Progress
        => throw new NotImplementedException();

#if !CODE_STYLE
    public static implicit operator FixAllContextWrapper(FixAllContext context)
        => throw new NotImplementedException();

    public static implicit operator FixAllContext(FixAllContextWrapper wrapper)
        => throw new NotImplementedException();
#endif
}
