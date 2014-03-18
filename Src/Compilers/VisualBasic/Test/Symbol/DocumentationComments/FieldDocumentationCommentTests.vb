' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class FieldDocumentationCommentTests

        Private m_compilation As VisualBasicCompilation
        Private m_acmeNamespace As NamespaceSymbol
        Private m_widgetClass As NamedTypeSymbol
        Private m_enumSymbol As NamedTypeSymbol
        Private m_valueType As NamedTypeSymbol

        Public Sub New()
            m_compilation = CompilationUtils.CreateCompilationWithMscorlib(
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

            m_acmeNamespace = DirectCast(m_compilation.GlobalNamespace.GetMembers("Acme").Single(), NamespaceSymbol)
            m_widgetClass = DirectCast(m_acmeNamespace.GetTypeMembers("Widget").Single(), NamedTypeSymbol)
            m_enumSymbol = DirectCast(m_acmeNamespace.GetTypeMembers("E").Single(), NamedTypeSymbol)
            m_ValueType = DirectCast(m_acmeNamespace.GetTypeMembers("ValueType").Single(), NamedTypeSymbol)
        End Sub

        <Fact>
        Public Sub TestFieldInStruct()
            Dim total1 = m_valueType.GetMembers("total1").Single()
            Dim total2 = m_valueType.GetMembers("total2").Single()
            Assert.Equal("F:Acme.ValueType.total1", total1.GetDocumentationCommentId())
            Assert.Equal(<![CDATA[
<member name="F:Acme.ValueType.total1">
<summary>Summary for total fields.</summary>
</member>
]]>.Value.Replace(vbLf, vbCrLf).Trim, total1.GetDocumentationCommentXml())

            Assert.Equal("F:Acme.ValueType.total2", total2.GetDocumentationCommentId())
            Assert.Equal(<![CDATA[
<member name="F:Acme.ValueType.total2">
<summary>Summary for total fields.</summary>
</member>
]]>.Value.Replace(vbLf, vbCrLf).Trim, total2.GetDocumentationCommentXml())
        End Sub

        <Fact>
        Public Sub TestFieldInNestedClass()
            Assert.Equal("F:Acme.Widget.NestedClass.value",
                         m_widgetClass.GetTypeMembers("NestedClass").Single() _
                            .GetMembers("value").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestInstanceField()
            Assert.Equal("F:Acme.Widget.message",
                         m_widgetClass.GetMembers("message").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestSharedField()
            Assert.Equal("F:Acme.Widget.defaultColor",
                         m_widgetClass.GetMembers("defaultColor").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestConstField()
            Assert.Equal("F:Acme.Widget.PI",
                         m_widgetClass.GetMembers("PI").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestReadOnlyField()
            Assert.Equal("F:Acme.Widget.monthlyAverage",
                         m_widgetClass.GetMembers("monthlyAverage").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestArray1()
            Assert.Equal("F:Acme.Widget.array1",
                         m_widgetClass.GetMembers("array1").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestArray2()
            Assert.Equal("F:Acme.Widget.array2",
                         m_widgetClass.GetMembers("array2").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestEnumField()
            Dim field = m_enumSymbol.GetMembers("A").Single()
            Assert.Equal("F:Acme.E.A", field.GetDocumentationCommentId())
            Assert.Equal(<![CDATA[
<member name="F:Acme.E.A">
<summary>Enum field</summary>
</member>
]]>.Value.Replace(vbLf, vbCrLf).Trim, field.GetDocumentationCommentXml())
        End Sub

    End Class
End Namespace
