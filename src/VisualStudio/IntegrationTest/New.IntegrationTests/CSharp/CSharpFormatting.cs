// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.Formatting)]
public class CSharpFormatting : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpFormatting()
        : base(nameof(CSharpFormatting))
    {
    }

    [IdeFact]
    public async Task AlignOpenBraceWithMethodDeclaration()
    {
        await using var telemetry = await TestServices.Telemetry.EnableTestTelemetryChannelAsync(HangMitigatingCancellationToken);
        await SetUpEditorAsync("""

            $$class C
            {
                void Main()
                 {
                }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Editor.FormatDocumentAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("""

            class C
            {
                void Main()
                {
                }
            }
            """, cancellationToken: HangMitigatingCancellationToken);
        await telemetry.VerifyFiredAsync(["vs/ide/vbcs/commandhandler/formatcommand"], HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task FormatOnSemicolon()
    {
        await SetUpEditorAsync("""

            public class C
            {
                void Goo()
                {
                    var x =        from a             in       new List<int>()
                where x % 2 = 0
                                  select x   ;$$
                }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync([VirtualKeyCode.BACK, ';'], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("""

            public class C
            {
                void Goo()
                {
                    var x = from a in new List<int>()
                            where x % 2 = 0
                            select x;
                }
            }
            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task FormatSelection()
    {
        await SetUpEditorAsync("""

            public class C {
                public void M( ) {$$
                    }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Editor.SelectTextInCurrentDocumentAsync("public void M( ) {", HangMitigatingCancellationToken);
        await TestServices.Editor.FormatSelectionAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("""

            public class C {
                public void M()
                {
                }
            }
            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task PasteCodeWithLambdaBody()
    {
        await SetUpEditorAsync("""

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
            }
            """, HangMitigatingCancellationToken);
        await TestServices.Editor.PasteAsync("""
                    Action b = () =>
                    {

                        };
            """, HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""

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
            }
            """, cancellationToken: HangMitigatingCancellationToken);
        // Undo should only undo the formatting
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("""

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
            }
            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task PasteCodeWithLambdaBody2()
    {
        await SetUpEditorAsync("""

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
            }
            """, HangMitigatingCancellationToken);
        await TestServices.Editor.PasteAsync("""
                    Action<int> b = n =>
                    {
                        Console.Writeline(n);
                    };
            """, HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""

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
            }
            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task PasteCodeWithLambdaBody3()
    {
        await SetUpEditorAsync("""

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
            }
            """, HangMitigatingCancellationToken);
        await TestServices.Editor.PasteAsync("""
                    D d = delegate(int x)
            {
                return 2 * x;
            };
            """, HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""

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
            }
            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task ShiftEnterWithIntelliSenseAndBraceMatching()
    {
        await SetUpEditorAsync("""

            class Program
            {
                object M(object bar)
                {
                    return M$$
                }
            }
            """, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(["(ba", (VirtualKeyCode.RETURN, VirtualKeyCode.SHIFT), "// comment"], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("""

            class Program
            {
                object M(object bar)
                {
                    return M(bar);
                    // comment
                }
            }
            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    [Trait(Traits.Feature, Traits.Features.EditorConfig)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/15003")]
    public async Task ApplyEditorConfigAndFormatDocument()
    {
        var markup = """

            class C
            {
                public int X1
                {
                    get
                    {
                        $$return 3;
                    }
                }
            }
            """;
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out var expectedTextFourSpaceIndent, out _);
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);

        /*
         * The first portion of this test verifies that Format Document uses the default indentation settings when
         * no .editorconfig is available.
         */

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles
            ],
            HangMitigatingCancellationToken);
        await TestServices.Editor.FormatDocumentAsync(HangMitigatingCancellationToken);

        Assert.Equal(expectedTextFourSpaceIndent, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

        /*
         * The second portion of this test adds a .editorconfig file to configure the indentation behavior, and
         * verifies that the next Format Document operation adheres to the formatting.
         */

        var editorConfig = """
            root = true

            [*.cs]
            indent_size = 2

            """;

        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, ".editorconfig", editorConfig, open: false, HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles
            ],
            HangMitigatingCancellationToken);
        await TestServices.Editor.FormatDocumentAsync(HangMitigatingCancellationToken);

        Assert.Equal("""

            class C
            {
              public int X1
              {
                get
                {
                  return 3;
                }
              }
            }
            """, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

        /*
         * The third portion of this test modifies the existing .editorconfig file with a new indentation behavior,
         * and verifies that the next Format Document operation adheres to the updated formatting.
         */

        await TestServices.SolutionExplorer.SetFileContentsAsync(ProjectName, ".editorconfig", editorConfig.Replace("2", "4"), HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles
            ],
            HangMitigatingCancellationToken);
        await TestServices.Editor.FormatDocumentAsync(HangMitigatingCancellationToken);

        Assert.Equal(expectedTextFourSpaceIndent, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }
}
