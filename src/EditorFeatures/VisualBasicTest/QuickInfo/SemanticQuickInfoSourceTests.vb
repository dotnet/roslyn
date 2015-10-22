' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.QuickInfo
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Projection
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.QuickInfo
    Public Class SemanticQuickInfoSourceTests
        Inherits AbstractSemanticQuickInfoSourceTests

        Protected Overrides Sub Test(markup As String, ParamArray expectedResults() As Action(Of Object))
            TestWithReferences(markup, Array.Empty(Of String)(), expectedResults)
        End Sub

        Protected Sub TestShared(workspace As TestWorkspace, position As Integer, ParamArray expectedResults() As Action(Of Object))
            Dim noListeners = SpecializedCollections.EmptyEnumerable(Of Lazy(Of IAsynchronousOperationListener, FeatureMetadata))()

            Dim provider = New SemanticQuickInfoProvider(
             workspace.GetService(Of ITextBufferFactoryService),
             workspace.GetService(Of IContentTypeRegistryService),
             workspace.GetService(Of IProjectionBufferFactoryService),
             workspace.GetService(Of IEditorOptionsFactoryService),
             workspace.GetService(Of ITextEditorFactoryService),
             workspace.GetService(Of IGlyphService),
             workspace.GetService(Of ClassificationTypeMap))

            TestShared(workspace, provider, position, expectedResults)

            ' speculative semantic model
            Dim document = workspace.CurrentSolution.Projects.First().Documents.First()
            If CanUseSpeculativeSemanticModel(document, position) Then
                Dim buffer = workspace.Documents.Single().TextBuffer
                Using edit = buffer.CreateEdit()
                    edit.Replace(0, buffer.CurrentSnapshot.Length, buffer.CurrentSnapshot.GetText())
                    edit.Apply()
                End Using

                TestShared(workspace, provider, position, expectedResults)
            End If
        End Sub

        Private Sub TestShared(workspace As TestWorkspace, provider As SemanticQuickInfoProvider, position As Integer, expectedResults() As Action(Of Object))
            Dim state = provider.GetItemAsync(workspace.CurrentSolution.Projects.First().Documents.First(),
                                         position, cancellationToken:=CancellationToken.None).Result

            If state IsNot Nothing Then
                WaitForDocumentationComment(state.Content)
            End If

            If expectedResults Is Nothing Then
                Assert.Null(state)
            Else
                Assert.NotNull(state)

                For Each expected In expectedResults
                    expected(state.Content)
                Next
            End If
        End Sub

        Protected Sub TestFromXml(markup As String, ParamArray expectedResults As Action(Of Object)())
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspace(markup)
                TestShared(workspace, workspace.Documents.First().CursorPosition.Value, expectedResults)
            End Using
        End Sub

        Protected Sub TestWithReferences(markup As String, metadataReferences As String(), ParamArray expectedResults() As Action(Of Object))
            Dim code As String = Nothing
            Dim position As Integer = Nothing
            MarkupTestFile.GetPosition(markup, code, position)

            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines({code}, Nothing, metadataReferences)
                TestShared(workspace, position, expectedResults)
            End Using
        End Sub

        Protected Sub TestWithImports(markup As String, ParamArray expectedResults() As Action(Of Object))
            Dim markupWithImports =
             "Imports System" & vbCrLf &
             "Imports System.Collections.Generic" & vbCrLf &
             "Imports System.Linq" & vbCrLf &
             markup

            Test(markupWithImports, expectedResults)
        End Sub

        Protected Sub TestInClass(markup As String, ParamArray expectedResults() As Action(Of Object))
            Dim markupInClass =
             "Class C" & vbCrLf &
             markup & vbCrLf &
             "End Class"

            TestWithImports(markupInClass, expectedResults)
        End Sub

        Protected Sub TestInMethod(markup As String, ParamArray expectedResults() As Action(Of Object))
            Dim markupInClass =
             "Class C" & vbCrLf &
             "Sub M()" & vbCrLf &
             markup & vbCrLf &
             "End Sub" & vbCrLf &
             "End Class"

            TestWithImports(markupInClass, expectedResults)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestInt32()
            TestInClass("Dim i As $$Int32",
             MainDescription("Structure System.Int32"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestInteger()
            TestInClass("Dim i As $$Integer",
             MainDescription("Structure System.Int32",
              ExpectedClassifications(
               Keyword("Structure"),
               WhiteSpace(" "),
               Identifier("System"),
               Operators.Dot,
               Struct("Int32"))))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestString()
            TestInClass("Dim i As $$String",
             MainDescription("Class System.String",
              ExpectedClassifications(
               Keyword("Class"),
               WhiteSpace(" "),
               Identifier("System"),
               Operators.Dot,
               [Class]("String"))))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestStringAtEndOfToken()
            TestInClass("Dim i As String$$",
             MainDescription("Class System.String"))
        End Sub

        <WorkItem(1280, "https://github.com/dotnet/roslyn/issues/1280")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestStringLiteral()
            TestInClass("Dim i = ""cat""$$",
             MainDescription("Class System.String"))
        End Sub

        <WorkItem(1280, "https://github.com/dotnet/roslyn/issues/1280")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestInterpolatedStringLiteral()
            TestInClass("Dim i = $""cat""$$", MainDescription("Class System.String"))
            TestInClass("Dim i = $""c$$at""", MainDescription("Class System.String"))
            TestInClass("Dim i = $""$$cat""", MainDescription("Class System.String"))
            TestInClass("Dim i = $""cat {1$$ + 2} dog""", MainDescription("Structure System.Int32"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestListOfString()
            TestInClass("Dim l As $$List(Of String)",
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
             TypeParameterMap(vbCrLf & $"T {FeaturesResources.Is} String",
              ExpectedClassifications(
                  WhiteSpace(vbCrLf),
               TypeParameter("T"),
               WhiteSpace(" "),
               Text(FeaturesResources.Is),
               WhiteSpace(" "),
               Keyword("String"))))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestListOfT()
            TestWithImports(<Text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestListOfT2()
            TestWithImports(<Text>
                    Class C(Of T)
                        Dim l As Lis$$t(Of T)
                    End Class
                 </Text>.NormalizedValue,
             MainDescription("Class System.Collections.Generic.List(Of T)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestListOfT3()
            TestWithImports(<Text>
                    Class C(Of T)
                        Dim l As List$$(Of T)
                    End Class
                </Text>.NormalizedValue,
             MainDescription("Class System.Collections.Generic.List(Of T)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestListOfT4()
            TestWithImports(<Text>
                    Class C(Of T)
                        Dim l As List $$(Of T)
                    End Class
                </Text>.NormalizedValue,
             Nothing)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestDictionaryOfIntegerAndString()
            TestWithImports(<Text>
                    Class C
                        Dim d As $$Dictionary(Of Integer, String)
                    End Class
                </Text>.NormalizedValue,
             MainDescription("Class System.Collections.Generic.Dictionary(Of TKey, TValue)"),
             TypeParameterMap(
              Lines(vbCrLf & $"TKey {FeaturesResources.Is} Integer",
                 $"TValue {FeaturesResources.Is} String")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestDictionaryOfTAndU()
            TestWithImports(<Text>
                    Class C(Of T, U)
                        Dim d As $$Dictionary(Of T, U)
                    End Class
                </Text>.NormalizedValue,
             MainDescription("Class System.Collections.Generic.Dictionary(Of TKey, TValue)"),
             TypeParameterMap(
              Lines(vbCrLf & $"TKey {FeaturesResources.Is} T",
                 $"TValue {FeaturesResources.Is} U")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestIEnumerableOfInteger()
            TestInClass("Dim ie As $$IEnumerable(Of Integer)",
             MainDescription("Interface System.Collections.Generic.IEnumerable(Of Out T)"),
             TypeParameterMap(vbCrLf & $"T {FeaturesResources.Is} Integer"))
        End Sub

        <WorkItem(542157)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestEvent()
            TestInMethod("AddHandler System.Console.$$CancelKeyPress, AddressOf S",
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestEventHandler()
            TestInClass("Dim e As $$EventHandler",
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestTypeParameter()
            Test(StringFromLines("Class C(Of T)",
                  "    Dim t As $$T",
                  "End Class"),
             MainDescription($"T {FeaturesResources.In} C(Of T)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestNullableOfInteger()
            TestInClass("Dim n As $$Nullable(Of Integer)",
             MainDescription("Structure System.Nullable(Of T As Structure)"),
             TypeParameterMap(vbCrLf & $"T {FeaturesResources.Is} Integer"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestGenericTypeDeclaredOnMethod1()
            Test(<Text>
                    Class C
                        Shared Sub Meth1(Of T1)
                            Dim i As $$T1
                        End Sub
                    End Class
                </Text>.NormalizedValue,
             MainDescription($"T1 {FeaturesResources.In} C.Meth1(Of T1)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestGenericTypeDeclaredOnMethod2()
            Test(<Text>
                    Class C
                        Shared Sub Meth1(Of T1 As Class)
                            Dim i As $$T1
                        End Sub
                    End Class
                </Text>.NormalizedValue,
             MainDescription($"T1 {FeaturesResources.In} C.Meth1(Of T1 As Class)"))
        End Sub

        <WorkItem(538732)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestParameter()
            TestWithImports(<Text>
                    Module C
                        Shared Sub Foo(Of T1 As Class)
                            Console.Wr$$ite(5)
                        End Sub
                    End Class
                </Text>.NormalizedValue,
             MainDescription($"Sub Console.Write(value As Integer) (+ 17 {FeaturesResources.Overloads})",
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
               Text(FeaturesResources.Overloads),
               Punctuation.CloseParen)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestOnFieldDeclaration()
            TestInClass("Dim $$i As Int32",
             MainDescription($"({FeaturesResources.Field}) C.i As Integer"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestMinimal1()
            Test(<Text>
                     Imports System.Collections.Generic
                     Class C
                     Dim p as New Li$$st(Of string)
                     End Class
                 </Text>.NormalizedValue,
              MainDescription($"Sub List(Of String).New() (+ 2 {FeaturesResources.Overloads})"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestMinimal2()
            Test(<Text>
                     Imports System.Collections.Generic
                     Class C
                     function $$P() as List(Of string)
                     End Class
                 </Text>.NormalizedValue,
              MainDescription("Function C.P() As List(Of String)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestAnd()
            Test(<Text>
                     Imports System.Collections.Generic
                     Class C
                        sub s()
                            dim x as Boolean
                            x= true a$$nd False
                        end sub
                     End Class
                 </Text>.NormalizedValue,
              MainDescription("Operator Boolean.And(left As Boolean, right As Boolean) As Boolean"))
        End Sub

        <WorkItem(538822)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestDelegate()
            Test(<Text>
                     Imports System
                     Class C
                        sub s()
                            dim F as F$$unc(of Integer, String)
                        end sub
                     End Class
                 </Text>.NormalizedValue,
              MainDescription("Delegate Function System.Func(Of In T, Out TResult)(arg As T) As TResult"),
              TypeParameterMap(
               Lines(vbCrLf & $"T {FeaturesResources.Is} Integer",
                  $"TResult {FeaturesResources.Is} String")))
        End Sub

        <WorkItem(538824)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestOnDelegateInvocation()
            Test(<Text>
                    Class Program
                        delegate sub D1()
                        shared sub Main()
                            dim d as D1
                            $$d()
                        end sub
                    end class</Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.LocalVariable}) d As D1"))
        End Sub

        <WorkItem(538786)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestOnGenericOverloads1()
            Test(<Text>
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
            MainDescription($"Sub C.M() (+ 2 {FeaturesResources.Overloads})"))
        End Sub

        <WorkItem(538786)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestOnGenericOverloads2()
            Test(<Text>
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
        End Sub

        <WorkItem(538773)>
        <WpfFact>
        Public Sub TestOverriddenMethod()
            Test(<Text>
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
            MainDescription($"Sub B.G() (+ 1 {FeaturesResources.Overload})"))
        End Sub

        <WorkItem(538918)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestOnMe()
            Test(<Text>
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
        End Sub

        <WorkItem(539240)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestOnArrayCreation1()
            Test(<Text>
class C
    Sub Test()
        Dim a As Integer() = N$$ew Integer(3) { }
    End Sub
End class
</Text>.NormalizedValue,
            Nothing)
        End Sub

        <WorkItem(539240)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestOnArrayCreation2()
            Test(<Text>
class C
    Sub Test()
        Dim a As Integer() = New In$$teger(3) { }
    End Sub
End class
</Text>.NormalizedValue,
            MainDescription("Structure System.Int32"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestDimInFieldDeclaration()
            TestInClass("Dim$$ a As Integer", MainDescription("Structure System.Int32"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestDimMultipleInFieldDeclaration()
            TestInClass("$$Dim x As Integer, y As String", MainDescription(VBEditorResources.MultipleTypes))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestDimInFieldDeclarationCustomType()
            Test(<Text>
Module Program
    Di$$m z As CustomClass	
    Private Class CustomClass
    End Class
End Module
</Text>.NormalizedValue,
            MainDescription("Class Program.CustomClass"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestDimInLocalDeclaration()
            TestInMethod("Dim$$ a As Integer", MainDescription("Structure System.Int32"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestDimMultipleInLocalDeclaration()
            TestInMethod("$$Dim x As Integer, y As String", MainDescription(VBEditorResources.MultipleTypes))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestDimInLocalDeclarationCustomType()
            Test(<Text>
Module Program
    Sub Main(args As String())
        D$$im z As CustomClass
    End Sub	
    Private Class CustomClass
    End Class
End Module
</Text>.NormalizedValue,
            MainDescription("Class Program.CustomClass"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestDefaultProperty1()
            Test(<Text>
Class X
    Public ReadOnly Property Foo As Y
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
        a = b.F$$oo(4)
    End Sub
End Module


</Text>.NormalizedValue,
            MainDescription("ReadOnly Property X.Foo As Y"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestDefaultProperty2()
            Test(<Text>
Class X
    Public ReadOnly Property Foo As Y
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
        a = b.Foo.I$$tem(4)
    End Sub
End Module


</Text>.NormalizedValue,
            MainDescription("ReadOnly Property Y.Item(a As Integer) As String"))
        End Sub

        <WorkItem(541582)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub LambdaExpression()
            Test(<Text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Module1
    Sub Main(ByVal args As String())
        Dim increment2 As Func(Of Integer, UInt16) = Function(x42)$$
                                                         Return x42 + 2
                                                     End Function
    End Sub
End Module</Text>.NormalizedValue, Nothing)
        End Sub

        <WorkItem(541353)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestUnboundMethodInvocation()
            TestInMethod("Me.Fo$$o()", Nothing)
        End Sub

        <WorkItem(541582)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestQuickInfoOnExtensionMethod()
            Test(<Text><![CDATA[Imports System.Runtime.CompilerServices
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestQuickInfoOnExtensionMethodOverloads()
            Test(<Text><![CDATA[Imports System.Runtime.CompilerServices
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
            MainDescription($"<{VBFeaturesResources.Extension}> Sub String.TestExt() (+ 2 {FeaturesResources.Overloads})"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestQuickInfoOnExtensionMethodOverloads2()
            Test(<Text><![CDATA[Imports System.Runtime.CompilerServices
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
            MainDescription($"<{VBFeaturesResources.Extension}> Sub String.TestExt() (+ 1 {FeaturesResources.Overload})"))
        End Sub

        <WorkItem(541960)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub DontRemoveAttributeSuffixAndProduceInvalidIdentifier1()
            Test(<Text><![CDATA[
Imports System
Class _Attribute
    Inherits Attribute

    Dim x$$ As _Attribute
End Class]]></Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.Field}) _Attribute.x As _Attribute"))
        End Sub

        <WorkItem(541960)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub DontRemoveAttributeSuffixAndProduceInvalidIdentifier2()
            Test(<Text><![CDATA[
Imports System
Class ClassAttribute
    Inherits Attribute

    Dim x$$ As ClassAttribute
End Class]]></Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.Field}) ClassAttribute.x As ClassAttribute"))
        End Sub

        <WorkItem(541960)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub DontRemoveAttributeSuffix1()
            Test(<Text><![CDATA[
Imports System
Class Class1Attribute
    Inherits Attribute

    Dim x$$ As Class1Attribute
End Class]]></Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.Field}) Class1Attribute.x As Class1Attribute"))
        End Sub

        <WorkItem(1696, "https://github.com/dotnet/roslyn/issues/1696")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub AttributeQuickInfoBindsToClassTest()
            Test("
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
        End Sub

        <WorkItem(1696, "https://github.com/dotnet/roslyn/issues/1696")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub AttributeConstructorQuickInfo()
            Test("
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
        End Sub

        <WorkItem(542613)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestUnboundGeneric()
            Test(<Text><![CDATA[
Imports System
Imports System.Collections.Generic
Class C
    Sub M()
        Dim t As Type = GetType(L$$ist(Of ))
    End Sub
End Class]]></Text>.NormalizedValue,
            MainDescription("Class System.Collections.Generic.List(Of T)"),
            NoTypeParameterMap)
        End Sub

        <WorkItem(543209)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub QuickInfoForAnonymousType1()
            Test(<Text><![CDATA[
Class C
    Sub S
        Dim product = $$New With {Key .Name = "", Key .Price = 0}
    End Sub
End Class]]></Text>.NormalizedValue,
            MainDescription("AnonymousType 'a"),
            NoTypeParameterMap,
            AnonymousTypes(vbCrLf & FeaturesResources.AnonymousTypes & vbCrLf & $"    'a {FeaturesResources.Is} New With {{ Key .Name As String, Key .Price As Integer }}"))
        End Sub

        <WorkItem(543226)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub QuickInfoForAnonymousType2()
            Test(<Text><![CDATA[
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
            AnonymousTypes(vbCrLf & FeaturesResources.AnonymousTypes & vbCrLf & $"    'a {FeaturesResources.Is} New With {{ Key .Name As String, Key .Price As Integer }}"))
        End Sub

        <WorkItem(543223)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub QuickInfoForAnonymousType3()
            Test(<Text><![CDATA[
Class C
    Sub S
        Dim x = $$New With {Key .Foo = x}
    End Sub
End Class
]]></Text>.NormalizedValue,
            MainDescription("AnonymousType 'a"),
            NoTypeParameterMap,
            AnonymousTypes(vbCrLf & FeaturesResources.AnonymousTypes & vbCrLf & $"    'a {FeaturesResources.Is} New With {{ Key .Foo As ? }}"))
        End Sub

        <WorkItem(543242)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub QuickInfoForUnboundLabel()
            Test(<Text><![CDATA[
Option Infer On
Option Strict On
Public Class D
    Public Sub foo()
        GoTo $$oo
    End Sub
End Class]]></Text>.NormalizedValue,
            Nothing)
        End Sub

        <WorkItem(543624)>
        <WorkItem(543275)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub QuickInfoForAnonymousDelegate1()
            Test(<Text><![CDATA[
Imports System

Module Program
    Sub Main
        Dim $$a = Sub() Return
    End Sub
End Module
]]></Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.LocalVariable}) a As <Sub()>"))
        End Sub

        <WorkItem(543624)>
        <WorkItem(543275)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub QuickInfoForAnonymousDelegate2()
            Test(<Text><![CDATA[
Imports System

Module Program
    Sub Main
        Dim $$a = Function() 1
    End Sub
End Module
]]></Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.LocalVariable}) a As <Function() As Integer>"))
        End Sub

        <WorkItem(543624)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub QuickInfoForAnonymousDelegate3()
            Test(<Text><![CDATA[
Imports System

Module Program
    Sub Main
        Dim $$a = Function() New With {.Foo = "Foo"}
    End Sub
End Module
]]></Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.LocalVariable}) a As <Function() As 'a>"),
            AnonymousTypes(vbCrLf & FeaturesResources.AnonymousTypes & vbCrLf &
                           $"    'a {FeaturesResources.Is} New With {{ .Foo As String }}"))
        End Sub

        <WorkItem(543624)>
        <WorkItem(543275)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub QuickInfoForAnonymousDelegate4()
            Test(<Text><![CDATA[
Imports System

Module Program
    Sub Main
        Dim $$a = Function(i As Integer) New With {.Sq = i * i, .M = Function(j As Integer) i * i}
    End Sub
End Module
]]></Text>.NormalizedValue,
            MainDescription($"({FeaturesResources.LocalVariable}) a As <Function(i As Integer) As 'a>"),
            AnonymousTypes(vbCrLf & FeaturesResources.AnonymousTypes & vbCrLf &
                           $"    'a {FeaturesResources.Is} New With {{ .Sq As Integer, .M As <Function(j As Integer) As Integer> }}"))
        End Sub

        <WorkItem(543389)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub ImplicitMemberNameLocal1()
            Test(<Text><![CDATA[
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
        End Sub

        <WorkItem(543389)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub ImplicitMemberNameLocal2()
            Test(<Text><![CDATA[
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
        End Sub

        <WorkItem(543389)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub ImplicitMemberNameLocal3()
            Test(<Text><![CDATA[
Imports System

Module Program
    Function Foo() As Integer
        Fo$$o = 1
    End Function
End Module
]]></Text>.NormalizedValue,
            MainDescription("Function Program.Foo() As Integer"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub BinaryConditionalExpression()
            TestInMethod("Dim x = If$$(True, False)",
                MainDescription($"If({Expression1}, {ExpressionIfNothing}) As Boolean"),
                Documentation(ExpressionEvalReturns))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TernaryConditionalExpression()
            TestInMethod("Dim x = If$$(True, ""Foo"", ""Bar"")",
                MainDescription($"If({Condition} As Boolean, {ExpressionIfTrue}, {ExpressionIfFalse}) As String"),
                Documentation(IfConditionReturnsResults))
        End Sub

        <WorkItem(957082)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub AddHandlerStatement()
            TestInMethod("$$AddHandler foo, bar",
                MainDescription($"AddHandler {Event1}, {Handler}"),
                Documentation(AssociatesAnEvent),
                SymbolGlyph(Glyph.Keyword))
        End Sub

        <WorkItem(957082)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub RemoveHandlerStatement()
            TestInMethod("$$RemoveHandler foo, bar",
                MainDescription($"RemoveHandler {Event1}, {Handler}"),
                Documentation(RemovesEventAssociation),
                SymbolGlyph(Glyph.Keyword))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub GetTypeExpression()
            TestInMethod("Dim x = GetType$$(String)",
                MainDescription("GetType(String) As Type"),
                Documentation(ReturnsSystemTypeObject))
        End Sub

        <WorkItem(544140)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub GetXmlNamespaceExpression()
            TestWithReferences(
                <text>
class C
    sub M()
        Dim x = GetXmlNamespace$$()
    end sub()
end class
                </text>.NormalizedValue,
                {GetType(System.Xml.XmlAttribute).Assembly.Location, GetType(System.Xml.Linq.XAttribute).Assembly.Location},
                MainDescription($"GetXmlNamespace([{XmlNamespacePrefix}]) As Xml.Linq.XNamespace"),
                Documentation(ReturnsXNamespaceObject))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TryCastExpression()
            TestInMethod("Dim x = TryCast$$(a, String)",
                MainDescription($"TryCast({Expression1}, String) As String"),
                Documentation(IntroducesSafeTypeConversion))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub DirectCastExpression()
            TestInMethod("Dim x = DirectCast$$(a, String)",
                MainDescription($"DirectCast({Expression1}, String) As String"),
                Documentation(IntroducesTypeConversion))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CTypeCastExpression()
            TestInMethod("Dim x = CType$$(a, String)",
                MainDescription($"CType({Expression1}, String) As String"),
                Documentation(ReturnsConvertResult))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CBoolExpression()
            TestInMethod("Dim x = CBool$$(a)",
                MainDescription($"CBool({Expression1}) As Boolean"),
                Documentation(String.Format(ConvertsToDataType, "Boolean")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CByteExpression()
            TestInMethod("Dim x = CByte$$(a)",
                MainDescription($"CByte({Expression1}) As Byte"),
                Documentation(String.Format(ConvertsToDataType, "Byte")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CCharExpression()
            TestInMethod("Dim x = CChar$$(a)",
                MainDescription($"CChar({Expression1}) As Char"),
                Documentation(String.Format(ConvertsToDataType, "Char")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CDateExpression()
            TestInMethod("Dim x = CDate$$(a)",
                MainDescription($"CDate({Expression1}) As Date"),
                Documentation(String.Format(ConvertsToDataType, "Date")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CDblExpression()
            TestInMethod("Dim x = CDbl$$(a)",
                MainDescription($"CDbl({Expression1}) As Double"),
                Documentation(String.Format(ConvertsToDataType, "Double")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CDecExpression()
            TestInMethod("Dim x = CDec$$(a)",
                MainDescription($"CDec({Expression1}) As Decimal"),
                Documentation(String.Format(ConvertsToDataType, "Decimal")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CIntExpression()
            TestInMethod("Dim x = CInt$$(a)",
                MainDescription($"CInt({Expression1}) As Integer"),
                Documentation(String.Format(ConvertsToDataType, "Integer")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CLngExpression()
            TestInMethod("Dim x = CLng$$(a)",
                MainDescription($"CLng({Expression1}) As Long"),
                Documentation(String.Format(ConvertsToDataType, "Long")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CObjExpression()
            TestInMethod("Dim x = CObj$$(a)",
                MainDescription($"CObj({Expression1}) As Object"),
                Documentation(String.Format(ConvertsToDataType, "Object")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CSByteExpression()
            TestInMethod("Dim x = CSByte$$(a)",
                MainDescription($"CSByte({Expression1}) As SByte"),
                Documentation(String.Format(ConvertsToDataType, "SByte")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CShortExpression()
            TestInMethod("Dim x = CShort$$(a)",
                MainDescription($"CShort({Expression1}) As Short"),
                Documentation(String.Format(ConvertsToDataType, "Short")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CSngExpression()
            TestInMethod("Dim x = CSng$$(a)",
                MainDescription($"CSng({Expression1}) As Single"),
                Documentation(String.Format(ConvertsToDataType, "Single")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CStrExpression()
            TestInMethod("Dim x = CStr$$(a)",
                MainDescription($"CStr({Expression1}) As String"),
                Documentation(String.Format(ConvertsToDataType, "String")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CUIntExpression()
            TestInMethod("Dim x = CUInt$$(a)",
                MainDescription($"CUInt({Expression1}) As UInteger"),
                Documentation(String.Format(ConvertsToDataType, "UInteger")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CULngExpression()
            TestInMethod("Dim x = CULng$$(a)",
                MainDescription($"CULng({Expression1}) As ULong"),
                Documentation(String.Format(ConvertsToDataType, "ULong")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CUShortExpression()
            TestInMethod("Dim x = CUShort$$(a)",
                MainDescription($"CUShort({Expression1}) As UShort"),
                Documentation(String.Format(ConvertsToDataType, "UShort")))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub MidAssignmentStatement1()
            TestInMethod("$$Mid(""foo"", 0) = ""bar""",
                MainDescription($"Mid({StringName}, {StartIndex}, [{Length}]) = {StringExpression}"),
                Documentation(ReplacesChars))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub MidAssignmentStatement2()
            TestInMethod("$$Mid(""foo"", 0, 0) = ""bar""",
                MainDescription($"Mid({StringName}, {StartIndex}, [{Length}]) = {StringExpression}"),
                Documentation(ReplacesChars))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestConstantField()
            TestInClass("const $$F = 1",
                MainDescription($"({FeaturesResources.Constant}) C.F As Integer = 1"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestMultipleConstantFields()
            TestInClass("Public Const X As Double = 1.0, Y As Double = 2.0, $$Z As Double = 3.5",
                MainDescription($"({FeaturesResources.Constant}) C.Z As Double = 3.5"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestConstantDependencies()
            Test(<Text><![CDATA[
Imports System

Class A
    Public Const $$X As Integer = B.Z + 1
    Public Const Y As Integer = 10
End Class
Class B
    Public Const Z As Integer = A.Y + 1
End Class
]]></Text>.NormalizedValue,
                MainDescription($"({FeaturesResources.Constant}) A.X As Integer = B.Z + 1"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestConstantCircularDependencies()
            Test(<Text><![CDATA[
Imports System

Class A
    Public Const $$X As Integer = B.Z + 1
End Class
Class B
    Public Const Z As Integer = A.X + 1
End Class
]]></Text>.NormalizedValue,
                MainDescription($"({FeaturesResources.Constant}) A.X As Integer = B.Z + 1"))
        End Sub

        <WorkItem(544620)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestConstantOverflow()
            TestInClass("Public Const $$Z As Integer = Integer.MaxValue + 1",
                MainDescription($"({FeaturesResources.Constant}) C.Z As Integer = Integer.MaxValue + 1"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestEnumInConstantField()
            Test(<Text><![CDATA[
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
                MainDescription($"({FeaturesResources.LocalConstant}) x As Integer = CInt(Days.Sun)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestEnumInConstantField2()
            Test(<Text><![CDATA[
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
                MainDescription($"({FeaturesResources.LocalConstant}) x As Days = Days.Sun"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestConstantParameter()
            TestInClass("Sub Bar(optional $$b as Integer = 1)",
                MainDescription($"({FeaturesResources.Parameter}) b As Integer = 1"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestConstantLocal()
            TestInMethod("const $$loc = 1",
                MainDescription($"({FeaturesResources.LocalConstant}) loc As Integer = 1"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestEnumValue1()
            TestInMethod("Const $$sunday = DayOfWeek.Sunday",
                MainDescription($"({FeaturesResources.LocalConstant}) sunday As DayOfWeek = DayOfWeek.Sunday"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestEnumValue2()
            TestInMethod("Const $$v = AttributeTargets.Constructor or AttributeTargets.Class",
                MainDescription($"({FeaturesResources.LocalConstant}) v As AttributeTargets = AttributeTargets.Constructor or AttributeTargets.Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestComplexConstantParameter()
            TestInClass("Sub Bar(optional $$b as Integer = 1 + True)",
                MainDescription($"({FeaturesResources.Parameter}) b As Integer = 1 + True"))
        End Sub

        <WorkItem(546849)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestIndexedPropertyWithOptionalParameter()
            Test(<Text><![CDATA[
Class Test
    Public Property Prop(p1 As Integer, Optional p2 As Integer = 0) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)

        End Set
    End Property
    Sub Foo()
        Dim x As New Test
        x.Pr$$op(0) = 0
    End Sub
End Class
]]></Text>.NormalizedValue,
                 MainDescription("Property Test.Prop(p1 As Integer, [p2 As Integer = 0]) As Integer"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestAwaitableMethod()
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports System.Threading.Tasks

Class C
    Async Function foo() As Task
        fo$$o()
    End Function
End Class
        </Document>
                             </Project>
                         </Workspace>.ToString()

            Dim description = <File>&lt;<%= VBFeaturesResources.Awaitable %>&gt; Function C.foo() As Task</File>.ConvertTestSourceTag()

            Dim doc = StringFromLines("", WorkspacesResources.Usage, $"  {VBFeaturesResources.Await} foo()")

            TestFromXml(markup,
                 MainDescription(description), Usage(doc))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestObsoleteItem()
            Test(<Text><![CDATA[
Imports System

Class C
    <Obsolete>
    Sub Foo()
        Fo$$o()
    End Sub
End Class
]]></Text>.NormalizedValue,
                MainDescription($"({VBFeaturesResources.Deprecated}) Sub C.Foo()"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestEnumMemberNameFromMetadata()
            Dim code =
<Code>
Imports System

Class C
    Sub M()
        Dim c = ConsoleColor.Bla$$ck
    End Sub
End Class
</Code>.NormalizedValue()

            Test(code,
                MainDescription("ConsoleColor.Black = 0"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestEnumMemberNameFromSource1()
            Dim code =
<Code>
Enum Foo
    A = 1 &lt;&lt; 0
    B = 1 &lt;&lt; 1
    C = 1 &lt;&lt; 2
End Enum

Class C
    Sub M()
        Dim e = Foo.B$$
    End Sub
End Class
</Code>.NormalizedValue()

            Test(code,
                MainDescription("Foo.B = 1 << 1"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestEnumMemberNameFromSource2()
            Dim code =
<Code>
Enum Foo
    A
    B
    C
End Enum

Class C
    Sub M()
        Dim e = Foo.B$$
    End Sub
End Class
</Code>.NormalizedValue()

            Test(code,
                MainDescription("Foo.B = 1"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TextOnlyDocComment()
            Test(<text><![CDATA[
''' <summary>
        '''foo
        ''' </summary>
Class C$$
End Class]]></text>.NormalizedValue(), Documentation("foo"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestTrimConcatMultiLine()
            Test(<text><![CDATA[
''' <summary>
        ''' foo
        ''' bar
        ''' </summary>
Class C$$
End Class]]></text>.NormalizedValue(), Documentation("foo bar"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestCref()
            Test(<text><![CDATA[
''' <summary>
        ''' <see cref="C"/>
        ''' <seealso cref="C"/>
        ''' </summary>
Class C$$
End Class]]></text>.NormalizedValue(), Documentation("C C"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub ExcludeTextOutsideSummaryBlock()
            Test(<text><![CDATA[
''' red
        ''' <summary>
        ''' green
        ''' </summary>
        ''' yellow
Class C$$
End Class]]></text>.NormalizedValue(), Documentation("green"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub NewlineAfterPara()
            Test(<text><![CDATA[
''' <summary>
        ''' <para>foo</para>
        ''' </summary>
Class C$$
End Class]]></text>.NormalizedValue(), Documentation("foo"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub Param()
            Test(<text><![CDATA[
''' <summary></summary>
Public Class C
            ''' <typeparam name="T">A type parameter of <see cref="Foo(Of T) (string(), T)"/></typeparam>
            ''' <param name="args">First parameter of <see cref="Foo(Of T) (string(), T)"/></param>
            ''' <param name="otherParam">Another parameter of <see cref="Foo(Of T)(string(), T)"/></param>
            Public Function Foo(Of T)(arg$$s As String(), otherParam As T)
    End Function
        End Class]]></text>.NormalizedValue(), Documentation("First parameter of C.Foo(Of T)(String(), T)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub Param2()
            Test(<text><![CDATA[
''' <summary></summary>
Public Class C
            ''' <typeparam name="T">A type parameter of <see cref="Foo(Of T) (string(), T)"/></typeparam>
            ''' <param name="args">First parameter of <see cref="Foo(Of T) (string(), T)"/></param>
            ''' <param name="otherParam">Another parameter of <see cref="Foo(Of T)(string(), T)"/></param>
            Public Function Foo(Of T)(args As String(), otherP$$aram As T)
            End Function
        End Class]]></text>.NormalizedValue(), Documentation("Another parameter of C.Foo(Of T)(String(), T)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TypeParam()
            Test(<text><![CDATA[
''' <summary></summary>
Public Class C
            ''' <typeparam name="T">A type parameter of <see cref="Foo(Of T) (string(), T)"/></typeparam>
            ''' <param name="args">First parameter of <see cref="Foo(Of T) (string(), T)"/></param>
            ''' <param name="otherParam">Another parameter of <see cref="Foo(Of T)(string(), T)"/></param>
            Public Function Foo(Of T$$)( args as String(), otherParam as T)
    End Function
        End Class]]></text>.NormalizedValue(), Documentation("A type parameter of C.Foo(Of T)(String(), T)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub UnboundCref()
            Test(<text><![CDATA[
''' <summary></summary>
Public Class C
            ''' <typeparam name="T">A type parameter of <see cref="foo(Of T) (string, T)"/></typeparam>
            Public Function Foo(Of T$$)( args as String(), otherParam as T)
    End Function
        End Class]]></text>.NormalizedValue(), Documentation("A type parameter of foo(Of T) (string, T)"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CrefInConstructor()
            Test(<text><![CDATA[
Public Class TestClass
            ''' <summary>
            ''' This sample shows how to specify the <see cref="TestClass"/> constructor as a cref attribute.
            ''' </summary>
            Public Sub N$$ew()
    End Sub
        End Class]]></text>.NormalizedValue(), Documentation("This sample shows how to specify the TestClass constructor as a cref attribute."))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CrefInConstructorOverloaded()
            Test(<text><![CDATA[
Public Class TestClass
            Public Sub New()
            End Sub
            ''' <summary>
            ''' This sample shows how to specify the <see cref="TestClass.New(Integer)"/> constructor as a cref attribute.
            ''' </summary>
            Public Sub Ne$$w(value As Integer)
    End Sub
        End Class]]></text>.NormalizedValue(), Documentation("This sample shows how to specify the New(Integer) constructor as a cref attribute."))
        End Sub

        <WorkItem(814191)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CrefInGenericMethod1()
            Test(<text><![CDATA[
Public Class TestClass
            ''' <summary>
            ''' This sample shows how to specify the <see cref="GetGenericValue"/> method as a cref attribute.
            ''' </summary>
            Public Shared Function GetGe$$nericValue(Of T)(para As T) As T
        Return para
            End Function
        End Class]]></text>.NormalizedValue(), Documentation("This sample shows how to specify the TestClass.GetGenericValue(Of T)(T) method as a cref attribute."))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CrefInGenericMethod2()
            Test(<text><![CDATA[
Public class TestClass
    ''' <summary>
    ''' This sample shows how to specify the <see cref="GetGenericValue(OfT)(T)"/> method as a cref attribute.
    ''' </summary>
    Public Shared Function GetGe$$nericValue(Of T)(para As T) As T
        Return para
    End Function
End Class]]></text>.NormalizedValue(), Documentation("This sample shows how to specify the GetGenericValue(OfT)(T) method as a cref attribute."))
        End Sub

        <WorkItem(813350)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CrefInMethodOverloading1()
            Test(<text><![CDATA[
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
        End Sub

        <WorkItem(813350)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CrefInMethodOverloading2()
            Test(<text><![CDATA[
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CrefInGenericType()
            Test(<text><![CDATA[
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
        End Sub

        ''' <Remarks>
        ''' As a part of fix for 756226, quick info for VB Await keyword now displays the type inferred from the AwaitExpression. This is C# behavior.
        ''' In Dev12, quick info for VB Await keyword was the syntactic help "Await &lt;expression&gt;".
        ''' In Roslyn, VB Syntactic quick info is Not yet Implemented. User story: 522342. 
        ''' While implementing this story, determine the correct behavior for quick info on VB Await keyword (syntactic vs semantic) and update these tests.
        ''' </Remarks>
        <WorkItem(756226), WorkItem(522342)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestAwaitKeywordOnTaskReturningAsync()
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports System.Threading.Tasks

Class C
    Async Function foo() As Task
        Aw$$ait foo()
    End Function
End Class
        </Document>
                             </Project>
                         </Workspace>.ToString()

            Dim description = <File><%= FeaturesResources.PrefixTextForAwaitKeyword %><%= " " %><%= FeaturesResources.TextForSystemVoid %></File>.ConvertTestSourceTag()

            TestFromXml(markup, MainDescription(description))
        End Sub

        <WorkItem(756226), WorkItem(522342)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestAwaitKeywordOnGenericTaskReturningAsync()
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports System.Threading.Tasks

Class C
    Async Function foo() As Task(Of Integer)
        Dim x = Aw$$ait foo()
        Return 42
    End Function
End Class
        </Document>
                             </Project>
                         </Workspace>.ToString()

            Dim description = <File><%= FeaturesResources.PrefixTextForAwaitKeyword %> Structure System.Int32</File>.ConvertTestSourceTag()

            TestFromXml(markup, MainDescription(description))
        End Sub

        <WorkItem(756226), WorkItem(522342)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestAwaitKeywordOnTaskReturningAsync2()
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports System.Threading.Tasks

Class C
    Async Sub Foo()
        Aw$$ait Task.Delay(10)
    End Sub
End Class
        </Document>
                             </Project>
                         </Workspace>.ToString()

            Dim description = <File><%= FeaturesResources.PrefixTextForAwaitKeyword %><%= " " %><%= FeaturesResources.TextForSystemVoid %></File>.ConvertTestSourceTag()

            TestFromXml(markup, MainDescription(description))
        End Sub

        <WorkItem(756226), WorkItem(522342)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestNestedAwaitKeywords1()
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

            Dim description = <File>&lt;<%= VBFeaturesResources.Awaitable %>&gt; <%= FeaturesResources.PrefixTextForAwaitKeyword %> Class System.Threading.Tasks.Task(Of TResult)</File>.ConvertTestSourceTag()
            TestFromXml(markup, MainDescription(description), TypeParameterMap(vbCrLf & $"TResult {FeaturesResources.Is} Integer"))
        End Sub

        <WorkItem(756226), WorkItem(522342)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestNestedAwaitKeywords2()
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

            Dim description = <File><%= FeaturesResources.PrefixTextForAwaitKeyword %> Structure System.Int32</File>.ConvertTestSourceTag()
            TestFromXml(markup, MainDescription(description))
        End Sub

        <WorkItem(756226), WorkItem(756337), WorkItem(522342)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestTaskType()
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports System
Imports System.Threading.Tasks

Class AsyncExample
    Sub Foo()
        Dim v as Tas$$k = Nothing
    End Sub
End Class
        </Document>
                             </Project>
                         </Workspace>.ToString()

            Dim description = <File>&lt;<%= VBFeaturesResources.Awaitable %>&gt; Class System.Threading.Tasks.Task</File>.ConvertTestSourceTag()
            TestFromXml(markup, MainDescription(description))
        End Sub

        <WorkItem(756226), WorkItem(756337), WorkItem(522342)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestTaskOfTType()
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports System
Imports System.Threading.Tasks

Class AsyncExample
    Sub Foo()
        Dim v as Tas$$k(Of Integer) = Nothing
    End Sub
End Class
        </Document>
                             </Project>
                         </Workspace>.ToString()

            Dim description = <File>&lt;<%= VBFeaturesResources.Awaitable %>&gt; Class System.Threading.Tasks.Task(Of TResult)</File>.ConvertTestSourceTag()
            TestFromXml(markup, MainDescription(description), TypeParameterMap(vbCrLf & $"TResult {FeaturesResources.Is} Integer"))
        End Sub

        <WorkItem(756226), WorkItem(756337), WorkItem(522342)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestAwaitablePrefixOnCustomAwaiter()
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
            TestFromXml(markup, MainDescription(description))
        End Sub

        <WorkItem(792629)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub GenericMethodWithConstraintsAtDeclaration()
            TestInClass("Private Function Fo$$o(Of TIn As Class, TOut)(arg As TIn) As TOut
    Foo(Of TIn, TOut)(Nothing)
End Function",
             MainDescription("Function C.Foo(Of TIn As Class, TOut)(arg As TIn) As TOut"))
        End Sub

        <WorkItem(792629)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub GenericMethodWithMultipleConstraintsAtDeclaration()
            TestInClass("Private Function Fo$$o(Of TIn As {IComparable, New}, TOut)(arg As TIn) As TOut
    Foo(Of TIn, TOut)(Nothing)
End Function",
             MainDescription("Function C.Foo(Of TIn As {IComparable, New}, TOut)(arg As TIn) As TOut"))
        End Sub

        <WorkItem(792629)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub UnConstructedGenericMethodWithConstraintsAtInvocation()
            TestInClass("Private Function Foo(Of TIn As {Class, New}, TOut)(arg As TIn) As TOut
    F$$oo(Of TIn, TOut)(Nothing)
End Function",
             MainDescription("Function C.Foo(Of TIn As {Class, New}, TOut)(arg As TIn) As TOut"))
        End Sub

        <WorkItem(991466)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub TestDocumentationInImportsDirectiveWithAlias()
            Dim markup = <Workspace>
                             <Project Language="Visual Basic" CommonReferencesNet45="true">
                                 <Document FilePath="SourceDocument">
Imports I = IFoo
Class C
    Implements I$$

    Public Sub Bar() Implements IFoo.Bar
        Throw New NotImplementedException()
    End Sub
End Class

''' &lt;summary&gt;
''' summary for interface IFoo
''' &lt;/summary&gt;
Interface IFoo
    Sub Bar()
End Interface
        </Document>
                             </Project>
                         </Workspace>.ToString()

            TestFromXml(markup, MainDescription("Interface IFoo"), Documentation("summary for interface IFoo"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(4868, "https://github.com/dotnet/roslyn/issues/4868")>
        Public Sub QuickInfoExceptions()
            Test("
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
                Exceptions($"{vbCrLf}{WorkspacesResources.Exceptions}{vbCrLf}  MyException1{vbCrLf}  MyException2{vbCrLf}  Integer{vbCrLf}  Double{vbCrLf}  Not_A_Class_But_Still_Displayed"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        <WorkItem(1516, "https://github.com/dotnet/roslyn/issues/1516")>
        Public Sub QuickInfoWithNonStandardSeeAttributesAppear()
            Test("
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
        End Sub

    End Class
End Namespace
