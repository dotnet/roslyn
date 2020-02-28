// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.FileHeaders
{
    internal abstract class AbstractFileHeaderDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        protected AbstractFileHeaderDiagnosticAnalyzer()
            : base(
                IDEDiagnosticIds.FileHeaderMismatch,
                CodeStyleOptions.FileHeaderTemplate,
                LanguageNames.CSharp,
                new LocalizableResourceString(nameof(FeaturesResources.The_file_header_is_missing_not_located_at_the_top_of_the_file_or_does_not_match_the_required_text), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                new LocalizableResourceString(nameof(FeaturesResources.A_source_file_is_missing_a_required_header), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected abstract AbstractFileHeaderHelper FileHeaderHelper { get; }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(HandleSyntaxTree);
        }

        public void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var root = context.Tree.GetRoot(context.CancellationToken);

            // don't process empty files
            if (root.FullSpan.IsEmpty)
            {
                return;
            }

            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree);
            if (!options.TryGetEditorConfigOption(CodeStyleOptions.FileHeaderTemplate, out var fileHeaderTemplate))
            {
                return;
            }

            var fileHeader = FileHeaderHelper.ParseFileHeader(root);
            if (fileHeader.IsMissing)
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, fileHeader.GetLocation(context.Tree)));
                return;
            }

            var expectedFileHeader = fileHeaderTemplate.Replace("{fileName}", Path.GetFileName(context.Tree.FilePath));
            if (!CompareCopyrightText(expectedFileHeader, fileHeader.CopyrightText))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, fileHeader.GetLocation(context.Tree)));
                return;
            }
        }

        private static bool CompareCopyrightText(string expectedFileHeader, string copyrightText)
        {
            // make sure that both \n and \r\n are accepted from the settings.
            var reformattedCopyrightTextParts = expectedFileHeader.Replace("\r\n", "\n").Split('\n');
            var fileHeaderCopyrightTextParts = copyrightText.Replace("\r\n", "\n").Split('\n');

            if (reformattedCopyrightTextParts.Length != fileHeaderCopyrightTextParts.Length)
            {
                return false;
            }

            // compare line by line, ignoring leading and trailing whitespace on each line.
            for (var i = 0; i < reformattedCopyrightTextParts.Length; i++)
            {
                if (string.CompareOrdinal(reformattedCopyrightTextParts[i].Trim(), fileHeaderCopyrightTextParts[i].Trim()) != 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
