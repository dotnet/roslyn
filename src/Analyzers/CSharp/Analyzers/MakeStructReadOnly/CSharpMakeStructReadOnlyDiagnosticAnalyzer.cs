// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MakeStructReadOnly;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class CSharpMakeStructReadOnlyDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public CSharpMakeStructReadOnlyDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.MakeStructReadOnlyDiagnosticId,
               EnforceOnBuildValues.MakeStructReadOnly,
               CSharpCodeStyleOptions.PreferDeconstructedVariableDeclaration,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Make_struct_readonly), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Struct_can_be_made_readonly), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.StructDeclaration, SyntaxKind.RecordStructDeclaration);

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var option = context.GetCSharpAnalyzerOptions().PreferReadOnlyStruct;
        if (!option.Value)
            return;

        var cancellationToken = context.CancellationToken;
        var semanticModel = context.SemanticModel;
        if (semanticModel.Compilation.LanguageVersion() < LanguageVersion.CSharp7_2)
            return;

        // if it's already syntactically readonly, nothing we need to do.
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        if (typeDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
            return;

        // quickly check if there are mutable fields/properties in this declaration (note: there may be other parts,
        // so this is not a sufficient test on its own).  If so, we def can't make this readonly.
        foreach (var member in typeDeclaration.Members)
        {
            if (member is FieldDeclarationSyntax field && !field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
                return;

            if (member is PropertyDeclarationSyntax { AccessorList.Accessors: var accessors } &&
                accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) && a.SemicolonToken != default))
            {
                return;
            }
        }

        // Only bother showing this for structs that have at least one declared data member.  We don't want to
        // aggressively offer this on empty structs that the user is just about to add members to.
        if (!typeDeclaration.Members.Any(m => m is FieldDeclarationSyntax or PropertyDeclarationSyntax) &&
            typeDeclaration is not RecordDeclarationSyntax { ParameterList.Parameters.Count: > 0 })
        {
            return;
        }

        // Ok, syntactically this looks viable.  Now go to semantics to ensure this really can be made readonly.

        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
        Contract.ThrowIfNull(typeSymbol);

        // This may have been partial, with another part already marking this struct as readonly.  Check again and
        // bail if so.
        if (typeSymbol.IsReadOnly)
            return;

        // Now, ensure we have at least one field and that all fields are readonly.
        var fields = typeSymbol.GetMembers().OfType<IFieldSymbol>().ToImmutableArray();
        if (fields.Length == 0)
            return;

        if (fields.Any(f => !f.IsReadOnly))
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            typeDeclaration.Identifier.GetLocation(),
            option.Notification.Severity,
            additionalLocations: ImmutableArray.Create(typeDeclaration.GetLocation()),
            properties: null));
    }
}
