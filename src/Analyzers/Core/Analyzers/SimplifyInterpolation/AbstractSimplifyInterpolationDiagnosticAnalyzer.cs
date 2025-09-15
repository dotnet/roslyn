// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.SimplifyInterpolation;

internal abstract class AbstractSimplifyInterpolationDiagnosticAnalyzer<
    TInterpolationSyntax,
    TExpressionSyntax>() : AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(IDEDiagnosticIds.SimplifyInterpolationId,
        EnforceOnBuildValues.SimplifyInterpolation,
        CodeStyleOptions2.PreferSimplifiedInterpolation,
        fadingOption: null,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_interpolation), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Interpolation_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    where TInterpolationSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
{
    protected abstract ISyntaxFacts SyntaxFacts { get; }
    protected abstract IVirtualCharService VirtualCharService { get; }
    protected abstract AbstractSimplifyInterpolationHelpers<TInterpolationSyntax, TExpressionSyntax> Helpers { get; }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(
            context =>
            {
                var compilation = context.Compilation;
                var knownToStringFormats = Helpers.BuildKnownToStringFormatsLookupTable(compilation);

                var readOnlySpanOfCharType = compilation.ReadOnlySpanOfTType()?.Construct(compilation.GetSpecialType(SpecialType.System_Char));
                var handlersAvailable = compilation.InterpolatedStringHandlerAttributeType() != null;

                context.RegisterOperationAction(context => AnalyzeInterpolation(context, compilation.FormattableStringType(), compilation.IFormattableType(), readOnlySpanOfCharType, knownToStringFormats, handlersAvailable), OperationKind.Interpolation);
            });

    private void AnalyzeInterpolation(
        OperationAnalysisContext context,
        INamedTypeSymbol? formattableStringType,
        INamedTypeSymbol? iFormattableType,
        INamedTypeSymbol? readOnlySpanOfCharType,
        ImmutableDictionary<IMethodSymbol, string> knownToStringFormats,
        bool handlersAvailable)
    {
        var option = context.GetAnalyzerOptions().PreferSimplifiedInterpolation;

        // No point in analyzing if the option is off.
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var interpolation = (IInterpolationOperation)context.Operation;

        // Formattable strings can observe the inner types of the arguments passed to them.  So we can't safely change
        // to drop ToString in that.
        if (interpolation.Parent is IInterpolatedStringOperation { Parent: IConversionOperation { Type: { } convertedType } conversion } &&
            (convertedType.Equals(formattableStringType) || convertedType.Equals(iFormattableType)))
        {
            // One exception to this is calling directly into FormattableString.Invariant.  That method has known good
            // behavior that is fine to continue calling into.
            var isInvariantInvocation =
                conversion.Parent is IArgumentOperation { Parent: IInvocationOperation invocation } &&
                invocation.TargetMethod.Name == nameof(FormattableString.Invariant) &&
                invocation.TargetMethod.ContainingType.Equals(formattableStringType);
            if (!isInvariantInvocation)
                return;
        }

        this.Helpers.UnwrapInterpolation(
            this.VirtualCharService, this.SyntaxFacts, interpolation, knownToStringFormats, readOnlySpanOfCharType, handlersAvailable, out _, out var alignment, out _,
            out var formatString, out var unnecessaryLocations);

        if (alignment == null && formatString == null)
            return;

        // The diagnostic itself fades the first unnecessary location, and the remaining locations are passed as
        // additional unnecessary locations.
        var firstUnnecessaryLocation = unnecessaryLocations[0];
        var remainingUnnecessaryLocations = unnecessaryLocations.RemoveAt(0);

        context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
            Descriptor,
            firstUnnecessaryLocation,
            option.Notification,
            context.Options,
            additionalLocations: [interpolation.Syntax.GetLocation()],
            additionalUnnecessaryLocations: remainingUnnecessaryLocations));
    }
}
