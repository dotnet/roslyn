' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Partial Public Class GetExtendedSemanticInfoTests

        <Fact>
        Public Sub [GetXmlNamespace]()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports <xmlns:p="http://roslyn/">
Module M
    Private F1 = GetXmlNamespace(p)
    Private F2 = GetXmlNamespace()
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)

            ' GetXmlNamespace with argument.
            Dim node = FindNodeFromText(tree, "GetXmlNamespace(p)")
            Dim info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XNamespace")

            ' GetXmlNamespace with no argument.
            node = FindNodeFromText(tree, "GetXmlNamespace()")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XNamespace")
        End Sub

        <Fact>
        Public Sub XmlDocument()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Module M
    Private F = <?xml version="1.0"?><?pi data?><!-- comment --><x/>
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)

            ' XML document
            Dim node = FindNodeFromText(tree, "<?xml").Parent
            Dim info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XDocument")

            ' Declaration
            node = FindNodeFromText(tree, "<?xml")
            Assert.IsNotType(Of ExpressionSyntax)(node)

            ' Processing instruction.
            node = FindNodeFromText(tree, "<?pi")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XProcessingInstruction")

            ' Comment.
            node = FindNodeFromText(tree, "<!--")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XComment")

            ' Root element.
            node = FindNodeFromText(tree, "<x/>")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XElement")
        End Sub

        <Fact>
        Public Sub XmlProcessingInstruction()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Module M
    Private F As Object = <?p?>
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim node = FindNodeFromText(tree, "<?p?>")
            Dim info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XProcessingInstruction")
        End Sub

        <Fact>
        Public Sub XmlElement()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb">
Module M
    Public F = &lt;x a="b"&gt;&lt;y/&gt;z&lt;![CDATA[c]]&gt;&#x42;&lt;/&gt;
End Module
    </file>
</compilation>, additionalRefs:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)

            ' XML element
            Dim node = FindNodeFromText(tree, "<x ").Parent
            Dim info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XElement")

            ' Start tag.
            node = FindNodeFromText(tree, "<x ")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XElement")

            ' End tag.
            node = FindNodeFromText(tree, "</>")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XElement")

            ' Element name.
            node = FindNodeFromText(tree, "x ")
            info = semanticModel.GetSemanticInfoSummary(node)
            Assert.Null(info.Type)

            ' Attribute.
            node = FindNodeFromText(tree, "a=").Parent
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XAttribute")

            ' Attribute name.
            node = FindNodeFromText(tree, "a=")
            info = semanticModel.GetSemanticInfoSummary(node)
            Assert.Null(info.Type)

            ' Attribute value.
            node = FindNodeFromText(tree, """b""")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "String")

            ' Sub element
            node = FindNodeFromText(tree, "<y/>")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XElement")

            ' Text.
            node = FindNodeFromText(tree, "z")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "String")

            ' CDATA.
            node = FindNodeFromText(tree, "<![CDATA[c]]>")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XCData")

            ' Entity.
            node = FindNodeFromText(tree, "B")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "String")
        End Sub

        <Fact>
        Public Sub XmlName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb">
Module M
    Sub Main()
        Dim x = &lt;xmlliteral&gt;
        &lt;/xmlliteral&gt;
    End Sub
End Module
    </file>
</compilation>, additionalRefs:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim syntax = tree.GetRoot().DescendantNodes().OfType(Of XmlNameSyntax).Last
            Assert.Equal("xmlliteral", syntax.ToString())

            Dim symbolInfo = semanticModel.GetSymbolInfo(syntax)
            Assert.Null(symbolInfo.Symbol)

            Dim typeInfo = semanticModel.GetTypeInfo(syntax)
            Assert.Null(typeInfo.Type)
        End Sub

        ' Redundant xmlns attributes (matching Imports) will be dropped.
        ' Ensure we are still generating BoundNodes for semantic info.
        <Fact>
        Public Sub XmlnsAttribute()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports <xmlns:p="http://roslyn/">
Module M
    Private F As Object = <x xmlns:p="http://roslyn/"/>
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim node = FindNodeOfTypeFromText(Of XmlAttributeSyntax)(tree, "xmlns:p=""http://roslyn/""/>")
            Dim info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XAttribute")
        End Sub

        <Fact>
        Public Sub XmlPrefix()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports <xmlns:p="http://roslyn/p">
