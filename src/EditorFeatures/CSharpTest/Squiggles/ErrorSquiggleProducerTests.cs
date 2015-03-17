// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Squiggles
{
    public class ErrorSquiggleProducerTests : AbstractSquiggleProducerTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public void ErrorTagGeneratedForError()
        {
            var spans = GetErrorSpans("class C {");
            Assert.Equal(1, spans.Count());

            var firstSpan = spans.First();
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public void ErrorTagGeneratedForWarning()
        {
            var spans = GetErrorSpans("class C { long x = 5l; }");
            Assert.Equal(1, spans.Count());
            Assert.Equal(PredefinedErrorTypeNames.Warning, spans.First().Tag.ErrorType);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public void ErrorTagGeneratedForWarningAsError()
        {
            var workspaceXml =
@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <CompilationOptions ReportDiagnostic = ""Error"" />
            <Document FilePath = ""Test.cs"" >
                class Program
                {
                    void Test()
                    {
                        int a = 5;
                    }
                }
        </Document>
    </Project>
</Workspace>";

            using (var workspace = TestWorkspaceFactory.CreateWorkspace(workspaceXml))
            {
                var spans = GetErrorSpans(workspace);

                Assert.Equal(1, spans.Count());
                Assert.Equal(PredefinedErrorTypeNames.SyntaxError, spans.First().Tag.ErrorType);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public void ErrorDoesNotCrashPastEOF()
        {
            var spans = GetErrorSpans("class C { int x =");
            Assert.Equal(3, spans.Count());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)]
        public void SemanticErrorReported()
        {
            var spans = GetErrorSpans("class C : Bar { }");
            Assert.Equal(1, spans.Count());

            var firstSpan = spans.First();
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType);
            Assert.Contains("Bar", (string)firstSpan.Tag.ToolTipContent, StringComparison.Ordinal);
        }

        private static IEnumerable<ITagSpan<IErrorTag>> GetErrorSpans(params string[] content)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(content))
            {
                return GetErrorSpans(workspace);
            }
        }
    }
}
