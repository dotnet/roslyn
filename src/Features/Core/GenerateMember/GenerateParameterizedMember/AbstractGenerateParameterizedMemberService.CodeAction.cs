// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal abstract partial class AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
    {
        private partial class GenerateParameterizedMemberCodeAction : CodeAction
        {
            private readonly TService service;
            private readonly Document document;
            private readonly State state;
            private readonly bool isAbstract;
            private readonly bool generateProperty;

            public GenerateParameterizedMemberCodeAction(
                TService service,
                Document document,
                State state,
                bool isAbstract,
                bool generateProperty)
            {
                this.service = service;
                this.document = document;
                this.state = state;
                this.isAbstract = isAbstract;
                this.generateProperty = generateProperty;
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
                        isAbstract ? FeaturesResources.GenerateAbstractProperty : FeaturesResources.GeneratePropertyIn :
                        isAbstract ? FeaturesResources.GenerateAbstractMethod : FeaturesResources.GenerateMethodIn;

                    var name = state.IdentifierToken.ValueText;
                    var destination = state.TypeToGenerateIn.Name;
                    return string.Format(text, name, destination);
                case MethodGenerationKind.ImplicitConversion:
                    return this.service.GetImplicitConversionDisplayText(this.state);
                case MethodGenerationKind.ExplicitConversion:
                    return this.service.GetExplicitConversionDisplayText(this.state);
                default:
                    throw ExceptionUtilities.Unreachable;
                }
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var syntaxFactory = document.Project.Solution.Workspace.Services.GetLanguageServices(state.TypeToGenerateIn.Language).GetService<SyntaxGenerator>();

                if (generateProperty)
                {
                    var property = state.SignatureInfo.GenerateProperty(syntaxFactory, isAbstract, state.IsWrittenTo, cancellationToken);

                    var result = await CodeGenerator.AddPropertyDeclarationAsync(
                        this.document.Project.Solution,
                        state.TypeToGenerateIn,
                        property,
                        new CodeGenerationOptions(afterThisLocation: state.IdentifierToken.GetLocation()),
                        cancellationToken)
                        .ConfigureAwait(false);

                    return result;
                }
                else
                {
                    var method = state.SignatureInfo.GenerateMethod(syntaxFactory, isAbstract, cancellationToken);

                    var result = await CodeGenerator.AddMethodDeclarationAsync(
                        this.document.Project.Solution,
                        state.TypeToGenerateIn,
                        method,
                        new CodeGenerationOptions(afterThisLocation: state.Location),
                        cancellationToken)
                        .ConfigureAwait(false);

                    return result;
                }
            }

            public override string Title
            {
                get
                {
                    return GetDisplayText(state, isAbstract, generateProperty);
                }
            }
        }
    }
}
