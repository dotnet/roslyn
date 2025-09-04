// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyPropertyAccessor;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpSimplifyPropertyAccessorDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public CSharpSimplifyPropertyAccessorDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.SimplifyPropertyAccessorDiagnosticId,
               EnforceOnBuildValues.SimplifyPropertyAccessor,
               CSharpCodeStyleOptions.PreferSimplePropertyAccessors,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Simplify_property_accessor), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Property_accessor_can_be_simplified), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);

    private void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
    {
        var option = context.GetCSharpAnalyzerOptions().PreferSimplePropertyAccessors;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

        if (propertyDeclaration.AccessorList is not { } accessorList ||
            accessorList.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return;
        }

        foreach (var accessor in accessorList.Accessors)
        {
            // get { return field; }
            // get => field;
            if (accessor is (SyntaxKind.GetAccessorDeclaration) { Body.Statements: [ReturnStatementSyntax { Expression.RawKind: (int)SyntaxKind.FieldExpression }] }
                         or (SyntaxKind.GetAccessorDeclaration) { ExpressionBody.Expression.RawKind: (int)SyntaxKind.FieldExpression })
            {
                ReportIfValid(accessor);
            }

            // set/init { field = value; }
            if (accessor is (SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration) { Body.Statements: [ExpressionStatementSyntax { Expression: var innerBlockBodyExpression }] } &&
                IsFieldValueAssignmentExpression(innerBlockBodyExpression))
            {
                ReportIfValid(accessor);
            }

            // set/init => field = value;
            if (accessor is (SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration) { ExpressionBody.Expression: var innerExpressionBodyExpression } &&
                IsFieldValueAssignmentExpression(innerExpressionBodyExpression))
            {
                ReportIfValid(accessor);
            }
        }

        static bool IsFieldValueAssignmentExpression(ExpressionSyntax expression)
        {
            return expression is AssignmentExpressionSyntax(SyntaxKind.SimpleAssignmentExpression)
            {
                Left.RawKind: (int)SyntaxKind.FieldExpression,
                Right: IdentifierNameSyntax { Identifier.ValueText: "value" }
            };
        }

        void ReportIfValid(AccessorDeclarationSyntax accessorDeclaration)
        {
            // If we are analyzing an accessor of a partial property and all other accessors have no bodies
            // then if we simplify our current accessor the property will no longer be a valid
            // implementation part. Thus we block that case
            if (accessorDeclaration is { Parent: AccessorListSyntax { Parent: BasePropertyDeclarationSyntax containingPropertyDeclaration } containingAccessorList } &&
                containingPropertyDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword) &&
                containingAccessorList.Accessors.All(a => ReferenceEquals(a, accessorDeclaration) || a is { Body: null, ExpressionBody: null }))
            {
                return;
            }

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                accessorDeclaration.GetLocation(),
                option.Notification,
                context.Options,
                additionalLocations: null,
                properties: null));
        }
    }
}
