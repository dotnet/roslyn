// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.PrivateAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpSuppressInternalExperimentalApiDiagnostics : AbstractSuppressInternalExperimentalApiDiagnostics
{
    protected override bool IsExposedLocation(SuppressionAnalysisContext context, SyntaxToken token, CancellationToken cancellationToken)
    {
        for (var node = token.Parent; node is not null; node = node.Parent)
        {
            if (node is not MemberDeclarationSyntax member)
                continue;

            if (member.Modifiers.Any(SyntaxKind.PrivateKeyword))
            {
                // This covers both 'private' and 'private protected'
                return false;
            }
            else if (member.Modifiers.Any(SyntaxKind.InternalKeyword) && !member.Modifiers.Any(SyntaxKind.ProtectedKeyword))
            {
                // This covers 'internal' but skips 'protected internal'
                return false;
            }
        }

        return true;
    }
}
