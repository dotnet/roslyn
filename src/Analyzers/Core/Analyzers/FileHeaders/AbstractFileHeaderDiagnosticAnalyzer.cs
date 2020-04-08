// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.IO;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FileHeaders
{
    internal abstract class AbstractFileHeaderDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        protected AbstractFileHeaderDiagnosticAnalyzer(string language)
            : base(
                IDEDiagnosticIds.FileHeaderMismatch,
                CodeStyleOptions2.FileHeaderTemplate,
                language,
                new LocalizableResourceString(nameof(AnalyzersResources.The_file_header_is_missing_or_not_located_at_the_top_of_the_file), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                new LocalizableResourceString(nameof(AnalyzersResources.A_source_file_is_missing_a_required_header), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
            RoslynDebug.AssertNotNull(DescriptorId);

            var invalidHeaderTitle = new LocalizableResourceString(nameof(AnalyzersResources.The_file_header_does_not_match_the_required_text), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
            var invalidHeaderMessage = new LocalizableResourceString(nameof(AnalyzersResources.A_source_file_contains_a_header_that_does_not_match_the_required_text), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
            InvalidHeaderDescriptor = CreateDescriptorWithId(DescriptorId, invalidHeaderTitle, invalidHeaderMessage);
        }

        protected abstract AbstractFileHeaderHelper FileHeaderHelper { get; }

        internal DiagnosticDescriptor MissingHeaderDescriptor => Descriptor;

        internal DiagnosticDescriptor InvalidHeaderDescriptor { get; }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(HandleSyntaxTree);

        private void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var tree = context.Tree;
            var root = tree.GetRoot(context.CancellationToken);

            // don't process empty files
            if (root.FullSpan.IsEmpty)
            {
                return;
            }

            if (!context.Options.TryGetEditorConfigOption(CodeStyleOptions2.FileHeaderTemplate, tree, out string fileHeaderTemplate)
                || string.IsNullOrEmpty(fileHeaderTemplate))
            {
                return;
            }

            var fileHeader = FileHeaderHelper.ParseFileHeader(root);
            if (fileHeader.IsMissing)
            {
                context.ReportDiagnostic(Diagnostic.Create(MissingHeaderDescriptor, fileHeader.GetLocation(tree)));
                return;
            }

            var expectedFileHeader = fileHeaderTemplate.Replace("{fileName}", Path.GetFileName(tree.FilePath));
            if (!CompareCopyrightText(expectedFileHeader, fileHeader.CopyrightText))
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidHeaderDescriptor, fileHeader.GetLocation(tree)));
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
