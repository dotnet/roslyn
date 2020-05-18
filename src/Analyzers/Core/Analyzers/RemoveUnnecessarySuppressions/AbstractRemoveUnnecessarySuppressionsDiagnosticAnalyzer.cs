// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.RemoveUnnecessarySuppressions
{
    internal abstract class AbstractRemoveUnnecessarySuppressionsDiagnosticAnalyzer
        : AbstractCodeQualityDiagnosticAnalyzer
    {
        private static readonly LocalizableResourceString s_localizableTitle = new LocalizableResourceString(
           nameof(AnalyzersResources.Invalid_global_SuppressMessageAttribute), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableResourceString s_localizableInvalidScopeMessage = new LocalizableResourceString(
            nameof(AnalyzersResources.Invalid_scope_for_SuppressMessageAttribute), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableResourceString s_localizableInvalidOrMissingTargetMessage = new LocalizableResourceString(
            nameof(AnalyzersResources.Invalid_or_missing_target_for_SuppressMessageAttribute), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        private static readonly DiagnosticDescriptor s_invalidScopeDescriptor = CreateDescriptor(
            IDEDiagnosticIds.InvalidSuppressMessageAttributeDiagnosticId, s_localizableTitle, s_localizableInvalidScopeMessage, isUnnecessary: true);
        private static readonly DiagnosticDescriptor s_invalidOrMissingTargetDescriptor = CreateDescriptor(
            IDEDiagnosticIds.InvalidSuppressMessageAttributeDiagnosticId, s_localizableTitle, s_localizableInvalidOrMissingTargetMessage, isUnnecessary: true);

        public AbstractRemoveUnnecessarySuppressionsDiagnosticAnalyzer()
            : base(ImmutableArray.Create(s_invalidScopeDescriptor, s_invalidOrMissingTargetDescriptor), GeneratedCodeAnalysisFlags.None)
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

        protected sealed class CompilationAnalyzer
        {
            private readonly SuppressMessageAttributeState _state;

            public CompilationAnalyzer(Compilation compilation, INamedTypeSymbol suppressMessageAttributeType)
            {
                _state = new SuppressMessageAttributeState(compilation, suppressMessageAttributeType);
            }

            public void AnalyzeAssemblyOrModuleAttribute(SyntaxNode attributeSyntax, SemanticModel model, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
            {
                if (!_state.IsSuppressMessageAttributeWithNamedArguments(attributeSyntax, model, cancellationToken, out var namedAttributeArguments))
                {
                    return;
                }

                DiagnosticDescriptor rule;
                if (_state.HasInvalidScope(namedAttributeArguments, out var targetScope))
                {
                    rule = s_invalidScopeDescriptor;
                }
                else if (_state.HasInvalidOrMissingTarget(namedAttributeArguments, targetScope))
                {
                    rule = s_invalidOrMissingTargetDescriptor;
                }
                else
                {
                    return;
                }

                reportDiagnostic(Diagnostic.Create(rule, attributeSyntax.GetLocation()));
            }
        }
    }
}
