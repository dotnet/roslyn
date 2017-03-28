// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Options;
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

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IntelliSenseTriggersOnParenWithBraceCompletionAndCorrectUndoMerging()
        {
            SetUpEditor(@"
Module Module1
    Sub Main()
        $$
    End Sub
End Module");

            this.SetUseSuggestionMode(false);

            this.SendKeys("dim q as lis(");
            this.VerifyCompletionItemExists("Of");

            this.VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As List($$)
    End Sub
End Module",
assertCaretPosition: true);

            this.SendKeys(
                VirtualKey.Down,
                VirtualKey.Tab);

            this.VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of$$)
    End Sub
End Module",
assertCaretPosition: true);

            this.SendKeys(" inte");
            this.VerifyCompletionItemExists("Integer");

            this.SendKeys(')');

            this.VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of Integer)$$
    End Sub
End Module",
assertCaretPosition: true);

            this.SendKeys(Ctrl(VirtualKey.Z));

            this.VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of inte)$$
    End Sub
End Module",
assertCaretPosition: true);

            this.SendKeys(Ctrl(VirtualKey.Z));

            this.VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of inte$$)
    End Sub
End Module",
assertCaretPosition: true);

            this.SendKeys(Ctrl(VirtualKey.Z));

            this.VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As List(Of$$)
    End Sub
End Module",
assertCaretPosition: true);

            this.SendKeys(Ctrl(VirtualKey.Z));

            this.VerifyTextContains(@"
Module Module1
    Sub Main()
        Dim q As lis($$)
    End Sub
End Module",
assertCaretPosition: true);

            this.SendKeys(Ctrl(VirtualKey.Z));

            this.VerifyTextContains(@"
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

            this.SetUseSuggestionMode(false);

            this.SendKeys("dim");
            this.VerifyCompletionItemExists("Dim", "ReDim");

            this.SendKeys(' ');
            Assert.Equal(false, Editor.IsCompletionActive());

            this.SendKeys('i');
            Assert.Equal(false, Editor.IsCompletionActive());

            this.SendKeys(' ');
            this.VerifyCompletionItemExists("As");

            this.SendKeys("a ");
            this.SendKeys("intege");
            this.VerifyCompletionItemExists("Integer", "UInteger");

            this.SendKeys(' ');
            Assert.Equal(false, Editor.IsCompletionActive());

            this.SendKeys('=');
            Assert.Equal(true, Editor.IsCompletionActive());

            this.SendKeys(' ');
            Assert.Equal(true, Editor.IsCompletionActive());

            this.SendKeys("fooo");
            Assert.Equal(false, Editor.IsCompletionActive());

            this.SendKeys(' ');
            Assert.Equal(true, Editor.IsCompletionActive());

            this.SendKeys(VirtualKey.Backspace);
            Assert.Equal(false, Editor.IsCompletionActive());

            this.SendKeys(VirtualKey.Backspace);
            Assert.Equal(true, Editor.IsCompletionActive());

            this.SendKeys(
                VirtualKey.Left,
                VirtualKey.Delete);
            Assert.Equal(true, Editor.IsCompletionActive());
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

            this.SetUseSuggestionMode(false);

            this.SendKeys("dim q as ");
            this.VerifyCompletionItemExists("_AppDomain");

            this.SendKeys("'");
            Assert.Equal(false, Editor.IsCompletionActive());
            var actualText = Editor.GetText();
            Assert.Contains(@"Module Module1
    Sub Main()
        Dim q As '
    End Sub
End Module", actualText);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeLeftAngleAfterImports()
        {
            SetUpEditor(@"
Imports$$");

            this.SetUseSuggestionMode(false);

            this.SendKeys(' ');
            this.VerifyCompletionItemExists("Microsoft", "System");

            this.SendKeys('<');
            Assert.Equal(false, Editor.IsCompletionActive());
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

            this.SetUseSuggestionMode(false);

            this.SendKeys('M');
            this.VerifyCompletionItemExists("M");

            this.SendKeys("=v");
            this.VerifyCompletionItemExists("val");

            this.SendKeys(' ');
            this.VerifyTextContains(@"
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
            this.SetUseSuggestionMode(false);

            this.SendKeys("Nam Foo");
            this.VerifyCurrentLineText("Namespace Foo$$", assertCaretPosition: true);

            ClearEditor();

            this.ExecuteCommand(WellKnownCommandNames.Edit_ToggleCompletionMode);

            this.SendKeys("Nam Foo");
            this.VerifyCurrentLineText("Nam Foo$$", assertCaretPosition: true);
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

            this.SetUseSuggestionMode(false);

            this.SendKeys(" UF");
            this.VerifyCompletionItemExists("UFoo");

            this.SendKeys(VirtualKey.Enter);
            Assert.Equal(false, Editor.IsCompletionActive());
            var actualText = Editor.GetText();
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
