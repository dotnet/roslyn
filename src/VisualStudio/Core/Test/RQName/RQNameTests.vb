﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.RQNameTests
    <[UseExportProvider]>
    Public Class RQNameTests
        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Async Function TestRQNameForNamespace() As Task
            Dim markup = "namespace $$MyNamespace { }"
            Dim expectedRQName = "Ns(NsName(MyNamespace))"

            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Async Function TestRQNameForDottedNamespace() As Task
            Dim markup = "namespace MyNamespace1.MyNamespace2.$$MyNamespace3 { }"
            Dim expectedRQName = "Ns(NsName(MyNamespace1),NsName(MyNamespace2),NsName(MyNamespace3))"

            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Async Function TestRQNameForInterface() As Task
            Dim markup = "interface $$IMyInterface { }"
            Dim expectedRQName = "Agg(AggName(IMyInterface,TypeVarCnt(0)))"

            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Async Function TestRQNameForInterfaceWithOneTypeParameter() As Task
            Dim markup = "interface $$IMyInterface<T> { }"
            Dim expectedRQName = "Agg(AggName(IMyInterface,TypeVarCnt(1)))"

            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Async Function TestRQNameForInterfaceWithMultipleTypeParameters() As Task
            Dim markup = "interface $$IMyInterface<T, U, V> { }"
            Dim expectedRQName = "Agg(AggName(IMyInterface,TypeVarCnt(3)))"

            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Async Function TestRQNameForDelegateType() As Task
            Dim markup = "delegate void $$MyDelegate();"
            Dim expectedRQName = "Agg(AggName(MyDelegate,TypeVarCnt(0)))"

            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Async Function TestRQNameForField() As Task
            Dim markup = <Text><![CDATA[
class MyClass
{
    int $$myField;
}"]]></Text>
            Dim expectedRQName = "Membvar(Agg(AggName(MyClass,TypeVarCnt(0))),MembvarName(myField))"

            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Async Function TestRQNameForFieldInNamespace() As Task
            Dim markup = <Text><![CDATA[
namespace MyNamespace
{
    class MyClass
    {
        int $$myField;
    }
}"]]></Text>
            Dim expectedRQName = "Membvar(Agg(NsName(MyNamespace),AggName(MyClass,TypeVarCnt(0))),MembvarName(myField))"

            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Async Function TestRQNameForEvent() As Task
            Dim markup = <Text><![CDATA[
class MyClass
{
    event Action $$MyEvent;
}"]]></Text>
            Dim expectedRQName = "Event(Agg(AggName(MyClass,TypeVarCnt(0))),EventName(MyEvent))"

            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Async Function TestRQNameForMethod() As Task
            Dim markup = <Text><![CDATA[
class MyClass
{
    void $$MyMethod();
}"]]></Text>
            Dim expectedRQName = "Meth(Agg(AggName(MyClass,TypeVarCnt(0))),MethName(MyMethod),TypeVarCnt(0),Params())"

            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Async Function TestRQNameForMethodWithArrayParameter() As Task
            Dim markup = <Text><![CDATA[
class MyClass
{
    void $$MyMethod(string[] args);
}"]]></Text>
            Dim expectedRQName = "Meth(Agg(AggName(MyClass,TypeVarCnt(0))),MethName(MyMethod),TypeVarCnt(0),Params(Param(Array(1,AggType(Agg(NsName(System),AggName(String,TypeVarCnt(0))),TypeParams())))))"

            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        <WorkItem(608534, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608534")>
        Public Async Function TestRQNameClassInModule() As Task
            Dim markup = <Text><![CDATA[
Module Module1
    Sub Main()
    End Sub

    Partial Class Partial_Generic_Event(Of GT)

        Private Sub $$Partial_Generic_Event_E5(ByVal x As ii(Of class2)) Handles Me.E5
        End Sub
    End Class
End Module
"]]></Text>
            Dim expectedRQName = "Meth(Agg(AggName(Module1,TypeVarCnt(0)),AggName(Partial_Generic_Event,TypeVarCnt(1))),MethName(Partial_Generic_Event_E5),TypeVarCnt(0),Params(Param(AggType(Agg(AggName(ii,TypeVarCnt(0))),TypeParams()))))"

            Await TestWorkerAsync(markup, LanguageNames.VisualBasic, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Async Function TestRQNameForIndexer() As Task
            Dim markup = <Text><![CDATA[
class MyClass
{
    int $$this[int i] { get { return 1; } };
}"]]></Text>
            Dim expectedRQName = "Prop(Agg(AggName(MyClass,TypeVarCnt(0))),PropName($Item$),TypeVarCnt(0),Params(Param(AggType(Agg(NsName(System),AggName(Int32,TypeVarCnt(0))),TypeParams()))))"
            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        <WorkItem(792487, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/792487")>
        Public Async Function TestRQNameForOperator() As Task
            Dim markup = <Text><![CDATA[
class MyClass
{
    public static bool $$operator ==(MyClass c1, MyClass c2)
    {
        return true;
    }

    public static bool operator !=(MyClass c1, MyClass c2)
    {
        return false;
    }
}"]]></Text>
            Dim expectedRQName As String = Nothing
            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Async Function TestRQNameForAnonymousTypeReturnsNull() As Task
            Dim markup = <Text><![CDATA[
class Program
{
    static void Main(string[] args)
    {
        var x = new { P = 3 };
        x.$$P = 4;
    }
}"]]></Text>
            Dim expectedRQName As String = Nothing
            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        <WorkItem(837914, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/837914")>
        Public Async Function TestRQNameForMethodInConstructedTypeReturnsNull() As Task
            Dim markup = <Text><![CDATA[
class G<T>
{
    public G()
    {
    }
}
 
class C
{
    // Using Progression "Calls" to navigate to G<T>.M() will FAIL
    // (and the UI shows it as G<int>.M())
    void Test()
    {
        new G$$<int>();
    }
}
 
"]]></Text>
            Dim expectedRQName As String = Nothing
            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.RQName)>
        <WorkItem(885151, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/885151")>
        Public Async Function TestRQNameForAlias() As Task
            Dim markup = <Text><![CDATA[
using d = System.Globalization.DigitShapes;
class G<T>
{
    public G()
    {
        $$d x;
    }
}
"]]></Text>
            Dim expectedRQName As String = Nothing
            Await TestWorkerAsync(markup, LanguageNames.CSharp, expectedRQName)
        End Function

        Public Function TestWorkerAsync(markup As XElement, languageName As String, expectedRQName As String) As Tasks.Task
            Return TestWorkerAsync(markup.NormalizedValue, languageName, expectedRQName)
        End Function

        Public Async Function TestWorkerAsync(markup As String, languageName As String, expectedRQName As String) As Tasks.Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= languageName %> CommonReferences="true">
                        <Document><%= markup.Replace(vbCrLf, vbLf) %></Document>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspace.Create(workspaceXml)
                Dim doc = workspace.Documents.Single()

                Dim workspaceDoc = workspace.CurrentSolution.GetDocument(doc.Id)
                Dim tree = Await workspaceDoc.GetSyntaxTreeAsync()
                Dim token = Await tree.GetTouchingWordAsync(doc.CursorPosition.Value, workspaceDoc.Project.LanguageServices.GetService(Of ISyntaxFactsService)(), CancellationToken.None)

                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(Await workspaceDoc.GetSemanticModelAsync(), token.SpanStart, workspace).ConfigureAwait(False)
                If symbol Is Nothing Then
                    symbol = (Await workspaceDoc.GetSemanticModelAsync()).GetDeclaredSymbol(token.Parent)
                End If

                If symbol Is Nothing Then
                    AssertEx.Fail("Could not find symbol")
                End If


                If expectedRQName IsNot Nothing Then
                    Dim refactoringQualifiedName = RQName.From(symbol)
                    Assert.NotNull(refactoringQualifiedName)
                    Assert.Equal(expectedRQName, refactoringQualifiedName)
                Else
                    Assert.Null(RQName.From(symbol))
                End If
            End Using
        End Function
    End Class
End Namespace
