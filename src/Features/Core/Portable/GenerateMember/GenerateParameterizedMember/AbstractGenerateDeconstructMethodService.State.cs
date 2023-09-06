// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal partial class AbstractGenerateDeconstructMethodService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
    {
        internal new class State :
            AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>.State
        {
            /// <summary>
            /// Make a State instance representing the Deconstruct method we want to generate.
            /// The method will be called "Deconstruct". It will be a member of `typeToGenerateIn`.
            /// Its arguments will be based on `targetVariables`.
            /// </summary>
            public static async Task<State> GenerateDeconstructMethodStateAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode targetVariables,
                INamedTypeSymbol typeToGenerateIn,
                CancellationToken cancellationToken)
            {
                var state = new State();
                if (!await state.TryInitializeMethodAsync(service, document, targetVariables, typeToGenerateIn, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }

                return state;
            }

            private async Task<bool> TryInitializeMethodAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode targetVariables,
                INamedTypeSymbol typeToGenerateIn,
                CancellationToken cancellationToken)
            {
                TypeToGenerateIn = typeToGenerateIn;
                IsStatic = false;
                var generator = SyntaxGenerator.GetGenerator(document.Document);
                IdentifierToken = generator.Identifier(WellKnownMemberNames.DeconstructMethodName);
                MethodGenerationKind = MethodGenerationKind.Member;
                MethodKind = MethodKind.Ordinary;

                cancellationToken.ThrowIfCancellationRequested();

                var semanticModel = document.SemanticModel;
                ContainingType = semanticModel.GetEnclosingNamedType(targetVariables.SpanStart, cancellationToken);
                if (ContainingType == null)
                {
                    return false;
                }

                var parameters = service.TryMakeParameters(semanticModel, targetVariables, cancellationToken);
                if (parameters.IsDefault)
                {
                    return false;
                }

                var methodSymbol = CodeGenerationSymbolFactory.CreateMethodSymbol(
                    attributes: default,
                    accessibility: default,
                    modifiers: default,
                    returnType: semanticModel.Compilation.GetSpecialType(SpecialType.System_Void),
                    refKind: RefKind.None,
                    explicitInterfaceImplementations: default,
                    name: null,
                    typeParameters: default,
                    parameters);

                SignatureInfo = new MethodSignatureInfo(document, this, methodSymbol);

                return await TryFinishInitializingStateAsync(service, document, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
