// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Editor.Venus;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.EditorUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Venus
{
    [UseExportProvider]
    public class ContainedDocumentBuffersTests
    {
        // Indent size used by the tests.
        private const int IndentSize = 2;

        private string AdjustIndentation(
            string razorBufferMarkup,
            string languageBufferMarkup,
            int[] spansToAdjust,
            string languageName,
            string languageContentTypeName,
            int tabSize = 4)
        {
            // check that line breaks are consistent:
            var razorLineBreak = razorBufferMarkup.GetLineBreak();
            var languageLineBreak = languageBufferMarkup.GetLineBreak();
            if (razorLineBreak != null && languageLineBreak != null)
            {
                Assert.Equal(razorLineBreak, languageLineBreak);
            }

            var lineBreak = razorLineBreak ?? languageLineBreak ?? "\r\n";

            var exportProvider = TestExportProvider.ExportProviderWithCSharpAndVisualBasic;
            var editorOptionsFactoryService = exportProvider.GetExport<IEditorOptionsFactoryService>().Value;
            var differenceSelectorService = exportProvider.GetExport<ITextDifferencingSelectorService>().Value;
            var contentTypeRegistry = exportProvider.GetExport<IContentTypeRegistryService>().Value;
            var razorContentType = contentTypeRegistry.GetContentType("Razor");
            var languageContentType = contentTypeRegistry.GetContentType(languageContentTypeName);

            using var workspace = new TestWorkspace(exportProvider);

            var languageServices = workspace.Services.GetLanguageServices(languageName);
            var contentTypeService = languageServices.GetService<IContentTypeLanguageService>();
            var syntaxFacts = languageServices.GetService<ISyntaxFactsService>();
            var projectionBufferFactory = workspace.GetService<IProjectionBufferFactoryService>();

            // Construct document for Razor text
            MarkupTestFile.GetNamedSpans(razorBufferMarkup, out var razorBufferText, out var razorSpans);
            var razorTextBuffer = EditorFactory.CreateBuffer(exportProvider, razorContentType, razorBufferText);
            var razorBaseDocument = new TestHostDocument(
                razorBufferText,
                selectedSpans: razorSpans,
                textBuffer: razorTextBuffer,
                textLoader: TextLoader.From(razorTextBuffer.AsTextContainer(), VersionStamp.Default));

            // Construct document for language text
            MarkupTestFile.GetNamedSpans(languageBufferMarkup, out var languageBufferText, out var languageSpans);
            var languageTextBuffer = EditorFactory.CreateBuffer(exportProvider, languageContentType, languageBufferText);
            var languageBaseDocument = new TestHostDocument(
                languageBufferText,
                selectedSpans: languageSpans,
                textBuffer: languageTextBuffer,
                textLoader: TextLoader.From(languageTextBuffer.AsTextContainer(), VersionStamp.Default));

            ValidateMarkupSpans(razorBufferText, razorSpans, languageBufferText, languageSpans);

            var diffService = (contentTypeService != null) ? differenceSelectorService.GetTextDifferencingService(contentTypeService.GetDefaultContentType()) : null;
            diffService ??= differenceSelectorService.DefaultTextDifferencingService;

            // Creates a new buffer for the surrounding generated text.
            // Projects language code snippets from Razor buffer and surrounding code from the newly created buffer.
            var languageProjection = workspace.CreateProjectionBuffer(
                languageBufferMarkup,
                new[] { razorBaseDocument },
                text => EditorFactory.CreateBuffer(exportProvider, languageContentType, text));

            var languageProjectionDocument = new TestHostDocument(
                languageBufferText,
                selectedSpans: languageSpans,
                textBuffer: languageProjection,
                textLoader: TextLoader.From(languageProjection.AsTextContainer(), VersionStamp.Default));

            // Projects language code snippets from language base buffer
            var xhtmlProjection = workspace.CreateProjectionBuffer(razorBufferMarkup, new[] { languageProjectionDocument },
                text =>
                {
                    Assert.Equal(text, razorBufferText);
                    return razorBaseDocument.GetTextBuffer();
                });

            // Create project
            workspace.AddTestProject(new TestHostProject(workspace, languageProjectionDocument));

            // set options on the XHTML projection buffer, the contained buffer logic uses these:
            var editorOptions = editorOptionsFactoryService.GetOptions(xhtmlProjection);
            editorOptions.SetOptionValue(DefaultOptions.IndentSizeOptionId, IndentSize);
            editorOptions.SetOptionValue(DefaultOptions.TabSizeOptionId, tabSize);
            editorOptions.SetOptionValue(DefaultOptions.NewLineCharacterOptionId, lineBreak);

            string GetBaseIndentation(int lineNumberInLanguageBuffer)
            {
                var languageLine = languageTextBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumberInLanguageBuffer);
                var languageLineSpan = new TextSpan(languageLine.Start, languageLine.Length);

                // find the language markup span that intersects with the requested line:
                var (spanName, _) = languageSpans.SingleOrDefault(namedSpan => namedSpan.Value.Single().IntersectsWith(languageLineSpan));
                if (spanName == null)
                {
                    return string.Empty;
                }

                // find the corresponding markup span in Razor:
                var razorMarkupSpan = razorSpans[spanName].Single();
                var razorLineNumber = razorTextBuffer.CurrentSnapshot.GetLineNumberFromPosition(razorMarkupSpan.Start);
                var razorLine = razorTextBuffer.CurrentSnapshot.GetLineFromLineNumber(razorLineNumber);

                // calculate the offset of the start of the span to the start of the line:
                return new string(' ', razorMarkupSpan.Start - razorLine.Start.Position);
            }

            var containedBuffers = new ContainedDocumentBuffers(
                subjectBuffer: languageProjection,
                dataBuffer: xhtmlProjection,
                languageName,
                languageProjectionDocument.Id,
                diffService,
                editorOptionsFactoryService,
                syntaxFacts,
                vbHelperFormattingRule: null,
                hostIndentationProvider: GetBaseIndentation);

            containedBuffers.AdjustIndentation(spansToAdjust, workspace.CurrentSolution);

            return languageProjection.CurrentSnapshot.GetText();
        }

        private static void ValidateMarkupSpans(
            string razorBufferText,
            IDictionary<string, ImmutableArray<TextSpan>> razorSpans,
            string languageBufferText,
            IDictionary<string, ImmutableArray<TextSpan>> languageSpans)
        {
            // content of markup spans matches exactly:
            Assert.Equal(razorSpans.Count, languageSpans.Count);

            var matchingSubstrings =
                (from razorSpan in razorSpans
                 join languageSpan in languageSpans on razorSpan.Key equals languageSpan.Key
                 select (razor: Substring(razorBufferText, razorSpan.Value.Single()), language: Substring(languageBufferText, languageSpan.Value.Single()))).ToArray();

            AssertEx.Equal(matchingSubstrings.Select(m => m.razor), matchingSubstrings.Select(m => m.language));
        }

        private static string Substring(string str, TextSpan span)
            => str.Substring(span.Start, span.Length);

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Venus)]
        [InlineData("\r\n")]
        [InlineData("\n")]
        [InlineData("\r")]
        public void Razor_NoNewLine(string lineBreak)
        {
            var razorSource =
@"<div>
    @{{|S1:int x = 1;|}}
</div>".NormalizeLineEndings(lineBreak);

            var generatedSource =
@"class C
{
    void F()
    {
{|S1:int x = 1;|}
    }
}".NormalizeLineEndings(lineBreak);

            var spansToAdjust = new[] { 0 };

            var updatedSource = AdjustIndentation(razorSource, generatedSource, spansToAdjust, LanguageNames.CSharp, ContentTypeNames.CSharpContentType);
            AssertEx.Equal(
@"class C
{
    void F()
    {
int x = 1;
    }
}".NormalizeLineEndings(lineBreak),
                updatedSource);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Venus)]
        [InlineData("\r\n")]
        [InlineData("\n")]
        [InlineData("\r")]
        public void Razor_OnSingleLine(string lineBreak)
        {
            var razorSource =
@"<div>
        @{{|S1:
int x = 1;
|}}
</div>".NormalizeLineEndings(lineBreak);

            var generatedSource =
@"public class C
{
    void F()
    {
#line ""file.cshtml"", 1{|S1:
int x = 1;
|}#line hidden
#line default
    }
}".NormalizeLineEndings(lineBreak);

            var spansToAdjust = new[] { 0 };

            var updatedSource = AdjustIndentation(razorSource, generatedSource, spansToAdjust, LanguageNames.CSharp, ContentTypeNames.CSharpContentType);
            AssertEx.Equal(
@"public class C
{
    void F()
    {
#line ""file.cshtml"", 1
int x = 1;



#line hidden
#line default
    }
}".NormalizeLineEndings(lineBreak),
                updatedSource);
        }

        [WpfTheory, Trait(Traits.Feature, Traits.Features.Venus)]
        [InlineData("\r\n")]
        [InlineData("\n")]
        [InlineData("\r")]
        public void Razor_OnMultipleLines(string lineBreak)
        {
            var razorSource =
@"<div>
        @{{|S1:
if(true)
{
int x = 1;
}
|}}
</div>".NormalizeLineEndings(lineBreak);

            var generatedSource =
@"public class C
{
    void F()
    {
#line ""file.cshtml"", 1{|S1:
if(true)
{
int x = 1;
}
|}#line hidden
#line default
    }
}".NormalizeLineEndings(lineBreak);

            var spansToAdjust = new[] { 0 };

            var updatedSource = AdjustIndentation(razorSource, generatedSource, spansToAdjust, LanguageNames.CSharp, ContentTypeNames.CSharpContentType);
            AssertEx.Equal(
@"public class C
{
    void F()
    {
#line ""file.cshtml"", 1
if(true)
            {
              int x = 1;
            }



#line hidden
#line default
    }
}".NormalizeLineEndings(lineBreak),
                updatedSource);
        }
    }
}

