' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.CSharp.ExternalAccess.Pythia.Api
Imports Microsoft.CodeAnalysis.CSharp.Formatting
Imports Microsoft.CodeAnalysis.CSharp.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Tagging
Imports Microsoft.CodeAnalysis.Tags
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Text.Projection
Imports Roslyn.Test.Utilities.TestGenerators

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public NotInheritable Class CSharpCompletionCommandHandlerTests
        <WpfTheory, CombinatorialData>
        Public Async Function CompletionOnFileType_SameFile_NonQualified(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
namespace NS
{
    file class FC { }

    class C
    {
        public static void M()
        {
            var x = new $$
        }
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("F")
                Await state.AssertSelectedCompletionItem(displayText:="FC", isHardSelected:=True)

                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("var x = new FC", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function CompletionOnFileType_SameFile_NamespaceQualified(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
namespace NS
{
    file class FC { }

    class C
    {
        public static void M()
        {
            var x = new NS.$$
        }
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("F")
                Await state.AssertSelectedCompletionItem(displayText:="FC", isHardSelected:=True)

                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("var x = new NS.FC", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function CompletionOnFileType_DifferentFile_NonQualified(showCompletionInArgumentLists As Boolean) As Task
            Using State = New TestState(<Workspace>
                                            <Project Language="C#" CommonReferences="true" LanguageVersion=<%= LanguageVersion.CSharp12.ToDisplayString() %>>
                                                <Document FilePath="a.cs">
namespace NS
{
    file class FC { }
}
                                                </Document>
                                                <Document FilePath="b.cs">
namespace NS
{
    class C
    {
        public static void M()
        {
            var x = new $$
        }
    }
}
                                                </Document>
                                            </Project>
                                        </Workspace>,
                                 excludedTypes:=Nothing, extraExportedTypes:=Nothing,
                                 includeFormatCommandHandler:=False, workspaceKind:=Nothing)

                State.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists)

                State.SendTypeChars("F")
                Await State.AssertCompletionItemsDoNotContainAny("FC")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function CompletionOnFileType_DifferentFile_NamespaceQualified(showCompletionInArgumentLists As Boolean) As Task
            Using State = New TestState(<Workspace>
                                            <Project Language="C#" CommonReferences="true" LanguageVersion=<%= LanguageVersion.CSharp12.ToDisplayString() %>>
                                                <Document FilePath="a.cs">
namespace NS
{
    file class FC { }
}
                                                </Document>
                                                <Document FilePath="b.cs">
namespace NS
{
    class C
    {
        public static void M()
        {
            var x = new NS.$$
        }
    }
}
                                                </Document>
                                            </Project>
                                        </Workspace>,
                                 excludedTypes:=Nothing, extraExportedTypes:=Nothing,
                                 includeFormatCommandHandler:=False, workspaceKind:=Nothing)

                State.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerInArgumentLists, LanguageNames.CSharp, showCompletionInArgumentLists)

                State.SendTypeChars("F")
                Await State.AssertCompletionItemsDoNotContainAny("FC")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                ' This is the expected behavior until we implement support for list-patterns.
                state.SendTypeChars("CP")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars(".")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("IP")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                ' No second completion since already tested at top-level
                state.SendTypeChars(", ")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("CP")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

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

        <WpfTheory, CombinatorialData>
        Public Async Function CompletionOnClassBaseType(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class Base(int Alice, int Bob);
class Derived(int Other) : [|Base$$|]
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/46397")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/44921")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/44921")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/47430")>
        <WpfTheory, CombinatorialData>
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
                              showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

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

        <WorkItem("https://github.com/dotnet/roslyn/issues/65482")>
        <WpfTheory, CombinatorialData>
        Public Async Function RequiredMembersDoNotHardSelect(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
struct A
{
    public required int F1 { get; init; }
    public int F2 { get; init; }
}

class D
{
    void goo()
    {
        A a = new A $$
    }
}
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp11)

                state.SendTypeChars("{ ")
                Await state.AssertSelectedCompletionItem(displayText:="F1", isHardSelected:=False)
                state.SendReturn()
                Await state.AssertNoCompletionSession()
                Assert.Contains("new A" & vbCrLf & "{" & vbCrLf, state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44921")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541201")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/35236")>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/44459")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/44459")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/71851"), CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

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

        <WpfTheory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/37315")>
        Public Async Function TestTypingDotBeforeExistingDot2(showCompletionInArgumentLists As Boolean) As Task
            ' Starting C# 8.0 two dots are considered as a DotDotToken of a Range expression.
            ' However, typing dot before a single dot (and adding the second one) should lead to a completion
            ' in the context of the previous token if this completion exists.
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    public List&lt;int&gt; X;
}

class D
{
    public List&lt;int&gt; X;
}

class E
{
    public static bool F(object obj) => obj switch
    {
        C c => c.X,
        D d => d.X,
        _ => throw null
    }$$.Any(i => i == 0);
}
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/37315")>
        Public Async Function TestTypingDotBeforeExistingDot3(showCompletionInArgumentLists As Boolean) As Task
            ' Starting C# 8.0 two dots are considered as a DotDotToken of a Range expression.
            ' However, typing dot before a single dot (and adding the second one) should lead to a completion
            ' in the context of the previous token if this completion exists.
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;

class E
{
    public void F(object o)
    {
        var v = (int)o$$.AddDays(1);
    }
}
                              </Document>,
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(".")
                Await state.AssertCompletionItemsContain("ToString", displayTextSuffix:="")
                Await state.AssertCompletionItemsDoNotContainAny("CompareTo")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.EnterKeyBehavior, LanguageNames.CSharp, EnterKeyRule.AfterFullyTypedWord)

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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.EnterKeyBehavior, LanguageNames.CSharp, EnterKeyRule.AfterFullyTypedWord)

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
        <WorkItem("https://github.com/dotnet/roslyn/issues/22342")>
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
                Await state.AssertSelectedCompletionItem(displayText:="Variable", isHardSelected:=False)

                state.SendTypeChars("x, vari")
                Await state.AssertSelectedCompletionItem(displayText:="Variable", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("(Variable x, Variable ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertSelectedCompletionItem(displayText:="Variable", isHardSelected:=False)
                Await state.AssertCompletionItemsContainAll("variable")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

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
        <WorkItem("https://github.com/dotnet/roslyn/issues/17256")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/17256")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/4677")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/4677")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/4677")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/4677")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/4677")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/24432")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/24432")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/24432")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/24432")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/24432")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/24432")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/24432")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/24432")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/24432")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/13527")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/13527")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/19335")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/19335")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/19335")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/19335")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/19335")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/19335")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/13527")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/13527")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/13527")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/13527")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/13527")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/13527")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/13527")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/13527")>
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
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543268")>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543519")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543559")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543561")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543771")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543771")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544293")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544017")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/479078")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/8320")>
        Public Sub EnumParamsCompletion(showCompletionInArgumentLists As Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
using System;

class C
{
    void X(params DayOfWeek[] x)
    {
        X($$);
    }
}
                </Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.AssertSelectedCompletionItem("DayOfWeek", isHardSelected:=True)
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        Public Async Function EnumCompletionNotTriggeredOnPlusCommitCharacter(showCompletionInArgumentLists As Boolean) As Task
            Await EnumCompletionNotTriggeredOn("+"c, showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function EnumCompletionNotTriggeredOnLeftBraceCommitCharacter(showCompletionInArgumentLists As Boolean) As Task
            Await EnumCompletionNotTriggeredOn("{"c, showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function EnumCompletionNotTriggeredOnSpaceCommitCharacter(showCompletionInArgumentLists As Boolean) As Task
            Await EnumCompletionNotTriggeredOn(" "c, showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/pull/49632")>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/77606")>
        Public Async Function CompletionEnumTypeAndValues_Escaped() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
namespace A
{
    public enum Colors
    {
        @int,
        @string
    }
}
namespace B
{
    class Program
    {
        static void Main(A.Colors c)
        {
            switch (c)
            {
                case $$
        }
    }
}                              </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain(Function(i) i.DisplayText = "A.Colors" AndAlso i.FilterText = "Colors")
                Await state.AssertCompletionItemsContain(Function(i) i.DisplayText = "A.Colors.@int" AndAlso i.FilterText = "A.Colors.@int")
                Await state.AssertCompletionItemsContain(Function(i) i.DisplayText = "A.Colors.@string" AndAlso i.FilterText = "A.Colors.@string")
                Await state.AssertSelectedCompletionItem("A.Colors", isHardSelected:=True)
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/pull/49632")>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SelectEnumMemberAdditionalFilterTextMatchOverInferiorFilterTextMatch() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
public enum Colors
{
    Red,
    Green
}

class Program
{
    Colors GreenNode { get; }                           
    void M()
    {
        Colors c = Green$$
    }
}                               </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("Colors.Green", "GreenNode")
                ' select full match "Colors.Green" over prefix match "GreenNode"
                Await state.AssertSelectedCompletionItem("Colors.Green", isHardSelected:=True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DoNotSelectEnumMemberAdditionalFilterTextMatchOverEqualFilterTextMatch() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
public enum Colors
{
    Red,
    Green
}

class Program
{            
    Colors Green { get; }               
    void M()
    {
        Colors c = gree$$
    }
}                               </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("Colors.Green", "Green")
                ' Select FilterText match "Green" over AdditionalFilterText match "Colors.Green"
                Await state.AssertSelectedCompletionItem("Green", isHardSelected:=True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SelectStaticMemberAdditionalFilterTextMatchOverInferiorFilterTextMatch() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
public class MyArray
{
    public static MyArray Empty { get; }
}

class Program
{         
    string EmptyString = "";                 
    void M()
    {                       
        MyArray c = Empty$$
    }
}                               </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("MyArray.Empty", "EmptyString")
                ' select full match "MyArray.Empty" over prefix match "EmptyString"
                Await state.AssertSelectedCompletionItem("MyArray.Empty", isHardSelected:=True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SelectCompletionListStaticMemberAdditionalFilterTextMatchOverInferiorFilterTextMatch() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
namespace NS
{

    /// <completionlist cref="TypeContainer"/>
    public class SomeType
    { }

    public static class TypeContainer
    {
        public static SomeType Foo1 = new SomeType();
        public static Program Foo2 = new Program();
    }

    public class Program
    {
        void Goo()
        {
            var myFoo = true;
            SomeType c = $$
        }
    }
}                             ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("myFoo", "TypeContainer", "TypeContainer.Foo1", "TypeContainer.Foo2")

                state.SendTypeChars("foo")
                Await state.AssertSelectedCompletionItem("TypeContainer.Foo1", isHardSelected:=True)
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/pull/49632")>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544296")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543687")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544925")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545070")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545590")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/69153")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestOverrideWithClassWithTrailingSemicolon(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[class Class1
{
    override tostring$$
};

class Class2
{

};]]></Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("class Class1
{
    public override string ToString()
    {
        return base.ToString();
    }
};

class Class2
{

};", state.SubjectBuffer.CurrentSnapshot.GetText())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545664")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545967")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546561")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546403")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546403")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546917")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547016")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530835")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/5487")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/622957")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/657658")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/672474")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/672474")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/737239")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/785637")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775370")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865089")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/852578")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/868286")>
        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("M")
                Await state.AssertSelectedCompletionItem("M")
                Assert.False(state.GetSelectedItem().Tags.Contains(WellKnownTags.Warning))
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/951726")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/930254")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/839555")>
        <WpfTheory, CombinatorialData>
        Public Async Function TriggeredOnHash(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
$$]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars("#")
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015893")>
        <WpfTheory, CombinatorialData>
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

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim slowProvider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of SlowProvider)().Single()
                slowProvider.checkpoint.Release()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/31135")>
        <WpfTheory, CombinatorialData>
        Public Async Function TypingWithoutMatchAfterBackspaceDismissesCompletion(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class$$ C
{
}]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

                state.SendBackspace()
                Await state.AssertCompletionSession()
                state.SendTypeChars("w")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(36515, "https://github.com/dotnet/roslyn/issues/36513")>
        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

                state.SendBackspace()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("structure")
                state.SendTypeChars(".")
                Await state.AssertCompletionItemsContainAll("A")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/1594")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/1594")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/1659")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/588")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/4978")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/588")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/588")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/29938")>
        <WpfTheory, CombinatorialData>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/12254")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/12530")>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/13481")>
        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

                For Each c In "Offset"
                    state.SendBackspace()
                    Await state.WaitForAsynchronousOperationsAsync()
                Next

                Await state.AssertSelectedCompletionItem("DateTime")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/13481")>
        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

                For Each c In "Offset."
                    state.SendBackspace()
                    Await state.WaitForAsynchronousOperationsAsync()
                Next

                Await state.AssertSelectedCompletionItem("DateTime")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/14465")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/14085")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/14085")>
        <WpfTheory, CombinatorialData>
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
        Public Async Function TestNoBlockOnCompletionItems1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  using $$
                              </Document>,
                              extraExportedTypes:={GetType(BooleanTaskControlledCompletionProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of BooleanTaskControlledCompletionProvider)().Single()

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionViewOptionsStorage.BlockForCompletionItems, LanguageNames.CSharp, False)

                state.SendTypeChars("Sys.")
                Await state.AssertNoCompletionSession()
                Assert.Contains("Sys.", state.GetLineTextFromCaretPosition())

                provider.completionSource.SetResult(True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNoBlockOnCompletionItems2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  using $$
                              </Document>,
                              extraExportedTypes:={GetType(CompletedTaskControlledCompletionProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionViewOptionsStorage.BlockForCompletionItems, LanguageNames.CSharp, False)

                state.SendTypeChars("Sys")
                Await state.AssertSelectedCompletionItem(displayText:="System")
                state.SendTypeChars(".")
                Assert.Contains("System.", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of BooleanTaskControlledCompletionProvider)().Single()

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionViewOptionsStorage.BlockForCompletionItems, LanguageNames.CSharp, False)

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

                state.SendTypeChars("Sys")

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

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of BooleanTaskControlledCompletionProvider)().Single()

                Dim globalOptions = state.Workspace.GetService(Of IGlobalOptionService)
                globalOptions.SetGlobalOption(CompletionViewOptionsStorage.BlockForCompletionItems, LanguageNames.CSharp, False)

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

                state.SendTypeChars("Sys")

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
        Public Async Function TestSwitchBetweenBlockingAndNoBlockOnCompletion(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  using $$
                              </Document>,
                              extraExportedTypes:={GetType(BooleanTaskControlledCompletionProvider)}.ToList(),
                              showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim globalOptions = state.Workspace.GetService(Of IGlobalOptionService)
                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of BooleanTaskControlledCompletionProvider)().Single()

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
                globalOptions.SetGlobalOption(CompletionViewOptionsStorage.BlockForCompletionItems, LanguageNames.CSharp, False)

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
                globalOptions.SetGlobalOption(CompletionViewOptionsStorage.BlockForCompletionItems, LanguageNames.CSharp, True)

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

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Dim oldFilters = state.GetCompletionItemFilters()
                Dim newFilters = ArrayBuilder(Of Data.CompletionFilterWithState).GetInstance()
                For Each f In oldFilters
                    Assert.NotEqual(FilterSet.InterfaceFilter.DisplayText, f.Filter.DisplayText)
                    newFilters.Add(f.WithSelected(False))
                Next

                newFilters.Add(New Data.CompletionFilterWithState(FilterSet.InterfaceFilter, isAvailable:=True, isSelected:=True))

                Await state.RaiseFiltersChangedAndWaitForUiRenderAsync(newFilters.ToImmutableAndFree())
                Assert.Null(state.GetSelectedItem())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Dim oldFilters = state.GetCompletionItemFilters()
                Dim newFilters = ArrayBuilder(Of Data.CompletionFilterWithState).GetInstance()
                For Each f In oldFilters
                    Assert.NotEqual(FilterSet.InterfaceFilter.DisplayText, f.Filter.DisplayText)
                    newFilters.Add(f.WithSelected(False))
                Next

                newFilters.Add(New Data.CompletionFilterWithState(FilterSet.InterfaceFilter, isAvailable:=True, isSelected:=True))

                Await state.RaiseFiltersChangedAndWaitForUiRenderAsync(newFilters.ToImmutableAndFree())
                Assert.Null(state.GetSelectedItem())
                state.SendTab()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Dim oldFilters = state.GetCompletionItemFilters()
                Dim newFilters = ArrayBuilder(Of Data.CompletionFilterWithState).GetInstance()
                For Each f In oldFilters
                    Assert.NotEqual(FilterSet.InterfaceFilter.DisplayText, f.Filter.DisplayText)
                    newFilters.Add(f.WithSelected(False))
                Next

                newFilters.Add(New Data.CompletionFilterWithState(FilterSet.InterfaceFilter, isAvailable:=True, isSelected:=True))

                Await state.RaiseFiltersChangedAndWaitForUiRenderAsync(newFilters.ToImmutableAndFree())
                Assert.Null(state.GetSelectedItem())
                state.SendReturn()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Dim oldFilters = state.GetCompletionItemFilters()
                Dim newFilters = ArrayBuilder(Of Data.CompletionFilterWithState).GetInstance()
                For Each f In oldFilters
                    Assert.NotEqual(FilterSet.InterfaceFilter.DisplayText, f.Filter.DisplayText)
                    newFilters.Add(f.WithSelected(False))
                Next

                newFilters.Add(New Data.CompletionFilterWithState(FilterSet.InterfaceFilter, isAvailable:=True, isSelected:=True))

                Await state.RaiseFiltersChangedAndWaitForUiRenderAsync(newFilters.ToImmutableAndFree())
                Assert.Null(state.GetSelectedItem())
                state.SendTypeChars(".")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/15881")>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/14704")>
        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="Environment", isHardSelected:=True)
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/16236")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=362890")>
        <WpfTheory, CombinatorialData>
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

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Await state.AssertSelectedCompletionItem("Red")
                Await state.AssertCompletionItemsContainAll("Red", "Green", "Blue", "Equals")

                Dim oldFilters = state.GetCompletionItemFilters()
                Dim newFiltersBuilder = ArrayBuilder(Of Data.CompletionFilterWithState).GetInstance()
                For Each f In oldFilters
                    newFiltersBuilder.Add(f.WithSelected(f.Filter.DisplayText = FilterSet.ConstantFilter.DisplayText))
                Next

                Await state.RaiseFiltersChangedAndWaitForUiRenderAsync(newFiltersBuilder.ToImmutableAndFree())

                Await state.AssertSelectedCompletionItem("Red")
                Await state.AssertCompletionItemsContainAll("Red", "Green", "Blue")
                Await state.AssertCompletionItemsDoNotContainAny("Equals")

                oldFilters = state.GetCompletionItemFilters()
                newFiltersBuilder = ArrayBuilder(Of Data.CompletionFilterWithState).GetInstance()
                For Each f In oldFilters
                    newFiltersBuilder.Add(f.WithSelected(False))
                Next

                Await state.RaiseFiltersChangedAndWaitForUiRenderAsync(newFiltersBuilder.ToImmutableAndFree())

                Await state.AssertSelectedCompletionItem("Red")
                Await state.AssertCompletionItemsContainAll({"Red", "Green", "Blue", "Equals"})
            End Using
        End Function

        <WpfFact>
        Public Async Function TestEnumMemberFilter() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[

public enum Color
{
    Red,
    Green,
    Blue
}

class C
{
    void M()
    {
        Color x = $$
    }
}
            ]]></Document>)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Await state.AssertCompletionItemsContainAll("Color.Red", "Color.Green", "Color.Blue", "Color")

                Dim oldFilters = state.GetCompletionItemFilters()
                Dim newFiltersBuilder = ArrayBuilder(Of Data.CompletionFilterWithState).GetInstance()

                ' ensure both Enum and EnumMembers filter present, and then select EnumMember filter
                Dim hasEnumerFilter = False, hasEnumMemberFilter = False
                For Each oldState In oldFilters

                    If Object.ReferenceEquals(oldState.Filter, FilterSet.EnumMemberFilter) Then
                        hasEnumMemberFilter = True
                        newFiltersBuilder.Add(oldState.WithSelected(True))
                        Continue For
                    End If

                    If Object.ReferenceEquals(oldState.Filter, FilterSet.EnumFilter) Then
                        hasEnumerFilter = True
                    End If

                    newFiltersBuilder.Add(oldState.WithSelected(False))
                Next

                Assert.True(hasEnumerFilter And hasEnumMemberFilter)

                Await state.RaiseFiltersChangedAndWaitForUiRenderAsync(newFiltersBuilder.ToImmutableAndFree())

                Await state.AssertCompletionItemsContainAll("Color.Red", "Color.Green", "Color.Blue")
                Await state.AssertCompletionItemsDoNotContainAny("Color")
            End Using
        End Function

        <WpfFact>
        Public Async Function TestEnumMembersMatchTargetType() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[

public enum Color
{
    Red,
    Green,
    Blue
}

class C
{
    void M()
    {
        Color x = $$
    }
}
            ]]></Document>)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Await state.AssertCompletionItemsContainAll("Color.Red", "Color.Green", "Color.Blue", "Color")

                Dim oldFilters = state.GetCompletionItemFilters()
                Dim newFiltersBuilder = ArrayBuilder(Of Data.CompletionFilterWithState).GetInstance()

                Dim hasTargetTypedFilter = False
                For Each oldState In oldFilters

                    If Object.ReferenceEquals(oldState.Filter, FilterSet.TargetTypedFilter) Then
                        hasTargetTypedFilter = True
                        newFiltersBuilder.Add(oldState.WithSelected(True))
                        Continue For
                    End If

                    newFiltersBuilder.Add(oldState.WithSelected(False))
                Next

                Assert.True(hasTargetTypedFilter)

                Await state.RaiseFiltersChangedAndWaitForUiRenderAsync(newFiltersBuilder.ToImmutableAndFree())

                Await state.AssertCompletionItemsContainAll("Color.Red", "Color.Green", "Color.Blue")
                Await state.AssertCompletionItemsDoNotContainAny("Color")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/16236")>
        <WpfTheory, CombinatorialData>
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

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of MultipleChangeCompletionProvider)().Single()

                Dim testDocument = state.Workspace.Documents(0)
                Dim textBuffer = testDocument.GetTextBuffer()

                Dim snapshotBeforeCommit = textBuffer.CurrentSnapshot
                provider.SetInfo(testDocument.CursorPosition.Value)

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

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of MultipleChangeCompletionProvider)().Single()

                Dim testDocument = state.Workspace.Documents(0)
                Dim textBuffer = testDocument.GetTextBuffer()

                Dim snapshotBeforeCommit = textBuffer.CurrentSnapshot
                provider.SetInfo(testDocument.CursorPosition.Value)

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

        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=296512")>
        <WpfTheory, CombinatorialData>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/49861")>
        Public Async Function PathDirective(directive As String) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    #   <%= directive %>  $$
}
                              </Document>)

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

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
        Public Async Function AfterIdentifierInCaseLabel1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void M()
    {
        switch (true)
        {
            case Identifier $$
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
        Public Async Function AfterIdentifierInCaseLabel2(showCompletionInArgumentLists As Boolean) As Task
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

                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionItemsContain(displayText:="identifier", displayTextSuffix:="")
                state.AssertSuggestedItemSelected(displayText:="z")

                state.SendBackspace()
                state.SendTypeChars("i")
                Await state.AssertSelectedCompletionItem(displayText:="identifier", isHardSelected:=False)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/11959")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/15348")>
        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

                state.SendInvokeCompletionList()
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="aaa", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

                state.SendInvokeCompletionList()
                state.SelectAndMoveCaret(-6)
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="Write", isHardSelected:=True)
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/30097")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/36546")>
        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

                state.SendBackspace()
                Await state.AssertCompletionItemsContainAll("WriteLine")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/36546")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/30097")>
        <WpfTheory, CombinatorialData>
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

                state.SendTypeChars("xyzz")
                Await state.AssertCompletionSession()
                state.AssertSuggestedItemSelected(displayText:="sxyzz")
                Await state.AssertSelectedCompletionItem(displayText:="sxyzz", isSoftSelected:=True)

                state.SendBackspace()
                Await state.AssertCompletionSession()
                state.AssertSuggestedItemSelected(displayText:="sxyz")
                Await state.AssertSelectedCompletionItem(displayText:="sxyz", isSoftSelected:=True)

                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                Await state.AssertCompletionSession()
                Assert.True(state.HasSuggestedItem())
                Await state.AssertSelectedCompletionItem(displayText:="sbyte", isSoftSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

                state.SendBackspace()
                Await state.AssertCompletionSession()
                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Normalize", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/37231")>
        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

                state.SendBackspace()
                Await state.AssertCompletionSession()
                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("AccessViolationException", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockProvider)().Single()

                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Length", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockProvider)().Single()

                state.SendTypeChars(".len")
                Await state.AssertCompletionItemsContainAll("Length", "★ Length")
                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Length", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockProvider)().Single()

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

                state.SendBackspace()
                Await state.AssertCompletionItemsContainAll("Normalize", "★ Normalize")
                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Normalize", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockProvider)().Single()

                state.SendInvokeCompletionList()
                state.SendSelectCompletionItem("★ Length")
                Await state.AssertSelectedCompletionItem(displayText:="★ Length", automationText:=provider.AutomationTextString)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockWeirdProvider)().Single()

                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Length", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockWeirdProvider)().Single()

                state.SendTypeChars(".len")
                Await state.AssertCompletionItemsContainAll("Length", "★ Length", "★ Length2")
                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("s.Length", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockProvider)().Single()

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

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

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of IntelliCodeMockProvider)().Single()

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

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
            Un$$
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

                Dim completionService = document.GetLanguageService(Of CompletionService)()
                completionService.GetTestAccessor().SuppressPartialSemantics()
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Dim service = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of ITypeImportCompletionService)()

                service.QueueCacheWarmUpTask(document.Project)
                Await state.WaitForAsynchronousOperationsAsync()

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Await state.AssertCompletionItemsContain(displayText:="UnimportedType", displayTextSuffix:="")

                service.QueueCacheWarmUpTask(document.Project)
                Await state.WaitForAsynchronousOperationsAsync()
            End Using
        End Function

        <WpfFact>
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
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Dim completionService = document.GetLanguageService(Of CompletionService)()
                completionService.GetTestAccessor().SuppressPartialSemantics()

                Await ExtensionMethodImportCompletionHelper.WarmUpCacheAsync(document.Project, CancellationToken.None)
                Await state.WaitForAsynchronousOperationsAsync()

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Await state.AssertCompletionItemsContain(displayText:="IntegerExtMethod", displayTextSuffix:="")

                ' Make sure any background work would be completed.
                Await ExtensionMethodImportCompletionHelper.WarmUpCacheAsync(document.Project, CancellationToken.None)
                Await state.WaitForAsynchronousOperationsAsync()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)

                ' trigger completion with import completion disabled
                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                ' make sure expander is selected
                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

                Await state.AssertSelectedCompletionItem(displayText:="Bar", inlineDescription:="NS2")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)

                ' unselect expander
                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=False)

                Await state.AssertCompletionItemsDoNotContainAny("Bar")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)

                ' select expander again
                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

                Await state.AssertSelectedCompletionItem(displayText:="Bar", inlineDescription:="NS2")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)

                ' dismiss completion
                state.SendEscape()
                Await state.AssertNoCompletionSession()

                ' trigger completion again
                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                ' should not show unimported item by default
                Await state.AssertCompletionItemsDoNotContainAny({"Bar"})
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)

            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                ' trigger completion with import completion enabled
                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                ' make sure expander is selected
                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

                Await state.AssertSelectedCompletionItem(displayText:="Bar", inlineDescription:="NS2")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)

                ' dismiss completion
                state.SendEscape()
                Await state.AssertNoCompletionSession()

                ' trigger completion again
                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                ' show expanded items by default
                Await state.AssertSelectedCompletionItem(displayText:="Bar", inlineDescription:="NS2")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)

            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function ExpanderAvailableWhenNotInTypeContextButNotAddingAnyItems(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
namespace NS1
{
    $$
}
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                ' trigger completion with import completion enabled
                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)
                Dim length = state.GetCompletionItems().Count

                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                Assert.Equal(length, state.GetCompletionItems().Count)
            End Using
        End Function

        <WpfFact>
        Public Async Function ExpandedItemsShouldNotShowInExclusiveContext() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
