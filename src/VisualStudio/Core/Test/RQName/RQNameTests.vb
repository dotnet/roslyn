' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.VisualStudio.LanguageServices.Implementation.RQName
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.RQNameTests
    Public Class RQNameTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Sub RQNameForNamespace()
            Dim markup = "namespace $$MyNamespace { }"
            Dim expectedRQName = "Ns(NsName(MyNamespace))"

            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Sub RQNameForDottedNamespace()
            Dim markup = "namespace MyNamespace1.MyNamespace2.$$MyNamespace3 { }"
            Dim expectedRQName = "Ns(NsName(MyNamespace1),NsName(MyNamespace2),NsName(MyNamespace3))"

            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Sub RQNameForInterface()
            Dim markup = "interface $$IMyInterface { }"
            Dim expectedRQName = "Agg(AggName(IMyInterface,TypeVarCnt(0)))"

            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Sub RQNameForInterfaceWithOneTypeParameter()
            Dim markup = "interface $$IMyInterface<T> { }"
            Dim expectedRQName = "Agg(AggName(IMyInterface,TypeVarCnt(1)))"

            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Sub RQNameForInterfaceWithMultipleTypeParameters()
            Dim markup = "interface $$IMyInterface<T, U, V> { }"
            Dim expectedRQName = "Agg(AggName(IMyInterface,TypeVarCnt(3)))"

            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Sub RQNameForDelegateType()
            Dim markup = "delegate void $$MyDelegate();"
            Dim expectedRQName = "Agg(AggName(MyDelegate,TypeVarCnt(0)))"

            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Sub RQNameForField()
            Dim markup = <Text><![CDATA[
class MyClass
{
    int $$myField;
}"]]></Text>
            Dim expectedRQName = "Membvar(Agg(AggName(MyClass,TypeVarCnt(0))),MembvarName(myField))"

            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Sub RQNameForFieldInNamespace()
            Dim markup = <Text><![CDATA[
namespace MyNamespace
{
    class MyClass
    {
        int $$myField;
    }
}"]]></Text>
            Dim expectedRQName = "Membvar(Agg(NsName(MyNamespace),AggName(MyClass,TypeVarCnt(0))),MembvarName(myField))"

            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Sub RQNameForEvent()
            Dim markup = <Text><![CDATA[
class MyClass
{
    event Action $$MyEvent;
}"]]></Text>
            Dim expectedRQName = "Event(Agg(AggName(MyClass,TypeVarCnt(0))),EventName(MyEvent))"

            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Sub RQNameForMethod()
            Dim markup = <Text><![CDATA[
class MyClass
{
    void $$MyMethod();
}"]]></Text>
            Dim expectedRQName = "Meth(Agg(AggName(MyClass,TypeVarCnt(0))),MethName(MyMethod),TypeVarCnt(0),Params())"

            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Sub RQNameForMethodWithArrayParameter()
            Dim markup = <Text><![CDATA[
class MyClass
{
    void $$MyMethod(string[] args);
}"]]></Text>
            Dim expectedRQName = "Meth(Agg(AggName(MyClass,TypeVarCnt(0))),MethName(MyMethod),TypeVarCnt(0),Params(Param(Array(1,AggType(Agg(NsName(System),AggName(String,TypeVarCnt(0))),TypeParams())))))"

            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        <WorkItem(608534)>
        Public Sub RQNameClassInModule()
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

            TestWorker(markup, LanguageNames.VisualBasic, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        Public Sub RQNameForIndexer()
            Dim markup = <Text><![CDATA[
class MyClass
{
    int $$this[int i] { get { return 1; } };
}"]]></Text>
            Dim expectedRQName = "Prop(Agg(AggName(MyClass,TypeVarCnt(0))),PropName($Item$),TypeVarCnt(0),Params(Param(AggType(Agg(NsName(System),AggName(Int32,TypeVarCnt(0))),TypeParams()))))"
            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        <WorkItem(792487)>
        Public Sub RQNameForOperator()
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
            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub


        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        <WorkItem(7924037)>
        Public Sub RQNameForAnonymousTypeReturnsNull()
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
            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        <WorkItem(837914)>
        Public Sub RQNameForMethodInConstructedTypeReturnsNull()
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
            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.RQName)>
        <WorkItem(885151)>
        Public Sub RQNameForAlias()
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
            TestWorker(markup, LanguageNames.CSharp, expectedRQName)
        End Sub

        Public Sub TestWorker(markup As XElement, languageName As String, expectedRQName As String)
            TestWorker(markup.NormalizedValue, languageName, expectedRQName)
        End Sub

        Public Sub TestWorker(markup As String, languageName As String, expectedRQName As String)
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= languageName %> CommonReferences="true">
                        <Document><%= markup.Replace(vbCrLf, vbLf) %></Document>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceXml)
                Dim doc = workspace.Documents.Single()

                Dim workspaceDoc = workspace.CurrentSolution.GetDocument(doc.Id)
                Dim token = workspaceDoc.GetSyntaxTreeAsync().Result.GetTouchingWord(doc.CursorPosition.Value, workspaceDoc.Project.LanguageServices.GetService(Of ISyntaxFactsService)(), CancellationToken.None)

                Dim symbol = SymbolFinder.FindSymbolAtPosition(workspaceDoc.GetSemanticModelAsync().Result, token.SpanStart, workspace, CancellationToken.None)
                If symbol Is Nothing Then
                    symbol = workspaceDoc.GetSemanticModelAsync().Result.GetDeclaredSymbol(token.Parent)
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
        End Sub
    End Class
End Namespace
