// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.RemoveUnnecessaryNullableDirective;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpRemoveRedundantNullableDirectiveDiagnosticAnalyzer
    : AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer
{
    public CSharpRemoveRedundantNullableDirectiveDiagnosticAnalyzer()
        : base(
            IDEDiagnosticIds.RemoveRedundantNullableDirectiveDiagnosticId,
            EnforceOnBuildValues.RemoveRedundantNullableDirective,
            option: null,
            fadingOption: null,
            new LocalizableResourceString(nameof(CSharpAnalyzersResources.Remove_redundant_nullable_directive), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
            new LocalizableResourceString(nameof(CSharpAnalyzersResources.Nullable_directive_is_redundant), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
        {
            var compilation = (CSharpCompilation)context.Compilation;
            if (compilation.LanguageVersion < LanguageVersion.CSharp8)
            {
                // Compilation does not support nullable directives
                return;
            }

            var compilationOptions = compilation.Options;
            context.RegisterSyntaxTreeAction(context => ProcessSyntaxTree(compilationOptions, context));
        });

    private void ProcessSyntaxTree(
        CSharpCompilationOptions compilationOptions,
        SyntaxTreeAnalysisContext context)
    {
        if (ShouldSkipAnalysis(context, compilationOptions, notification: null))
            return;

        var root = context.GetAnalysisRoot(findInTrivia: true);

        // Bail out if the root contains no nullable directives.
        if (!root.ContainsDirective(SyntaxKind.NullableDirectiveTrivia))
            return;

        var defaultNullableContext = compilationOptions.NullableContextOptions;
        NullableContextOptions? currentState = context.Tree.IsGeneratedCode(context.Options, CSharpSyntaxFacts.Instance, context.CancellationToken)
            ? NullableContextOptions.Disable
            : defaultNullableContext;

        using var pooledStack = SharedPools.Default<Stack<SyntaxNodeOrToken>>().GetPooledObject();
        var stack = pooledStack.Object;

        stack.Push(root);

        while (stack.TryPop(out var current))
        {
            if (!current.ContainsDirectives)
                continue;

            if (current.AsNode(out var childNode))
            {
                // Add the nodes in reverse so we continue walking in a depth-first fashion.
                foreach (var child in childNode.ChildNodesAndTokens().Reverse())
                    stack.Push(child);
            }
            else if (current.IsToken)
            {
                foreach (var trivia in current.AsToken().LeadingTrivia)
                    ProcessTrivia(trivia);
            }
        }

        return;

        void ProcessTrivia(SyntaxTrivia trivia)
        {
            if (!trivia.IsDirective)
                return;

            var directive = trivia.GetStructure()!;
            switch (directive.Kind())
            {
                case SyntaxKind.NullableDirectiveTrivia:
                    {
                        // Report if the nullable directive puts us in the same state we're already in.
                        var newState = GetNullableContextOptions(defaultNullableContext, currentState, (NullableDirectiveTriviaSyntax)directive);
                        if (newState == currentState)
                            context.ReportDiagnostic(Diagnostic.Create(Descriptor, directive.GetLocation()));

                        currentState = newState;
                        return;
                    }

                case SyntaxKind.IfDirectiveTrivia or
                     SyntaxKind.ElifDirectiveTrivia or
                     SyntaxKind.ElseDirectiveTrivia or
                     SyntaxKind.EndIfDirectiveTrivia:
                    // Reset the known nullable state when crossing a conditional compilation boundary
                    currentState = null;
                    return;
            }
        }
    }

    internal static NullableContextOptions? GetNullableContextOptions(NullableContextOptions compilationOptions, NullableContextOptions? options, NullableDirectiveTriviaSyntax directive)
    {
        if (!directive.TargetToken.IsKind(SyntaxKind.None))
        {
            if (options is not { } knownState)
            {
                return null;
            }

            NullableContextOptions flagToChange;
            if (directive.TargetToken.IsKind(SyntaxKind.AnnotationsKeyword))
            {
                flagToChange = NullableContextOptions.Annotations;
            }
            else if (directive.TargetToken.IsKind(SyntaxKind.WarningsKeyword))
            {
                flagToChange = NullableContextOptions.Warnings;
            }
            else
            {
                return null;
            }

            if (directive.SettingToken.IsKind(SyntaxKind.EnableKeyword))
            {
                return knownState | flagToChange;
            }
            else if (directive.SettingToken.IsKind(SyntaxKind.DisableKeyword))
            {
                return knownState & (~flagToChange);
            }
            else
            {
                return null;
            }
        }

        if (directive.SettingToken.IsKind(SyntaxKind.EnableKeyword))
        {
            return NullableContextOptions.Annotations | NullableContextOptions.Warnings;
        }
        else if (directive.SettingToken.IsKind(SyntaxKind.DisableKeyword))
        {
            return NullableContextOptions.Disable;
        }
        else if (directive.SettingToken.IsKind(SyntaxKind.RestoreKeyword))
        {
            return compilationOptions;
        }
        else
        {
            return null;
        }
    }
}
