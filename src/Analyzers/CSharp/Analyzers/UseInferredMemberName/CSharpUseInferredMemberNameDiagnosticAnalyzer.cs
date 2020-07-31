// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UseInferredMemberName;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseInferredMemberName
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUseInferredMemberNameDiagnosticAnalyzer : AbstractUseInferredMemberNameDiagnosticAnalyzer
    {
        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.NameColon, SyntaxKind.NameEquals);

        protected override void LanguageSpecificAnalyzeSyntax(SyntaxNodeAnalysisContext context, SyntaxTree syntaxTree, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            switch (context.Node.Kind())
            {
                case SyntaxKind.NameColon:
                    ReportDiagnosticsIfNeeded((NameColonSyntax)context.Node, context, options, syntaxTree, cancellationToken);
                    break;
                case SyntaxKind.NameEquals:
                    ReportDiagnosticsIfNeeded((NameEqualsSyntax)context.Node, context, options, syntaxTree, cancellationToken);
                    break;
            }
        }

        private void ReportDiagnosticsIfNeeded(NameColonSyntax nameColon, SyntaxNodeAnalysisContext context, AnalyzerOptions options, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            if (!nameColon.Parent.IsKind(SyntaxKind.Argument, out ArgumentSyntax? argument))
            {
                return;
            }

            RoslynDebug.Assert(context.Compilation is object);
            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            var preference = options.GetOption(
                CodeStyleOptions2.PreferInferredTupleNames, context.Compilation.Language, syntaxTree, cancellationToken);
            if (!preference.Value ||
                !CSharpInferredMemberNameSimplifier.CanSimplifyTupleElementName(argument, parseOptions))
            {
                return;
            }

            // Create a normal diagnostic
            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor,
                    nameColon.GetLocation(),
                    preference.Notification.Severity,
                    additionalLocations: null,
                    properties: null));

            // Also fade out the part of the name-colon syntax
            RoslynDebug.AssertNotNull(UnnecessaryWithoutSuggestionDescriptor);
            var fadeSpan = TextSpan.FromBounds(nameColon.Name.SpanStart, nameColon.ColonToken.Span.End);
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnnecessaryWithoutSuggestionDescriptor,
                    syntaxTree.GetLocation(fadeSpan)));
        }

        private void ReportDiagnosticsIfNeeded(NameEqualsSyntax nameEquals, SyntaxNodeAnalysisContext context, AnalyzerOptions options, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            if (!nameEquals.Parent.IsKind(SyntaxKind.AnonymousObjectMemberDeclarator, out AnonymousObjectMemberDeclaratorSyntax? anonCtor))
            {
                return;
            }

            RoslynDebug.Assert(context.Compilation is object);
            var preference = options.GetOption(
                CodeStyleOptions2.PreferInferredAnonymousTypeMemberNames, context.Compilation.Language, syntaxTree, cancellationToken);
            if (!preference.Value ||
                !CSharpInferredMemberNameSimplifier.CanSimplifyAnonymousTypeMemberName(anonCtor))
            {
                return;
            }

            // Create a normal diagnostic
            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor,
                    nameEquals.GetLocation(),
                    preference.Notification.Severity,
                    additionalLocations: null,
                    properties: null));

            // Also fade out the part of the name-equals syntax
            RoslynDebug.AssertNotNull(UnnecessaryWithoutSuggestionDescriptor);
            var fadeSpan = TextSpan.FromBounds(nameEquals.Name.SpanStart, nameEquals.EqualsToken.Span.End);
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnnecessaryWithoutSuggestionDescriptor,
                    syntaxTree.GetLocation(fadeSpan)));
        }
    }
}
