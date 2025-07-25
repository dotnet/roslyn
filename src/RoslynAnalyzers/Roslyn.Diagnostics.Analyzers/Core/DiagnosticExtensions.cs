// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;

namespace Roslyn.Diagnostics.Analyzers
{
    public static class DiagnosticExtensions
    {
        extension(SyntaxReference syntaxReference)
        {
            public Diagnostic CreateDiagnostic(
            DiagnosticDescriptor rule,
            CancellationToken cancellationToken,
            params object[] args)
            => syntaxReference.GetSyntax(cancellationToken).CreateDiagnostic(rule, args);

            public Diagnostic CreateDiagnostic(
                DiagnosticDescriptor rule,
                ImmutableDictionary<string, string?>? properties,
                CancellationToken cancellationToken,
                params object[] args)
                => syntaxReference.GetSyntax(cancellationToken).CreateDiagnostic(rule, properties, args);
        }

        extension(IEnumerable<SyntaxReference> syntaxReferences)
        {
            public Diagnostic CreateDiagnostic(
            DiagnosticDescriptor rule,
            ImmutableDictionary<string, string?>? properties,
            CancellationToken cancellationToken,
            params object[] args)
            => syntaxReferences.Select(s => s.GetSyntax(cancellationToken).GetLocation()).CreateDiagnostic(rule, properties, args);
        }
    }
}
