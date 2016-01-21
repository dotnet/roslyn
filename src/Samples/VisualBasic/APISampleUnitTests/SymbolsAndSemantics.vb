' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Xunit

Public Class SymbolsAndSemantics

    <Fact>
    Public Sub GetExpressionType()
        Dim code =
<text>
Class C
    Public Shared Sub Method()
        Dim local As String = New C().ToString() &amp; String.Empty
    End Sub
End Class
</text>.GetCode()

        Dim testCode = New TestCodeContainer(code)

        Dim localDeclaration = testCode.SyntaxTree.GetRoot().DescendantNodes().OfType(Of LocalDeclarationStatementSyntax).First()
        Dim initializer = localDeclaration.Declarators.First().Initializer.Value
        Dim semanticInfo = testCode.SemanticModel.GetTypeInfo(initializer)
        Assert.Equal("String", semanticInfo.Type.Name)
    End Sub

    <Fact>
    Public Sub BindNameToSymbol()
        Dim code = New TestCodeContainer("Imports System")
        Dim compilationUnit = CType(code.SyntaxTree.GetRoot(), CompilationUnitSyntax)

        Dim name = CType(compilationUnit.Imports(0).ImportsClauses.First(), SimpleImportsClauseSyntax).Name
        Assert.Equal("System", name.ToString())

        Dim nameInfo = code.SemanticModel.GetSymbolInfo(name)
        Dim nameSymbol = CType(nameInfo.Symbol, INamespaceSymbol)
        Assert.True(nameSymbol.GetNamespaceMembers().Any(Function(s) s.Name = "Collections"))
    End Sub

    <Fact>
    Public Sub GetDeclaredSymbol()
        Dim code =
<text>
Namespace Acme
    Friend Class C$lass1
    End Class
End Namespace
</text>.GetCode()

        Dim testCode = New TestCodeContainer(code)
        Dim symbol = testCode.SemanticModel.GetDeclaredSymbol(CType(testCode.SyntaxNode, TypeStatementSyntax))

        Assert.Equal(True, symbol.CanBeReferencedByName)
        Assert.Equal("Acme", symbol.ContainingNamespace.Name)
        Assert.Equal(Accessibility.Friend, symbol.DeclaredAccessibility)
        Assert.Equal(SymbolKind.NamedType, symbol.Kind)
        Assert.Equal("Class1", symbol.Name)
        Assert.Equal("Acme.Class1", symbol.ToDisplayString())
        Assert.Equal("Acme.Class1", symbol.ToString())
    End Sub

    <Fact>
    Public Sub GetSymbolXmlDocComments()
        Dim code =
