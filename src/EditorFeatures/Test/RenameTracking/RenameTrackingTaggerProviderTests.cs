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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                await state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingNotInBlankFile()
        {
            var code = @"$$";
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("d");
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");
                await state.AssertTag("C", "Cat");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("AB");
                await state.AssertTag("C", "ABC");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("ZZ");
                await state.AssertTag("ABCD", "ABZZCD");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                await state.AssertTag("ABC", "AB");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Delete();
                await state.AssertTag("ABC", "BC");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                await state.AssertTag("ABC", "AC");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("d");
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("a");
                await state.AssertNoTag();

                state.EditorOperations.InsertText("b");
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");
                await state.AssertTag("C", "Cat");

                state.EditorOperations.InsertText("s");
                await state.AssertTag("C", "Cats");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("d");
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("@");
                await state.AssertTag("C", "@C");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public async Task RenameTrackingHandlesSquareBracketsAsVisualBasicEscape()
        {
            var code = @"
Class $$C
End Class";
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.InsertText("[");
                await state.AssertNoTag();

                state.MoveCaret(1);
                state.EditorOperations.InsertText("]");
                await state.AssertTag("C", "[C]");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("[");
                await state.AssertNoTag();

                state.MoveCaret(1);
                state.EditorOperations.InsertText("]");
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("ДѮѪÛÊÛÄÁÍäáŒŸõàŸüÄµÁiÛEêàêèäåíòèôèêàòîðñëîâî");
                await state.AssertTag("C", "CДѮѪÛÊÛÄÁÍäáŒŸõàŸüÄµÁiÛEêàêèäåíòèôèêàòîðñëîâî");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("n");
                await state.AssertNoTag();

                state.EditorOperations.InsertText("t");
                await state.AssertNoTag();

                state.EditorOperations.InsertText("s");
                await state.AssertTag("i", "ints");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("9");
                await state.AssertNoTag();

                state.MoveCaret(-1);
                state.EditorOperations.InsertText("t");
                await state.AssertTag("abc", "t9abc");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("Abc");
                await state.AssertTag("Def", "AbcDef");

                state.MoveCaret(3);
                state.EditorOperations.InsertText("Ghi");
                await state.AssertTag("Def", "AbcDefGhi");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("C", "Cs");

                state.EditorOperations.Backspace();
                await state.AssertNoTag();

                state.EditorOperations.InsertText("s");
                await state.AssertTag("C", "Cs");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                await state.AssertNoTag();

                state.EditorOperations.InsertText("D");
                await state.AssertTag("C", "D");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                state.MoveCaret(-4);
                state.MoveCaret(4);
                await state.AssertNoTag();

                state.EditorOperations.InsertText("D");
                await state.AssertTag("C", "D");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();

                // Move to previous space
                state.MoveCaret(-1);

                state.EditorOperations.InsertText("D");
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                var textSpan = state.HostDocument.SelectedSpans.Single();
                state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "cation");
                await state.AssertTag("Identifier", "Identification");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                var textSpan = state.HostDocument.SelectedSpans.Single();
                state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "Complex");
                await state.AssertTag("Identifier", "Complexifier");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                var textSpan = state.HostDocument.SelectedSpans.Single();
                state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "Dog");
                await state.AssertTag("Cat", "Dog");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("Cat", "Cats", invokeAction: true);

                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("Cat", "Cats", invokeAction: true);

                await state.AssertNoTag();

                state.EditorOperations.Backspace();
                await state.AssertTag("Cats", "Cat");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("abc");
                await state.AssertTag("Cat", "Catabc", invokeAction: true);

                await state.AssertNoTag();

                // Back to original
                state.Undo();
                await state.AssertNoTag();

                state.EditorOperations.InsertText("xyz");
                await state.AssertTag("Cat", "Catxyz");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("abc");
                await state.AssertTag("Cat", "Catabc", invokeAction: true);

                await state.AssertNoTag();

                // Resume rename tracking session
                state.Undo(2);
                await state.AssertTag("Cat", "Catabc");

                state.EditorOperations.InsertText("xyz");
                await state.AssertTag("Cat", "Catabcxyz");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("Cat", "Cats", invokeAction: true);

                await state.AssertNoTag();

                // Resume rename tracking session
                state.Undo(2);
                await state.AssertTag("Cat", "Cats");

                state.Redo();
                await state.AssertNoTag();

                state.Redo();
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("Cat", "Cats", invokeAction: true);

                await state.AssertNoTag();

                // Resume rename tracking session
                state.Undo(2);
                await state.AssertTag("Cat", "Cats");

                state.Redo(2);
                await state.AssertNoTag();

                // Back to original
                state.Undo();
                await state.AssertNoTag();

                // Resume rename tracking session
                state.Undo();
                await state.AssertTag("Cat", "Cats");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("yz");
                await state.AssertTag("x", "xyz");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("yz");
                await state.AssertTag("x", "xyz");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("A");
                await state.AssertTag("NS", "NSA");
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("Cat", "Cats", invokeAction: true);
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
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp, onBeforeGlobalSymbolRenamedReturnValue: false))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("Cat", "Cats", invokeAction: true);
                Assert.Equal(1, state.RefactorNotifyService.OnBeforeSymbolRenamedCount);

                // Make sure the rename didn't proceed
                Assert.Equal(0, state.RefactorNotifyService.OnAfterSymbolRenamedCount);
                await state.AssertNoTag();

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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp, onAfterGlobalSymbolRenamedReturnValue: false))
            {
                state.EditorOperations.InsertText("s");
                await state.AssertTag("Cat", "Cats", invokeAction: true);

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
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.InsertText("]");
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.Delete();
                await state.AssertTag("main", "ain");
                state.EditorOperations.InsertText("M");
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.VisualBasic))
            {
                var textSpan = state.HostDocument.SelectedSpans.Single();
                state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "Fo");
                await state.AssertTag("main", "Fo");
                state.EditorOperations.InsertText("o");
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.InsertText("    a");
                state.EditorOperations.InsertText("b");
                await state.AssertNoTag();
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

            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("eow");
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");
                await state.AssertTag("C", "Cat");
                state.SendEscape();
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.InsertText("    a");
                state.EditorOperations.InsertText("b");
                await state.AssertNoTag();
                state.SendEscape();
                state.EditorOperations.InsertText("c");
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");
                await state.AssertTag("C", "Cat");

                Assert.NotEmpty(await state.GetDocumentDiagnosticsAsync());

                state.SendEscape();
                await state.AssertNoTag();

                Assert.Empty(await state.GetDocumentDiagnosticsAsync());
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");

                await state.AssertTag("M", "Mat", invokeAction: true);

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
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");

                await state.AssertTag("M", "Mat", invokeAction: true);

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
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");

                await state.AssertTag("M", "Mat", invokeAction: true);

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
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("z");
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                await state.AssertNoTag();
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                state.EditorOperations.InsertText("va");

                await state.AssertTag("C", "va");
                Assert.NotEmpty(await state.GetDocumentDiagnosticsAsync());

                state.EditorOperations.InsertText("r");
                await state.AssertNoTag();
                Assert.Empty(await state.GetDocumentDiagnosticsAsync());

                state.EditorOperations.InsertText("p");
                await state.AssertTag("C", "varp");
                Assert.NotEmpty(await state.GetDocumentDiagnosticsAsync());
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                await state.AssertNoTag();
                Assert.Empty(await state.GetDocumentDiagnosticsAsync());
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.Backspace();
                state.EditorOperations.InsertText("var");

                await state.AssertTag("C", "var");
                Assert.NotEmpty(await state.GetDocumentDiagnosticsAsync());
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
            using (var state = await RenameTrackingTestState.CreateAsync(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                state.EditorOperations.InsertText("dynami");

                await state.AssertTag("C", "dynami");
                Assert.NotEmpty(await state.GetDocumentDiagnosticsAsync());

                state.EditorOperations.InsertText("c");
                await state.AssertNoTag();
                Assert.Empty(await state.GetDocumentDiagnosticsAsync());

                state.EditorOperations.InsertText("s");
                await state.AssertTag("C", "dynamics");
                Assert.NotEmpty(await state.GetDocumentDiagnosticsAsync());
            }
        }
    }
}
