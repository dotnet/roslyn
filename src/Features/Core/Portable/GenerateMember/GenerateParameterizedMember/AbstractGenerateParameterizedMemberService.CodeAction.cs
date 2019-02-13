// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            public GenerateParameterizedMemberCodeAction(
                TService service,
                Document document,
                State state,
                bool isAbstract,
                bool generateProperty)
            {
                _service = service;
                _document = document;
                _state = state;
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
                            isAbstract ? FeaturesResources.Generate_abstract_property_1_0 : FeaturesResources.Generate_property_1_0 :
                            isAbstract ? FeaturesResources.Generate_abstract_method_1_0 : FeaturesResources.Generate_method_1_0;

                        var name = state.IdentifierToken.ValueText;
                        var destination = state.TypeToGenerateIn.Name;
                        return string.Format(text, name, destination);
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
                var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var syntaxFactory = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language).GetService<SyntaxGenerator>();

                if (_generateProperty)
                {
                    var property = _state.SignatureInfo.GenerateProperty(syntaxFactory, _isAbstract, _state.IsWrittenTo, cancellationToken);

                    var result = await CodeGenerator.AddPropertyDeclarationAsync(
                        _document.Project.Solution,
                        _state.TypeToGenerateIn,
                        property,
                        new CodeGenerationOptions(
                            afterThisLocation: _state.IdentifierToken.GetLocation(),
                            generateMethodBodies: _state.TypeToGenerateIn.TypeKind != TypeKind.Interface),
                        cancellationToken)
                        .ConfigureAwait(false);

                    return result;
                }
                else
                {
                    var method = _state.SignatureInfo.GenerateMethod(syntaxFactory, _isAbstract, cancellationToken);

                    var result = await CodeGenerator.AddMethodDeclarationAsync(
                        _document.Project.Solution,
                        _state.TypeToGenerateIn,
                        method,
                        new CodeGenerationOptions(
                            afterThisLocation: _state.Location,
                            generateMethodBodies: _state.TypeToGenerateIn.TypeKind != TypeKind.Interface,
                            parseOptions: syntaxTree.Options),
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
