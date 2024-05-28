// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.RenameTracking;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.RenameTracking)]
public class RenameTrackingTaggerProviderTests
{
    [WpfFact]
    public async Task RenameTrackingNotOnCreation()
    {
        var code = @"
class C$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        await state.AssertNoTag();
    }

    [WpfFact]
    public async Task RenameTrackingNotInBlankFile()
    {
        var code = @"$$";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("d");
        await state.AssertNoTag();
    }

    [WpfFact]
    public async Task RenameTrackingTypingAtEnd()
    {
        var code = @"
class C$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("at");
        await state.AssertTag("C", "Cat");
    }

    [WpfFact]
    public async Task RenameTrackingTypingAtBeginning()
    {
        var code = @"
class $$C
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("AB");
        await state.AssertTag("C", "ABC");
    }

    [WpfFact]
    public async Task RenameTrackingTypingInMiddle()
    {
        var code = @"
class AB$$CD
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("ZZ");
        await state.AssertTag("ABCD", "ABZZCD");
    }

    [WpfFact]
    public async Task RenameTrackingDeleteFromEnd()
    {
        var code = @"
class ABC$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.Backspace();
        await state.AssertTag("ABC", "AB");
    }

    [WpfFact]
    public async Task RenameTrackingDeleteFromBeginning()
    {
        var code = @"
class $$ABC
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.Delete();
        await state.AssertTag("ABC", "BC");
    }

    [WpfFact]
    public async Task RenameTrackingDeleteFromMiddle()
    {
        var code = @"
class AB$$C
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.Backspace();
        await state.AssertTag("ABC", "AC");
    }

    [WpfFact]
    public async Task RenameTrackingNotOnClassKeyword()
    {
        var code = @"
class$$ ABCD
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("d");
        await state.AssertNoTag();
    }

