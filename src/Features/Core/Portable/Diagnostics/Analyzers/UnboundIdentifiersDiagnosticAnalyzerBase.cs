// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.AddImport;

/// <summary>
/// See https://github.com/dotnet/roslyn/issues/7536.  IDE should not be analyzing and reporting
/// compiler diagnostics for normal constructs.  However, the compiler does not report issues
/// for incomplete members.  That means that if you just have `public DateTime` that that is counted 
/// as an incomplete member where no binding happens at all.  This means that features like 'add import'
/// won't work here to offer to add `using System;` if that is all that is written.  
/// <para>
/// This definitely needs to be fixed at the compiler layer.  However, until that happens, this is 
/// only alternative at our disposal.
/// </para>
/// </summary>
internal abstract class UnboundIdentifiersDiagnosticAnalyzerBase<TLanguageKindEnum, TSimpleNameSyntax, TQualifiedNameSyntax, TIncompleteMemberSyntax> : DiagnosticAnalyzer, IBuiltInAnalyzer
    where TLanguageKindEnum : struct
    where TSimpleNameSyntax : SyntaxNode
    where TQualifiedNameSyntax : SyntaxNode
    where TIncompleteMemberSyntax : SyntaxNode
{
    protected abstract DiagnosticDescriptor DiagnosticDescriptor { get; }
    protected abstract ImmutableArray<TLanguageKindEnum> SyntaxKindsOfInterest { get; }
    protected abstract bool IsNameOf(SyntaxNode node);

    // High priority as we need to know about unbound identifiers so that we can run add-using to fix them.
    public bool IsHighPriority
        => true;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [DiagnosticDescriptor];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKindsOfInterest.ToArray());
    }

    protected static DiagnosticDescriptor GetDiagnosticDescriptor(string id, LocalizableString messageFormat)
    {
        // it is not configurable diagnostic, title doesn't matter
        return new DiagnosticDescriptor(
            id, string.Empty, messageFormat,
            DiagnosticCategory.Compiler,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: DiagnosticCustomTags.Microsoft.Append(WellKnownDiagnosticTags.NotConfigurable));
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is TIncompleteMemberSyntax)
        {
            ReportUnboundIdentifierNames(context, context.Node);
        }
    }

    private void ReportUnboundIdentifierNames(SyntaxNodeAnalysisContext context, SyntaxNode member)
    {
        var typeNames = member.DescendantNodes().Where(n => IsQualifiedOrSimpleName(n) && !n.Span.IsEmpty);
        foreach (var typeName in typeNames)
        {
            var info = context.SemanticModel.GetSymbolInfo(typeName);
            if (info.Symbol == null && info.CandidateSymbols.Length == 0)
            {
                // GetSymbolInfo returns no symbols for "nameof" expression, so handle it specially.
                if (IsNameOf(typeName))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, typeName.GetLocation(), typeName.ToString()));
            }
        }
    }

    private static bool IsQualifiedOrSimpleName(SyntaxNode n)
        => n is TQualifiedNameSyntax or TSimpleNameSyntax;

    public DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
}
