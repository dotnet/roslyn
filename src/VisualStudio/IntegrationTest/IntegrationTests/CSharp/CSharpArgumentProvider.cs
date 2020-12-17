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

            VisualStudio.Workspace.SetTabTabCompletionOption(true);
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
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(null$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(null$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(null$$, null)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys("\"format\"");
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(\"format\"$$, null)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(\"format\", null$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Up);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(\"format\"$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Up);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(null$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(\"format\"$$)", assertCaretPosition: true);
        }

        [WpfFact]
        public void SemicolonWithTabTabCompletion()
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

        [WpfFact]
        public void SmartBreakLineWithTabTabCompletion()
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
    }
}
