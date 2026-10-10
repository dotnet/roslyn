// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MakeTypeClosed;

namespace Microsoft.CodeAnalysis.CSharp.MakeTypeClosed;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeTypeClosed), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpMakeTypeClosedCodeFixProvider() : AbstractMakeTypeClosedCodeFixProvider<TypeDeclarationSyntax>
{
    public override ImmutableArray<string> FixableDiagnosticIds
           => ["CS8509"];

    protected override async Task<(bool, TypeDeclarationSyntax?)> GetRefactorContextValidityAndTypeAsync(
        SyntaxNode? node,
        SemanticModel semanticModel,
        Solution solution,
        CancellationToken cancellationToken)
    {
        TypeDeclarationSyntax? typeDeclaration = null;

        var switchExpression = node?.FirstAncestorOrSelf<SwitchExpressionSyntax>();
        if (switchExpression == null)
            return (false, typeDeclaration);

        var switchedOnType = semanticModel.GetTypeInfo(switchExpression.GoverningExpression, cancellationToken: cancellationToken).Type;

        if (switchedOnType is not INamedTypeSymbol namedType)
            return (false, typeDeclaration);

        if (namedType.TypeKind != TypeKind.Class)
            return (false, typeDeclaration);

        var declaration = namedType.DeclaringSyntaxReferences.FirstOrDefault();
        if (declaration == null)
            return (false, typeDeclaration);

        // Check if the user owns both projects, aka they are in their solution.
        if (solution.GetProject(namedType.ContainingAssembly, cancellationToken) == null
            || solution.GetProject(semanticModel.Compilation.Assembly, cancellationToken) == null)
        {
            return (false, typeDeclaration);
        }

        if (namedType.IsStatic)
            return (false, typeDeclaration);

        if (namedType.IsSealed)
            return (false, typeDeclaration);

        if (declaration.GetSyntax(cancellationToken) is not TypeDeclarationSyntax syntax)
            return (false, typeDeclaration);

        if (await HasExternalDerivedTypesAsync(namedType, solution, cancellationToken).ConfigureAwait(false))
            return (false, typeDeclaration);

        typeDeclaration = syntax;
        return (true, typeDeclaration);
    }
}