Module M
    Private F As Object = p
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim node = FindNodeOfTypeFromText(Of EqualsValueSyntax)(tree, "= p").Value
            Dim info = semanticModel.GetSemanticInfoSummary(node)
            Assert.True(DirectCast(info.Type, TypeSymbol).IsErrorType())
        End Sub

        <Fact>
        Public Sub XmlEmbeddedExpression()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Module M
    Private F1 As String = "y"
    Private F2 = <x <%= F1 %>=""/>
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)

            ' Embedded expression.
            Dim node = FindNodeFromText(tree, "<%= F1 %>")
            Dim info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "String")

            ' Expression only.
            node = FindNodeFromText(tree, "F1 %>")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "String")
        End Sub

        <Fact>
        Public Sub XmlElementAccess()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports <xmlns:p="...">
Module M
    Private F = <x/>.<y>.<p:z>
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)

            ' XElement member access.
            Dim node = FindNodeFromText(tree, "<y>")
            Dim info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "IEnumerable(Of XElement)")

            ' IEnumerable(Of XElement) member access.
            node = FindNodeFromText(tree, "<p:z>")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "IEnumerable(Of XElement)")

            ' Member access name.
            node = FindNodeFromText(tree, "y")
            info = semanticModel.GetSemanticInfoSummary(node)
            Assert.Null(info.Type)

            ' Member access qualified name.
            node = FindNodeFromText(tree, "p:").Parent
            info = semanticModel.GetSemanticInfoSummary(node)
            Assert.Null(info.Type)

            ' Member access local name.
            node = FindNodeFromText(tree, "z")
            info = semanticModel.GetSemanticInfoSummary(node)
            Assert.Null(info.Type)
        End Sub

        <Fact>
        Public Sub XmlDescendantAccess()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports <xmlns:p="...">
Module M
    Private F = <x/>...<y>...<p:z>
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)

            ' XElement member access.
            Dim node = FindNodeFromText(tree, "<y>")
            Dim info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "IEnumerable(Of XElement)")

            ' IEnumerable(Of XElement) member access.
            node = FindNodeFromText(tree, "<p:z>")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "IEnumerable(Of XElement)")

            ' Member access name.
            node = FindNodeFromText(tree, "y")
            info = semanticModel.GetSemanticInfoSummary(node)
            Assert.Null(info.Type)

            ' Member access qualified name.
            node = FindNodeFromText(tree, "p:").Parent
            info = semanticModel.GetSemanticInfoSummary(node)
            Assert.Null(info.Type)

            ' Member access local name.
            node = FindNodeFromText(tree, "z")
            info = semanticModel.GetSemanticInfoSummary(node)
            Assert.Null(info.Type)
        End Sub

        <Fact>
        Public Sub XmlAttributeAccess()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports <xmlns:p="...">
