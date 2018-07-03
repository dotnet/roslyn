// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpFormatting : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpFormatting()
            : base(nameof(CSharpFormatting))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task AlignOpenBraceWithMethodDeclarationAsync()
        {
            using (var telemetry = await VisualStudio.VisualStudio.EnableTestTelemetryChannelAsync())
            {
                await SetUpEditorAsync(@"
$$class C
{
    void Main()
     {
    }
}");

                await VisualStudio.Editor.FormatDocumentAsync();
                await VisualStudio.Editor.Verify.TextContainsAsync(@"
class C
{
    void Main()
    {
    }
}");
                await telemetry.VerifyFiredAsync("vs/ide/vbcs/commandhandler/formatcommand");
            }
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatOnSemicolonAsync()
        {
            await SetUpEditorAsync(@"
public class C
{
    void Goo()
    {
        var x =        from a             in       new List<int>()
    where x % 2 = 0
                      select x   ;$$
    }
}");

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Backspace, ";");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
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

        [IdeFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatSelectionAsync()
        {
            await SetUpEditorAsync(@"
public class C {
    public void M( ) {$$
        }
}");

            await VisualStudio.Editor.SelectTextInCurrentDocumentAsync("public void M( ) {");
            await VisualStudio.Editor.FormatSelectionAsync();
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
public class C {
    public void M()
    {
    }
}");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task PasteCodeWithLambdaBodyAsync()
        {
            await SetUpEditorAsync(@"
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
            await VisualStudio.Editor.PasteAsync(@"        Action b = () =>
        {

            };");

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
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
            await VisualStudio.Editor.UndoAsync();
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
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

        [IdeFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task PasteCodeWithLambdaBody2Async()
        {
            await SetUpEditorAsync(@"
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
            await VisualStudio.Editor.PasteAsync(@"        Action<int> b = n =>
        {
            Console.Writeline(n);
        };");

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
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

        [IdeFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task PasteCodeWithLambdaBody3Async()
        {
            await SetUpEditorAsync(@"
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
            await VisualStudio.Editor.PasteAsync(@"        D d = delegate(int x)
{
    return 2 * x;
};");

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
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

        [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/18065")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ShiftEnterWithIntelliSenseAndBraceMatchingAsync()
        {
            await SetUpEditorAsync(@"
class Program
{
    object M(object bar)
    {
        return M$$
    }
}");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.Editor.SendKeysAsync("(ba", new KeyPress(VirtualKey.Enter, ShiftState.Shift), "// comment");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class Program
{
    object M(object bar)
    {
        return M(bar);
        // comment
    }
}");
        }

        [IdeFact]
        [Trait(Traits.Feature, Traits.Features.EditorConfig)]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(15003, "https://github.com/dotnet/roslyn/issues/15003")]
        public async Task ApplyEditorConfigAndFormatDocumentAsync()
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
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, @"Properties\AssemblyInfo.cs");

            // Switch back to the main document we'll be editing
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs");

            MarkupTestFile.GetSpans(markup, out var expectedTextFourSpaceIndent, out ImmutableArray<TextSpan> spans);
            await SetUpEditorAsync(markup);
            await VisualStudio.VisualStudio.WaitForApplicationIdleAsync(CancellationToken.None);

            /*
             * The first portion of this test verifies that Format Document uses the default indentation settings when
             * no .editorconfig is available.
             */

            await VisualStudio.Workspace.WaitForAllAsyncOperationsAsync(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService);
            await VisualStudio.Editor.FormatDocumentViaCommandAsync();

            Assert.Equal(expectedTextFourSpaceIndent, await VisualStudio.Editor.GetTextAsync());

            /*
             * The second portion of this test adds a .editorconfig file to configure the indentation behavior, and
             * verifies that the next Format Document operation adheres to the formatting.
             */

            var editorConfig = @"root = true

[*.cs]
indent_size = 2
";

            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, ".editorconfig", editorConfig, open: false);

            // Wait for CodingConventions library events to propagate to the workspace
            await VisualStudio.VisualStudio.WaitForApplicationIdleAsync(CancellationToken.None);
            await VisualStudio.Workspace.WaitForAllAsyncOperationsAsync(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService);
            await VisualStudio.Editor.FormatDocumentViaCommandAsync();

            Assert.Equal(expectedTextTwoSpaceIndent, await VisualStudio.Editor.GetTextAsync());

            /*
             * The third portion of this test modifies the existing .editorconfig file with a new indentation behavior,
             * and verifies that the next Format Document operation adheres to the updated formatting.
             */

            VisualStudio.SolutionExplorer.SetFileContents(ProjectName, ".editorconfig", editorConfig.Replace("2", "4"));

            // Wait for CodingConventions library events to propagate to the workspace
            await VisualStudio.VisualStudio.WaitForApplicationIdleAsync(CancellationToken.None);
            await VisualStudio.Workspace.WaitForAllAsyncOperationsAsync(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService);
            await VisualStudio.Editor.FormatDocumentViaCommandAsync();

            Assert.Equal(expectedTextFourSpaceIndent, await VisualStudio.Editor.GetTextAsync());
        }
    }
}
