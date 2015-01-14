// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class SyntaxTriviaFunctions
    {
        internal static readonly Func<SyntaxTrivia, bool> Any = t => true;
        internal static readonly Func<SyntaxTrivia, bool> Skipped = t => t.Kind() == SyntaxKind.SkippedTokensTrivia;
    }
}
