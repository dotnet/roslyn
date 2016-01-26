// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive.Commands
{
    internal class InteractiveCommandHandlerTests
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
    void foo() {
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
            AssertExecuteInInteractive(Caret, new string[0]);
            AssertExecuteInInteractive(ExampleCode1 + Caret, ExampleCode1);
            AssertExecuteInInteractive(ExampleCode1.Insert(3, Caret), ExampleCode1);
            AssertExecuteInInteractive(ExampleCode2 + Caret, ExampleCode2Line2);
            AssertExecuteInInteractive(ExampleMultiline, ExpectedMultilineSelection);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void TestExecuteInInteractiveWithEmptyBuffer()
        {
            AssertExecuteInInteractive($@"{{|Selection:{ExampleCode1}$$|}}", ExampleCode1);
            AssertExecuteInInteractive($@"{{|Selection:{ExampleCode2}$$|}}", ExampleCode2);
            AssertExecuteInInteractive(
$@"var o = new object[] {{ 1, 2, 3 }};
Console.WriteLine(o);
{{|Selection:{ExampleCode2}$$|}}

Console.WriteLine(x);", ExampleCode2);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void TestExecuteInInteractiveWithBoxSelection()
        {
            string expectedBoxSubmissionResult = @"int x;
int y;";
            AssertExecuteInInteractive(
$@"some text {{|Selection:$$int x;|}} also here
text some {{|Selection:int y;|}} here also", expectedBoxSubmissionResult);
            AssertExecuteInInteractive(
$@"some text {{|Selection:int x;$$|}} also here
text some {{|Selection:int y;|}} here also", expectedBoxSubmissionResult);
            AssertExecuteInInteractive(
$@"some text {{|Selection:int x;|}} also here
text some {{|Selection:$$int y;|}} here also", expectedBoxSubmissionResult);
            AssertExecuteInInteractive(
$@"some text {{|Selection:int x;|}} also here
text some {{|Selection:int y;$$|}} here also", expectedBoxSubmissionResult);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void TestExecuteInInteractiveWithNonEmptyBuffer()
        {
            // Execute in interactive clears the existing current buffer before execution.
            // Therefore `var x = 1;` will not be executed.
            AssertExecuteInInteractive(
                @"{|Selection:var y = 2;$$|}",
                "var y = 2;",
                submissionBuffer: "var x = 1;");
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void TestCopyToInteractiveWithoutSelection()
        {
            AssertCopyToInteractive(Caret, "");
            AssertCopyToInteractive($"{ExampleCode2}$$", ExampleCode2Line2);
            AssertCopyToInteractive(
                code: ExampleCode2 + Caret,
                submissionBuffer: ExampleCode1,
                expectedBufferText: ExampleCode1 + "\r\n" + ExampleCode2Line2);
            AssertCopyToInteractive(
                code: ExampleCode2 + Caret,
                submissionBuffer: "x = 2;",
                expectedBufferText: "x = 2;\r\n" + ExampleCode2Line2);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void TestCopyToInteractive()
        {
            AssertCopyToInteractive($"{{|Selection:{ExampleCode2}$$|}}", ExampleCode2);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Interactive)]
        public void TestCopyToInteractiveWithNonEmptyBuffer()
        {
          // Copy to interactive does not clear the existing buffer.
          // Therefore `var x = 1;` will still be present in the final buffer.
            AssertCopyToInteractive(
                $"{{|Selection:{ExampleCode2}$$|}}",
                $"var x = 1;\r\n{ExampleCode2}",
                submissionBuffer: "var x = 1;");
        }

        private static void AssertCopyToInteractive(string code, string expectedBufferText, string submissionBuffer = null)
        {
            using (var workspace = InteractiveWindowCommandHandlerTestState.CreateTestState(code))
            {
                PrepareSubmissionBuffer(submissionBuffer, workspace);
                workspace.SendCopyToInteractive();
                Assert.Equal(expectedBufferText, workspace.WindowCurrentLanguageBuffer.CurrentSnapshot.GetText());
            }
        }

        private static void AssertExecuteInInteractive(string code, string expectedSubmission, string submissionBuffer = null)
        {
            AssertExecuteInInteractive(code, new string[] { expectedSubmission }, submissionBuffer);
        }

        private static void AssertExecuteInInteractive(string code, string[] expectedSubmissions, string submissionBuffer = null)
        {
            List<string> submissions = new List<string>();
            EventHandler<string> appendSubmission = (_, item) => { submissions.Add(item.TrimEnd()); };

            using (var workspace = InteractiveWindowCommandHandlerTestState.CreateTestState(code))
            {
                PrepareSubmissionBuffer(submissionBuffer, workspace);
                Assert.Equal(CommandState.Available, workspace.GetStateForExecuteInInteractive());

                workspace.Evaluator.OnExecute += appendSubmission;
                workspace.ExecuteInInteractive();
                AssertEx.Equal(expectedSubmissions, submissions);
            }
        }

        private static void PrepareSubmissionBuffer(string submissionBuffer, InteractiveWindowCommandHandlerTestState workspace)
        {
            if (string.IsNullOrEmpty(submissionBuffer))
            {
                return;
            }

            workspace.WindowCurrentLanguageBuffer.Insert(0, submissionBuffer);
        }
    }
}
