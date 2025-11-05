// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions;

internal abstract class AbstractRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer
    : AbstractCodeQualityDiagnosticAnalyzer
{
    internal const string DocCommentIdKey = nameof(DocCommentIdKey);

    private static readonly LocalizableResourceString s_localizableTitle = new(
       nameof(AnalyzersResources.Invalid_global_SuppressMessageAttribute), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
    private static readonly LocalizableResourceString s_localizableInvalidScopeMessage = new(
        nameof(AnalyzersResources.Invalid_scope_for_SuppressMessageAttribute), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
    private static readonly LocalizableResourceString s_localizableInvalidOrMissingTargetMessage = new(
        nameof(AnalyzersResources.Invalid_or_missing_target_for_SuppressMessageAttribute), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

    private static readonly DiagnosticDescriptor s_invalidScopeDescriptor = CreateDescriptor(
        IDEDiagnosticIds.InvalidSuppressMessageAttributeDiagnosticId,
        EnforceOnBuildValues.InvalidSuppressMessageAttribute,
        s_localizableTitle, s_localizableInvalidScopeMessage,
        hasAnyCodeStyleOption: false, isUnnecessary: true);
    private static readonly DiagnosticDescriptor s_invalidOrMissingTargetDescriptor = CreateDescriptor(
        IDEDiagnosticIds.InvalidSuppressMessageAttributeDiagnosticId,
        EnforceOnBuildValues.InvalidSuppressMessageAttribute,
        s_localizableTitle, s_localizableInvalidOrMissingTargetMessage,
        hasAnyCodeStyleOption: false, isUnnecessary: true);

    private static readonly LocalizableResourceString s_localizableLegacyFormatTitle = new(
       nameof(AnalyzersResources.Avoid_legacy_format_target_in_SuppressMessageAttribute), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
    private static readonly LocalizableResourceString s_localizableLegacyFormatMessage = new(
        nameof(AnalyzersResources.Avoid_legacy_format_target_0_in_SuppressMessageAttribute), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
    internal static readonly DiagnosticDescriptor LegacyFormatTargetDescriptor = CreateDescriptor(
        IDEDiagnosticIds.LegacyFormatSuppressMessageAttributeDiagnosticId,
        EnforceOnBuildValues.LegacyFormatSuppressMessageAttribute,
        s_localizableLegacyFormatTitle, s_localizableLegacyFormatMessage,
        hasAnyCodeStyleOption: false, isUnnecessary: false);

    protected AbstractRemoveUnnecessaryAttributeSuppressionsDiagnosticAnalyzer()
        : base([s_invalidScopeDescriptor, s_invalidOrMissingTargetDescriptor, LegacyFormatTargetDescriptor], GeneratedCodeAnalysisFlags.None)
    {
    }

    protected abstract void RegisterAttributeSyntaxAction(CompilationStartAnalysisContext context, CompilationAnalyzer compilationAnalyzer);
    public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    protected sealed override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            var suppressMessageAttributeType = context.Compilation.SuppressMessageAttributeType();
            if (suppressMessageAttributeType == null)
            {
                return;
            }

            RegisterAttributeSyntaxAction(context, new CompilationAnalyzer(context.Compilation, suppressMessageAttributeType));
        });
    }

    protected sealed class CompilationAnalyzer(Compilation compilation, INamedTypeSymbol suppressMessageAttributeType)
    {
        private readonly SuppressMessageAttributeState _state = new(compilation, suppressMessageAttributeType);

        public void AnalyzeAssemblyOrModuleAttribute(SyntaxNode attributeSyntax, SemanticModel model, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            if (!_state.IsSuppressMessageAttributeWithNamedArguments(attributeSyntax, model, cancellationToken, out var namedAttributeArguments))
            {
                return;
            }

            if (!SuppressMessageAttributeState.HasValidScope(namedAttributeArguments, out var targetScope))
            {
                reportDiagnostic(Diagnostic.Create(s_invalidScopeDescriptor, attributeSyntax.GetLocation()));
                return;
            }

            if (!_state.HasValidTarget(namedAttributeArguments, targetScope, out var targetHasDocCommentIdFormat,
                    out var targetSymbolString, out var targetValueOperation, out var resolvedSymbols))
            {
                reportDiagnostic(Diagnostic.Create(s_invalidOrMissingTargetDescriptor, attributeSyntax.GetLocation()));
                return;
            }

            // We want to flag valid target which uses legacy format to update to Roslyn based DocCommentId format.
            if (resolvedSymbols.Length > 0 && !targetHasDocCommentIdFormat)
            {
                RoslynDebug.Assert(!string.IsNullOrEmpty(targetSymbolString));
                RoslynDebug.Assert(targetValueOperation != null);

                var properties = ImmutableDictionary<string, string?>.Empty;
                if (resolvedSymbols is [var resolvedSymbol])
                {
                    // We provide a code fix for the case where the target resolved to a single symbol.
                    var docCommentId = DocumentationCommentId.CreateDeclarationId(resolvedSymbol);
                    if (!string.IsNullOrEmpty(docCommentId))
                    {
                        // Suppression target has an optional "~" prefix to distinguish it from legacy FxCop suppressions.
                        // IDE suppression code fixes emit this prefix, so we we also add this prefix to new suppression target string.
                        properties = properties.Add(DocCommentIdKey, "~" + docCommentId);
                    }
                }

                reportDiagnostic(Diagnostic.Create(LegacyFormatTargetDescriptor, targetValueOperation.Syntax.GetLocation(), properties, targetSymbolString));
                return;
            }
        }
    }
}
