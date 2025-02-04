// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols;

/// <summary>
/// Accessor of a <see cref="SourceFieldLikeEventSymbol"/> which is a partial definition.
/// </summary>
internal sealed class SourceEventDefinitionAccessorSymbol : SourceEventAccessorSymbol
{
    internal SourceEventDefinitionAccessorSymbol(
        SourceFieldLikeEventSymbol ev,
        bool isAdder,
        BindingDiagnosticBag diagnostics)
        : base(
            @event: ev,
            syntaxReference: ev.SyntaxReference,
            location: ev.Location,
            explicitlyImplementedEventOpt: null,
            aliasQualifierOpt: null,
            isAdder: isAdder,
            isIterator: false,
            isNullableAnalysisEnabled: ev.DeclaringCompilation.IsNullableAnalysisEnabledIn(ev.CSharpSyntaxNode),
            isExpressionBodied: false)
    {
        CheckFeatureAvailabilityAndRuntimeSupport(ev.CSharpSyntaxNode, ev.Location, hasBody: false, diagnostics: diagnostics);
    }

    public override Accessibility DeclaredAccessibility => AssociatedEvent.DeclaredAccessibility;

    public override bool IsImplicitlyDeclared => true;

    internal override bool GenerateDebugInfo => true;

    internal override ExecutableCodeBinder? TryGetBodyBinder(BinderFactory? binderFactoryOpt = null, bool ignoreAccessibility = false)
    {
        return null;
    }
}
