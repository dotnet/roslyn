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
        private readonly struct Model : IEquatable<Model>
        {
            public readonly ImmutableArray<string> ErrorNames;

            public Model(ImmutableArray<string> errorNames)
            {
                ErrorNames = errorNames;
            }

            public override bool Equals(object? obj)
            {
                return obj is Model model && Equals(model);
            }

            public bool Equals(Model other)
            {
                return ErrorNames.SequenceEqual(other.ErrorNames);
            }

            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }

            public static bool operator ==(Model left, Model right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Model left, Model right)
            {
                return !(left == right);
            }
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (n, _) => n is EnumDeclarationSyntax enumDeclaration && enumDeclaration.Identifier.ValueText.Equals("ErrorCode", StringComparison.Ordinal),
                transform: (context, cancellationToken) => new Model(((INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node, cancellationToken)).GetMembers().OfType<IFieldSymbol>().Select(m => m.Name).ToImmutableArray()));

            context.RegisterSourceOutput(provider, (context, model) => context.AddSource("ErrorFacts.Generated.cs", GetOutputText(model.ErrorNames)));
        }
    }
}

#endif