Module M
    Private F1 = <x/>.@y
    Private F2 = <x/>.@p:z
    Private F3 = <x/>.@<z>
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)

            ' XElement attribute access.
            Dim node = FindNodeFromText(tree, "@y")
            Dim info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "String")

            ' IEnumerable(Of XElement) attribute access.
            node = FindNodeFromText(tree, "@p:z")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "String")

            ' XElement attribute access, bracketed name syntax.
            node = FindNodeFromText(tree, "@<z>")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "String")

            ' Member access name.
            node = FindNodeFromText(tree, "y")
            info = semanticModel.GetSemanticInfoSummary(node)
            Assert.Null(info.Type)

            ' Member access qualified name.
            node = FindNodeFromText(tree, "p:").Parent
            info = semanticModel.GetSemanticInfoSummary(node)
            Assert.Null(info.Type)

            ' Member access local name.
            node = FindNodeFromText(tree, "z")
            info = semanticModel.GetSemanticInfoSummary(node)
            Assert.Null(info.Type)

            ' Member access bracketed name.
            node = FindNodeFromText(tree, "<z>")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "String")
        End Sub

        <Fact>
        Public Sub ValueExtensionProperty()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Module M
    Sub M()
        Dim x = <x/>
        x.<y>.Value = x.<z>.Value
    End Sub
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)
            compilation.AssertNoErrors()
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            ValueExtensionPropertyCore(model, FindNodeOfTypeFromText(Of MemberAccessExpressionSyntax)(tree, "x.<y>.Value"))
            ValueExtensionPropertyCore(model, FindNodeOfTypeFromText(Of MemberAccessExpressionSyntax)(tree, "x.<z>.Value"))
        End Sub

        Private Sub ValueExtensionPropertyCore(model As SemanticModel, expr As MemberAccessExpressionSyntax)
            Dim info = model.GetSymbolInfo(expr)
            Dim symbol = TryCast(info.Symbol, PropertySymbol)
            Assert.NotNull(symbol)
            CheckSymbol(symbol, "Property InternalXmlHelper.Value As String")
            Assert.Equal(0, symbol.GetMethod.Parameters().Length)
            Assert.Equal(1, symbol.SetMethod.Parameters().Length)
        End Sub

        <WorkItem(545659, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545659")>
        <Fact>
        Public Sub LookupValueExtensionProperty()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Xml.Linq
Structure S
    Implements IEnumerable(Of XElement)
    Private Function GetEnumerator() As IEnumerator(Of XElement) Implements IEnumerable(Of XElement).GetEnumerator
        Return Nothing
    End Function
    Private Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Structure
Class C
    Implements IEnumerable(Of XElement)
    Private Function GetEnumerator() As IEnumerator(Of XElement) Implements IEnumerable(Of XElement).GetEnumerator
        Return Nothing
    End Function
    Private Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Nothing
    End Function
End Class
Module M
    Sub M(Of T As IEnumerable(Of XElement))(_1 As IEnumerable(Of XElement), _2 As C, _3 As S, _4 As T, _5 As IEnumerable(Of Object))
        Dim o As Object
        o = _1.Value
        o = _2.Value
        o = _3.Value
        o = _4.Value
        o = _5.Value
    End Sub
End Module
]]></file>
</compilation>, additionalRefs:=XmlReferences)
            compilation.AssertTheseDiagnostics(<errors><![CDATA[
BC30456: 'Value' is not a member of 'IEnumerable(Of Object)'.
        o = _5.Value
            ~~~~~~~~
]]></errors>)
            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim position = FindNodeFromText(tree, "_1.Value").SpanStart
            Dim method = compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("M").GetMember(Of MethodSymbol)("M")
            Dim n = method.Parameters.Length - 1
            For i = 0 To n
                Dim parameter = method.Parameters(i)
                Dim type = parameter.Type
                Dim descriptions = If(i < n, {"Property InternalXmlHelper.Value As String"}, {})
                Dim symbols = model.LookupSymbols(position, container:=type, name:="Value", includeReducedExtensionMethods:=True)
                CheckSymbols(symbols, descriptions)
                symbols = model.LookupSymbols(position, container:=Nothing, name:="Value", includeReducedExtensionMethods:=True)
                CheckSymbols(symbols)
                symbols = model.LookupSymbols(position, container:=type, name:=Nothing, includeReducedExtensionMethods:=True)
                symbols = symbols.WhereAsArray(Function(s) s.Name = "Value")
                CheckSymbols(symbols, descriptions)
            Next
        End Sub

        <WorkItem(544421, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544421")>
        <Fact()>
        Public Sub XmlEndElementNoMatchingStart()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Module M
    Private F = <?xml version="1.0"?><?p?></x>
End Module
    ]]></file>
</compilation>, additionalRefs:=XmlReferences)
            Dim tree = compilation.SyntaxTrees(0)
            Dim semanticModel = compilation.GetSemanticModel(tree)

            ' XDocument.
            Dim node = FindNodeFromText(tree, "<?xml").Parent
            Dim info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XDocument")

            ' Invalid root XmlElement.
            node = FindNodeFromText(tree, "</x>")
            info = semanticModel.GetSemanticInfoSummary(node)
            CheckSymbol(info.Type, "XElement")
        End Sub

        <WorkItem(545167, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545167")>
        <Fact()>
        Public Sub XmlElementEndTag()
            Dim compilation = CreateCompilationWithMscorlib(
            <compilation>
                <file name="a.vb"><![CDATA[
Module M
    Private F As String = </>.ToString()'BIND:"</>"
End Module
    ]]></file>
            </compilation>)

            Dim semanticSummary = CompilationUtils.GetSemanticInfoSummary(Of XmlElementSyntax)(compilation, "a.vb")

            Assert.Equal("System.Xml.Linq.XElement[missing]", semanticSummary.Type.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticSummary.Type.TypeKind)
            Assert.Equal("System.Xml.Linq.XElement[missing]", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.Equal(TypeKind.Error, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(CandidateReason.None, semanticSummary.CandidateReason)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)

            Assert.Equal(0, semanticSummary.MemberGroup.Length)

            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

    End Class

End Namespace
