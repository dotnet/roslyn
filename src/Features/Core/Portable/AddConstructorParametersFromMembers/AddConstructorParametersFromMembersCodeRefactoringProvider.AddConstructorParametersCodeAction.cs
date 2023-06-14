// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
{
    internal partial class AddConstructorParametersFromMembersCodeRefactoringProvider
    {
        /// <param name="useSubMenuName">
        /// If there is more than one constructor, the suggested actions will be split into two sub menus,
        /// one for regular parameters and one for optional. This boolean is used by the Title property
        /// to determine if the code action should be given the complete title or the sub menu title
        /// </param>
        private class AddConstructorParametersCodeAction(
            Document document,
            CodeGenerationContextInfo info,
            ConstructorCandidate constructorCandidate,
            ISymbol containingType,
            ImmutableArray<IParameterSymbol> missingParameters,
            bool useSubMenuName) : CodeAction
        {
            protected override Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                var services = document.Project.Solution.Services;
                var declarationService = document.GetRequiredLanguageService<ISymbolDeclarationService>();
                var constructor = declarationService.GetDeclarations(
                    constructorCandidate.Constructor).Select(r => r.GetSyntax(cancellationToken)).First();

                var codeGenerator = document.GetRequiredLanguageService<ICodeGenerationService>();

                var newConstructor = constructor;
                newConstructor = codeGenerator.AddParameters(newConstructor, missingParameters, info, cancellationToken);
                newConstructor = codeGenerator.AddStatements(newConstructor, CreateAssignStatements(constructorCandidate), info, cancellationToken)
                                                      .WithAdditionalAnnotations(Formatter.Annotation);

                var syntaxTree = constructor.SyntaxTree;
                var newRoot = syntaxTree.GetRoot(cancellationToken).ReplaceNode(constructor, newConstructor);

                // Make sure we get the document that contains the constructor we just updated
                var constructorDocument = document.Project.GetDocument(syntaxTree);
                Contract.ThrowIfNull(constructorDocument);

                return Task.FromResult<Solution?>(constructorDocument.WithSyntaxRoot(newRoot).Project.Solution);
            }

            private IEnumerable<SyntaxNode> CreateAssignStatements(ConstructorCandidate constructorCandidate)
            {
                var factory = document.GetRequiredLanguageService<SyntaxGenerator>();
                for (var i = 0; i < missingParameters.Length; ++i)
                {
                    var memberName = constructorCandidate.MissingMembers[i].Name;
                    var parameterName = missingParameters[i].Name;
                    yield return factory.ExpressionStatement(
                        factory.AssignmentStatement(
                            factory.MemberAccessExpression(factory.ThisExpression(), factory.IdentifierName(memberName)),
                            factory.IdentifierName(parameterName)));
                }
            }

            public override string Title
            {
                get
                {
                    var parameters = constructorCandidate.Constructor.Parameters.Select(p => p.ToDisplayString(SimpleFormat));
                    var parameterString = string.Join(", ", parameters);
                    var signature = $"{containingType.Name}({parameterString})";

                    if (useSubMenuName)
                    {
                        return string.Format(CodeFixesResources.Add_to_0, signature);
                    }
                    else
                    {
                        return missingParameters[0].IsOptional
                            ? string.Format(FeaturesResources.Add_optional_parameters_to_0, signature)
                            : string.Format(FeaturesResources.Add_parameters_to_0, signature);
                    }
                }
            }

            /// <summary>
            /// A metadata name used by telemetry to distinguish between the different kinds of this code action.
            /// This code action will perform 2 different actions depending on if missing parameters can be optional.
            /// 
            /// In this case we don't want to use the title as it depends on the class name for the ctor.
            /// </summary>
            internal string ActionName => missingParameters[0].IsOptional
                ? nameof(FeaturesResources.Add_optional_parameters_to_0)
                : nameof(FeaturesResources.Add_parameters_to_0);
        }
    }
}
