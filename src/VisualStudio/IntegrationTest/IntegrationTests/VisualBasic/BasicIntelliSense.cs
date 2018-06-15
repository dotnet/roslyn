﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicIntelliSense : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicIntelliSense(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicIntelliSense))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IntelliSenseTriggersOnParenWithBraceCompletionAndCorrectUndoMerging()
        {
            SetUpEditor(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module");

            VisualStudio.Workspace.SetUseSuggestionMode(false);

            VisualStudio.SendKeys.Send("dim q as lis(");
            VisualStudio.Editor.Verify.CompletionItemsExist("Of");

            VisualStudio.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As List($$)
    End Sub
End Module",
assertCaretPosition: true);

            VisualStudio.SendKeys.Send(
                VirtualKey.Down,
                VirtualKey.Tab);

            VisualStudio.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of$$)
    End Sub
End Module",
assertCaretPosition: true);

            VisualStudio.SendKeys.Send(" inte");
            VisualStudio.Editor.Verify.CompletionItemsExist("Integer");

            VisualStudio.SendKeys.Send(')');

            VisualStudio.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of Integer)$$
    End Sub
End Module",
assertCaretPosition: true);

            VisualStudio.SendKeys.Send(Ctrl(VirtualKey.Z));

            VisualStudio.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of inte)$$
    End Sub
End Module",
assertCaretPosition: true);

            VisualStudio.SendKeys.Send(Ctrl(VirtualKey.Z));

            VisualStudio.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of inte$$)
    End Sub
End Module",
assertCaretPosition: true);

            VisualStudio.SendKeys.Send(Ctrl(VirtualKey.Z));

            VisualStudio.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of$$)
    End Sub
End Module",
assertCaretPosition: true);

            VisualStudio.SendKeys.Send(Ctrl(VirtualKey.Z));

            VisualStudio.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As lis($$)
    End Sub
End Module",
assertCaretPosition: true);

            VisualStudio.SendKeys.Send(Ctrl(VirtualKey.Z));

            VisualStudio.Editor.Verify.TextContains(@"
Module Module1
    Sub Main()
        Dim q As lis($$
    End Sub
End Module",
assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeAVariableDeclaration()
        {
            SetUpEditor(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module");

            VisualStudio.Workspace.SetUseSuggestionMode(false);

            VisualStudio.SendKeys.Send("dim");
            VisualStudio.Editor.Verify.CompletionItemsExist("Dim", "ReDim");

            VisualStudio.SendKeys.Send(' ');
            Assert.Equal(false, VisualStudio.Editor.IsCompletionActive());

            VisualStudio.SendKeys.Send('i');
            Assert.Equal(false, VisualStudio.Editor.IsCompletionActive());

            VisualStudio.SendKeys.Send(' ');
            VisualStudio.Editor.Verify.CompletionItemsExist("As");

            VisualStudio.SendKeys.Send("a ");
            VisualStudio.SendKeys.Send("intege");
            VisualStudio.Editor.Verify.CompletionItemsExist("Integer", "UInteger");

            VisualStudio.SendKeys.Send(' ');
            Assert.Equal(false, VisualStudio.Editor.IsCompletionActive());

            VisualStudio.SendKeys.Send('=');
            Assert.Equal(true, VisualStudio.Editor.IsCompletionActive());

            VisualStudio.SendKeys.Send(' ');
            Assert.Equal(true, VisualStudio.Editor.IsCompletionActive());

            VisualStudio.SendKeys.Send("fooo");
            Assert.Equal(false, VisualStudio.Editor.IsCompletionActive());

            VisualStudio.SendKeys.Send(' ');
            Assert.Equal(true, VisualStudio.Editor.IsCompletionActive());

            VisualStudio.SendKeys.Send(VirtualKey.Backspace);
            Assert.Equal(false, VisualStudio.Editor.IsCompletionActive());

            VisualStudio.SendKeys.Send(VirtualKey.Backspace);
            Assert.Equal(true, VisualStudio.Editor.IsCompletionActive());

            VisualStudio.SendKeys.Send(
                VirtualKey.Left,
                VirtualKey.Delete);
            Assert.Equal(true, VisualStudio.Editor.IsCompletionActive());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DismissIntelliSenseOnApostrophe()
        {
            SetUpEditor(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module");

            VisualStudio.Workspace.SetUseSuggestionMode(false);

            VisualStudio.SendKeys.Send("dim q as ");
            VisualStudio.Editor.Verify.CompletionItemsExist("_AppDomain");

            VisualStudio.SendKeys.Send("'");
            Assert.Equal(false, VisualStudio.Editor.IsCompletionActive());
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"Module Module1
    Sub Main()
        Dim q As '
    End Sub
End Module", actualText);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeLeftAngleAfterImports()
        {
            SetUpEditor(@"
Imports$$");

            VisualStudio.Workspace.SetUseSuggestionMode(false);

            VisualStudio.SendKeys.Send(' ');
            VisualStudio.Editor.Verify.CompletionItemsExist("Microsoft", "System");

            VisualStudio.SendKeys.Send('<');
            Assert.Equal(false, VisualStudio.Editor.IsCompletionActive());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DismissAndRetriggerIntelliSenseOnEquals()
        {
            SetUpEditor(@"
Module Module1
    Function M(val As Integer) As Integer
        $$
    End Function
End Module");

            VisualStudio.Workspace.SetUseSuggestionMode(false);

            VisualStudio.SendKeys.Send('M');
            VisualStudio.Editor.Verify.CompletionItemsExist("M");

            VisualStudio.SendKeys.Send("=v");
            VisualStudio.Editor.Verify.CompletionItemsExist("val");

            VisualStudio.SendKeys.Send(' ');
            VisualStudio.Editor.Verify.TextContains(@"
Module Module1
    Function M(val As Integer) As Integer
        M=val $$
    End Function
End Module",
assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CtrlAltSpaceOption()
        {
            VisualStudio.Workspace.SetUseSuggestionMode(false);

            VisualStudio.SendKeys.Send("Nam Foo");
            VisualStudio.Editor.Verify.CurrentLineText("Namespace Foo$$", assertCaretPosition: true);

            ClearEditor();

            VisualStudio.ExecuteCommand(WellKnownCommandNames.Edit_ToggleCompletionMode);

            VisualStudio.SendKeys.Send("Nam Foo");
            VisualStudio.Editor.Verify.CurrentLineText("Nam Foo$$", assertCaretPosition: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EnterTriggerCompletionListAndImplementInterface()
        {
            SetUpEditor(@"
Interface UFoo
    Sub FooBar()
End Interface

Public Class Bar
    Implements$$

End Class");

            VisualStudio.Workspace.SetUseSuggestionMode(false);

            VisualStudio.SendKeys.Send(" UF");
            VisualStudio.Editor.Verify.CompletionItemsExist("UFoo");

            VisualStudio.SendKeys.Send(VirtualKey.Enter);
            Assert.Equal(false, VisualStudio.Editor.IsCompletionActive());
            var actualText = VisualStudio.Editor.GetText();
            Assert.Contains(@"
Interface UFoo
    Sub FooBar()
End Interface

Public Class Bar
    Implements UFoo

    Public Sub FooBar() Implements UFoo.FooBar
        Throw New NotImplementedException()
    End Sub
End Class", actualText);
        }
    }
}
