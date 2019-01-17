// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpFormatting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpFormatting(VisualStudioInstanceFactory instanceFactory)
            : base(nameof(CSharpFormatting))
        {
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Formatting)]
        public void AlignOpenBraceWithMethodDeclaration()
        {
            using (var telemetry = VisualStudioInstance.EnableTestTelemetryChannel())
            {
                SetUpEditor(@"
$$class C
{
    void Main()
     {
    }
}");

                VisualStudioInstance.Editor.FormatDocument();
                VisualStudioInstance.Editor.Verify.TextContains(@"
class C
{
    void Main()
    {
    }
}");
                telemetry.VerifyFired("vs/ide/vbcs/commandhandler/formatcommand");
            }
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Formatting)]
        public void FormatOnSemicolon()
        {
            SetUpEditor(@"
public class C
{
    void Goo()
    {
        var x =        from a             in       new List<int>()
    where x % 2 = 0
                      select x   ;$$
    }
}");

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Backspace, ";");
            VisualStudioInstance.Editor.Verify.TextContains(@"
public class C
{
    void Goo()
    {
        var x = from a in new List<int>()
                where x % 2 = 0
                select x;
    }
}");
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Formatting)]
        public void FormatSelection()
        {
            SetUpEditor(@"
public class C {
    public void M( ) {$$
        }
}");

            VisualStudioInstance.Editor.SelectTextInCurrentDocument("public void M( ) {");
            VisualStudioInstance.Editor.FormatSelection();
            VisualStudioInstance.Editor.Verify.TextContains(@"
public class C {
    public void M()
    {
    }
}");
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Formatting)]
        public void PasteCodeWithLambdaBody()
        {
            SetUpEditor(@"
using System;
class Program
{
    static void Main()
    {
        Action a = () =>
        {
            using (null)
            {
                $$
            }
        };
    }
}");
            VisualStudioInstance.Editor.Paste(@"        Action b = () =>
        {

            };");

            VisualStudioInstance.Editor.Verify.TextContains(@"
using System;
class Program
{
    static void Main()
    {
        Action a = () =>
        {
            using (null)
            {
                Action b = () =>
                {

                };
            }
        };
    }
}");
            // Undo should only undo the formatting
            VisualStudioInstance.Editor.Undo();
            VisualStudioInstance.Editor.Verify.TextContains(@"
using System;
class Program
{
    static void Main()
    {
        Action a = () =>
        {
            using (null)
            {
                        Action b = () =>
        {

            };
            }
        };
    }
}");
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Formatting)]
        public void PasteCodeWithLambdaBody2()
        {
            SetUpEditor(@"
using System;
class Program
{
    static void Main()
    {
        Action a = () =>
        {
            using (null)
            {
                $$
            }
        };
    }
}");
            VisualStudioInstance.Editor.Paste(@"        Action<int> b = n =>
        {
            Console.Writeline(n);
        };");

            VisualStudioInstance.Editor.Verify.TextContains(@"
using System;
class Program
{
    static void Main()
    {
        Action a = () =>
        {
            using (null)
            {
                Action<int> b = n =>
                {
                    Console.Writeline(n);
                };
            }
        };
    }
}");
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Formatting)]
        public void PasteCodeWithLambdaBody3()
        {
            SetUpEditor(@"
using System;
class Program
{
    static void Main()
    {
        Action a = () =>
        {
            using (null)
            {
                $$
            }
        };
    }
}");
            VisualStudioInstance.Editor.Paste(@"        D d = delegate(int x)
{
    return 2 * x;
};");

            VisualStudioInstance.Editor.Verify.TextContains(@"
using System;
class Program
{
    static void Main()
    {
        Action a = () =>
        {
            using (null)
            {
                D d = delegate (int x)
                {
                    return 2 * x;
                };
            }
        };
    }
}");
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/18065"), TestProperty(Traits.Feature, Traits.Features.Formatting)]
        public void ShiftEnterWithIntelliSenseAndBraceMatching()
        {
            SetUpEditor(@"
class Program
{
    object M(object bar)
    {
        return M$$
    }
}");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.Editor.SendKeys("(ba", new KeyPress(VirtualKey.Enter, ShiftState.Shift), "// comment");
            VisualStudioInstance.Editor.Verify.TextContains(@"
class Program
{
    object M(object bar)
    {
        return M(bar);
        // comment
    }
}");
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/30015")]
        [TestProperty(Traits.Feature, Traits.Features.EditorConfig)]
        [TestProperty(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(15003, "https://github.com/dotnet/roslyn/issues/15003")]
        public void ApplyEditorConfigAndFormatDocument()
        {
            var markup = @"
class C
{
    public int X1
    {
        get
        {
            $$return 3;
        }
    }
}";
            var expectedTextTwoSpaceIndent = @"
class C
{
  public int X1
  {
    get
    {
      return 3;
    }
  }
}";

            // CodingConventions only sends notifications if a file is open for all directories in the project
            VisualStudioInstance.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), @"Properties\AssemblyInfo.cs");

            // Switch back to the main document we'll be editing
            VisualStudioInstance.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), "Class1.cs");

            MarkupTestFile.GetSpans(markup, out var expectedTextFourSpaceIndent, out ImmutableArray<TextSpan> spans);
            SetUpEditor(markup);
            VisualStudioInstance.WaitForApplicationIdle(CancellationToken.None);

            /*
             * The first portion of this test verifies that Format Document uses the default indentation settings when
             * no .editorconfig is available.
             */

            VisualStudioInstance.Workspace.WaitForAllAsyncOperations(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService);
            VisualStudioInstance.Editor.FormatDocumentViaCommand();

            Assert.AreEqual(expectedTextFourSpaceIndent, VisualStudioInstance.Editor.GetText());

            /*
             * The second portion of this test adds a .editorconfig file to configure the indentation behavior, and
             * verifies that the next Format Document operation adheres to the formatting.
             */

            var editorConfig = @"root = true

[*.cs]
indent_size = 2
";

            VisualStudioInstance.SolutionExplorer.AddFile(new ProjectUtils.Project(ProjectName), ".editorconfig", editorConfig, open: false);

            // Wait for CodingConventions library events to propagate to the workspace
            VisualStudioInstance.WaitForApplicationIdle(CancellationToken.None);
            VisualStudioInstance.Workspace.WaitForAllAsyncOperations(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService);
            VisualStudioInstance.Editor.FormatDocumentViaCommand();

            Assert.AreEqual(expectedTextTwoSpaceIndent, VisualStudioInstance.Editor.GetText());

            /*
             * The third portion of this test modifies the existing .editorconfig file with a new indentation behavior,
             * and verifies that the next Format Document operation adheres to the updated formatting.
             */

            VisualStudioInstance.SolutionExplorer.SetFileContents(new ProjectUtils.Project(ProjectName), ".editorconfig", editorConfig.Replace("2", "4"));

            // Wait for CodingConventions library events to propagate to the workspace
            VisualStudioInstance.WaitForApplicationIdle(CancellationToken.None);
            VisualStudioInstance.Workspace.WaitForAllAsyncOperations(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService);
            VisualStudioInstance.Editor.FormatDocumentViaCommand();

            Assert.AreEqual(expectedTextFourSpaceIndent, VisualStudioInstance.Editor.GetText());
        }
    }
}
