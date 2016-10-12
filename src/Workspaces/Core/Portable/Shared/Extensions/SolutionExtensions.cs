// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SolutionExtensions
    {
        public static SourceCodeKind GetSourceCodeKind(this TextDocumentState state)
        {
            // these can just be abstract property
            return (state as DocumentState)?.SourceCodeKind ?? SourceCodeKind.Regular;
        }

        public static bool IsGenerated(this TextDocumentState state)
        {
            // these can just be abstract property
            return (state as DocumentState)?.IsGenerated ?? false;
        }
    }
}