namespace CC
{
    public class DD
    {
    }
}
public class AA
{
    public AA DDProp1 { get; set; }

    private static void A()
    {
        AA a = new()
            {$$
            };
    }
}</Document>)

                state.TextView.Options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)
                Await state.AssertCompletionItemsContain("DDProp1", "")
                Await state.AssertCompletionItemsDoNotContainAny("DD")

                Dim session = Await state.GetCompletionSession()
                Dim sessionData = CompletionSessionData.GetOrCreateSessionData(session)
                Assert.Null(sessionData.ExpandedItemsTask)

                Await state.SendTypeCharsAndWaitForUiRenderAsync("D")

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)
                Await state.AssertCompletionItemsContain("DDProp1", "")
                Await state.AssertCompletionItemsDoNotContainAny("DD")
            End Using
        End Function

        <WpfFact>
        Public Async Function ExpandedItemsShouldNotShowViaExpanderInExclusiveContext() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
namespace CC
{
    public class DD
    {
    }
}
public class AA
{
    public AA Prop1 { get; set; }
    public int Prop2 { get; set; }

    private static void A()
    {
        AA a = new()
            {$$
            };
    }
}</Document>)

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                ' import completion is disabled, so we shouldn't have expander selected by default
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)
                Await state.AssertCompletionItemsContain("Prop1", "")
                Await state.AssertCompletionItemsDoNotContainAny("DD")

                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

                ' since we are in exclusive context (property name provider is exclusive in this case), selceting expander is a no-op
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                Await state.AssertCompletionItemsContain("Prop1", "")
                Await state.AssertCompletionItemsDoNotContainAny("DD")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/34943")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/36187")>
        <WpfTheory, CombinatorialData>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/24960")>
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
        <WorkItem("https://github.com/dotnet/roslyn/issues/24960")>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/35163")>
        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                Await state.AssertSelectedCompletionItem(displayText:="Bar", displayTextSuffix:="<>")

                state.SendTab()
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/35163")>
        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                Await state.AssertSelectedCompletionItem(displayText:="Bar")

                state.SendTab()
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/35163")>
        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                Await state.AssertSelectedCompletionItem(displayText:="ABar")

                state.SendTab()
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/38253")>
        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                Await state.AssertSelectedCompletionItem(displayText:="Bar", inlineDescription:="")
                state.SendTab()
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/38253")>
        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Await state.AssertSelectedCompletionItem(displayText:="Bar", inlineDescription:="NS2")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)

                state.SendTab()
                Assert.Equal(expectedText, state.GetDocumentText())
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Await state.AssertSelectedCompletionItem(displayText:="designer")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/38253")>
        <WpfTheory, CombinatorialData>
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

        <WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/67081")>
        <InlineData("System", True)>
        <InlineData("System.Collections", True)>
        <InlineData("SystemNamespace", False)>
        <InlineData("MyNamespace1", True)>
        <InlineData("MyNamespace3", False)>
        Public Async Function SortUnimportedItemFromSystemNamespacesFirst(containingNamespace As String, sortedAhead As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
namespace NS1
{
    class C
    {
        void M()
        {
            unimportedtype$$
        }
    }
}

namespace MyNamespace2
{
    public class UnimportedType { }
}

namespace  <%= containingNamespace %>
{
    public class UnimportedType { }
}
</Document>)

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendCommitUniqueCompletionListItemAsync()

                ' make sure expander is selected
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)

                Dim expectedOrder As (String, String)()

                If sortedAhead Then
                    Await state.AssertSelectedCompletionItem(displayText:="UnimportedType", inlineDescription:=containingNamespace)
                    expectedOrder =
                    {
                        ("UnimportedType", containingNamespace),
                        ("UnimportedType", "MyNamespace2")
                    }
                Else
                    Await state.AssertSelectedCompletionItem(displayText:="UnimportedType", inlineDescription:="MyNamespace2")
                    expectedOrder =
                    {
                        ("UnimportedType", "MyNamespace2"),
                        ("UnimportedType", containingNamespace)
                    }
                End If

                state.AssertItemsInOrder(expectedOrder)

            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/41601")>
        <WpfTheory, CombinatorialData>
        Public Async Function SortItemsByExpandedFlag(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
namespace NS1
{
    class C
    {
        void M()
        {
            mytask$$
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendCommitUniqueCompletionListItemAsync()

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

        <WorkItem("https://github.com/dotnet/roslyn/issues/39519")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestSuggestedNamesDoNotStartWithDigit_DigitsInTheMiddle(showCompletionInArgumentLists As Boolean) As Task
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowNameSuggestions, LanguageNames.CSharp, True)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("foo123Bar", "foo123", "foo", "bar")
                Await state.AssertCompletionItemsDoNotContainAny("123Bar")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/39519")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestSuggestedNamesDoNotStartWithDigit_DigitsOnTheRight(showCompletionInArgumentLists As Boolean) As Task
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowNameSuggestions, LanguageNames.CSharp, True)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("foo123", "foo")
                Await state.AssertCompletionItemsDoNotContainAny("123")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/38289")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestShowCompletionsWhenTypingCompilerDirective_SingleDirectiveWord(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
#nullable$$
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync()

                Await state.AssertCompletionItemsContainAll("disable", "enable", "restore")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/38289")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestShowCompletionsWhenTypingCompilerDirective_MultipleDirectiveWords(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[
#pragma warning$$
]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync()

                Await state.AssertCompletionItemsContainAll("disable", "enable", "restore")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/38289")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestCompletionsWhenTypingCompilerDirective_DoNotCrashOnDocumentStart(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document><![CDATA[nullable$$]]></Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync()

                ' This assertion would fail if any unhandled exception was thrown during computing completions
                Await state.AssertCompletionItemsDoNotContainAny("disable", "enable", "restore")
            End Using
        End Function

        <ExportCompletionProvider(NameOf(MultipleChangeCompletionProvider), LanguageNames.CSharp)>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class MultipleChangeCompletionProvider
            Inherits CompletionProvider

            Private _caretPosition As Integer

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Sub SetInfo(caretPosition As Integer)
                _caretPosition = caretPosition
            End Sub

            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                context.AddItem(CompletionItem.Create(
                    "CustomItem",
                    rules:=CompletionItemRules.Default.WithMatchPriority(1000), isComplexTextEdit:=True))
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/43439")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/43439")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/944031")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/944031")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/944031")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/944031")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/944031")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1128749")>
        <WpfTheory, CombinatorialData>
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

        <WorkItem("https://github.com/dotnet/roslyn/pull/47511")>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

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
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                ' Make sure expander is selected
                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                ' Make sure expander is selected
                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

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
        Public Async Function TestTypeImportCompletionAfterScoped(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
namespace MyNamespace
{
    public ref struct MyRefStruct { }
}

namespace Test
{
    class Program
    {
        public static void Main()
        {
            scoped $$
        }
    }
}
</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                ' Make sure expander is selected
                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

                Dim expectedText = "
using MyNamespace;

namespace MyNamespace
{
    public ref struct MyRefStruct { }
}

namespace Test
{
    class Program
    {
        public static void Main()
        {
            scoped MyRefStruct 
        }
    }
}
"
                state.SendTypeChars("MyR")
                state.SendSelectCompletionItem("MyRefStruct")
                state.SendTypeChars(" ")
                Assert.Equal(expectedText, state.GetDocumentText())
                Await state.AssertLineTextAroundCaret(expectedTextBeforeCaret:="            scoped MyRefStruct ", expectedTextAfterCaret:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTypeImportCompletionAfterScopedInTopLevel(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
scoped $$

namespace MyNamespace
{
    public ref struct MyRefStruct { }
}
</Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.WaitForUIRenderedAsync()

                ' Make sure expander is selected
                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

                Dim expectedText = "
using MyNamespace;

scoped MyRefStruct 

namespace MyNamespace
{
    public ref struct MyRefStruct { }
}
"
                state.SendTypeChars("MyR")
                state.SendSelectCompletionItem("MyRefStruct")
                state.SendTypeChars(" ")
                Assert.Equal(expectedText, state.GetDocumentText())
                Await state.AssertLineTextAroundCaret(expectedTextBeforeCaret:="scoped MyRefStruct ", expectedTextAfterCaret:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

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

        <WpfFact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/63922")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/55546")>
        Public Async Function DoNotSelectMatchPriorityDeprioritizeAndBetterCaseSensitiveWithOnlyLowercaseTyped() As Task
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
}</Document>)
                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()
                Await state.SendTypeCharsAndWaitForUiRenderAsync("sh")

                ' "(short)" item has a MatchPriority of "Deprioritize", so we don't want to select it over regular item "Should"
                ' even if it matches with filter text better in term of case-sensitivity.
                Await state.AssertSelectedCompletionItem("Should")
                Await state.AssertCompletionItemsContain(Function(item)
                                                             Return item.GetEntireDisplayText() = "(short)"
                                                         End Function)
            End Using
        End Function

        <WpfFact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/55546")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/63922")>
        Public Async Function PreferBestMatchPriorityAndCaseSensitiveOverPreselect() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
