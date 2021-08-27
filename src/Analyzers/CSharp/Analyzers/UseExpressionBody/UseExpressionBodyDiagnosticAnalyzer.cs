﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#endif

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class UseExpressionBodyDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public const string FixesError = nameof(FixesError);

        private readonly ImmutableArray<SyntaxKind> _syntaxKinds;

        private static readonly ImmutableArray<UseExpressionBodyHelper> _helpers = UseExpressionBodyHelper.Helpers;

        public UseExpressionBodyDiagnosticAnalyzer()
            : base(GetSupportedDescriptorsWithOptions(), LanguageNames.CSharp)
        {
            _syntaxKinds = _helpers.SelectMany(h => h.SyntaxKinds).ToImmutableArray();
        }

        private static ImmutableDictionary<DiagnosticDescriptor, ILanguageSpecificOption> GetSupportedDescriptorsWithOptions()
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticDescriptor, ILanguageSpecificOption>();
            foreach (var helper in _helpers)
            {
                var descriptor = CreateDescriptorWithId(helper.DiagnosticId, helper.EnforceOnBuild, helper.UseExpressionBodyTitle, helper.UseExpressionBodyTitle);
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
            var options = context.Options;
            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = options.GetAnalyzerOptionSet(syntaxTree, cancellationToken);

            var nodeKind = context.Node.Kind();

            // Don't offer a fix on an accessor, if we would also offer it on the property/indexer.
            if (UseExpressionBodyForAccessorsHelper.Instance.SyntaxKinds.Contains(nodeKind))
            {
                var grandparent = context.Node.Parent.Parent;

                if (grandparent.Kind() == SyntaxKind.PropertyDeclaration &&
                    AnalyzeSyntax(optionSet, grandparent, UseExpressionBodyForPropertiesHelper.Instance) != null)
                {
                    return;
                }

                if (grandparent.Kind() == SyntaxKind.IndexerDeclaration &&
                    AnalyzeSyntax(optionSet, grandparent, UseExpressionBodyForIndexersHelper.Instance) != null)
                {
                    return;
                }
            }

            foreach (var helper in _helpers)
            {
                if (helper.SyntaxKinds.Contains(nodeKind))
                {
                    var diagnostic = AnalyzeSyntax(optionSet, context.Node, helper);
                    if (diagnostic != null)
                    {
                        context.ReportDiagnostic(diagnostic);
                        return;
                    }
                }
            }
        }

        private static Diagnostic AnalyzeSyntax(
            OptionSet optionSet, SyntaxNode declaration, UseExpressionBodyHelper helper)
        {
            var preferExpressionBodiedOption = optionSet.GetOption(helper.Option);
            var severity = preferExpressionBodiedOption.Notification.Severity;

            if (helper.CanOfferUseExpressionBody(optionSet, declaration, forAnalyzer: true))
            {
                var location = severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) == ReportDiagnostic.Hidden
                    ? declaration.GetLocation()
                    : helper.GetDiagnosticLocation(declaration);

                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                var properties = ImmutableDictionary<string, string>.Empty.Add(nameof(UseExpressionBody), "");
                return DiagnosticHelper.Create(
                    CreateDescriptorWithId(helper.DiagnosticId, helper.EnforceOnBuild, helper.UseExpressionBodyTitle, helper.UseExpressionBodyTitle),
                    location, severity, additionalLocations: additionalLocations, properties: properties);
            }

            var (canOffer, fixesError) = helper.CanOfferUseBlockBody(optionSet, declaration, forAnalyzer: true);
            if (canOffer)
            {
                // They have an expression body.  Create a diagnostic to convert it to a block
                // if they don't want expression bodies for this member.  
                var location = severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) == ReportDiagnostic.Hidden
                    ? declaration.GetLocation()
                    : helper.GetExpressionBody(declaration).GetLocation();

                var properties = ImmutableDictionary<string, string>.Empty;
                if (fixesError)
                {
                    properties = properties.Add(FixesError, "");
                }

                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                return DiagnosticHelper.Create(
                    CreateDescriptorWithId(helper.DiagnosticId, helper.EnforceOnBuild, helper.UseBlockBodyTitle, helper.UseBlockBodyTitle),
                    location, severity, additionalLocations: additionalLocations, properties: properties);
            }

            return null;
        }
    }
}
