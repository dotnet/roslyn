﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp
{
    public class CSharpArgumentProvider : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpArgumentProvider()
            : base(nameof(CSharpArgumentProvider))
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);

            var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
            globalOptions.SetGlobalOption(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, LanguageNames.CSharp, true);
            globalOptions.SetGlobalOption(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, LanguageNames.VisualBasic, true);
        }

        [IdeFact]
        public async Task SimpleTabTabCompletion()
        {
            await SetUpEditorAsync(@"
public class Test
{
    private object f;

    public void Method()
    {$$
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("f.ToSt", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString()$$", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task TabTabCompleteObjectEquals()
        {
            await SetUpEditorAsync(@"
public class Test
{
    public void Method()
    {
        $$
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync("object.Equ", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        object.Equals$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        object.Equals(null$$, null)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        object.Equals(null, null$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        object.Equals(null, null)$$", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task TabTabCompleteNewObject()
        {
            await SetUpEditorAsync(@"
public class Test
{
    public void Method()
    {
        var value = $$
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync("new obje", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        var value = new object$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        var value = new object($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        var value = new object()$$", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task TabTabBeforeSemicolon()
        {
            await SetUpEditorAsync(@"
public class Test
{
    private object f;

    public void Method()
    {
        $$;
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync("f.ToSt", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString$$;", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString($$);", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString()$$;", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task TabTabCompletionWithArguments()
        {
            await SetUpEditorAsync(@"
using System;
public class Test
{
    private int f;

    public void Method(IFormatProvider provider)
    {$$
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("f.ToSt", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(provider$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(null$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(null$$, provider)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync("\"format\"", HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(\"format\"$$, provider)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(\"format\", provider$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.UP, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(\"format\"$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.UP, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(provider$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(\"format\"$$)", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task FullCycle()
        {
            await SetUpEditorAsync(@"
using System;
public class TestClass
{
    public void Method()
    {$$
    }

    void Test() { }
    void Test(int x) { }
    void Test(int x, int y) { }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("Test", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Test$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Test($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$, 0)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Test($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$, 0)", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task ImplicitArgumentSwitching()
        {
            await SetUpEditorAsync(@"
using System;
public class TestClass
{
    public void Method()
    {$$
    }

    void Test() { }
    void Test(int x) { }
    void Test(int x, int y) { }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("Tes", HangMitigatingCancellationToken);

            // Trigger the session and type '0' without waiting for the session to finish initializing
            await TestServices.Input.SendAsync([VirtualKeyCode.TAB, VirtualKeyCode.TAB, '0'], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$, 0)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.UP, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$)", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        /// <summary>
        /// Argument completion with no arguments.
        /// </summary>
        [IdeFact]
        public async Task SemicolonWithTabTabCompletion1()
        {
            await SetUpEditorAsync(@"
public class Test
{
    private object f;

    public void Method()
    {$$
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("f.ToSt", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(';', HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString();$$", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        /// <summary>
        /// Argument completion with one or more arguments.
        /// </summary>
        [IdeFact]
        public async Task SemicolonWithTabTabCompletion2()
        {
            await SetUpEditorAsync(@"
public class Test
{
    private object f;

    public void Method()
    {$$
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("object.Equ", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        object.Equals$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        object.Equals(null$$, null)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        object.Equals(null, null$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(';', HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        object.Equals(null, null);$$", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        /// <summary>
        /// Argument completion with exactly one argument.
        /// </summary>
        [IdeFact]
        public async Task SemicolonWithTabTabCompletion3()
        {
            await SetUpEditorAsync(@"
public class Test
{
    private object f;

    public void Method(int value)
    {$$
    }

    public void Method2(int value)
    {
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("this.M2", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        this.Method2$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        this.Method2(value$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(';', HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        this.Method2(value);$$", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task SmartBreakLineWithTabTabCompletion1()
        {
            await SetUpEditorAsync(@"
public class Test
{
    private object f;

    public void Method()
    {$$
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("f.ToSt", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync((VirtualKeyCode.RETURN, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(@"
public class Test
{
    private object f;

    public void Method()
    {
        f.ToString();
$$
    }
}
", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task SmartBreakLineWithTabTabCompletion2()
        {
            await SetUpEditorAsync(@"
public class Test
{
    private object f;

    public void Method()
    {$$
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("object.Equ", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        object.Equals$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        object.Equals(null$$, null)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        object.Equals(null, null$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync((VirtualKeyCode.RETURN, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.TextContainsAsync(@"
public class Test
{
    private object f;

    public void Method()
    {
        object.Equals(null, null);
$$
    }
}
", assertCaretPosition: true, HangMitigatingCancellationToken);
        }

        [IdeTheory]
        [InlineData("\"<\"", Skip = "https://github.com/dotnet/roslyn/issues/29669")]
        [InlineData("\">\"")] // testing things that might break XML
        [InlineData("\"&\"")]
        [InlineData("\"  \"")]
        [InlineData("\"$placeholder$\"")] // ensuring our snippets aren't substituted in ways we don't expect
        [InlineData("\"$end$\"")]
        public async Task EnsureParameterContentPreserved(string parameterText)
        {
            await SetUpEditorAsync(@"
public class Test
{
    public void Method()
    {$$
    }

    public void M(string s, int i)
    {
    }

    public void M(string s, int i, int i2)
    {
    }
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
            await TestServices.Input.SendAsync("M", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        M$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        M(null, 0)", cancellationToken: HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(parameterText, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        M(" + parameterText + ", 0)", cancellationToken: HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("        M(" + parameterText + ", 0, 0)", cancellationToken: HangMitigatingCancellationToken);
        }

        [IdeFact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/54038")]
        public async Task InsertPreprocessorSnippet()
        {
            await SetUpEditorAsync(@"
using System;
public class TestClass
{
$$
}
", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync("#i", HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("#if$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CurrentLineTextAsync("#if true$$", assertCaretPosition: true, HangMitigatingCancellationToken);

            var expected = @"
using System;
public class TestClass
{
#if true

#endif
}
";

            AssertEx.EqualOrDiff(expected, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
        }
    }
}
