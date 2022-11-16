// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// We only build the Source Generator in the netstandard target
#if NETSTANDARD

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Internal.CSharpErrorFactsGenerator
{
    [Generator]
    public sealed partial class ErrorGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (n, _) => n is EnumDeclarationSyntax enumDeclaration && enumDeclaration.Identifier.ValueText.Equals("ErrorCode", StringComparison.Ordinal),
                transform: (context, cancellationToken) => ((INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken)).GetMembers().OfType<IFieldSymbol>().Select(m => m.Name).ToImmutableArray());

            context.RegisterSourceOutput(
                provider.SelectMany((errorNames, _) => errorNames).Collect(),
                (context, errorNames) => context.AddSource("ErrorFacts.Generated.cs", GetOutputText(errorNames)));
        }
    }
}

#endif
