// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.ExtractMethod;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

internal sealed class CSharpSyntaxTriviaService : AbstractSyntaxTriviaService
{
    public static readonly CSharpSyntaxTriviaService Instance = new();

    private CSharpSyntaxTriviaService()
        : base((int)SyntaxKind.EndOfLineTrivia)
    {
    }
}
