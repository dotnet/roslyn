// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers;

internal sealed partial class AddConstructorParametersFromMembersCodeRefactoringProvider
{
    private sealed class AddConstructorParametersCodeAction(
        Document document,
        CodeGenerationContextInfo info,
        ConstructorCandidate constructorCandidate,
        ISymbol containingType,
        ImmutableArray<IParameterSymbol> missingParameters,
        bool useSubMenuName) : CodeAction
    {
        private readonly Document _document = document;
        private readonly CodeGenerationContextInfo _info = info;
        private readonly ConstructorCandidate _constructorCandidate = constructorCandidate;
        private readonly ISymbol _containingType = containingType;
        private readonly ImmutableArray<IParameterSymbol> _missingParameters = missingParameters;

        /// <summary>
        /// If there is more than one constructor, the suggested actions will be split into two sub menus,
        /// one for regular parameters and one for optional. This boolean is used by the Title property
        /// to determine if the code action should be given the complete title or the sub menu title
        /// </summary>
        private readonly bool _useSubMenuName = useSubMenuName;

        protected override Task<Solution?> GetChangedSolutionAsync(
            IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
        {
            var services = _document.Project.Solution.Services;
            var declarationService = _document.GetRequiredLanguageService<ISymbolDeclarationService>();
            var constructor = declarationService.GetDeclarations(
                _constructorCandidate.Constructor).Select(r => r.GetSyntax(cancellationToken)).First();

            var codeGenerator = _document.GetRequiredLanguageService<ICodeGenerationService>();

            var newConstructor = constructor;
            newConstructor = codeGenerator.AddParameters(newConstructor, _missingParameters, _info, cancellationToken);
            newConstructor = codeGenerator.AddStatements(newConstructor, CreateAssignStatements(_constructorCandidate), _info, cancellationToken)
                                                  .WithAdditionalAnnotations(Formatter.Annotation);

            var syntaxTree = constructor.SyntaxTree;
            var newRoot = syntaxTree.GetRoot(cancellationToken).ReplaceNode(constructor, newConstructor);

            // Make sure we get the document that contains the constructor we just updated
            var constructorDocument = _document.Project.GetDocument(syntaxTree);
            Contract.ThrowIfNull(constructorDocument);

            return Task.FromResult<Solution?>(constructorDocument.WithSyntaxRoot(newRoot).Project.Solution);
        }

        private IEnumerable<SyntaxNode> CreateAssignStatements(ConstructorCandidate constructorCandidate)
        {
            var factory = _document.GetRequiredLanguageService<SyntaxGenerator>();
            for (var i = 0; i < _missingParameters.Length; ++i)
            {
                var memberName = constructorCandidate.MissingMembers[i].Name;
                var parameterName = _missingParameters[i].Name;
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
                var signature = $"{_containingType.Name}({parameterString})";

                if (_useSubMenuName)
                {
                    return string.Format(CodeFixesResources.Add_to_0, signature);
                }
                else
                {
                    return _missingParameters[0].IsOptional
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
        internal string ActionName => _missingParameters[0].IsOptional
            ? nameof(FeaturesResources.Add_optional_parameters_to_0)
            : nameof(FeaturesResources.Add_parameters_to_0);
    }
}
