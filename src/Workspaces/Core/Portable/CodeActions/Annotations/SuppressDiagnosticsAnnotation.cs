// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal class SuppressDiagnosticsAnnotation
    {
        public const string Kind = "CodeAction_SuppressDiagnostics";

        public static SyntaxAnnotation Create()
            => new(Kind);
    }
}
