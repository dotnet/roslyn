// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class UseExpressionBodyDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public const string FixesError = nameof(FixesError);

        private readonly ImmutableArray<SyntaxKind> _syntaxKinds;

        private static readonly ImmutableArray<UseExpressionBodyHelper> _helpers = UseExpressionBodyHelper.Helpers;

        public UseExpressionBodyDiagnosticAnalyzer()
            : base(GetSupportedDescriptorsWithOptions())
        {
            _syntaxKinds = _helpers.SelectManyAsArray(h => h.SyntaxKinds);
        }

        private static ImmutableDictionary<DiagnosticDescriptor, IOption2> GetSupportedDescriptorsWithOptions()
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticDescriptor, IOption2>();
            foreach (var helper in _helpers)
            {
                var descriptor = CreateDescriptorWithId(helper.DiagnosticId, helper.EnforceOnBuild, hasAnyCodeStyleOption: true, helper.UseExpressionBodyTitle, helper.UseExpressionBodyTitle);
                builder.Add(descriptor, helper.Option);
            }

            return builder.ToImmutable();
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, _syntaxKinds);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var options = context.GetCSharpAnalyzerOptions().GetCodeGenerationOptions();

            var nodeKind = context.Node.Kind();

            // Don't offer a fix on an accessor, if we would also offer it on the property/indexer.
            if (UseExpressionBodyForAccessorsHelper.Instance.SyntaxKinds.Contains(nodeKind))
            {
                var grandparent = context.Node.GetRequiredParent().GetRequiredParent();

                if (grandparent.Kind() == SyntaxKind.PropertyDeclaration &&
                    AnalyzeSyntax(options, grandparent, context, UseExpressionBodyForPropertiesHelper.Instance, cancellationToken) != null)
                {
                    return;
                }

                if (grandparent.Kind() == SyntaxKind.IndexerDeclaration &&
                    AnalyzeSyntax(options, grandparent, context, UseExpressionBodyForIndexersHelper.Instance, cancellationToken) != null)
                {
                    return;
                }
            }

            foreach (var helper in _helpers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (helper.SyntaxKinds.Contains(nodeKind))
                {
                    var diagnostic = AnalyzeSyntax(options, context.Node, context, helper, cancellationToken);
                    if (diagnostic != null)
                    {
                        context.ReportDiagnostic(diagnostic);
                        return;
                    }
                }
            }
        }

        private Diagnostic? AnalyzeSyntax(
            CSharpCodeGenerationOptions options, SyntaxNode declaration, SyntaxNodeAnalysisContext context, UseExpressionBodyHelper helper, CancellationToken cancellationToken)
        {
            var preference = helper.GetExpressionBodyPreference(options);
            if (ShouldSkipAnalysis(context, preference.Notification))
                return null;

            var severity = preference.Notification.Severity;

            if (helper.CanOfferUseExpressionBody(preference, declaration, forAnalyzer: true, cancellationToken))
            {
                var location = severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) == ReportDiagnostic.Hidden
                    ? declaration.GetLocation()
                    : helper.GetDiagnosticLocation(declaration);

                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                var properties = ImmutableDictionary<string, string?>.Empty.Add(nameof(UseExpressionBody), "");
                return DiagnosticHelper.Create(
                    CreateDescriptorWithId(helper.DiagnosticId, helper.EnforceOnBuild, hasAnyCodeStyleOption: true, helper.UseExpressionBodyTitle, helper.UseExpressionBodyTitle),
                    location, preference.Notification, additionalLocations: additionalLocations, properties: properties);
            }

            if (helper.CanOfferUseBlockBody(preference, declaration, forAnalyzer: true, out var fixesError, out var expressionBody))
            {
                // They have an expression body.  Create a diagnostic to convert it to a block
                // if they don't want expression bodies for this member.  
                var location = severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) == ReportDiagnostic.Hidden
                    ? declaration.GetLocation()
                    : expressionBody.GetLocation();

                var properties = ImmutableDictionary<string, string?>.Empty;
                if (fixesError)
                    properties = properties.Add(FixesError, "");

                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                return DiagnosticHelper.Create(
                    CreateDescriptorWithId(helper.DiagnosticId, helper.EnforceOnBuild, hasAnyCodeStyleOption: true, helper.UseBlockBodyTitle, helper.UseBlockBodyTitle),
                    location, preference.Notification, additionalLocations: additionalLocations, properties: properties);
            }

            return null;
        }
    }
}
