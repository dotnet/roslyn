' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.CSharp.Formatting
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Tags
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Text.Projection
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpCompletionCommandHandlerTests

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnExtendedPropertyPattern_FirstNested(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class C
{
    public C2 CProperty { get; set; }
}
public class C2
{
    public int IntProperty { get; set; }
    void M(C c)
    {
        _ = c is { CProperty$$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars(".")
                Await state.AssertSelectedCompletionItem(displayText:="IntProperty", isHardSelected:=False)

                state.SendTypeChars("IP")
                Await state.AssertSelectedCompletionItem(displayText:="IntProperty", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(": 2 }")
                Await state.AssertNoCompletionSession()
                Assert.Contains("c is { CProperty.IntProperty: 2 }", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnListPattern_FirstNested(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class C
{
}
public class C2
{
    public C2 CProperty { get; set; }
    public int IntProperty { get; set; }
    void M(C c)
    {
        _ = c is { $$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                ' This is the expected behavior until we implement support for list-patterns.
                state.SendTypeChars("CP")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnExtendedPropertyPattern_Hidden(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" LanguageVersion="Preview" CommonReferences="true">
                        <ProjectReference>VBAssembly1</ProjectReference>
                        <Document FilePath="C.cs">
public class C3
{
    void M(C c)
    {
        _ = c is { CProperty$$
    }
}
                        </Document>
                    </Project>
                    <Project Language="Visual Basic" AssemblyName="VBAssembly1" CommonReferences="true">
                        <Document><![CDATA[
Public Class C
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Property CProperty As C2
End Class

Public Class C2
    Public Property IntProperty As Integer
End Class
                        ]]></Document>
                    </Project>
                </Workspace>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertSelectedCompletionItem(displayText:="IntProperty", isHardSelected:=False)

                state.SendTypeChars("IP")
                Await state.AssertSelectedCompletionItem(displayText:="IntProperty", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(": 2 }")
                Await state.AssertNoCompletionSession()
                Assert.Contains("c is { CProperty.IntProperty: 2 }", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnExtendedPropertyPattern_SecondNested(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class C
{
    public C2 C2Property { get; set; }
}
public class C2
{
    public C3 C3Property { get; set; }
}
public class C3
{
    public int IntProperty { get; set; }
    void M(C c)
    {
        _ = c is { C2Property.C3Property$$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars(".")
                Await state.AssertSelectedCompletionItem(displayText:="IntProperty", isHardSelected:=False)

                state.SendTypeChars("IP")
                Await state.AssertSelectedCompletionItem(displayText:="IntProperty", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(": 2 }")
                Await state.AssertNoCompletionSession()
                Assert.Contains("c is { C2Property.C3Property.IntProperty: 2 }", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnExtendedPropertyPattern_SecondNested_Fields(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class C
{
    public C2 C2Field;
}
public class C2
{
    public C3 C3Field;
}
public class C3
{
    public int IntField;
    void M(C c)
    {
        _ = c is { C2Field$$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars(".")
                Await state.AssertSelectedCompletionItem(displayText:="C3Field", isHardSelected:=False)

                state.SendTypeChars("CF")
                Await state.AssertSelectedCompletionItem(displayText:="C3Field", isHardSelected:=True)

                state.SendTypeChars(".")
                Await state.AssertSelectedCompletionItem(displayText:="IntField", isHardSelected:=False)

                state.SendTypeChars("IF")
                Await state.AssertSelectedCompletionItem(displayText:="IntField", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(": 2 }")
                Await state.AssertNoCompletionSession()
                Assert.Contains("c is { C2Field.C3Field.IntField: 2 }", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnExtendedPropertyPattern_ErrorProperty(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class C
{
    public int IntProperty { get; set; }
    void M(C c)
    {
        _ = c is { Error$$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars(".")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("IP")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnExtendedPropertyPattern(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class C
{
    public C2 CProperty { get; set; }
}
public class C2
{
    public int IntProperty { get; set; }
    void M(C c)
    {
        _ = c is $$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars("{ ")
                Await state.AssertSelectedCompletionItem(displayText:="CProperty", isHardSelected:=False)

                state.SendTypeChars("CP")
                Await state.AssertSelectedCompletionItem(displayText:="CProperty", isHardSelected:=True)

                state.SendTypeChars(".")
                Assert.Contains("c is { CProperty.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertSelectedCompletionItem(displayText:="IntProperty", isHardSelected:=False)

                state.SendTypeChars("IP")
                Await state.AssertSelectedCompletionItem(displayText:="IntProperty", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(": 2 }")
                Await state.AssertNoCompletionSession()
                Assert.Contains("c is { CProperty.IntProperty: 2 }", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnExtendedPropertyPattern_AlreadyTestedBySimplePattern(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class C
{
    public C2 CProperty { get; set; }
}
public class C2
{
    public int IntProperty { get; set; }
    void M(C c)
    {
        _ = c is { CProperty: 2$$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                ' No second completion since already tested at top-level
                state.SendTypeChars(", ")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("CP")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnExtendedPropertyPattern_AlreadyTestedByExtendedPattern(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class C
{
    public C2 CProperty { get; set; }
}
public class C2
{
    public int IntProperty { get; set; }
    public short ShortProperty { get; set; }
    void M(C c)
    {
        _ = c is { CProperty.IntProperty: 2$$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars(", ")
                Await state.AssertSelectedCompletionItem(displayText:="CProperty", isHardSelected:=False)

                state.SendTypeChars("CP")
                Await state.AssertSelectedCompletionItem(displayText:="CProperty", isHardSelected:=True)

                state.SendTypeChars(".")
                Assert.Contains("is { CProperty.IntProperty: 2, CProperty.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                ' Note: same completion is offered a second time
                Await state.AssertSelectedCompletionItem(displayText:="IntProperty", isHardSelected:=False)

                state.SendTypeChars("SP")
                Await state.AssertSelectedCompletionItem(displayText:="ShortProperty", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(": 3")
                Await state.AssertNoCompletionSession()
                Assert.Contains("is { CProperty.IntProperty: 2, CProperty.ShortProperty: 3", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnExtendedPropertyPattern_AlreadyTestedByNestedPattern(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class C
{
    public C2 CProperty { get; set; }
}
public class C2
{
    public int IntProperty { get; set; }
    public short ShortProperty { get; set; }
    void M(C c)
    {
        _ = c is { CProperty: { IntProperty: 2 }$$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars(", ")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("CProperty")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars(".")
                Assert.Contains("is { CProperty: { IntProperty: 2 }, CProperty.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                ' Note: same completion is offered a second time
                Await state.AssertSelectedCompletionItem(displayText:="IntProperty", isHardSelected:=False)

                state.SendTypeChars("SP")
                Await state.AssertSelectedCompletionItem(displayText:="ShortProperty", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(": 3")
                Await state.AssertNoCompletionSession()
                Assert.Contains("is { CProperty: { IntProperty: 2 }, CProperty.ShortProperty: 3", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnExtendedPropertyPattern_BeforeAnotherPattern(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class C
{
    public C2 CProperty { get; set; }
}
public class C2
{
    public int IntProperty { get; set; }
    public short ShortProperty { get; set; }
    void M(C c)
    {
        _ = c is {$$ CProperty.IntProperty: 2 }
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="CProperty", isHardSelected:=False)

                state.SendTypeChars("CP")
                Await state.AssertSelectedCompletionItem(displayText:="CProperty", isHardSelected:=True)

                state.SendTypeChars(".")
                Assert.Contains("is { CProperty. CProperty.IntProperty: 2 }", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertSelectedCompletionItem(displayText:="Equals", isHardSelected:=False)

                state.SendTypeChars("SP")
                Await state.AssertSelectedCompletionItem(displayText:="ShortProperty", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(": 3,")
                Await state.AssertNoCompletionSession()
                Assert.Contains("is { CProperty.ShortProperty: 3, CProperty.IntProperty: 2 }", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnPropertyPattern_BeforeAnotherPattern(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class C
{
    public int IntProperty { get; set; }
    public short ShortProperty { get; set; }
}
public class C2
{
    void M(C c)
    {
        _ = c is {$$ IntProperty: 2 }
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="ShortProperty", isHardSelected:=False)

                state.SendTypeChars("SP")
                Await state.AssertSelectedCompletionItem(displayText:="ShortProperty", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(": 3,")
                Await state.AssertNoCompletionSession()
                Assert.Contains("is { ShortProperty: 3, IntProperty: 2 }", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnRecordBaseType(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
record Base(int Alice, int Bob);
record Derived(int Other) : [|Base$$|]
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp9)

                state.SendTypeChars("(")
                If showCompletionInArgumentLists Then
                    Await state.AssertSelectedCompletionItem(displayText:="Alice:", isHardSelected:=False)
                End If

                state.SendTypeChars("A")

                If showCompletionInArgumentLists Then
                    Await state.AssertSelectedCompletionItem(displayText:="Alice:", isHardSelected:=True)
                End If

                state.SendTypeChars(": 1, B")

                If showCompletionInArgumentLists Then
                    Await state.AssertSelectedCompletionItem(displayText:="Bob:", isHardSelected:=True)
                End If

                state.SendTab()
                state.SendTypeChars(": 2)")

                Await state.AssertNoCompletionSession()
                Assert.Contains(": Base(Alice: 1, Bob: 2)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(46397, "https://github.com/dotnet/roslyn/issues/46397")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnImplicitObjectCreationExpressionInitializer(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
    public int Alice;
    public int Bob;

    void M(int value)
    {
        C c = new() $$
    }
}
                              </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp9)

                state.SendTypeChars("{ ")
                Await state.AssertSelectedCompletionItem(displayText:="Alice", isHardSelected:=False)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new() { Alice", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars(" = va")
                Await state.AssertSelectedCompletionItem(displayText:="value", isHardSelected:=True)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new() { Alice = value", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(44921, "https://github.com/dotnet/roslyn/issues/44921")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnWithExpressionInitializer(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
record Base(int Alice, int Bob)
{
    void M(int value)
    {
        _ = this with $$
    }
}
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp9)

                state.SendTypeChars("{ ")
                Await state.AssertSelectedCompletionItem(displayText:="Alice", isHardSelected:=False)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("with { Alice", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars(" = va")
                Await state.AssertSelectedCompletionItem(displayText:="value", isHardSelected:=True)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("with { Alice = value", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(44921, "https://github.com/dotnet/roslyn/issues/44921")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnWithExpressionInitializer_AfterComma(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
record Base(int Alice, int Bob)
{
    void M(int value)
    {
        _ = this with { Alice = value$$
    }
}
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp9)

                state.SendTypeChars(", ")
                Await state.AssertSelectedCompletionItem(displayText:="Bob", isHardSelected:=False)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                state.SendTypeChars(" = va")
                Await state.AssertSelectedCompletionItem(displayText:="value", isHardSelected:=True)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("with { Alice = value, Bob = value", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(47430, "https://github.com/dotnet/roslyn/issues/47430")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnWithExpressionForTypeParameter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
public abstract record MyRecord
{
    public string Name { get; init; }
}

public static class Test
{
    public static TRecord WithNameSuffix&lt;TRecord&gt;(this TRecord record, string nameSuffix)
        where TRecord : MyRecord
        => record with
        {
            $$
        };
}
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp9)

                state.SendTypeChars("N")
                Await state.AssertSelectedCompletionItem(displayText:="Name", isHardSelected:=True)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("Name", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnWithExpressionInitializer_AnonymousType(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void M()
    {
        var a = new { Property = 1 };
        _ = a $$
    }
}
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars("w")
                Await state.AssertSelectedCompletionItem(displayText:="with", isHardSelected:=True)
                state.SendTab()
                state.SendTypeChars(" { ")
                Await state.AssertSelectedCompletionItem(displayText:="Property", isHardSelected:=False)
                state.SendTypeChars("P")
                Await state.AssertSelectedCompletionItem(displayText:="Property", isHardSelected:=True)
                state.SendTypeChars(" = 2")
                Await state.AssertNoCompletionSession()
                Assert.Contains("with { Property = 2", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(44921, "https://github.com/dotnet/roslyn/issues/44921")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionOnObjectCreation(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    int Alice { get; set; }
    void M()
    {
        _ = new C() $$
    }
}
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("{ ")
                Await state.AssertSelectedCompletionItem(displayText:="Alice", isHardSelected:=False)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new C() { Alice", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(541201, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541201")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TabCommitsWithoutAUniqueMatch(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  $$
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("using System.Ne")
                Await state.AssertSelectedCompletionItem(displayText:="Net", isHardSelected:=True)
                state.SendTypeChars("x")
                Await state.AssertSelectedCompletionItem(displayText:="Net", isSoftSelected:=True)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("using System.Net", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(35236, "https://github.com/dotnet/roslyn/issues/35236")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBetweenTwoDotsInNamespaceName(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
namespace N.O.P
{
}

namespace N$$.P
{
}
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="O", isHardSelected:=False)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAtEndOfFile(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                                <Document>$$</Document>,
                                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("usi")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("using", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(44459, "https://github.com/dotnet/roslyn/issues/44459")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSelectUsingOverUshort(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
$$
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                ' 'us' should select 'using' instead of 'ushort' (even though 'ushort' sorts higher in the list textually).
                state.SendTypeChars("us")
                Await state.AssertSelectedCompletionItem(displayText:="using", isHardSelected:=True)
                Await state.AssertCompletionItemsContain("ushort", "")

                ' even after 'ushort' is selected, deleting the 'h' should still take us back to 'using'.
                state.SendTypeChars("h")
                Await state.AssertSelectedCompletionItem(displayText:="ushort", isHardSelected:=True)
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="using", isHardSelected:=True)
            End Using
        End Function

        <WorkItem(44459, "https://github.com/dotnet/roslyn/issues/44459")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSelectUshortOverUsingOnceInMRU(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
$$
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("ush")
                Await state.AssertCompletionItemsContain("ushort", "")
                state.SendTab()
                Assert.Contains("ushort", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)

                state.SendDeleteWordToLeft()

                ' 'ushort' should be in the MRU now. so typing 'us' should select it instead of 'using'.
                state.SendTypeChars("us")
                Await state.AssertSelectedCompletionItem(displayText:="ushort", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDeletingWholeWordResetCompletionToTheDefaultItem(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  using System;

class C
{
    void M()
    {
        var replyUri = new Uri("");
        $$
    }
}

                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendTypeChars("repl")
                state.SendTab()
                For i = 1 To 7
                    state.SendBackspace()
                    Await state.WaitForAsynchronousOperationsAsync()
                Next

                Await state.AssertCompletionSession()

                state.SendBackspace()
                Await state.AssertSelectedCompletionItem("AccessViolationException")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestTabsDoNotTriggerCompletion(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  using System;

class C
{
    void M()
    {
        var replyUri = new Uri("");
        replyUri$$
    }
}

                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTab()
                state.SendTab()
                Assert.Equal("        replyUri" & vbTab & vbTab, state.GetLineTextFromCaretPosition())
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEnterDoesNotTriggerCompletion(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;

class C
{
    void M()
    {
        String.Equals("foo", "bar", $$StringComparison.CurrentCulture)
    }
}

                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendReturn()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotAtStartOfExistingWord(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>$$using</Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("u")
                Await state.AssertNoCompletionSession()
                Assert.Contains("using", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMSCorLibTypes(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;

class c : $$
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("A")
                Await state.AssertCompletionItemsContainAll("Attribute", "Exception", "IDisposable")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFiltering1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;

class c { $$
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("Sy")
                Await state.AssertCompletionItemsContainAll("OperatingSystem", "System", "SystemException")
                Await state.AssertCompletionItemsDoNotContainAny("Exception", "Activator")
            End Using
        End Function

        ' NOTE(cyrusn): This should just be a unit test for SymbolCompletionProvider.  However, I'm
        ' just porting the integration tests to here for now.
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMultipleTypes(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C { $$ } struct S { } enum E { } interface I { } delegate void D();
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("C")
                Await state.AssertCompletionItemsContainAll("C", "S", "E", "I", "D")
            End Using
        End Function

        ' NOTE(cyrusn): This should just be a unit test for KeywordCompletionProvider.  However, I'm
        ' just porting the integration tests to here for now.
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInEmptyFile(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
$$
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("abstract", "class", "namespace")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotAfterTypingDotAfterIntegerLiteral(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class c { void M() { 3$$ } }
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterExplicitInvokeAfterDotAfterIntegerLiteral(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class c { void M() { 3.$$ } }
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("ToString")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion), Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)>
        Public Async Function TestTypingDotBeforeExistingDot(showCompletionInArgumentLists As Boolean) As Task
            ' Starting C# 8.0 two dots are considered as a DotDotToken of a Range expression.
            ' However, typing dot before a single dot (and adding the second one) should lead to a completion
            ' in the context of the previous token if this completion exists.
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class c { void M() { this$$.ToString() } }
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertCompletionItemsContainAll("ToString")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypingDotAfterExistingDot(showCompletionInArgumentLists As Boolean) As Task
            ' Starting C# 8.0 two dots are considered as a DotDotToken of a Range expression.
            ' A test above (TestTypingDotBeforeExistingDot) verifies that the completion happens
            ' if we type dot before a single dot.
            ' However, we should not have a completion if typing dot after a dot.
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class c { void M() { this.$$ToString() } }
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion), Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)>
        Public Async Function TestInvokingCompletionBetweenTwoDots(showCompletionInArgumentLists As Boolean) As Task
            ' Starting C# 8.0 two dots are considered as a DotDotToken of a Range expression.
            ' However, we may want to have a completion when invoking it aqfter the first dot.
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class c { void M() { this.$$.ToString() } }
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("ToString")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestEnterIsConsumed(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document>
class Class1
{
    void Main(string[] args)
    {
        $$
    }
}</Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("System.TimeSpan.FromMin")
                state.SendReturn()
                Assert.Equal(<text>
class Class1
{
    void Main(string[] args)
    {
        System.TimeSpan.FromMinutes
    }
}</text>.NormalizedValue, state.GetDocumentText())
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestEnterIsConsumedWithAfterFullyTypedWordOption_NotFullyTyped(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document>
class Class1
{
    void Main(string[] args)
    {
        $$
    }
}</Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.EnterKeyBehavior, LanguageNames.CSharp), EnterKeyRule.AfterFullyTypedWord)

                state.SendTypeChars("System.TimeSpan.FromMin")
                state.SendReturn()
                Assert.Equal(<text>
class Class1
{
    void Main(string[] args)
    {
        System.TimeSpan.FromMinutes
    }
}</text>.NormalizedValue, state.GetDocumentText())
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestEnterIsConsumedWithAfterFullyTypedWordOption_FullyTyped(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document>
class Class1
{
    void Main(string[] args)
    {
        $$
    }
}</Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.EnterKeyBehavior, LanguageNames.CSharp), EnterKeyRule.AfterFullyTypedWord)

                state.SendTypeChars("System.TimeSpan.FromMinutes")
                state.SendReturn()
                Assert.Equal(<text>
class Class1
{
    void Main(string[] args)
    {
        System.TimeSpan.FromMinutes

    }
}</text>.NormalizedValue, state.GetDocumentText())
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDescription1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
using System;

/// <summary>
/// TestDocComment
/// </summary>
class TestException : Exception { }

class MyException : $$]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("Test")
                Await state.AssertSelectedCompletionItem(description:="class TestException" & vbCrLf & "TestDocComment")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestObjectCreationPreselection1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
using System.Collections.Generic;

class C
{
    public void Goo()
    {
        List<int> list = new$$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="List<int>", isHardSelected:=True)
                Await state.AssertCompletionItemsContainAll("LinkedList", "List", "System")
                state.SendTypeChars("Li")
                Await state.AssertSelectedCompletionItem(displayText:="List<int>", isHardSelected:=True)
                Await state.AssertCompletionItemsContainAll("LinkedList", "List")
                Await state.AssertCompletionItemsDoNotContainAny("System")
                state.SendTypeChars("n")
                Await state.AssertSelectedCompletionItem(displayText:="LinkedList", displayTextSuffix:="<>", isHardSelected:=True)
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="List<int>", isHardSelected:=True)
                state.SendTab()
                Assert.Contains("new List<int>", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDeconstructionDeclaration(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void Goo()
    {
       var ($$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("i")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDeconstructionDeclaration2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void Goo()
    {
       var (a, $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("i")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDeconstructionDeclaration3(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void Goo()
    {
       var ($$) = (1, 2);
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("i")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParenthesizedDeconstructionDeclarationWithVar(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       (var a$$) = (1, 2);
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="as", isHardSelected:=False)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParenthesizedDeconstructionDeclarationWithVarAfterComma(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       (var a, var a$$) = (1, 2);
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="as", isHardSelected:=False)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParenthesizedVarDeconstructionDeclarationWithVar(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       (var a, var ($$)) = (1, 2);
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("a")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars(", a")
                Await state.AssertNoCompletionSession()
                Assert.Contains("(var a, var (a, a)) = ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestVarDeconstructionDeclarationWithVar(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
        $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("va")
                Await state.AssertSelectedCompletionItem(displayText:="var", isHardSelected:=True)

                state.SendTypeChars(" (a")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars(", a")
                Await state.AssertNoCompletionSession()
                Assert.Contains("var (a, a", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParenthesizedDeconstructionDeclarationWithSymbol(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       ($$) = (1, 2);
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("vari")
                Await state.AssertSelectedCompletionItem(displayText:="Variable", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("(Variable ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("x, vari")
                Await state.AssertSelectedCompletionItem(displayText:="Variable", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("(Variable x, Variable ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertSelectedCompletionItem(displayText:="Variable", isHardSelected:=False)
                Await state.AssertCompletionItemsContainAll("variable")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParenthesizedDeconstructionDeclarationWithInt(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Integer
{
    public void Goo()
    {
       ($$) = (1, 2);
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("int")
                Await state.AssertSelectedCompletionItem(displayText:="int", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("(int ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("x, int")
                Await state.AssertSelectedCompletionItem(displayText:="int", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("(int x, int ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestIncompleteParenthesizedDeconstructionDeclaration(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       ($$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("va")
                Await state.AssertSelectedCompletionItem(displayText:="var", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("a")
                Await state.AssertSelectedCompletionItem(displayText:="as", isSoftSelected:=True)

                state.SendTypeChars(", va")
                Await state.AssertSelectedCompletionItem(displayText:="var", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("a")
                Await state.AssertSelectedCompletionItem(displayText:="as", isSoftSelected:=True)
                state.SendTypeChars(")")
                Assert.Contains("(var a, var a)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestIncompleteParenthesizedDeconstructionDeclaration2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       ($$)
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("va")
                Await state.AssertSelectedCompletionItem(displayText:="var", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("a")
                Await state.AssertSelectedCompletionItem(displayText:="as", isSoftSelected:=True)

                state.SendTypeChars(", va")
                Await state.AssertSelectedCompletionItem(displayText:="var", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("a")
                Await state.AssertSelectedCompletionItem(displayText:="as", isSoftSelected:=True)
                state.SendReturn()

                Dim caretLine = state.GetLineFromCurrentCaretPosition()
                Assert.Contains("            )", caretLine.GetText(), StringComparison.Ordinal)

                Dim previousLine = caretLine.Snapshot.Lines(caretLine.LineNumber - 1)
                Assert.Contains("(var a, var a", previousLine.GetText(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackspaceInIncompleteParenthesizedDeconstructionDeclaration(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       (var as$$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="as", isSoftSelected:=True)

                state.SendTypeChars(", var as")
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="as", isSoftSelected:=True)

                state.SendTypeChars(")")
                Await state.AssertNoCompletionSession()
                Assert.Contains("(var a, var a)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackspaceInParenthesizedDeconstructionDeclaration(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       (var as$$)
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="as", isSoftSelected:=True)

                state.SendTypeChars(", var as")
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="as", isSoftSelected:=True)

                state.SendReturn()
                Await state.AssertNoCompletionSession()

                Dim caretLine = state.GetLineFromCurrentCaretPosition()
                Assert.Contains("            )", caretLine.GetText(), StringComparison.Ordinal)

                Dim previousLine = caretLine.Snapshot.Lines(caretLine.LineNumber - 1)
                Assert.Contains("(var a, var a", previousLine.GetText(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(17256, "https://github.com/dotnet/roslyn/issues/17256")>
        Public Async Function TestThrowExpression(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
using System;
class C
{
    public object Goo()
    {
        return null ?? throw new$$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="Exception", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(17256, "https://github.com/dotnet/roslyn/issues/17256")>
        Public Async Function TestThrowStatement(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
using System;
class C
{
    public object Goo()
    {
        throw new$$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="Exception", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNonTrailingNamedArgumentInCSharp7_1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                 <Workspace>
                     <Project Language="C#" LanguageVersion="7.1" CommonReferences="true" AssemblyName="CSProj">
                         <Document FilePath="C.cs">
class C
{
    public void M()
    {
        int better = 2;
        M(a: 1, $$)
    }
    public void M(int a, int bar, int c) { }
}
                         </Document>
                     </Project>
                 </Workspace>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("b")
                Await state.AssertSelectedCompletionItem(displayText:="bar", displayTextSuffix:=":", isHardSelected:=True)
                state.SendTypeChars("e")
                Await state.AssertSelectedCompletionItem(displayText:="bar", displayTextSuffix:=":", isSoftSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNonTrailingNamedArgumentInCSharp7_2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                 <Workspace>
                     <Project Language="C#" LanguageVersion="7.2" CommonReferences="true" AssemblyName="CSProj">
                         <Document FilePath="C.cs">
class C
{
    public void M()
    {
        int better = 2;
        M(a: 1, $$)
    }
    public void M(int a, int bar, int c) { }
}
                         </Document>
                     </Project>
                 </Workspace>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("b")
                Await state.AssertSelectedCompletionItem(displayText:="better", isHardSelected:=True)
                state.SendTypeChars("a")
                Await state.AssertSelectedCompletionItem(displayText:="bar", displayTextSuffix:=":", isHardSelected:=True)
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="better", isHardSelected:=True)
                state.SendTypeChars(", ")
                Assert.Contains("M(a: 1, better,", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(4677, "https://github.com/dotnet/roslyn/issues/4677")>
        Public Async Function TestDefaultSwitchLabel(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void M(object o)
    {
        switch (o)
        {
            default:
                goto $$
        }
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("d")
                Await state.AssertSelectedCompletionItem(displayText:="default", isHardSelected:=True)
                state.SendTypeChars(";")
                Assert.Contains("goto default;", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(4677, "https://github.com/dotnet/roslyn/issues/4677")>
        Public Async Function TestGotoOrdinaryLabel(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void M(object o)
    {
label1:
        goto $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("l")
                Await state.AssertSelectedCompletionItem(displayText:="label1", isHardSelected:=True)
                state.SendTypeChars(";")
                Assert.Contains("goto label1;", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(4677, "https://github.com/dotnet/roslyn/issues/4677")>
        Public Async Function TestEscapedDefaultLabel(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void M(object o)
    {
@default:
        goto $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("d")
                Await state.AssertSelectedCompletionItem(displayText:="@default", isHardSelected:=True)
                state.SendTypeChars(";")
                Assert.Contains("goto @default;", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(4677, "https://github.com/dotnet/roslyn/issues/4677")>
        Public Async Function TestEscapedDefaultLabel2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void M(object o)
    {
        switch (o)
        {
            default:
@default:
                goto $$
        }
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("d")
                Await state.AssertSelectedCompletionItem(displayText:="default", isHardSelected:=True)
                state.SendTypeChars(";")
                Assert.Contains("goto default;", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(4677, "https://github.com/dotnet/roslyn/issues/4677")>
        Public Async Function TestEscapedDefaultLabelWithoutSwitch(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void M(object o)
    {
@default:
        goto $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("d")
                Await state.AssertSelectedCompletionItem(displayText:="@default", isHardSelected:=True)
                state.SendTypeChars(";")
                Assert.Contains("goto @default;", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestArrayInitialization(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        Class[] x = $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("new ")
                Await state.AssertSelectedCompletionItem(displayText:="Class", isSoftSelected:=True)
                state.SendTypeChars("C")
                Await state.AssertSelectedCompletionItem(displayText:="Class", isHardSelected:=True)
                state.SendTypeChars("[")
                Assert.Contains("Class[] x = new Class[", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars("] {")
                Assert.Contains("Class[] x = new Class[] {", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestImplicitArrayInitialization(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        Class[] x = $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("n")
                Await state.AssertSelectedCompletionItem(displayText:="nameof", isHardSelected:=True)
                state.SendTypeChars("e")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="Class", isSoftSelected:=True)
                state.SendTypeChars("[")
                Assert.Contains("Class[] x = new [", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars("] {")
                Assert.Contains("Class[] x = new [] {", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestImplicitArrayInitialization2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        Class[] x = $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
                state.SendTypeChars("[")
                Assert.Contains("Class[] x = new[", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestImplicitArrayInitialization3(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        Class[] x = $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="Class", isSoftSelected:=True)
                Assert.Contains("Class[] x = new ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars("[")
                Assert.Contains("Class[] x = new [", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestImplicitArrayInitialization4(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        Class[] x =$$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
                state.SendTypeChars("{")
                Assert.Contains("Class[] x = {", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestImplicitArrayInitialization_WithTab(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        Class[] x = $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="Class", isSoftSelected:=True)
                Assert.Contains("Class[] x = new ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTab()
                Assert.Contains("Class[] x = new Class", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestTypelessImplicitArrayInitialization(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        var x = $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
                state.SendTypeChars("[")
                Assert.Contains("var x = new [", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars("] {")
                Assert.Contains("var x = new [] {", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestTypelessImplicitArrayInitialization2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        var x = $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
                state.SendTypeChars("[")
                Assert.Contains("var x = new[", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestTypelessImplicitArrayInitialization3(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        var x = $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("var x = new ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars("[")
                Assert.Contains("var x = new [", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPropertyInPropertySubpattern(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Class
{
    int Prop { get; set; }
    int OtherProp { get; set; }
    public void M()
    {
        _ = this is $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Await state.AssertNoCompletionSession()
                state.SendTypeChars("C")
                Await state.AssertSelectedCompletionItem(displayText:="Class", isHardSelected:=True)
                state.SendTypeChars(" { P")
                Await state.AssertSelectedCompletionItem(displayText:="Prop", displayTextSuffix:="", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("{ Prop:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars(" 0, ")
                Await state.AssertSelectedCompletionItem(displayText:="OtherProp", displayTextSuffix:="", isSoftSelected:=True)
                state.SendTypeChars("O")
                Await state.AssertSelectedCompletionItem(displayText:="OtherProp", displayTextSuffix:="", isHardSelected:=True)
                state.SendTypeChars(": 1 }")
                Assert.Contains("is Class { Prop: 0, OtherProp: 1 }", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPropertyInPropertySubpattern_TriggerWithSpace(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class Class
{
    int Prop { get; set; }
    int OtherProp { get; set; }
    public void M()
    {
        _ = this is $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Await state.AssertNoCompletionSession()
                state.SendTypeChars("C")
                Await state.AssertSelectedCompletionItem(displayText:="Class", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("is Class", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars("{ P")
                Await state.AssertSelectedCompletionItem(displayText:="Prop", displayTextSuffix:="", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("is Class { Prop ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars(":")
                Assert.Contains("is Class { Prop :", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars(" 0, ")
                Await state.AssertSelectedCompletionItem(displayText:="OtherProp", displayTextSuffix:="", isSoftSelected:=True)
                state.SendTypeChars("O")
                Await state.AssertSelectedCompletionItem(displayText:="OtherProp", displayTextSuffix:="", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("is Class { Prop : 0, OtherProp", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars(": 1 }")
                Assert.Contains("is Class { Prop : 0, OtherProp : 1 }", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestSymbolInTupleLiteral(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void Fo()
    {
        ($$)
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("F")
                Await state.AssertSelectedCompletionItem(displayText:="Fo", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(F:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestSymbolInTupleLiteralAfterComma(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void Fo()
    {
        (x, $$)
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("F")
                Await state.AssertSelectedCompletionItem(displayText:="Fo", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(x, F:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(19335, "https://github.com/dotnet/roslyn/issues/19335")>
        Public Async Function ColonInTupleNameInTupleLiteral(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void M()
    {
        (int first, int second) t = ($$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("fi")
                Await state.AssertSelectedCompletionItem(displayText:="first", displayTextSuffix:=":", isHardSelected:=True)
                Assert.Equal("first", state.GetSelectedItem().FilterText)
                state.SendTypeChars(":")
                Assert.Contains("(first:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(19335, "https://github.com/dotnet/roslyn/issues/19335")>
        Public Async Function ColonInExactTupleNameInTupleLiteral(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void M()
    {
        (int first, int second) t = ($$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("first")
                Await state.AssertSelectedCompletionItem(displayText:="first", displayTextSuffix:=":", isHardSelected:=True)
                Assert.Equal("first", state.GetSelectedItem().FilterText)
                state.SendTypeChars(":")
                Assert.Contains("(first:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(19335, "https://github.com/dotnet/roslyn/issues/19335")>
        Public Async Function ColonInTupleNameInTupleLiteralAfterComma(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void M()
    {
        (int first, int second) t = (0, $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("se")
                Await state.AssertSelectedCompletionItem(displayText:="second", displayTextSuffix:=":", isHardSelected:=True)
                Assert.Equal("second", state.GetSelectedItem().FilterText)
                state.SendTypeChars(":")
                Assert.Contains("(0, second:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(19335, "https://github.com/dotnet/roslyn/issues/19335")>
        Public Async Function TabInTupleNameInTupleLiteral(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void M()
    {
        (int first, int second) t = ($$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("fi")
                Await state.AssertSelectedCompletionItem(displayText:="first", displayTextSuffix:=":", isHardSelected:=True)
                Assert.Equal("first", state.GetSelectedItem().FilterText)
                state.SendTab()
                state.SendTypeChars(":")
                state.SendTypeChars("0")
                Assert.Contains("(first:0", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(19335, "https://github.com/dotnet/roslyn/issues/19335")>
        Public Async Function TabInExactTupleNameInTupleLiteral(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void M()
    {
        (int first, int second) t = ($$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("first")
                Await state.AssertSelectedCompletionItem(displayText:="first", displayTextSuffix:=":", isHardSelected:=True)
                Assert.Equal("first", state.GetSelectedItem().FilterText)
                state.SendTab()
                state.SendTypeChars(":")
                state.SendTypeChars("0")
                Assert.Contains("(first:0", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(19335, "https://github.com/dotnet/roslyn/issues/19335")>
        Public Async Function TabInTupleNameInTupleLiteralAfterComma(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void M()
    {
        (int first, int second) t = (0, $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("se")
                Await state.AssertSelectedCompletionItem(displayText:="second", displayTextSuffix:=":", isHardSelected:=True)
                Assert.Equal("second", state.GetSelectedItem().FilterText)
                state.SendTab()
                state.SendTypeChars(":")
                state.SendTypeChars("1")
                Assert.Contains("(0, second:1", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestKeywordInTupleLiteral(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void Goo()
    {
        ($$)
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("d")
                Await state.AssertSelectedCompletionItem(displayText:="decimal", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(d:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestTupleType(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void Goo()
    {
        ($$)
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("d")
                Await state.AssertSelectedCompletionItem(displayText:="decimal", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("(decimal ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestDefaultKeyword(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void Goo()
    {
        switch(true)
        {
            $$
        }
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("def")
                Await state.AssertSelectedCompletionItem(displayText:="default", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("default:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestInvocationExpression(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void Goo(int Alice)
    {
        Goo($$)
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("A")
                Await state.AssertSelectedCompletionItem(displayText:="Alice", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("Goo(Alice:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestImplicitObjectCreationExpression(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
public class C
{
    public C(int Alice, int Bob) { }
    public C(string ignored) { }

    public void M()
    {
        C c = new($$
    }
}]]></Document>, languageVersion:=LanguageVersion.CSharp9, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("A")
                Await state.AssertSelectedCompletionItem(displayText:="Alice:", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("new(Alice:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestImplicitObjectCreationExpression_WithSpace(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
public class C
{
    public C(int Alice, int Bob) { }
    public C(string ignored) { }

    public void M()
    {
        C c = new$$
    }
}]]></Document>, languageVersion:=LanguageVersion.CSharp9, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="C", isHardSelected:=True)
                state.SendTypeChars("(")
                If showCompletionInArgumentLists Then
                    Await state.AssertSignatureHelpSession()
                Else
                    Await state.AssertNoCompletionSession()
                End If

                state.SendTypeChars("A")
                Await state.AssertSelectedCompletionItem(displayText:="Alice:", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("new C(Alice:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestInvocationExpressionAfterComma(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void Goo(int Alice, int Bob)
    {
        Goo(1, $$)
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("B")
                Await state.AssertSelectedCompletionItem(displayText:="Bob", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("Goo(1, Bob:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestCaseLabel(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public void Fo()
    {
        switch (1)
        {
            case $$
        }
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("F")
                Await state.AssertSelectedCompletionItem(displayText:="Fo", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("case Fo:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(543268, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543268")>
        Public Async Function TestTypePreselection1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
partial class C
{
}
partial class C
{
    $$
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("C")
                Await state.AssertSelectedCompletionItem(displayText:="C", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WorkItem(543519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543519")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNewPreselectionAfterVar(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    void M()
    {
        var c = $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("new ")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(543559, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543559")>
        <WorkItem(543561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543561")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEscapedIdentifiers(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class @return
{
    void goo()
    {
        $$
    }
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("@")
                Await state.AssertNoCompletionSession()
                state.SendTypeChars("r")
                Await state.AssertSelectedCompletionItem(displayText:="@return", isHardSelected:=True)
                state.SendTab()
                Assert.Contains("@return", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(543771, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543771")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitUniqueItem1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteL$$();
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("WriteLine()", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(543771, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543771")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitUniqueItem2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteL$$ine();
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForUsingDirective1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  $$
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("using Sys")
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars("(")
                Await state.AssertNoCompletionSession()
                Assert.Contains("using Sys(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForUsingDirective2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  $$
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("using Sys")
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
                Assert.Contains("using System.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForUsingDirective3(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  $$
                              </Document>,
                              extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("using Sys")
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars(";")
                Await state.AssertNoCompletionSession()
                state.AssertMatchesTextStartingAtLine(1, "using System;")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForUsingDirective4(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                            <Document>
                                $$
                            </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("using Sys")
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
                Assert.Contains("using Sys ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function KeywordsIncludedInObjectCreationCompletion(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo()
    {
        string s = new$$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="string", isHardSelected:=True)
                Await state.AssertCompletionItemsContainAll("int")
            End Using
        End Function

        <WorkItem(544293, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544293")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoKeywordsOrSymbolsAfterNamedParameterWithCSharp7(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                                <Document>
class Goo
{
    void Test()
    {
        object m = null;
        Method(obj:m, $$
    }

    void Method(object obj, int num = 23, string str = "")
    {
    }
}
                              </Document>, languageVersion:=LanguageVersion.CSharp7, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("a")
                Await state.AssertCompletionItemsDoNotContainAny("System", "int")
                Await state.AssertCompletionItemsContain("num", ":")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function KeywordsOrSymbolsAfterNamedParameter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                                <Document>
class Goo
{
    void Test()
    {
        object m = null;
        Method(obj:m, $$
    }

    void Method(object obj, int num = 23, string str = "")
    {
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("a")
                Await state.AssertCompletionItemsContainAll("System", "int")
                Await state.AssertCompletionItemsContain("num", ":")
            End Using
        End Function

        <WorkItem(544017, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544017")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionTriggeredOnSpace(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
enum Numeros { Uno, Dos }
class Goo
{
    void Bar(int a, Numeros n) { }
    void Baz()
    {
        Bar(0$$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(", ")
                Await state.AssertSelectedCompletionItem(displayText:="Numeros", isHardSelected:=True)
                Assert.Equal(1, state.GetCompletionItems().Where(Function(c) c.DisplayText = "Numeros").Count())
            End Using
        End Function

        <WorkItem(479078, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/479078")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionTriggeredOnSpaceForNullables(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
enum Numeros { Uno, Dos }
class Goo
{
    void Bar(int a, Numeros? n) { }
    void Baz()
    {
        Bar(0$$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(", ")
                Await state.AssertSelectedCompletionItem(displayText:="Numeros", isHardSelected:=True)
                Assert.Equal(1, state.GetCompletionItems().Where(Function(c) c.DisplayText = "Numeros").Count())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EnumCompletionTriggeredOnDot(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
enum Numeros { Uno, Dos }
class Goo
{
    void Bar()
    {
        Numeros num = $$
    }
}
                </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("Nu.")
                Assert.Contains("Numeros num = Numeros.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionNotTriggeredOnPlusCommitCharacter(showCompletionInArgumentLists As Boolean) As Task
            Await EnumCompletionNotTriggeredOn("+"c, showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionNotTriggeredOnLeftBraceCommitCharacter(showCompletionInArgumentLists As Boolean) As Task
            Await EnumCompletionNotTriggeredOn("{"c, showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionNotTriggeredOnSpaceCommitCharacter(showCompletionInArgumentLists As Boolean) As Task
            Await EnumCompletionNotTriggeredOn(" "c, showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionNotTriggeredOnSemicolonCommitCharacter(showCompletionInArgumentLists As Boolean) As Task
            Await EnumCompletionNotTriggeredOn(";"c, showCompletionInArgumentLists)
        End Function

        Private Shared Async Function EnumCompletionNotTriggeredOn(c As Char, showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
enum Numeros { Uno, Dos }
class Goo
{
    void Bar()
    {
        Numeros num = $$
    }
}
                </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("Nu")
                Await state.AssertSelectedCompletionItem(displayText:="Numeros", isHardSelected:=True)
                state.SendTypeChars(c.ToString())
                Await state.AssertSessionIsNothingOrNoCompletionItemLike("Numberos")
                Assert.Contains(String.Format("Numeros num = Nu{0}", c), state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(49632, "https://github.com/dotnet/roslyn/pull/49632")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionEnumTypeAndValues() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
namespace A
{
    public enum Colors
    {
        Red,
        Green
    }
}
namespace B
{
    class Program
    {
        static void Main()
        {
            var color = A.Colors.Red;
            switch (color)
            {
                case $$
        }
    }
}                              </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain(Function(i) i.DisplayText = "A.Colors" AndAlso i.FilterText = "Colors")
                Await state.AssertCompletionItemsContain(Function(i) i.DisplayText = "A.Colors.Green" AndAlso i.FilterText = "A.Colors.Green")
                Await state.AssertCompletionItemsContain(Function(i) i.DisplayText = "A.Colors.Red" AndAlso i.FilterText = "A.Colors.Red")
                Await state.AssertSelectedCompletionItem("A.Colors", isHardSelected:=True)
            End Using
        End Function

        <WorkItem(49632, "https://github.com/dotnet/roslyn/pull/49632")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionEnumTypeSelectionSequenceTest() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
public enum Colors
{
    Red,
    Green
}

class Program
{
    void M(Colors color) { }

    static void Main()
    {
        M$$
    }
}                             </Document>)
                state.SendTypeChars("(")
                Await state.AssertCompletionSession
                Await state.AssertCompletionItemsContain("Colors", "")
                Await state.AssertCompletionItemsContain("Colors.Green", "")
                Await state.AssertCompletionItemsContain("Colors.Red", "")
                Await state.AssertSelectedCompletionItem("Colors", isHardSelected:=True)

                state.SendDownKey() 'Select "Colors.Red"
                state.SendTab() ' Insert "Colors.Red"
                state.SendUndo() 'Undo insert
                state.SendInvokeCompletionList()

                Await state.AssertSelectedCompletionItem("Colors", isHardSelected:=True)
            End Using
        End Function

        <WorkItem(49632, "https://github.com/dotnet/roslyn/pull/49632")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionEnumTypeAndValuesWithAlias() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using AT = System.AttributeTargets;

public class Program
{
    static void M(AT attributeTargets) { }
    
    public static void Main()
    {
        M($$
    }
}                              </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain(Function(i) i.DisplayText = "AT" AndAlso i.SortText = "AT" AndAlso i.FilterText = "AT")
                Await state.AssertCompletionItemsContain(Function(i) i.DisplayText = "AT.All" AndAlso i.FilterText = "AT.All")
                Await state.AssertSelectedCompletionItem("AT", isHardSelected:=True)
            End Using
        End Function

        <WorkItem(544296, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544296")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestVerbatimNamedIdentifierFiltering(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class Program
{
    void Goo(int @int)
    {
        Goo($$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("i")
                Await state.AssertCompletionSession()
                Await state.AssertCompletionItemsContain("@int", ":")
                state.SendTypeChars("n")
                Await state.AssertCompletionItemsContain("@int", ":")
                state.SendTypeChars("t")
                Await state.AssertCompletionItemsContain("@int", ":")
            End Using
        End Function

        <WorkItem(543687, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543687")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoPreselectInInvalidObjectCreationLocation(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
using System;

class Program
{
    void Test()
    {
        $$
    }
}

class Bar { }

class Goo<T> : IGoo<T>
{
}

interface IGoo<T>
{
}]]>
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("IGoo<Bar> a = new ")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(544925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544925")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestQualifiedEnumSelection(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;

class Program
{
    void Main()
    {
        Environment.GetFolderPath$$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(")
                state.SendTab()
                Assert.Contains("Environment.SpecialFolder", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <WorkItem(545070, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545070")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTextChangeSpanWithAtCharacter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
public class @event
{
    $$@event()
    {
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("public ")
                Await state.AssertNoCompletionSession()
                Assert.Contains("public @event", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDoNotInsertColonSoThatUserCanCompleteOutAVariableNameThatDoesNotCurrentlyExist_IE_TheCyrusCase(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        Goo($$)
    }

    void Goo(CancellationToken cancellationToken)
    {
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("can")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("Goo(cancellationToken)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

#If False Then
    <Scenario Name="Verify correct intellisense selection on ENTER">
        <SetEditorText>
            <![CDATA[class Class1
{
    void Main(string[] args)
    {
        //
    }
}]]>
        </SetEditorText>
        <PlaceCursor Marker="//"/>
        <SendKeys>var a = System.TimeSpan.FromMin{ENTER}{(}</SendKeys>
        <VerifyEditorContainsText>
            <![CDATA[class Class1
{
    void Main(string[] args)
    {
        var a = System.TimeSpan.FromMinutes(
    }
}]]>
        </VerifyEditorContainsText>
    </Scenario>
#End If

        <WorkItem(544940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeNamedPropertyCompletionCommitWithTab(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                            <Document>
class MyAttribute : System.Attribute
{
    public string Name { get; set; }
}

[MyAttribute($$
public class Goo
{
}
                            </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("Nam")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("[MyAttribute(Name =", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(544940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function LocalFunctionAttributeNamedPropertyCompletionCommitWithTab(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                            <Document>
class MyAttribute : System.Attribute
{
    public string Name { get; set; }
}

public class Goo
{
    void M()
    {
        [MyAttribute($$
        void local1() { }
    }
}
                            </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("Nam")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("        [MyAttribute(Name =", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(544940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeOnLocalFunctionCompletionCommitWithTab(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                            <Document>
class MyGoodAttribute : System.Attribute
{
    public string Name { get; set; }
}

public class Goo
{
    void M()
    {
        [$$
        void local1()
        {
        }
    }
}
                            </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("MyG")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("        [MyGood", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(544940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeOnMissingStatementCompletionCommitWithTab(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                            <Document>
class MyGoodAttribute : System.Attribute
{
    public string Name { get; set; }
}

public class Goo
{
    void M()
    {
        [$$
    }
}
                            </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("MyG")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("        [MyGood", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(544940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TypeAfterAttributeListOnStatement(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                            <Document>
class MyGoodAttribute : System.Attribute
{
    public string Name { get; set; }
}

public class Goo
{
    void M()
    {
        [MyGood] $$
    }
}
                            </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("Go")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("        [MyGood] Goo", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(544940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeNamedPropertyCompletionCommitWithEquals(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                            <Document>
class MyAttribute : System.Attribute
{
    public string Name { get; set; }
}

[MyAttribute($$
public class Goo
{
}
                            </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("Nam=")
                Await state.AssertNoCompletionSession()
                Assert.Equal("[MyAttribute(Name =", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(544940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeNamedPropertyCompletionCommitWithSpace(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                            <Document>
class MyAttribute : System.Attribute
{
    public string Name { get; set; }
}

[MyAttribute($$
public class Goo
{
}
                            </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("Nam ")
                Await state.AssertNoCompletionSession()
                Assert.Equal("[MyAttribute(Name ", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(545590, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545590")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOverrideDefaultParameter_CSharp7(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    public virtual void Goo<S>(S x = default(S))
    {
    }
}

class D : C
{
    override $$
}
            ]]></Document>,
                   languageVersion:=LanguageVersion.CSharp7, showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars(" Goo")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("public override void Goo<S>(S x = default(S))", state.SubjectBuffer.CurrentSnapshot.GetText(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOverrideDefaultParameter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    public virtual void Goo<S>(S x = default(S))
    {
    }
}

class D : C
{
    override $$
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars(" Goo")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("public override void Goo<S>(S x = default)", state.SubjectBuffer.CurrentSnapshot.GetText(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(545664, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545664")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestArrayAfterOptionalParameter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class A
{
    public virtual void Goo(int x = 0, int[] y = null) { }
}

class B : A
{
public override void Goo(int x = 0, params int[] y) { }
}

class C : B
{
    override$$
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars(" Goo")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("    public override void Goo(int x = 0, int[] y = null)", state.SubjectBuffer.CurrentSnapshot.GetText(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(545967, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545967")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestVirtualSpaces(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    public string P { get; set; }
    void M()
    {
        var v = new C
        {$$
        };
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendReturn()
                Assert.True(state.TextView.Caret.InVirtualSpace)
                Assert.Equal(12, state.TextView.Caret.Position.VirtualSpaces)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("P", isSoftSelected:=True)
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem("P", isHardSelected:=True)
                state.SendTab()
                Assert.Equal("            P", state.GetLineFromCurrentCaretPosition().GetText())

                Dim bufferPosition = state.TextView.Caret.Position.BufferPosition
                Assert.Equal(13, bufferPosition.Position - bufferPosition.GetContainingLine().Start.Position)
                Assert.False(state.TextView.Caret.InVirtualSpace)
            End Using
        End Function

        <WorkItem(546561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546561")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNamedParameterAgainstMRU(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class Program
{
    void Goo(string s) { }

    static void Main()
    {
        $$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                ' prime the MRU
                state.SendTypeChars("string")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' Delete what we just wrote.
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendEscape()
                Await state.AssertNoCompletionSession()

                ' ensure we still select the named param even though 'string' is in the MRU.
                state.SendTypeChars("Goo(s")
                Await state.AssertSelectedCompletionItem("s", displayTextSuffix:=":")
            End Using
        End Function

        <WorkItem(546403, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546403")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMissingOnObjectCreationAfterVar1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class A
{
    void Goo()
    {
        var v = new$$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(546403, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546403")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMissingOnObjectCreationAfterVar2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class A
{
    void Goo()
    {
        var v = new $$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("X")
                Await state.AssertCompletionItemsDoNotContainAny("X")
            End Using
        End Function

        <WorkItem(546917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546917")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEnumInSwitch(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
enum Numeros
{
}
class C
{
    void M()
    {
        Numeros n;
        switch (n)
        {
            case$$
        }
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="Numeros")
            End Using
        End Function

        <WorkItem(547016, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547016")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAmbiguityInLocalDeclaration(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    public int W;
    public C()
    {
        $$
        W = 0;
    }
}

            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("w")
                Await state.AssertSelectedCompletionItem(displayText:="W")
            End Using
        End Function

        <WorkItem(530835, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530835")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompletionFilterSpanCaretBoundary(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    public void Method()
    {
        $$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("Met")
                Await state.AssertSelectedCompletionItem(displayText:="Method")
                state.SendLeftKey()
                state.SendLeftKey()
                state.SendLeftKey()
                state.SendTypeChars("new")
                Await state.AssertSelectedCompletionItem(displayText:="Method", isSoftSelected:=True)
            End Using
        End Function

        <WorkItem(5487, "https://github.com/dotnet/roslyn/issues/5487")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitCharTypedAtTheBeginingOfTheFilterSpan(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    public bool Method()
    {
        if ($$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("Met")
                Await state.AssertCompletionSession()
                state.SendLeftKey()
                state.SendLeftKey()
                state.SendLeftKey()
                Await state.AssertSelectedCompletionItem(isSoftSelected:=True)
                state.SendTypeChars("!")
                Await state.AssertNoCompletionSession()
                Assert.Equal("if (!Met", state.GetLineTextFromCaretPosition().Trim())
                Assert.Equal("M", state.GetCaretPoint().BufferPosition.GetChar())
            End Using
        End Function

        <WorkItem(622957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/622957")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBangFiltersInDocComment(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
using System;

/// $$
/// TestDocComment
/// </summary>
class TestException : Exception { }
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("!")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("!--")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeCompletionDoesNotFilter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class C
{
    public void Method()
    {
        string$$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("string")
                Await state.AssertCompletionItemsContainAll("int", "Method")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeBeforeWordDoesNotSelect(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class C
{
    public void Method()
    {
        $$string
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("AccessViolationException")
                Await state.AssertCompletionItemsContainAll("int", "Method")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeCompletionSelectsWithoutRegardToCaretPosition(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class C
{
    public void Method()
    {
        s$$tring
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("string")
                Await state.AssertCompletionItemsContainAll("int", "Method")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TabAfterQuestionMark(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class C
{
    public void Method()
    {
        ?$$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTab()
                Assert.Equal(state.GetLineTextFromCaretPosition(), "        ?" + vbTab)
            End Using
        End Sub

        <WorkItem(657658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/657658")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function PreselectionIgnoresBrackets(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    $$

    static void Main(string[] args)
    {

    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("static void F<T>(int a, Func<T, int> b) { }")
                state.SendEscape()

                state.TextView.Caret.MoveTo(New VisualStudio.Text.SnapshotPoint(state.SubjectBuffer.CurrentSnapshot, 220))

                state.SendTypeChars("F")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("F", displayTextSuffix:="<>")
            End Using
        End Function

        <WorkItem(672474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/672474")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvokeSnippetCommandDismissesCompletion(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>$$</Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("us")
                Await state.AssertCompletionSession()
                state.SendInsertSnippetCommand()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(672474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/672474")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSurroundWithCommandDismissesCompletion(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>$$</Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("us")
                Await state.AssertCompletionSession()
                state.SendSurroundWithCommand()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(737239, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/737239")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function LetEditorHandleOpenParen(showCompletionInArgumentLists As Boolean) As Task
            Dim expected = <Document><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        List<int> x = new List<int>(
    }
}]]></Document>.Value.Replace(vbLf, vbCrLf)

            Using state = TestStateFactory.CreateCSharpTestState(<Document><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        List<int> x = new$$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(" ")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("List<int>")
                state.SendTypeChars("(")
                Assert.Equal(expected, state.GetDocumentText())
            End Using
        End Function

        <WorkItem(785637, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/785637")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitMovesCaretToWordEnd(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class C
{
    public void Main()
    {
        M$$ain
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Await state.SendCommitUniqueCompletionListItemAsync()
                Assert.Equal(state.GetLineFromCurrentCaretPosition().End, state.GetCaretPoint().BufferPosition)
            End Using
        End Function

        <WorkItem(775370, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775370")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function MatchingConsidersAtSign(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class C
{
    public void Main()
    {
        $$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("var @this = ""goo"";")
                state.SendReturn()
                state.SendTypeChars("string str = this.ToString();")
                state.SendReturn()
                state.SendTypeChars("str = @th")

                Await state.AssertSelectedCompletionItem("@this")
            End Using
        End Function

        <WorkItem(865089, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865089")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeFilterTextRemovesAttributeSuffix(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
[$$]
class AtAttribute : System.Attribute { }]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("At")
                Await state.AssertSelectedCompletionItem("At")
                Assert.Equal("At", state.GetSelectedItem().FilterText)
            End Using
        End Function

        <WorkItem(852578, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/852578")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function PreselectExceptionOverSnippet(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class C
{
    Exception goo() {
        return new $$
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem("Exception")
            End Using
        End Function

        <WorkItem(868286, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/868286")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitNameAfterAlias(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using goo = System$$]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars(".act<")
                state.AssertMatchesTextStartingAtLine(1, "using goo = System.Action<")
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompletionInLinkedFiles(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Thing2">
                        <Document FilePath="C.cs">
class C
{
    void M()
    {
        $$
    }

#if Thing1
    void Thing1() { }
#elif Thing2
    void Thing2() { }
#endif
}
                              </Document>
                    </Project>
                    <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Thing1">
                        <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                    </Project>
                </Workspace>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim documents = state.Workspace.Documents
                Dim linkDocument = documents.Single(Function(d) d.IsLinkFile)
                state.SendTypeChars("Thing1")
                Await state.AssertSelectedCompletionItem("Thing1")
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendEscape()
                state.Workspace.SetDocumentContext(linkDocument.Id)
                state.SendTypeChars("Thing1")
                Await state.AssertSelectedCompletionItem("Thing1")
                Assert.True(state.GetSelectedItem().Tags.Contains(WellKnownTags.Warning))
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendTypeChars("M")
                Await state.AssertSelectedCompletionItem("M")
                Assert.False(state.GetSelectedItem().Tags.Contains(WellKnownTags.Warning))
            End Using
        End Function

        <WorkItem(951726, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/951726")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DismissUponSave(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    $$
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("voi")
                Await state.AssertSelectedCompletionItem("void")
                state.SendSave()
                Await state.AssertNoCompletionSession()
                state.AssertMatchesTextStartingAtLine(3, "    voi")
            End Using
        End Function

        <WorkItem(930254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/930254")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoCompletionWithBoxSelection(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    {|Selection:$$int x;|}
    {|Selection:int y;|}
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
                state.SendTypeChars("goo")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(839555, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/839555")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TriggeredOnHash(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
$$]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("#")
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WorkItem(771761, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function RegionCompletionCommitTriggersFormatting_1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    $$
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("#reg")
                Await state.AssertSelectedCompletionItem("region")
                state.SendReturn()
                state.AssertMatchesTextStartingAtLine(3, "    #region")
            End Using
        End Function

        <WorkItem(771761, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function RegionCompletionCommitTriggersFormatting_2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    $$
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("#reg")
                Await state.AssertSelectedCompletionItem("region")
                state.SendTypeChars(" ")
                state.AssertMatchesTextStartingAtLine(3, "    #region ")
            End Using
        End Function

        <WorkItem(771761, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EndRegionCompletionCommitTriggersFormatting_2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    #region NameIt
    $$
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("#endreg")
                Await state.AssertSelectedCompletionItem("endregion")
                state.SendReturn()
                state.AssertMatchesTextStartingAtLine(4, "    #endregion ")
            End Using
        End Function

        <ExportCompletionProvider(NameOf(SlowProvider), LanguageNames.CSharp)>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class SlowProvider
            Inherits CommonCompletionProvider

            Public checkpoint As Checkpoint = New Checkpoint()

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Overrides Async Function ProvideCompletionsAsync(context As CompletionContext) As Task
                Await checkpoint.Task.ConfigureAwait(False)
            End Function

            Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
                Return True
            End Function

            Friend Overrides ReadOnly Property Language As String
                Get
                    Return LanguageNames.CSharp
                End Get
            End Property
        End Class

        <WorkItem(1015893, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015893")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceDismissesIfComputationIsIncomplete(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    void goo()
    {
        goo($$
    }
}]]></Document>,
                extraExportedTypes:={GetType(SlowProvider)}.ToList(),
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("f")
                state.SendBackspace()

                ' Send a backspace that goes beyond the session's applicable span
                ' before the model computation has finished. Then, allow the
                ' computation to complete. There should still be no session.
                state.SendBackspace()

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim slowProvider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of SlowProvider)().Single()
                slowProvider.checkpoint.Release()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(31135, "https://github.com/dotnet/roslyn/issues/31135")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TypingWithoutMatchAfterBackspaceDismissesCompletion(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class$$ C
{
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendBackspace()
                Await state.AssertCompletionSession()
                state.SendTypeChars("w")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(36515, "https://github.com/dotnet/roslyn/issues/36513")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TypingBackspaceShouldPreserveCase(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class Program
{
    void M()
    {
        Structure structure;
        structure.$$
    }

    struct Structure
    {
        public int A;
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendBackspace()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("structure")
                state.SendTypeChars(".")
                Await state.AssertCompletionItemsContainAll("A")
            End Using
        End Function

        <WorkItem(1594, "https://github.com/dotnet/roslyn/issues/1594")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoPreselectionOnSpaceWhenAbuttingWord(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class Program
{
    void Main()
    {
        Program p = new $$Program();
    }
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(1594, "https://github.com/dotnet/roslyn/issues/1594")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SpacePreselectionAtEndOfFile(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class Program
{
    void Main()
    {
        Program p = new $$]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars(" ")
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WorkItem(1659, "https://github.com/dotnet/roslyn/issues/1659")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DismissOnSelectAllCommand(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    void goo(int x)
    {
        $$]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                ' Note: the caret is at the file, so the Select All command's movement
                ' of the caret to the end of the selection isn't responsible for
                ' dismissing the session.
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendSelectAll()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(588, "https://github.com/dotnet/roslyn/issues/588")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CompletionCommitAndFormatAreSeparateUndoTransactions(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    void goo(int x)
    {
        int doodle;
$$]]></Document>,
                extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("doo;")
                state.AssertMatchesTextStartingAtLine(6, "        doodle;")
                state.SendUndo()
                state.AssertMatchesTextStartingAtLine(6, "doo;")
            End Using
        End Sub

        <WorkItem(4978, "https://github.com/dotnet/roslyn/issues/4978")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SessionNotStartedWhenCaretNotMappableIntoSubjectBuffer(showCompletionInArgumentLists As Boolean) As Task
            ' In inline diff view, typing delete next to a "deletion",
            ' can cause our CommandChain to be called with a subjectbuffer
            ' and TextView such that the textView's caret can't be mapped
            ' into our subject buffer.
            '
            ' To test this, we create a projection buffer with 2 source
            ' spans: one of "text" content type and one based on a C#
            ' buffer. We create a TextView with that projection as
            ' its buffer, setting the caret such that it maps only
            ' into the "text" buffer. We then call the completionImplementation
            ' command handlers with commandargs based on that TextView
            ' but with the C# buffer as the SubjectBuffer.

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    void goo(int x)
    {$$
        /********/
        int doodle;
        }
}]]></Document>,
                extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim textBufferFactoryService = state.GetExportedValue(Of ITextBufferFactoryService)()
                Dim contentTypeService = state.GetExportedValue(Of VisualStudio.Utilities.IContentTypeRegistryService)()
                Dim contentType = contentTypeService.GetContentType(ContentTypeNames.CSharpContentType)
                Dim textViewFactory = state.GetExportedValue(Of ITextEditorFactoryService)()
                Dim editorOperationsFactory = state.GetExportedValue(Of IEditorOperationsFactoryService)()

                Dim otherBuffer = textBufferFactoryService.CreateTextBuffer("text", contentType)
                Dim otherExposedSpan = otherBuffer.CurrentSnapshot.CreateTrackingSpan(0, 4, SpanTrackingMode.EdgeExclusive, TrackingFidelityMode.Forward)

                Dim subjectBufferExposedSpan = state.SubjectBuffer.CurrentSnapshot.CreateTrackingSpan(0, state.SubjectBuffer.CurrentSnapshot.Length, SpanTrackingMode.EdgeExclusive, TrackingFidelityMode.Forward)

                Dim projectionBufferFactory = state.GetExportedValue(Of IProjectionBufferFactoryService)()
                Dim projection = projectionBufferFactory.CreateProjectionBuffer(Nothing, New Object() {otherExposedSpan, subjectBufferExposedSpan}.ToList(), ProjectionBufferOptions.None)

                Using disposableView As DisposableTextView = textViewFactory.CreateDisposableTextView(projection)
                    disposableView.TextView.Caret.MoveTo(New SnapshotPoint(disposableView.TextView.TextBuffer.CurrentSnapshot, 0))

                    Dim editorOperations = editorOperationsFactory.GetEditorOperations(disposableView.TextView)
                    state.SendDeleteToSpecificViewAndBuffer(disposableView.TextView, state.SubjectBuffer)

                    Await state.AssertNoCompletionSession()
                End Using
            End Using
        End Function

        <WorkItem(588, "https://github.com/dotnet/roslyn/issues/588")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround1(showCompletionInArgumentLists As Boolean) As Task
            Using New CultureContext(New CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateCSharpTestState(
                               <Document><![CDATA[
        class C
        {
            void goo(int x)
            {
                string.$$]]></Document>,
                               extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                               showCompletionInArgumentLists:=showCompletionInArgumentLists)
                    state.SendTypeChars("is")
                    Await state.AssertSelectedCompletionItem("IsInterned")
                End Using
            End Using

        End Function

        <WorkItem(588, "https://github.com/dotnet/roslyn/issues/588")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround2(showCompletionInArgumentLists As Boolean) As Task
            Using New CultureContext(New CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateCSharpTestState(
                               <Document><![CDATA[
        class C
        {
            void goo(int x)
            {
                string.$$]]></Document>,
                               extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                               showCompletionInArgumentLists:=showCompletionInArgumentLists)
                    state.SendTypeChars("ı")
                    Await state.AssertSelectedCompletionItem()
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround3(showCompletionInArgumentLists As Boolean) As Task
            Using New CultureContext(New CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateCSharpTestState(
                               <Document><![CDATA[
        class TARIFE { }
        class C
        {
            void goo(int x)
            {
                var t = new $$]]></Document>,
                               extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                               showCompletionInArgumentLists:=showCompletionInArgumentLists)
                    state.SendTypeChars("tarif")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("TARIFE")
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround4(showCompletionInArgumentLists As Boolean) As Task
            Using New CultureContext(New CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateCSharpTestState(
                               <Document><![CDATA[
        class IFADE {}
        class ifTest {}
        class C
        {
            void goo(int x)
            {
              IFADE ifade = null;
              $$]]></Document>,
                               extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                               showCompletionInArgumentLists:=showCompletionInArgumentLists)
                    state.SendTypeChars("if")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("if")
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround5(showCompletionInArgumentLists As Boolean) As Task
            Using New CultureContext(New CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateCSharpTestState(
                               <Document><![CDATA[
        class İFADE {}
        class ifTest {}
        class C
        {
            void goo(int x)
            {
              İFADE ifade = null;
                $$]]></Document>,
                               extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                               showCompletionInArgumentLists:=showCompletionInArgumentLists)
                    state.SendTypeChars("if")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("if")
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround6(showCompletionInArgumentLists As Boolean) As Task
            Using New CultureContext(New CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateCSharpTestState(
                               <Document><![CDATA[
        class TARİFE { }
        class C
        {
            void goo(int x)
            {
                var obj = new $$]]></Document>,
                               extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                               showCompletionInArgumentLists:=showCompletionInArgumentLists)
                    state.SendTypeChars("tarif")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("TARİFE")
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround7(showCompletionInArgumentLists As Boolean) As Task
            Using New CultureContext(New CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateCSharpTestState(
                               <Document><![CDATA[
        class İFADE {}
        class ifTest {}
        class C
        {
            void goo(int x)
            {
              var obj = new $$]]></Document>,
                               extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                               showCompletionInArgumentLists:=showCompletionInArgumentLists)
                    state.SendTypeChars("ifad")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("İFADE")
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround8(showCompletionInArgumentLists As Boolean) As Task
            Using New CultureContext(New CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateCSharpTestState(
                               <Document><![CDATA[
        class IFADE {}
        class ifTest {}
        class C
        {
            void goo(int x)
            {
              var obj = new $$]]></Document>,
                               extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                               showCompletionInArgumentLists:=showCompletionInArgumentLists)
                    state.SendTypeChars("ifad")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("IFADE")
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround9(showCompletionInArgumentLists As Boolean) As Task
            Using New CultureContext(New CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateCSharpTestState(
                               <Document><![CDATA[
        class IFADE {}
        class ifTest {}
        class C
        {
            void goo(int x)
            {
              IFADE ifade = null;
              $$]]></Document>,
                               extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                               showCompletionInArgumentLists:=showCompletionInArgumentLists)
                    state.SendTypeChars("IF")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("if")
                End Using
            End Using

        End Function

        <WorkItem(29938, "https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround10(showCompletionInArgumentLists As Boolean) As Task
            Using New CultureContext(New CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateCSharpTestState(
                               <Document><![CDATA[
        class İFADE {}
        class ifTest {}
        class C
        {
            void goo(int x)
            {
              İFADE ifade = null;
                $$]]></Document>, extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                    state.SendTypeChars("IF")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("if")
                End Using
            End Using

        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselection1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
using System.Threading;
class Program
{
    void Cancel(int x, CancellationToken cancellationToken)
    {
        Cancel(x + 1, cancellationToken: $$)
    }
}]]></Document>,
                           extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("cancellationToken", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselection2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
class Program
{
    static void Main(string[] args)
    {
        int aaz = 0;
        args = $$
    }
}]]></Document>,
                           extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("a")
                Await state.AssertSelectedCompletionItem("args", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselection_DoesNotOverrideEnumPreselection(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
enum E
{

}

class Program
{
    static void Main(string[] args)
    {
        E e;
        e = $$
    }
}]]></Document>,
                           extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("E", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselection_DoesNotOverrideEnumPreselection2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
enum E
{
    A
}

class Program
{
    static void Main(string[] args)
    {
        E e = E.A;
        if (e == $$
    }
}]]></Document>,
                           extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("E", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselection3(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
class D {}

class Program
{
    static void Main(string[] args)
    {
       int cw = 7;
       D cx = new D();
       D cx2 = $$
    }
}]]></Document>,
                           extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("c")
                Await state.AssertSelectedCompletionItem("cx", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselectionLocalsOverType(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
class A {}

class Program
{
    static void Main(string[] args)
    {
       A cx = new A();
       A cx2 = $$
    }
}]]></Document>,
                           extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("c")
                Await state.AssertSelectedCompletionItem("cx", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselectionParameterOverMethod(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
class Program
{
    bool f;

    void goo(bool x) { }

    void Main(string[] args)
    {
        goo($$) // Not "Equals"
    }
}]]></Document>,
                           extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("f", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/6942"), CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselectionConvertibility1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
abstract class C {}
class D : C {}
class Program
{
    static void Main(string[] args)
    {
       D cx = new D();
       C cx2 = $$
    }
}]]></Document>,
                           extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("c")
                Await state.AssertSelectedCompletionItem("cx", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselectionLocalOverProperty(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
class Program
{
    public int aaa { get; }

     void Main(string[] args)
    {
        int aaq;

        int y = a$$
    }
}]]></Document>,
                           extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("aaq", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(12254, "https://github.com/dotnet/roslyn/issues/12254")>
        Public Sub TestGenericCallOnTypeContainingAnonymousType(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        new[] { new { x = 1 } }.ToArr$$
    }
}]]></Document>,
                           extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.SendTypeChars("(")
                state.AssertMatchesTextStartingAtLine(7, "new[] { new { x = 1 } }.ToArray(")
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselectionSetterValuey(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
class Program
{
    int _x;
    int X
    {
        set
        {
            _x = $$
        }
    }
}]]></Document>,
                           extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("value", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(12530, "https://github.com/dotnet/roslyn/issues/12530")>
        Public Async Function TestAnonymousTypeDescription(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        new[] { new { x = 1 } }.ToArr$$
    }
}]]></Document>,
                           extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(description:=
$"({ CSharpFeaturesResources.extension }) 'a[] System.Collections.Generic.IEnumerable<'a>.ToArray<'a>()

{ FeaturesResources.Types_colon }
    'a { FeaturesResources.is_ } new {{ int x }}")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRecursiveGenericSymbolKey(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
using System.Collections.Generic;

class Program
{
    static void ReplaceInList<T>(List<T> list, T oldItem, T newItem)
    {
        $$
    }
}]]></Document>,
                           extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("list")
                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
                state.SendTypeChars("Add")

                Await state.AssertSelectedCompletionItem("Add", description:="void List<T>.Add(T item)")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitNamedParameterWithColon(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
using System.Collections.Generic;

class Program
{
    static void Main(int args)
    {
        Main(args$$
    }
}]]></Document>,
                           extraExportedTypes:={GetType(CSharpFormattingInteractionService)}.ToList(),
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.SendTypeChars(":")
                Await state.AssertNoCompletionSession()
                Assert.Contains("args:", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(13481, "https://github.com/dotnet/roslyn/issues/13481")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackspaceSelection1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

class Program
{
    static void Main()
    {
        DateTimeOffset$$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                For Each c In "Offset"
                    state.SendBackspace()
                    Await state.WaitForAsynchronousOperationsAsync()
                Next

                Await state.AssertSelectedCompletionItem("DateTime")
            End Using
        End Function

        <WorkItem(13481, "https://github.com/dotnet/roslyn/issues/13481")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackspaceSelection2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

class Program
{
    static void Main()
    {
        DateTimeOffset.$$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                For Each c In "Offset."
                    state.SendBackspace()
                    Await state.WaitForAsynchronousOperationsAsync()
                Next

                Await state.AssertSelectedCompletionItem("DateTime")
            End Using
        End Function

        <WorkItem(14465, "https://github.com/dotnet/roslyn/issues/14465")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TypingNumberShouldNotDismiss1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    void Moo1()
    {
        new C()$$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
                state.SendTypeChars("1")
                Await state.AssertSelectedCompletionItem("Moo1")
            End Using
        End Function

        <WorkItem(14085, "https://github.com/dotnet/roslyn/issues/14085")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypingDoesNotOverrideExactMatch(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.IO;
class C
{
    void Moo1()
    {
        string path = $$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("Path")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("Path")
            End Using
        End Function

        <WorkItem(14085, "https://github.com/dotnet/roslyn/issues/14085")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function MRUOverTargetTyping(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.IO;
using System.Threading.Tasks;
class C
{
    async Task Moo()
    {
        await Moo().$$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("Configure")
                state.SendTab()
                For i = 1 To "ConfigureAwait".Length
                    state.SendBackspace()
                Next

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("ConfigureAwait")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function MovingCaretToStartSoftSelects(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;

class C
{
    void M()
    {
        $$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("Conso")
                Await state.AssertSelectedCompletionItem(displayText:="Console", isHardSelected:=True)
                For Each ch In "Conso"
                    state.SendLeftKey()
                Next

                Await state.AssertSelectedCompletionItem(displayText:="Console", isHardSelected:=False)

                state.SendRightKey()
                Await state.AssertSelectedCompletionItem(displayText:="Console", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoBlockOnCompletionItems1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  using $$
                              </Document>,
                              extraExportedTypes:={GetType(BooleanTaskControlledCompletionProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of BooleanTaskControlledCompletionProvider)().Single()

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionViewOptions.BlockForCompletionItems, LanguageNames.CSharp), False)

                state.SendTypeChars("Sys.")
                Await state.AssertNoCompletionSession()
                Assert.Contains("Sys.", state.GetLineTextFromCaretPosition())

                provider.completionSource.SetResult(True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoBlockOnCompletionItems2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  using $$
                              </Document>,
                              extraExportedTypes:={GetType(CompletedTaskControlledCompletionProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionViewOptions.BlockForCompletionItems, LanguageNames.CSharp), False)

                state.SendTypeChars("Sys")
                Await state.AssertSelectedCompletionItem(displayText:="System")
                state.SendTypeChars(".")
                Assert.Contains("System.", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoBlockOnCompletionItems4(showCompletionInArgumentLists As Boolean) As Task
            ' This test verifies a scenario with the following conditions:
            ' a. A slow completion provider
            ' b. The block option set to false.
            ' Scenario:
            ' 1. Type 'Sys'
            ' 2. Send CommitIfUnique (Ctrl + space)
            ' 3. Wait for 250ms.
            ' 4. Verify that there is no completion window shown. In the new completion, we can just start the verification and check that the verification is still running.
            ' 5. Check that the commit is not yet provided: there is 'Sys' but no 'System'
            ' 6. Simulate unblocking the provider.
            ' 7. Verify that the completion completes CommitIfUnique.
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  using $$
                              </Document>,
                              extraExportedTypes:={GetType(BooleanTaskControlledCompletionProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of BooleanTaskControlledCompletionProvider)().Single()

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionViewOptions.BlockForCompletionItems, LanguageNames.CSharp), False)

                state.SendTypeChars("Sys")

                Dim task1 As Task = Nothing
                Dim task2 As Task = Nothing

                Dim providerCalledHandler =
                    Sub()
                        task2 = New Task(
                        Sub()
                            Thread.Sleep(250)
                            Try
                                ' 3. Check that the other task is running/deadlocked.
                                Assert.Equal(TaskStatus.Running, task1.Status)
                                Assert.Contains("Sys", state.GetLineTextFromCaretPosition())
                                Assert.DoesNotContain("System", state.GetLineTextFromCaretPosition())
                                ' Need the Finally to avoid deadlocks if any of Asserts failed, the task will never complete and Task.WhenAll will wait forever.
                            Finally
                                ' 4. Unblock the first task and the main thread.
                                provider.completionSource.SetResult(True)
                            End Try
                        End Sub)

                        task1 = Task.Run(
                        Sub()
                            task2.Start()
                            ' 2. Deadlock here as well: getting items is waiting provider to respond.
                            state.CalculateItemsIfSessionExists()
                        End Sub)

                    End Sub

                AddHandler provider.ProviderCalled, providerCalledHandler

                ' SendCommitUniqueCompletionListItem is a asynchronous operation.
                ' It guarantees that ProviderCalled will be triggered and after that the completion will deadlock waiting for a task to be resolved.
                ' In the new completion, when pressed <ctrl>-<space>, we have to wait for the aggregate operation to complete.
                ' 1. Deadlock here.
                Await state.SendCommitUniqueCompletionListItemAsync()

                Assert.NotNull(task1)
                Assert.NotNull(task2)
                Await Task.WhenAll(task1, task2)

                Await state.AssertNoCompletionSession()
                Assert.Contains("System", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoBlockOnCompletionItems3(showCompletionInArgumentLists As Boolean) As Task
            ' This test verifies a scenario with the following conditions:
            ' a. A slow completion provider
            ' b. The block option set to false.
            ' Scenario:
            ' 1. Type 'Sys'
            ' 2. Send CommitIfUnique (Ctrl + space)
            ' 3. Wait for 250ms.
            ' 4. Verify that there is no completion window shown. In the new completion, we can just start the verification and check that the verification is still running.
            ' 5. Check that the commit is not yet provided: there is 'Sys' but no 'System'
            ' 6. The next statement in the UI thread after CommitIfUnique is typing 'a'.
            ' 7. Simulate unblocking the provider.
            ' 8. Verify that
            ' 8.a. The old completion adds 'a' to 'Sys' and displays 'Sysa'. CommitIfUnique is canceled because it was interrupted by typing 'a'.
            ' 8.b. The new completion completes CommitIfUnique and then adds 'a'.
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  using $$
                              </Document>,
                              extraExportedTypes:={GetType(BooleanTaskControlledCompletionProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of BooleanTaskControlledCompletionProvider)().Single()

                Dim globalOptions = state.Workspace.GetService(Of IGlobalOptionService)
                globalOptions.SetGlobalOption(New OptionKey(CompletionViewOptions.BlockForCompletionItems, LanguageNames.CSharp), False)

                state.SendTypeChars("Sys")
                Dim task1 As Task = Nothing
                Dim task2 As Task = Nothing

                Dim providerCalledHandler =
                    Sub()
                        task2 = New Task(
                            Sub()
                                Thread.Sleep(250)
                                Try
                                    ' 3. Check that the other task is running/deadlocked.
                                    Assert.Equal(TaskStatus.Running, task1.Status)
                                    Assert.Contains("Sys", state.GetLineTextFromCaretPosition())
                                    Assert.DoesNotContain("System", state.GetLineTextFromCaretPosition())
                                    ' Need the Finally to avoid deadlocks if any of Asserts failed, the task will never complete and Task.WhenAll will wait forever.
                                Finally
                                    ' 4. Unblock the first task and the main thread.
                                    provider.completionSource.SetResult(True)
                                End Try
                            End Sub)

                        task1 = Task.Run(
                        Sub()
                            task2.Start()
                            ' 2. Deadlock here as well: getting items is waiting provider to respond.
                            state.CalculateItemsIfSessionExists()
                        End Sub)
                    End Sub

                AddHandler provider.ProviderCalled, providerCalledHandler

                ' SendCommitUniqueCompletionListItem is an asynchronous operation.
                ' It guarantees that ProviderCalled will be triggered and after that the completion will deadlock waiting for a task to be resolved.
                ' In the new completion, when pressed <ctrl>-<space>, we have to wait for the aggregate operation to complete.
                ' 1. Deadlock here.
                Await state.SendCommitUniqueCompletionListItemAsync()

                ' 5. Put insertion of 'a' into the edtior queue. It can be executed in the foreground thread only
                state.SendTypeChars("a")

                Assert.NotNull(task1)
                Assert.NotNull(task2)
                Await Task.WhenAll(task1, task2)

                Await state.AssertNoCompletionSession()
                ' Here is a difference between the old and the new completions:
                ' The old completion adds 'a' to 'Sys' and displays 'Sysa'. CommitIfUnique is canceled because it was interrupted by typing 'a'.
                ' The new completion completes CommitIfUnique and then adds 'a'.
                Assert.Contains("Systema", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSwitchBetweenBlockingAndNoBlockOnCompletion(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  using $$
                              </Document>,
                              extraExportedTypes:={GetType(BooleanTaskControlledCompletionProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim globalOptions = state.Workspace.GetService(Of IGlobalOptionService)
                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of BooleanTaskControlledCompletionProvider)().Single()

#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(Function()
                             Task.Delay(TimeSpan.FromSeconds(10))
                             provider.completionSource.SetResult(True)
                             Return True
                         End Function)
#Enable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed

                state.SendTypeChars("Sys.")
                Assert.Contains("System.", state.GetLineTextFromCaretPosition())

                ' reset the input
                For i As Integer = 1 To "System.".Length
                    state.SendBackspace()
                Next

                state.SendEscape()

                Await state.WaitForAsynchronousOperationsAsync()

                ' reset the task
                provider.Reset()

                ' Switch to the non-blocking mode
                globalOptions.SetGlobalOption(New OptionKey(CompletionViewOptions.BlockForCompletionItems, LanguageNames.CSharp), False)

                ' re-use of TestNoBlockOnCompletionItems1
                state.SendTypeChars("Sys.")
                Await state.AssertNoCompletionSession()
                Assert.Contains("Sys.", state.GetLineTextFromCaretPosition())
                provider.completionSource.SetResult(True)

                For i As Integer = 1 To "Sys.".Length
                    state.SendBackspace()
                Next

                state.SendEscape()

                Await state.WaitForAsynchronousOperationsAsync()

                ' reset the task
                provider.Reset()

                ' Switch to the blocking mode
                globalOptions.SetGlobalOption(New OptionKey(CompletionViewOptions.BlockForCompletionItems, LanguageNames.CSharp), True)

#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(Function()
                             Task.Delay(TimeSpan.FromSeconds(10))
                             provider.completionSource.SetResult(True)
                             Return True
                         End Function)
#Enable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed

                state.SendTypeChars("Sys.")
                Await state.AssertCompletionSession()
                Assert.Contains("System.", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        Private MustInherit Class TaskControlledCompletionProvider
            Inherits CompletionProvider

            Private _task As Task

            Public Event ProviderCalled()

            Public Sub New(task As Task)
                _task = task
            End Sub

            Public Sub UpdateTask(task As Task)
                _task = task
            End Sub

            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                RaiseEvent ProviderCalled()
                Return _task
            End Function
        End Class

        <ExportCompletionProvider(NameOf(CompletedTaskControlledCompletionProvider), LanguageNames.CSharp)>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class CompletedTaskControlledCompletionProvider
            Inherits TaskControlledCompletionProvider

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
                MyBase.New(Task.FromResult(True))
            End Sub
        End Class

        <ExportCompletionProvider(NameOf(BooleanTaskControlledCompletionProvider), LanguageNames.CSharp)>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class BooleanTaskControlledCompletionProvider
            Inherits TaskControlledCompletionProvider

            Public completionSource As TaskCompletionSource(Of Boolean)

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
                MyBase.New(Task.CompletedTask)
                Reset()
            End Sub

            Public Sub Reset()
                completionSource = New TaskCompletionSource(Of Boolean)
                UpdateTask(completionSource.Task)
            End Sub
        End Class

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function Filters_EmptyList1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.IO;
using System.Threading.Tasks;
class C
{
    async Task Moo()
    {
        var x = asd$$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                Dim oldFilters = state.GetCompletionItemFilters()
                Dim newFilters = ArrayBuilder(Of Data.CompletionFilterWithState).GetInstance()
                For Each f In oldFilters
                    Assert.NotEqual(FilterSet.InterfaceFilter.DisplayText, f.Filter.DisplayText)
                    newFilters.Add(f.WithSelected(False))
                Next

                newFilters.Add(New Data.CompletionFilterWithState(FilterSet.InterfaceFilter, isAvailable:=True, isSelected:=True))

                state.RaiseFiltersChanged(newFilters.ToImmutableAndFree())

                Await state.WaitForUIRenderedAsync()
                Assert.Null(state.GetSelectedItem())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function Filters_EmptyList2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.IO;
using System.Threading.Tasks;
class C
{
    async Task Moo()
    {
        var x = asd$$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()
                Dim oldFilters = state.GetCompletionItemFilters()
                Dim newFilters = ArrayBuilder(Of Data.CompletionFilterWithState).GetInstance()
                For Each f In oldFilters
                    Assert.NotEqual(FilterSet.InterfaceFilter.DisplayText, f.Filter.DisplayText)
                    newFilters.Add(f.WithSelected(False))
                Next

                newFilters.Add(New Data.CompletionFilterWithState(FilterSet.InterfaceFilter, isAvailable:=True, isSelected:=True))

                state.RaiseFiltersChanged(newFilters.ToImmutableAndFree())
                Await state.WaitForUIRenderedAsync()
                Assert.Null(state.GetSelectedItem())
                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function Filters_EmptyList3(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.IO;
using System.Threading.Tasks;
class C
{
    async Task Moo()
    {
        var x = asd$$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()
                Dim oldFilters = state.GetCompletionItemFilters()
                Dim newFilters = ArrayBuilder(Of Data.CompletionFilterWithState).GetInstance()
                For Each f In oldFilters
                    Assert.NotEqual(FilterSet.InterfaceFilter.DisplayText, f.Filter.DisplayText)
                    newFilters.Add(f.WithSelected(False))
                Next

                newFilters.Add(New Data.CompletionFilterWithState(FilterSet.InterfaceFilter, isAvailable:=True, isSelected:=True))

                state.RaiseFiltersChanged(newFilters.ToImmutableAndFree())
                Await state.WaitForUIRenderedAsync()
                Assert.Null(state.GetSelectedItem())
                state.SendReturn()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function Filters_EmptyList4(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.IO;
using System.Threading.Tasks;
class C
{
    async Task Moo()
    {
        var x = asd$$
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()
                Dim oldFilters = state.GetCompletionItemFilters()
                Dim newFilters = ArrayBuilder(Of Data.CompletionFilterWithState).GetInstance()
                For Each f In oldFilters
                    Assert.NotEqual(FilterSet.InterfaceFilter.DisplayText, f.Filter.DisplayText)
                    newFilters.Add(f.WithSelected(False))
                Next

                newFilters.Add(New Data.CompletionFilterWithState(FilterSet.InterfaceFilter, isAvailable:=True, isSelected:=True))

                state.RaiseFiltersChanged(newFilters.ToImmutableAndFree())
                Await state.WaitForUIRenderedAsync()
                Assert.Null(state.GetSelectedItem())
                state.SendTypeChars(".")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(15881, "https://github.com/dotnet/roslyn/issues/15881")>
        Public Async Function CompletionAfterDotBeforeAwaitTask(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Threading.Tasks;

class C
{
    async Task Moo()
    {
        Task.$$
        await Task.Delay(50);
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WorkItem(14704, "https://github.com/dotnet/roslyn/issues/14704")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceTriggerSubstringMatching(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;
class Program
{
    static void Main(string[] args)
    {
        if (Environment$$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="Environment", isHardSelected:=True)
            End Using
        End Function

        <WorkItem(16236, "https://github.com/dotnet/roslyn/issues/16236")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeNamedParameterEqualsItemCommittedOnSpace(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
[A($$)]
class AAttribute: Attribute
{
    public string Skip { get; set; }
} </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("Skip")
                Await state.AssertCompletionSession()
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
                Assert.Equal("[A(Skip )]", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(362890, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=362890")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFilteringAfterSimpleInvokeShowsAllItemsMatchingFilter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[

static class Color
{
    public const uint Red = 1;
    public const uint Green = 2;
    public const uint Blue = 3;
}

class C
{
    void M()
    {
        Color.Re$$d
    }
}
            ]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                Await state.AssertSelectedCompletionItem("Red")
                Await state.AssertCompletionItemsContainAll("Red", "Green", "Blue", "Equals")

                Dim oldFilters = state.GetCompletionItemFilters()
                Dim newFiltersBuilder = ArrayBuilder(Of Data.CompletionFilterWithState).GetInstance()
                For Each f In oldFilters
                    newFiltersBuilder.Add(f.WithSelected(f.Filter.DisplayText = FilterSet.ConstantFilter.DisplayText))
                Next

                state.RaiseFiltersChanged(newFiltersBuilder.ToImmutableAndFree())

                Await state.WaitForUIRenderedAsync()
                Await state.AssertSelectedCompletionItem("Red")
                Await state.AssertCompletionItemsContainAll("Red", "Green", "Blue")
                Await state.AssertCompletionItemsDoNotContainAny("Equals")

                oldFilters = state.GetCompletionItemFilters()
                newFiltersBuilder = ArrayBuilder(Of Data.CompletionFilterWithState).GetInstance()
                For Each f In oldFilters
                    newFiltersBuilder.Add(f.WithSelected(False))
                Next

                state.RaiseFiltersChanged(newFiltersBuilder.ToImmutableAndFree())

                Await state.WaitForUIRenderedAsync()
                Await state.AssertSelectedCompletionItem("Red")
                Await state.AssertCompletionItemsContainAll({"Red", "Green", "Blue", "Equals"})
            End Using
        End Function

        <WorkItem(16236, "https://github.com/dotnet/roslyn/issues/16236")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NameCompletionSorting(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
interface ISyntaxFactsService {}
class C
{
    void M()
    {
        ISyntaxFactsService $$
    }
} </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()

                Dim expectedOrder =
                    {
                        "syntaxFactsService",
                        "syntaxFacts",
                        "factsService",
                        "syntax",
                        "service"
                    }

                state.AssertItemsInOrder(expectedOrder)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestLargeChangeBrokenUpIntoSmallTextChanges(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class C
{
    void goo() {
        return $$
    }
}]]></Document>,
                extraExportedTypes:={GetType(MultipleChangeCompletionProvider)}.ToList(),
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of MultipleChangeCompletionProvider)().Single()

                Dim testDocument = state.Workspace.Documents(0)
                Dim textBuffer = testDocument.GetTextBuffer()

                Dim snapshotBeforeCommit = textBuffer.CurrentSnapshot
                provider.SetInfo(snapshotBeforeCommit.GetText(), testDocument.CursorPosition.Value)

                ' First send a space to trigger out special completionImplementation provider.
                state.SendInvokeCompletionList()
                state.SendTab()

                ' Verify that we see the entire change
                Dim finalText = textBuffer.CurrentSnapshot.GetText()
                Assert.Equal(
"using NewUsing;
using System;
class C
{
    void goo() {
        return InsertedItem
    }
}", finalText)

                Dim changes = snapshotBeforeCommit.Version.Changes
                ' This should have happened as two text changes to the buffer.
                Assert.Equal(2, changes.Count)

                Dim actualChanges = changes.ToArray()
                Dim firstChange = actualChanges(0)
                Assert.Equal(New Span(0, 0), firstChange.OldSpan)
                Assert.Equal("using NewUsing;", firstChange.NewText)

                Dim secondChange = actualChanges(1)
                Assert.Equal(New Span(testDocument.CursorPosition.Value, 0), secondChange.OldSpan)
                Assert.Equal("InsertedItem", secondChange.NewText)

                ' Make sure new edits happen after the text that was inserted.
                state.SendTypeChars("1")

                finalText = textBuffer.CurrentSnapshot.GetText()
                Assert.Equal(
"using NewUsing;
using System;
class C
{
    void goo() {
        return InsertedItem1
    }
}", finalText)
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestLargeChangeBrokenUpIntoSmallTextChanges2(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class C
{
    void goo() {
        return Custom$$
    }
}]]></Document>,
                extraExportedTypes:={GetType(MultipleChangeCompletionProvider)}.ToList(),
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of MultipleChangeCompletionProvider)().Single()

                Dim testDocument = state.Workspace.Documents(0)
                Dim textBuffer = testDocument.GetTextBuffer()

                Dim snapshotBeforeCommit = textBuffer.CurrentSnapshot
                provider.SetInfo(snapshotBeforeCommit.GetText(), testDocument.CursorPosition.Value)

                ' First send a space to trigger out special completionImplementation provider.
                state.SendInvokeCompletionList()
                state.SendTab()

                ' Verify that we see the entire change
                Dim finalText = textBuffer.CurrentSnapshot.GetText()
                Assert.Equal(
"using NewUsing;
using System;
class C
{
    void goo() {
        return InsertedItem
    }
}", finalText)

                Dim changes = snapshotBeforeCommit.Version.Changes
                ' This should have happened as two text changes to the buffer.
                Assert.Equal(2, changes.Count)

                Dim actualChanges = changes.ToArray()
                Dim firstChange = actualChanges(0)
                Assert.Equal(New Span(0, 0), firstChange.OldSpan)
                Assert.Equal("using NewUsing;", firstChange.NewText)

                Dim secondChange = actualChanges(1)
                Assert.Equal(New Span(testDocument.CursorPosition.Value - "Custom".Length, "Custom".Length), secondChange.OldSpan)
                Assert.Equal("InsertedItem", secondChange.NewText)

                ' Make sure new edits happen after the text that was inserted.
                state.SendTypeChars("1")

                finalText = textBuffer.CurrentSnapshot.GetText()
                Assert.Equal(
"using NewUsing;
using System;
class C
{
    void goo() {
        return InsertedItem1
    }
}", finalText)
            End Using
        End Sub

        <WorkItem(296512, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=296512")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRegionDirectiveIndentation(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    $$
}
                              </Document>,
                              includeFormatCommandHandler:=True,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("#")

                Assert.Equal("#", state.GetLineFromCurrentCaretPosition().GetText())
                Await state.AssertCompletionSession()

                state.SendTypeChars("reg")
                Await state.AssertSelectedCompletionItem(displayText:="region")
                state.SendReturn()
                Await state.AssertNoCompletionSession()
                Assert.Equal("    #region", state.GetLineFromCurrentCaretPosition().GetText())
                Assert.Equal(state.GetLineFromCurrentCaretPosition().End, state.GetCaretPoint().BufferPosition)

                state.SendReturn()
                Assert.Equal("", state.GetLineFromCurrentCaretPosition().GetText())
                state.SendTypeChars("#")

                Assert.Equal("#", state.GetLineFromCurrentCaretPosition().GetText())
                Await state.AssertCompletionSession()

                state.SendTypeChars("endr")
                Await state.AssertSelectedCompletionItem(displayText:="endregion")
                state.SendReturn()
                Assert.Equal("    #endregion", state.GetLineFromCurrentCaretPosition().GetText())
                Assert.Equal(state.GetLineFromCurrentCaretPosition().End, state.GetCaretPoint().BufferPosition)

            End Using
        End Function

        <WpfTheory>
        <InlineData("r")>
        <InlineData("load")>
        <WorkItem(49861, "https://github.com/dotnet/roslyn/issues/49861")>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function PathDirective(directive As String) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    #   <%= directive %>  $$
}
                              </Document>)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendTypeChars("""")

                Assert.Equal($"    #   {directive}  """, state.GetLineFromCurrentCaretPosition().GetText())
                Await state.AssertCompletionSession()

                state.SendTypeChars("x")

                Assert.Equal($"    #   {directive}  ""x", state.GetLineFromCurrentCaretPosition().GetText())
                Await state.AssertCompletionSession()

                state.SendBackspace()

                Assert.Equal($"    #   {directive}  """, state.GetLineFromCurrentCaretPosition().GetText())
                Await state.AssertCompletionSession()

                state.SendBackspace()

                Assert.Equal($"    #   {directive}  ", state.GetLineFromCurrentCaretPosition().GetText())
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AfterIdentifierInCaseLabel(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void M()
    {
        switch (true)
        {
            case identifier $$
        }
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("w")
                Await state.AssertSelectedCompletionItem(displayText:="when", isHardSelected:=False)

                state.SendBackspace()
                state.SendTypeChars("i")
                Await state.AssertSelectedCompletionItem(displayText:="identifier", isHardSelected:=False)

            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AfterIdentifierInCaseLabel_ColorColor(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class identifier { }
class C
{
    const identifier identifier = null;
    void M()
    {
        switch (true)
        {
            case identifier $$
        }
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("w")
                Await state.AssertSelectedCompletionItem(displayText:="when", isHardSelected:=False)

                state.SendBackspace()
                state.SendTypeChars("i")
                Await state.AssertSelectedCompletionItem(displayText:="identifier", isHardSelected:=False)

            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AfterIdentifierInCaseLabel_ClassNameOnly(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class identifier { }
class C
{
    void M()
    {
        switch (true)
        {
            case identifier $$
        }
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("z")
                Await state.AssertSelectedCompletionItem(displayText:="identifier", isHardSelected:=False)

                state.SendBackspace()
                state.SendTypeChars("i")
                Await state.AssertSelectedCompletionItem(displayText:="identifier", isHardSelected:=False)

            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AfterIdentifierInCaseLabel_ClassNameOnly_WithMiscLetters(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class identifier { }
class C
{
    void M()
    {
        switch (true)
        {
            case identifier $$
        }
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("a")
                Await state.AssertSelectedCompletionItem(displayText:="and", isHardSelected:=False)

                state.SendBackspace()
                state.SendTypeChars("w")
                Await state.AssertSelectedCompletionItem(displayText:="when", isHardSelected:=False)

            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AfterDoubleIdentifierInCaseLabel(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void M()
    {
        switch (true)
        {
            case identifier identifier $$
        }
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("w")
                Await state.AssertSelectedCompletionItem(displayText:="when", isHardSelected:=True)

            End Using
        End Function

        <WorkItem(11959, "https://github.com/dotnet/roslyn/issues/11959")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestGenericAsyncTaskDeclaration(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
namespace A.B
{
    class TestClass { }
}

namespace A
{
    class C
    {
        async Task&lt;A$$ Method()
        { }
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertSelectedCompletionItem(displayText:="B", isSoftSelected:=True)
            End Using
        End Function

        <WorkItem(15348, "https://github.com/dotnet/roslyn/issues/15348")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterCasePatternSwitchLabel(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void M()
    {
        object o = 1;
        switch(o)
        {
            case int i:
                $$
                break;
        }
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("this")
                Await state.AssertSelectedCompletionItem(displayText:="this", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackspaceInMiddleOfSelection(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
public enum foo
{
    aaa
}

public class Program
{
    public static void Main(string[] args)
    {
        foo.a$$a
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="aaa", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackspaceWithMultipleCharactersSelected(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine$$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                state.SelectAndMoveCaret(-6)
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="Write", isHardSelected:=True)
            End Using
        End Function

        <WorkItem(30097, "https://github.com/dotnet/roslyn/issues/30097")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMRUKeepsTwoRecentlyUsedItems(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    public double Ma(double m) => m;

    public void Test()
    {
        $$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("M(M(M(M(")
                Await state.AssertNoCompletionSession()
                Assert.Equal("        Ma(m:(Ma(m:(", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(36546, "https://github.com/dotnet/roslyn/issues/36546")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDoNotDismissIfEmptyOnBackspaceIfStartedWithBackspace(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;

class C
{
    public void M()
    {
        Console.W$$
    }
}</Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendBackspace()
                Await state.AssertCompletionItemsContainAll("WriteLine")
            End Using
        End Function

        <WorkItem(36546, "https://github.com/dotnet/roslyn/issues/36546")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDoNotDismissIfEmptyOnMultipleBackspaceIfStartedInvoke(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;

class C
{
    public void M()
    {
        Console.Wr$$
    }
}</Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendBackspace()
                state.SendBackspace()
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WorkItem(30097, "https://github.com/dotnet/roslyn/issues/30097")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNamedParameterDoesNotAddExtraColon(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    public double M(double some) => m;

    public void Test()
    {
        $$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("M(some:M(some:")
                Await state.AssertNoCompletionSession()
                Assert.Equal("        M(some:M(some:", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSuggestionMode(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void M()
    {    
        $$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendToggleCompletionMode()
                Await state.WaitForAsynchronousOperationsAsync()
                state.SendTypeChars("s")
                Await state.AssertCompletionSession()
                Assert.True(state.HasSuggestedItem())
                Await state.AssertSelectedCompletionItem(displayText:="sbyte", isSoftSelected:=True)

                state.SendToggleCompletionMode()
                Await state.AssertCompletionSession()
                Assert.False(state.HasSuggestedItem())
                ' We want to soft select if we were already in soft select mode.
                Await state.AssertSelectedCompletionItem(displayText:="sbyte", isSoftSelected:=True)

                state.SendToggleCompletionMode()
                Await state.AssertCompletionSession()
                Assert.True(state.HasSuggestedItem())
                Await state.AssertSelectedCompletionItem(displayText:="sbyte", isSoftSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTabAfterOverride(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    override $$
    public static void M() { }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("gethashcod")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                state.AssertMatchesTextStartingAtLine(3, "    public override int GetHashCode()")
                state.AssertMatchesTextStartingAtLine(4, "    {")
                state.AssertMatchesTextStartingAtLine(5, "        return base.GetHashCode();")
                state.AssertMatchesTextStartingAtLine(6, "    }")
                state.AssertMatchesTextStartingAtLine(7, "    public static void M() { }")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSuppressNullableWarningExpression(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void M()
    {
        var s = "";
        s$$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("!")
                Await state.AssertNoCompletionSession()
                state.SendTypeChars(".")
                Await state.AssertCompletionItemsContainAll("ToString", "GetHashCode")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitIfUniqueFiltersIfNotUnique(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Method()
    {
        Me$$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertCompletionItemsContainAll("MemberwiseClone", "Method")
                Await state.AssertCompletionItemsDoNotContainAny("int", "ToString()", "Microsoft", "Math")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDismissCompletionOnBacktick(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;
class C
{
    void Method()
    {
        Con$$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("`")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSendCommitIfUnique(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
              <Document>
using System;
class C
{
    void Method()
    {
        var s="";
        s.Len$$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Length", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSendCommitIfUniqueInInsertionSession(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Method()
    {
        var s = "";
        s$$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".len")
                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Length", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSendCommitIfUniqueInDeletionSession1(showCompletionInArgumentLists As Boolean) As Task
            ' We explicitly use a weak matching on Delete.
            ' It matches by the first letter. Therefore, if backspace in s.Length, it matches s.Length and s.LastIndexOf.
            ' In this case, CommitIfUnique is not applied.
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Method()
    {
        var s = "";
        s.Normalize$$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendBackspace()
                Await state.AssertCompletionSession()
                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Normalize", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(37231, "https://github.com/dotnet/roslyn/issues/37231")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSendCommitIfUniqueInDeletionSession2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;
class C
{
    void Method()
    {
        AccessViolationException$$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendBackspace()
                Await state.AssertCompletionSession()
                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("AccessViolationException", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSendCommitIfUniqueWithIntelliCode(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Method()
    {
        var s = "";
        s.Len$$
    }
}
                              </Document>,
                              extraExportedTypes:={GetType(IntelliCodeMockProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockProvider)().Single()

                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Length", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSendCommitIfUniqueInInsertionSessionWithIntelliCode(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Method()
    {
        var s = "";
        s$$
    }
}
                              </Document>,
                              extraExportedTypes:={GetType(IntelliCodeMockProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockProvider)().Single()

                state.SendTypeChars(".len")
                Await state.AssertCompletionItemsContainAll("Length", "★ Length")
                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Length", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSendCommitIfUniqueInDeletionSessionWithIntelliCode(showCompletionInArgumentLists As Boolean) As Task
            ' We explicitly use a weak matching on Delete.
            ' It matches by the first letter. Therefore, if backspace in s.Length, it matches s.Length and s.LastIndexOf.
            ' In this case, CommitIfUnique is not applied.
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Method()
    {
        var s = "";
        s.Normalize$$
    }
}
                              </Document>,
                              extraExportedTypes:={GetType(IntelliCodeMockProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockProvider)().Single()

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendBackspace()
                Await state.AssertCompletionItemsContainAll("Normalize", "★ Normalize")
                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Normalize", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAutomationTextPassedToEditor(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Method()
    {
        var s = "";
        s.Len$$
    }
}
                              </Document>,
                              extraExportedTypes:={GetType(IntelliCodeMockProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockProvider)().Single()

                state.SendInvokeCompletionList()
                state.SendSelectCompletionItem("★ Length")
                Await state.AssertSelectedCompletionItem(displayText:="★ Length", automationText:=provider.AutomationTextString)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSendCommitIfUniqueWithIntelliCodeAndDuplicateItemsFromIntelliCode(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Method()
    {
        var s = "";
        s.Len$$
    }
}
                              </Document>,
                              extraExportedTypes:={GetType(IntelliCodeMockWeirdProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockWeirdProvider)().Single()

                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Length", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSendCommitIfUniqueInInsertionSessionWithIntelliCodeAndDuplicateItemsFromIntelliCode(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Method()
    {
        var s = "";
        s$$
    }
}
                              </Document>,
                              extraExportedTypes:={GetType(IntelliCodeMockWeirdProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockWeirdProvider)().Single()

                state.SendTypeChars(".len")
                Await state.AssertCompletionItemsContainAll("Length", "★ Length", "★ Length2")
                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Length", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function IntelliCodeItemPreferredAfterCommitingIntelliCodeItem(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Method()
    {
        var s = "";
        s$$
    }
}
                              </Document>,
                              extraExportedTypes:={GetType(IntelliCodeMockProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockProvider)().Single()

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendTypeChars(".nor")
                Await state.AssertCompletionItemsContainAll("Normalize", "★ Normalize")
                Await state.AssertSelectedCompletionItem("★ Normalize", displayTextSuffix:="()")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Normalize", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                For i = 1 To "ze".Length
                    state.SendBackspace()
                Next

                Await state.AssertSelectedCompletionItem("★ Normalize", displayTextSuffix:="()")

                state.SendEscape()
                For i = 1 To "Normali".Length
                    state.SendBackspace()
                Next

                state.SendEscape()
                Assert.Contains("s.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("★ Normalize", displayTextSuffix:="()")
                state.SendEscape()

                state.SendTypeChars("n")
                Await state.AssertSelectedCompletionItem("★ Normalize", displayTextSuffix:="()")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function IntelliCodeItemPreferredAfterCommitingNonIntelliCodeItem(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Method()
    {
        var s = "";
        s$$
    }
}
                              </Document>,
                              extraExportedTypes:={GetType(IntelliCodeMockProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockProvider)().Single()

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendTypeChars(".nor")
                Await state.AssertCompletionItemsContainAll("Normalize", "★ Normalize")
                Await state.AssertSelectedCompletionItem("★ Normalize", displayTextSuffix:="()")

                state.NavigateToDisplayText("Normalize")
                state.SendTab()

                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Normalize", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                For i = 1 To "ze".Length
                    state.SendBackspace()
                Next

                Await state.AssertSelectedCompletionItem("★ Normalize", displayTextSuffix:="()")

                state.SendEscape()
                For i = 1 To "Normali".Length
                    state.SendBackspace()
                Next

                state.SendEscape()
                Assert.Contains("s.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("★ Normalize", displayTextSuffix:="()")
                state.SendEscape()

                state.SendTypeChars("n")
                Await state.AssertSelectedCompletionItem("★ Normalize", displayTextSuffix:="()")
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function WarmUpTypeImportCompletionCache() As Task

            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" LanguageVersion="Preview" CommonReferences="true">
                        <ProjectReference>RefProj</ProjectReference>
                        <Document FilePath="C.cs"><![CDATA[
namespace NS1
    public class C1
    {
        void M()
        {
            $$
        }
    }
}
                        ]]></Document>
                    </Project>
                    <Project Language="C#" AssemblyName="RefProj" CommonReferences="true">
                        <Document><![CDATA[
namespace NS2
{
    public class UnimportedType
    {
    }
}
                        ]]></Document>
                    </Project>
                </Workspace>)

                Dim document = state.Workspace.CurrentSolution.GetDocument(state.Workspace.Documents.Single(Function(d) d.Name = "C.cs").Id)

                Dim completionService = CType(document.GetLanguageService(Of CompletionService)(), CompletionServiceWithProviders)
                completionService.GetTestAccessor().SuppressPartialSemantics()
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                Dim service = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of ITypeImportCompletionService)()

                service.QueueCacheWarmUpTask(document.Project)
                Await state.WaitForAsynchronousOperationsAsync()

                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                Await state.AssertCompletionItemsContain(displayText:="UnimportedType", displayTextSuffix:="")

                service.QueueCacheWarmUpTask(document.Project)
                Await state.WaitForAsynchronousOperationsAsync()
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function WarmUpExtensionMethodImportCompletionCache() As Task

            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" LanguageVersion="Preview" CommonReferences="true">
                        <ProjectReference>RefProj</ProjectReference>
                        <Document FilePath="C.cs"><![CDATA[
namespace NS1
    public class C1
    {
        void M(int x)
        {
            x.$$
        }
    }
}
                        ]]></Document>
                    </Project>
                    <Project Language="C#" AssemblyName="RefProj" CommonReferences="true">
                        <Document><![CDATA[
namespace NS2
{
    public static class Ext
    {
        public static bool IntegerExtMethod(this int x) => false;
    }
}
                        ]]></Document>
                    </Project>
                </Workspace>)

                Dim document = state.Workspace.CurrentSolution.GetDocument(state.Workspace.Documents.Single(Function(d) d.Name = "C.cs").Id)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                Dim completionService = CType(document.GetLanguageService(Of CompletionService)(), CompletionServiceWithProviders)
                completionService.GetTestAccessor().SuppressPartialSemantics()

                Await ExtensionMethodImportCompletionHelper.WarmUpCacheAsync(document.Project, CancellationToken.None)
                Await state.WaitForAsynchronousOperationsAsync()

                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                Await state.AssertCompletionItemsContain(displayText:="IntegerExtMethod", displayTextSuffix:="")

                ' Make sure any background work would be completed.
                Await ExtensionMethodImportCompletionHelper.WarmUpCacheAsync(document.Project, CancellationToken.None)
                Await state.WaitForAsynchronousOperationsAsync()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExpanderWithImportCompletionDisabled(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
namespace NS1
{
    class C
    {
        public void Foo()
        {
            Bar$$
        }
    }
}

namespace NS2
{
    public class Bar { }
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), False)

                ' trigger completion with import completion disabled
                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                ' make sure expander is selected
                state.SetCompletionItemExpanderState(isSelected:=True)
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                Await state.AssertSelectedCompletionItem(displayText:="Bar", inlineDescription:="NS2")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)

                ' unselect expander
                state.SetCompletionItemExpanderState(isSelected:=False)
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                Await state.AssertCompletionItemsDoNotContainAny("Bar")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)

                ' select expander again
                state.SetCompletionItemExpanderState(isSelected:=True)
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                Await state.AssertSelectedCompletionItem(displayText:="Bar", inlineDescription:="NS2")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)

                ' dismiss completion
                state.SendEscape()
                Await state.AssertNoCompletionSession()

                ' trigger completion again
                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                ' should not show unimported item by default
                Await state.AssertCompletionItemsDoNotContainAny({"Bar"})
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)

            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestExpanderWithImportCompletionEnabled(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
namespace NS1
{
    class C
    {
        public void Foo()
        {
            Bar$$
        }
    }
}

namespace NS2
{
    public class Bar { }
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                ' trigger completion with import completion enabled
                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                ' make sure expander is selected
                state.SetCompletionItemExpanderState(isSelected:=True)
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                Await state.AssertSelectedCompletionItem(displayText:="Bar", inlineDescription:="NS2")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)

                ' dismiss completion
                state.SendEscape()
                Await state.AssertNoCompletionSession()

                ' trigger completion again
                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                ' show expanded items by default
                Await state.AssertSelectedCompletionItem(displayText:="Bar", inlineDescription:="NS2")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)

            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ExpanderAvailableWhenNotInTypeContextButNotAddingAnyItems(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
namespace NS1
{
    $$
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                ' trigger completion with import completion enabled
                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)
                Dim length = state.GetCompletionItems().Count

                state.SetCompletionItemExpanderState(isSelected:=True)
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                Assert.Equal(length, state.GetCompletionItems().Count)
            End Using
        End Function

        <WorkItem(34943, "https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion), Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)>
        Public Async Function TypingDotsAfterInt(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C 
{
    void M()
    {
        int first = 3;
        int[] array = new int[100];
        var range = array[first$$];
    }
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
                Assert.True(state.IsSoftSelected())
                state.SendTypeChars(".")
                Assert.Contains("var range = array[first..];", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(34943, "https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion), Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)>
        Public Async Function TypingDotsAfterClassAndAfterIntProperty(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C 
{
    void M()
    {
        var d = new D();
        int[] array = new int[100];
        var range = array[d$$];
    }
}

class D
{
    public int A;
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertSelectedCompletionItem("A", isHardSelected:=True)
                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
                Assert.True(state.IsSoftSelected())
                state.SendTypeChars(".")
                Assert.Contains("var range = array[d.A..];", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(34943, "https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion), Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)>
        Public Async Function TypingDotsAfterClassAndAfterIntMethod(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C 
{
    void M()
    {
        var d = new D();
        int[] array = new int[100];
        var range = array[d$$];
    }
}

class D
{
    public int A() => 0;
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertSelectedCompletionItem("A", isHardSelected:=True)
                state.SendTypeChars("().")
                Await state.AssertCompletionSession()
                Assert.True(state.IsSoftSelected())
                state.SendTypeChars(".")
                Assert.Contains("var range = array[d.A()..];", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(34943, "https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion), Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)>
        Public Async Function TypingDotsAfterClassAndAfterDecimalProperty(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C 
{
    void M()
    {
        var d = new D();
        int[] array = new int[100];
        var range = array[d$$];
    }
}

class D
{
    public decimal A;
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertSelectedCompletionItem("GetHashCode", isHardSelected:=True)
                state.SendTypeChars("A.")
                Await state.AssertCompletionSession()
                Assert.True(state.IsSoftSelected())
                state.SendTypeChars(".")
                Assert.Contains("var range = array[d.A..];", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(34943, "https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion), Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)>
        Public Async Function TypingDotsAfterClassAndAfterDoubleMethod(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C 
{
    void M()
    {
        var d = new D();
        int[] array = new int[100];
        var range = array[d$$];
    }
}

class D
{
    public double A() => 0;
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertSelectedCompletionItem("GetHashCode", isHardSelected:=True)
                state.SendTypeChars("A().")
                Await state.AssertCompletionSession()
                Assert.True(state.IsSoftSelected())
                state.SendTypeChars(".")
                Assert.Contains("var range = array[d.A()..];", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(34943, "https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion), Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)>
        Public Async Function TypingDotsAfterIntWithinArrayDeclaration(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C 
{
    void M()
    {
        int d = 1;
        var array = new int[d$$];
    }
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
                Assert.True(state.IsSoftSelected())
                state.SendTypeChars(".")
                Assert.Contains("var array = new int[d..];", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(34943, "https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion), Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)>
        Public Async Function TypingDotsAfterIntInVariableDeclaration(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C 
{
    void M()
    {
        int d = 1;
        var e = d$$;
    }
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
                Assert.True(state.IsSoftSelected())
                state.SendTypeChars(".")
                Assert.Contains("var e = d..;", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(34943, "https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion), Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)>
        Public Async Function TypingToStringAfterIntInVariableDeclaration(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C 
{
    void M()
    {
        int d = 1;
        var e = d$$;
    }
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
                Assert.True(state.IsSoftSelected())
                state.SendTypeChars("ToStr(")
                Assert.Contains("var e = d.ToString(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(36187, "https://github.com/dotnet/roslyn/issues/36187")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion), Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)>
        Public Async Function CompletionWithTwoOverloadsOneOfThemIsEmpty(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    private enum A
    {
    	A,
    	B,
    }

    private void Get(string a) { }
    private void Get(A a) { }

    private void Test()
    {
    	Get$$
    }
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(")
                Await state.AssertSelectedCompletionItem(displayText:="A", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24960, "https://github.com/dotnet/roslyn/issues/24960")>
        Public Async Function TypeParameterTOnType(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C<T>
{
    $$
}]]>
                </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("T")
                Await state.AssertSelectedCompletionItem("T")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24960, "https://github.com/dotnet/roslyn/issues/24960")>
        Public Async Function TypeParameterTOnMethod(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    void M<T>()
    {
        $$
    }
}]]>
                </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("T")
                Await state.AssertSelectedCompletionItem("T")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionBeforeVarWithEnableNullableReferenceAnalysisIDEFeatures(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                 <Workspace>
                     <Project Language="C#" LanguageVersion="8" CommonReferences="true" AssemblyName="CSProj" Features="run-nullable-analysis=always">
                         <Document><![CDATA[
class C
{
    void M(string s)
    {
        s$$
        var o = new object();
    }
}]]></Document>
                     </Project>
                 </Workspace>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertCompletionItemsContainAll("Length")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletingWithColonInMethodParametersWithNoInstanceToInsert(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
<Document><![CDATA[class C
{
    void M(string s)
    {
        N(10, $$);
    }

    void N(int id, string serviceName) {}
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("serviceN")
                Await state.AssertCompletionSession()
                state.SendTypeChars(":")
                Assert.Contains("N(10, serviceName:);", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletingWithSpaceInMethodParametersWithNoInstanceToInsert(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
<Document><![CDATA[class C
{
    void M(string s)
    {
        N(10, $$);
    }

    void N(int id, string serviceName) {}
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("serviceN")
                Await state.AssertCompletionSession()
                state.SendTypeChars(" ")
                Assert.Contains("N(10, serviceName );", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(35163, "https://github.com/dotnet/roslyn/issues/35163")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NonExpandedItemShouldBePreferred_SameDisplayText(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
namespace NS1
{
    class C
    {
        public void Foo()
        {
            Bar$$
        }
    }

    public class Bar<T>
    {
    } 
}

namespace NS2
{
    public class Bar
    {
    }
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim expectedText = "
namespace NS1
{
    class C
    {
        public void Foo()
        {
            Bar
        }
    }

    public class Bar<T>
    {
    } 
}

namespace NS2
{
    public class Bar
    {
    }
}
"

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                Await state.AssertSelectedCompletionItem(displayText:="Bar", displayTextSuffix:="<>")

                state.SendTab()
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WorkItem(35163, "https://github.com/dotnet/roslyn/issues/35163")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NonExpandedItemShouldBePreferred_SameFullDisplayText(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
namespace NS1
{
    class C
    {
        public void Foo()
        {
            Bar$$
        }
    }

    public class Bar
    {
    } 
}

namespace NS2
{
    public class Bar
    {
    }
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim expectedText = "
namespace NS1
{
    class C
    {
        public void Foo()
        {
            Bar
        }
    }

    public class Bar
    {
    } 
}

namespace NS2
{
    public class Bar
    {
    }
}
"

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                Await state.AssertSelectedCompletionItem(displayText:="Bar")

                state.SendTab()
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WorkItem(35163, "https://github.com/dotnet/roslyn/issues/35163")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NonExpandedItemShouldBePreferred_ExpandedItemHasBetterButNotCompleteMatch(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
namespace NS1
{
    class C
    {
        public void Foo()
        {
            bar$$
        }
    }

    public class ABar
    {
    } 
}

namespace NS2
{
    public class Bar1
    {
    }
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim expectedText = "
namespace NS1
{
    class C
    {
        public void Foo()
        {
            ABar
        }
    }

    public class ABar
    {
    } 
}

namespace NS2
{
    public class Bar1
    {
    }
}
"

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                Await state.AssertSelectedCompletionItem(displayText:="ABar")

                state.SendTab()
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WorkItem(38253, "https://github.com/dotnet/roslyn/issues/38253")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NonExpandedItemShouldBePreferred_BothExpandedAndNonExpandedItemsHaveCompleteMatch(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
namespace NS1
{
    class C
    {
        public void Foo()
        {
            bar$$
        }
    }

    public class Bar
    {
    } 
}

namespace NS2
{
    public class Bar
    {
    }
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim expectedText = "
namespace NS1
{
    class C
    {
        public void Foo()
        {
            Bar
        }
    }

    public class Bar
    {
    } 
}

namespace NS2
{
    public class Bar
    {
    }
}
"

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                Await state.AssertSelectedCompletionItem(displayText:="Bar", inlineDescription:="")
                state.SendTab()
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WorkItem(38253, "https://github.com/dotnet/roslyn/issues/38253")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletelyMatchedExpandedItemAndWorseThanPrefixMatchedNonExpandedItem(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
namespace NS1
{
    class C
    {
        public void Foo()
        {
            bar$$
        }
    }

    public class ABar
    {
    } 
}

namespace NS2
{
    public class Bar
    {
    }
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim expectedText = "
using NS2;

namespace NS1
{
    class C
    {
        public void Foo()
        {
            Bar
        }
    }

    public class ABar
    {
    } 
}

namespace NS2
{
    public class Bar
    {
    }
}
"

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                Await state.AssertSelectedCompletionItem(displayText:="Bar", inlineDescription:="NS2")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)

                state.SendTab()
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletelyMatchedExpandedItemAndPrefixMatchedNonExpandedItem(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
namespace NS
{
    class C
    {
        void M()
        {
            object designer = null;
            des$$
        }
    }
}
 
namespace OtherNS
{
    public class DES { }                              
}
</Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                Await state.AssertSelectedCompletionItem(displayText:="designer")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
            End Using
        End Function

        <WorkItem(38253, "https://github.com/dotnet/roslyn/issues/38253")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SortItemsByPatternMatch(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
namespace NS
{
    class C
    {
        void M()
        {
            $$
        }
    }

    class Task { }

    class BTask1 { }
    class BTask2 { }
    class BTask3 { }


    class Task1 { }
    class Task2 { }
    class Task3 { }

    class ATaAaSaKa { }
} </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), False)

                state.SendTypeChars("task")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="Task")

                Dim expectedOrder =
                    {
                        "Task",
                        "Task1",
                        "Task2",
                        "Task3",
                        "BTask1",
                        "BTask2",
                        "BTask3",
                        "ATaAaSaKa"
                    }

                state.AssertItemsInOrder(expectedOrder)
            End Using
        End Function

        <WorkItem(41601, "https://github.com/dotnet/roslyn/issues/41601")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SortItemsByExpandedFlag(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
namespace NS1
{
    class C
    {
        void M()
        {
            $$
        }
    }

    class MyTask1 { }
    class MyTask2 { }
    class MyTask3 { }
}

namespace NS2
{
    class MyTask1 { }
    class MyTask2 { }
    class MyTask3 { }
}
</Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                ' trigger completion with import completion disabled
                state.SendInvokeCompletionList()
                Await state.WaitForUIRenderedAsync()

                ' make sure expander is selected
                state.SetCompletionItemExpanderState(isSelected:=True)
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                state.SendEscape()
                Await state.AssertNoCompletionSession()

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.SendTypeChars("mytask")
                Await state.WaitForAsynchronousOperationsAsync()

                ' make sure expander is selected
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                Await state.AssertSelectedCompletionItem(displayText:="MyTask1", inlineDescription:="")

                Dim expectedOrder As (String, String)() =
                    {
                        ("MyTask1", ""),
                        ("MyTask2", ""),
                        ("MyTask3", ""),
                        ("MyTask1", "NS2"),
                        ("MyTask2", "NS2"),
                        ("MyTask3", "NS2")
                    }
                state.AssertItemsInOrder(expectedOrder)
            End Using
        End Function

        <WorkItem(39519, "https://github.com/dotnet/roslyn/issues/39519")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSuggestedNamesDontStartWithDigit_DigitsInTheMiddle(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
namespace NS
{
    class C
    {
        public void Foo(Foo123Bar $$)
        {
        }
    }

    public class Foo123Bar
    {
    } 
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.ShowNameSuggestions, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("foo123Bar", "foo123", "foo", "bar")
                Await state.AssertCompletionItemsDoNotContainAny("123Bar")
            End Using
        End Function

        <WorkItem(39519, "https://github.com/dotnet/roslyn/issues/39519")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSuggestedNamesDontStartWithDigit_DigitsOnTheRight(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
namespace NS
{
    class C
    {
        public void Foo(Foo123 $$)
        {
        }
    }

    public class Foo123
    {
    } 
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.ShowNameSuggestions, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("foo123", "foo")
                Await state.AssertCompletionItemsDoNotContainAny("123")
            End Using
        End Function

        <ExportCompletionProvider(NameOf(MultipleChangeCompletionProvider), LanguageNames.CSharp)>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class MultipleChangeCompletionProvider
            Inherits CompletionProvider

            Private _text As String
            Private _caretPosition As Integer

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Sub SetInfo(text As String, caretPosition As Integer)
                _text = text
                _caretPosition = caretPosition
            End Sub

            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                context.AddItem(CompletionItem.Create(
                    "CustomItem",
                    rules:=CompletionItemRules.Default.WithMatchPriority(1000)))
                Return Task.CompletedTask
            End Function

            Public Overrides Function ShouldTriggerCompletion(text As SourceText, caretPosition As Integer, trigger As CompletionTrigger, options As OptionSet) As Boolean
                Return True
            End Function

            Public Overrides Function GetChangeAsync(document As Document, item As CompletionItem, commitKey As Char?, cancellationToken As CancellationToken) As Task(Of CompletionChange)
                Dim newText =
"using NewUsing;
using System;
class C
{
    void goo() {
        return InsertedItem"

                Dim change = CompletionChange.Create(
                    New TextChange(New TextSpan(0, _caretPosition), newText))
                Return Task.FromResult(change)
            End Function
        End Class

        <ExportCompletionProvider(NameOf(IntelliCodeMockProvider), LanguageNames.CSharp)>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class IntelliCodeMockProvider
            Inherits CompletionProvider

            Public AutomationTextString As String = "Hello from IntelliCode: Length"

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                Dim intelliCodeItem = CompletionItem.Create(displayText:="★ Length", filterText:="Length")
                intelliCodeItem.AutomationText = AutomationTextString
                context.AddItem(intelliCodeItem)

                context.AddItem(CompletionItem.Create(displayText:="★ Normalize", filterText:="Normalize", displayTextSuffix:="()"))
                context.AddItem(CompletionItem.Create(displayText:="Normalize", filterText:="Normalize"))
                context.AddItem(CompletionItem.Create(displayText:="Length", filterText:="Length"))
                context.AddItem(CompletionItem.Create(displayText:="ToString", filterText:="ToString", displayTextSuffix:="()"))
                context.AddItem(CompletionItem.Create(displayText:="First", filterText:="First", displayTextSuffix:="()"))
                Return Task.CompletedTask
            End Function

            Public Overrides Function ShouldTriggerCompletion(text As SourceText, caretPosition As Integer, trigger As CompletionTrigger, options As OptionSet) As Boolean
                Return True
            End Function

            Public Overrides Function GetChangeAsync(document As Document, item As CompletionItem, commitKey As Char?, cancellationToken As CancellationToken) As Task(Of CompletionChange)
                Dim commitText = item.DisplayText
                If commitText.StartsWith("★") Then
                    ' remove the star and the following space
                    commitText = commitText.Substring(2)
                End If

                Return Task.FromResult(CompletionChange.Create(New TextChange(item.Span, commitText)))
            End Function
        End Class

        <WorkItem(43439, "https://github.com/dotnet/roslyn/issues/43439")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSelectNullOverNuint(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    public static void Main()
    {
        object o = $$
    }
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                ' 'nu' should select 'null' instead of 'nuint' (even though 'nuint' sorts higher in the list textually).
                state.SendTypeChars("nu")
                Await state.AssertSelectedCompletionItem(displayText:="null", isHardSelected:=True)
                Await state.AssertCompletionItemsContain("nuint", "")

                ' even after 'nuint' is selected, deleting the 'i' should still take us back to 'null'.
                state.SendTypeChars("i")
                Await state.AssertSelectedCompletionItem(displayText:="nuint", isHardSelected:=True)
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="null", isHardSelected:=True)
            End Using
        End Function

        <WorkItem(43439, "https://github.com/dotnet/roslyn/issues/43439")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSelectNuintOverNullOnceInMRU(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    public static void Main()
    {
        object o = $$
    }
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("nui")
                Await state.AssertCompletionItemsContain("nuint", "")
                state.SendTab()
                Assert.Contains("nuint", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)

                state.SendDeleteWordToLeft()

                ' nuint should be in the mru now.  so typing 'nu' should select it instead of null.
                state.SendTypeChars("nu")
                Await state.AssertSelectedCompletionItem(displayText:="nuint", isHardSelected:=True)
            End Using
        End Function

        <WorkItem(944031, "https://dev.azure.com/devdiv/DevDiv/_workitems/edit/944031")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLambdaParameterInferenceInJoin1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System.Collections.Generic;
using System.Linq;

class Program
{
    public class Book
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public string Name { get; set; }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Nickname { get; set; }
    }

    static void Main()
    {
        var books = new List&lt;Book&gt;();
        var persons = new List&lt;Person&gt;();

        var join = persons.Join(books, person => person.Id, book => book.$$, (person, book) => new
        {
            person.Id,
            person.Nickname,
            book.Name
        });
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("OwnerId", "")
            End Using
        End Function

        <WorkItem(944031, "https://dev.azure.com/devdiv/DevDiv/_workitems/edit/944031")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLambdaParameterInferenceInJoin2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System.Collections.Generic;
using System.Linq;

class Program
{
    public class Book
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public string Name { get; set; }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Nickname { get; set; }
    }

    static void Main()
    {
        var books = new List&lt;Book&gt;();
        var persons = new List&lt;Person&gt;();

        var join = persons.Join(books, person => person.Id, book => book.OwnerId, (person, book) => new
        {
            person.Id,
            person.Nickname,
            book.$$
        });
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("Name", "")
            End Using
        End Function

        <WorkItem(944031, "https://dev.azure.com/devdiv/DevDiv/_workitems/edit/944031")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLambdaParameterInferenceInGroupJoin1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System.Collections.Generic;
using System.Linq;

class Program
{
    public class Book
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public string Name { get; set; }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Nickname { get; set; }
    }

    static void Main()
    {
        var books = new List&lt;Book&gt;();
        var persons = new List&lt;Person&gt;();

        var join = persons.GroupJoin(books, person => person.Id, book => book.$$, (person, books1) => new
        {
            person.Id,
            person.Nickname,
            books1.Select(s => s.Name)
        });
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("OwnerId", "")
            End Using
        End Function

        <WorkItem(944031, "https://dev.azure.com/devdiv/DevDiv/_workitems/edit/944031")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLambdaParameterInferenceInGroupJoin2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System.Collections.Generic;
using System.Linq;

class Program
{
    public class Book
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public string Name { get; set; }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Nickname { get; set; }
    }

    static void Main()
    {
        var books = new List&lt;Book&gt;();
        var persons = new List&lt;Person&gt;();

        var join = persons.GroupJoin(books, person => person.Id, book => book.OwnerId, (person, books1) => new
        {
            person.Id,
            person.Nickname,
            books1.$$
        });
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("Select", "<>")
            End Using
        End Function

        <WorkItem(944031, "https://dev.azure.com/devdiv/DevDiv/_workitems/edit/944031")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestLambdaParameterInferenceInGroupJoin3(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System.Collections.Generic;
using System.Linq;

class Program
{
    public class Book
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public string Name { get; set; }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Nickname { get; set; }
    }

    static void Main()
    {
        var books = new List&lt;Book&gt;();
        var persons = new List&lt;Person&gt;();

        var join = persons.GroupJoin(books, person => person.Id, book => book.OwnerId, (person, books1) => new
        {
            person.Id,
            person.Nickname,
            books1.Select(s => s.$$)
        });
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("Name", "")
            End Using
        End Function

        <WorkItem(1128749, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1128749")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFallingBackToItemWithLongestCommonPrefixWhenNoMatch(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class SomePrefixAndName {}

class C
{
    void Method()
    {
        SomePrefixOrName$$
    }
}
                              </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()

                state.SendEscape()
                Await state.WaitForAsynchronousOperationsAsync()

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="SomePrefixAndName", isHardSelected:=False)

            End Using
        End Function

        <WorkItem(47511, "https://github.com/dotnet/roslyn/pull/47511")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ConversionsOperatorsAndIndexerAreShownBelowMethodsAndPropertiesAndBeforeUnimportedItems()
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
namespace A
{
    using B;
    public static class CExtensions{
        public static void ExtensionUnimported(this C c) { }
    }
}
namespace B
{
    public static class CExtensions{
        public static void ExtensionImported(this C c) { }
    }

    public class C
    {
        public int A { get; } = default;
        public int Z { get; } = default;
        public void AM() { }
        public void ZM() { }
        public int this[int _] => default;
        public static explicit operator int(C _) => default;
        public static C operator +(C a, C b) => default;
    }

    class Program
    {
        static void Main()
        {
            var c = new C();
            c.$$
        }
    }
}                              </Document>)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                state.AssertItemsInOrder(New String() {
                    "A", ' Method, properties, and imported extension methods alphabetical ordered
                    "AM",
                    "Equals",
                    "ExtensionImported",
                    "GetHashCode",
                    "GetType",
                    "this[]", ' Indexer
                    "ToString",
                    "Z",
                    "ZM",
                    "(int)", ' Conversions
                    "+", ' Operators
                    "ExtensionUnimported" 'Unimported extension methods
                })
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestCompleteMethodParenthesisForSymbolCompletionProvider(showCompletionInArgumentLists As Boolean, <CombinatorialValues(";"c, "."c)> commitChar As Char)
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
                public class B
                {
                    private void C11()
                    {
                        $$
                    }
                }</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim expectedText = $"
                public class B
                {{
                    private void C11()
                    {{
                        C11(){commitChar}
                    }}
                }}"
                state.SendTypeChars("C")
                Dim expectingItem = state.GetCompletionItems().First(Function(item) item.DisplayText.Equals("C11"))
                Assert.True(SymbolCompletionItem.GetShouldProvideParenthesisCompletion(expectingItem))

                state.SendSelectCompletionItem("C11")
                state.SendTypeChars(commitChar)
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        Public Sub TestNestedMethodCallWhenCommitUsingSemicolon(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
                public class B
                {
                    private void C11()
                    {
                        AAA($$)
                    }

                    private int DDD() => 1;
                    private int AAA(int i) => 1;
                }</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim expectedText = $"
                public class B
                {{
                    private void C11()
                    {{
                        AAA(DDD());
                    }}

                    private int DDD() => 1;
                    private int AAA(int i) => 1;
                }}"
                state.SendTypeChars("D")
                Dim expectingItem = state.GetCompletionItems().First(Function(item) item.DisplayText.Equals("DDD"))
                Assert.True(SymbolCompletionItem.GetShouldProvideParenthesisCompletion(expectingItem))

                state.SendSelectCompletionItem("DDD")
                state.SendTypeChars(";"c)
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        Public Sub TestNestedMethodCallUnderDelegateContextWhenCommitUsingSemicolon(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
                using System;
                public class B
                {
                    private void C11()
                    {
                        AAA($$)
                    }

                    private void DDD() {}
                    private int AAA(Action c) => 1;
                }</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim expectedText = $"
                using System;
                public class B
                {{
                    private void C11()
                    {{
                        AAA(DDD);
                    }}

                    private void DDD() {{}}
                    private int AAA(Action c) => 1;
                }}"
                state.SendTypeChars("D")
                Dim expectingItem = state.GetCompletionItems().First(Function(item) item.DisplayText.Equals("DDD"))
                Assert.False(SymbolCompletionItem.GetShouldProvideParenthesisCompletion(expectingItem))

                state.SendSelectCompletionItem("DDD")
                state.SendTypeChars(";"c)
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        Public Sub TestNestedMethodCallWhenCommitUsingDot(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
                public class B
                {
                    private void C11()
                    {
                        AAA($$)
                    }

                    private int DDD() => 1;
                    private int AAA(int i) => 1;
                }</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim expectedText = $"
                public class B
                {{
                    private void C11()
                    {{
                        AAA(DDD().)
                    }}

                    private int DDD() => 1;
                    private int AAA(int i) => 1;
                }}"
                state.SendTypeChars("D")
                Dim expectingItem = state.GetCompletionItems().First(Function(item) item.DisplayText.Equals("DDD"))
                Assert.True(SymbolCompletionItem.GetShouldProvideParenthesisCompletion(expectingItem))

                state.SendSelectCompletionItem("DDD")
                state.SendTypeChars("."c)
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestCompleteMethodParenthesisForSymbolCompletionProviderUnderDelegateContext(showCompletionInArgumentLists As Boolean, <CombinatorialValues(";"c, "."c)> commitChar As Char)
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
                using System;
                public class B
                {
                    private void C11()
                    {
                        Action t = $$
                    }
                }</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim expectedText = $"
                using System;
                public class B
                {{
                    private void C11()
                    {{
                        Action t = C11{commitChar}
                    }}
                }}"
                state.SendTypeChars("C")
                Dim expectingItem = state.GetCompletionItems().First(Function(item) item.DisplayText.Equals("C11"))
                Assert.False(SymbolCompletionItem.GetShouldProvideParenthesisCompletion(expectingItem))

                state.SendSelectCompletionItem("C11")
                state.SendTypeChars(commitChar)
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestCompleteObjectCreationParenthesisForSymbolCreationCompletionProvider(showCompletionInArgumentLists As Boolean, <CombinatorialValues(";"c, "."c)> commitChar As Char)
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
                using Bar = System.String
                public class AA
                {
                    private static void CC()
                    {
                        var a = new $$
                    }
                }</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim expectedText = $"
                using Bar = System.String
                public class AA
                {{
                    private static void CC()
                    {{
                        var a = new Bar(){commitChar}
                    }}
                }}"
                state.SendTypeChars("B")
                Dim expectingItem = state.GetCompletionItems().First(Function(item) item.DisplayText.Equals("AA"))
                Assert.True(SymbolCompletionItem.GetShouldProvideParenthesisCompletion(expectingItem))

                state.SendSelectCompletionItem("Bar")
                state.SendTypeChars(commitChar)
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestCompleteObjectCreationParenthesisForSymbolCreationCompletionProviderUnderNonObjectCreationContext(showCompletionInArgumentLists As Boolean, <CombinatorialValues(";"c, "."c)> commitChar As Char)
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
                using Bar = System.String
                public class AA
                {
                    private static void CC()
                    {
                        $$
                    }
                }</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim expectedText = $"
                using Bar = System.String
                public class AA
                {{
                    private static void CC()
                    {{
                        Bar{commitChar}
                    }}
                }}"
                state.SendTypeChars("B")
                Dim expectingItem = state.GetCompletionItems().First(Function(item) item.DisplayText.Equals("AA"))
                Assert.False(SymbolCompletionItem.GetShouldProvideParenthesisCompletion(expectingItem))

                state.SendSelectCompletionItem("Bar")

                state.SendTypeChars(commitChar)
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestCompleteParenthesisForObjectCreationCompletionProvider(showCompletionInArgumentLists As Boolean, <CombinatorialValues(";"c, "."c)> commitChar As Char)
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
                public class AA
                {
                    private static void CC()
                    {
                        AA a = new $$
                    }
                }</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim expectedText = $"
                public class AA
                {{
                    private static void CC()
                    {{
                        AA a = new AA(){commitChar}
                    }}
                }}"
                state.SendTypeChars("A")
                state.SendSelectCompletionItem("AA")
                state.SendTypeChars(commitChar)
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestCompleteParenthesisForExtensionMethodImportCompletionProvider(showCompletionInArgumentLists As Boolean, <CombinatorialValues(";"c, "."c)> commitChar As Char)
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
namespace CC
{
    public static class DD
    {
        public static int ToInt(this AA a) => 1;
    }
}
public class AA
{
    private static void CC()
    {
        AA a = new AA();
        var value = a.$$
    }
}</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                Dim expectedText = $"
using CC;

namespace CC
{{
    public static class DD
    {{
        public static int ToInt(this AA a) => 1;
    }}
}}
public class AA
{{
    private static void CC()
    {{
        AA a = new AA();
        var value = a.ToInt(){commitChar}
    }}
}}"
                state.SendTypeChars("To")
                state.SendSelectCompletionItem("ToInt")
                state.SendTypeChars(commitChar)
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompleteParenthesisForTypeImportCompletionProvider(showCompletionInArgumentLists As Boolean, <CombinatorialValues(";"c, "."c)> commitChar As Char) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
namespace CC
{
    public class Bar
    {
    }
}
public class AA
{
    private static void CC()
    {
        var a = new $$
    }
}</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                ' Make sure expander is selected
                state.SetCompletionItemExpanderState(isSelected:=True)
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                Dim expectedText = $"
using CC;

namespace CC
{{
    public class Bar
    {{
    }}
}}
public class AA
{{
    private static void CC()
    {{
        var a = new Bar(){commitChar}
    }}
}}"
                state.SendTypeChars("Ba")
                state.SendSelectCompletionItem("Bar")
                state.SendTypeChars(commitChar)
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompleteParenthesisForTypeImportCompletionProviderUnderNonObjectCreationContext(showCompletionInArgumentLists As Boolean, <CombinatorialValues(";"c, "."c)> commitChar As Char) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
namespace CC
{
    public class Bar
    {
    }
}
public class AA
{
    private static void CC()
    {
        $$
    }
}</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                ' Make sure expander is selected
                state.SetCompletionItemExpanderState(isSelected:=True)
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                Dim expectedText = $"
using CC;

namespace CC
{{
    public class Bar
    {{
    }}
}}
public class AA
{{
    private static void CC()
    {{
        Bar{commitChar}
    }}
}}"
                state.SendTypeChars("Ba")
                state.SendSelectCompletionItem("Bar")
                state.SendTypeChars(commitChar)
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompleteParenthesisForMethodUnderNameofContext(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
public class AA
{
    private static void CC()
    {
        var x = nameof($$)
    }
}</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                state.SetCompletionItemExpanderState(isSelected:=True)
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                Dim expectedText = "
public class AA
{
    private static void CC()
    {
        var x = nameof(CC);
    }
}"
                state.SendTypeChars("CC")
                state.SendSelectCompletionItem("CC")
                state.SendTypeChars(";")
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompleteParenthesisForGenericMethodUnderNameofContext(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
using System;
public class AA
{
    private static void CC()
    {
        var x = nameof($$)
    }

    private static T GetSomething&lt;T&gt;() => (T)Activator.GetInstance(typeof(T));
}</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                state.SetCompletionItemExpanderState(isSelected:=True)
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                Dim expectedText = "
using System;
public class AA
{
    private static void CC()
    {
        var x = nameof(GetSomething);
    }

    private static T GetSomething<T>() => (T)Activator.GetInstance(typeof(T));
}"
                state.SendTypeChars("Get")
                state.SendSelectCompletionItem("GetSomething<>")
                state.SendTypeChars(";")
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompleteParenthesisForFullMethodUnderNameofContext(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
public class AA
{
    private static void CC()
    {
        var x = nameof($$)
    }
}
namespace Bar1
{
    public class Bar2
    {
        public void Bar3() { }
    }
}</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                state.SetCompletionItemExpanderState(isSelected:=True)
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                Dim expectedText = "
public class AA
{
    private static void CC()
    {
        var x = nameof(Bar1.Bar2.Bar3);
    }
}
namespace Bar1
{
    public class Bar2
    {
        public void Bar3() { }
    }
}"
                state.SendTypeChars("Bar1.Bar2.Ba")
                state.SendSelectCompletionItem("Bar3")
                state.SendTypeChars(";")
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompleteParenthesisForFunctionPointer(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
using System;
public unsafe class AA
{
    private static void CC()
    {
        delegate*&lt;void&gt; p = $$
    }

    public static void Bar() {}
}</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)
                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                state.SetCompletionItemExpanderState(isSelected:=True)
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                Dim expectedText = "
using System;
public unsafe class AA
{
    private static void CC()
    {
        delegate*<void> p = Bar;
    }

    public static void Bar() {}
}"
                state.SendTypeChars("Ba")
                state.SendSelectCompletionItem("Bar")
                state.SendTypeChars(";")
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInPreprocessorIf(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Goo,Bar,Baz">
                            <Document>
#if $$
                            </Document>
                        </Project>
                    </Workspace>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll({"Goo", "Bar", "Baz", "true", "false"})
                state.SendTypeChars("Go")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("#if Goo", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInPreprocessorElif(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Goo,Bar,Baz">
                            <Document>
#if false
#elif $$
                            </Document>
                        </Project>
                    </Workspace>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll({"Goo", "Bar", "Baz", "true", "false"})
                state.SendTypeChars("Go")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("#elif Goo", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionNotInPreprocessorElse(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Goo,Bar,Baz">
                            <Document>
#if false
#elif false
#else $$
                            </Document>
                        </Project>
                    </Workspace>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInPreprocessorParenthesized(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Goo,Bar,Baz">
                            <Document>
#if ($$
                            </Document>
                        </Project>
                    </Workspace>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll({"Goo", "Bar", "Baz", "true", "false"})
                state.SendTypeChars("Go")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("#if (Goo", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInPreprocessorNot(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Goo,Bar,Baz">
                            <Document>
#if !$$
                            </Document>
                        </Project>
                    </Workspace>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll({"Goo", "Bar", "Baz", "true", "false"})
                state.SendTypeChars("Go")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("#if !Goo", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInPreprocessorAnd(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Goo,Bar,Baz">
                            <Document>
#if true &amp;&amp; $$
                            </Document>
                        </Project>
                    </Workspace>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll({"Goo", "Bar", "Baz", "true", "false"})
                state.SendTypeChars("Go")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("#if true && Goo", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInPreprocessorOr(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Goo,Bar,Baz">
                            <Document>
#if true || $$
                            </Document>
                        </Project>
                    </Workspace>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll({"Goo", "Bar", "Baz", "true", "false"})
                state.SendTypeChars("Go")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("#if true || Goo", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInPreprocessorCasingDifference(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Goo,Bar,BAR,Baz">
                            <Document>
#if $$
                            </Document>
                        </Project>
                    </Workspace>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll({"Goo", "Bar", "BAR", "Baz", "true", "false"})
                state.SendTypeChars("Go")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("#if Goo", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(55546, "https://github.com/dotnet/roslyn/issues/55546")>
        Public Async Function PreferHigherMatchPriorityOverCaseSensitiveWithOnlyLowercaseTyped(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
using System;
public static class Ext
{
    public static bool Should(this int x) => false;
}
public class AA
{
    private static void CC(int x)
    {
        var y = x.$$
    }
}</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                state.SendTypeChars("sh")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                Await state.AssertSelectedCompletionItem("Should")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(55546, "https://github.com/dotnet/roslyn/issues/55546")>
        Public Async Function PreferBestMatchPriorityAndCaseSensitiveWithOnlyLowercaseTyped(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
public class AA
{
    private static void CC)
    {
        $$
    }
}</Document>,
                extraExportedTypes:={GetType(MultipleLowercaseItemsWithNoHigherMatchPriorityProvider)}.ToList(),
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                state.SendTypeChars("item")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                ' We have multiple items have "item" prefix but with different MatchPriority, 
                ' need to ensure we select the one with best casing AND MatchPriority.
                Await state.AssertSelectedCompletionItem("item2")
            End Using
        End Function

        <ExportCompletionProvider(NameOf(MultipleLowercaseItemsWithNoHigherMatchPriorityProvider), LanguageNames.CSharp)>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class MultipleLowercaseItemsWithNoHigherMatchPriorityProvider
            Inherits CompletionProvider

            Private ReadOnly highPriorityRule As CompletionItemRules = CompletionItemRules.Default.WithMatchPriority(MatchPriority.Default + 1)
            Private ReadOnly lowPriorityRule As CompletionItemRules = CompletionItemRules.Default.WithMatchPriority(MatchPriority.Default - 1)

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            ' All lowercase items have lower MatchPriority than uppercase item, except one with equal value.
            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                context.AddItem(CompletionItem.Create(displayText:="item1", rules:=lowPriorityRule))
                context.AddItem(CompletionItem.Create(displayText:="item2", rules:=highPriorityRule))
                context.AddItem(CompletionItem.Create(displayText:="item3", rules:=CompletionItemRules.Default))
                context.AddItem(CompletionItem.Create(displayText:="Item4", rules:=highPriorityRule))
                Return Task.CompletedTask
            End Function

            Public Overrides Function ShouldTriggerCompletion(text As SourceText, caretPosition As Integer, trigger As CompletionTrigger, options As OptionSet) As Boolean
                Return True
            End Function
        End Class

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(55546, "https://github.com/dotnet/roslyn/issues/55546")>
        Public Async Function PreferBestCaseSensitiveWithUppercaseTyped(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
public class AA
{
    private static void CC)
    {
        $$
    }
}</Document>,
                extraExportedTypes:={GetType(UppercaseItemWithLowerPriorityProvider)}.ToList(),
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                state.SendTypeChars("Item")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()
                Await state.AssertSelectedCompletionItem("Item3")   ' ensure we prefer casing over match priority if uppercase is typed
            End Using
        End Function

        <ExportCompletionProvider(NameOf(UppercaseItemWithLowerPriorityProvider), LanguageNames.CSharp)>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class UppercaseItemWithLowerPriorityProvider
            Inherits CompletionProvider

            Private ReadOnly highPriorityRule As CompletionItemRules = CompletionItemRules.Default.WithMatchPriority(MatchPriority.Default + 1)
            Private ReadOnly lowPriorityRule As CompletionItemRules = CompletionItemRules.Default.WithMatchPriority(MatchPriority.Default - 1)

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                context.AddItem(CompletionItem.Create(displayText:="item1", rules:=highPriorityRule))
                context.AddItem(CompletionItem.Create(displayText:="item2", rules:=highPriorityRule))
                context.AddItem(CompletionItem.Create(displayText:="Item3", rules:=lowPriorityRule))
                Return Task.CompletedTask
            End Function

            Public Overrides Function ShouldTriggerCompletion(text As SourceText, caretPosition As Integer, trigger As CompletionTrigger, options As OptionSet) As Boolean
                Return True
            End Function
        End Class

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSuggestionModeWithDeletionTrigger(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                           <Document><![CDATA[
using System.Collections.Generic;
using System.Linq;

class C
{
    public static void Baz(List<int> list)
    {
        var xml = 0;
        list.FirstOrDefault(xx$$)
    }
}]]></Document>,
                           showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendBackspace()
                Await state.AssertSelectedCompletionItem("xml", isSoftSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        ' Simulates a situation where IntelliCode provides items not included into the Rolsyn original list.
        ' We want to ignore these items in CommitIfUnique.
        ' This situation should not happen. Tests with this provider were added to cover protective scenarios.
        <ExportCompletionProvider(NameOf(IntelliCodeMockWeirdProvider), LanguageNames.CSharp)>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class IntelliCodeMockWeirdProvider
            Inherits IntelliCodeMockProvider

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
                MyBase.New()
            End Sub

            Public Overrides Async Function ProvideCompletionsAsync(context As CompletionContext) As Task
                Await MyBase.ProvideCompletionsAsync(context).ConfigureAwait(False)
                context.AddItem(CompletionItem.Create(displayText:="★ Length2", filterText:="Length"))
            End Function
        End Class

        <WorkItem(49813, "https://github.com/dotnet/roslyn/issues/49813")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCaseSensitiveMatchWithLowerMatchPriority(showCompletionInArgumentLists As Boolean) As Task
            ' PreselectionProvider will provide an item "★ length" with filter text "length",
            ' which is a case-insentive match to typed text "Length", but with higher match priority.
            ' In this case, we need to make sure the case-sensitive match "Length" is selected.
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
struct Range
{
    public (int Offset, int Length) GetOffsetAndLength(int length) => (0, 0);
}

class Repro
{
    public int Length { get; }

    public void Test(Range x)
    {
        var (offset, length) = x.GetOffsetAndLength(Length$$);
    }
}
                              </Document>,
                              extraExportedTypes:={GetType(PreselectionProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(
                    New OptionKey(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp), True)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll({"★ length", "length", "Length"})
                Await state.AssertSelectedCompletionItem("Length", isHardSelected:=True)
                state.SendEscape()
                Await state.AssertNoCompletionSession()

                state.SendBackspace()
                Await state.AssertCompletionSession()
                Await state.AssertCompletionItemsContainAll({"★ length", "length", "Length"})
                Await state.AssertSelectedCompletionItem("Length", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInListPattern(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class C
{
    const int Constant = 1;
    void M(C c)
    {
        _ = c is$$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars(" [ Co")
                Await state.AssertSelectedCompletionItem(displayText:="Constant", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(", Co")
                Await state.AssertSelectedCompletionItem(displayText:="Constant", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(", ni")
                Await state.AssertSelectedCompletionItem(displayText:="nint", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(", no")
                Await state.AssertSelectedCompletionItem(displayText:="not", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(" 1, va")
                Await state.AssertSelectedCompletionItem(displayText:="var", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(" x ]")
                Await state.AssertNoCompletionSession()
                Assert.Contains("c is [ Constant, Constant, nint, not 1, var x ]", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInSlicePattern(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class RestType
{
    public int IntProperty { get; set; }
}
public class C
{
    public int Length => 0;
    public int this[int i] => 0;
    public RestType Slice(int i, int j) => null;

    void M(C c)
    {
        _ = c is [ $$ ]
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars("..")
                Await state.AssertSelectedCompletionItem(displayText:="async", isHardSelected:=False)

                state.SendTypeChars("{ ")
                Await state.AssertSelectedCompletionItem(displayText:="IntProperty", isHardSelected:=False)

                state.SendTypeChars("IP")
                Await state.AssertSelectedCompletionItem(displayText:="IntProperty", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(": 1")
                Await state.AssertNoCompletionSession()
                Assert.Contains("c is [ ..{ IntProperty: 1 ]", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInSlicePattern_VarKeyword(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class C
{
    public int Length => 0;
    public int this[int i] => 0;
    public C Slice(int i, int j) => null;

    void M(C c)
    {
        _ = c is [$$]
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars(".. va")
                Await state.AssertSelectedCompletionItem(displayText:="var", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(" x")
                Await state.AssertNoCompletionSession()
                Assert.Contains("c is [.. var x]", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInSlicePattern_NullKeyword(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class RestType
{
}
public class C
{
    public int Length => 0;
    public int this[int i] => 0;
    public RestType Slice(int i, int j) => null;

    void M(C c)
    {
        _ = c is [ 0, $$ ]
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars("..")
                Await state.AssertSelectedCompletionItem(displayText:="async", isHardSelected:=False)

                state.SendTypeChars("nu")
                Await state.AssertSelectedCompletionItem(displayText:="null", isHardSelected:=True)

                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("c is [ 0, ..null ]", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInRawStringLiteralInterpolation_SingleLine(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
	void M(int y)
	{
        var s = $"""""{$$}""""";
    }        
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInRawStringLiteralInterpolation_SingleLine_MultiBrace(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
	void M(int y)
	{
        var s = ${|#0:|}$"""""{{$$}}""""";
    }        
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInRawStringLiteralInterpolation_SingleLine_Partial(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
	void M(int y)
	{
        var s = $"""""{$$
    }        
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInRawStringLiteralInterpolation_MultiLine(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
	void M(int y)
	{
        var s = $"""""
        {$$}
        """"";
    }        
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInRawStringLiteralInterpolation_MultiLine_MultiBrace(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
	void M(int y)
	{
        var s = ${|#0:|}$"""""
        {{$$}}
        """"";
    }        
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionInRawStringLiteralInterpolation_MultiLine_Partial(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
	void M(int y)
	{
        var s = $"""""
        {$$
    }        
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.Preview)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
            End Using
        End Function

        ' Simulate the situation that some provider (e.g. IntelliCode) provides items with higher match priority that only match case-insensitively.
        <ExportCompletionProvider(NameOf(PreselectionProvider), LanguageNames.CSharp)>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class PreselectionProvider
            Inherits CommonCompletionProvider

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Friend Overrides ReadOnly Property Language As String
                Get
                    Return LanguageNames.CSharp
                End Get
            End Property

            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                Dim rules = CompletionItemRules.Default.WithSelectionBehavior(CompletionItemSelectionBehavior.HardSelection).WithMatchPriority(MatchPriority.Preselect)
                context.AddItem(CompletionItem.Create(displayText:="★ length", filterText:="length", rules:=rules))
                Return Task.CompletedTask
            End Function

            Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
                Return True
            End Function
        End Class

        <WorkItem(53712, "https://github.com/dotnet/roslyn/issues/53712")>
        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotifyCommittingItemCompletionProvider(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    public void M()
    {
        ItemFromNotifyCommittingItemCompletion$$
    }
}
                              </Document>,
                              extraExportedTypes:={GetType(NotifyCommittingItemCompletionProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim notifyProvider As NotifyCommittingItemCompletionProvider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of NotifyCommittingItemCompletionProvider)().Single()
                notifyProvider.Reset()

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain(NotifyCommittingItemCompletionProvider.DisplayText, "")
                Await state.AssertSelectedCompletionItem(NotifyCommittingItemCompletionProvider.DisplayText, isHardSelected:=True)

                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains(NotifyCommittingItemCompletionProvider.DisplayText, state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)

                Await notifyProvider.Checkpoint.Task
                Assert.False(notifyProvider.CalledOnMainThread)
            End Using
        End Function

        <ExportCompletionProvider(NameOf(NotifyCommittingItemCompletionProvider), LanguageNames.CSharp)>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class NotifyCommittingItemCompletionProvider
            Inherits CommonCompletionProvider
            Implements INotifyCommittingItemCompletionProvider

            Private ReadOnly _threadingContext As IThreadingContext
            Public Const DisplayText As String = "ItemFromNotifyCommittingItemCompletionProvider"

            Public Checkpoint As Checkpoint = New Checkpoint()
            Public CalledOnMainThread As Boolean?

            Public Sub Reset()
                Checkpoint = New Checkpoint()
                CalledOnMainThread = Nothing
            End Sub

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New(threadingContext As IThreadingContext)
                _threadingContext = threadingContext
            End Sub

            Friend Overrides ReadOnly Property Language As String
                Get
                    Return LanguageNames.CSharp
                End Get
            End Property

            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                context.AddItem(CompletionItem.Create(displayText:=DisplayText, filterText:=DisplayText))
                Return Task.CompletedTask
            End Function

            Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
                Return True
            End Function

#Disable Warning IDE0060 ' Remove unused parameter
            Public Function NotifyCommittingItemAsync(document As Document, item As CompletionItem, commitKey As Char?, cancellationToken As CancellationToken) As Task Implements INotifyCommittingItemCompletionProvider.NotifyCommittingItemAsync
#Enable Warning IDE0060 ' Remove unused parameter

                CalledOnMainThread = _threadingContext.HasMainThread AndAlso _threadingContext.JoinableTaskContext.IsOnMainThread

                Checkpoint.Release()
                Return Task.CompletedTask
            End Function
        End Class

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNonBlockingExpandCompletionViaTyping() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  using $$
                              </Document>,
                              extraExportedTypes:={GetType(TestProvider)}.ToList())

                Dim workspace = state.Workspace

                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                globalOptions.SetGlobalOption(New OptionKey(CompletionViewOptions.BlockForCompletionItems, LanguageNames.CSharp), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.TextView.Options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, True)

                Dim completionService = DirectCast(workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of TestProvider)().Single()

                ' completion list shouldn't have expand item until we release the checkpoint
                state.SendTypeChars("TestUnimp")
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")

                Dim session = Await state.GetCompletionSession()
                Dim sessionData = CompletionSessionData.GetOrCreateSessionData(session)
                Dim expandTask = sessionData.ExpandedItemsTask

                Assert.NotNull(expandTask)
                Assert.False(expandTask.IsCompleted)

                ' following up by typing a few more characters each triggers an list update
                state.SendTypeChars("o")
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")
                Assert.False(expandTask.IsCompleted)

                Dim uiRender = state.WaitForUIRenderedAsync()
                state.SendTypeChars("r")
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")
                Assert.False(expandTask.IsCompleted)
                Await uiRender
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)

                provider.Checkpoint.Release()
                Await expandTask

                ' OK, now the expand task is completed,  but we shouldn't have expand item
                ' until a refresh is triggered
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")
                Assert.True(expandTask.IsCompleted)

                uiRender = state.WaitForUIRenderedAsync()
                state.SendTypeChars("t")
                Await state.AssertCompletionItemsContain("TestUnimportedItem", "")
                Await state.AssertSelectedCompletionItem("TestUnimportedItem", inlineDescription:="Test.Name.Spaces")
                Await uiRender
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNonBlockingExpandCompletionViaExpander() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  using $$
                              </Document>,
                              extraExportedTypes:={GetType(TestProvider)}.ToList())

                Dim workspace = state.Workspace

                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                globalOptions.SetGlobalOption(New OptionKey(CompletionViewOptions.BlockForCompletionItems, LanguageNames.CSharp), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)

                state.TextView.Options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, True)

                Dim completionService = DirectCast(workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of TestProvider)().Single()

                ' completion list shouldn't have expand item until we release the checkpoint
                state.SendTypeChars("TestUnimp")
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")

                Dim session = Await state.GetCompletionSession()
                Dim sessionData = CompletionSessionData.GetOrCreateSessionData(session)
                Dim expandTask = sessionData.ExpandedItemsTask

                Assert.NotNull(expandTask)
                Assert.False(expandTask.IsCompleted)

                ' following up by typing more characters each triggers an list update
                Dim uiRender = state.WaitForUIRenderedAsync()
                state.SendTypeChars("o")
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")
                Await uiRender
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)
                Assert.False(expandTask.IsCompleted)

                provider.Checkpoint.Release()
                Await expandTask

                ' OK, now the expand task is completed,  but we shouldn't have expand item
                ' until a refresh is triggered
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")
                Assert.True(expandTask.IsCompleted)

                ' trigger update by using expander
                uiRender = state.WaitForUIRenderedAsync()
                state.SetCompletionItemExpanderState(isSelected:=True)
                Await state.WaitForAsynchronousOperationsAsync()
                Await uiRender

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                Await state.AssertCompletionItemsContain("TestUnimportedItem", "")
                Await state.AssertSelectedCompletionItem("TestUnimportedItem", inlineDescription:="Test.Name.Spaces")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNonBlockingExpandCompletionDoesNotChangeItemOrder() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  $$
                              </Document>,
                              extraExportedTypes:={GetType(TestProvider)}.ToList())

                Dim workspace = state.Workspace

                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                globalOptions.SetGlobalOption(New OptionKey(CompletionViewOptions.BlockForCompletionItems, LanguageNames.CSharp), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), True)
                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation), True)

                state.TextView.Options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, True)

                Dim completionService = DirectCast(workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)(), CompletionServiceWithProviders)
                Dim provider = completionService.GetTestAccessor().GetAllProviders(ImmutableHashSet(Of String).Empty).OfType(Of TestProvider)().Single()

                ' First we enable delay for expand item, and trigger completion with test provider blocked
                ' this would ensure completion list don't have expand item until we release the checkpoint
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")

                Dim session = Await state.GetCompletionSession()
                Dim sessionData = CompletionSessionData.GetOrCreateSessionData(session)
                Dim expandTask = sessionData.ExpandedItemsTask

                Assert.NotNull(expandTask)
                Assert.False(expandTask.IsCompleted)

                provider.Checkpoint.Release()
                Await expandTask

                ' Now delayed expand item task is completed, following up by typing and delete a character to trigger
                ' update so the list would contains all items
                Dim uiRender = state.WaitForUIRenderedAsync()
                state.SendTypeChars("t")
                Await state.AssertCompletionItemsContain("TestUnimportedItem", "")
                state.SendBackspace()
                Await uiRender
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)

                ' Get the full list from session where delay happened
                Dim list1 = state.GetCompletionItems()

                state.SendEscape()
                Await state.AssertNoCompletionSession()

                ' Now disable expand item delay, so intial trigger should contain full list
                state.TextView.Options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, False)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("TestUnimportedItem", "")

                ' Get the full list from session where delay didn't happen
                Dim list2 = state.GetCompletionItems()
                Assert.Equal(list1.Count, list2.Count)

                ' Two list of items should be identical in order.
                For i As Integer = 0 To list1.Count - 1
                    Dim item1 = list1(i)
                    Dim item2 = list2(i)
                    Assert.Equal(item1, item2)
                Next

            End Using
        End Function

        <ExportCompletionProvider(NameOf(TestProvider), LanguageNames.CSharp)>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class TestProvider
            Inherits CommonCompletionProvider

            Public Checkpoint As Checkpoint = New Checkpoint()

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Overrides Async Function ProvideCompletionsAsync(context As CompletionContext) As Task
                Await checkpoint.Task.ConfigureAwait(False)
                Dim item = ImportCompletionItem.Create("TestUnimportedItem", 0, "Test.Name.Spaces", Glyph.ClassPublic, "", CompletionItemFlags.CachedAndExpanded, Nothing)
                context.AddItem(item)
            End Function

            Public Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As CompletionOptions) As Boolean
                Return True
            End Function

            Friend Overrides ReadOnly Property IsExpandItemProvider As Boolean
                Get
                    Return True
                End Get
            End Property

            Friend Overrides ReadOnly Property Language As String
                Get
                    Return LanguageNames.CSharp
                End Get
            End Property
        End Class
    End Class
End Namespace
