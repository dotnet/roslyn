// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UseInferredMemberName;

namespace Microsoft.CodeAnalysis.CSharp.UseInferredMemberName
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUseInferredMemberNameDiagnosticAnalyzer : AbstractUseInferredMemberNameDiagnosticAnalyzer
    {
        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.NameColon, SyntaxKind.NameEquals);

        override protected void LanguageSpecificAnalyzeSyntax(SyntaxNodeAnalysisContext context, SyntaxTree syntaxTree, OptionSet optionSet)
        {
            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            switch (context.Node.Kind())
            {
                case SyntaxKind.NameColon:
                    ReportDiagnosticsIfNeeded((NameColonSyntax)context.Node, context, optionSet, syntaxTree);
                    break;
                case SyntaxKind.NameEquals:
                    ReportDiagnosticsIfNeeded((NameEqualsSyntax)context.Node, context, optionSet, syntaxTree);
                    break;
            }
        }

        private void ReportDiagnosticsIfNeeded(NameColonSyntax nameColon, SyntaxNodeAnalysisContext context, OptionSet optionSet, SyntaxTree syntaxTree)
        {
            if (!nameColon.Parent.IsKind(SyntaxKind.Argument, out ArgumentSyntax? argument))
            {
                return;
            }

            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            var option = optionSet.GetOption(CodeStyleOptions.PreferInferredTupleNames, context.Compilation.Language);
            if (option == null ||
                !option.Value ||
                !CSharpInferredMemberNameReducer.CanSimplifyTupleElementName(argument, parseOptions))
            {
                return;
            }

            // Create a normal diagnostic
            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor,
                    nameColon.GetLocation(),
                    option.Notification.Severity,
                    additionalLocations: null,
                    properties: null));

            // Also fade out the part of the name-colon syntax
            var fadeSpan = TextSpan.FromBounds(nameColon.Name.SpanStart, nameColon.ColonToken.Span.End);
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnnecessaryWithoutSuggestionDescriptor,
                    syntaxTree.GetLocation(fadeSpan)));
        }

        private void ReportDiagnosticsIfNeeded(NameEqualsSyntax nameEquals, SyntaxNodeAnalysisContext context, OptionSet optionSet, SyntaxTree syntaxTree)
        {
            if (!nameEquals.Parent.IsKind(SyntaxKind.AnonymousObjectMemberDeclarator, out AnonymousObjectMemberDeclaratorSyntax? anonCtor))
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.PreferInferredAnonymousTypeMemberNames, context.Compilation.Language);
            if (option == null ||
                !option.Value ||
                !CSharpInferredMemberNameReducer.CanSimplifyAnonymousTypeMemberName(anonCtor))
            {
                return;
            }

            // Create a normal diagnostic
            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor,
                    nameEquals.GetLocation(),
                    option.Notification.Severity,
                    additionalLocations: null,
                    properties: null));

            // Also fade out the part of the name-equals syntax
            var fadeSpan = TextSpan.FromBounds(nameEquals.Name.SpanStart, nameEquals.EqualsToken.Span.End);
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnnecessaryWithoutSuggestionDescriptor,
                    syntaxTree.GetLocation(fadeSpan)));
        }
    }
}
