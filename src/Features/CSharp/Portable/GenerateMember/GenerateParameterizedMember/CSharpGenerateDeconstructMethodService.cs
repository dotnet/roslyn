// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateParameterizedMember;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateMethod
{
    [ExportLanguageService(typeof(IGenerateDeconstructMemberService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpGenerateDeconstructMethodService :
        AbstractGenerateDeconstructMethodService<CSharpGenerateDeconstructMethodService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpGenerateDeconstructMethodService()
        {
        }

        protected override bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
            => containingType.ContainingTypesOrSelfHasUnsafeKeyword();

        protected override AbstractInvocationInfo CreateInvocationMethodInfo(SemanticDocument document, AbstractGenerateParameterizedMemberService<CSharpGenerateDeconstructMethodService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>.State state)
            => new CSharpGenerateParameterizedMemberService<CSharpGenerateDeconstructMethodService>.InvocationExpressionInfo(document, state);

        protected override bool AreSpecialOptionsActive(SemanticModel semanticModel)
            => CSharpCommonGenerationServiceMethods.AreSpecialOptionsActive();

        protected override bool IsValidSymbol(ISymbol symbol, SemanticModel semanticModel)
            => CSharpCommonGenerationServiceMethods.IsValidSymbol();
    }
}
