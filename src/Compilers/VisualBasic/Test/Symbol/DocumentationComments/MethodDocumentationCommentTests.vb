' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class MethodDocumentationCommentTests
        Inherits BasicTestBase

        Private ReadOnly _compilation As VisualBasicCompilation
        Private ReadOnly _acmeNamespace As NamespaceSymbol
        Private ReadOnly _widgetClass As NamedTypeSymbol

        Public Sub New()
            _compilation = CompilationUtils.CreateCompilationWithMscorlib(
                <compilation name="MethodDocumentationCommentTests">
                    <file name="a.vb">
                    Namespace Acme
                        Structure ValueType
                            Public Sub M(i As Integer)
                            End Sub

                            Public Shared Widening Operator CType(value As Byte) As ValueType
                                Return New ValueType
                            End Operator
                        End Structure

                        Class Widget
                            Public Class NestedClass
                                Public Sub M(i As Integer)
                                End Sub
                            End Class

                            Public Shared Sub M0()
                            End Sub

                            Public Sub M1(c As Char, ByRef f As Single, _
                                ByRef v As ValueType)
                            End Sub

                            Public Sub M2(x1() As Short, x2(,) As Integer, _
                                x3()() As Long)
                            End Sub

                            Public Sub M3(x3()() As Long, x4()(,,) As Widget)
                            End Sub

                            Public Sub M4(Optional i As Integer = 1)
                            End Sub

                            Public Sub M5(ParamArray args() As Object)
                            End Sub
                        End Class

                        Class MyList(Of T)
                            Public Sub Test(t As T)
                            End Sub

                            Public Sub Zip(other As MyList(Of T))
                            End Sub

                            Public Sub ReallyZip(other as MyList(Of MyList(Of T)))
                            End Sub
                        End Class

                        Class UseList
                            Public Sub Process(list As MyList(Of Integer))
                            End Sub

                            Public Function GetValues(Of T)(inputValue As T) As MyList(Of T)
                                Return Nothing
                            End Function
                        End Class
                    End Namespace
                    </file>
                </compilation>)

            _acmeNamespace = DirectCast(_compilation.GlobalNamespace.GetMembers("Acme").Single(), NamespaceSymbol)
            _widgetClass = DirectCast(_acmeNamespace.GetTypeMembers("Widget").Single(), NamedTypeSymbol)
        End Sub

        <Fact>
        Public Sub TestMethodInStructure()
            Assert.Equal("M:Acme.ValueType.M(System.Int32)",
                         _acmeNamespace.GetTypeMembers("ValueType").Single() _
                             .GetMembers("M").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestMethodInNestedClass()
            Assert.Equal("M:Acme.Widget.NestedClass.M(System.Int32)",
                         _widgetClass.GetTypeMembers("NestedClass").Single() _
                             .GetMembers("M").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestMethod1()
            Assert.Equal("M:Acme.Widget.M0",
                         _widgetClass.GetMembers("M0").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestMethod2()
            Assert.Equal("M:Acme.Widget.M1(System.Char,System.Single@,Acme.ValueType@)",
                         _widgetClass.GetMembers("M1").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestMethod3()
            Assert.Equal("M:Acme.Widget.M2(System.Int16[],System.Int32[0:,0:],System.Int64[][])",
                         _widgetClass.GetMembers("M2").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestMethod4()
            Assert.Equal("M:Acme.Widget.M3(System.Int64[][],Acme.Widget[0:,0:,0:][])",
                         _widgetClass.GetMembers("M3").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestMethod5()
            Assert.Equal("M:Acme.Widget.M4(System.Int32)",
                         _widgetClass.GetMembers("M4").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestMethod6()
            Assert.Equal("M:Acme.Widget.M5(System.Object[])",
                         _widgetClass.GetMembers("M5").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestMethodInGenericClass()
            Assert.Equal("M:Acme.MyList`1.Test(`0)",
                         _acmeNamespace.GetTypeMembers("MyList", 1).Single() _
                            .GetMembers("Test").Single().GetDocumentationCommentId())
        End Sub

        <WorkItem(766313, "DevDiv")>
        <Fact>
        Public Sub TestMethodWithGenericDeclaringTypeAsParameter()
            Assert.Equal("M:Acme.MyList`1.Zip(Acme.MyList{`0})",
                         _acmeNamespace.GetTypeMembers("MyList", 1).Single() _
                            .GetMembers("Zip").Single().GetDocumentationCommentId())
        End Sub

        <WorkItem(766313, "DevDiv")>
        <Fact>
        Public Sub TestMethodWithGenericDeclaringTypeAsTypeParameter()
            Assert.Equal("M:Acme.MyList`1.ReallyZip(Acme.MyList{Acme.MyList{`0}})",
                         _acmeNamespace.GetTypeMembers("MyList", 1).Single() _
                            .GetMembers("ReallyZip").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestMethodWithClosedGenericParameter()
            Assert.Equal("M:Acme.UseList.Process(Acme.MyList{System.Int32})",
                         _acmeNamespace.GetTypeMembers("UseList").Single() _
                            .GetMembers("Process").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestGenericMethod()
            Assert.Equal("M:Acme.UseList.GetValues``1(``0)",
                         _acmeNamespace.GetTypeMembers("UseList").Single() _
                            .GetMembers("GetValues").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestMethodWithMissingType()
            Dim csharpAssemblyReference = TestReferences.SymbolsTests.UseSiteErrors.CSharp
            Dim ilAssemblyReference = TestReferences.SymbolsTests.UseSiteErrors.IL
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndReferences(
<compilation>
    <file name="a.vb">
Class C
    Friend Shared F As CSharpErrors.ClassMethods
End Class
    </file>
</compilation>,
                {csharpAssemblyReference, ilAssemblyReference})
            Dim type = compilation.Assembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            type = DirectCast(type.GetMember(Of FieldSymbol)("F").Type, NamedTypeSymbol)
            Dim members = type.GetMembers()
            Assert.InRange(members.Length, 1, Integer.MaxValue)
            For Each member In members
                Dim docComment = member.GetDocumentationCommentXml()
                Assert.NotNull(docComment)
            Next
        End Sub

        <Fact, WorkItem(530924, "DevDiv")>
        Public Sub TestConversionOperator()
            Assert.Equal("M:Acme.ValueType.op_Implicit(System.Byte)~Acme.ValueType",
                         _acmeNamespace.GetTypeMembers("ValueType").Single() _
                             .GetMembers("op_Implicit").Single().GetDocumentationCommentId())
        End Sub

        <Fact, WorkItem(4699, "https://github.com/dotnet/roslyn/issues/4699")>
        Public Sub GetMalformedDocumentationCommentXml()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Class Test
    ''' <summary>
    ''' Info
    ''' <!-- comment
    ''' </summary
    Shared Sub Main()
    End Sub
End Class
    ]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(source, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Diagnose))
            Dim main = compilation.GetTypeByMetadataName("Test").GetMember(Of MethodSymbol)("Main")

            Assert.Equal(
"<member name=""M:Test.Main"">
 <summary>
 Info
 <!-- comment
 </summary
</member>", main.GetDocumentationCommentXml().Trim())

            compilation = CompilationUtils.CreateCompilationWithMscorlib(source, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.Parse))
            main = compilation.GetTypeByMetadataName("Test").GetMember(Of MethodSymbol)("Main")

            Assert.Equal(
"<member name=""M:Test.Main"">
 <summary>
 Info
 <!-- comment
 </summary
</member>", main.GetDocumentationCommentXml().Trim())

            compilation = CompilationUtils.CreateCompilationWithMscorlib(source, parseOptions:=TestOptions.Regular.WithDocumentationMode(DocumentationMode.None))
            main = compilation.GetTypeByMetadataName("Test").GetMember(Of MethodSymbol)("Main")

            Assert.Equal("", main.GetDocumentationCommentXml().Trim())
        End Sub

    End Class
End Namespace