public class AA
{
    private static void CC()
    {
        $$
    }
}</Document>,
                extraExportedTypes:={GetType(TestMatchPriorityCompletionProvider)}.ToList())

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of TestMatchPriorityCompletionProvider)().Single()

                provider.AddItems(New(displayText As String, matchPriority As Integer)() {
                                  ("item1", MatchPriority.Default - 1),
                                  ("item2", MatchPriority.Default + 1),
                                  ("item3", MatchPriority.Default),
                                  ("Item4", MatchPriority.Preselect)})

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()
                Await state.SendTypeCharsAndWaitForUiRenderAsync("item")

                ' always prefer case-sensitive match of highest priority, even in the presence of item with MatchPriority.Preselect
                Await state.AssertSelectedCompletionItem("item2")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/55546")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/63922")>
        Public Async Function PreferBestCaseSensitiveWithUppercaseTyped(uppercaseItemIsDeprioritize As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
public class AA
{
    private static void CC()
    {
        $$
    }
}</Document>,
                extraExportedTypes:={GetType(TestMatchPriorityCompletionProvider)}.ToList())

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of TestMatchPriorityCompletionProvider)().Single()

                provider.AddItems(New(displayText As String, matchPriority As Integer)() {
                                  ("item1", MatchPriority.Preselect),
                                  ("item2", MatchPriority.Default + 1),
                                  ("Item3", If(uppercaseItemIsDeprioritize, MatchPriority.Deprioritize, MatchPriority.Default - 1))})

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()
                Await state.SendTypeCharsAndWaitForUiRenderAsync("Item")

                ' regardless of priority, if any uppercase letter is typed, ensure we prefer casing over match priority (including Preselect items) if uppercase is typed
                ' even if item with best matched casing has MatchPriority.Deprioritize
                Await state.AssertSelectedCompletionItem("Item3")
            End Using
        End Function

        <PartNotDiscoverable>
        <[Shared], ExportCompletionProvider(NameOf(TestMatchPriorityCompletionProvider), LanguageNames.CSharp)>
        Private Class TestMatchPriorityCompletionProvider
            Inherits CompletionProvider

            Public Property Items As ImmutableArray(Of CompletionItem)

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Sub AddItems(items As (displayText As String, matchPriority As Integer)())
                Dim builder = ArrayBuilder(Of CompletionItem).GetInstance(items.Length)
                For Each item In items
                    builder.Add(CompletionItem.Create(displayText:=item.displayText, rules:=CompletionItemRules.Default.WithMatchPriority(item.matchPriority)))
                Next
                Me.Items = builder.ToImmutableAndFree()
            End Sub

            ' All lowercase items have lower MatchPriority than uppercase item, except one with equal value.
            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                context.AddItems(Items)
                Return Task.CompletedTask
            End Function

            Public Overrides Function ShouldTriggerCompletion(text As SourceText, caretPosition As Integer, trigger As CompletionTrigger, options As OptionSet) As Boolean
                Return True
            End Function
        End Class

        <WpfFact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/63922")>
        Public Async Function DoNotSelectItemWithHigherMatchPriorityButWorseCaseSensitivity() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
            <Document>
