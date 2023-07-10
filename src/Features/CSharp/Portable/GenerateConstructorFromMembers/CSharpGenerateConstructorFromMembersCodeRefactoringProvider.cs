// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.GenerateConstructorFromMembers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.GenerateConstructorFromMembers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers), Shared]
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers)]
    [IntentProvider(WellKnownIntents.GenerateConstructor, LanguageNames.CSharp)]
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

        protected override async ValueTask<bool> PrefersThrowExpressionAsync(Document document, SimplifierOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var options = (CSharpSimplifierOptions)await document.GetSimplifierOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            return options.PreferThrowExpression.Value;
        }

        protected override IFieldSymbol? TryMapToWritableInstanceField(IPropertySymbol property, CancellationToken cancellationToken)
        {
            var containingType = property.ContainingType;
            if (property.DeclaringSyntaxReferences.Length == 0)
                return null;

            if (property.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) is not PropertyDeclarationSyntax propertyDeclaration)
                return null;

            var getAccessor = propertyDeclaration.AccessorList?.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
            var body = propertyDeclaration.ExpressionBody ?? getAccessor?.ExpressionBody ?? (SyntaxNode?)getAccessor?.Body;

            var accessedMemberName = GetAccessedMemberName(body);
            if (accessedMemberName is null)
                return null;

            return property.ContainingType.GetMembers(accessedMemberName).FirstOrDefault() as IFieldSymbol;
        }

        private static string? GetAccessedMemberName(SyntaxNode? body)
        {
            // Finally found a name.
            if (body is IdentifierNameSyntax identifierName)
                return identifierName.Identifier.ValueText;

            // `this.name`, recurse into `name`
            if (body is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } memberAccessExpress)
                return GetAccessedMemberName(memberAccessExpress.Name);

            // `return this.name;`
            if (body is ReturnStatementSyntax returnStatement)
                return GetAccessedMemberName(returnStatement.Expression);

            // `=> this.name;`
            if (body is ArrowExpressionClauseSyntax arrowExpression)
                return GetAccessedMemberName(arrowExpression.Expression);

            // { return this.name; }
            if (body is BlockSyntax { Statements.Count: > 0 } block)
                return GetAccessedMemberName(block.Statements.First());

            return null;
        }
    }
}
