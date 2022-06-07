// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

                ContainingType = document.SemanticModel.GetEnclosingNamedType(node.SpanStart, cancellationToken);
                if (ContainingType == null)
                {
                    return false;
                }

                IdentifierToken = identifierToken;
                TypeToGenerateIn = typeToGenerateIn;
                SignatureInfo = new MethodSignatureInfo(document, this, methodSymbol);
                location = node.GetLocation();
                MethodGenerationKind = MethodGenerationKind.ExplicitConversion;
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

                ContainingType = document.SemanticModel.GetEnclosingNamedType(node.SpanStart, cancellationToken);
                if (ContainingType == null)
                {
                    return false;
                }

                IdentifierToken = identifierToken;
                TypeToGenerateIn = typeToGenerateIn;
                SignatureInfo = new MethodSignatureInfo(document, this, methodSymbol);
                MethodGenerationKind = MethodGenerationKind.ImplicitConversion;
                return true;
            }
        }
    }
}
