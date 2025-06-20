// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.MoveToResx;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class String2ResAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "String2Res";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(CSharpFeaturesResources.Move_to_Resx), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(CSharpFeaturesResources.Use_Resx_for_user_facing_strings), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register to analyze string literals
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
    }

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
    {
        var stringLiteral = (LiteralExpressionSyntax)context.Node;

        // Report a diagnostic for every string literal
        var diagnostic = Diagnostic.Create(Rule, stringLiteral.GetLocation(), stringLiteral.Token.ValueText);
        context.ReportDiagnostic(diagnostic);
    }
}
