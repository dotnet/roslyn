// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers
{
    internal partial class AddConstructorParametersFromMembersCodeRefactoringProvider
    {
        private class AddConstructorParametersCodeAction : CodeAction
        {
            private readonly Document _document;
            private readonly ConstructorCandidate _constructorCandidate;
            private readonly ISymbol _containingType;
            private readonly ImmutableArray<IParameterSymbol> _parametersToAdd;

            /// <summary>
            /// If there is more than one constructor, the suggested actions will be split into two sub menus,  
            /// one for regular parameters and one for optional. This boolean is used by the Title property 
            /// to determine if the code action should be given the complete title or the sub menu title
            /// </summary>
            private readonly bool _useSubMenuName;

            public AddConstructorParametersCodeAction(
                Document document,
                ConstructorCandidate constructorCandidate,
                ISymbol containingType,
                ImmutableArray<IParameterSymbol> parametersToAdd,
                bool useSubMenuName)
            {
                _document = document;
                _constructorCandidate = constructorCandidate;
                _containingType = containingType;
                _parametersToAdd = parametersToAdd;
                _useSubMenuName = useSubMenuName;
            }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var workspace = _document.Project.Solution.Workspace;
                var declarationService = _document.GetLanguageService<ISymbolDeclarationService>();
                var constructor = declarationService.GetDeclarations(_constructorCandidate.Constructor).Select(r => r.GetSyntax(cancellationToken)).First();

                var newConstructor = constructor;
                newConstructor = CodeGenerator.AddParameterDeclarations(newConstructor, _parametersToAdd, workspace);
                newConstructor = CodeGenerator.AddStatements(newConstructor, CreateAssignStatements(_constructorCandidate), workspace)
                                                      .WithAdditionalAnnotations(Formatter.Annotation);

                var syntaxTree = constructor.SyntaxTree;
                var newRoot = syntaxTree.GetRoot(cancellationToken).ReplaceNode(constructor, newConstructor);

                return Task.FromResult(_document.WithSyntaxRoot(newRoot));
            }

            private IEnumerable<SyntaxNode> CreateAssignStatements(ConstructorCandidate constructorCandidate)
            {
                var factory = _document.GetLanguageService<SyntaxGenerator>();
                for (var i = 0; i < _parametersToAdd.Length; ++i)
                {
                    var memberName = constructorCandidate.MissingMembers[i].Name;
                    var parameterName = _parametersToAdd[i].Name;
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
                    var parameters = _constructorCandidate.Constructor.Parameters.Select(p => p.ToDisplayString(SimpleFormat));
                    var parameterString = string.Join(", ", parameters);
                    var signature = $"{_containingType}({parameterString})";
                    var submenu = _useSubMenuName;

                    if (submenu)
                    {
                        return string.Format(FeaturesResources.Add_to_0, signature);
                    }
                    else
                    {
                        return _parametersToAdd[0].IsOptional
                            ? string.Format(FeaturesResources.Add_optional_parameters_to_0, signature)
                            : string.Format(FeaturesResources.Add_parameters_to_0, signature);
                    }
                }
            }
        }
    }
}
