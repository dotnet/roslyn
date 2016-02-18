// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Analyzers.FixAnalyzers;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.FixAnalyzers
{
    /// <summary>
    /// A <see cref="CodeFixProvider"/> that intends to support fix all occurrences must classify the registered code actions into equivalence classes by assigning it an explicit, non-null equivalence key which is unique across all registered code actions by this fixer.
    /// This enables the <see cref="FixAllProvider"/> to fix all diagnostics in the required scope by applying code actions from this fixer that are in the equivalence class of the trigger code action.
    /// This analyzer catches violations of this requirement in the code actions registered by a fixer that supports <see cref="FixAllProvider"/>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpFixerWithFixAllAnalyzer : FixerWithFixAllAnalyzer<SyntaxKind>
    {
        protected override CompilationAnalyzer GetCompilationAnalyzer(INamedTypeSymbol codeFixProviderSymbol, IMethodSymbol getFixAllProvider, INamedTypeSymbol codeActionSymbol, ImmutableHashSet<IMethodSymbol> createMethods, IPropertySymbol equivalenceKeyProperty)
        {
            return new CSharpCompilationAnalyzer(codeFixProviderSymbol, getFixAllProvider, codeActionSymbol, createMethods, equivalenceKeyProperty);
        }

        private sealed class CSharpCompilationAnalyzer : CompilationAnalyzer
        {
            public CSharpCompilationAnalyzer(
                INamedTypeSymbol codeFixProviderSymbol,
                IMethodSymbol getFixAllProvider,
                INamedTypeSymbol codeActionSymbol,
                ImmutableHashSet<IMethodSymbol> createMethods,
                IPropertySymbol equivalenceKeyProperty)
                : base(codeFixProviderSymbol, getFixAllProvider, codeActionSymbol, createMethods, equivalenceKeyProperty)
            {
            }

            protected override SyntaxKind GetInvocationKind => SyntaxKind.InvocationExpression;
            protected override SyntaxKind GetObjectCreationKind => SyntaxKind.ObjectCreationExpression;

            protected override bool HasNonNullArgumentForParameter(SyntaxNode node, IParameterSymbol parameter, int indexOfParameter, SemanticModel model, CancellationToken cancellationToken)
            {
                var invocation = (InvocationExpressionSyntax)node;
                if (invocation.ArgumentList == null)
                {
                    return false;
                }

                var seenNamedArgument = false;
                var indexOfArgument = 0;
                foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
                {
                    if (argument.NameColon != null)
                    {
                        seenNamedArgument = true;
                        if (parameter.Name.Equals(argument.NameColon.Name.Identifier.ValueText))
                        {
                            return !HasNullConstantValue(argument.Expression, model, cancellationToken);
                        }
                    }
                    else if (!seenNamedArgument)
                    {
                        if (indexOfArgument == indexOfParameter)
                        {
                            return !HasNullConstantValue(argument.Expression, model, cancellationToken);
                        }

                        indexOfArgument++;
                    }
                }

                return false;
            }
        }
    }
}
