// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.RenameTracking
{
    public class RenameTrackingTaggerProviderTests
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotOnCreation()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotInBlankFile()
        {
            var code = @"$$";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("d");
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingTypingAtEnd()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");
                await state.AssertTag("C", "Cat").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingTypingAtBeginning()
        {
            var code = @"
class $$C
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("AB");
                await state.AssertTag("C", "ABC").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingTypingInMiddle()
        {
            var code = @"
class AB$$CD
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("ZZ");
                await state.AssertTag("ABCD", "ABZZCD").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingDeleteFromEnd()
        {
            var code = @"
class ABC$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                await state.AssertTag("ABC", "AB").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingDeleteFromBeginning()
        {
            var code = @"
class $$ABC
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Delete();
                await state.AssertTag("ABC", "BC").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingDeleteFromMiddle()
        {
            var code = @"
class AB$$C
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                await state.AssertTag("ABC", "AC").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotOnClassKeyword()
        {
            var code = @"
class$$ ABCD
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("d");
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotAtMethodArgument()
        {
            var code = @"
class ABCD
{
    void Foo(int x)
    {
        int abc = 3;
        Foo($$
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("a");
                await state.AssertNoTag().ConfigureAwait(true);

                state.EditorOperations.InsertText("b");
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingSessionContinuesAfterViewingTag()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");
                await state.AssertTag("C", "Cat").ConfigureAwait(true);

                state.EditorOperations.InsertText("s");
                await state.AssertTag("C", "Cats").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotInString()
        {
            var code = @"
class C
{
    void Foo()
    {
        string s = ""abc$$""
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("d");
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingHandlesAtSignAsCSharpEscape()
        {
            var code = @"
class $$C
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("@");
                await state.AssertTag("C", "@C").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingHandlesSquareBracketsAsVisualBasicEscape()
        {
            var code = @"
Class $$C
End Class";
            using (var state = new RenameTrackingTestState(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.InsertText("[");
                await state.AssertNoTag().ConfigureAwait(true);

                state.MoveCaret(1);
                state.EditorOperations.InsertText("]");
                await state.AssertTag("C", "[C]").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotOnSquareBracketsInCSharp()
        {
            var code = @"
class $$C
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("[");
                await state.AssertNoTag().ConfigureAwait(true);

                state.MoveCaret(1);
                state.EditorOperations.InsertText("]");
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingHandlesUnicode()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("ДѮѪÛÊÛÄÁÍäáŒŸõàŸüÄµÁiÛEêàêèäåíòèôèêàòîðñëîâî");
                await state.AssertTag("C", "CДѮѪÛÊÛÄÁÍäáŒŸõàŸüÄµÁiÛEêàêèäåíòèôèêàòîðñëîâî").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingThroughKeyword()
        {
            var code = @"
class i$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("n");
                await state.AssertNoTag().ConfigureAwait(true);

                state.EditorOperations.InsertText("t");
                await state.AssertNoTag().ConfigureAwait(true);

                state.EditorOperations.InsertText("s");
                await state.AssertTag("i", "ints").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingThroughIllegalStartCharacter()
        {
            var code = @"
class $$abc
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("9");
                await state.AssertNoTag().ConfigureAwait(true);

                state.MoveCaret(-1);
                state.EditorOperations.InsertText("t");
                await state.AssertTag("abc", "t9abc").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingOnBothSidesOfIdentifier()
        {
            var code = @"
class $$Def
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("Abc");
                await state.AssertTag("Def", "AbcDef").ConfigureAwait(true);

                state.MoveCaret(3);
                state.EditorOperations.InsertText("Ghi");
                await state.AssertTag("Def", "AbcDefGhi").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingThroughSameIdentifier()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("C", "Cs").ConfigureAwait(true);

                state.EditorOperations.Backspace();
                await state.AssertNoTag().ConfigureAwait(true);

                state.EditorOperations.InsertText("s");
                await state.AssertTag("C", "Cs").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingThroughEmptyString()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                await state.AssertNoTag().ConfigureAwait(true);

                state.EditorOperations.InsertText("D");
                await state.AssertTag("C", "D").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingThroughEmptyStringWithCaretMove()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                state.MoveCaret(-4);
                state.MoveCaret(4);
                await state.AssertNoTag().ConfigureAwait(true);

                state.EditorOperations.InsertText("D");
                await state.AssertTag("C", "D").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotThroughEmptyStringResumeOnDifferentSpace()
        {
            var code = @"
class  C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();

                // Move to previous space
                state.MoveCaret(-1);

                state.EditorOperations.InsertText("D");
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingReplaceIdentifierSuffix()
        {
            var code = @"
class Identifi[|er|]$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                var textSpan = state.HostDocument.SelectedSpans.Single();
                state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "cation");
                await state.AssertTag("Identifier", "Identification").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingReplaceIdentifierPrefix()
        {
            var code = @"
class $$[|Ident|]ifier
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                var textSpan = state.HostDocument.SelectedSpans.Single();
                state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "Complex");
                await state.AssertTag("Identifier", "Complexifier").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingReplaceIdentifierCompletely()
        {
            var code = @"
class [|Cat|]$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                var textSpan = state.HostDocument.SelectedSpans.Single();
                state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "Dog");
                await state.AssertTag("Cat", "Dog").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotAfterInvoke()
        {
            var code = @"
class Cat$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("Cat", "Cats", invokeAction: true).ConfigureAwait(true);

                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingInvokeAndChangeBackToOriginal()
        {
            var code = @"
class Cat$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("Cat", "Cats", invokeAction: true).ConfigureAwait(true);

                await state.AssertNoTag().ConfigureAwait(true);

                state.EditorOperations.Backspace();
                await state.AssertTag("Cats", "Cat").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingUndoOnceAndStartNewSession()
        {
            var code = @"
class Cat$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("abc");
                await state.AssertTag("Cat", "Catabc", invokeAction: true).ConfigureAwait(true);

                await state.AssertNoTag().ConfigureAwait(true);

                // Back to original
                state.Undo();
                await state.AssertNoTag().ConfigureAwait(true);

                state.EditorOperations.InsertText("xyz");
                await state.AssertTag("Cat", "Catxyz").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingUndoTwiceAndContinueSession()
        {
            var code = @"
class Cat$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("abc");
                await state.AssertTag("Cat", "Catabc", invokeAction: true).ConfigureAwait(true);

                await state.AssertNoTag().ConfigureAwait(true);

                // Resume rename tracking session
                state.Undo(2);
                await state.AssertTag("Cat", "Catabc").ConfigureAwait(true);

                state.EditorOperations.InsertText("xyz");
                await state.AssertTag("Cat", "Catabcxyz").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingRedoAlwaysClearsState()
        {
            var code = @"
class Cat$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("Cat", "Cats", invokeAction: true).ConfigureAwait(true);

                await state.AssertNoTag().ConfigureAwait(true);

                // Resume rename tracking session
                state.Undo(2);
                await state.AssertTag("Cat", "Cats").ConfigureAwait(true);

                state.Redo();
                await state.AssertNoTag().ConfigureAwait(true);

                state.Redo();
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingUndoTwiceRedoTwiceUndoStillWorks()
        {
            var code = @"
class Cat$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("Cat", "Cats", invokeAction: true).ConfigureAwait(true);

                await state.AssertNoTag().ConfigureAwait(true);

                // Resume rename tracking session
                state.Undo(2);
                await state.AssertTag("Cat", "Cats").ConfigureAwait(true);

                state.Redo(2);
                await state.AssertNoTag().ConfigureAwait(true);

                // Back to original
                state.Undo();
                await state.AssertNoTag().ConfigureAwait(true);

                // Resume rename tracking session
                state.Undo();
                await state.AssertTag("Cat", "Cats").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingOnReference_ParameterAsArgument()
        {
            var code = @"
class C
{
    void M(int x)
    {
        M(x$$);
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("yz");
                await state.AssertTag("x", "xyz").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingOnReference_ParameterAsNamedArgument()
        {
            var code = @"
class C
{
    void M(int x)
    {
        M(x$$: x);
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("yz");
                await state.AssertTag("x", "xyz").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingOnReference_Namespace()
        {
            var code = @"
namespace NS
{
    class C
    {
        static void M()
        {
            NS$$.C.M();
        }
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("A");
                await state.AssertTag("NS", "NSA").ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotifiesThirdPartiesOfRenameOperation()
        {
            var code = @"
class Cat$$
{
    public Cat()
    {
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("Cat", "Cats", invokeAction: true).ConfigureAwait(true);
                Assert.Equal(1, state.RefactorNotifyService.OnBeforeSymbolRenamedCount);
                Assert.Equal(1, state.RefactorNotifyService.OnAfterSymbolRenamedCount);

                var expectedCode = @"
class Cats
{
    public Cats()
    {
    }
}";
                Assert.Equal(expectedCode, state.HostDocument.TextBuffer.CurrentSnapshot.GetText());

                state.AssertNoNotificationMessage();
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingHonorsThirdPartyRequestsForCancellationBeforeRename()
        {
            var code = @"
class Cat$$
{
    public Cat()
    {
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp, onBeforeGlobalSymbolRenamedReturnValue: false))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("Cat", "Cats", invokeAction: true).ConfigureAwait(true);
                Assert.Equal(1, state.RefactorNotifyService.OnBeforeSymbolRenamedCount);

                // Make sure the rename didn't proceed
                Assert.Equal(0, state.RefactorNotifyService.OnAfterSymbolRenamedCount);
                await state.AssertNoTag().ConfigureAwait(true);

                var expectedCode = @"
class Cat
{
    public Cat()
    {
    }
}";
                Assert.Equal(expectedCode, state.HostDocument.TextBuffer.CurrentSnapshot.GetText());

                state.AssertNotificationMessage();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingAlertsAboutThirdPartyRequestsForCancellationAfterRename()
        {
            var code = @"
class Cat$$
{
    public Cat()
    {
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp, onAfterGlobalSymbolRenamedReturnValue: false))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("Cat", "Cats", invokeAction: true).ConfigureAwait(true);

                Assert.Equal(1, state.RefactorNotifyService.OnBeforeSymbolRenamedCount);
                Assert.Equal(1, state.RefactorNotifyService.OnAfterSymbolRenamedCount);
                state.AssertNotificationMessage();

                // Make sure the rename completed            
                var expectedCode = @"
class Cats
{
    public Cats()
    {
    }
}";
                Assert.Equal(expectedCode, state.HostDocument.TextBuffer.CurrentSnapshot.GetText());
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact, WorkItem(530469)]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotWhenStartedFromTextualWordInTrivia()
        {
            var code = @"
Module Program
    Sub Main()
        Dim [x$$ = 1
    End Sub
End Module";
            using (var state = new RenameTrackingTestState(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.InsertText("]");
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact, WorkItem(530495)]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotWhenCaseCorrectingReference()
        {
            var code = @"
Module Program
    Sub Main()
        $$main()
    End Sub
End Module";
            using (var state = new RenameTrackingTestState(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.Delete();
                await state.AssertTag("main", "ain").ConfigureAwait(true);
                state.EditorOperations.InsertText("M");
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact, WorkItem(599508)]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotWhenNewIdentifierReferenceBinds()
        {
            var code = @"
Module Program
    Sub Main()
        $$[|main|]()
    End Sub
    Sub Foo()
    End Sub
End Module";
            using (var state = new RenameTrackingTestState(code, LanguageNames.VisualBasic))
            {
                var textSpan = state.HostDocument.SelectedSpans.Single();
                state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "Fo");
                await state.AssertTag("main", "Fo").ConfigureAwait(true);
                state.EditorOperations.InsertText("o");
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact, WorkItem(530400)]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotWhenDeclaringEnumMembers()
        {
            var code = @"
Enum E
$$    
End Enum";
            using (var state = new RenameTrackingTestState(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.InsertText("    a");
                state.EditorOperations.InsertText("b");
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact, WorkItem(1028072)]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingDoesNotThrowAggregateException()
        {
            var waitForResult = false;
            Task<RenameTrackingTaggerProvider.TriggerIdentifierKind> notRenamable = Task.FromResult(RenameTrackingTaggerProvider.TriggerIdentifierKind.NotRenamable);
            Assert.False(RenameTrackingTaggerProvider.IsRenamableIdentifier(notRenamable, waitForResult, CancellationToken.None));

            var source = new TaskCompletionSource<RenameTrackingTaggerProvider.TriggerIdentifierKind>();
            Assert.False(RenameTrackingTaggerProvider.IsRenamableIdentifier(source.Task, waitForResult, CancellationToken.None));
            source.TrySetResult(RenameTrackingTaggerProvider.TriggerIdentifierKind.RenamableReference);
            Assert.True(RenameTrackingTaggerProvider.IsRenamableIdentifier(source.Task, waitForResult, CancellationToken.None));

            source = new TaskCompletionSource<RenameTrackingTaggerProvider.TriggerIdentifierKind>();
            source.TrySetCanceled();
            Assert.False(RenameTrackingTaggerProvider.IsRenamableIdentifier(source.Task, waitForResult, CancellationToken.None));
            Assert.False(RenameTrackingTaggerProvider.WaitForIsRenamableIdentifier(source.Task, CancellationToken.None));

            source = new TaskCompletionSource<RenameTrackingTaggerProvider.TriggerIdentifierKind>();
            Assert.Throws<OperationCanceledException>(() => RenameTrackingTaggerProvider.WaitForIsRenamableIdentifier(source.Task, new CancellationToken(canceled: true)));
            source.TrySetException(new Exception());
            Assert.Throws<AggregateException>(() => RenameTrackingTaggerProvider.WaitForIsRenamableIdentifier(source.Task, CancellationToken.None));
        }

        [WpfFact, WorkItem(1063943)]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotFromReferenceWithWrongNumberOfArguments()
        {
            var code = @"
class C
{
    void M(int x)
    {
        M$$();
    }
}";

            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("eow");
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task CancelRenameTracking()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");
                await state.AssertTag("C", "Cat").ConfigureAwait(true);
                state.SendEscape();
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotWhenDeclaringEnumMembersEvenAfterCancellation()
        {
            var code = @"
Enum E
$$    
End Enum";
            using (var state = new RenameTrackingTestState(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.InsertText("    a");
                state.EditorOperations.InsertText("b");
                await state.AssertNoTag().ConfigureAwait(true);
                state.SendEscape();
                state.EditorOperations.InsertText("c");
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [WorkItem(540, "https://github.com/dotnet/roslyn/issues/540")]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingDoesNotProvideDiagnosticAfterCancellation()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");
                await state.AssertTag("C", "Cat").ConfigureAwait(true);

                Assert.NotEmpty(state.GetDocumentDiagnostics());

                state.SendEscape();
                await state.AssertNoTag().ConfigureAwait(true);

                Assert.Empty(state.GetDocumentDiagnostics());
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTracking_Nameof_FromMethodGroupReference()
        {
            var code = @"
class C
{
    void M()
    {
        nameof(M$$).ToString();
    }

    void M(int x)
    {
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");

                await state.AssertTag("M", "Mat", invokeAction: true).ConfigureAwait(true);

                // Make sure the rename completed            
                var expectedCode = @"
class C
{
    void Mat()
    {
        nameof(Mat).ToString();
    }

    void Mat(int x)
    {
    }
}";
                Assert.Equal(expectedCode, state.HostDocument.TextBuffer.CurrentSnapshot.GetText());
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTracking_Nameof_FromMethodDefinition_NoOverloads()
        {
            var code = @"
class C
{
    void M$$()
    {
        nameof(M).ToString();
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");

                await state.AssertTag("M", "Mat", invokeAction: true).ConfigureAwait(true);

                // Make sure the rename completed            
                var expectedCode = @"
class C
{
    void Mat()
    {
        nameof(Mat).ToString();
    }
}";
                Assert.Equal(expectedCode, state.HostDocument.TextBuffer.CurrentSnapshot.GetText());
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTracking_Nameof_FromMethodDefinition_WithOverloads()
        {
            var code = @"
class C
{
    void M$$()
    {
        nameof(M).ToString();
    }

    void M(int x)
    {
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");

                await state.AssertTag("M", "Mat", invokeAction: true).ConfigureAwait(true);

                // Make sure the rename completed            
                var expectedCode = @"
class C
{
    void Mat()
    {
        nameof(M).ToString();
    }

    void M(int x)
    {
    }
}";
                Assert.Equal(expectedCode, state.HostDocument.TextBuffer.CurrentSnapshot.GetText());
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTracking_Nameof_FromReferenceToMetadata_NoTag()
        {
            var code = @"
class C
{
    void M()
    {
        var x = nameof(ToString$$);
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("z");
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [WorkItem(762964)]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTracking_NoTagWhenFirstEditChangesReferenceToAnotherSymbol()
        {
            var code = @"
class C
{
    void M()
    {
        int abc = 7;
        int ab = 8;
        int z = abc$$;
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                await state.AssertNoTag().ConfigureAwait(true);
            }
        }

        [WpfFact]
        [WorkItem(2605, "https://github.com/dotnet/roslyn/issues/2605")]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTracking_CannotRenameToVarInCSharp()
        {
            var code = @"
class C
{
    void M()
    {
        C$$ c;
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                state.EditorOperations.InsertText("va");

                await state.AssertTag("C", "va").ConfigureAwait(true);
                Assert.NotEmpty(state.GetDocumentDiagnostics());

                state.EditorOperations.InsertText("r");
                await state.AssertNoTag().ConfigureAwait(true);
                Assert.Empty(state.GetDocumentDiagnostics());

                state.EditorOperations.InsertText("p");
                await state.AssertTag("C", "varp").ConfigureAwait(true);
                Assert.NotEmpty(state.GetDocumentDiagnostics());
            }
        }

        [WpfFact]
        [WorkItem(2605, "https://github.com/dotnet/roslyn/issues/2605")]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTracking_CannotRenameFromVarInCSharp()
        {
            var code = @"
class C
{
    void M()
    {
        var$$ c = new C();
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                await state.AssertNoTag().ConfigureAwait(true);
                Assert.Empty(state.GetDocumentDiagnostics());
            }
        }

        [WpfFact]
        [WorkItem(2605, "https://github.com/dotnet/roslyn/issues/2605")]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTracking_CanRenameToVarInVisualBasic()
        {
            var code = @"
Class C
    Sub M()
        Dim x as C$$
    End Sub
End Class";
            using (var state = new RenameTrackingTestState(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.Backspace();
                state.EditorOperations.InsertText("var");

                await state.AssertTag("C", "var").ConfigureAwait(true);
                Assert.NotEmpty(state.GetDocumentDiagnostics());
            }
        }

        [WpfFact]
        [WorkItem(2605, "https://github.com/dotnet/roslyn/issues/2605")]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTracking_CannotRenameToDynamicInCSharp()
        {
            var code = @"
class C
{
    void M()
    {
        C$$ c;
    }
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                state.EditorOperations.InsertText("dynami");

                await state.AssertTag("C", "dynami").ConfigureAwait(true);
                Assert.NotEmpty(state.GetDocumentDiagnostics());

                state.EditorOperations.InsertText("c");
                await state.AssertNoTag().ConfigureAwait(true);
                Assert.Empty(state.GetDocumentDiagnostics());

                state.EditorOperations.InsertText("s");
                await state.AssertTag("C", "dynamics").ConfigureAwait(true);
                Assert.NotEmpty(state.GetDocumentDiagnostics());
            }
        }
    }
}
