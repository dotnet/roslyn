' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class FieldDocumentationCommentTests

        Private ReadOnly _compilation As VisualBasicCompilation
        Private ReadOnly _acmeNamespace As NamespaceSymbol
        Private ReadOnly _widgetClass As NamedTypeSymbol
        Private ReadOnly _enumSymbol As NamedTypeSymbol
        Private ReadOnly _valueType As NamedTypeSymbol

        Public Sub New()
            _compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="FieldDocumentationCommentTests">
    <file name="a.vb"><![CDATA[
Namespace Acme
    Structure ValueType
        '''<summary>Summary for total fields.</summary>
        Private total1 As Integer, total2 As Integer
    End Structure

    Class Widget
        Public Class NestedClass
            Private value As Integer
        End Class

        Private message As String
        Private Shared defaultColor As Color
        Private Const PI As Double = 3.14159
        Protected ReadOnly monthlyAverage As Double
        Private array1() As Long
        Private array2(,) As Widget
    End Class

    Enum E
        '''<summary>Enum field</summary>
        A = 1
    End Enum

End Namespace
]]>
    </file>
</compilation>)

            _acmeNamespace = DirectCast(_compilation.GlobalNamespace.GetMembers("Acme").Single(), NamespaceSymbol)
            _widgetClass = DirectCast(_acmeNamespace.GetTypeMembers("Widget").Single(), NamedTypeSymbol)
            _enumSymbol = DirectCast(_acmeNamespace.GetTypeMembers("E").Single(), NamedTypeSymbol)
            _valueType = DirectCast(_acmeNamespace.GetTypeMembers("ValueType").Single(), NamedTypeSymbol)
        End Sub

        <Fact>
        Public Sub TestFieldInStruct()
            Dim total1 = _valueType.GetMembers("total1").Single()
            Dim total2 = _valueType.GetMembers("total2").Single()
            Assert.Equal("F:Acme.ValueType.total1", total1.GetDocumentationCommentId())
            Assert.Equal(<![CDATA[
<member name="F:Acme.ValueType.total1">
<summary>Summary for total fields.</summary>
</member>
]]>.Value.Replace(vbLf, Environment.NewLine).Trim, total1.GetDocumentationCommentXml())

            Assert.Equal("F:Acme.ValueType.total2", total2.GetDocumentationCommentId())
            Assert.Equal(<![CDATA[
<member name="F:Acme.ValueType.total2">
<summary>Summary for total fields.</summary>
</member>
]]>.Value.Replace(vbLf, Environment.NewLine).Trim, total2.GetDocumentationCommentXml())
        End Sub

        <Fact>
        Public Sub TestFieldInNestedClass()
            Assert.Equal("F:Acme.Widget.NestedClass.value",
                         _widgetClass.GetTypeMembers("NestedClass").Single() _
                            .GetMembers("value").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestInstanceField()
            Assert.Equal("F:Acme.Widget.message",
                         _widgetClass.GetMembers("message").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestSharedField()
            Assert.Equal("F:Acme.Widget.defaultColor",
                         _widgetClass.GetMembers("defaultColor").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestConstField()
            Assert.Equal("F:Acme.Widget.PI",
                         _widgetClass.GetMembers("PI").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestReadOnlyField()
            Assert.Equal("F:Acme.Widget.monthlyAverage",
                         _widgetClass.GetMembers("monthlyAverage").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestArray1()
            Assert.Equal("F:Acme.Widget.array1",
                         _widgetClass.GetMembers("array1").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestArray2()
            Assert.Equal("F:Acme.Widget.array2",
                         _widgetClass.GetMembers("array2").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestEnumField()
            Dim field = _enumSymbol.GetMembers("A").Single()
            Assert.Equal("F:Acme.E.A", field.GetDocumentationCommentId())
            Assert.Equal(<![CDATA[
<member name="F:Acme.E.A">
<summary>Enum field</summary>
</member>
]]>.Value.Replace(vbLf, Environment.NewLine).Trim, field.GetDocumentationCommentXml())
        End Sub

    End Class
End Namespace
