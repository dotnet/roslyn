// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseInferredMemberName
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUseInferredMemberNameDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public CSharpUseInferredMemberNameDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseInferredMemberNameDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_inferred_member_name), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Member_name_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.NameColon, SyntaxKind.NameEquals);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;

            var syntaxTree = context.Node.SyntaxTree;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

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
            if (!nameColon.IsParentKind(SyntaxKind.Argument))
            {
                return;
            }

            var argument = (ArgumentSyntax)nameColon.Parent;
            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            if (!optionSet.GetOption(CSharpCodeStyleOptions.PreferInferredTupleNames).Value ||
                !CSharpInferredMemberNameReducer.CanSimplifyTupleElementName(argument, parseOptions))
            {
                return;
            }

            // Create a normal diagnostic
            context.ReportDiagnostic(
                Diagnostic.Create(GetDescriptorWithSeverity(
                    optionSet.GetOption(CSharpCodeStyleOptions.PreferInferredTupleNames).Notification.Value),
                    nameColon.GetLocation()));

            // Also fade out the part of the name-colon syntax
            var fadeSpan = TextSpan.FromBounds(nameColon.Name.SpanStart, nameColon.ColonToken.Span.End);
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnnecessaryWithoutSuggestionDescriptor,
                    syntaxTree.GetLocation(fadeSpan)));
        }

        private void ReportDiagnosticsIfNeeded(NameEqualsSyntax nameEquals, SyntaxNodeAnalysisContext context, OptionSet optionSet, SyntaxTree syntaxTree)
        {
            if (!nameEquals.IsParentKind(SyntaxKind.AnonymousObjectMemberDeclarator))
            {
                return;
            }

            var anonCtor = (AnonymousObjectMemberDeclaratorSyntax)nameEquals.Parent;
            if (!optionSet.GetOption(CSharpCodeStyleOptions.PreferInferredAnonymousTypeMemberNames).Value ||
                !CSharpInferredMemberNameReducer.CanSimplifyAnonymousTypeMemberName(anonCtor))
            {
                return;
            }

            // Create a normal diagnostic
            context.ReportDiagnostic(
                Diagnostic.Create(GetDescriptorWithSeverity(
                    optionSet.GetOption(CSharpCodeStyleOptions.PreferInferredAnonymousTypeMemberNames).Notification.Value),
                    nameEquals.GetLocation()));

            // Also fade out the part of the name-equals syntax
            var fadeSpan = TextSpan.FromBounds(nameEquals.Name.SpanStart, nameEquals.EqualsToken.Span.End);
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnnecessaryWithoutSuggestionDescriptor,
                    syntaxTree.GetLocation(fadeSpan)));
        }
    }
}
