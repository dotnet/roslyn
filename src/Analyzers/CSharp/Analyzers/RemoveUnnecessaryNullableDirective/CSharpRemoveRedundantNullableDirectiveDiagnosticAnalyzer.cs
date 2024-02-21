// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.RemoveUnnecessaryNullableDirective
{
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
        {
            context.RegisterCompilationStartAction(context =>
            {
                if (((CSharpCompilation)context.Compilation).LanguageVersion < LanguageVersion.CSharp8)
                {
                    // Compilation does not support nullable directives
                    return;
                }

                var compilationOptions = context.Compilation.Options;
                var defaultNullableContext = ((CSharpCompilationOptions)compilationOptions).NullableContextOptions;
                context.RegisterSyntaxTreeAction(context =>
                {
                    if (ShouldSkipAnalysis(context, compilationOptions, notification: null))
                        return;

                    var root = context.GetAnalysisRoot(findInTrivia: true);

                    // Bail out if the root contains no nullable directives.
                    if (!root.ContainsDirective(SyntaxKind.NullableDirectiveTrivia))
                        return;

                    var initialState = context.Tree.IsGeneratedCode(context.Options, CSharpSyntaxFacts.Instance, context.CancellationToken)
                        ? NullableContextOptions.Disable
                        : defaultNullableContext;

                    NullableContextOptions? currentState = initialState;
                    for (var directive = root.GetFirstDirective(); directive is not null; directive = directive.GetNextDirective())
                    {
                        if (directive.DirectiveNameToken.IsKind(SyntaxKind.NullableKeyword))
                        {
                            var newState = GetNullableContextOptions(defaultNullableContext, currentState, (NullableDirectiveTriviaSyntax)directive);
                            if (newState == currentState)
                                context.ReportDiagnostic(Diagnostic.Create(Descriptor, directive.GetLocation()));

                            currentState = newState;
                        }
                        else if (directive.DirectiveNameToken.Kind() is
                            SyntaxKind.IfKeyword or
                            SyntaxKind.ElifKeyword or
                            SyntaxKind.ElseKeyword or
                            SyntaxKind.EndIfKeyword)
                        {
                            // Reset the known nullable state when crossing a conditional compilation boundary
                            currentState = null;
                        }
                    }
                });
            });
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
}
