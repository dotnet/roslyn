// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Completion;

#if Unified_ExternalAccess
namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Completion;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion;
#endif

internal static class FSharpCommonCompletionProvider
{
    public static CompletionProvider Create(FSharpCommonCompletionProviderBase fsharpCommonCompletionProvider)
        => new FSharpInternalCommonCompletionProvider(fsharpCommonCompletionProvider);
}
