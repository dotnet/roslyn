// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class SourceNamedTypeSymbol
    {
        private StrongBox<ParameterSymbol?> _lazyExtensionParameter;

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

        internal ParameterSymbol? ExtensionParameter
        {
            get
            {
                if (_lazyExtensionParameter == null)
                {
                    var diagnostics = BindingDiagnosticBag.GetInstance();
                    var extensionParameter = makeExtensionParameter(this, diagnostics);
                    if (Interlocked.CompareExchange(ref _lazyExtensionParameter, new StrongBox<ParameterSymbol?>(extensionParameter), null) == null)
                    {
                        AddDeclarationDiagnostics(diagnostics);
                    }
                    diagnostics.Free();
                }

                return _lazyExtensionParameter.Value;

                static ParameterSymbol? makeExtensionParameter(SourceNamedTypeSymbol symbol, BindingDiagnosticBag diagnostics)
                {
                    var syntax = (ExtensionDeclarationSyntax)symbol.GetNonNullSyntaxNode();
                    var parameterList = syntax.ParameterList;
                    Debug.Assert(parameterList is not null);

                    int count = parameterList.Parameters.Count;
                    Debug.Assert(count > 0);

                    if (parameterList is null || count == 0)
                    {
                        return null;
                    }

                    BinderFactory binderFactory = symbol.DeclaringCompilation.GetBinderFactory(syntax.SyntaxTree);
                    var withTypeParamsBinder = binderFactory.GetBinder(parameterList);

                    // Constraints are checked later
                    var signatureBinder = withTypeParamsBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, symbol);

                    for (int parameterIndex = 1; parameterIndex < count; parameterIndex++)
                    {
                        diagnostics.Add(ErrorCode.ERR_ReceiverParameterOnlyOne, parameterList.Parameters[parameterIndex].GetLocation());
                    }

                    return ParameterHelpers.MakeExtensionReceiverParameter(withTypeParametersBinder: signatureBinder, owner: symbol, parameterList, diagnostics);
                }
            }
        }
    }
}
