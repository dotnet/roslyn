// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PrivateAnalyzers;

/// <summary>
/// Prefer <c>argument: value</c> syntax to <c>value /*argument*/</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseNamedArgumentsDiagnosticAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Descriptor = new(PrivateDiagnosticIds.UseNamedArguments, "Use named arguments", "Prefer named argument syntax to commented identifiers", "Style", DiagnosticSeverity.Warning, isEnabledByDefault: false);
    internal static readonly string ParameterNameKey = nameof(ParameterNameKey);
    internal static readonly string CommentSpanStartKey = nameof(CommentSpanStartKey);
    internal static readonly string CommentSpanLengthKey = nameof(CommentSpanLengthKey);

    private static readonly Regex s_pattern = new(@"^/\*\s*[a-zA-Z_][a-zA-Z0-9_]*\s*\*/$", RegexOptions.Compiled);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;
            var expressionType = compilation.GetTypeByMetadataName(typeof(Expression<>).FullName!);

            context.RegisterOperationAction(context => AnalyzeArgument(context, expressionType), OperationKind.Argument);
        });
    }

    private static void AnalyzeArgument(in OperationAnalysisContext context, INamedTypeSymbol? expressionType)
    {
        var argument = (IArgumentOperation)context.Operation;
        if (argument.Parameter is null)
            return;

        if (argument.Syntax is ArgumentSyntax { NameColon: not null })
            return;
        else if (argument.Syntax is AttributeArgumentSyntax { NameColon: not null })
            return;
        else if (argument.Syntax is AttributeArgumentSyntax { NameEquals: not null })
            return;

        if (argument.Parameter.IsParams)
            return;

        AnalyzeLeadingTrivia(in context, argument.Syntax, expressionType, argument.Parameter.Name);
        AnalyzeTrailingTrivia(in context, argument.Syntax, expressionType, argument.Parameter.Name);
    }

    private static void AnalyzeLeadingTrivia(in OperationAnalysisContext context, SyntaxNode argument, INamedTypeSymbol? expressionType, string parameterName)
    {
        var leadingTrivia = argument.HasLeadingTrivia
            ? argument.GetLeadingTrivia()
            : argument.GetFirstToken().GetPreviousToken().TrailingTrivia;

        if (!leadingTrivia.Any())
            return;

        foreach (var trivia in leadingTrivia.Reverse())
        {
            if (!AnalyzeTrivia(in context, trivia, argument, expressionType, parameterName))
                return;
        }
    }

    private static void AnalyzeTrailingTrivia(in OperationAnalysisContext context, SyntaxNode argument, INamedTypeSymbol? expressionType, string parameterName)
    {
        if (!argument.HasTrailingTrivia)
            return;

        foreach (var trivia in argument.GetTrailingTrivia())
        {
            if (!AnalyzeTrivia(in context, trivia, argument, expressionType, parameterName))
                return;
        }
    }

    private static bool AnalyzeTrivia(in OperationAnalysisContext context, SyntaxTrivia trivia, SyntaxNode argument, INamedTypeSymbol? expressionType, string parameterName)
    {
        switch (trivia.Kind())
        {
            case SyntaxKind.WhitespaceTrivia:
                // Allow whitespace between the argument and the comment
                return true;

            case SyntaxKind.MultiLineCommentTrivia:
                // Delay the semantic model check until we are actually ready to report a diagnostic.
                RoslynDebug.AssertNotNull(context.Operation.SemanticModel);
                if (s_pattern.IsMatch(trivia.ToString())
                    && !argument.IsInExpressionTree(context.Operation.SemanticModel, expressionType, context.CancellationToken))
                {
                    var properties = ImmutableDictionary.Create<string, string?>()
                        .Add(ParameterNameKey, parameterName)
                        .Add(CommentSpanStartKey, trivia.Span.Start.ToString())
                        .Add(CommentSpanLengthKey, trivia.Span.Length.ToString());
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, argument.GetLocation(), properties));
                }

                return false;

            default:
                // We only consider comments immediately following the argument
                return false;
        }
    }
}
