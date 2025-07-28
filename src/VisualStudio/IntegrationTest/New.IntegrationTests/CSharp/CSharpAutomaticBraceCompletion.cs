// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
public class CSharpAutomaticBraceCompletion : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpAutomaticBraceCompletion()
        : base(nameof(CSharpAutomaticBraceCompletion))
    {
    }

    [IdeTheory(Skip = "https://github.com/dotnet/roslyn/issues/63576"), CombinatorialData]
    public async Task Braces_InsertionAndTabCompleting(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
                void Goo() {
                    $$
                }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("if (true) {", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        if (true) { $$}", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        if (true) { }$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Braces_Overtyping(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
                void Goo() {
                    $$
                }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("if (true) {", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        if (true) { $$}", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("}", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        if (true) { }$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    /// <summary>
    /// This is a muscle-memory test for users who rely on the following sequence:
    /// <list type="number">
    /// <item><description><c>Enter</c></description></item>
    /// <item><description><c>{</c></description></item>
    /// <item><description><c>Enter</c></description></item>
    /// <item><description><c>}</c></description></item>
    /// </list>
    /// </summary>
    [IdeFact]
    public async Task Braces_Overtyping_Method()
    {
        await SetUpEditorAsync("""

            class C {
                $$
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("public void A()", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.RETURN, '{', VirtualKeyCode.RETURN, '}'], HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CurrentLineTextAsync("    }$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    /// <summary>
    /// This is a muscle-memory test for users who rely on the following sequence:
    /// <list type="number">
    /// <item><description><c>Enter</c></description></item>
    /// <item><description><c>{</c></description></item>
    /// <item><description><c>Enter</c></description></item>
    /// <item><description><c>}</c></description></item>
    /// </list>
    /// </summary>
    [IdeFact]
    public async Task Braces_Overtyping_Property()
    {
        await SetUpEditorAsync("""

            class C {
                $$
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("public int X", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.RETURN, '{', VirtualKeyCode.RETURN, '}'], HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CurrentLineTextAsync("    }$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    /// <summary>
    /// This is a muscle-memory test for users who rely on the following sequence:
    /// <list type="number">
    /// <item><description><c>Enter</c></description></item>
    /// <item><description><c>{</c></description></item>
    /// <item><description><c>Enter</c></description></item>
    /// <item><description><c>}</c></description></item>
    /// </list>
    /// </summary>
    [IdeFact]
    public async Task Braces_Overtyping_CollectionInitializer()
    {
        await SetUpEditorAsync("""

            using System.Collections.Generic;
            class C {
                void Method() {
                    $$
                }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("var x = new List<string>()", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.RETURN, '{', VirtualKeyCode.RETURN, '}'], HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CurrentLineTextAsync("        }$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    /// <summary>
    /// This is a muscle-memory test for users who rely on the following sequence:
    /// <list type="number">
    /// <item><description><c>Enter</c></description></item>
    /// <item><description><c>{</c></description></item>
    /// <item><description><c>Enter</c></description></item>
    /// <item><description><c>}</c></description></item>
    /// </list>
    /// </summary>
    [IdeFact]
    public async Task Braces_Overtyping_ObjectInitializer()
    {
        await SetUpEditorAsync("""

            class C {
                void Method() {
                    $$
                }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("var x = new object()", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.RETURN, '{', VirtualKeyCode.RETURN, '}'], HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CurrentLineTextAsync("        }$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Braces_OnReturnNoFormattingOnlyIndentationBeforeCloseBrace(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
                void Goo() {
                    $$
                }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(
            [
                "if (true) {",
                VirtualKeyCode.RETURN,
                "var a = 1;",
            ],
            HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""

            class C {
                void Goo() {
                    if (true)
                    {
                        var a = 1;$$
                    }
                }
            }
            """,
assertCaretPosition: true,
HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Braces_OnReturnOvertypingTheClosingBrace(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
                void Goo() {
                    $$
                }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(
            [
                "if (true) {",
                VirtualKeyCode.RETURN,
                "var a = 1;",
                '}',
            ],
            HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""

            class C {
                void Goo() {
                    if (true)
                    {
                        var a = 1;
                    }$$
                }
            }
            """,
assertCaretPosition: true,
HangMitigatingCancellationToken);
    }

    [WorkItem(653540, "DevDiv")]
    [IdeTheory, CombinatorialData]
    public async Task Braces_OnReturnWithNonWhitespaceSpanInside(bool showCompletionInArgumentLists)
    {
        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(
            [
                "class A { int i;",
                VirtualKeyCode.RETURN,
            ],
            HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextContainsAsync("""
            class A { int i;
            $$}
            """,
assertCaretPosition: true,
HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Paren_InsertionAndTabCompleting(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
                $$
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("void Goo(", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    void Goo($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(["int x", VirtualKeyCode.TAB], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    void Goo(int x)$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Paren_Overtyping(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
                $$
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(
            [
                "void Goo(",
                VirtualKeyCode.ESCAPE,
                ")",
            ],
            HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CurrentLineTextAsync("    void Goo()$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory(Skip = "https://github.com/dotnet/roslyn/issues/70671"), CombinatorialData]
    public async Task SquareBracket_Insertion(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
                $$
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("int [", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    int[$$] ", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory(Skip = "https://github.com/dotnet/roslyn/issues/70671"), CombinatorialData]
    public async Task SquareBracket_Overtyping(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C { 
                $$
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(["int [", ']'], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    int[]$$ ", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task DoubleQuote_InsertionAndTabCompletion(bool showCompletionInArgumentLists)

    {
        await SetUpEditorAsync("""

            class C {
                $$
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(["""
            string str = "
            """, VirtualKeyCode.TAB], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    string str = \"\"$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task DoubleQuote_InsertionAndOvertyping(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
                $$
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(["string str = \"Hi Roslyn!", '"'], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    string str = \"Hi Roslyn!\"$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task DoubleQuote_FixedInterpolatedVerbatimString(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C
            {
                void M()
                {
                    $$
                }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("""
            var v = @$"
            """, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("""
                    var v = $@"$$"
            """, assertCaretPosition: true, HangMitigatingCancellationToken);

        // Backspace removes quotes
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.BACK, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        var v = $@$$", assertCaretPosition: true, HangMitigatingCancellationToken);

        // Undo puts them back
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
        // Incorrect assertion: https://github.com/dotnet/roslyn/issues/33672
        await TestServices.EditorVerifier.CurrentLineTextAsync("        var v = $@\"\"$$", assertCaretPosition: true, HangMitigatingCancellationToken);

        // First, the FixInterpolatedVerbatimString action is undone (@$ reordering)
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
        // Incorrect assertion: https://github.com/dotnet/roslyn/issues/33672
        await TestServices.EditorVerifier.CurrentLineTextAsync("        var v = @$\"\"$$", assertCaretPosition: true, HangMitigatingCancellationToken);

        // Then the automatic quote completion is undone
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.Undo, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        var v = @$\"$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory(Skip = "https://github.com/dotnet/roslyn/issues/63576"), CombinatorialData]
    public async Task AngleBracket_PossibleGenerics_InsertionAndCompletion(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
                //field
                $$
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(["System.Action<", VirtualKeyCode.TAB], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    System.Action<>$$", assertCaretPosition: true, HangMitigatingCancellationToken);

        await SetUpEditorAsync("""

            class C {
                //method decl
                $$
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(["void GenericMethod<", VirtualKeyCode.TAB], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    void GenericMethod<>$$", assertCaretPosition: true, HangMitigatingCancellationToken);

        await SetUpEditorAsync("""

            class C {
                //delegate
                $$
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("delegate void Del<", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    delegate void Del<$$>", assertCaretPosition: true, HangMitigatingCancellationToken);

        await SetUpEditorAsync("""

            //using directive
            $$

            """, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("using ActionOfT = System.Action<", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("using ActionOfT = System.Action<$$>", assertCaretPosition: true, HangMitigatingCancellationToken);

        await SetUpEditorAsync("""

            //class
            $$

            """, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(["class GenericClass<", '>'], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("class GenericClass<>$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task SingleQuote_InsertionAndCompletion(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
                $$
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("char c = '", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    char c = '$$'", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.DELETE, VirtualKeyCode.BACK], HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(["'\u6666", "'"], HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CurrentLineTextAsync("    char c = '\u6666'$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Nested_AllKinds(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class Bar<U>
            {
                T Goo<T>(T t) { return t; }
                void M()
                {
                    $$
                }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(
            "var arr=new object[,]{{Goo(0", HangMitigatingCancellationToken);

        if (showCompletionInArgumentLists)
        {
            Assert.False(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));
        }

        await TestServices.Input.SendWithoutActivateAsync(
            [
                VirtualKeyCode.TAB,
                VirtualKeyCode.TAB,
                ",{Goo(Goo(\"hello",
                VirtualKeyCode.TAB,
                VirtualKeyCode.TAB,
                VirtualKeyCode.TAB,
                VirtualKeyCode.TAB,
                VirtualKeyCode.TAB,
                ';',
            ],
            HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CurrentLineTextAsync("        var arr = new object[,] { { Goo(0) }, { Goo(Goo(\"hello\")) } };$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Negative_NoCompletionInSingleLineComments(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
                // $$
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("{([\"'", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    // {([\"'$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Negative_NoCompletionInMultiLineComments(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
                /*
                 $$
                */
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("{([\"'", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("     {([\"'$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Negative_NoCompletionStringVerbatimStringOrCharLiterals(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
                $$
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("string s = \"{([<'", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("""
                string s = "{([<'$$"
            """, assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.END, ';', VirtualKeyCode.RETURN], HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("string y = @\"{([<'", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("""
                string y = @"{([<'$$"
            """, assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.END, ';', VirtualKeyCode.RETURN], HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("""
            char ch = '{([<"
            """, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("    char ch = '{([<\"$$'", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Negative_NoCompletionInXmlDocComments(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            $$
            class C { }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(
            [
                "///",
                "{([<\"'",
            ],
            HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CurrentLineTextAsync("/// {([<\"'$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Negative_NoCompletionInDisabledPreprocesser(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
            #if false
            $$
            #endif
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("void Goo(", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("void Goo($$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Negative_NoCompletionAfterRegionPreprocesser(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            #region $$

            #endregion

            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("{([<\"'", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("#region {([<\"'$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Negative_NoCompletionAfterEndregionPreprocesser(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            #region

            #endregion $$

            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("{([<\"'", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("#endregion {([<\"'$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Negative_NoCompletionAfterIfPreprocesser(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            #if $$

            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("{([<\"'", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("#if {([<\"'$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Negative_NoCompletionAfterPragmaPreprocesser(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            #pragma $$

            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("{([<\"'", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("#pragma {([<\"'$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [WorkItem(651954, "DevDiv")]
    [IdeTheory, CombinatorialData]
    public async Task InteractionWithOverrideStubGeneration(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class A
            {
                public virtual void Goo() { }
            }
            class B : A
            {
                // type "override Goo("
                $$
            }

            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("override ", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        await TestServices.Input.SendWithoutActivateAsync("Goo(", HangMitigatingCancellationToken);
        var actualText = await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken);
        Assert.Contains("""

            class B : A
            {
                // type "override Goo("
                public override void Goo()
                {
                    base.Goo();
                }
            }
            """, actualText);
    }

    [WorkItem(531107, "DevDiv")]
    [IdeTheory, CombinatorialData]
    public async Task InteractionWithCompletionList(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            using System.Collections.Generic;
            class C 
            {
                void M()
                {
                    List<int> li = $$
                }
            }

            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("new Li", HangMitigatingCancellationToken);
        Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));

        if (showCompletionInArgumentLists)
        {
            await TestServices.Input.SendWithoutActivateAsync(["(", ")"], HangMitigatingCancellationToken);
        }
        else
        {
            await TestServices.Input.SendWithoutActivateAsync(["(", VirtualKeyCode.TAB], HangMitigatingCancellationToken);
        }

        await TestServices.EditorVerifier.CurrentLineTextAsync("        List<int> li = new List<int>()$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [WorkItem(823958, "DevDiv")]
    [IdeTheory, CombinatorialData]
    public async Task AutoBraceCompleteDoesNotFormatBracePairInInitializers(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C 
            {
                void M()
                {
                    var x = $$
                }
            }

            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("new int[]{", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        var x = new int[] {$$}", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [WorkItem(823958, "DevDiv")]
    [IdeTheory, CombinatorialData]
    public async Task AutoBraceCompleteDoesNotFormatBracePairInObjectCreationExpression(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C 
            {
                void M()
                {
                    var x = $$
                }
            }

            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("new {", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        var x = new {$$}", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [WorkItem(823958, "DevDiv")]
    [IdeTheory, CombinatorialData]
    public async Task AutoBraceCompleteFormatsBracePairInClassDeclarationAndAutoProperty(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class $$

            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("C{", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("class C { $$}", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(
            [
                VirtualKeyCode.RETURN,
                "int Prop {",
            ],
            HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("""

            class C
            {
                int Prop { $$}
            }
            """,
assertCaretPosition: true,
HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    [Trait(Traits.Feature, Traits.Features.CompleteStatement)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18104")]
    public async Task CompleteStatementTriggersCompletion(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class Program
            {
                static void Main(string[] args)
                {
                    Main$$
                }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("(ar", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Main(ar$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        if (showCompletionInArgumentLists)
        {
            Assert.True(await TestServices.Editor.IsCompletionActiveAsync(HangMitigatingCancellationToken));
        }

        await TestServices.Input.SendWithoutActivateAsync(";", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Main(args);$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeTheory, CombinatorialData]
    public async Task Braces_InsertionOnNewLine(bool showCompletionInArgumentLists)
    {
        await SetUpEditorAsync("""

            class C {
                void Goo() {
                    $$
                }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Workspace.SetTriggerCompletionInArgumentListsAsync(LanguageNames.CSharp, showCompletionInArgumentLists, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(
            [
                "if (true)",
                VirtualKeyCode.RETURN,
                "{",
            ],
            HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        { $$}", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("""

            class C {
                void Goo() {
                    if (true)
                    {

                    }
                }
            }
            """, cancellationToken: HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync("}", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("""

            class C {
                void Goo() {
                    if (true)
                    {
                    }$$
                }
            }
            """, assertCaretPosition: true, HangMitigatingCancellationToken);
    }
}
