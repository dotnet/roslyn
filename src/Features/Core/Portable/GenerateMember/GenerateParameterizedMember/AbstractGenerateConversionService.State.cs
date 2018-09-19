﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal partial class AbstractGenerateConversionService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
    {
        protected new class State : AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>.State
        {
            public static async Task<State> GenerateConversionStateAsync(
               TService service,
               SemanticDocument document,
               SyntaxNode interfaceNode,
               CancellationToken cancellationToken)
            {
                var state = new State();
                if (!await state.TryInitializeConversionAsync(service, document, interfaceNode, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }

                return state;
            }

            private Task<bool> TryInitializeConversionAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode node,
                CancellationToken cancellationToken)
            {
                if (service.IsImplicitConversionGeneration(node))
                {
                    if (!TryInitializeImplicitConversion(service, document, node, cancellationToken))
                    {
                        return SpecializedTasks.False;
                    }
                }
                else if (service.IsExplicitConversionGeneration(node))
                {
                    if (!TryInitializeExplicitConversion(service, document, node, cancellationToken))
                    {
                        return SpecializedTasks.False;
                    }
                }

                return TryFinishInitializingStateAsync(service, document, cancellationToken);
            }

            private bool TryInitializeExplicitConversion(TService service, SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
            {
                MethodKind = MethodKind.Conversion;
                if (!service.TryInitializeExplicitConversionState(
                    document, node, ClassInterfaceModuleStructTypes, cancellationToken,
                    out var identifierToken, out var methodSymbol, out var typeToGenerateIn))
                {
                    return false;
                }

                this.ContainingType = document.SemanticModel.GetEnclosingNamedType(node.SpanStart, cancellationToken);
                if (ContainingType == null)
                {
                    return false;
                }

                this.IdentifierToken = identifierToken;
                this.TypeToGenerateIn = typeToGenerateIn;
                this.SignatureInfo = new MethodSignatureInfo(document, this, methodSymbol);
                this.location = node.GetLocation();
                this.MethodGenerationKind = MethodGenerationKind.ExplicitConversion;
                return true;
            }

            private bool TryInitializeImplicitConversion(TService service, SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
            {
                MethodKind = MethodKind.Conversion;
                if (!service.TryInitializeImplicitConversionState(
                    document, node, ClassInterfaceModuleStructTypes, cancellationToken,
                    out var identifierToken, out var methodSymbol, out var typeToGenerateIn))
                {
                    return false;
                }

                this.ContainingType = document.SemanticModel.GetEnclosingNamedType(node.SpanStart, cancellationToken);
                if (ContainingType == null)
                {
                    return false;
                }

                this.IdentifierToken = identifierToken;
                this.TypeToGenerateIn = typeToGenerateIn;
                this.SignatureInfo = new MethodSignatureInfo(document, this, methodSymbol);
                this.MethodGenerationKind = MethodGenerationKind.ImplicitConversion;
                return true;
            }
        }
    }
}
