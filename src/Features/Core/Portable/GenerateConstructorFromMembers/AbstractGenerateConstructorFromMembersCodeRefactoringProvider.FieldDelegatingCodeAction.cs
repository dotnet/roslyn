// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateConstructorFromMembers
{
    internal abstract partial class AbstractGenerateConstructorFromMembersCodeRefactoringProvider
    {
        private class FieldDelegatingCodeAction : CodeAction
        {
            private readonly AbstractGenerateConstructorFromMembersCodeRefactoringProvider _service;
            private readonly Document _document;
            private readonly State _state;
            private readonly bool _addNullChecks;

            public FieldDelegatingCodeAction(
                AbstractGenerateConstructorFromMembersCodeRefactoringProvider service,
                Document document,
                State state,
                bool addNullChecks)
            {
                _service = service;
                _document = document;
                _state = state;
                _addNullChecks = addNullChecks;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                // First, see if there are any constructors that would take the first 'n' arguments
                // we've provided.  If so, delegate to those, and then create a field for any
                // remaining arguments.  Try to match from largest to smallest.
                //
                // Otherwise, just generate a normal constructor that assigns any provided
                // parameters into fields.
                var parameterToExistingFieldMap = new Dictionary<string, ISymbol>();
                for (var i = 0; i < _state.Parameters.Length; i++)
                {
                    parameterToExistingFieldMap[_state.Parameters[i].Name] = _state.SelectedMembers[i];
                }

                var factory = _document.GetLanguageService<SyntaxGenerator>();

                var semanticModel = await _document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var syntaxTree = semanticModel.SyntaxTree;
                var options = await _document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var preferThrowExpression = _service.PrefersThrowExpression(options);

                var (fields, constructor) = factory.CreateFieldDelegatingConstructor(
                    semanticModel,
                    _state.ContainingType.Name,
                    _state.ContainingType,
                    _state.Parameters,
                    parameterToExistingFieldMap,
                    parameterToNewFieldMap: null,
                    addNullChecks: _addNullChecks,
                    preferThrowExpression: preferThrowExpression);

                // If the user has selected a set of members (i.e. TextSpan is not empty), then we will
                // choose the right location (i.e. null) to insert the constructor.  However, if they're 
                // just invoking the feature manually at a specific location, then we'll insert the 
                // members at that specific place in the class/struct.
                var afterThisLocation = _state.TextSpan.IsEmpty
                    ? syntaxTree.GetLocation(_state.TextSpan)
                    : null;

                var result = await CodeGenerator.AddMemberDeclarationsAsync(
                    _document.Project.Solution,
                    _state.ContainingType,
                    fields.Concat(constructor),
                    new CodeGenerationOptions(
                        contextLocation: syntaxTree.GetLocation(_state.TextSpan),
                        afterThisLocation: afterThisLocation),
                    cancellationToken).ConfigureAwait(false);

                return await AddNavigationAnnotationAsync(result, cancellationToken).ConfigureAwait(false);
            }

            public override string Title
            {
                get
                {
                    var symbolDisplayService = _document.GetLanguageService<ISymbolDisplayService>();
                    var parameters = _state.Parameters.Select(p => symbolDisplayService.ToDisplayString(p, SimpleFormat));
                    var parameterString = string.Join(", ", parameters);

                    if (_state.DelegatedConstructor == null)
                    {
                        return string.Format(FeaturesResources.Generate_constructor_0_1,
                            _state.ContainingType.Name, parameterString);
                    }
                    else
                    {
                        return string.Format(FeaturesResources.Generate_field_assigning_constructor_0_1,
                            _state.ContainingType.Name, parameterString);
                    }
                }
            }
        }
    }
}
