﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpFormatting : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpFormatting(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpFormatting))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void AlignOpenBraceWithMethodDeclaration()
        {
            using (var telemetry = VisualStudio.EnableTestTelemetryChannel())
            {
                SetUpEditor(@"
$$class C
{
    void Main()
     {
    }
}");

                VisualStudio.Editor.FormatDocument();
                VisualStudio.Editor.Verify.TextContains(@"
class C
{
    void Main()
    {
    }
}");
                telemetry.VerifyFired("vs/ide/vbcs/commandhandler/formatcommand");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
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

            VisualStudio.Editor.SendKeys(VirtualKey.Backspace, ";");
            VisualStudio.Editor.Verify.TextContains(@"
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatSelection()
        {
            SetUpEditor(@"
public class C {
    public void M( ) {$$
        }
}");

            VisualStudio.Editor.SelectTextInCurrentDocument("public void M( ) {");
            VisualStudio.Editor.FormatSelection();
            VisualStudio.Editor.Verify.TextContains(@"
public class C {
    public void M()
    {
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
            VisualStudio.Editor.Paste(@"        Action b = () =>
        {

            };");

            VisualStudio.Editor.Verify.TextContains(@"
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
            VisualStudio.Editor.Undo();
            VisualStudio.Editor.Verify.TextContains(@"
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
            VisualStudio.Editor.Paste(@"        Action<int> b = n =>
        {
            Console.Writeline(n);
        };");

            VisualStudio.Editor.Verify.TextContains(@"
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
            VisualStudio.Editor.Paste(@"        D d = delegate(int x)
{
    return 2 * x;
};");

            VisualStudio.Editor.Verify.TextContains(@"
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

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/18065"),
         Trait(Traits.Feature, Traits.Features.Formatting)]
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
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.Editor.SendKeys("(ba", new KeyPress(VirtualKey.Enter, ShiftState.Shift), "// comment");
            VisualStudio.Editor.Verify.TextContains(@"
class Program
{
    object M(object bar)
    {
        return M(bar);
        // comment
    }
}");
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.EditorConfig)]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
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

            VisualStudio.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), "Class1.cs");

            MarkupTestFile.GetSpans(markup, out var expectedTextFourSpaceIndent, out ImmutableArray<TextSpan> _);
            SetUpEditor(markup);

            /*
             * The first portion of this test verifies that Format Document uses the default indentation settings when
             * no .editorconfig is available.
             */

            VisualStudio.Workspace.WaitForAllAsyncOperations(
                Helper.HangMitigatingTimeout,
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles);
            VisualStudio.Editor.FormatDocumentViaCommand();

            Assert.Equal(expectedTextFourSpaceIndent, VisualStudio.Editor.GetText());

            /*
             * The second portion of this test adds a .editorconfig file to configure the indentation behavior, and
             * verifies that the next Format Document operation adheres to the formatting.
             */

            var editorConfig = @"root = true

[*.cs]
indent_size = 2
";

            VisualStudio.SolutionExplorer.AddFile(new ProjectUtils.Project(ProjectName), ".editorconfig", editorConfig, open: false);

            VisualStudio.Workspace.WaitForAllAsyncOperations(
                Helper.HangMitigatingTimeout,
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles);
            VisualStudio.Editor.FormatDocumentViaCommand();

            Assert.Equal(expectedTextTwoSpaceIndent, VisualStudio.Editor.GetText());

            /*
             * The third portion of this test modifies the existing .editorconfig file with a new indentation behavior,
             * and verifies that the next Format Document operation adheres to the updated formatting.
             */

            VisualStudio.SolutionExplorer.SetFileContents(new ProjectUtils.Project(ProjectName), ".editorconfig", editorConfig.Replace("2", "4"));

            VisualStudio.Workspace.WaitForAllAsyncOperations(
                Helper.HangMitigatingTimeout,
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles);
            VisualStudio.Editor.FormatDocumentViaCommand();

            Assert.Equal(expectedTextFourSpaceIndent, VisualStudio.Editor.GetText());
        }
    }
}