using System;
public class AA
{
    public void F(object node)
    {
        var Node = (string)nod$$
    }
}</Document>)

                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                ' test prefix match
                Await state.AssertSelectedCompletionItem("node", isHardSelected:=True)
                Await state.AssertCompletionItemsContain("Node", "")

                Await state.SendTypeCharsAndWaitForUiRenderAsync("e")

                ' test complete match
                Await state.AssertSelectedCompletionItem("node", isHardSelected:=True)
                Await state.AssertCompletionItemsContain("Node", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

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

        <WorkItem("https://github.com/dotnet/roslyn/issues/49813")>
        <WpfTheory, CombinatorialData>
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

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnDeletion, LanguageNames.CSharp, True)

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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("..")
                Await state.AssertNoCompletionSession()

                state.SendInvokeCompletionList()
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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars(".. va")
                Await state.AssertSelectedCompletionItem(displayText:="var", isHardSelected:=True)

                state.SendTab()
                state.SendTypeChars(" x")
                Await state.AssertNoCompletionSession()
                Assert.Contains("c is [.. var x]", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("..")
                Await state.AssertNoCompletionSession()

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="async", isHardSelected:=False)

                state.SendTypeChars("nu")
                Await state.AssertSelectedCompletionItem(displayText:="null", isHardSelected:=True)

                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("c is [ 0, ..null ]", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionForLambdaDefaultParameters_01(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
    void M()
    {
        (int x = $$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("int.M")
                Await state.AssertSelectedCompletionItem(displayText:="MaxValue")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionForLambdaDefaultParameters_01_AferParameter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
    void M()
    {
        (int y, int x = $$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("int.M")
                Await state.AssertSelectedCompletionItem(displayText:="MaxValue")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionForLambdaDefaultParameters_01_AferOptionalParameter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
    void M()
    {
        (int y = 1, int x = $$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("int.M")
                Await state.AssertSelectedCompletionItem(displayText:="MaxValue")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionForLambdaDefaultParameters_02(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
    const int myConst = 100;
    void M()
    {
        (int x = $$) => x;
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)
                state.SendTypeChars("my")
                Await state.AssertCompletionItemsContain("myConst", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionForLambdaDefaultParameters_02_AferParameter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
    const int myConst = 100;
    void M()
    {
        (int y, int x = $$) => x;
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)
                state.SendTypeChars("my")
                Await state.AssertCompletionItemsContain("myConst", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionForLambdaDefaultParameters_02_AferOptionalParameter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
    const int myConst = 100;
    void M()
    {
        (int y = 1, int x = $$) => x;
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)
                state.SendTypeChars("my")
                Await state.AssertCompletionItemsContain("myConst", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionForLambdaDefaultParameters_02_BeforeParameter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
    const int myConst = 100;
    void M()
    {
        (int x = $$, int y) => x;
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)
                state.SendTypeChars("my")
                Await state.AssertCompletionItemsContain("myConst", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionForLambdaDefaultParameters_02_BeforeOptionalParameter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class C
{
    const int myConst = 100;
    void M()
    {
        (int x = $$, int y = 1) => x;
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)
                state.SendTypeChars("my")
                Await state.AssertCompletionItemsContain("myConst", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionForLambdaParamsArray(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class pType { }
class C
{
    void M()
    {
        string pLocal = "p";
        var lam = ($$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("p")
                Await state.AssertSelectedCompletionItem(displayText:="params")
                Await state.AssertCompletionItemsContainAll("pType", "pLocal")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionForLambdaParamsArray_BeforeParameter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class pType { }
class C
{
    void M()
    {
        string pLocal = "p";
        var lam = ($$ int[] xs) => { };
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("p")
                Await state.AssertSelectedCompletionItem(displayText:="params")
                Await state.AssertCompletionItemsContainAll("pType", "pLocal")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionForLambdaParamsArray_AfterParameter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class pType { }
class C
{
    void M()
    {
        string pLocal = "p";
        var lam = (int x, $$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("p")
                Await state.AssertSelectedCompletionItem(displayText:="params")
                Await state.AssertCompletionItemsContainAll("pType", "pLocal")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionForLambdaParamsArray_AfterOptionalParameter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class pType { }
class C
{
    void M()
    {
        string pLocal = "p";
        var lam = (int x = 1, $$) => { };
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("p")
                Await state.AssertSelectedCompletionItem(displayText:="params")
                Await state.AssertCompletionItemsContainAll("pType")
                Await state.AssertCompletionItemsDoNotContainAny("pLocal")
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

        <WorkItem("https://github.com/dotnet/roslyn/issues/53712")>
        <WpfTheory, CombinatorialData>
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

                Dim completionService = state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim notifyProvider As NotifyCommittingItemCompletionProvider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of NotifyCommittingItemCompletionProvider)().Single()
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
                globalOptions.SetGlobalOption(CompletionViewOptionsStorage.BlockForCompletionItems, LanguageNames.CSharp, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                state.TextView.Options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, True)

                Dim completionService = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of TestProvider)().Single()

                ' completion list shouldn't have expand item until we release the checkpoint
                Await state.SendTypeCharsAndWaitForUiRenderAsync("TestUnimp")
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")

                Dim session = Await state.GetCompletionSession()
                Dim sessionData = CompletionSessionData.GetOrCreateSessionData(session)
                Dim expandTask = sessionData.ExpandedItemsTask

                Assert.NotNull(expandTask)
                Assert.False(expandTask.IsCompleted)

                ' following up by typing a few more characters each triggers an list update
                Await state.SendTypeCharsAndWaitForUiRenderAsync("o")
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")
                Assert.False(expandTask.IsCompleted)

                Await state.SendTypeCharsAndWaitForUiRenderAsync("r")
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")
                Assert.False(expandTask.IsCompleted)
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)

                provider.Checkpoint.Release()
                Await expandTask

                ' OK, now the expand task is completed,  but we shouldn't have expand item
                ' until a refresh is triggered
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")
                Assert.True(expandTask.IsCompleted)

                Await state.SendTypeCharsAndWaitForUiRenderAsync("t")
                Await state.AssertCompletionItemsContain("TestUnimportedItem", "")
                Await state.AssertSelectedCompletionItem("TestUnimportedItem", inlineDescription:="Test.Name.Spaces")
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
                globalOptions.SetGlobalOption(CompletionViewOptionsStorage.BlockForCompletionItems, LanguageNames.CSharp, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                state.TextView.Options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, True)

                Dim completionService = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of TestProvider)().Single()

                ' completion list shouldn't have expand item until we release the checkpoint
                state.SendTypeChars("TestUnimp")
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")

                Dim session = Await state.GetCompletionSession()
                Dim sessionData = CompletionSessionData.GetOrCreateSessionData(session)
                Dim expandTask = sessionData.ExpandedItemsTask

                Assert.NotNull(expandTask)
                Assert.False(expandTask.IsCompleted)

                ' following up by typing more characters each triggers an list update
                Await state.SendTypeCharsAndWaitForUiRenderAsync("o")
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=False)
                Assert.False(expandTask.IsCompleted)

                provider.Checkpoint.Release()
                Await expandTask

                ' OK, now the expand task is completed,  but we shouldn't have expand item
                ' until a refresh is triggered
                Await state.AssertCompletionItemsDoNotContainAny("TestUnimportedItem")
                Assert.True(expandTask.IsCompleted)

                ' trigger update by using expander
                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)
                Await state.AssertCompletionItemsContain("TestUnimportedItem", "")
                Await state.AssertSelectedCompletionItem("TestUnimportedItem", inlineDescription:="Test.Name.Spaces")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNonBlockingExpandCompletionDoesNotChangeItemOrder() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
                                  Test$$
                              </Document>,
                              extraExportedTypes:={GetType(TestProvider)}.ToList())

                Dim workspace = state.Workspace

                Dim globalOptions = workspace.GetService(Of IGlobalOptionService)
                globalOptions.SetGlobalOption(CompletionViewOptionsStorage.BlockForCompletionItems, LanguageNames.CSharp, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)

                state.TextView.Options.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, True)

                Dim completionService = workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetRequiredService(Of CompletionService)()
                Dim provider = completionService.GetTestAccessor().GetImportedAndBuiltInProviders(ImmutableHashSet(Of String).Empty).OfType(Of TestProvider)().Single()

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
                Await state.SendTypeCharsAndWaitForUiRenderAsync("U")
                Await state.AssertCompletionItemsContain("TestUnimportedItem", "")
                state.AssertCompletionItemExpander(isAvailable:=True, isSelected:=True)

                Dim uiRender = state.WaitForUIRenderedAsync()
                state.SendBackspace()
                Await uiRender

                ' Get the full list from session where delay happened
                Dim list1 = state.GetCompletionItems()

                state.SendEscape()
                Await state.AssertNoCompletionSession()

                ' Now disable expand item delay, so initial trigger should contain full list
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

        <WpfTheory, CombinatorialData>
        <WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/58890")>
        Public Async Function TestComparisionOperatorsInPatternMatchingCompletion(
            showCompletionInArgumentLists As Boolean,
            <CombinatorialValues("", ">", ">=", "<", "<=")> comparisonOperator As String) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document>
class Class
{
    int Prop { get; set; }
    int OtherProp { get; set; }
    public void M()
    {
        Prop is <%= comparisonOperator %> $$
    }
}</Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Await state.AssertNoCompletionSession()
                state.SendTypeChars("O")
                Await state.AssertSelectedCompletionItem(displayText:="OtherProp", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/58890")>
        Public Async Function TestComparisionOperatorsInPatternMatchingCompletion_01(
            showCompletionInArgumentLists As Boolean,
            <CombinatorialValues("", ">", ">=", "<", "<=")> comparisonOperator As String) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document>
class Class
{
    int Prop { get; set; }
    int OtherProp { get; set; }
    public void M()
    {
        Prop is > 2 and <%= comparisonOperator %> $$
    }
}</Document>,
                  showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Await state.AssertNoCompletionSession()
                state.SendTypeChars("O")
                Await state.AssertSelectedCompletionItem(displayText:="OtherProp", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory>
        <InlineData("string", "string")>
        <InlineData("string", "String")>
        <InlineData("String", "string")>
        <InlineData("String", "String")>
        Public Async Function TestSpecialTypeKeywordSelection(first As String, second As String) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                  <Document>
using System;
class Class
{
    public void M()
    {
        $$
    }
}</Document>)

                ' filter text decides selection when no type can be inferred.
                state.SendTypeChars(first)
                Await state.AssertCompletionItemsContainAll(first, second)
                Await state.AssertSelectedCompletionItem(displayText:=first, isHardSelected:=True)
                state.SendTab()

                state.SendTypeChars(" x =")

                ' We should let what user has typed to dictate whether to select keyword or type form, even when we can infer the type.
                state.SendTypeChars(second.Substring(0, 3))
                Await state.AssertCompletionItemsContainAll(first, second)
                Await state.AssertSelectedCompletionItem(displayText:=second, isHardSelected:=True)
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
                Await Checkpoint.Task.ConfigureAwait(False)
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

        <WpfFact>
        Public Async Function NamespaceFromMetadataWithoutVisibleMembersShouldBeExcluded() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="Project1">
        <Document FilePath="SourceDocument">
namespace NS
{
    public class C
    {
        public void M()
        {
            $$
        }
    }
}
        </Document>
        <MetadataReferenceFromSource Language="C#" CommonReferences="true" IncludeXmlDocComments="true" DocumentationMode="Diagnose">
            <Document FilePath="ReferencedDocument">
namespace ReferencedNamespace1
{
    internal class InternalClass {}
}

namespace ReferencedNamespace2
{
    public class PublicClass {}
}
            </Document>
        </MetadataReferenceFromSource>
    </Project>
</Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("ReferencedNamespace2", "")
                Await state.AssertCompletionItemsDoNotContainAny({"ReferencedNamespace1"})
            End Using
        End Function

        <WpfFact>
        Public Async Function TestAdditionalFilterTexts() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class MyClass
{
    public void MyMethod()
    {
        $$
    }
}
                              </Document>,
                              extraExportedTypes:={GetType(ItemWithAdditionalFilterTextsProvider)}.ToList())

                state.SendTypeChars(" ")
                Await state.AssertCompletionItemsContainAll("Consolation", "Add code that write to console", "Add code that write line to console")

                ' "c" Matches all 3 item's FilterText, so select "Add code that write line to console" which was sorted alphabetically ahead of others
                state.SendTypeChars("c")
                Await state.AssertCompletionItemsContainAll("Add code that write to console", "Add code that write line to console", "Consolation")
                Await state.AssertSelectedCompletionItem("Add code that write line to console", isHardSelected:=True)

                ' "cwl" only matches FilterText of "Add code that write line to console"
                state.SendTypeChars("wl")
                Await state.AssertCompletionItemsDoNotContainAny("Consolation", "Add code that write to console")
                Await state.AssertSelectedCompletionItem("Add code that write line to console", isHardSelected:=True)

                ' "consol" matches FilterText of "Consolation" and AdditionalFilterTexts of other 2 items, and the pattern match scores are same (prefix)
                ' but we select "Consolation" becaseu we prefer FilterText match over AdditionalFilterTexts match
                state.SendBackspaces("wl".Length)
                state.SendTypeChars("onsol")
                Await state.AssertCompletionItemsContainAll("Consolation", "Add code that write to console", "Add code that write line to console")
                Await state.AssertSelectedCompletionItem("Consolation", isHardSelected:=True)

                ' "consola"
                state.SendTypeChars("a")
                Await state.AssertCompletionItemsContain("Consolation", "")
                Await state.AssertCompletionItemsDoNotContainAny("Add code that write to console", "Add code that write line to console")
                Await state.AssertSelectedCompletionItem("Consolation", isHardSelected:=True)

                ' "console" is perfect match for "Add code that write to console" and "Add code that write line to console" (both of AdditionalFilterTexts)
                ' so we select "Add code that write line to console", which was sorted higher alphabetically
                state.SendBackspace()
                state.SendTypeChars("e")
                Await state.AssertCompletionItemsContainAll("Add code that write to console", "Add code that write line to console")
                Await state.AssertCompletionItemsDoNotContainAny("Consolation")
                Await state.AssertSelectedCompletionItem("Add code that write line to console", isHardSelected:=True)

                ' "write" is a perfect match for "Add code that write to console" and prefix match for "Add code that write line to console" (both of AdditionalFilterTexts)
                ' so we select "Add code that write to console"
                state.SendBackspaces("console".Length)
                state.SendTypeChars("write")
                Await state.AssertCompletionItemsContainAll("Add code that write to console", "Add code that write line to console")
                Await state.AssertCompletionItemsDoNotContainAny("Consolation")
                Await state.AssertSelectedCompletionItem("Add code that write to console", isHardSelected:=True)

                ' "writel"
                state.SendTypeChars("l")
                Await state.AssertCompletionItemsDoNotContainAny("Add code that write to console", "Consolation")
                Await state.AssertSelectedCompletionItem("Add code that write line to console", isHardSelected:=True)
            End Using
        End Function

        <ExportCompletionProvider(NameOf(ItemWithAdditionalFilterTextsProvider), LanguageNames.CSharp)>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class ItemWithAdditionalFilterTextsProvider
            Inherits CompletionProvider

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                context.AddItem(CompletionItem.Create(displayText:="Consolation"))
                context.AddItem(CompletionItem.Create(displayText:="Add code that write to console", filterText:="cw").WithAdditionalFilterTexts(ImmutableArray.Create("Console", "Write")))
                context.AddItem(CompletionItem.Create(displayText:="Add code that write line to console", filterText:="cwl").WithAdditionalFilterTexts(ImmutableArray.Create("Console", "WriteLine")))
                Return Task.CompletedTask
            End Function

            Public Overrides Function ShouldTriggerCompletion(text As SourceText, caretPosition As Integer, trigger As CompletionTrigger, options As OptionSet) As Boolean
                Return True
            End Function

            Public Overrides Function GetChangeAsync(document As Document, item As CompletionItem, commitKey As Char?, cancellationToken As CancellationToken) As Task(Of CompletionChange)
                Throw New NotImplementedException()
            End Function
        End Class

        <WpfFact>
        Public Async Function TestSortingOfSameNamedCompletionItems() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class MyClass
{
    public void MyMethod()
    {
        $$
    }
}
                              </Document>)

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowNewSnippetExperienceUserOption, LanguageNames.CSharp, True)
                state.SendTypeChars("if")
                Await state.AssertSelectedCompletionItem(displayText:="if", inlineDescription:=Nothing, isHardSelected:=True)
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem(displayText:="if", description:="if statement" & vbCrLf & "Code snippet for 'if statement'", inlineDescription:="if statement", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function HardSelectBreakAfterYieldIfNoYieldType(hasYieldType As Boolean) As Task
            Dim yieldDeclaration = If(hasYieldType, "public class yield{}", String.Empty)

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
namespace NS
{
     <%= yieldDeclaration %>

    class C
    {
        public static void M()
        {
            yield bre$$
        }
    }
}
                </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="break", isHardSelected:=Not hasYieldType)

            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function HardSelectReturnAfterYieldIfNoYieldType(hasYieldType As Boolean) As Task
            Dim yieldDeclaration = If(hasYieldType, "public class yield{}", String.Empty)
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
namespace NS
{
     <%= yieldDeclaration %>

    class C
    {
        public static void M()
        {
            yield ret$$
        }
    }
}
                </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="return", isHardSelected:=Not hasYieldType)

            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDeclarationNameSuggestionDoNotCrash() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;
class MyClass
{
    public void MyMethod()
    {
        ArgumentException $$
    }
}
                              </Document>)

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowNewSnippetExperienceUserOption, LanguageNames.CSharp, False)
                state.SendInvokeCompletionList()
                ' We should still work normally w/o pythia recommender
                Await state.AssertCompletionItemsContainAll("argumentException", "exception")
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDeclarationNameSuggestionWithPythiaRecommender() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;
class MyClass
{
    public void MyMethod()
    {
        ArgumentException $$
    }
}
                              </Document>,
                              extraExportedTypes:={GetType(TestPythiaDeclarationNameRecommenderImplmentation)}.ToList())

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowNewSnippetExperienceUserOption, LanguageNames.CSharp, False)

                state.SendInvokeCompletionList()
                Dim computedItems = (Await state.GetCompletionSession()).GetComputedItems(CancellationToken.None)

                Assert.NotNull(computedItems.SuggestionItem)

                Dim firstItem = computedItems.Items.First()
                Assert.Equal("PythiaRecommendName", firstItem.DisplayText)
                Assert.True({"PythiaRecommendName", "argumentException", "exception"}.All(Function(v) computedItems.Items.Any(Function(i) i.DisplayText = v)))
            End Using
        End Function

        <Export(GetType(IPythiaDeclarationNameRecommenderImplementation))>
        <[Shared]>
        <PartNotDiscoverable>
        Private Class TestPythiaDeclarationNameRecommenderImplmentation
            Implements IPythiaDeclarationNameRecommenderImplementation

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function ProvideRecommendationsAsync(context As PythiaDeclarationNameContext, cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of String)) Implements IPythiaDeclarationNameRecommenderImplementation.ProvideRecommendationsAsync
                Dim result = ImmutableArray.Create("PythiaRecommendName")
                Return Task.FromResult(result)
            End Function
        End Class

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/40393")>
        Public Async Function TestAfterUsingStatement1() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
namespace NS
{
    class C
    {
        public static void M()
        {
            using $$
        }
    }
}
                </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain(displayText:="System", displayTextSuffix:="")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/40393")>
        Public Async Function TestAfterUsingStatement2() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
namespace NS
{
    class C
    {
        public static void M()
        {
            using Sys$$
        }
    }
}
                </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain(displayText:="System", displayTextSuffix:="")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64531")>
        Public Async Function AttributeCompletionNoColonsIfAlreadyPresent() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
using System;
class TestAttribute : Attribute
{
    public string Text { get; set; }
}
 
[Test($$ = )]
class Goo
{
}
                </Document>)

                state.SendTypeChars("Tex")
                Await state.AssertSelectedCompletionItem("Text", displayTextSuffix:="")

                state.SendTab()
                Await state.WaitForAsynchronousOperationsAsync()
                state.AssertMatchesTextStartingAtLine(7, "[Test(Text = )]")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64531")>
        Public Async Function AttributeCompletionNoEqualsIfAlreadyPresent() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
using System;
class TestAttribute : Attribute
{
    public TestAttribute(int argument = 42)
    { }
}
[Test($$:)]
class Goo
{ }
                </Document>)

                state.SendTypeChars("argum")
                Await state.AssertSelectedCompletionItem("argument", displayTextSuffix:="")

                state.SendTab()
                Await state.WaitForAsynchronousOperationsAsync()
                state.AssertMatchesTextStartingAtLine(7, "[Test(argument:)]")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39689")>
        Public Async Function TestFilteringOfExtensionMethodsWithConstraints1() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
using System;

public interface ISomeInterface&lt;T&gt;
{
}

public static class Extensions
{       
    public static bool SomeExtMethod&lt;T1, T2&gt;(this T1 builder, T2 x)
        where T1 : ISomeInterface&lt;T2&gt;
    {
        return true;
    }
}
public class Bar
{
    void M(string[] s)
    {
        s.$$
    }
}
                </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsDoNotContainAny("SomeExtMethod")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39689")>
        Public Async Function TestFilteringOfExtensionMethodsWithConstraints2() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
using System;

public interface ISomeInterface&lt;T&gt;
{
}

public static class Extensions
{       
    public static bool SomeExtMethod&lt;T1, T2&gt;(this T1 builder, T2 x)
        where T1 : ISomeInterface&lt;T2&gt;
    {
        return true;
    }
}
public class Bar : ISomeInterface&lt;int&gt;
{
    void M(Bar s)
    {
        s.$$
    }
}
                </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("SomeExtMethod", displayTextSuffix:="<>")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39689")>
        Public Async Function TestFilteringOfExtensionMethodsWithConstraints3() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
using System;

public interface ISomeInterface&lt;T&gt;
{
}

public static class Extensions
{       
    public static bool SomeExtMethod&lt;T1, T2&gt;(this T1 builder, T2 x)
        where T1 : ISomeInterface&lt;T2&gt;
    {
        return true;
    }
}
public class Bar&lt;T&gt; : ISomeInterface&lt;T&gt;
{
    void M(Bar&lt;T&gt; s)
    {
        s.$$
    }
}
                </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("SomeExtMethod", displayTextSuffix:="<>")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39689")>
        Public Async Function TestFilteringOfExtensionMethodsWithConstraints4() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
using System;

public interface ISomeInterface&lt;T&gt;
{
}

public static class Extensions
{       
    public static bool SomeExtMethod&lt;T1, T2&gt;(this T1 builder, T2 x)
        where T1 : ISomeInterface&lt;T2&gt;
    {
        return true;
    }
}
public class Bar&lt;T&gt;
{
    void M(ISomeInterface&lt;T&gt; s)
    {
        s.$$
    }
}
                </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("SomeExtMethod", displayTextSuffix:="<>")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39689")>
        Public Async Function TestFilteringOfExtensionMethodsWithConstraints5() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
using System;

public interface ISomeInterface&lt;T&gt;
{
}

public static class Extensions
{       
    public static bool SomeExtMethod&lt;T1, T2&gt;(this T1 builder, T2 x)
        where T1 : ISomeInterface&lt;T2&gt;
    {
        return true;
    }
}
public class Bar&lt;T&gt;
{
    void M(ISomeInterface&lt;int&gt; s)
    {
        s.$$
    }
}
                </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("SomeExtMethod", displayTextSuffix:="<>")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39689")>
        Public Async Function TestFilteringOfExtensionMethodsWithConstraints6() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
using System;

public interface ISomeInterface&lt;T&gt;
{
}

public static class Extensions
{       
    public static bool SomeExtMethod&lt;T1, T2&gt;(this T1 builder, T2 x)
        where T1 : T2
    {
        return true;
    }
}
public class Bar&lt;T&gt;
{
    void M(string[] s)
    {
        s.$$
    }
}
                </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("SomeExtMethod", displayTextSuffix:="<>")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39689")>
        Public Async Function TestFilteringOfExtensionMethodsWithConstraints7() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
using System;

public interface ISomeInterface&lt;T&gt;
{
}

public static class Extensions
{       
    public static bool SomeExtMethod&lt;T1, T2&gt;(this T1 builder, T2 x)
        where T1 : ISomeInterface&lt;T2&gt;
    {
        return true;
    }
}
public class Bar&lt;T&gt; : ISomeInterface&lt;T&gt;
{
    void M(Bar&lt;int&gt; s)
    {
        s.$$
    }
}
                </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("SomeExtMethod", displayTextSuffix:="<>")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/64862")>
        Public Async Function TestAsyncMethodReturningValueTask() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
using System.Threading.Tasks;

class Program
{
    async ValueTask&lt;string&gt; M2Async()
    {
        return new $$;
    }
}
                </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("string", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/21055")>
        Public Async Function CompletionInOutParamWithVariableDirectlyAfter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
class Program
{
    static void Main(string[] args)
    {
        if (TryParse("", out $$

        Program p = null;
    }

    static bool TryParse(string s, out Program p) { }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("P")
                Await state.AssertSelectedCompletionItem(displayText:="Program", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/42910")>
        Public Async Function CompletionOffOfNullableLambdaParameter(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

struct TestStruct
{
    public int TestField;
}

class Program
{
    void Main() => TestMethod1(x => { return x?.$$ });

    void TestMethod1(Predicate<TestStruct?> predicate) => default;
}
]]>
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain(displayText:="TestField", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData, WorkItem(21055, "https://github.com/dotnet/roslyn/issues/43966")>
        Public Async Function CompletionOnLambaParameter_MatchDelegateParameterCount1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

class Goo { public string first; }
class Bar { public string second; }

class Program
{
    static void Quux(Action<Bar> x) { }
    static void Quux(Action<Goo, Bar> x) { }

    static void Main(string[] args)
    {
        Quux(a =>
        {
            a.$$
        });
    }
}
]]>
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain(displayText:="second", displayTextSuffix:="")
                Await state.AssertCompletionItemsDoNotContainAny("first")
            End Using
        End Function

        <WpfTheory, CombinatorialData, WorkItem(21055, "https://github.com/dotnet/roslyn/issues/43966")>
        Public Async Function CompletionOnLambaParameter_MatchDelegateParameterCount2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

class Goo { public string first; }
class Bar { public string second; }

class Program
{
    static void Quux(Action<Bar> x) { }
    static void Quux(Action<Goo, Bar> x) { }

    static void Main(string[] args)
    {
        Quux((a) =>
        {
            a.$$
        });
    }
}
]]>
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain(displayText:="second", displayTextSuffix:="")
                Await state.AssertCompletionItemsDoNotContainAny("first")
            End Using
        End Function

        <WpfTheory, CombinatorialData, WorkItem(21055, "https://github.com/dotnet/roslyn/issues/43966")>
        Public Async Function CompletionOnLambaParameter_MatchDelegateParameterCount3(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

class Goo { public string first; }
class Bar { public string second; }

class Program
{
    static void Quux(Action<Bar> x) { }
    static void Quux(Action<Goo, Bar> x) { }

    static void Main(string[] args)
    {
        Quux((a, b) =>
        {
            a.$$
        });
    }
}
]]>
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain(displayText:="first", displayTextSuffix:="")
                Await state.AssertCompletionItemsDoNotContainAny("second")
            End Using
        End Function

        <WpfTheory, CombinatorialData, WorkItem(21055, "https://github.com/dotnet/roslyn/issues/43966")>
        Public Async Function CompletionOnLambaParameter_MatchDelegateParameterCount4(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

class Goo { public string first; }
class Bar { public string second; }

class Program
{
    static void Quux(Action<Bar> x) { }
    static void Quux(Action<Goo, Bar> x) { }

    static void Main(string[] args)
    {
        Quux((a, b) =>
        {
            b.$$
        });
    }
}
]]>
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain(displayText:="second", displayTextSuffix:="")
                Await state.AssertCompletionItemsDoNotContainAny("first")
            End Using
        End Function

        <WpfTheory, CombinatorialData, WorkItem(21055, "https://github.com/dotnet/roslyn/issues/43966")>
        Public Async Function CompletionOnLambaParameter_MatchDelegateParameterCount5(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

class Goo { public string first; }
class Bar { public string second; }

class Program
{
    static void Quux(Action<Bar> x) { }
    static void Quux(Action<Goo, Bar> x) { }

    static void Main(string[] args)
    {
        Quux((a, b, c) =>
        {
            a.$$
        });
    }
}
]]>
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain(displayText:="first", displayTextSuffix:="")
                Await state.AssertCompletionItemsContain(displayText:="second", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData, WorkItem(21055, "https://github.com/dotnet/roslyn/issues/43966")>
        Public Async Function CompletionOnLambaParameter_MatchDelegateParameterCount6(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

class Goo { public string first; }
class Bar { public string second; }

class Program
{
    static void Quux(Action<Bar> x) { }
    static void Quux(Action<Goo, Bar> x) { }

    static void Main(string[] args)
    {
        Quux((a, b, c) =>
        {
            b.$$
        });
    }
}
]]>
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain(displayText:="second", displayTextSuffix:="")
                Await state.AssertCompletionItemsDoNotContainAny("first")
            End Using
        End Function

        <WpfTheory, CombinatorialData, WorkItem(21055, "https://github.com/dotnet/roslyn/issues/43966")>
        Public Async Function CompletionOnLambaParameter_MatchDelegateParameterCount7(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

class Goo { public string first; }
class Bar { public string second; }

class Program
{
    static void Quux(Action<Bar> x) { }
    static void Quux(Action<Goo, Bar> x) { }

    static void Main(string[] args)
    {
        Quux((a, b, c) =>
        {
            c.$$
        });
    }
}
]]>
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsDoNotContainAny("first", "second")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/67565")>
        Public Async Function NameOf_Flat() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class C
{
    public C1 Property0 { get; }
    public C1 Field0;
    public event System.Action Event0;
                
    public static string StaticField =
        nameof($$);
}
                
public class C1
{
    public int Property1 { get; }
    public int Field1;
    public event System.Action Event1;
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("Property0", "Field0", "Event0")
                Await state.AssertCompletionItemsDoNotContainAny("Property1", "Field1", "Event1")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/67565")>
        Public Async Function NameOf_Nested() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class C
{
    public C1 Property0 { get; }
    public C1 Field0;
    public event System.Action Event0;
                
    public static string StaticField =
        nameof(Property0.$$);
}
                
public class C1
{
    public int Property1 { get; }
    public int Field1;
    public event System.Action Event1;
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("Property1", "Field1", "Event1")
                Await state.AssertCompletionItemsDoNotContainAny("Property0", "Field0", "Event0")
            End Using
        End Function

        <InlineData("""text""u8")>
        <InlineData("""""""text""""""u8")>
        <InlineData("""""""
        text
        """"""u8")>
        <Theory, WorkItem("https://github.com/dotnet/roslyn/issues/68704")>
        Public Async Function TriggerCompletionAtEndOfUtf8StringLiteral(stringText As String) As Task
            Dim workspaceDefinition =
            <Workspace>
                <Project Language="C#" AssemblyName="TestAssembly" CommonReferencesPortable="true" LanguageVersion=<%= LanguageVersion.CSharp12.ToDisplayString() %>>
                    <Document>
public class Class1
{
    public void M()
    { 
        var channel = <%= stringText %>$$
    }
}
                    </Document>
                </Project>
            </Workspace>

            Using workspace = EditorTestWorkspace.Create(workspaceDefinition, composition:=EditorTestCompositions.EditorFeatures)
                Dim cursorDocument = workspace.Documents.First(Function(d As TestHostDocument)
                                                                   Return d.CursorPosition.HasValue
                                                               End Function)
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)
                Dim completionService = document.GetRequiredLanguageService(Of CompletionService)()

                ' This should not throw
                Dim list = Await completionService.GetCompletionsAsync(
                    document, caretPosition:=cursorPosition, CompletionOptions.Default, OptionSet.Empty, CompletionTrigger.Invoke)
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69300")>
        Public Async Function FilterPrimaryConstructorParameters1() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class C(int x)
{
    private int _x = $$;
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("x", displayTextSuffix:="")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69300")>
        Public Async Function FilterPrimaryConstructorParameters2() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class C(int x)
{
    private int _x;

    void M()
    {
        $$
    }
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("x", "_x")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69300")>
        Public Async Function FilterPrimaryConstructorParameters3() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class C(int x)
{
    private int _x = x;

    void M()
    {
        $$
    }
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("_x", displayTextSuffix:="")
                Await state.AssertCompletionItemsDoNotContainAny("x")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69300")>
        Public Async Function FilterPrimaryConstructorParameters4() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public partial class C(int x)
{
    private int _x = x;
}

public partial class C
{
    void M()
    {
        $$
    }
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("_x", displayTextSuffix:="")
                Await state.AssertCompletionItemsDoNotContainAny("x")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69300")>
        Public Async Function FilterPrimaryConstructorParameters5() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class C(int x)
{
    private int _x = x + 1;

    void M()
    {
        $$
    }
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("x", "_x")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69300")>
        Public Async Function FilterPrimaryConstructorParameters1_Property() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class C(int x)
{
    private int X { get; } = $$;
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("x", displayTextSuffix:="")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69300")>
        Public Async Function FilterPrimaryConstructorParameters2_Property() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class C(int x)
{
    private int X;

    void M()
    {
        $$
    }
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("x", "X")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69300")>
        Public Async Function FilterPrimaryConstructorParameters3_Property() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class C(int x)
{
    private int X { get; } = x;

    void M()
    {
        $$
    }
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("X", displayTextSuffix:="")
                Await state.AssertCompletionItemsDoNotContainAny("x")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69300")>
        Public Async Function FilterPrimaryConstructorParameters4_Property() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public partial class C(int x)
{
    private int X { get; } = x;
}

public partial class C
{
    void M()
    {
        $$
    }
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("X", displayTextSuffix:="")
                Await state.AssertCompletionItemsDoNotContainAny("x")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69300")>
        Public Async Function FilterPrimaryConstructorParameters5_Property() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class C(int x)
{
    private int X { get; } = x + 1;

    void M()
    {
        $$
    }
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("x", "X")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69300")>
        Public Async Function FilterPrimaryConstructorParameters_BaseType1() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class Base
{
    protected int X;
}

public class C(int x) : Base
{
    void M()
    {
        $$
    }
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("x", "X")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69300")>
        Public Async Function FilterPrimaryConstructorParameters_BaseType2() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class Base
{
    protected int X;

    public Base(int x)
    {
        X = x;
    }
}

public class C(int x) : Base(x + 1)
{
    void M()
    {
        $$
    }
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("x", "X")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69300")>
        Public Async Function FilterPrimaryConstructorParameters_BaseType3() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class Base
{
    protected int X;

    public Base(int x)
    {
        X = x;
    }
}

public class C(int x) : Base(x)
{
    void M()
    {
        $$
    }
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsDoNotContainAny("x")
                Await state.AssertCompletionItemsContainAll("X")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/78917")>
        Public Async Function FilterPrimaryConstructorParameters_AllowInBaseTypeWhenCapturedAsMember() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public sealed class Derived(
    string value)
    : Base($"{val$$}")
{
    public string Value { get; } = value;
}

public abstract class Base(string value);
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("value", displayTextSuffix:="")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestItemsSorted() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class Program
{
    public static void Main()
    {
        $$
    }
}
             ]]></Document>)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)

                ' trigger completion with import completion enabled
                Await state.SendInvokeCompletionListAndWaitForUiRenderAsync()

                ' make sure expander is selected
                Await state.SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected:=True)

                Dim completionItems = state.GetCompletionItems()
                Dim manuallySortedItems = completionItems.ToList()
                manuallySortedItems.Sort()

                Assert.True(manuallySortedItems.SequenceEqual(completionItems))
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/70106")>
        Public Async Function FilterPrimaryConstructorParameters_BaseType4() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public class Base(int x)
{
}

public class C() : Base($$)
{
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("x", ":")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/70106")>
        Public Async Function FilterPrimaryConstructorParameters_BaseType5() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
public abstract class Base(int x)
{
}

public class C() : Base($$)
{
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("x", ":")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/66305")>
        Public Async Function TestScopedKeywordRecommender() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
void M()
{
    $$
}
]]>
                </Document>,
                languageVersion:=LanguageVersion.CSharp11)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("scoped", "")
            End Using
        End Function

        <WpfFact>
        <WorkItem("https://github.com/dotnet/razor/issues/9377")>
        Public Async Function TriggerOnTypingShouldNotAffectExplicitInvoke() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
void M()
{
    $$
}
]]>
                </Document>)

                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.TriggerOnTyping, LanguageNames.CSharp, False)

                ' TriggerOnTyping should not block explicit trigger
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("M", "")

                state.SendEscape()
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("M")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/70403")>
        Public Async Function AccessStaticMembersOffOfColorColor1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
struct Cursor
{
    public static int StaticMember;
    public int InstanceMember;
}

class BaseClass
{
    public Cursor Cursor { get; set; }
}

class Derived : BaseClass
{
    void Method()
    {
        Cursor.$$
        Object o = new Object();
    }
}

                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("StaticMember", "InstanceMember")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/70403")>
        Public Async Function AccessStaticMembersOffOfColorColor2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
struct Cursor
{
    public static int StaticMember;
    public int InstanceMember;
}

class BaseClass
{
    public Cursor Cursor { get; set; }
}

class Derived : BaseClass
{
    void Method()
    {
        Cursor.$$
    }
}

                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("StaticMember", "InstanceMember")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/70732")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TypingCommitCharWhichIsAlsoFilterCharOfAnotherNoMatchingItemShouldCommit() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
namespace A
{  
    public struct PointF
    {

        public float Y { get; set; }
        
        public float X { get; set; }
        
        public static int operator +(PointF pt, PointF sz) => 0;
        
        public static int operator -(PointF ptA, PointF ptB) => 0;

        public static bool operator ==(PointF ptA, PointF ptB) => true;
        
        public static bool operator !=(PointF ptA, PointF ptB) => true;
    }

    class Program
    {
        static void Main()
        {
            PointF point;
            point$$
        }
    }
}                              </Document>)

                state.SendTypeChars(".x")
                Await state.AssertSelectedCompletionItem(displayText:="X", isHardSelected:=True)
                state.SendTypeChars("+")
                Await state.AssertNoCompletionSession()
                Assert.Contains("point.X+", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/70732")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TypingCommitCharWhichIsAlsoFilterCharOfAnotherPotentiallyMatchingItemShouldNotCommit() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
namespace A
{  
    public struct PointF
    {

        public float Y { get; set; }
        
        public float X { get; set; }
        
        public static int operator +(PointF pt, PointF sz) => 0;
        
        public static int operator -(PointF ptA, PointF ptB) => 0;

        public static bool operator ==(PointF ptA, PointF ptB) => true;
        
        public static bool operator !=(PointF ptA, PointF ptB) => true;
    }

    class Program
    {
        static void Main()
        {
            PointF point;
            point$$
        }
    }
}                              </Document>)

                state.SendTypeChars(".+")
                Await state.AssertSelectedCompletionItem(displayText:="+", inlineDescription:="x + y")
                state.SendTab()
                Assert.Contains("point +", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/vscode-csharp/issues/6374")>
        Public Sub TestArgumentCompletionTriggerForRegularMethod_NoOverLoad(hasParameter As Boolean)
            Dim parameter As String
            If (hasParameter) Then
                parameter = "int x"
            Else
                parameter = ""
            End If
            Using state = TestStateFactory.CreateCSharpTestState(
                    <Document>
public class Class1
{
    public void M()
    { 
        Bar$$
    }

    private bool Bar(<%= parameter %>) => true;
}
                    </Document>,
                showCompletionInArgumentLists:=True)
                state.SendTypeChars("(")

                If (hasParameter) Then
                    Assert.NotEmpty(state.GetCompletionItems())
                Else
                    state.AssertCompletionSession()
                End If
            End Using
        End Sub

        <WpfFact>
        <WorkItem("https://github.com/dotnet/vscode-csharp/issues/6374")>
        Public Sub TestArgumentCompletionTriggerForRegularMethod_HasOverLoad()
            Using state = TestStateFactory.CreateCSharpTestState(
                    <Document>
public class Class1
{
    public void M()
    { 
        Bar$$
    }

    private bool Bar() => true;
    private bool Bar(int x) => true;
}
                    </Document>,
                showCompletionInArgumentLists:=True)
                state.SendTypeChars("(")

                Assert.NotEmpty(state.GetCompletionItems())
            End Using
        End Sub

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/vscode-csharp/issues/6374")>
        Public Sub TestArgumentCompletionTriggerForExtensionMethod_NoOverLoad(hasParameter As Boolean)
            Dim parameter As String
            If (hasParameter) Then
                parameter = ", int x"
            Else
                parameter = ""
            End If
            Using state = TestStateFactory.CreateCSharpTestState(
                    <Document>
public class Class1
{
    public void M(string x)
    { 
        x.Bar$$
    }
}

public static class Ext
{
    public static bool Bar(this string <%= parameter %>) => true;
}
                    </Document>,
                showCompletionInArgumentLists:=True)
                state.SendTypeChars("(")

                If (hasParameter) Then
                    Assert.NotEmpty(state.GetCompletionItems())
                Else
                    state.AssertCompletionSession()
                End If
            End Using
        End Sub

        <WpfFact>
        <WorkItem("https://github.com/dotnet/vscode-csharp/issues/6374")>
        Public Sub TestArgumentCompletionTriggerForExtensionMethod_HasOverLoad()
            Using state = TestStateFactory.CreateCSharpTestState(
                    <Document>
public class Class1
{
    public void M(string x)
    { 
        x.Bar$$
    }
}

public static class Ext
{
    public static bool Bar(this string) => true;
    public static bool Bar(this string, int x) => true;
}
                    </Document>,
                showCompletionInArgumentLists:=True)
                state.SendTypeChars("(")

                Assert.NotEmpty(state.GetCompletionItems())
            End Using
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/vscode-csharp/issues/6374")>
        Public Sub TestArgumentCompletionTriggerForExtensionMethod_DirectInvoke()
            Using state = TestStateFactory.CreateCSharpTestState(
                    <Document>
public class Class1
{
    public void M(string x)
    { 
        Ext.Bar$$
    }
}

public static class Ext
{
    public static bool Bar(this string) => true;
}
                    </Document>,
                showCompletionInArgumentLists:=True)
                state.SendTypeChars("(")

                Assert.NotEmpty(state.GetCompletionItems())
            End Using
        End Sub

        <WpfTheory>
        <InlineData("task$$", True)>
        <InlineData("task$$", False)>
        <InlineData("class C { void M() { c$$ } }", True)>
        <InlineData("class C { void M() { c$$ } }", False)>
        <InlineData("class C { void M() { System.Threading.CancellationToken $$ } }", True)>
        <InlineData("class C { void M() { System.Threading.CancellationToken $$ } }", False)>
        <InlineData("class C { void M(string x) { x.$$ } }", True)>
        <InlineData("class C { void M(string x) { x.$$ } }", False)>
        <InlineData("class C
        {
            override $$
        }", True)>
        <InlineData("class C
        {
            override $$
        }", False)>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1884712")>
        Public Async Function TestCalculatingCompletionDoNotRunSourceGenerator(code As String, hasCompilationAvailable As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                    <Document>
                        <%= code %>
                    </Document>)

                ' Disable features that would try to get full compilation outside of completion code path under test

                CompilationAvailableHelpers.TestAccessor.SkipComputation = True
                state.Workspace.GlobalOptions.SetGlobalOption(EditorComponentOnOffOptions.Tagger, False)
                state.Workspace.GlobalOptions.SetGlobalOption(SemanticColorizerOptionsStorage.SemanticColorizer, False)
                state.Workspace.GlobalOptions.SetGlobalOption(SyntacticColorizerOptionsStorage.SyntacticColorizer, False)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ForceExpandedCompletionIndexCreation, True)
                state.Workspace.GlobalOptions.SetGlobalOption(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, True)

                Dim partialSolutionsTestHook = state.Workspace.CurrentSolution.Services.GetRequiredService(Of IWorkspacePartialSolutionsTestHook)()
                partialSolutionsTestHook.IsPartialSolutionDisabled = False

                ' this will be set to true when the generator ran
                Dim generatorRan = False
                Dim analyzerReference = New TestGeneratorReference(New CallbackGenerator(onInit:=Sub(x)
                                                                                                 End Sub,
                                                                                         onExecute:=Sub(y)
                                                                                                        generatorRan = True
                                                                                                    End Sub))

                ' Add the generator reference to the project
                Dim projectWithGenerator = state.Workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(analyzerReference)
                Dim document = projectWithGenerator.Documents.Single()
                Await state.Workspace.ChangeProjectAsync(projectWithGenerator.Id, projectWithGenerator.Solution)

                Dim completionService = document.GetRequiredLanguageService(Of CompletionService)()

                Assert.False(generatorRan)

                Dim compilation As Compilation = Nothing
                If (hasCompilationAvailable) Then
                    ' Ensure the compilation is created therefore a non-frozen document would be used for completion
                    ' See CompletionService.GetDocumentWithFrozenPartialSemanticsAsync for how it is implemented
                    compilation = Await projectWithGenerator.GetCompilationAsync()

                    ' We should have ran the generator
                    Assert.True(generatorRan)

                    ' Reset
                    generatorRan = False
                End If

                state.SendInvokeCompletionList()
                Dim list = state.GetCompletionItems()

                Assert.NotEmpty(list)

                ' We should not have ran the generator as part of the calculating completion list
                Assert.False(generatorRan)

                ' Go through items from each provider and make sure getting change won't run generator
                Dim seenProvider = New HashSet(Of String)
                For Each item In list
                    If (seenProvider.Add(item.ProviderName)) Then
                        Dim change = Await completionService.GetChangeAsync(document, item)

                        ' We should not have ran the generator as part of the GetChangeAsync
                        Assert.False(generatorRan, item.ProviderName)
                    End If
                Next

                Assert.NotEmpty(seenProvider)
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/8623")>
        Public Async Function TestGenerics1() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
        <Document><![CDATA[
class C
{
    /// <summary>
    /// <see cref="$$"/>
    /// </summary>
    public void M() { }
}

class Ddd { }
class Ddd<T1> { }
class Ddd<T1, T2> { }
                    ]]></Document>,
    showCompletionInArgumentLists:=True)
                state.SendTypeChars("ddd")

                Await state.AssertCompletionSession()
                state.AssertItemsInOrder({"Ddd", "Ddd{T1}", "Ddd{T1, T2}"})
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/8623")>
        Public Async Function TestGenerics2() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
        <Document><![CDATA[
class C
{
    /// <summary>
    /// <see cref="$$"/>
    /// </summary>
    public void M() { }

    void Ddd() { }
    void Ddd<T1>() { }
    void Ddd<T1, T2>() { }
}
                    ]]></Document>,
    showCompletionInArgumentLists:=True)
                state.SendTypeChars("ddd")

                Await state.AssertCompletionSession()
                state.AssertItemsInOrder({"Ddd()", "Ddd{T1}()", "Ddd{T1, T2}()"})
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/21418")>
        Public Async Function TestOverrideFiltering1() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                    <Document>
public class A
{
    public sealed override bool Equals(object obj) => throw null;
    public sealed override int GetHashCode() => throw null;
    public sealed override string ToString() => throw null;

    public virtual void Moo() { }
}

public class B : A
{
    public new virtual void Moo() { }
}

public class C : B
{
   override$$
}

                    </Document>,
                showCompletionInArgumentLists:=True)
                state.SendTypeChars(" ")

                Await state.AssertCompletionSession()
                Await state.AssertCompletionItemsContain("Moo()", "")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/21418")>
        Public Async Function TestOverrideFiltering2() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                    <Document>
public class A
{
    public sealed override bool Equals(object obj) => throw null;
    public sealed override int GetHashCode() => throw null;
    public sealed override string ToString() => throw null;

    public virtual void Moo() { }
}

public class B : A
{
    public new virtual void Moo() { }
}

public class C : B
{
    public override void Moo() { }
    override$$
}

                    </Document>,
                showCompletionInArgumentLists:=True)
                state.SendTypeChars(" ")

                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/discussions/71432")>
        Public Async Function TestAccessibilityChecksInPatterns1() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                    <Document>
object x = null;
bool b = x is N.$$;

namespace N
{
    public class C
    {
        private class B1 { }
        public class B2 { }
    }
}
                    </Document>,
                showCompletionInArgumentLists:=True)
                state.SendInvokeCompletionList()

                Await state.AssertCompletionSession()
                Await state.AssertCompletionItemsContain("C", displayTextSuffix:="")
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/discussions/71432")>
        Public Async Function TestAccessibilityChecksInPatterns2() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                    <Document>
object x = null;
bool b = x is N.C.$$;

namespace N
{
    public class C
    {
        private class B1 { }
        public class B2 { }
    }
}
                    </Document>,
                showCompletionInArgumentLists:=True)
                state.SendInvokeCompletionList()

                Await state.AssertCompletionSession()
                Await state.AssertCompletionItemsContain("B2", displayTextSuffix:="")
                Await state.AssertCompletionItemsDoNotContainAny("B1")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/72392")>
        Public Async Function AliasToDynamicType(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
using System = dynamic;
$$
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertCompletionItemsContain("System", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/pull/74484")>
        Public Async Function ReferenceToMethodThatFollow(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                            this.Sw$$

                    private void SwitchColor() { }
                }
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertCompletionItemsContain("SwitchColor", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/73120")>
        Public Async Function TestAfterPrimaryConstructorAttribute(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                class C([X] $$ client)
                {
                }
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertCompletionItemsContain("int", displayTextSuffix:="")
                Await state.AssertCompletionItemsContain("System", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/72872")>
        Public Async Function CompletionInsideImplicitObjectCreationInsideCollectionExpression(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System.Collections.Generic;

public record Parent
{
    public List<Child>? Children { get; init; }
}

public record Child
{
    public string? Id { get; init; }
}


internal class Program
{
    public string ProgramProp { get; set; }

    public void Main(string[] args)
    {
        var V1 = new Parent()
        {
            Children = [
                new()
                {
                    $$
                },
            ],
        };

    }
}]]>
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertCompletionItemsContain("Id", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/17275")>
        Public Async Function PreferCamelCasedExactMatchOverPrefixCaseInsensitiveMatch1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
                class C
                {
                    void Create() { }
                    void CreateRange() { }

                    void M()
                    {
                        this.$$
                    }
                }
]]>
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("CR")
                Await state.AssertSelectedCompletionItem("CreateRange")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/17275")>
        Public Async Function PreferCamelCasedExactMatchOverPrefixCaseInsensitiveMatch2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
                class C
                {
                    void Create() { }
                    void CreateRange() { }

                    void M()
                    {
                        this.$$
                    }
                }
]]>
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("cr")
                Await state.AssertSelectedCompletionItem("Create")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/17275")>
        Public Async Function PreferCamelCasedExactMatchOverPrefixCaseInsensitiveMatch3(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                class C
                {
                    void Create() { }
                    void CreateRange() { }

                    void M()
                    {
                        this.$$
                    }
                }
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars("Cr")
                Await state.AssertSelectedCompletionItem("Create")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/5399")>
        Public Async Function TestFilterOutOwnTypeInBaseList1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                class C : $$
                {
                }
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsDoNotContainAny("C")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/5399")>
        Public Async Function TestFilterOutOwnTypeInBaseList2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                class C : $$
                {
                    interface IGoo
                    {
                    }
                }
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("C", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/5399")>
        Public Async Function TestFilterOutOwnTypeInBaseList3(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                class C : IComparable&lt;$$&gt;
                {
                }
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("C", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/5399")>
        Public Async Function TestFilterOutOwnTypeInBaseList4(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                class C : BaseType($$);
                {
                }
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("C", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function NullConditionalAssignment1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class Class1
{
    private string s;

    public void M(Class1 c)
    { 
        c?.s = new$$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp14)

                state.SendTypeChars(" "c)
                Await state.AssertCompletionSession()

                Await state.AssertSelectedCompletionItem("string", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function NullConditionalAssignment2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
enum E
{
    Red,
    Green,
}

public class Class1
{
    private E e;

    public void M(Class1 c)
    { 
        c?.e =$$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp14)

                state.SendTypeChars(" "c)
                Await state.AssertCompletionSession()

                Await state.AssertSelectedCompletionItem("E", isHardSelected:=True)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/5399")>
        Public Async Function TestExtensionParameter1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                static class C
                {
                    extension($$)
                    {
                    }
                }
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp14)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("string", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/5399")>
        Public Async Function TestExtensionParameter2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                static class C
                {
                    extension(Customer $$)
                    {
                    }
                }
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp14)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("customer", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/5399")>
        Public Async Function TestSpeculativeTInExtension(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                static class C
                {
                    extension(Customer customer)
                    {
                        $$
                    }
                }
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp14)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("T", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/5399")>
        Public Async Function TestReturnSymbolInExtension1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                using System;

                static class C
                {
                    extension(Customer customer)
                    {
                        $$
                    }
                }
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp14)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("String", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/5399")>
        Public Async Function TestReturnSymbolInExtension2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                using System;

                static class C
                {
                    extension(Customer customer)
                    {
                        public $$
                    }
                }
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp14)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("String", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestStaticExtensionMethod(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                using System;

                object.$$

                static class C
                {
                    extension(object o)
                    {
                        public static void EM() { }
                    }
                }
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp14)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("EM", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/79444")>
        Public Async Function TestStaticExtensionMethod_OnEnumType(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
                using System;

                E.$$

                enum E;

                static class C
                {
                    extension(E)
                    {
                        public static void EM() { }
                    }
                }
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp14)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContain("EM", displayTextSuffix:="")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/78284")>
        Public Async Function TestOverrideInstanceAssignmentOperator(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
abstract class C1
{
    virtual public void operator ++() { }

    virtual public void operator -=(int i) { }
}


class C2 : C1
{
    override$$
}
            ]]></Document>,
                   languageVersion:=LanguageVersion.CSharp7, showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendTypeChars(" ")
                Await state.AssertCompletionItemsContainAll(
                    "operator ++()",
                    "operator -=(int i)")
                state.SendTypeChars("operator")
                state.SendTab()
                Assert.Contains("public override void operator ++()
    {
        throw new System.NotImplementedException();
    }", state.SubjectBuffer.CurrentSnapshot.GetText(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/54070")>
        Public Async Function TestRecommendRefInArgumentList1(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[

public class TestClass1
{
    public void TestMethod1()
    {
        int x = 5;
        Goo($$)
    }

    private void Goo(ref int a)
    {
    }
}
            ]]></Document>,
                   languageVersion:=LanguageVersion.CSharp7, showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("ref")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/54070")>
        Public Async Function TestRecommendRefInArgumentList2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[

public class TestClass1
{
    public void TestMethod1()
    {
        int x = 5;
        Goo($$)
    }

    private void Goo(int i, ref int a)
    {
    }
}
            ]]></Document>,
                   languageVersion:=LanguageVersion.CSharp7, showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("x")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/54070")>
        Public Async Function TestRecommendRefInArgumentList3(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[

public class TestClass1
{
    public void TestMethod1()
    {
        int x = 5;
        Goo(x, $$)
    }

    private void Goo(int i, ref int a)
    {
    }
}
            ]]></Document>,
                   languageVersion:=LanguageVersion.CSharp7, showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("ref")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/54070")>
        Public Async Function TestRecommendRefInArgumentList4(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[

public class TestClass1
{
    public void TestMethod1()
    {
        int x = 5;
        Goo(a: $$)
    }

    private void Goo(int i, ref int a)
    {
    }
}
            ]]></Document>,
                   languageVersion:=LanguageVersion.CSharp7, showCompletionInArgumentLists:=showCompletionInArgumentLists)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("ref")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTypeParameterPattern_InMemberDeclaration(
            showCompletionInArgumentLists As Boolean,
            <CombinatorialValues("TBu", "Func<TBu")> typeParameter As String) As Task ' not testing "(TBu" here because `TBuilder` would not be in the completion list in this case
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class TypeBuilder { }
public class C
{
    public <%= typeParameter %>$$ GetBuilder&lt;TBuilder&gt;() { }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="TBuilder", isHardSelected:=True) ' hard-select TBuilder type parameter (which is in the completion list)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTypeParameterPatternInTuple_InMemberDeclaration(
            showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class TypeBuilder { }
public class C
{
    public (TBu$$ GetBuilder&lt;TBuilder&gt;() { }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="TypeBuilder", isSoftSelected:=True) ' soft-select TypeBuilder type parameter (because `TBuilder` is not in the completion list)
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestSpeculativeTypeParameterPattern_InMemberDeclaration(
            showCompletionInArgumentLists As Boolean,
            <CombinatorialValues("TBu", "(TBu", "Func<TBu", "delegate TBu")> typeParameter As String) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class TypeBuilder { }
public class C
{
    public <%= typeParameter %>$$
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="TypeBuilder", isSoftSelected:=True) ' soft-select TypeBuilder class
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestSpeculativeTypeParameterPattern_InMemberDeclarationNoModifier(
            showCompletionInArgumentLists As Boolean,
            <CombinatorialValues("TBu", "(TBu", "Func<TBu", "delegate TBu")> typeParameter As String) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class TypeBuilder { }
public class C
{
    <%= typeParameter %>$$
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="TypeBuilder", isSoftSelected:=True) ' soft-select TypeBuilder class
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestTypeParameterPattern_InStatement(
            showCompletionInArgumentLists As Boolean,
            <CombinatorialValues("TBu", "(TBu", "Func<TBu")> typeParameter As String) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class TypeBuilder { }
public class C
{
    public bool M&lt;TBuilder&gt;()
    {
        <%= typeParameter %>$$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="TBuilder", isHardSelected:=True) ' hard-select TBuilder type parameter
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestSpeculativeTypeParameterPattern_InStatement(
            showCompletionInArgumentLists As Boolean,
            <CombinatorialValues("TBu", "(TBu", "Func<TBu")> typeParameter As String) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
public class TypeBuilder { }
public class C
{
    public bool M()
    {
        <%= typeParameter %>$$
    }
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="TypeBuilder", isHardSelected:=True) ' hard-select TypeBuilder class
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/81021")>
        Public Async Function TestStartTypingInsideTargetTypedConditionalExpression(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
using System;

class C
{
    public DateTime M(bool b)
    {
        return b ? new($$) : default;
    }
}
            ]]></Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("tick")
                Await state.AssertSelectedCompletionItem("ticks:")
            End Using
        End Function
    End Class
End Namespace
