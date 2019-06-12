// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Completion;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Completion
{
    internal static class FSharpCommonCompletionProvider
    {
        public static CompletionProvider Create(IFSharpCommonCompletionProvider fsharpCommonCompletionProvider)
        {
            return new FSharpInternalCommonCompletionProvider(fsharpCommonCompletionProvider);
        }
    }
}
