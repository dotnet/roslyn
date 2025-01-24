// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceNamedTypeSymbol
    {
        private ImmutableArray<ParameterSymbol> _lazyExtensionParameters;

        internal override string ExtensionName
        {
            get
            {
                if (!IsExtension)
                {
                    throw ExceptionUtilities.Unreachable();
                }

                MergedNamespaceOrTypeDeclaration declaration;
                if (ContainingType is not null)
                {
                    declaration = ((SourceNamedTypeSymbol)this.ContainingType).declaration;
                }
                else
                {
                    declaration = ((SourceNamespaceSymbol)this.ContainingSymbol).MergedDeclaration;
                }

                var index = declaration.Children.IndexOf(this.declaration);
                return GeneratedNames.MakeExtensionName(index);
            }
        }

        internal ImmutableArray<ParameterSymbol> ExtensionParameters
        {
            get
            {
                if (_lazyExtensionParameters.IsDefault)
                {
                    var diagnostics = BindingDiagnosticBag.GetInstance();
                    var extensionParameters = makeExtensionParameters(this, diagnostics);
                    if (ImmutableInterlocked.InterlockedCompareExchange(ref _lazyExtensionParameters, extensionParameters, default(ImmutableArray<ParameterSymbol>)).IsDefault)
                    {
                        AddDeclarationDiagnostics(diagnostics);
                    }
                    diagnostics.Free();
                }

                return _lazyExtensionParameters;

                static ImmutableArray<ParameterSymbol> makeExtensionParameters(SourceNamedTypeSymbol symbol, BindingDiagnosticBag diagnostics)
                {
                    var syntax = (ExtensionDeclarationSyntax)symbol.GetNonNullSyntaxNode();
                    var parameterList = syntax.ParameterList;

                    BinderFactory binderFactory = symbol.DeclaringCompilation.GetBinderFactory(syntax.SyntaxTree);
                    var withTypeParamsBinder = binderFactory.GetBinder(parameterList);

                    // Constraints are checked later
                    var signatureBinder = withTypeParamsBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, symbol);

                    ImmutableArray<ParameterSymbol> parameters = ParameterHelpers.MakeParameters(
                        withTypeParametersBinder: signatureBinder,
                        owner: symbol,
                        syntax.ParameterList,
                        arglistToken: out _,
                        allowRefOrOut: true,
                        allowThis: false,
                        addRefReadOnlyModifier: false,
                        diagnostics: diagnostics).Cast<SourceParameterSymbol, ParameterSymbol>();

                    return parameters;
                }
            }
        }
    }
}
