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
        public void RenameTrackingNotOnCreation()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingNotInBlankFile()
        {
            var code = @"$$";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("d");
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingTypingAtEnd()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");
                state.AssertTag("C", "Cat");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingTypingAtBeginning()
        {
            var code = @"
class $$C
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("AB");
                state.AssertTag("C", "ABC");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingTypingInMiddle()
        {
            var code = @"
class AB$$CD
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("ZZ");
                state.AssertTag("ABCD", "ABZZCD");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingDeleteFromEnd()
        {
            var code = @"
class ABC$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                state.AssertTag("ABC", "AB");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingDeleteFromBeginning()
        {
            var code = @"
class $$ABC
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Delete();
                state.AssertTag("ABC", "BC");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingDeleteFromMiddle()
        {
            var code = @"
class AB$$C
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                state.AssertTag("ABC", "AC");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingNotOnClassKeyword()
        {
            var code = @"
class$$ ABCD
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("d");
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingNotAtMethodArgument()
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
                state.AssertNoTag();

                state.EditorOperations.InsertText("b");
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingSessionContinuesAfterViewingTag()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");
                state.AssertTag("C", "Cat");

                state.EditorOperations.InsertText("s");
                state.AssertTag("C", "Cats");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingNotInString()
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
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingHandlesAtSignAsCSharpEscape()
        {
            var code = @"
class $$C
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("@");
                state.AssertTag("C", "@C");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingHandlesSquareBracketsAsVisualBasicEscape()
        {
            var code = @"
Class $$C
End Class";
            using (var state = new RenameTrackingTestState(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.InsertText("[");
                state.AssertNoTag();

                state.MoveCaret(1);
                state.EditorOperations.InsertText("]");
                state.AssertTag("C", "[C]");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingNotOnSquareBracketsInCSharp()
        {
            var code = @"
class $$C
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("[");
                state.AssertNoTag();

                state.MoveCaret(1);
                state.EditorOperations.InsertText("]");
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingHandlesUnicode()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("ДѮѪÛÊÛÄÁÍäáŒŸõàŸüÄµÁiÛEêàêèäåíòèôèêàòîðñëîâî");
                state.AssertTag("C", "CДѮѪÛÊÛÄÁÍäáŒŸõàŸüÄµÁiÛEêàêèäåíòèôèêàòîðñëîâî");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingThroughKeyword()
        {
            var code = @"
class i$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("n");
                state.AssertNoTag();

                state.EditorOperations.InsertText("t");
                state.AssertNoTag();

                state.EditorOperations.InsertText("s");
                state.AssertTag("i", "ints");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingThroughIllegalStartCharacter()
        {
            var code = @"
class $$abc
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("9");
                state.AssertNoTag();

                state.MoveCaret(-1);
                state.EditorOperations.InsertText("t");
                state.AssertTag("abc", "t9abc");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingOnBothSidesOfIdentifier()
        {
            var code = @"
class $$Def
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("Abc");
                state.AssertTag("Def", "AbcDef");

                state.MoveCaret(3);
                state.EditorOperations.InsertText("Ghi");
                state.AssertTag("Def", "AbcDefGhi");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingThroughSameIdentifier()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                state.AssertTag("C", "Cs");

                state.EditorOperations.Backspace();
                state.AssertNoTag();

                state.EditorOperations.InsertText("s");
                state.AssertTag("C", "Cs");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingThroughEmptyString()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.Backspace();
                state.AssertNoTag();

                state.EditorOperations.InsertText("D");
                state.AssertTag("C", "D");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingThroughEmptyStringWithCaretMove()
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
                state.AssertNoTag();

                state.EditorOperations.InsertText("D");
                state.AssertTag("C", "D");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingNotThroughEmptyStringResumeOnDifferentSpace()
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
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingReplaceIdentifierSuffix()
        {
            var code = @"
class Identifi[|er|]$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                var textSpan = state.HostDocument.SelectedSpans.Single();
                state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "cation");
                state.AssertTag("Identifier", "Identification");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingReplaceIdentifierPrefix()
        {
            var code = @"
class $$[|Ident|]ifier
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                var textSpan = state.HostDocument.SelectedSpans.Single();
                state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "Complex");
                state.AssertTag("Identifier", "Complexifier");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingReplaceIdentifierCompletely()
        {
            var code = @"
class [|Cat|]$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                var textSpan = state.HostDocument.SelectedSpans.Single();
                state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "Dog");
                state.AssertTag("Cat", "Dog");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingNotAfterInvoke()
        {
            var code = @"
class Cat$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                state.AssertTag("Cat", "Cats", invokeAction: true);

                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingInvokeAndChangeBackToOriginal()
        {
            var code = @"
class Cat$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                state.AssertTag("Cat", "Cats", invokeAction: true);

                state.AssertNoTag();

                state.EditorOperations.Backspace();
                state.AssertTag("Cats", "Cat");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingUndoOnceAndStartNewSession()
        {
            var code = @"
class Cat$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("abc");
                state.AssertTag("Cat", "Catabc", invokeAction: true);

                state.AssertNoTag();

                // Back to original
                state.Undo();
                state.AssertNoTag();

                state.EditorOperations.InsertText("xyz");
                state.AssertTag("Cat", "Catxyz");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingUndoTwiceAndContinueSession()
        {
            var code = @"
class Cat$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("abc");
                state.AssertTag("Cat", "Catabc", invokeAction: true);

                state.AssertNoTag();

                // Resume rename tracking session
                state.Undo(2);
                state.AssertTag("Cat", "Catabc");

                state.EditorOperations.InsertText("xyz");
                state.AssertTag("Cat", "Catabcxyz");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingRedoAlwaysClearsState()
        {
            var code = @"
class Cat$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                state.AssertTag("Cat", "Cats", invokeAction: true);

                state.AssertNoTag();

                // Resume rename tracking session
                state.Undo(2);
                state.AssertTag("Cat", "Cats");

                state.Redo();
                state.AssertNoTag();

                state.Redo();
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingUndoTwiceRedoTwiceUndoStillWorks()
        {
            var code = @"
class Cat$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("s");
                state.AssertTag("Cat", "Cats", invokeAction: true);

                state.AssertNoTag();

                // Resume rename tracking session
                state.Undo(2);
                state.AssertTag("Cat", "Cats");

                state.Redo(2);
                state.AssertNoTag();

                // Back to original
                state.Undo();
                state.AssertNoTag();

                // Resume rename tracking session
                state.Undo();
                state.AssertTag("Cat", "Cats");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingOnReference_ParameterAsArgument()
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
                state.AssertTag("x", "xyz");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingOnReference_ParameterAsNamedArgument()
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
                state.AssertTag("x", "xyz");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingOnReference_Namespace()
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
                state.AssertTag("NS", "NSA");
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingNotifiesThirdPartiesOfRenameOperation()
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
                state.AssertTag("Cat", "Cats", invokeAction: true);
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
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingHonorsThirdPartyRequestsForCancellationBeforeRename()
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
                state.AssertTag("Cat", "Cats", invokeAction: true);
                Assert.Equal(1, state.RefactorNotifyService.OnBeforeSymbolRenamedCount);

                // Make sure the rename didn't proceed
                Assert.Equal(0, state.RefactorNotifyService.OnAfterSymbolRenamedCount);
                state.AssertNoTag();

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
        public void RenameTrackingAlertsAboutThirdPartyRequestsForCancellationAfterRename()
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
                state.AssertTag("Cat", "Cats", invokeAction: true);

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
                state.AssertNoTag();
            }
        }

        [WpfFact, WorkItem(530469)]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingNotWhenStartedFromTextualWordInTrivia()
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
                state.AssertNoTag();
            }
        }

        [WpfFact, WorkItem(530495)]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingNotWhenCaseCorrectingReference()
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
                state.AssertTag("main", "ain");
                state.EditorOperations.InsertText("M");
                state.AssertNoTag();
            }
        }

        [WpfFact, WorkItem(599508)]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingNotWhenNewIdentifierReferenceBinds()
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
                state.AssertTag("main", "Fo");
                state.EditorOperations.InsertText("o");
                state.AssertNoTag();
            }
        }

        [WpfFact, WorkItem(530400)]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingNotWhenDeclaringEnumMembers()
        {
            var code = @"
Enum E
$$    
End Enum";
            using (var state = new RenameTrackingTestState(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.InsertText("    a");
                state.EditorOperations.InsertText("b");
                state.AssertNoTag();
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
        public void RenameTrackingNotFromReferenceWithWrongNumberOfArguments()
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
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void CancelRenameTracking()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");
                state.AssertTag("C", "Cat");
                state.SendEscape();
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingNotWhenDeclaringEnumMembersEvenAfterCancellation()
        {
            var code = @"
Enum E
$$    
End Enum";
            using (var state = new RenameTrackingTestState(code, LanguageNames.VisualBasic))
            {
                state.EditorOperations.InsertText("    a");
                state.EditorOperations.InsertText("b");
                state.AssertNoTag();
                state.SendEscape();
                state.EditorOperations.InsertText("c");
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [WorkItem(540, "https://github.com/dotnet/roslyn/issues/540")]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTrackingDoesNotProvideDiagnosticAfterCancellation()
        {
            var code = @"
class C$$
{
}";
            using (var state = new RenameTrackingTestState(code, LanguageNames.CSharp))
            {
                state.EditorOperations.InsertText("at");
                state.AssertTag("C", "Cat");

                Assert.NotEmpty(state.GetDocumentDiagnostics());

                state.SendEscape();
                state.AssertNoTag();

                Assert.Empty(state.GetDocumentDiagnostics());
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTracking_Nameof_FromMethodGroupReference()
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

                state.AssertTag("M", "Mat", invokeAction: true);

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
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTracking_Nameof_FromMethodDefinition_NoOverloads()
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

                state.AssertTag("M", "Mat", invokeAction: true);

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
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTracking_Nameof_FromMethodDefinition_WithOverloads()
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

                state.AssertTag("M", "Mat", invokeAction: true);

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
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTracking_Nameof_FromReferenceToMetadata_NoTag()
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
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [WorkItem(762964)]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTracking_NoTagWhenFirstEditChangesReferenceToAnotherSymbol()
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
                state.AssertNoTag();
            }
        }

        [WpfFact]
        [WorkItem(2605, "https://github.com/dotnet/roslyn/issues/2605")]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTracking_CannotRenameToVarInCSharp()
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

                state.AssertTag("C", "va");
                Assert.NotEmpty(state.GetDocumentDiagnostics());

                state.EditorOperations.InsertText("r");
                state.AssertNoTag();
                Assert.Empty(state.GetDocumentDiagnostics());

                state.EditorOperations.InsertText("p");
                state.AssertTag("C", "varp");
                Assert.NotEmpty(state.GetDocumentDiagnostics());
            }
        }

        [WpfFact]
        [WorkItem(2605, "https://github.com/dotnet/roslyn/issues/2605")]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTracking_CannotRenameFromVarInCSharp()
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
                state.AssertNoTag();
                Assert.Empty(state.GetDocumentDiagnostics());
            }
        }

        [WpfFact]
        [WorkItem(2605, "https://github.com/dotnet/roslyn/issues/2605")]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTracking_CanRenameToVarInVisualBasic()
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

                state.AssertTag("C", "var");
                Assert.NotEmpty(state.GetDocumentDiagnostics());
            }
        }

        [WpfFact]
        [WorkItem(2605, "https://github.com/dotnet/roslyn/issues/2605")]
        [Trait(Traits.Feature, Traits.Features.RenameTracking)]
        public void RenameTracking_CannotRenameToDynamicInCSharp()
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

                state.AssertTag("C", "dynami");
                Assert.NotEmpty(state.GetDocumentDiagnostics());

                state.EditorOperations.InsertText("c");
                state.AssertNoTag();
                Assert.Empty(state.GetDocumentDiagnostics());

                state.EditorOperations.InsertText("s");
                state.AssertTag("C", "dynamics");
                Assert.NotEmpty(state.GetDocumentDiagnostics());
            }
        }
    }
}
