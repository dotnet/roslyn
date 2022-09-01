// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal abstract partial class AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
    {
        private partial class GenerateParameterizedMemberCodeAction : CodeAction
        {
            private readonly TService _service;
            private readonly Document _document;
            private readonly State _state;
            private readonly bool _isAbstract;
            private readonly bool _generateProperty;
            private readonly string _equivalenceKey;
            private readonly CodeAndImportGenerationOptionsProvider _fallbackOptions;

            public GenerateParameterizedMemberCodeAction(
                TService service,
                Document document,
                State state,
                CodeAndImportGenerationOptionsProvider fallbackOptions,
                bool isAbstract,
                bool generateProperty)
            {
                _service = service;
                _document = document;
                _state = state;
                _fallbackOptions = fallbackOptions;
                _isAbstract = isAbstract;
                _generateProperty = generateProperty;
                _equivalenceKey = Title;
            }

            private string GetDisplayText(
                State state,
                bool isAbstract,
                bool generateProperty)
            {
                switch (state.MethodGenerationKind)
                {
                    case MethodGenerationKind.Member:
                        var text = generateProperty ?
                            isAbstract ? FeaturesResources.Generate_abstract_property_0 : FeaturesResources.Generate_property_0 :
                            isAbstract ? FeaturesResources.Generate_abstract_method_0 : FeaturesResources.Generate_method_0;

                        var name = state.IdentifierToken.ValueText;
                        return string.Format(text, name);
                    case MethodGenerationKind.ImplicitConversion:
                        return _service.GetImplicitConversionDisplayText(_state);
                    case MethodGenerationKind.ExplicitConversion:
                        return _service.GetExplicitConversionDisplayText(_state);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(state.MethodGenerationKind);
                }
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var syntaxFactory = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language).GetService<SyntaxGenerator>();

                if (_generateProperty)
                {
                    var property = await _state.SignatureInfo.GeneratePropertyAsync(syntaxFactory, _isAbstract, _state.IsWrittenTo, cancellationToken).ConfigureAwait(false);

                    var result = await CodeGenerator.AddPropertyDeclarationAsync(
                        new CodeGenerationSolutionContext(
                            _document.Project.Solution,
                            new CodeGenerationContext(
                                afterThisLocation: _state.IdentifierToken.GetLocation(),
                                generateMethodBodies: _state.TypeToGenerateIn.TypeKind != TypeKind.Interface),
                            _fallbackOptions),
                        _state.TypeToGenerateIn,
                        property,
                        cancellationToken).ConfigureAwait(false);

                    return result;
                }
                else
                {
                    var method = await _state.SignatureInfo.GenerateMethodAsync(syntaxFactory, _isAbstract, cancellationToken).ConfigureAwait(false);

                    var result = await CodeGenerator.AddMethodDeclarationAsync(
                       new CodeGenerationSolutionContext(
                           _document.Project.Solution,
                           new CodeGenerationContext(
                               afterThisLocation: _state.Location,
                               generateMethodBodies: _state.TypeToGenerateIn.TypeKind != TypeKind.Interface),
                           _fallbackOptions),
                        _state.TypeToGenerateIn,
                        method,
                        cancellationToken)
                        .ConfigureAwait(false);

                    return result;
                }
            }

            public override string Title
            {
                get
                {
                    return GetDisplayText(_state, _isAbstract, _generateProperty);
                }
            }

            public override string EquivalenceKey => _equivalenceKey;
        }
    }
}
