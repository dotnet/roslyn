' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.QuickInfo

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.QuickInfo
    Public Class SemanticQuickInfoSourceTests
        Inherits AbstractSemanticQuickInfoSourceTests

        Protected Overrides Function TestAsync(markup As String, ParamArray expectedResults() As Action(Of QuickInfoItem)) As Task
            Return TestWithReferencesAsync(markup, Array.Empty(Of String)(), expectedResults)
        End Function

        Protected Async Function TestSharedAsync(workspace As TestWorkspace, position As Integer, ParamArray expectedResults() As Action(Of QuickInfoItem)) As Task
            Dim service = workspace.Services _
                .GetLanguageServices(LanguageNames.VisualBasic) _
                .GetService(Of QuickInfoService)

            Await TestSharedAsync(workspace, service, position, expectedResults)

            ' speculative semantic model
            Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
            If Await CanUseSpeculativeSemanticModelAsync(document, position) Then
                Dim buffer = workspace.Documents.Single().TextBuffer
                Using edit = buffer.CreateEdit()
                    edit.Replace(0, buffer.CurrentSnapshot.Length, buffer.CurrentSnapshot.GetText())
                    edit.Apply()
                End Using

                Await TestSharedAsync(workspace, service, position, expectedResults)
            End If
        End Function

        Private Async Function TestSharedAsync(workspace As TestWorkspace, service As QuickInfoService, position As Integer, expectedResults() As Action(Of QuickInfoItem)) As Task
            Dim info = Await service.GetQuickInfoAsync(
                workspace.CurrentSolution.Projects.First().Documents.First(),
                position, cancellationToken:=CancellationToken.None)

            If expectedResults Is Nothing Then
                Assert.Null(info)
            Else
                Assert.NotNull(info)

                For Each expected In expectedResults
                    expected(info)
                Next
            End If
        End Function

        Private Async Function TestFromXmlAsync(markup As String, ParamArray expectedResults As Action(Of QuickInfoItem)()) As Task
            Using workspace = TestWorkspace.Create(markup)
                Await TestSharedAsync(workspace, workspace.Documents.First().CursorPosition.Value, expectedResults)
            End Using
        End Function

        Private Async Function TestWithReferencesAsync(markup As String, metadataReferences As String(), ParamArray expectedResults() As Action(Of QuickInfoItem)) As Task
            Dim code As String = Nothing
            Dim position As Integer = Nothing
            MarkupTestFile.GetPosition(markup, code, position)

            Using workspace = TestWorkspace.CreateVisualBasic(code, Nothing, metadataReferences:=metadataReferences)
                Await TestSharedAsync(workspace, position, expectedResults)
            End Using
        End Function

        Private Async Function TestWithImportsAsync(markup As String, ParamArray expectedResults() As Action(Of QuickInfoItem)) As Task
            Dim markupWithImports =
             "Imports System" & vbCrLf &
             "Imports System.Collections.Generic" & vbCrLf &
             "Imports System.Linq" & vbCrLf &
             markup

            Await TestAsync(markupWithImports, expectedResults)
        End Function

        Private Async Function TestInClassAsync(markup As String, ParamArray expectedResults() As Action(Of QuickInfoItem)) As Task
            Dim markupInClass =
             "Class C" & vbCrLf &
             markup & vbCrLf &
             "End Class"

            Await TestWithImportsAsync(markupInClass, expectedResults)
        End Function

        Private Async Function TestInMethodAsync(markup As String, ParamArray expectedResults() As Action(Of QuickInfoItem)) As Task
            Dim markupInClass =
             "Class C" & vbCrLf &
             "Sub M()" & vbCrLf &
             markup & vbCrLf &
             "End Sub" & vbCrLf &
             "End Class"

            Await TestWithImportsAsync(markupInClass, expectedResults)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestInt32() As Task
            Await TestInClassAsync("Dim i As $$Int32",
             MainDescription("Structure System.Int32"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestInteger() As Task
            Await TestInClassAsync("Dim i As $$Integer",
             MainDescription("Structure System.Int32",
              ExpectedClassifications(
               Keyword("Structure"),
               WhiteSpace(" "),
               Identifier("System"),
               Operators.Dot,
               Struct("Int32"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestString() As Task
            Await TestInClassAsync("Dim i As $$String",
             MainDescription("Class System.String",
              ExpectedClassifications(
               Keyword("Class"),
               WhiteSpace(" "),
               Identifier("System"),
               Operators.Dot,
               [Class]("String"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestStringAtEndOfToken() As Task
            Await TestInClassAsync("Dim i As String$$",
             MainDescription("Class System.String"))
        End Function

        <WorkItem(1280, "https://github.com/dotnet/roslyn/issues/1280")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestStringLiteral() As Task
            Await TestInClassAsync("Dim i = ""cat""$$",
             MainDescription("Class System.String"))
        End Function

        <WorkItem(1280, "https://github.com/dotnet/roslyn/issues/1280")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestInterpolatedStringLiteral() As Task
            Await TestInClassAsync("Dim i = $""cat""$$", MainDescription("Class System.String"))
            Await TestInClassAsync("Dim i = $""c$$at""", MainDescription("Class System.String"))
            Await TestInClassAsync("Dim i = $""$$cat""", MainDescription("Class System.String"))
            Await TestInClassAsync("Dim i = $""cat {1$$ + 2} dog""", MainDescription("Structure System.Int32"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestListOfString() As Task
            Await TestInClassAsync("Dim l As $$List(Of String)",
             MainDescription("Class System.Collections.Generic.List(Of T)",
              ExpectedClassifications(
               Keyword("Class"),
               WhiteSpace(" "),
               Identifier("System"),
               Operators.Dot,
               Identifier("Collections"),
               Operators.Dot,
               Identifier("Generic"),
               Operators.Dot,
               [Class]("List"),
               Punctuation.OpenParen,
               Keyword("Of"),
               WhiteSpace(" "),
               TypeParameter("T"),
               Punctuation.CloseParen)),
             TypeParameterMap(vbCrLf & $"T {FeaturesResources.is_} String",
              ExpectedClassifications(
                  WhiteSpace(vbCrLf),
               TypeParameter("T"),
               WhiteSpace(" "),
               Text(FeaturesResources.is_),
               WhiteSpace(" "),
               Keyword("String"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestListOfT() As Task
            Await TestWithImportsAsync(<Text>
                    Class C(Of T)
                        Dim l As $$List(Of T)
                    End Class
                </Text>.NormalizedValue,
             MainDescription("Class System.Collections.Generic.List(Of T)",
              ExpectedClassifications(
               Keyword("Class"),
               WhiteSpace(" "),
               Identifier("System"),
               Operators.Dot,
               Identifier("Collections"),
               Operators.Dot,
               Identifier("Generic"),
               Operators.Dot,
               [Class]("List"),
               Punctuation.OpenParen,
               Keyword("Of"),
               WhiteSpace(" "),
               TypeParameter("T"),
               Punctuation.CloseParen)))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestListOfT2() As Task
            Await TestWithImportsAsync(<Text>
                    Class C(Of T)
                        Dim l As Lis$$t(Of T)
                    End Class
                 </Text>.NormalizedValue,
             MainDescription("Class System.Collections.Generic.List(Of T)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestListOfT3() As Task
            Await TestWithImportsAsync(<Text>
                    Class C(Of T)
                        Dim l As List$$(Of T)
                    End Class
                </Text>.NormalizedValue,
             MainDescription("Class System.Collections.Generic.List(Of T)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestListOfT4() As Task
            Await TestWithImportsAsync(<Text>
                    Class C(Of T)
                        Dim l As List $$(Of T)
                    End Class
                </Text>.NormalizedValue,
             Nothing)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDictionaryOfIntegerAndString() As Task
            Await TestWithImportsAsync(<Text>
                    Class C
                        Dim d As $$Dictionary(Of Integer, String)
                    End Class
                </Text>.NormalizedValue,
             MainDescription("Class System.Collections.Generic.Dictionary(Of TKey, TValue)"),
             TypeParameterMap(
              Lines(vbCrLf & $"TKey {FeaturesResources.is_} Integer",
                 $"TValue {FeaturesResources.is_} String")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDictionaryOfTAndU() As Task
            Await TestWithImportsAsync(<Text>
                    Class C(Of T, U)
                        Dim d As $$Dictionary(Of T, U)
                    End Class
                </Text>.NormalizedValue,
             MainDescription("Class System.Collections.Generic.Dictionary(Of TKey, TValue)"),
             TypeParameterMap(
              Lines(vbCrLf & $"TKey {FeaturesResources.is_} T",
                 $"TValue {FeaturesResources.is_} U")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestIEnumerableOfInteger() As Task
            Await TestInClassAsync("Dim ie As $$IEnumerable(Of Integer)",
             MainDescription("Interface System.Collections.Generic.IEnumerable(Of Out T)"),
             TypeParameterMap(vbCrLf & $"T {FeaturesResources.is_} Integer"))
        End Function

        <WorkItem(542157, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542157")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestEvent() As Task
            Await TestInMethodAsync("AddHandler System.Console.$$CancelKeyPress, AddressOf S",
             MainDescription("Event Console.CancelKeyPress As ConsoleCancelEventHandler",
              ExpectedClassifications(
               Keyword("Event"),
               WhiteSpace(" "),
               [Class]("Console"),
               Operators.Dot,
               Identifier("CancelKeyPress"),
               WhiteSpace(" "),
               Keyword("As"),
               WhiteSpace(" "),
               [Delegate]("ConsoleCancelEventHandler"))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestEventHandler() As Task
            Await TestInClassAsync("Dim e As $$EventHandler",
             MainDescription("Delegate Sub System.EventHandler(sender As Object, e As System.EventArgs)",
              ExpectedClassifications(
               Keyword("Delegate"),
               WhiteSpace(" "),
               Keyword("Sub"),
               WhiteSpace(" "),
               Identifier("System"),
               Operators.Dot,
               [Delegate]("EventHandler"),
               Punctuation.OpenParen,
               Identifier("sender"),
               WhiteSpace(" "),
               Keyword("As"),
               WhiteSpace(" "),
               Keyword("Object"),
               Punctuation.Comma,
               WhiteSpace(" "),
               Identifier("e"),
               WhiteSpace(" "),
               Keyword("As"),
               WhiteSpace(" "),
               Identifier("System"),
               Operators.Dot,
               [Class]("EventArgs"),
               Punctuation.CloseParen)))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestTypeParameter() As Task
            Await TestAsync(StringFromLines("Class C(Of T)",
                  "    Dim t As $$T",
                  "End Class"),
             MainDescription($"T {FeaturesResources.in_} C(Of T)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestNullableOfInteger() As Task
            Await TestInClassAsync("Dim n As $$Nullable(Of Integer)",
             MainDescription("Structure System.Nullable(Of T As Structure)"),
             TypeParameterMap(vbCrLf & $"T {FeaturesResources.is_} Integer"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestGenericTypeDeclaredOnMethod1() As Task
            Await TestAsync(<Text>
                    Class C
                        Shared Sub Meth1(Of T1)
                            Dim i As $$T1
                        End Sub
                    End Class
                </Text>.NormalizedValue,
             MainDescription($"T1 {FeaturesResources.in_} C.Meth1(Of T1)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestGenericTypeDeclaredOnMethod2() As Task
            Await TestAsync(<Text>
                    Class C
                        Shared Sub Meth1(Of T1 As Class)
                            Dim i As $$T1
                        End Sub
                    End Class
                </Text>.NormalizedValue,
             MainDescription($"T1 {FeaturesResources.in_} C.Meth1(Of T1 As Class)"))
        End Function

        <WorkItem(538732, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538732")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestParameter() As Task
            Await TestWithImportsAsync(<Text>
                    Module C
                        Shared Sub Goo(Of T1 As Class)
                            Console.Wr$$ite(5)
                        End Sub
                    End Class
                </Text>.NormalizedValue,
             MainDescription($"Sub Console.Write(value As Integer) (+ 17 {FeaturesResources.overloads_})",
              ExpectedClassifications(
               Keyword("Sub"),
               WhiteSpace(" "),
               [Class]("Console"),
               Operators.Dot,
               Identifier("Write"),
               Punctuation.OpenParen,
               Identifier("value"),
               WhiteSpace(" "),
               Keyword("As"),
               WhiteSpace(" "),
               Keyword("Integer"),
               Punctuation.CloseParen,
               WhiteSpace(" "),
               Punctuation.OpenParen,
               Punctuation.Text("+"),
               WhiteSpace(" "),
               Text("17"),
               WhiteSpace(" "),
               Text(FeaturesResources.overloads_),
               Punctuation.CloseParen)))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestOnFieldDeclaration() As Task
            Await TestInClassAsync("Dim $$i As Int32",
             MainDescription($"({FeaturesResources.field}) C.i As Integer"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestMinimal1() As Task
            Await TestAsync(<Text>
                     Imports System.Collections.Generic
                     Class C
                     Dim p as New Li$$st(Of string)
                     End Class
                 </Text>.NormalizedValue,
              MainDescription($"Sub List(Of String).New() (+ 2 {FeaturesResources.overloads_})"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestMinimal2() As Task
            Await TestAsync(<Text>
                     Imports System.Collections.Generic
                     Class C
                     function $$P() as List(Of string)
                     End Class
                 </Text>.NormalizedValue,
              MainDescription("Function C.P() As List(Of String)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAnd() As Task
            Await TestAsync(<Text>
                     Imports System.Collections.Generic
                     Class C
                        sub s()
                            dim x as Boolean
                            x= true a$$nd False
                        end sub
                     End Class
                 </Text>.NormalizedValue,
              MainDescription("Operator Boolean.And(left As Boolean, right As Boolean) As Boolean"))
        End Function

        <WorkItem(538822, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538822")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDelegate() As Task
            Await TestAsync(<Text>
                     Imports System
                     Class C
                        sub s()
                            dim F as F$$unc(of Integer, String)
                        end sub
                     End Class
                 </Text>.NormalizedValue,
              MainDescription("Delegate Function System.Func(Of In T, Out TResult)(arg As T) As TResult"),
              TypeParameterMap(
               Lines(vbCrLf & $"T {FeaturesResources.is_} Integer",
                  $"TResult {FeaturesResources.is_} String")))
        End Function

        <WorkItem(538824, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538824")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestOnDelegateInvocation() As Task
            Await TestAsync(<Text>
                    Class Program
                        delegate sub D1()
                        shared sub Main()
                            dim d as D1
                            $$d()
                        end sub
                    end class</Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.local_variable}) d As D1"))
        End Function

        <WorkItem(538786, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538786")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestOnGenericOverloads1() As Task
            Await TestAsync(<Text>
Module C
    Sub M()
    End Sub

    Sub M(Of T)()
    End Sub

    Sub M(Of T, U)()
    End Sub
End Module
 
Class Test
    Sub MySub()
        C.$$M() 
        C.M(Of Integer)() 
    End Sub
End Class
</Text>.NormalizedValue,
            MainDescription($"Sub C.M() (+ 2 {FeaturesResources.overloads_})"))
        End Function

        <WorkItem(538786, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538786")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestOnGenericOverloads2() As Task
            Await TestAsync(<Text>
Module C
    Sub M()
    End Sub

    Sub M(Of T)()
    End Sub

    Sub M(Of T, U)()
    End Sub
End Module
 
Class Test
    Sub MySub()
        C.M() 
        C.$$M(Of Integer)() 
    End Sub
End Class
</Text>.NormalizedValue,
            MainDescription("Sub C.M(Of Integer)()"))
        End Function

        <WorkItem(538773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538773")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestOverriddenMethod() As Task
            Await TestAsync(<Text>
Class A
    Public Overridable Sub G()
    End Sub
End Class

Class B
    Inherits A
    Public Overrides Sub G()
    End Sub
End Class

Class C
    Sub Test()
        Dim x As New B
        x.G$$()
    End Sub
End Class
</Text>.NormalizedValue,
            MainDescription($"Sub B.G() (+ 1 {FeaturesResources.overload})"))
        End Function

        <WorkItem(538918, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538918")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestOnMe() As Task
            Await TestAsync(<Text>
class C
    Sub Test()
        $$Me.Test()
    End Sub
End class
</Text>.NormalizedValue,
            MainDescription("Class C",
             ExpectedClassifications(
              Keyword("Class"),
              WhiteSpace(" "),
              [Class]("C"))))
        End Function

        <WorkItem(539240, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539240")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestOnArrayCreation1() As Task
            Await TestAsync("
class C
    Sub Test()
        Dim a As Integer() = N$$ew Integer(3) { }
    End Sub
End class",
            MainDescription("Integer()"))
        End Function

        <WorkItem(539240, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539240")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestOnArrayCreation2() As Task
            Await TestAsync(<Text>
class C
    Sub Test()
        Dim a As Integer() = New In$$teger(3) { }
    End Sub
End class
</Text>.NormalizedValue,
            MainDescription("Structure System.Int32"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDimInFieldDeclaration() As Task
            Await TestInClassAsync("Dim$$ a As Integer", MainDescription("Structure System.Int32"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDimMultipleInFieldDeclaration() As Task
            Await TestInClassAsync("$$Dim x As Integer, y As String", MainDescription(VBFeaturesResources.Multiple_Types))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDimInFieldDeclarationCustomType() As Task
            Await TestAsync(<Text>
Module Program
    Di$$m z As CustomClass	
    Private Class CustomClass
    End Class
End Module
</Text>.NormalizedValue,
            MainDescription("Class Program.CustomClass"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDimInLocalDeclaration() As Task
            Await TestInMethodAsync("Dim$$ a As Integer", MainDescription("Structure System.Int32"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDimMultipleInLocalDeclaration() As Task
            Await TestInMethodAsync("$$Dim x As Integer, y As String", MainDescription(VBFeaturesResources.Multiple_Types))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDimInLocalDeclarationCustomType() As Task
            Await TestAsync(<Text>
Module Program
    Sub Main(args As String())
        D$$im z As CustomClass
    End Sub	
    Private Class CustomClass
    End Class
End Module
</Text>.NormalizedValue,
            MainDescription("Class Program.CustomClass"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDefaultProperty1() As Task
            Await TestAsync(<Text>
Class X
    Public ReadOnly Property Goo As Y
        Get
            Return Nothing
        End Get
    End Property
End Class
Class Y
    Public Default ReadOnly Property Item(ByVal a As Integer) As String
        Get
            Return "hi"
        End Get
    End Property
End Class
Module M1
    Sub Main()
        Dim a As String
        Dim b As X
        b = New X()
        a = b.G$$oo(4)
    End Sub
End Module


</Text>.NormalizedValue,
            MainDescription("ReadOnly Property X.Goo As Y"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDefaultProperty2() As Task
            Await TestAsync(<Text>
Class X
    Public ReadOnly Property Goo As Y
        Get
            Return Nothing
        End Get
    End Property
End Class
Class Y
    Public Default ReadOnly Property Item(ByVal a As Integer) As String
        Get
            Return "hi"
        End Get
    End Property
End Class
Module M1
    Sub Main()
        Dim a As String
        Dim b As X
        b = New X()
        a = b.Goo.I$$tem(4)
    End Sub
End Module


</Text>.NormalizedValue,
            MainDescription("ReadOnly Property Y.Item(a As Integer) As String"))
        End Function

        <WorkItem(541582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541582")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestLambdaExpression() As Task
            Await TestAsync(<Text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Sub Main(ByVal args As String())
        Dim increment2 As Func(Of Integer, UInt16) = Function(x42)$$
                                                         Return x42 + 2
                                                     End Function
    End Sub
End Module</Text>.NormalizedValue, Nothing)
        End Function

        <WorkItem(541353, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541353")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestUnboundMethodInvocation() As Task
            Await TestInMethodAsync("Me.Go$$o()", Nothing)
        End Function

        <WorkItem(541582, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541582")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestQuickInfoOnExtensionMethod() As Task
            Await TestAsync(<Text><![CDATA[Imports System.Runtime.CompilerServices
Class Program
    Private Shared Sub Main(args As String())
        Dim values As Integer() = New Integer() {1}
        Dim isArray As Boolean = 7.Co$$unt(values)
    End Sub
End Class

Module MyExtensions
    <Extension> _
    Public Function Count(Of T)(o As T, items As IEnumerable(Of T)) As Boolean
        Return True
    End Function
End Module]]></Text>.NormalizedValue,
            MainDescription($"<{VBFeaturesResources.Extension}> Function Integer.Count(items As IEnumerable(Of Integer)) As Boolean"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestQuickInfoOnExtensionMethodOverloads() As Task
            Await TestAsync(<Text><![CDATA[Imports System.Runtime.CompilerServices
Class Program
    Private Shared Sub Main(args As String())
        Dim i as string = "1"
        i.Test$$Ext()
    End Sub
End Class

Module Ex
    <Extension()>
    Public Sub TestExt(Of T)(ex As T)
    End Sub
    <Extension()>
    Public Sub TestExt(Of T)(ex As T, arg As T)
    End Sub
    <Extension()>
    Public Sub TestExt(ex As String, arg As Integer)
    End Sub
End Module]]></Text>.NormalizedValue,
            MainDescription($"<{VBFeaturesResources.Extension}> Sub String.TestExt() (+ 2 {FeaturesResources.overloads_})"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestQuickInfoOnExtensionMethodOverloads2() As Task
            Await TestAsync(<Text><![CDATA[Imports System.Runtime.CompilerServices
Class Program
    Private Shared Sub Main(args As String())
        Dim i as string = "1"
        i.Test$$Ext()
    End Sub
End Class

Module Ex
    <Extension()>
    Public Sub TestExt(Of T)(ex As T)
    End Sub
    <Extension()>
    Public Sub TestExt(Of T)(ex As T, arg As T)
    End Sub
    <Extension()>
    Public Sub TestExt(ex As Integer, arg As Integer)
    End Sub
End Module]]></Text>.NormalizedValue,
            MainDescription($"<{VBFeaturesResources.Extension}> Sub String.TestExt() (+ 1 {FeaturesResources.overload})"))
        End Function

        <WorkItem(541960, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541960")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDontRemoveAttributeSuffixAndProduceInvalidIdentifier1() As Task
            Await TestAsync(<Text><![CDATA[
Imports System
Class _Attribute
    Inherits Attribute

    Dim x$$ As _Attribute
End Class]]></Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.field}) _Attribute.x As _Attribute"))
        End Function

        <WorkItem(541960, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541960")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDontRemoveAttributeSuffixAndProduceInvalidIdentifier2() As Task
            Await TestAsync(<Text><![CDATA[
Imports System
Class ClassAttribute
    Inherits Attribute

    Dim x$$ As ClassAttribute
End Class]]></Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.field}) ClassAttribute.x As ClassAttribute"))
        End Function

        <WorkItem(541960, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541960")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDontRemoveAttributeSuffix1() As Task
            Await TestAsync(<Text><![CDATA[
Imports System
Class Class1Attribute
    Inherits Attribute

    Dim x$$ As Class1Attribute
End Class]]></Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.field}) Class1Attribute.x As Class1Attribute"))
        End Function

        <WorkItem(1696, "https://github.com/dotnet/roslyn/issues/1696")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAttributeQuickInfoBindsToClassTest() As Task
            Await TestAsync("
Imports System

''' <summary>
''' class comment
''' </summary>
<Some$$>
Class SomeAttribute
    Inherits Attribute

    ''' <summary>
    ''' ctor comment
    ''' </summary>
    Public Sub New()
    End Sub
End Class
",
                Documentation("class comment"))
        End Function

        <WorkItem(1696, "https://github.com/dotnet/roslyn/issues/1696")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAttributeConstructorQuickInfo() As Task
            Await TestAsync("
Imports System

''' <summary>
''' class comment
''' </summary>
Class SomeAttribute
    Inherits Attribute

    ''' <summary>
    ''' ctor comment
    ''' </summary>
    Public Sub New()
        Dim s = New Some$$Attribute()
    End Sub
End Class
",
                Documentation("ctor comment"))
        End Function

        <WorkItem(542613, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542613")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestUnboundGeneric() As Task
            Await TestAsync(<Text><![CDATA[
Imports System
Imports System.Collections.Generic
Class C
    Sub M()
        Dim t As Type = GetType(L$$ist(Of ))
    End Sub
End Class]]></Text>.NormalizedValue,
            MainDescription("Class System.Collections.Generic.List(Of T)"),
            NoTypeParameterMap)
        End Function

        <WorkItem(543209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543209")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestQuickInfoForAnonymousType1() As Task
            Await TestAsync(<Text><![CDATA[
Class C
    Sub S
        Dim product = $$New With {Key .Name = "", Key .Price = 0}
    End Sub
End Class]]></Text>.NormalizedValue,
            MainDescription("AnonymousType 'a"),
            NoTypeParameterMap,
            AnonymousTypes(vbCrLf & FeaturesResources.Anonymous_Types_colon & vbCrLf & $"    'a {FeaturesResources.is_} New With {{ Key .Name As String, Key .Price As Integer }}"))
        End Function

        <WorkItem(543226, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543226")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestQuickInfoForAnonymousType2() As Task
            Await TestAsync(<Text><![CDATA[
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim product = New With {Key .Name = "", Key .Price = 0}
        Dim products = Enumerable.Repeat(product, 1)
        Dim namePriceQuery = From prod In products
                             Select prod.$$Name, prod.Price
    End Sub
End Module]]></Text>.NormalizedValue,
            MainDescription("ReadOnly Property 'a.Name As String"),
            NoTypeParameterMap,
            AnonymousTypes(vbCrLf & FeaturesResources.Anonymous_Types_colon & vbCrLf & $"    'a {FeaturesResources.is_} New With {{ Key .Name As String, Key .Price As Integer }}"))
        End Function

        <WorkItem(543223, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543223")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestQuickInfoForAnonymousType3() As Task
            Await TestAsync(<Text><![CDATA[
Class C
    Sub S
        Dim x = $$New With {Key .Goo = x}
    End Sub
End Class
]]></Text>.NormalizedValue,
            MainDescription("AnonymousType 'a"),
            NoTypeParameterMap,
            AnonymousTypes(vbCrLf & FeaturesResources.Anonymous_Types_colon & vbCrLf & $"    'a {FeaturesResources.is_} New With {{ Key .Goo As ? }}"))
        End Function

        <WorkItem(543242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543242")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestQuickInfoForUnboundLabel() As Task
            Await TestAsync(<Text><![CDATA[
Option Infer On
Option Strict On
Public Class D
    Public Sub goo()
        GoTo $$oo
    End Sub
End Class]]></Text>.NormalizedValue,
            Nothing)
        End Function

        <WorkItem(543624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543624")>
        <WorkItem(543275, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543275")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestQuickInfoForAnonymousDelegate1() As Task
            Await TestAsync(<Text><![CDATA[
Imports System

Module Program
    Sub Main
        Dim $$a = Sub() Return
    End Sub
End Module
]]></Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.local_variable}) a As <Sub()>"))
        End Function

        <WorkItem(543624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543624")>
        <WorkItem(543275, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543275")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestQuickInfoForAnonymousDelegate2() As Task
            Await TestAsync(<Text><![CDATA[
Imports System

Module Program
    Sub Main
        Dim $$a = Function() 1
    End Sub
End Module
]]></Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.local_variable}) a As <Function() As Integer>"))
        End Function

        <WorkItem(543624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543624")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestQuickInfoForAnonymousDelegate3() As Task
            Await TestAsync(<Text><![CDATA[
Imports System

Module Program
    Sub Main
        Dim $$a = Function() New With {.Goo = "Goo"}
    End Sub
End Module
]]></Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.local_variable}) a As <Function() As 'a>"),
            AnonymousTypes(vbCrLf & FeaturesResources.Anonymous_Types_colon & vbCrLf &
                           $"    'a {FeaturesResources.is_} New With {{ .Goo As String }}"))
        End Function

        <WorkItem(543624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543624")>
        <WorkItem(543275, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543275")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestQuickInfoForAnonymousDelegate4() As Task
            Await TestAsync(<Text><![CDATA[
Imports System

Module Program
    Sub Main
        Dim $$a = Function(i As Integer) New With {.Sq = i * i, .M = Function(j As Integer) i * i}
    End Sub
End Module
]]></Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.local_variable}) a As <Function(i As Integer) As 'a>"),
            AnonymousTypes(vbCrLf & FeaturesResources.Anonymous_Types_colon & vbCrLf &
                           $"    'a {FeaturesResources.is_} New With {{ .Sq As Integer, .M As <Function(j As Integer) As Integer> }}"))
        End Function

        <WorkItem(543389, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543389")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestImplicitMemberNameLocal1() As Task
            Await TestAsync(<Text><![CDATA[
Imports System

Module Program
    ReadOnly Property Prop As Long
        Get
            Pr$$op = 1
            Dim a = New With {.id = Prop}
            Return 1
        End Get
    End Property
End Module
]]></Text>.NormalizedValue,
            MainDescription("ReadOnly Property Program.Prop As Long"))
        End Function

        <WorkItem(543389, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543389")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestImplicitMemberNameLocal2() As Task
            Await TestAsync(<Text><![CDATA[
Imports System

Module Program
    ReadOnly Property Prop As Long
        Get
            Prop = 1
            Dim a = New With {.id = Pr$$op}
            Return 1
        End Get
    End Property
End Module
]]></Text>.NormalizedValue,
            MainDescription("ReadOnly Property Program.Prop As Long"))
        End Function

        <WorkItem(543389, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543389")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestImplicitMemberNameLocal3() As Task
            Await TestAsync(<Text><![CDATA[
Imports System

Module Program
    Function Goo() As Integer
        Go$$o = 1
    End Function
End Module
]]></Text>.NormalizedValue,
            MainDescription("Function Program.Goo() As Integer"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestBinaryConditionalExpression() As Task
            Await TestInMethodAsync("Dim x = If$$(True, False)",
                MainDescription($"If({VBWorkspaceResources.expression}, {VBWorkspaceResources.expressionIfNothing}) As Boolean"),
                Documentation(VBWorkspaceResources.If_expression_evaluates_to_a_reference_or_Nullable_value_that_is_not_Nothing_the_function_returns_that_value_Otherwise_it_calculates_and_returns_expressionIfNothing))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestTernaryConditionalExpression() As Task
            Await TestInMethodAsync("Dim x = If$$(True, ""Goo"", ""Bar"")",
                MainDescription($"If({VBWorkspaceResources.condition} As Boolean, {VBWorkspaceResources.expressionIfTrue}, {VBWorkspaceResources.expressionIfFalse}) As String"),
                Documentation(VBWorkspaceResources.If_condition_returns_True_the_function_calculates_and_returns_expressionIfTrue_Otherwise_it_returns_expressionIfFalse))
        End Function

        <WorkItem(957082, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/957082")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAddHandlerStatement() As Task
            Await TestInMethodAsync("$$AddHandler goo, bar",
                MainDescription($"AddHandler {VBWorkspaceResources.event_}, {VBWorkspaceResources.handler}"),
                Documentation(VBWorkspaceResources.Associates_an_event_with_an_event_handler_delegate_or_lambda_expression_at_run_time),
                SymbolGlyph(Glyph.Keyword))
        End Function

        <WorkItem(957082, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/957082")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestRemoveHandlerStatement() As Task
            Await TestInMethodAsync("$$RemoveHandler goo, bar",
                MainDescription($"RemoveHandler {VBWorkspaceResources.event_}, {VBWorkspaceResources.handler}"),
                Documentation(VBWorkspaceResources.Removes_the_association_between_an_event_and_an_event_handler_or_delegate_at_run_time),
                SymbolGlyph(Glyph.Keyword))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestGetTypeExpression() As Task
            Await TestInMethodAsync("Dim x = GetType$$(String)",
                MainDescription("GetType(String) As Type"),
                Documentation(VBWorkspaceResources.Returns_a_System_Type_object_for_the_specified_type_name))
        End Function

        <WorkItem(544140, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544140")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestGetXmlNamespaceExpression() As Task
            Await TestWithReferencesAsync(
                <text>
class C
    sub M()
        Dim x = GetXmlNamespace$$()
    end sub()
end class
                </text>.NormalizedValue,
                {GetType(System.Xml.XmlAttribute).Assembly.Location, GetType(System.Xml.Linq.XAttribute).Assembly.Location},
                MainDescription($"GetXmlNamespace([{VBWorkspaceResources.xmlNamespacePrefix}]) As Xml.Linq.XNamespace"),
                Documentation(VBWorkspaceResources.Returns_the_System_Xml_Linq_XNamespace_object_corresponding_to_the_specified_XML_namespace_prefix))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestTryCastExpression() As Task
            Await TestInMethodAsync("Dim x = TryCast$$(a, String)",
                MainDescription($"TryCast({VBWorkspaceResources.expression}, String) As String"),
                Documentation(VBWorkspaceResources.Introduces_a_type_conversion_operation_that_does_not_throw_an_exception_If_an_attempted_conversion_fails_TryCast_returns_Nothing_which_your_program_can_test_for))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDirectCastExpression() As Task
            Await TestInMethodAsync("Dim x = DirectCast$$(a, String)",
                MainDescription($"DirectCast({VBWorkspaceResources.expression}, String) As String"),
                Documentation(VBWorkspaceResources.Introduces_a_type_conversion_operation_similar_to_CType_The_difference_is_that_CType_succeeds_as_long_as_there_is_a_valid_conversion_whereas_DirectCast_requires_that_one_type_inherit_from_or_implement_the_other_type))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCTypeCastExpression() As Task
            Await TestInMethodAsync("Dim x = CType$$(a, String)",
                MainDescription($"CType({VBWorkspaceResources.expression}, String) As String"),
                Documentation(VBWorkspaceResources.Returns_the_result_of_explicitly_converting_an_expression_to_a_specified_data_type))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCBoolExpression() As Task
            Await TestInMethodAsync("Dim x = CBool$$(a)",
                MainDescription($"CBool({VBWorkspaceResources.expression}) As Boolean"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Boolean")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCByteExpression() As Task
            Await TestInMethodAsync("Dim x = CByte$$(a)",
                MainDescription($"CByte({VBWorkspaceResources.expression}) As Byte"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Byte")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCCharExpression() As Task
            Await TestInMethodAsync("Dim x = CChar$$(a)",
                MainDescription($"CChar({VBWorkspaceResources.expression}) As Char"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Char")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCDateExpression() As Task
            Await TestInMethodAsync("Dim x = CDate$$(a)",
                MainDescription($"CDate({VBWorkspaceResources.expression}) As Date"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Date")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCDblExpression() As Task
            Await TestInMethodAsync("Dim x = CDbl$$(a)",
                MainDescription($"CDbl({VBWorkspaceResources.expression}) As Double"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Double")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCDecExpression() As Task
            Await TestInMethodAsync("Dim x = CDec$$(a)",
                MainDescription($"CDec({VBWorkspaceResources.expression}) As Decimal"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Decimal")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCIntExpression() As Task
            Await TestInMethodAsync("Dim x = CInt$$(a)",
                MainDescription($"CInt({VBWorkspaceResources.expression}) As Integer"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Integer")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCLngExpression() As Task
            Await TestInMethodAsync("Dim x = CLng$$(a)",
                MainDescription($"CLng({VBWorkspaceResources.expression}) As Long"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Long")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCObjExpression() As Task
            Await TestInMethodAsync("Dim x = CObj$$(a)",
                MainDescription($"CObj({VBWorkspaceResources.expression}) As Object"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Object")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCSByteExpression() As Task
            Await TestInMethodAsync("Dim x = CSByte$$(a)",
                MainDescription($"CSByte({VBWorkspaceResources.expression}) As SByte"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "SByte")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCShortExpression() As Task
            Await TestInMethodAsync("Dim x = CShort$$(a)",
                MainDescription($"CShort({VBWorkspaceResources.expression}) As Short"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Short")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCSngExpression() As Task
            Await TestInMethodAsync("Dim x = CSng$$(a)",
                MainDescription($"CSng({VBWorkspaceResources.expression}) As Single"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "Single")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCStrExpression() As Task
            Await TestInMethodAsync("Dim x = CStr$$(a)",
                MainDescription($"CStr({VBWorkspaceResources.expression}) As String"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "String")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCUIntExpression() As Task
            Await TestInMethodAsync("Dim x = CUInt$$(a)",
                MainDescription($"CUInt({VBWorkspaceResources.expression}) As UInteger"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "UInteger")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCULngExpression() As Task
            Await TestInMethodAsync("Dim x = CULng$$(a)",
                MainDescription($"CULng({VBWorkspaceResources.expression}) As ULong"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "ULong")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCUShortExpression() As Task
            Await TestInMethodAsync("Dim x = CUShort$$(a)",
                MainDescription($"CUShort({VBWorkspaceResources.expression}) As UShort"),
                Documentation(String.Format(VBWorkspaceResources.Converts_an_expression_to_the_0_data_type, "UShort")))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestMidAssignmentStatement1() As Task
            Await TestInMethodAsync("$$Mid(""goo"", 0) = ""bar""",
                MainDescription($"Mid({VBWorkspaceResources.stringName}, {VBWorkspaceResources.startIndex}, [{VBWorkspaceResources.length}]) = {VBWorkspaceResources.stringExpression}"),
                Documentation(VBWorkspaceResources.Replaces_a_specified_number_of_characters_in_a_String_variable_with_characters_from_another_string))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestMidAssignmentStatement2() As Task
            Await TestInMethodAsync("$$Mid(""goo"", 0, 0) = ""bar""",
                MainDescription($"Mid({VBWorkspaceResources.stringName}, {VBWorkspaceResources.startIndex}, [{VBWorkspaceResources.length}]) = {VBWorkspaceResources.stringExpression}"),
                Documentation(VBWorkspaceResources.Replaces_a_specified_number_of_characters_in_a_String_variable_with_characters_from_another_string))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConstantField() As Task
            Await TestInClassAsync("const $$F = 1",
                MainDescription($"({FeaturesResources.constant}) C.F As Integer = 1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestMultipleConstantFields() As Task
            Await TestInClassAsync("Public Const X As Double = 1.0, Y As Double = 2.0, $$Z As Double = 3.5",
                MainDescription($"({FeaturesResources.constant}) C.Z As Double = 3.5"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConstantDependencies() As Task
            Await TestAsync(<Text><![CDATA[
Imports System

Class A
    Public Const $$X As Integer = B.Z + 1
    Public Const Y As Integer = 10
End Class
Class B
    Public Const Z As Integer = A.Y + 1
End Class
]]></Text>.NormalizedValue,
                MainDescription($"({FeaturesResources.constant}) A.X As Integer = B.Z + 1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConstantCircularDependencies() As Task
            Await TestAsync(<Text><![CDATA[
Imports System

Class A
    Public Const $$X As Integer = B.Z + 1
End Class
Class B
    Public Const Z As Integer = A.X + 1
End Class
]]></Text>.NormalizedValue,
                MainDescription($"({FeaturesResources.constant}) A.X As Integer = B.Z + 1"))
        End Function

        <WorkItem(544620, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544620")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConstantOverflow() As Task
            Await TestInClassAsync("Public Const $$Z As Integer = Integer.MaxValue + 1",
                MainDescription($"({FeaturesResources.constant}) C.Z As Integer = Integer.MaxValue + 1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestEnumInConstantField() As Task
            Await TestAsync(<Text><![CDATA[
Public Class EnumTest
    Private Enum Days
        Sun
        Mon
        Tue
        Wed
        Thu
        Fri
        Sat
    End Enum
    Private Shared Sub Main()
        Const $$x As Integer = CInt(Days.Sun)
    End Sub
End Class
]]></Text>.NormalizedValue,
                MainDescription($"({FeaturesResources.local_constant}) x As Integer = CInt(Days.Sun)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestEnumInConstantField2() As Task
            Await TestAsync(<Text><![CDATA[
Public Class EnumTest
    Private Enum Days
        Sun
        Mon
        Tue
        Wed
        Thu
        Fri
        Sat
    End Enum
    Private Shared Sub Main()
        Const $$x As Days = Days.Sun
    End Sub
End Class
]]></Text>.NormalizedValue,
                MainDescription($"({FeaturesResources.local_constant}) x As Days = Days.Sun"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConstantParameter() As Task
            Await TestInClassAsync("Sub Bar(optional $$b as Integer = 1)",
                MainDescription($"({FeaturesResources.parameter}) b As Integer = 1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestConstantLocal() As Task
            Await TestInMethodAsync("const $$loc = 1",
                MainDescription($"({FeaturesResources.local_constant}) loc As Integer = 1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestEnumValue1() As Task
            Await TestInMethodAsync("Const $$sunday = DayOfWeek.Sunday",
                MainDescription($"({FeaturesResources.local_constant}) sunday As DayOfWeek = DayOfWeek.Sunday"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestEnumValue2() As Task
            Await TestInMethodAsync("Const $$v = AttributeTargets.Constructor or AttributeTargets.Class",
                MainDescription($"({FeaturesResources.local_constant}) v As AttributeTargets = AttributeTargets.Constructor or AttributeTargets.Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestComplexConstantParameter() As Task
            Await TestInClassAsync("Sub Bar(optional $$b as Integer = 1 + True)",
                MainDescription($"({FeaturesResources.parameter}) b As Integer = 1 + True"))
        End Function

        <WorkItem(546849, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546849")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestIndexedPropertyWithOptionalParameter() As Task
            Await TestAsync(<Text><![CDATA[
Class Test
    Public Property Prop(p1 As Integer, Optional p2 As Integer = 0) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)

        End Set
    End Property
    Sub Goo()
        Dim x As New Test
        x.Pr$$op(0) = 0
    End Sub
End Class
]]></Text>.NormalizedValue,
                 MainDescription("Property Test.Prop(p1 As Integer, [p2 As Integer = 0]) As Integer"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAwaitableMethod() As Task
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports System.Threading.Tasks

Class C
    Async Function goo() As Task
        go$$o()
    End Function
End Class
                                 </Document>
                             </Project>
                         </Workspace>.ToString()

            Dim description = <File>&lt;<%= VBFeaturesResources.Awaitable %>&gt; Function C.goo() As Task</File>.ConvertTestSourceTag()

            Dim doc = StringFromLines("", WorkspacesResources.Usage_colon, $"  {SyntaxFacts.GetText(SyntaxKind.AwaitKeyword)} goo()")

            Await TestFromXmlAsync(markup,
                 MainDescription(description), Usage(doc))
        End Function

        <WorkItem(7100, "https://github.com/dotnet/roslyn/issues/7100")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestObjectWithOptionStrictOffIsntAwaitable() As Task
            Dim markup = "
Option Strict Off
Class C
    Function D() As Object
        Return Nothing
    End Function
    Sub M()
        D$$()
    End Sub
End Class
"
            Await TestAsync(markup, MainDescription("Function C.D() As Object"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestObsoleteItem() As Task
            Await TestAsync(<Text><![CDATA[
Imports System

Class C
    <Obsolete>
    Sub Goo()
        Go$$o()
    End Sub
End Class
]]></Text>.NormalizedValue,
                MainDescription($"({VBFeaturesResources.Deprecated}) Sub C.Goo()"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestEnumMemberNameFromMetadata() As Task
            Dim code =
<Code>
Imports System

Class C
    Sub M()
        Dim c = ConsoleColor.Bla$$ck
    End Sub
End Class
</Code>.NormalizedValue()

            Await TestAsync(code,
                MainDescription("ConsoleColor.Black = 0"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestEnumMemberNameFromSource1() As Task
            Dim code =
<Code>
Enum Goo
    A = 1 &lt;&lt; 0
    B = 1 &lt;&lt; 1
    C = 1 &lt;&lt; 2
End Enum

Class C
    Sub M()
        Dim e = Goo.B$$
    End Sub
End Class
</Code>.NormalizedValue()

            Await TestAsync(code,
                MainDescription("Goo.B = 1 << 1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestEnumMemberNameFromSource2() As Task
            Dim code =
<Code>
Enum Goo
    A
    B
    C
End Enum

Class C
    Sub M()
        Dim e = Goo.B$$
    End Sub
End Class
</Code>.NormalizedValue()

            Await TestAsync(code,
                MainDescription("Goo.B = 1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestTextOnlyDocComment() As Task
            Await TestAsync(<text><![CDATA[
''' <summary>
        '''goo
        ''' </summary>
Class C$$
End Class]]></text>.NormalizedValue(), Documentation("goo"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestTrimConcatMultiLine() As Task
            Await TestAsync(<text><![CDATA[
''' <summary>
        ''' goo
        ''' bar
        ''' </summary>
Class C$$
End Class]]></text>.NormalizedValue(), Documentation("goo bar"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCref() As Task
            Await TestAsync(<text><![CDATA[
''' <summary>
        ''' <see cref="C"/>
        ''' <seealso cref="C"/>
        ''' </summary>
Class C$$
End Class]]></text>.NormalizedValue(), Documentation("C C"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestExcludeTextOutsideSummaryBlock() As Task
            Await TestAsync(<text><![CDATA[
''' red
        ''' <summary>
        ''' green
        ''' </summary>
        ''' yellow
Class C$$
End Class]]></text>.NormalizedValue(), Documentation("green"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestNewlineAfterPara() As Task
            Await TestAsync(<text><![CDATA[
''' <summary>
        ''' <para>goo</para>
        ''' </summary>
Class C$$
End Class]]></text>.NormalizedValue(), Documentation("goo"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestParam() As Task
            Await TestAsync(<text><![CDATA[
''' <summary></summary>
Public Class C
            ''' <typeparam name="T">A type parameter of <see cref="Goo(Of T) (string(), T)"/></typeparam>
            ''' <param name="args">First parameter of <see cref="Goo(Of T) (string(), T)"/></param>
            ''' <param name="otherParam">Another parameter of <see cref="Goo(Of T)(string(), T)"/></param>
            Public Function Goo(Of T)(arg$$s As String(), otherParam As T)
    End Function
        End Class]]></text>.NormalizedValue(), Documentation("First parameter of C.Goo(Of T)(String(), T)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestParam2() As Task
            Await TestAsync(<text><![CDATA[
''' <summary></summary>
Public Class C
            ''' <typeparam name="T">A type parameter of <see cref="Goo(Of T) (string(), T)"/></typeparam>
            ''' <param name="args">First parameter of <see cref="Goo(Of T) (string(), T)"/></param>
            ''' <param name="otherParam">Another parameter of <see cref="Goo(Of T)(string(), T)"/></param>
            Public Function Goo(Of T)(args As String(), otherP$$aram As T)
            End Function
        End Class]]></text>.NormalizedValue(), Documentation("Another parameter of C.Goo(Of T)(String(), T)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestTypeParam() As Task
            Await TestAsync(<text><![CDATA[
''' <summary></summary>
Public Class C
            ''' <typeparam name="T">A type parameter of <see cref="Goo(Of T) (string(), T)"/></typeparam>
            ''' <param name="args">First parameter of <see cref="Goo(Of T) (string(), T)"/></param>
            ''' <param name="otherParam">Another parameter of <see cref="Goo(Of T)(string(), T)"/></param>
            Public Function Goo(Of T$$)( args as String(), otherParam as T)
    End Function
        End Class]]></text>.NormalizedValue(), Documentation("A type parameter of C.Goo(Of T)(String(), T)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestUnboundCref() As Task
            Await TestAsync(<text><![CDATA[
''' <summary></summary>
Public Class C
            ''' <typeparam name="T">A type parameter of <see cref="goo(Of T) (string, T)"/></typeparam>
            Public Function Goo(Of T$$)( args as String(), otherParam as T)
    End Function
        End Class]]></text>.NormalizedValue(), Documentation("A type parameter of goo(Of T) (string, T)"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCrefInConstructor() As Task
            Await TestAsync(<text><![CDATA[
Public Class TestClass
            ''' <summary>
            ''' This sample shows how to specify the <see cref="TestClass"/> constructor as a cref attribute.
            ''' </summary>
            Public Sub N$$ew()
    End Sub
        End Class]]></text>.NormalizedValue(), Documentation("This sample shows how to specify the TestClass constructor as a cref attribute."))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCrefInConstructorOverloaded() As Task
            Await TestAsync(<text><![CDATA[
Public Class TestClass
            Public Sub New()
            End Function
            ''' <summary>
            ''' This sample shows how to specify the <see cref="TestClass.New(Integer)"/> constructor as a cref attribute.
            ''' </summary>
            Public Sub Ne$$w(value As Integer)
    End Sub
        End Class]]></text>.NormalizedValue(), Documentation("This sample shows how to specify the New(Integer) constructor as a cref attribute."))
        End Function

        <WorkItem(814191, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/814191")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCrefInGenericMethod1() As Task
            Await TestAsync(<text><![CDATA[
Public Class TestClass
            ''' <summary>
            ''' This sample shows how to specify the <see cref="GetGenericValue"/> method as a cref attribute.
            ''' </summary>
            Public Shared Function GetGe$$nericValue(Of T)(para As T) As T
        Return para
            End Function
        End Class]]></text>.NormalizedValue(), Documentation("This sample shows how to specify the TestClass.GetGenericValue(Of T)(T) method as a cref attribute."))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCrefInGenericMethod2() As Task
            Await TestAsync(<text><![CDATA[
Public class TestClass
    ''' <summary>
    ''' This sample shows how to specify the <see cref="GetGenericValue(OfT)(T)"/> method as a cref attribute.
    ''' </summary>
    Public Shared Function GetGe$$nericValue(Of T)(para As T) As T
        Return para
    End Function
End Class]]></text>.NormalizedValue(), Documentation("This sample shows how to specify the GetGenericValue(OfT)(T) method as a cref attribute."))
        End Function

        <WorkItem(813350, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/813350")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCrefInMethodOverloading1() As Task
            Await TestAsync(<text><![CDATA[
public class TestClass
    Public Shared Function GetZero() As Integer
        GetGenericVa$$lue()
        Return GetGenericValue(5)
    End Function

    ''' <summary>
    ''' This sample shows how to specify the <see cref="GetGenericValue(OfT)(T)"/> method as a cref attribute.
    ''' </summary>
    Public Shared Function GetGenericValue(Of T)(para As T) As T
        Return para
    End Function

    ''' <summary>
    ''' This sample shows how to specify the <see cref="GetGenericValue()"/> method as a cref attribute.
    ''' </summary>
    Public Shared Sub GetGenericValue()
    End Sub
End Class]]></text>.NormalizedValue(), Documentation("This sample shows how to specify the TestClass.GetGenericValue() method as a cref attribute."))
        End Function

        <WorkItem(813350, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/813350")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCrefInMethodOverloading2() As Task
            Await TestAsync(<text><![CDATA[
public class TestClass
    Public Shared Function GetZero() As Integer
        GetGenericValue()
        Return GetGe$$nericValue(5)
    End Function

    ''' <summary>
    ''' This sample shows how to specify the <see cref="GetGenericValue(OfT)(T)"/> method as a cref attribute.
    ''' </summary>
    Public Shared Function GetGenericValue(Of T)(para As T) As T
        Return para
    End Function

    ''' <summary>
    ''' This sample shows how to specify the <see cref="GetGenericValue()"/> method as a cref attribute.
    ''' </summary>
    Public Shared Sub GetGenericValue()
    End Sub
End Class]]></text>.NormalizedValue(), Documentation("This sample shows how to specify the GetGenericValue(OfT)(T) method as a cref attribute."))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestCrefInGenericType() As Task
            Await TestAsync(<text><![CDATA[
''' <summary>
''' This sample shows how to specify the <see cref="GenericClass(Of T)"/> cref.
''' </summary>
Public Class Generic$$Class(Of T)
End Class]]></text>.NormalizedValue(),
                 Documentation("This sample shows how to specify the GenericClass(Of T) cref.",
                    ExpectedClassifications(
                        Text("This sample shows how to specify the"),
                        WhiteSpace(" "),
                        [Class]("GenericClass"),
                        Punctuation.OpenParen,
                        Keyword("Of"),
                        WhiteSpace(" "),
                        TypeParameter("T"),
                        Punctuation.CloseParen,
                        WhiteSpace(" "),
                        Text("cref."))))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestIntegerLiteral() As Task
            Await TestInMethodAsync("Dim f = 37$$",
                                    MainDescription("Structure System.Int32"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDateLiteral() As Task
            Await TestInMethodAsync("Dim f = #8/23/1970 $$3:45:39 AM#",
                                    MainDescription("Structure System.DateTime"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestTrueKeyword() As Task
            Await TestInMethodAsync("Dim f = True$$",
                                    MainDescription("Structure System.Boolean"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestFalseKeyword() As Task
            Await TestInMethodAsync("Dim f = False$$",
                                    MainDescription("Structure System.Boolean"))
        End Function

        <WorkItem(26027, "https://github.com/dotnet/roslyn/issues/26027")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestNothingLiteral() As Task
            Await TestInMethodAsync("Dim f As String = Nothing$$",
                                    MainDescription("Class System.String"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestNothingLiteralWithNoType() As Task
            Await TestInMethodAsync("Dim f = Nothing$$",
                                    MainDescription("Class System.Object"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestNothingLiteralFieldDimOptionStrict() As Task
            Await TestAsync("
Option Strict On
Class C
    Dim f = Nothing$$
End Class",
                            MainDescription("Class System.Object"))
        End Function

        ''' <Remarks>
        ''' As a part of fix for 756226, quick info for VB Await keyword now displays the type inferred from the AwaitExpression. This is C# behavior.
        ''' In Dev12, quick info for VB Await keyword was the syntactic help "Await &lt;expression&gt;".
        ''' In Roslyn, VB Syntactic quick info is Not yet Implemented. User story: 522342. 
        ''' While implementing this story, determine the correct behavior for quick info on VB Await keyword (syntactic vs semantic) and update these tests.
        ''' </Remarks>
        <WorkItem(756226, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226"), WorkItem(522342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522342")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAwaitKeywordOnTaskReturningAsync() As Task
            Dim markup =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferencesNet45="true">
                        <Document FilePath="SourceDocument">
Imports System.Threading.Tasks

Class C
    Async Function goo() As Task
        Aw$$ait goo()
    End Function
End Class
        </Document>
                    </Project>
                </Workspace>.ToString()

            Dim description = <File><%= FeaturesResources.Awaited_task_returns %><%= " " %><%= FeaturesResources.no_value %></File>.ConvertTestSourceTag()

            Await TestFromXmlAsync(markup, MainDescription(description))
        End Function

        <WorkItem(756226, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226"), WorkItem(522342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522342")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAwaitKeywordOnGenericTaskReturningAsync() As Task
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports System.Threading.Tasks

Class C
    Async Function goo() As Task(Of Integer)
        Dim x = Aw$$ait goo()
        Return 42
    End Function
End Class
                                 </Document>
                             </Project>
                         </Workspace>.ToString()

            Dim description = <File><%= FeaturesResources.Awaited_task_returns %> Structure System.Int32</File>.ConvertTestSourceTag()

            Await TestFromXmlAsync(markup, MainDescription(description))
        End Function

        <WorkItem(756226, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226"), WorkItem(522342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522342")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAwaitKeywordOnTaskReturningAsync2() As Task
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports System.Threading.Tasks

Class C
    Async Sub Goo()
        Aw$$ait Task.Delay(10)
    End Sub
End Class
        </Document>
                             </Project>
                         </Workspace>.ToString()

            Dim description = <File><%= FeaturesResources.Awaited_task_returns %><%= " " %><%= FeaturesResources.no_value %></File>.ConvertTestSourceTag()

            Await TestFromXmlAsync(markup, MainDescription(description))
        End Function

        <WorkItem(756226, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226"), WorkItem(522342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522342")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestNestedAwaitKeywords1() As Task
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports System
Imports System.Threading.Tasks

Class AsyncExample
    Async Function AsyncMethod() As Task(Of Task(Of Integer))
        Return NewMethod()
    End Function

    Private Shared Function NewMethod() As Task(Of Integer)
        Throw New NotImplementedException()
    End Function

    Async Function UseAsync() As Task
        Dim lambda As Func(Of Task(Of Integer)) = Async Function()
                                                      Return Await Await AsyncMethod()
                                                  End Function
        Dim result = Await Await AsyncMethod()
        Dim resultTask As Task(Of Task(Of Integer)) = AsyncMethod()
        result = Await Awai$$t resultTask
        result = Await lambda()
    End Function
End Class
        </Document>
                             </Project>
                         </Workspace>.ToString()

            Dim description = <File>&lt;<%= VBFeaturesResources.Awaitable %>&gt; <%= FeaturesResources.Awaited_task_returns %> Class System.Threading.Tasks.Task(Of TResult)</File>.ConvertTestSourceTag()
            Await TestFromXmlAsync(markup, MainDescription(description), TypeParameterMap(vbCrLf & $"TResult {FeaturesResources.is_} Integer"))
        End Function

        <WorkItem(756226, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226"), WorkItem(522342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522342")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestNestedAwaitKeywords2() As Task
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports System
Imports System.Threading.Tasks

Class AsyncExample
    Async Function AsyncMethod() As Task(Of Task(Of Integer))
        Return NewMethod()
    End Function

    Private Shared Function NewMethod() As Task(Of Integer)
        Throw New NotImplementedException()
    End Function

    Async Function UseAsync() As Task
        Dim lambda As Func(Of Task(Of Integer)) = Async Function()
                                                      Return Await Await AsyncMethod()
                                                  End Function
        Dim result = Await Await AsyncMethod()
        Dim resultTask As Task(Of Task(Of Integer)) = AsyncMethod()
        result = Awai$$t Await resultTask
        result = Await lambda()
    End Function
End Class
        </Document>
                             </Project>
                         </Workspace>.ToString()

            Dim description = <File><%= FeaturesResources.Awaited_task_returns %> Structure System.Int32</File>.ConvertTestSourceTag()
            Await TestFromXmlAsync(markup, MainDescription(description))
        End Function

        <WorkItem(756226, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226"), WorkItem(756337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756337"), WorkItem(522342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522342")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestTaskType() As Task
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports System
Imports System.Threading.Tasks

Class AsyncExample
    Sub Goo()
        Dim v as Tas$$k = Nothing
    End Sub
End Class
        </Document>
                             </Project>
                         </Workspace>.ToString()

            Dim description = <File>&lt;<%= VBFeaturesResources.Awaitable %>&gt; Class System.Threading.Tasks.Task</File>.ConvertTestSourceTag()
            Await TestFromXmlAsync(markup, MainDescription(description))
        End Function

        <WorkItem(756226, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226"), WorkItem(756337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756337"), WorkItem(522342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522342")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestTaskOfTType() As Task
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports System
Imports System.Threading.Tasks

Class AsyncExample
    Sub Goo()
        Dim v as Tas$$k(Of Integer) = Nothing
    End Sub
End Class
        </Document>
                             </Project>
                         </Workspace>.ToString()

            Dim description = <File>&lt;<%= VBFeaturesResources.Awaitable %>&gt; Class System.Threading.Tasks.Task(Of TResult)</File>.ConvertTestSourceTag()
            Await TestFromXmlAsync(markup, MainDescription(description), TypeParameterMap(vbCrLf & $"TResult {FeaturesResources.is_} Integer"))
        End Function

        <WorkItem(756226, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756226"), WorkItem(756337, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/756337"), WorkItem(522342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/522342")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestAwaitablePrefixOnCustomAwaiter() As Task
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices

Module Program
    Sub Main(args As String())
        Dim x As C$$
    End Sub
End Module

Class C
    Public Function GetAwaiter() As MyAwaiter

    End Function

End Class

Public Class MyAwaiter
    Implements INotifyCompletion

    Public Property IsCompleted As Boolean
    Public Sub GetResult()

    End Sub
    Public Sub OnCompleted(continuation As Action) Implements INotifyCompletion.OnCompleted
        Throw New NotImplementedException()
    End Sub
End Class
        </Document>
                             </Project>
                         </Workspace>.ToString()

            Dim description = <File>&lt;<%= VBFeaturesResources.Awaitable %>&gt; Class C</File>.ConvertTestSourceTag()
            Await TestFromXmlAsync(markup, MainDescription(description))
        End Function

        <WorkItem(792629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/792629")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestGenericMethodWithConstraintsAtDeclaration() As Task
            Await TestInClassAsync("Private Function Go$$o(Of TIn As Class, TOut)(arg As TIn) As TOut
    Goo(Of TIn, TOut)(Nothing)
End Function",
             MainDescription("Function C.Goo(Of TIn As Class, TOut)(arg As TIn) As TOut"))
        End Function

        <WorkItem(792629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/792629")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestGenericMethodWithMultipleConstraintsAtDeclaration() As Task
            Await TestInClassAsync("Private Function Go$$o(Of TIn As {IComparable, New}, TOut)(arg As TIn) As TOut
    Goo(Of TIn, TOut)(Nothing)
End Function",
             MainDescription("Function C.Goo(Of TIn As {IComparable, New}, TOut)(arg As TIn) As TOut"))
        End Function

        <WorkItem(792629, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/792629")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestUnConstructedGenericMethodWithConstraintsAtInvocation() As Task
            Await TestInClassAsync("Private Function Goo(Of TIn As {Class, New}, TOut)(arg As TIn) As TOut
    G$$oo(Of TIn, TOut)(Nothing)
End Function",
             MainDescription("Function C.Goo(Of TIn As {Class, New}, TOut)(arg As TIn) As TOut"))
        End Function

        <WorkItem(991466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991466")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestDocumentationInImportsDirectiveWithAlias() As Task
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports I = IGoo
Class C
    Implements I$$

    Public Sub Bar() Implements IGoo.Bar
        Throw New NotImplementedException()
    End Sub
End Class

''' &lt;summary&gt;
''' summary for interface IGoo
''' &lt;/summary&gt;
Interface IGoo
    Sub Bar()
End Interface
        </Document>
                             </Project>
                         </Workspace>.ToString()

            Await TestFromXmlAsync(markup, MainDescription("Interface IGoo"), Documentation("summary for interface IGoo"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(4868, "https://github.com/dotnet/roslyn/issues/4868")>
        Public Async Function TestQuickInfoExceptions() As Task
            Await TestAsync("
Imports System
Namespace MyNs
    Class MyException1
        Inherits Exception
    End Class
    Class MyException2
        Inherits Exception
    End Class
    Class TestClass
        ''' <exception cref=""MyException1""></exception>
        ''' <exception cref=""T:MyNs.MyException2""></exception>
        ''' <exception cref=""System.Int32""></exception>
        ''' <exception cref=""Double""></exception>
        ''' <exception cref=""Not_A_Class_But_Still_Displayed""></exception>
        Sub M()
            M$$()
        End Sub
    End Class
End Namespace
",
                Exceptions($"{vbCrLf}{WorkspacesResources.Exceptions_colon}{vbCrLf}  MyException1{vbCrLf}  MyException2{vbCrLf}  Integer{vbCrLf}  Double{vbCrLf}  Not_A_Class_But_Still_Displayed"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(23307, "https://github.com/dotnet/roslyn/issues/23307")>
        Public Async Function TestQuickInfoCaptures() As Task
            Await TestAsync("
Class C
    Sub M(x As Integer)
        Dim a As System.Action = Sub$$()
            x = x + 1
        End Sub
    End Sub
End Class
",
                Captures($"{vbCrLf}{WorkspacesResources.Variables_captured_colon} x"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(23307, "https://github.com/dotnet/roslyn/issues/23307")>
        Public Async Function TestQuickInfoCaptures2() As Task
            Await TestAsync("
Class C
    Sub M(x As Integer)
        Dim a As System.Action = S$$ub() x = x + 1
    End Sub
End Class
",
                Captures($"{vbCrLf}{WorkspacesResources.Variables_captured_colon} x"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(23307, "https://github.com/dotnet/roslyn/issues/23307")>
        Public Async Function TestQuickInfoCaptures3() As Task
            Await TestAsync("
Class C
    Sub M(x As Integer)
        Dim a As System.Action(Of Integer) = Functio$$n(a) x = x + 1
    End Sub
End Class
",
                Captures($"{vbCrLf}{WorkspacesResources.Variables_captured_colon} x"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(23307, "https://github.com/dotnet/roslyn/issues/23307")>
        Public Async Function TestQuickInfoCaptures4() As Task
            Await TestAsync("
Class C
    Sub M(x As Integer)
        Dim a As System.Action(Of Integer) = Functio$$n(a)
            x = x + 1
        End Function
    End Sub
End Class
",
                Captures($"{vbCrLf}{WorkspacesResources.Variables_captured_colon} x"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(23307, "https://github.com/dotnet/roslyn/issues/23307")>
        Public Async Function TestQuickInfoCaptures5() As Task
            Await TestAsync("
Class C
    Sub M([Me] As Integer)
        Dim x As Integer = 0
        Dim a As System.Action(Of Integer) = Functio$$n(a)
            M(1)
            x = x + 1
            [Me] = [Me] + 1
        End Function
    End Sub
End Class
",
                Captures($"{vbCrLf}{WorkspacesResources.Variables_captured_colon} Me, [Me], x"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(1516, "https://github.com/dotnet/roslyn/issues/1516")>
        Public Async Function TestQuickInfoWithNonStandardSeeAttributesAppear() As Task
            Await TestAsync("
Class C
    ''' <summary>
    ''' <see cref=""System.String"" />
    ''' <see href=""http://microsoft.com"" />
    ''' <see langword=""Nothing"" />
    ''' <see unsupported-attribute=""cat"" />
    ''' </summary>
    Sub M()
        M$$()
    End Sub
End Class
",
                 Documentation("String http://microsoft.com Nothing cat"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(410932, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=410932")>
        Public Async Function TestGenericMethodInDocComment() As Task
            Await TestWithImportsAsync(<Text><![CDATA[
Public Class Test
    Function F(Of T)() As T
        F(Of T)()
    End Function

    ''' <summary>
    ''' <see cref="F$$(Of T)()"/>
    ''' </summary>
    Public Sub S()
    End Sub
End Class
                ]]></Text>.NormalizedValue,
             MainDescription("Function Test.F(Of T)() As T"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(2644, "https://github.com/dotnet/roslyn/issues/2644")>
        Public Async Function PropertyWithSameNameAsOtherType() As Task
            Await TestAsync("
Imports ConsoleApp3.ConsoleApp

Module Program
    Public B As A
    Public A As B

    Sub Main()
        B = ConsoleApp.B.F$$()
    End Sub
End Module
Namespace ConsoleApp
    Class A
    End Class

    Class B
        Public Shared Function F() As A
            Return Nothing
        End Function
    End Class
End Namespace
",
            MainDescription("Function ConsoleApp.B.F() As ConsoleApp.A"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(2644, "https://github.com/dotnet/roslyn/issues/2644")>
        Public Async Function PropertyWithSameNameAsOtherType2() As Task
            Await TestAsync("
Module Program
    Public Bar As List(Of Bar)

    Sub Main()
        Tes$$t(Of Bar)()
    End Sub

    Sub Test(Of T)()
    End Sub
End Module

Class Bar
End Class
",
            MainDescription("Sub Program.Test(Of Bar)()"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(548762, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=548762")>
        Public Async Function DefaultPropertyTransformation_01() As Task
            Await TestAsync("
<System.Reflection.DefaultMember(""Item"")>
Class C1
    Default Public Property Item(x As String) As String
        Get
            Return """"
        End Get
        Set(value As String)

        End Set
    End Property
End Class

Class C2
    Public Property ViewData As C1
End Class

Class C3
    Inherits C2

    Sub Test()
        View$$Data(""Title"") = ""About""
    End Sub
End Class",
            MainDescription("Property C2.ViewData As C1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(548762, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=548762")>
        Public Async Function DefaultPropertyTransformation_02() As Task
            Await TestAsync("
<System.Reflection.DefaultMember(""Item"")>
Class C1
    Default Public Property Item(x As String) As String
        Get
            Return """"
        End Get
        Set(value As String)

        End Set
    End Property
End Class

Class C2
    Public Shared Property ViewData As C1
End Class

Class C3
    Inherits C2

    Sub Test()
        View$$Data(""Title"") = ""About""
    End Sub
End Class",
            MainDescription("Property C2.ViewData As C1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(548762, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=548762")>
        Public Async Function DefaultPropertyTransformation_03() As Task
            Await TestAsync("
<System.Reflection.DefaultMember(""Item"")>
Class C1
    Default Public Property Item(x As String) As String
        Get
            Return """"
        End Get
        Set(value As String)

        End Set
    End Property
End Class

Class C2
    Public Function ViewData As C1
        Return Nothing
    End Function
End Class

Class C3
    Inherits C2

    Sub Test()
        View$$Data(""Title"") = ""About""
    End Sub
End Class",
            MainDescription("Function C2.ViewData() As C1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(548762, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=548762")>
        Public Async Function DefaultPropertyTransformation_04() As Task
            Await TestAsync("
<System.Reflection.DefaultMember(""Item"")>
Class C1
    Default Public Property Item(x As String) As String
        Get
            Return """"
        End Get
        Set(value As String)

        End Set
    End Property
End Class

Class C2
    Public Shared Function ViewData As C1
        Return Nothing
    End Function
End Class

Class C3
    Inherits C2

    Sub Test()
        View$$Data(""Title"") = ""About""
    End Sub
End Class",
            MainDescription("Function C2.ViewData() As C1"))
        End Function

        <WorkItem(30642, "https://github.com/dotnet/roslyn/issues/30642")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function BuiltInOperatorWithUserDefinedEquivalent() As Task
            Await TestAsync(
"
class X
    sub N(a as string, b as string)
        dim v = a $$= b
    end sub
end class",
                MainDescription("Operator String.=(a As String, b As String) As Boolean"),
                SymbolGlyph(Glyph.Operator))
        End Function

        <WorkItem(29703, "https://github.com/dotnet/roslyn/issues/29703")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestGetAccessorDocumentation() As Task
            Await TestAsync("
Class C
    ''' <summary>Summary for property Goo</summary>
    Property Goo As Integer
        G$$et
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class",
            Documentation("Summary for property Goo"))
        End Function

        <WorkItem(29703, "https://github.com/dotnet/roslyn/issues/29703")>
        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Async Function TestSetAccessorDocumentation() As Task
            Await TestAsync("
Class C
    ''' <summary>Summary for property Goo</summary>
    Property Goo As Integer
        Get
            Return 0
        End Get
        S$$et(value As Integer)
        End Set
    End Property
End Class",
            Documentation("Summary for property Goo"))
        End Function
    End Class
End Namespace
