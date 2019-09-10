// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive.Commands
{
    [UseExportProvider]
    public class InteractiveCommandHandlerTests
    {
        private const string Caret = "$$";
        private const string ExampleCode1 = @"var x = 1;";
        private const string ExampleCode2 =
@"var x = 1;
Task.Run(() => { return 1; });";
        private const string ExampleCode2Line2 =
@"Task.Run(() => { return 1; });";
        private const string ExampleCode3 =
@"Console.WriteLine(
    ""InteractiveCommandHandlerExample"");";

        private const string ExampleMultiline =
@"namespace N {
    void goo() {
        Console.WriteLine(
            $$""LLL"");
    }
}";
        private const string ExpectedMultilineSelection =
@"Console.WriteLine(
            ""LLL"");";

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void TestExecuteInInteractiveWithoutSelection()
        {
            var exportProvider = InteractiveWindowTestHost.ExportProviderFactory.CreateExportProvider();

            AssertExecuteInInteractive(exportProvider, Caret, new string[0]);
            AssertExecuteInInteractive(
                exportProvider,
@"var x = 1;
$$
var y = 2;", new string[0]);
            AssertExecuteInInteractive(exportProvider, ExampleCode1 + Caret, ExampleCode1);
            AssertExecuteInInteractive(exportProvider, ExampleCode1.Insert(3, Caret), ExampleCode1);
            AssertExecuteInInteractive(exportProvider, ExampleCode2 + Caret, ExampleCode2Line2);
            AssertExecuteInInteractive(exportProvider, ExampleMultiline, ExpectedMultilineSelection);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void TestExecuteInInteractiveWithEmptyBuffer()
        {
            var exportProvider = InteractiveWindowTestHost.ExportProviderFactory.CreateExportProvider();

            AssertExecuteInInteractive(
                exportProvider,
@"{|Selection:|}var x = 1;
{|Selection:$$|}var y = 2;", new string[0]);
            AssertExecuteInInteractive(exportProvider, $@"{{|Selection:{ExampleCode1}$$|}}", ExampleCode1);
            AssertExecuteInInteractive(exportProvider, $@"{{|Selection:{ExampleCode2}$$|}}", ExampleCode2);
            AssertExecuteInInteractive(
                exportProvider,
$@"var o = new object[] {{ 1, 2, 3 }};
Console.WriteLine(o);
{{|Selection:{ExampleCode2}$$|}}

Console.WriteLine(x);", ExampleCode2);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void TestExecuteInInteractiveWithBoxSelection()
        {
            var exportProvider = InteractiveWindowTestHost.ExportProviderFactory.CreateExportProvider();

            var expectedBoxSubmissionResult = @"int x;
int y;";
            AssertExecuteInInteractive(
                exportProvider,
$@"some text {{|Selection:$$int x;|}} also here
text some {{|Selection:int y;|}} here also", expectedBoxSubmissionResult);
            AssertExecuteInInteractive(
                exportProvider,
$@"some text {{|Selection:int x;$$|}} also here
text some {{|Selection:int y;|}} here also", expectedBoxSubmissionResult);
            AssertExecuteInInteractive(
                exportProvider,
$@"some text {{|Selection:int x;|}} also here
text some {{|Selection:$$int y;|}} here also", expectedBoxSubmissionResult);
            AssertExecuteInInteractive(
                exportProvider,
$@"some text {{|Selection:int x;|}} also here
text some {{|Selection:int y;$$|}} here also", expectedBoxSubmissionResult);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void TestExecuteInInteractiveWithNonEmptyBuffer()
        {
            var exportProvider = InteractiveWindowTestHost.ExportProviderFactory.CreateExportProvider();

            // Execute in interactive clears the existing current buffer before execution.
            // Therefore `var x = 1;` will not be executed.
            AssertExecuteInInteractive(
                exportProvider,
                @"{|Selection:var y = 2;$$|}",
                "var y = 2;",
                submissionBuffer: "var x = 1;");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/23200")]
        public void TestExecuteInInteractiveWithDefines()
        {
            var exportProvider = InteractiveWindowTestHost.ExportProviderFactory.CreateExportProvider();

            var exampleWithIfDirective =
@"#if DEF
public void $$Run()
{
}
#endif";

            AssertExecuteInInteractive(exportProvider, exampleWithIfDirective,
@"public void Run()");

            var exampleWithDefine =
$@"#define DEF
{exampleWithIfDirective}";

            AssertExecuteInInteractive(exportProvider, exampleWithDefine,
@"public void Run()
{
}");
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void TestCopyToInteractiveWithoutSelection()
        {
            var exportProvider = InteractiveWindowTestHost.ExportProviderFactory.CreateExportProvider();

            AssertCopyToInteractive(exportProvider, Caret, "");
            AssertCopyToInteractive(exportProvider, $"{ExampleCode2}$$", ExampleCode2Line2);
            AssertCopyToInteractive(
                exportProvider,
                code: ExampleCode2 + Caret,
                submissionBuffer: ExampleCode1,
                expectedBufferText: ExampleCode1 + "\r\n" + ExampleCode2Line2);
            AssertCopyToInteractive(
                exportProvider,
                code: ExampleCode2 + Caret,
                submissionBuffer: "x = 2;",
                expectedBufferText: "x = 2;\r\n" + ExampleCode2Line2);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void TestCopyToInteractive()
        {
            var exportProvider = InteractiveWindowTestHost.ExportProviderFactory.CreateExportProvider();

            AssertCopyToInteractive(exportProvider, $"{{|Selection:{ExampleCode2}$$|}}", ExampleCode2);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void TestCopyToInteractiveWithNonEmptyBuffer()
        {
            var exportProvider = InteractiveWindowTestHost.ExportProviderFactory.CreateExportProvider();

            // Copy to interactive does not clear the existing buffer.
            // Therefore `var x = 1;` will still be present in the final buffer.
            AssertCopyToInteractive(
                exportProvider,
                $"{{|Selection:{ExampleCode2}$$|}}",
                $"var x = 1;\r\n{ExampleCode2}",
                submissionBuffer: "var x = 1;");
        }

        private static void AssertCopyToInteractive(ExportProvider exportProvider, string code, string expectedBufferText, string submissionBuffer = null)
        {
            using var workspace = InteractiveWindowCommandHandlerTestState.CreateTestState(exportProvider, code);
            PrepareSubmissionBuffer(submissionBuffer, workspace);
            workspace.SendCopyToInteractive();
            Assert.Equal(expectedBufferText, workspace.WindowCurrentLanguageBuffer.CurrentSnapshot.GetText());
        }

        private static void AssertExecuteInInteractive(ExportProvider exportProvider, string code, string expectedSubmission, string submissionBuffer = null)
        {
            AssertExecuteInInteractive(exportProvider, code, new string[] { expectedSubmission }, submissionBuffer);
        }

        private static void AssertExecuteInInteractive(ExportProvider exportProvider, string code, string[] expectedSubmissions, string submissionBuffer = null)
        {
            var submissions = new List<string>();
            void appendSubmission(object _, string item) { submissions.Add(item.TrimEnd()); }

            using var workspace = InteractiveWindowCommandHandlerTestState.CreateTestState(exportProvider, code);
            PrepareSubmissionBuffer(submissionBuffer, workspace);
            Assert.Equal(VSCommanding.CommandState.Available, workspace.GetStateForExecuteInInteractive());

            workspace.Evaluator.OnExecute += appendSubmission;
            workspace.ExecuteInInteractive();
            AssertEx.Equal(expectedSubmissions, submissions);
        }

        private static void PrepareSubmissionBuffer(string submissionBuffer, InteractiveWindowCommandHandlerTestState workspace)
        {
            if (!string.IsNullOrEmpty(submissionBuffer))
            {
                workspace.WindowCurrentLanguageBuffer.Insert(0, submissionBuffer);
            }
        }
    }
}
