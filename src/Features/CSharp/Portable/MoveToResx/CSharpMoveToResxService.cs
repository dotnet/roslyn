// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.MoveToResx;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class MoveToResx : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MoveToResx";

    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(CSharpFeaturesResources.Move_to_Resx), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(CSharpFeaturesResources.Use_Resx_for_user_facing_strings), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources));
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor s_moveToResxRule = new DiagnosticDescriptor(
        DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_moveToResxRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeStringLiteral, SyntaxKind.StringLiteralExpression);
    }

    private static void AnalyzeStringLiteral(SyntaxNodeAnalysisContext context)
    {
        var stringLiteral = (LiteralExpressionSyntax)context.Node;
        var valueText = stringLiteral.Token.ValueText;

        // Heuristic: Only analyze likely user-facing strings.
        // 1. Ignore empty/whitespace strings
        if (string.IsNullOrWhiteSpace(valueText))
            return;

        // 2. Ignore strings that look like code, identifiers, or file paths
        if (valueText.Length < 3 || valueText.Length > 200)
            return;
        if (valueText.All(c => char.IsLetterOrDigit(c) || c == '_'))
            return;
        if (valueText.Contains("\\") || valueText.Contains("/") || valueText.Contains(":") || valueText.Contains(".cs") || valueText.Contains(".dll"))
            return;
        if (valueText.StartsWith("System.") || valueText.StartsWith("Microsoft."))
            return;
        // 3. Ignore strings that look like numbers or GUIDs
        if (Guid.TryParse(valueText, out _) || double.TryParse(valueText, out _))
            return;
        // 4. Ignore strings that look like resource keys
        if (valueText.All(c => char.IsUpper(c) || c == '_' || char.IsDigit(c)))
            return;
        // 5. Ignore strings with only punctuation
        if (valueText.All(c => char.IsPunctuation(c) || char.IsWhiteSpace(c)))
            return;
        // 6. Ignore strings that are likely logging/event IDs
        if (valueText.StartsWith("ERR_") || valueText.StartsWith("WRN_") || valueText.StartsWith("INF_"))
            return;

        // 7. Avoid certain function contexts that are not user-facing
        var parent = stringLiteral.Parent;
        if (parent is ArgumentSyntax arg)
        {
            var invocation = arg.Parent?.Parent as InvocationExpressionSyntax;
            if (invocation != null)
            {
                var invoked = invocation.Expression.ToString();
                // Common not-user-facing APIs
                if (invoked.Contains("Throw") ||
                    invoked.Contains("Log") ||
                    invoked.Contains("Debug") ||
                    invoked.Contains("Trace") ||
                    invoked.Contains("Assert") ||
                    invoked.Contains("Invariant") ||
                    invoked.Contains("Exception") ||
                    invoked.Contains("Parse") ||
                    invoked.Contains("TryParse") ||
                    invoked.Contains("nameof") ||
                    invoked.Contains("Type.GetType") ||
                    invoked.Contains("Activator.CreateInstance") ||
                    invoked.Contains("AddError") ||
                    invoked.Contains("AddWarning") ||
                    invoked.Contains("AddInfo") ||
                    invoked.Contains("SetProperty") ||
                    invoked.Contains("GetProperty") ||
                    invoked.Contains("SetField") ||
                    invoked.Contains("GetField") ||
                    invoked.Contains("SetValue") ||
                    invoked.Contains("GetValue") ||
                    invoked.Contains("Equals") ||
                    invoked.Contains("Compare") ||
                    invoked.Contains("Hash") ||
                    invoked.Contains("ToString") ||
                    invoked.Contains("Format"))
                {
                    return;
                }
            }
        }

        // If it looks like a user-facing string, report a diagnostic
        var diagnostic = Diagnostic.Create(s_moveToResxRule, stringLiteral.GetLocation(), valueText);
        context.ReportDiagnostic(diagnostic);
    }
}
