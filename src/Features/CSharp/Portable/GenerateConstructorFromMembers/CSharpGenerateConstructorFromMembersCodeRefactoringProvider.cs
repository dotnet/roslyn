// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.GenerateConstructorFromMembers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PickMembers;

namespace Microsoft.CodeAnalysis.CSharp.GenerateConstructorFromMembers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers), Shared]
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers)]
    internal sealed class CSharpGenerateConstructorFromMembersCodeRefactoringProvider
        : AbstractGenerateConstructorFromMembersCodeRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpGenerateConstructorFromMembersCodeRefactoringProvider()
        {
        }

        /// <summary>
        /// For testing purposes only.
        /// </summary>
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Used incorrectly by tests")]
        internal CSharpGenerateConstructorFromMembersCodeRefactoringProvider(IPickMembersService pickMembersService_forTesting)
            : base(pickMembersService_forTesting)
        {
        }

        protected override bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
            => containingType.ContainingTypesOrSelfHasUnsafeKeyword();

        protected override string ToDisplayString(IParameterSymbol parameter, SymbolDisplayFormat format)
            => SymbolDisplay.ToDisplayString(parameter, format);

        protected override bool PrefersThrowExpression(DocumentOptionSet options)
            => options.GetOption(CSharpCodeStyleOptions.PreferThrowExpression).Value;
    }
}
