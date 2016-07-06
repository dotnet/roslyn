// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities;
using Roslyn.VisualStudio.Test.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicIntelliSense : AbstractEditorTests
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicIntelliSense(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(BasicIntelliSense))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IntelliSenseTriggersOnParenWithBraceCompletionAndCorrectUndoMerging()
        {
            SetUpEditor(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module");

            DisableSuggestionMode();

            SendKeys("dim q as lis(");
            VerifyCompletionItemExists("Of");

            VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As List($$)
    End Sub
End Module",
assertCaretPosition: true);

            SendKeys(
                VirtualKey.Down,
                VirtualKey.Tab);

            VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of$$)
    End Sub
End Module",
assertCaretPosition: true);

            SendKeys(" inte");
            VerifyCompletionItemExists("Integer");

            SendKeys(')');

            VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of Integer)$$
    End Sub
End Module",
assertCaretPosition: true);

            SendKeys(Ctrl(VirtualKey.Z));

            VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of inte)$$
    End Sub
End Module",
assertCaretPosition: true);

            SendKeys(Ctrl(VirtualKey.Z));

            VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of inte$$)
    End Sub
End Module",
assertCaretPosition: true);

            SendKeys(Ctrl(VirtualKey.Z));

            VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of$$)
    End Sub
End Module",
assertCaretPosition: true);

            SendKeys(Ctrl(VirtualKey.Z));

            VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As lis($$)
    End Sub
End Module",
assertCaretPosition: true);

            SendKeys(Ctrl(VirtualKey.Z));

            VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As lis($$
    End Sub
End Module",
assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeAVariableDeclaration()
        {
            SetUpEditor(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module");

            DisableSuggestionMode();

            SendKeys("dim");
            VerifyCompletionItemExists("Dim", "ReDim");

            SendKeys(' ');
            VerifyCompletionListIsActive(expected: false);

            SendKeys('i');
            VerifyCompletionListIsActive(expected: false);

            SendKeys(' ');
            VerifyCompletionItemExists("As");

            SendKeys("a ");
            SendKeys("intege");
            VerifyCompletionItemExists("Integer", "UInteger");

            SendKeys(' ');
            VerifyCompletionListIsActive(expected: false);

            SendKeys('=');
            VerifyCompletionListIsActive(expected: true);

            SendKeys(' ');
            VerifyCompletionListIsActive(expected: true);

            SendKeys("foo");
            VerifyCompletionListIsActive(expected: false);

            SendKeys(' ');
            VerifyCompletionListIsActive(expected: true);

            SendKeys(VirtualKey.Backspace);
            VerifyCompletionListIsActive(expected: false);

            SendKeys(VirtualKey.Backspace);
            VerifyCompletionListIsActive(expected: true);

            SendKeys(
                VirtualKey.Left,
                VirtualKey.Delete);
            VerifyCompletionListIsActive(expected: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DismissIntelliSenseOnApostrophe()
        {
            SetUpEditor(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module");

            DisableSuggestionMode();

            SendKeys("dim q as ");
            VerifyCompletionItemExists("_AppDomain");

            SendKeys("'");
            VerifyCompletionListIsActive(expected: false);

            VerifyTextContains(@"Module Module1
    Sub Main()
        Dim q As '
    End Sub
End Module");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeLeftAngleAfterImports()
        {
            SetUpEditor(@"
Imports$$");

            DisableSuggestionMode();

            SendKeys(' ');
            VerifyCompletionItemExists("Microsoft", "System");

            SendKeys('<');
            VerifyCompletionListIsActive(expected: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DismissAndRetriggerIntelliSenseOnEquals()
        {
            SetUpEditor(@"
Module Module1
    Function M(val As Integer) As Integer
        $$
    End Function
End Module");

            DisableSuggestionMode();

            SendKeys('M');
            VerifyCompletionItemExists("M");

            SendKeys("=v");
            VerifyCompletionItemExists("val");

            SendKeys(' ');
            VerifyTextContains(@"
Module Module1
    Function M(val As Integer) As Integer
        M=val $$
    End Function
End Module",
assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CtrlAltSpaceOption()
        {
            DisableSuggestionMode();

            SendKeys("Nam Foo");
            VerifyCurrentLineText("Namespace Foo$$", assertCaretPosition: true);

            ClearEditor();

            ExecuteCommand(WellKnownCommandNames.ToggleCompletionMode);

            SendKeys("Nam Foo");
            VerifyCurrentLineText("Nam Foo$$", assertCaretPosition: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EnterTriggerCompletionListAndImplementInterface()
        {
            SetUpEditor(@"
Interface UFoo
    Sub FooBar()
End Interface

Public Class Bar
    Implements$$

End Class");

            DisableSuggestionMode();

            SendKeys(" UF");
            VerifyCompletionItemExists("UFoo");

            SendKeys(VirtualKey.Enter);
            VerifyCompletionListIsActive(expected: false);

            VerifyTextContains(@"
Interface UFoo
    Sub FooBar()
End Interface

Public Class Bar
    Implements UFoo

    Public Sub FooBar() Implements UFoo.FooBar
        Throw New NotImplementedException()
    End Sub
End Class");
        }
    }
}
