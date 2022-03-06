// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpArgumentProvider : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpArgumentProvider(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpArgumentProvider))
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);

            VisualStudio.Workspace.SetArgumentCompletionSnippetsOption(true);
        }

        [WpfFact]
        public void SimpleTabTabCompletion()
        {
            SetUpEditor(@"
public class Test
{
    private object f;

    public void Method()
    {$$
    }
}
");

            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.SendKeys("f.ToSt");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString()$$", assertCaretPosition: true);
        }

        [WpfFact]
        public void TabTabCompleteObjectEquals()
        {
            SetUpEditor(@"
public class Test
{
    public void Method()
    {
        $$
    }
}
");

            VisualStudio.Editor.SendKeys("object.Equ");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("object.Equals$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("object.Equals(null$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("object.Equals(null, null$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("object.Equals(null, null)$$", assertCaretPosition: true);
        }

        [WpfFact]
        public void TabTabCompleteNewObject()
        {
            SetUpEditor(@"
public class Test
{
    public void Method()
    {
        var value = $$
    }
}
");

            VisualStudio.Editor.SendKeys("new obje");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("var value = new object$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("var value = new object($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("var value = new object()$$", assertCaretPosition: true);
        }

        [WpfFact]
        public void TabTabBeforeSemicolon()
        {
            SetUpEditor(@"
public class Test
{
    private object f;

    public void Method()
    {
        $$;
    }
}
");

            VisualStudio.Editor.SendKeys("f.ToSt");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString$$;", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString($$);", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString()$$;", assertCaretPosition: true);
        }

        [WpfFact]
        public void TabTabCompletionWithArguments()
        {
            SetUpEditor(@"
using System;
public class Test
{
    private int f;

    public void Method(IFormatProvider provider)
    {$$
    }
}
");

            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.SendKeys("f.ToSt");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(provider$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(null$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(null$$, provider)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys("\"format\"");
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(\"format\"$$, provider)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(\"format\", provider$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Up);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(\"format\"$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Up);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(provider$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(\"format\"$$)", assertCaretPosition: true);
        }

        [WpfFact]
        public void FullCycle()
        {
            SetUpEditor(@"
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
");

            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.SendKeys("Test");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("Test$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("Test($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("Test(0$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("Test(0$$, 0)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("Test($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("Test(0$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("Test(0$$, 0)", assertCaretPosition: true);
        }

        [WpfFact]
        public void ImplicitArgumentSwitching()
        {
            SetUpEditor(@"
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
");

            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.SendKeys("Tes");

            // Trigger the session and type '0' without waiting for the session to finish initializing
            VisualStudio.Editor.SendKeys(VirtualKey.Tab, VirtualKey.Tab, '0');
            VisualStudio.Editor.Verify.CurrentLineText("Test(0$$)", assertCaretPosition: true);

            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("Test(0$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("Test(0$$, 0)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Up);
            VisualStudio.Editor.Verify.CurrentLineText("Test(0$$)", assertCaretPosition: true);
        }

        /// <summary>
        /// Argument completion with no arguments.
        /// </summary>
        [WpfFact]
        public void SemicolonWithTabTabCompletion1()
        {
            SetUpEditor(@"
public class Test
{
    private object f;

    public void Method()
    {$$
    }
}
");

            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.SendKeys("f.ToSt");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(';');
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString();$$", assertCaretPosition: true);
        }

        /// <summary>
        /// Argument completion with one or more arguments.
        /// </summary>
        [WpfFact]
        public void SemicolonWithTabTabCompletion2()
        {
            SetUpEditor(@"
public class Test
{
    private object f;

    public void Method()
    {$$
    }
}
");

            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.SendKeys("object.Equ");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("object.Equals$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("object.Equals(null$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("object.Equals(null, null$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(';');
            VisualStudio.Editor.Verify.CurrentLineText("object.Equals(null, null);$$", assertCaretPosition: true);
        }

        /// <summary>
        /// Argument completion with exactly one argument.
        /// </summary>
        [WpfFact]
        public void SemicolonWithTabTabCompletion3()
        {
            SetUpEditor(@"
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
");

            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.SendKeys("this.M2");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("this.Method2$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("this.Method2(value$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(';');
            VisualStudio.Editor.Verify.CurrentLineText("this.Method2(value);$$", assertCaretPosition: true);
        }

        [WpfFact]
        public void SmartBreakLineWithTabTabCompletion1()
        {
            SetUpEditor(@"
public class Test
{
    private object f;

    public void Method()
    {$$
    }
}
");

            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.SendKeys("f.ToSt");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(Shift(VirtualKey.Enter));
            VisualStudio.Editor.Verify.TextContains(@"
public class Test
{
    private object f;

    public void Method()
    {
        f.ToString();
$$
    }
}
", assertCaretPosition: true);
        }

        [WpfFact]
        public void SmartBreakLineWithTabTabCompletion2()
        {
            SetUpEditor(@"
public class Test
{
    private object f;

    public void Method()
    {$$
    }
}
");

            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.SendKeys("object.Equ");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("object.Equals$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("object.Equals(null$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("object.Equals(null, null$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(Shift(VirtualKey.Enter));
            VisualStudio.Editor.Verify.TextContains(@"
public class Test
{
    private object f;

    public void Method()
    {
        object.Equals(null, null);
$$
    }
}
", assertCaretPosition: true);
        }

        [WpfTheory]
        [InlineData("\"<\"", Skip = "https://github.com/dotnet/roslyn/issues/29669")]
        [InlineData("\">\"")] // testing things that might break XML
        [InlineData("\"&\"")]
        [InlineData("\"  \"")]
        [InlineData("\"$placeholder$\"")] // ensuring our snippets aren't substituted in ways we don't expect
        [InlineData("\"$end$\"")]
        public void EnsureParameterContentPreserved(string parameterText)
        {
            SetUpEditor(@"
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
");

            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.SendKeys("M");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("M$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("M(null, 0)");

            VisualStudio.Editor.SendKeys(parameterText);
            VisualStudio.Editor.Verify.CurrentLineText("M(" + parameterText + ", 0)");

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("M(" + parameterText + ", 0, 0)");
        }

        [WpfFact]
        [WorkItem(54038, "https://github.com/dotnet/roslyn/issues/54038")]
        public void InsertPreprocessorSnippet()
        {
            SetUpEditor(@"
using System;
public class TestClass
{
$$
}
");

            VisualStudio.Editor.SendKeys("#i");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("#if$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("#if true$$", assertCaretPosition: true);

            var expected = @"
using System;
public class TestClass
{
#if true

#endif
}
";

            AssertEx.EqualOrDiff(expected, VisualStudio.Editor.GetText());
        }
    }
}