    [WpfFact]
    public async Task RenameTrackingNotAtMethodArgument()
    {
        var code = @"
class ABCD
{
    void Goo(int x)
    {
        int abc = 3;
        Goo($$
    }
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("a");
        await state.AssertNoTag();

        state.EditorOperations.InsertText("b");
        await state.AssertNoTag();
    }

    [WpfFact]
    public async Task RenameTrackingSessionContinuesAfterViewingTag()
    {
        var code = @"
class C$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("at");
        await state.AssertTag("C", "Cat");

        state.EditorOperations.InsertText("s");
        await state.AssertTag("C", "Cats");
    }

    [WpfFact]
    public async Task RenameTrackingNotInString()
    {
        var code = @"
class C
{
    void Goo()
    {
        string s = ""abc$$""
    }
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("d");
        await state.AssertNoTag();
    }

    [WpfFact]
    public async Task RenameTrackingHandlesAtSignAsCSharpEscape()
    {
        var code = @"
class $$C
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("@");
        await state.AssertTag("C", "@C");
    }

    [WpfFact]
    public async Task RenameTrackingHandlesSquareBracketsAsVisualBasicEscape()
    {
        var code = @"
Class $$C
End Class";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        state.EditorOperations.InsertText("[");
        await state.AssertNoTag();

        state.MoveCaret(1);
        state.EditorOperations.InsertText("]");
        await state.AssertTag("C", "[C]");
    }

    [WpfFact]
    public async Task RenameTrackingNotOnSquareBracketsInCSharp()
    {
        var code = @"
class $$C
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("[");
        await state.AssertNoTag();

        state.MoveCaret(1);
        state.EditorOperations.InsertText("]");
        await state.AssertNoTag();
    }

    [WpfFact]
    public async Task RenameTrackingHandlesUnicode()
    {
        var code = @"
class C$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("\u0414\u046E\u046A\u00DB\u00CA\u00DB\u00C4\u00C1\u00CD\u00E4\u00E1\u0152\u0178\u00F5\u00E0\u0178\u00FC\u00C4\u00B5\u00C1i\u00DBE\u00EA\u00E0\u00EA\u00E8\u00E4\u00E5\u00ED\u00F2\u00E8\u00F4\u00E8\u00EA\u00E0\u00F2\u00EE\u00F0\u00F1\u00EB\u00EE\u00E2\u00EE");
        await state.AssertTag("C", "C\u0414\u046E\u046A\u00DB\u00CA\u00DB\u00C4\u00C1\u00CD\u00E4\u00E1\u0152\u0178\u00F5\u00E0\u0178\u00FC\u00C4\u00B5\u00C1i\u00DBE\u00EA\u00E0\u00EA\u00E8\u00E4\u00E5\u00ED\u00F2\u00E8\u00F4\u00E8\u00EA\u00E0\u00F2\u00EE\u00F0\u00F1\u00EB\u00EE\u00E2\u00EE");
    }

    [WpfFact]
    public async Task RenameTrackingThroughKeyword()
    {
        var code = @"
class i$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("n");
        await state.AssertNoTag();

        state.EditorOperations.InsertText("t");
        await state.AssertNoTag();

        state.EditorOperations.InsertText("s");
        await state.AssertTag("i", "ints");
    }

    [WpfFact]
    public async Task RenameTrackingThroughIllegalStartCharacter()
    {
        var code = @"
class $$abc
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("9");
        await state.AssertNoTag();

        state.MoveCaret(-1);
        state.EditorOperations.InsertText("t");
        await state.AssertTag("abc", "t9abc");
    }

    [WpfFact]
    public async Task RenameTrackingOnBothSidesOfIdentifier()
    {
        var code = @"
class $$Def
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("Abc");
        await state.AssertTag("Def", "AbcDef");

        state.MoveCaret(3);
        state.EditorOperations.InsertText("Ghi");
        await state.AssertTag("Def", "AbcDefGhi");
    }

    [WpfFact]
    public async Task RenameTrackingThroughSameIdentifier()
    {
        var code = @"
class C$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("s");
        await state.AssertTag("C", "Cs");

        state.EditorOperations.Backspace();
        await state.AssertNoTag();

        state.EditorOperations.InsertText("s");
        await state.AssertTag("C", "Cs");
    }

    [WpfFact]
    public async Task RenameTrackingThroughEmptyString()
    {
        var code = @"
class C$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.Backspace();
        await state.AssertNoTag();

        state.EditorOperations.InsertText("D");
        await state.AssertTag("C", "D");
    }

    [WpfFact]
    public async Task RenameTrackingThroughEmptyStringWithCaretMove()
    {
        var code = @"
class C$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.Backspace();
        state.MoveCaret(-4);
        state.MoveCaret(4);
        await state.AssertNoTag();

        state.EditorOperations.InsertText("D");
        await state.AssertTag("C", "D");
    }

    [WpfFact]
    public async Task RenameTrackingNotThroughEmptyStringResumeOnDifferentSpace()
    {
        var code = @"
class  C$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.Backspace();

        // Move to previous space
        state.MoveCaret(-1);

        state.EditorOperations.InsertText("D");
        await state.AssertNoTag();
    }

    [WpfFact]
    public async Task RenameTrackingReplaceIdentifierSuffix()
    {
        var code = @"
class Identifi[|er|]$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        var textSpan = state.HostDocument.SelectedSpans.Single();
        state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "cation");
        await state.AssertTag("Identifier", "Identification");
    }

    [WpfFact]
    public async Task RenameTrackingReplaceIdentifierPrefix()
    {
        var code = @"
class $$[|Ident|]ifier
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        var textSpan = state.HostDocument.SelectedSpans.Single();
        state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "Complex");
        await state.AssertTag("Identifier", "Complexifier");
    }

    [WpfFact]
    public async Task RenameTrackingReplaceIdentifierCompletely()
    {
        var code = @"
class [|Cat|]$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        var textSpan = state.HostDocument.SelectedSpans.Single();
        state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "Dog");
        await state.AssertTag("Cat", "Dog");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34280")]
    public async Task RenameTrackingReplaceIdentifierWithDiscard()
    {
        var code = @"
class Class
{
    int Method()
    {
        int i;
        [|i|]$$ = Method();
        rteurn 0;
    }
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        var textSpan = state.HostDocument.SelectedSpans.Single();
        state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "_");
        await state.AssertNoTag();
    }

    [WpfFact]
    public async Task RenameTrackingNotAfterInvoke()
    {
        var code = @"
class Cat$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("s");
        await state.AssertTag("Cat", "Cats", invokeAction: true);

        await state.AssertNoTag();
    }

    [WpfFact]
    public async Task RenameTrackingInvokeAndChangeBackToOriginal()
    {
        var code = @"
class Cat$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("s");
        await state.AssertTag("Cat", "Cats", invokeAction: true);

        await state.AssertNoTag();

        state.EditorOperations.Backspace();
        await state.AssertTag("Cats", "Cat");
    }

    [WpfFact]
    public async Task RenameTrackingUndoOnceAndStartNewSession()
    {
        var code = @"
class Cat$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("abc");
        await state.AssertTag("Cat", "Catabc", invokeAction: true);

        await state.AssertNoTag();

        // Back to original
        state.Undo();
        await state.AssertNoTag();

        state.EditorOperations.InsertText("xyz");
        await state.AssertTag("Cat", "Catxyz");
    }

    [WpfFact]
    public async Task RenameTrackingUndoTwiceAndContinueSession()
    {
        var code = @"
class Cat$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("abc");
        await state.AssertTag("Cat", "Catabc", invokeAction: true);

        await state.AssertNoTag();

        // Resume rename tracking session
        state.Undo(2);
        await state.AssertTag("Cat", "Catabc");

        state.EditorOperations.InsertText("xyz");
        await state.AssertTag("Cat", "Catabcxyz");
    }

    [WpfFact]
    public async Task RenameTrackingRedoAlwaysClearsState()
    {
        var code = @"
class Cat$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
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

    [WpfFact]
    public async Task RenameTrackingUndoTwiceRedoTwiceUndoStillWorks()
    {
        var code = @"
class Cat$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
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

    [WpfFact]
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
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("yz");
        await state.AssertTag("x", "xyz");
    }

    [WpfFact]
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
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("yz");
        await state.AssertTag("x", "xyz");
    }

    [WpfFact]
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
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("A");
        await state.AssertTag("NS", "NSA");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/21657")]
    public async Task RenameTrackingOnReference_Attribute_CSharp()
    {
        var code = @"
using System;

class [|$$ustom|]Attribute : Attribute
{
}
";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("C");
        await state.AssertTag("ustomAttribute", "CustomAttribute", invokeAction: true);
        var expectedCode = @"
using System;

class CustomAttribute : Attribute
{
}
";
        Assert.Equal(expectedCode, state.HostDocument.GetTextBuffer().CurrentSnapshot.GetText());
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/21657")]
    public async Task RenameTrackingOnReference_Attribute_VB()
    {
        var code = @"
Import System;

Public Class [|$$ustom|]Attribute 
        Inherits Attribute
End Class
";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        state.EditorOperations.InsertText("C");
        await state.AssertTag("ustomAttribute", "CustomAttribute", invokeAction: true);
        var expectedCode = @"
Import System;

Public Class CustomAttribute 
        Inherits Attribute
End Class
";
        Assert.Equal(expectedCode, state.HostDocument.GetTextBuffer().CurrentSnapshot.GetText());
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/21657")]
    public async Task RenameTrackingOnReference_Capitalized_Attribute_VB()
    {
        var code = @"
Import System;

Public Class [|$$ustom|]ATTRIBUTE 
        Inherits Attribute
End Class
";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        state.EditorOperations.InsertText("C");
        await state.AssertTag("ustomATTRIBUTE", "CustomATTRIBUTE", invokeAction: true);
        var expectedCode = @"
Import System;

Public Class CustomATTRIBUTE 
        Inherits Attribute
End Class
";
        Assert.Equal(expectedCode, state.HostDocument.GetTextBuffer().CurrentSnapshot.GetText());
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/21657")]
    public async Task RenameTrackingOnReference_Not_Capitalized_Attribute_VB()
    {
        var code = @"
Import System;

Public Class [|$$ustom|]attribute 
        Inherits Attribute
End Class
";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        state.EditorOperations.InsertText("C");
        await state.AssertTag("ustomattribute", "Customattribute", invokeAction: true);
        var expectedCode = @"
Import System;

Public Class Customattribute 
        Inherits Attribute
End Class
";
        Assert.Equal(expectedCode, state.HostDocument.GetTextBuffer().CurrentSnapshot.GetText());
    }

    [WpfFact]
    public async Task RenameTrackingNotifiesThirdPartiesOfRenameOperation()
    {
        var code = @"
class Cat$$
{
    public Cat()
    {
    }
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
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
        Assert.Equal(expectedCode, state.HostDocument.GetTextBuffer().CurrentSnapshot.GetText());

        state.AssertNoNotificationMessage();
        await state.AssertNoTag();
    }

    [WpfFact]
    public async Task RenameTrackingHonorsThirdPartyRequestsForCancellationBeforeRename()
    {
        var code = @"
class Cat$$
{
    public Cat()
    {
    }
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp, onBeforeGlobalSymbolRenamedReturnValue: false);
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
        Assert.Equal(expectedCode, state.HostDocument.GetTextBuffer().CurrentSnapshot.GetText());

        state.AssertNotificationMessage();
    }

    [WpfFact]
    public async Task RenameTrackingAlertsAboutThirdPartyRequestsForCancellationAfterRename()
    {
        var code = @"
class Cat$$
{
    public Cat()
    {
    }
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp, onAfterGlobalSymbolRenamedReturnValue: false);
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
        Assert.Equal(expectedCode, state.HostDocument.GetTextBuffer().CurrentSnapshot.GetText());
        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530469")]
    public async Task RenameTrackingNotWhenStartedFromTextualWordInTrivia()
    {
        var code = @"
Module Program
    Sub Main()
        Dim [x$$ = 1
    End Sub
End Module";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        state.EditorOperations.InsertText("]");
        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530495")]
    public async Task RenameTrackingNotWhenCaseCorrectingReference()
    {
        var code = @"
Module Program
    Sub Main()
        $$main()
    End Sub
End Module";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        state.EditorOperations.Delete();
        await state.AssertTag("main", "ain");
        state.EditorOperations.InsertText("M");
        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/599508")]
    public async Task RenameTrackingNotWhenNewIdentifierReferenceBinds()
    {
        var code = @"
Module Program
    Sub Main()
        $$[|main|]()
    End Sub
    Sub Goo()
    End Sub
End Module";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        var textSpan = state.HostDocument.SelectedSpans.Single();
        state.EditorOperations.ReplaceText(new Span(textSpan.Start, textSpan.Length), "Go");
        await state.AssertTag("main", "Go");
        state.EditorOperations.InsertText("o");
        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530400")]
    public async Task RenameTrackingNotWhenDeclaringEnumMembers()
    {
        var code = @"
Enum E
$$    
End Enum";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        state.EditorOperations.InsertText("    a");
        state.EditorOperations.InsertText("b");
        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1028072")]
    public void RenameTrackingDoesNotThrowAggregateException()
    {
        var waitForResult = false;
        var notRenamable = Task.FromResult(RenameTrackingTaggerProvider.TriggerIdentifierKind.NotRenamable);
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
        source.TrySetException(new OperationCanceledException());
        Assert.False(RenameTrackingTaggerProvider.IsRenamableIdentifier(source.Task, waitForResult, CancellationToken.None));
        Assert.False(RenameTrackingTaggerProvider.WaitForIsRenamableIdentifier(source.Task, CancellationToken.None));
        Assert.False(RenameTrackingTaggerProvider.WaitForIsRenamableIdentifier(source.Task, new CancellationTokenSource().Token));

        source = new TaskCompletionSource<RenameTrackingTaggerProvider.TriggerIdentifierKind>();
        Assert.Throws<OperationCanceledException>(() => RenameTrackingTaggerProvider.WaitForIsRenamableIdentifier(source.Task, new CancellationToken(canceled: true)));
        var thrownException = new Exception();
        source.TrySetException(thrownException);
        var caughtException = Assert.Throws<Exception>(() => RenameTrackingTaggerProvider.WaitForIsRenamableIdentifier(source.Task, CancellationToken.None));
        Assert.Same(thrownException, caughtException);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1063943")]
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

        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("eow");
        await state.AssertNoTag();
    }

    [WpfFact]
    public async Task CancelRenameTracking()
    {
        var code = @"
class C$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("at");
        await state.AssertTag("C", "Cat");
        state.SendEscape();
        await state.AssertNoTag();
    }

    [WpfFact]
    public async Task RenameTrackingNotWhenDeclaringEnumMembersEvenAfterCancellation()
    {
        var code = @"
Enum E
$$    
End Enum";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        state.EditorOperations.InsertText("    a");
        state.EditorOperations.InsertText("b");
        await state.AssertNoTag();
        state.SendEscape();
        state.EditorOperations.InsertText("c");
        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/540")]
    public async Task RenameTrackingDoesNotProvideDiagnosticAfterCancellation()
    {
        var code = @"
class C$$
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("at");
        await state.AssertTag("C", "Cat");

        Assert.NotNull(await state.TryGetCodeActionAsync());

        state.SendEscape();
        await state.AssertNoTag();

        Assert.Null(await state.TryGetCodeActionAsync());
    }

    [WpfFact]
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
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
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
        Assert.Equal(expectedCode, state.HostDocument.GetTextBuffer().CurrentSnapshot.GetText());
        await state.AssertNoTag();
    }

    [WpfFact]
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
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
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
        Assert.Equal(expectedCode, state.HostDocument.GetTextBuffer().CurrentSnapshot.GetText());
        await state.AssertNoTag();
    }

    [WpfFact]
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
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
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
        Assert.Equal(expectedCode, state.HostDocument.GetTextBuffer().CurrentSnapshot.GetText());
        await state.AssertNoTag();
    }

    [WpfFact]
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
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("z");
        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/762964")]
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
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.Backspace();
        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2605")]
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
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.Backspace();
        state.EditorOperations.InsertText("va");

        await state.AssertTag("C", "va");
        Assert.NotNull(await state.TryGetCodeActionAsync());

        state.EditorOperations.InsertText("r");
        await state.AssertNoTag();
        Assert.Null(await state.TryGetCodeActionAsync());

        state.EditorOperations.InsertText("p");
        await state.AssertTag("C", "varp");
        Assert.NotNull(await state.TryGetCodeActionAsync());
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2605")]
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
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.Backspace();
        await state.AssertNoTag();
        Assert.Null(await state.TryGetCodeActionAsync());
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2605")]
    public async Task RenameTracking_CanRenameToVarInVisualBasic()
    {
        var code = @"
Class C
    Sub M()
        Dim x as C$$
    End Sub
End Class";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        state.EditorOperations.Backspace();
        state.EditorOperations.InsertText("var");

        await state.AssertTag("C", "var");
        Assert.NotNull(await state.TryGetCodeActionAsync());
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2605")]
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
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.Backspace();
        state.EditorOperations.InsertText("dynami");

        await state.AssertTag("C", "dynami");
        Assert.NotNull(await state.TryGetCodeActionAsync());

        state.EditorOperations.InsertText("c");
        await state.AssertNoTag();
        Assert.Null(await state.TryGetCodeActionAsync());

        state.EditorOperations.InsertText("s");
        await state.AssertTag("C", "dynamics");
        Assert.NotNull(await state.TryGetCodeActionAsync());
    }

    [WpfFact]
    public async Task RenameImplicitTupleField()
    {
        var code = @"
class C
{
    void M()
    {
        (int, int) x = (1, 2);
        var y = x.Item1$$;
    }
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.Backspace();
        state.EditorOperations.Backspace();
        await state.AssertNoTag();
        Assert.Null(await state.TryGetCodeActionAsync());
    }

    [WpfFact]
    public async Task RenameImplicitTupleFieldVB()
    {
        var code = @"
class C
    Sub M()
        Dim x as (Integer, Integer) = (1, 2)
        Dim y = x.Item1$$
    End Sub
End Class
";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        state.EditorOperations.Backspace();
        state.EditorOperations.Backspace();
        await state.AssertNoTag();
        Assert.Null(await state.TryGetCodeActionAsync());
    }

    [WpfFact]
    public async Task RenameImplicitTupleFieldExtended()
    {
        var code = @"
class C
{
    void M()
    {
        (int, int, int, int, int, int, int, int, int, int) x = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        var y = x.Item9$$;
    }
}
";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.Backspace();
        state.EditorOperations.Backspace();
        await state.AssertNoTag();
        Assert.Null(await state.TryGetCodeActionAsync());
    }

    [WpfFact]
    public async Task RenameImplicitTupleFieldExtendedVB()
    {
        var code = @"
Class C
    Sub M()
        Dim x as (Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer, Integer) = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10)
        Dim y = x.Item9$$
    End Sub
End Class
";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        state.EditorOperations.Backspace();
        state.EditorOperations.Backspace();
        await state.AssertNoTag();
        Assert.Null(await state.TryGetCodeActionAsync());
    }

    [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?_a=edit&id=371205")]
    public async Task RenameTrackingNotOnExplicitTupleReturnDeclaration_CSharp()
    {
        var code = @"
class C
{
    void M()
    {
        (int abc$$, int) x = (1, 2);
        var y = x.abc;
    }
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.Backspace();
        state.EditorOperations.Backspace();

        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?_a=edit&id=371205")]
    public async Task RenameTrackingNotOnExplicitTupleReturnDeclaration_VB()
    {
        var code = @"
class C
    Sub M()
        Dim x as (abc$$ as integer, int Item2 as integer) = (1, 2)
        Dim y = x.abc
    End Sub
End Class";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        state.EditorOperations.Backspace();
        state.EditorOperations.Backspace();

        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?_a=edit&id=371205")]
    public async Task RenameTrackingNotOnExplicitTupleFieldReference_CSharp()
    {
        var code = @"
class C
{
    void M()
    {
        (int abc, int) x = (1, 2);
        var y = x.abc$$;
    }
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.Backspace();
        state.EditorOperations.Backspace();

        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?_a=edit&id=371205")]
    public async Task RenameTrackingNotOnExplicitTupleFieldReference_VB()
    {
        var code = @"
class C
    Sub M()
        Dim x as (abc as integer, int Item2 as integer) = (1, 2)
        Dim y = x.abc$$
    End Sub
End Class";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        state.EditorOperations.Backspace();
        state.EditorOperations.Backspace();

        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?_a=edit&id=371205")]
    public async Task RenameTrackingNotOnExplicitTupleElementsInDeclarations_CSharp()
    {
        var code = @"
class C
{
    void M()
    {
        var t = (x$$: 1, y: 2);
    }
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("2");
        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?_a=edit&id=371205")]
    public async Task RenameTrackingNotOnExplicitTupleElementsInDeclarations_VB()
    {
        var code = @"
Class C
    Sub M()
        Dim t = (x$$:=1, y:=2)
    End Sub
End Class";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.VisualBasic);
        state.EditorOperations.InsertText("2");
        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/14159")]
    public async Task RenameTrackingNotOnWellKnownValueTupleType()
    {
        var workspaceXml = @"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" LanguageVersion=""7"">
        <Document>
using System;

class C
{
    void M()
    {
        var x = new ValueTuple$$&lt;int&gt;();
    }
}

namespace System
{
    public struct ValueTuple&lt;T1&gt;
    {
        public T1 Item1;
    }
}
        </Document>
    </Project>
</Workspace>";
        using var state = RenameTrackingTestState.CreateFromWorkspaceXml(workspaceXml, LanguageNames.CSharp);
        state.EditorOperations.InsertText("2");
        await state.AssertNoTag();
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/14159")]
    public async Task RenameTrackingOnThingsCalledValueTupleThatAreNotTheWellKnownType()
    {
        var workspaceXml = @"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" LanguageVersion=""7"">
        <Document>
class C
{
    void M()
    {
        var x = new ValueTuple$$&lt;int&gt;();
    }
}

public struct ValueTuple&lt;T1&gt;
{
    public T1 Item1;
}
        </Document>
    </Project>
</Workspace>";
        using var state = RenameTrackingTestState.CreateFromWorkspaceXml(workspaceXml, LanguageNames.CSharp);
        state.EditorOperations.InsertText("2");
        await state.AssertTag("ValueTuple", "ValueTuple2");
    }

    [WpfFact]
    public async Task RenameTrackingOnDeconstruct()
    {
        var code = @"
class C
{
    void Deconstruct$$(out int x1, out int x2) { x1 = 1; x2 = 2; }
    void M()
    {
        var (y1, y2) = this;
    }
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("2");
        await state.AssertTag("Deconstruct", "Deconstruct2");
    }

    [WpfFact]
    public async Task RenameTracking_UnmanagedConstraint_Keyword()
    {
        var code = @"
class C&lt;T&gt; where T : $$unmanaged
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        await state.AssertNoTag();
    }

    [WpfFact]
    public async Task RenameTracking_UnmanagedConstraint_Type()
    {
        var code = @"
interface unmanaged
{
}
class C&lt;T&gt; where T : $$unmanaged
{
}";
        using var state = RenameTrackingTestState.Create(code, LanguageNames.CSharp);
        state.EditorOperations.InsertText("my");

        await state.AssertTag("unmanaged", "myunmanaged", invokeAction: true);

        // Make sure the rename completed            
        var expectedCode = @"
interface myunmanaged
{
}
class C<T> where T : myunmanaged
{
}";
        Assert.Equal(expectedCode, state.HostDocument.GetTextBuffer().CurrentSnapshot.GetText());
        await state.AssertNoTag();
    }
}