<text>
''' &lt;summary&gt;
''' This is a test class!
''' &lt;/summary&gt;
Class C$lass1
End Class
</text>.GetCode()

        Dim testCode = New TestCodeContainer(code)

        Dim symbol = testCode.SemanticModel.GetDeclaredSymbol(CType(testCode.SyntaxNode, TypeStatementSyntax))
        Dim actualXml = symbol.GetDocumentationCommentXml()
        Dim expectedXml = "<member name=""T:Class1""> <summary> This is a test class! </summary></member>"
        Assert.Equal(expectedXml, actualXml.Replace(vbCr, "").Replace(vbLf, ""))
    End Sub

    <Fact>
    Public Sub SymbolDisplayFormatTest()
        Dim code =
<text>
Class C1(Of T)
End Class
Class C2
    Public Shared Function M(Of TSource)(source as C1(Of TSource), index as Integer) As TSource
    End Function
End Class
</text>.GetCode()

        Dim testCode = New TestCodeContainer(code)

        Dim displayFormat = New SymbolDisplayFormat(
            genericsOptions:=
                SymbolDisplayGenericsOptions.IncludeTypeParameters Or
                SymbolDisplayGenericsOptions.IncludeVariance,
            memberOptions:=
                SymbolDisplayMemberOptions.IncludeParameters Or
                SymbolDisplayMemberOptions.IncludeModifiers Or
                SymbolDisplayMemberOptions.IncludeAccessibility Or
                SymbolDisplayMemberOptions.IncludeType Or
                SymbolDisplayMemberOptions.IncludeContainingType,
            kindOptions:=
                SymbolDisplayKindOptions.IncludeMemberKeyword,
            parameterOptions:=
                SymbolDisplayParameterOptions.IncludeExtensionThis Or
                SymbolDisplayParameterOptions.IncludeType Or
                SymbolDisplayParameterOptions.IncludeName Or
                SymbolDisplayParameterOptions.IncludeDefaultValue,
            miscellaneousOptions:=
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

        Dim symbol = testCode.Compilation.SourceModule.GlobalNamespace.GetTypeMembers("C2").First().GetMembers("M").First()
        Assert.Equal(
            "Public Shared Function C2.M(Of TSource)(source As C1(Of TSource), index As Integer) As TSource",
            symbol.ToDisplayString(displayFormat))
    End Sub

    <Fact>
    Public Sub EnumerateSymbolsInCompilation()
        Dim file1 =
<text>
Public Class Animal
    Public Overridable Sub MakeSound()
    End Sub
End Class
</text>.GetCode()

        Dim file2 =
<text>
Class Cat
    Inherits Animal

    Public Overrides Sub MakeSound()
    End Sub
End Class
</text>.GetCode()


        Dim comp = VisualBasicCompilation.Create(
            "test",
            syntaxTrees:={SyntaxFactory.ParseSyntaxTree(file1), SyntaxFactory.ParseSyntaxTree(file2)},
            references:={MetadataReference.CreateFromFile(GetType(Object).Assembly.Location)})

        Dim globalNamespace = comp.SourceModule.GlobalNamespace

        Dim builder = New StringBuilder()
        EnumSymbols(globalNamespace, builder)

        Dim expected = "Global" & vbCrLf &
                       "Animal" & vbCrLf &
                       "Public Sub New()" & vbCrLf &
                       "Public Overridable Sub MakeSound()" & vbCrLf &
                       "Cat" & vbCrLf &
                       "Public Sub New()" & vbCrLf &
                       "Public Overrides Sub MakeSound()" & vbCrLf

        Assert.Equal(expected, builder.ToString())
    End Sub

    Private Sub EnumSymbols(symbol As ISymbol, builder As StringBuilder)
        builder.AppendLine(symbol.ToString())

        For Each childSymbol In GetMembers(symbol)
            EnumSymbols(childSymbol, builder)
        Next
    End Sub

    Private Function GetMembers(parent As ISymbol) As IEnumerable(Of ISymbol)
        Dim container = TryCast(parent, INamespaceOrTypeSymbol)
        If container IsNot Nothing Then
            Return container.GetMembers().AsEnumerable()
        End If

        Return Enumerable.Empty(Of ISymbol)()
    End Function

    <Fact>
    Public Sub AnalyzeRegionControlFlow()
        Dim code =
<text>
Class C
    Public Sub F()
        Goto L1 ' 1

'start
        L1: Stop

        If False Then
            Return
        End If
'end
        Goto L1 ' 2
    End Sub
End Class
</text>.GetCode()

        Dim testCode = New TestCodeContainer(code)

        Dim firstStatement As StatementSyntax = Nothing
        Dim lastStatement As StatementSyntax = Nothing
        testCode.GetStatementsBetweenMarkers(firstStatement, lastStatement)

        Dim controlFlowAnalysis1 = testCode.SemanticModel.AnalyzeControlFlow(firstStatement, lastStatement)
        Assert.Equal(1, controlFlowAnalysis1.EntryPoints.Count())
        Assert.Equal(1, controlFlowAnalysis1.ExitPoints.Count())
        Assert.True(controlFlowAnalysis1.EndPointIsReachable)

        Dim methodBlock = testCode.SyntaxTree.GetRoot().DescendantNodes().OfType(Of MethodBlockSyntax)().First()
        Dim controlFlowAnalysis2 = testCode.SemanticModel.AnalyzeControlFlow(methodBlock.Statements.First, methodBlock.Statements.Last)
        Assert.False(controlFlowAnalysis2.EndPointIsReachable)
    End Sub

    <Fact>
    Public Sub AnalyzeRegionDataFlow()
        Dim code =
<text>
Class C
    Public Sub F(x As Integer)
        Dim a As Integer
'start
        Dim b As Integer
        Dim x As Integer
        Dim y As Integer = 1

        If True Then
            Dim z As String = "a"
        End If
'end
        Dim c As Integer
    End Sub
End Class
</text>.GetCode()

        Dim testCode = New TestCodeContainer(code)

        Dim firstStatement As StatementSyntax = Nothing
        Dim lastStatement As StatementSyntax = Nothing
        testCode.GetStatementsBetweenMarkers(firstStatement, lastStatement)

        Dim dataFlowAnalysis = testCode.SemanticModel.AnalyzeDataFlow(firstStatement, lastStatement)
        Assert.Equal("b,x,y,z", String.Join(",", dataFlowAnalysis.VariablesDeclared.Select(Function(s) s.Name)))
    End Sub

    <Fact>
    Public Sub SemanticFactsTests()
        Dim code =
<text>
Class C1
    Sub M(i As Integer)
    End Sub
End Class
Class C2
    Sub M(i As Integer)
    End Sub
End Class
</text>.GetCode()

        Dim testCode = New TestCodeContainer(code)

        Dim classNode1 = CType(testCode.SyntaxTree.GetRoot().FindToken(testCode.Text.IndexOf("C1")).Parent, ClassStatementSyntax)
        Dim classNode2 = CType(testCode.SyntaxTree.GetRoot().FindToken(testCode.Text.IndexOf("C2")).Parent, ClassStatementSyntax)

        Dim class1 = testCode.SemanticModel.GetDeclaredSymbol(classNode1)
        Dim class2 = testCode.SemanticModel.GetDeclaredSymbol(classNode2)

        Dim method1 = CType(class1.GetMembers().First(), IMethodSymbol)
        Dim method2 = CType(class2.GetMembers().First(), IMethodSymbol)

        ' TODO: this API has been made internal. What is the replacement? Do we even need it here?
        ' Assert.IsTrue(Symbol.HaveSameSignature(method1, method2))
    End Sub

    <Fact>
    Public Sub FailedOverloadResolution()
        Dim code =
<text>
Option Strict On
Module Module1
    Sub Main()
        X$.F("hello")
    End Sub
End Module
Module X
    Sub F()
    End Sub
    Sub F(i As Integer)
    End Sub
End Module
</text>.GetCode()

        Dim testCode = New TestCodeContainer(code)

        Dim expression = CType(testCode.SyntaxNode, ExpressionSyntax)
        Dim typeInfo = testCode.SemanticModel.GetTypeInfo(expression)
        Dim semanticInfo = testCode.SemanticModel.GetSymbolInfo(expression)

        Assert.Null(typeInfo.Type)
        Assert.Null(typeInfo.ConvertedType)
        Assert.Null(semanticInfo.Symbol)
        Assert.Equal(CandidateReason.OverloadResolutionFailure, semanticInfo.CandidateReason)
        Assert.Equal(1, semanticInfo.CandidateSymbols.Count)

        Assert.Equal("Public Sub F(i As Integer)", semanticInfo.CandidateSymbols(0).ToDisplayString())
        Assert.Equal(SymbolKind.Method, semanticInfo.CandidateSymbols(0).Kind)

        Dim memberGroup = testCode.SemanticModel.GetMemberGroup(expression)

        Assert.Equal(2, memberGroup.Count)

        Dim sortedMethodGroup = Aggregate s In memberGroup.AsEnumerable()
                                Order By s.ToDisplayString()
                                Into ToArray()

        Assert.Equal("Public Sub F()", sortedMethodGroup(0).ToDisplayString())
        Assert.Equal("Public Sub F(i As Integer)", sortedMethodGroup(1).ToDisplayString())
    End Sub
End Class
