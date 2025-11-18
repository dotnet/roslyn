// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UseInferredMemberName;

namespace Microsoft.CodeAnalysis.CSharp.UseInferredMemberName;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseInferredMemberNameDiagnosticAnalyzer : AbstractUseInferredMemberNameDiagnosticAnalyzer
{
    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.NameColon, SyntaxKind.NameEquals);

    protected override void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        switch (context.Node.Kind())
        {
            case SyntaxKind.NameColon:
                ReportDiagnosticsIfNeeded((NameColonSyntax)context.Node, context);
                break;
            case SyntaxKind.NameEquals:
                ReportDiagnosticsIfNeeded((NameEqualsSyntax)context.Node, context);
                break;
        }
    }

    private void ReportDiagnosticsIfNeeded(NameColonSyntax nameColon, SyntaxNodeAnalysisContext context)
    {
        if (nameColon.Parent is not ArgumentSyntax argument)
        {
            return;
        }

        var syntaxTree = context.Node.SyntaxTree;
        var parseOptions = (CSharpParseOptions)syntaxTree.Options;
        var preference = context.GetAnalyzerOptions().PreferInferredTupleNames;
        if (!preference.Value
            || ShouldSkipAnalysis(context, preference.Notification)
            || !CSharpInferredMemberNameSimplifier.CanSimplifyTupleElementName(argument, parseOptions))
        {
            return;
        }

        // Create a normal diagnostic
        var fadeSpan = TextSpan.FromBounds(nameColon.Name.SpanStart, nameColon.ColonToken.Span.End);
        context.ReportDiagnostic(
            DiagnosticHelper.CreateWithLocationTags(
                Descriptor,
                nameColon.GetLocation(),
                preference.Notification,
                context.Options,
                additionalLocations: [],
                additionalUnnecessaryLocations: [syntaxTree.GetLocation(fadeSpan)]));
    }

    private void ReportDiagnosticsIfNeeded(NameEqualsSyntax nameEquals, SyntaxNodeAnalysisContext context)
    {
        if (nameEquals.Parent is not AnonymousObjectMemberDeclaratorSyntax anonCtor)
        {
            return;
        }

        var preference = context.GetAnalyzerOptions().PreferInferredAnonymousTypeMemberNames;
        if (!preference.Value ||
            !CSharpInferredMemberNameSimplifier.CanSimplifyAnonymousTypeMemberName(anonCtor))
        {
            return;
        }

        // Create a normal diagnostic
        var fadeSpan = TextSpan.FromBounds(nameEquals.Name.SpanStart, nameEquals.EqualsToken.Span.End);
        context.ReportDiagnostic(
            DiagnosticHelper.CreateWithLocationTags(
                Descriptor,
                nameEquals.GetLocation(),
                preference.Notification,
                context.Options,
                additionalLocations: [],
                additionalUnnecessaryLocations: [context.Node.SyntaxTree.GetLocation(fadeSpan)]));
    }
}
