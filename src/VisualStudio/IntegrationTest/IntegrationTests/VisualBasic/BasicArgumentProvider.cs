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

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicArgumentProvider : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicArgumentProvider(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicArgumentProvider))
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
Public Class Test
    Private f As Object

    Public Sub Method()$$
    End Sub
End Class
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
Public Class Test
    Public Sub Method()
        $$
    End Sub
End Class
");

            VisualStudio.Editor.SendKeys("Object.Equ");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("Object.Equals$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("Object.Equals(Nothing$$)", assertCaretPosition: true);
        }

        [WpfFact]
        public void TabTabCompleteNewObject()
        {
            SetUpEditor(@"
Public Class Test
    Public Sub Method()
        Dim value = $$
    End Sub
End Class
");

            VisualStudio.Editor.SendKeys("New Obje");

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("Dim value = New Object$$", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Workspace.WaitForAllAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SignatureHelp);
            VisualStudio.Editor.Verify.CurrentLineText("Dim value = New Object($$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Tab);
            VisualStudio.Editor.Verify.CurrentLineText("Dim value = New Object()$$", assertCaretPosition: true);
        }

        [WpfFact]
        public void TabTabCompletionWithArguments()
        {
            SetUpEditor(@"
Imports System
Public Class Test
    Private f As Integer

    Public Sub Method(provider As IFormatProvider)$$
    End Sub
End Class
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
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(Nothing$$)", assertCaretPosition: true);

            VisualStudio.Editor.SendKeys(VirtualKey.Down);
            VisualStudio.Editor.Verify.CurrentLineText("f.ToString(Nothing$$, provider)", assertCaretPosition: true);

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
Imports System
Public Class TestClass
    Public Sub Method()$$
    End Sub

    Sub Test()
    End Sub

    Sub Test(x As Integer)
    End Sub

    Sub Test(x As Integer, y As Integer)
    End Sub
End Class
");

            VisualStudio.Editor.SendKeys(VirtualKey.Enter);
            VisualStudio.Editor.SendKeys("Tes");

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
Imports System
Public Class TestClass
    Public Sub Method()$$
    End Sub

    Sub Test()
    End Sub

    Sub Test(x As Integer)
    End Sub

    Sub Test(x As Integer, y As Integer)
    End Sub
End Class
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

        [WpfFact]
        public void SmartBreakLineWithTabTabCompletion()
        {
            SetUpEditor(@"
Public Class Test
    Private f As Object

    Public Sub Method()$$
    End Sub
End Class
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
Public Class Test
    Private f As Object

    Public Sub Method()
        f.ToString()
$$
    End Sub
End Class
", assertCaretPosition: true);
        }
    }
}
