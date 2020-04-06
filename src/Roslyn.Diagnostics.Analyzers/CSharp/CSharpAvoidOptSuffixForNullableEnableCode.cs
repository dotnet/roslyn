// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.Lightup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpAvoidOptSuffixForNullableEnableCode : AvoidOptSuffixForNullableEnableCode
    {
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(context =>
            {
                var parameter = (ParameterSyntax)context.Node;

                if (parameter.Identifier.Text.EndsWith("Opt", System.StringComparison.Ordinal) &&
                    context.SemanticModel.GetNullableContext(parameter.SpanStart).AnnotationsEnabled())
                {
                    context.ReportDiagnostic(parameter.Identifier.CreateDiagnostic(Rule));
                }
            }, SyntaxKind.Parameter);

            context.RegisterSyntaxNodeAction(context =>
            {
                var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

                foreach (var variable in fieldDeclaration.Declaration.Variables)
                {
                    if (variable.Identifier.Text.EndsWith("Opt", System.StringComparison.Ordinal) &&
                        context.SemanticModel.GetNullableContext(variable.SpanStart).AnnotationsEnabled())
                    {
                        context.ReportDiagnostic(variable.Identifier.CreateDiagnostic(Rule));
                    }
                }
            }, SyntaxKind.FieldDeclaration);
        }
    }
}
