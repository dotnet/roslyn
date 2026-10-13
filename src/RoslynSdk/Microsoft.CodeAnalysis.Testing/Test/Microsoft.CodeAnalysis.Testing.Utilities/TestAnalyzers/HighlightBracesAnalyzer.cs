// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Testing.TestAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class HighlightBracesAnalyzer : AbstractHighlightBracesAnalyzer
    {
        public HighlightBracesAnalyzer()
        {
        }

        [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1025:Configure generated code analysis", Justification = "False positive: https://github.com/dotnet/roslyn-analyzers/issues/4624")]
        [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1026:Enable concurrent execution", Justification = "False positive: https://github.com/dotnet/roslyn-analyzers/issues/4625")]
        public override void Initialize(AnalysisContext context)
        {
            base.Initialize(context);

            // Also register a callback to handle braces in additional files
            context.RegisterCompilationAction(HandleCompilation);
        }

        private void HandleCompilation(CompilationAnalysisContext context)
        {
            foreach (var file in context.Options.AdditionalFiles)
            {
                var sourceText = file.GetText(context.CancellationToken);
                if (sourceText is null)
                {
                    continue;
                }

                var text = sourceText.ToString();
                for (var i = text.IndexOf('{'); i >= 0; i = text.IndexOf('{', i + 1))
                {
                    var textSpan = new TextSpan(i, 1);
                    var lineSpan = sourceText.Lines.GetLinePositionSpan(textSpan);
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, Location.Create(file.Path, textSpan, lineSpan)));
                }
            }
        }
    }
}
