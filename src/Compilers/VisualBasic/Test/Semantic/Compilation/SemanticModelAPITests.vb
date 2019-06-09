' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    ''' Note: 
    ''' * Tests for Flow Analysis APIs are under FlowAnalysis folder
    ''' * Tests for GetDeclaredSymbol API are in SemanticModelGetDeclaredSymbolAPITests.vb
    ''' * Tests for LookupSymbols API are in SemanticModelLookupSymbolsAPITests.vb
    Public Class SemanticModelAPITests
        Inherits SemanticModelTestBase

#Region "Get Various Semantic Info, such as GetSymbolInfo, GetTypeInfo"

        <WorkItem(541500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541500")>
        <Fact()>
        Public Sub TestModuleNamespaceClassNesting()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation name="TestModuleNamespaceClassNesting">
    <file name="a.vb">
Module
  Namespace
    Public Class
        Public Shared x As Integer1 = 0
    End Class
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)
            Dim syntaxNode = tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierName, 2).AsNode()
            Dim info = model.GetSemanticInfoSummary(DirectCast(syntaxNode, ExpressionSyntax))
            Assert.Equal(CandidateReason.None, info.CandidateReason)
        End Sub

        <Fact(), WorkItem(543532, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543532")>
        Public Sub GetSymbolInfoForImplicitDeclaredControlVariable()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Friend Module M
    Sub Main(args As String())
        For i = 0 To 1
            Console.Write("Hi")
        Next

        For Each i In args

        Next
    End Sub

End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim children = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim forstat = children.OfType(Of ForStatementSyntax).First()
            Dim ctrlvar = TryCast(forstat.ControlVariable, ExpressionSyntax)
            Assert.NotNull(ctrlvar)
            Dim syminfo = semanticModel.GetSymbolInfo(ctrlvar)
            Assert.NotNull(syminfo.Symbol)
            Assert.Equal(SymbolKind.Local, syminfo.Symbol.Kind)

            Dim foreach = children.OfType(Of ForEachStatementSyntax).First()
            ctrlvar = TryCast(foreach.ControlVariable, ExpressionSyntax)
            Assert.NotNull(ctrlvar)
            syminfo = semanticModel.GetSymbolInfo(ctrlvar)
            Assert.NotNull(syminfo.Symbol)
            Assert.Equal(SymbolKind.Local, syminfo.Symbol.Kind)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub GetSymbolInfoForVarianceConversion()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Class A : End Class
Class B
    Inherits A
End Class

Module Module1
    Sub Main()
        Dim c1 As IEnumerable(Of B) = New List(Of B) From {New B}
        Dim c2 As IEnumerable(Of A) = c1
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of LocalDeclarationStatementSyntax).Skip(1).First
            Dim expr1 As ExpressionSyntax = node.Declarators(0).Initializer.Value

            Assert.Equal("c1", expr1.ToString())

            Dim infoP = semanticModel.GetSemanticInfoSummary(expr1)
            Dim node2 = CompilationUtils.FindTokenFromText(tree, "c2").Parent

            Dim declSym = semanticModel.GetDeclaredSymbol(DirectCast(node2, ModifiedIdentifierSyntax))
            Dim IEnumerableOfA As TypeSymbol = DirectCast(declSym, LocalSymbol).Type

            Assert.Equal(IEnumerableOfA, infoP.ConvertedType)
            Assert.Equal(SymbolKind.Local, infoP.Symbol.Kind)
        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub GetSymbolInfoForVarianceConversionWithStaticLocals()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System.Collections.Generic
Class A : End Class
Class B
    Inherits A
End Class

Module Module1
    Sub Main()
        Static c1 As IEnumerable(Of B) = New List(Of B) From {New B}
        Static c2 As IEnumerable(Of A) = c1
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of LocalDeclarationStatementSyntax).Skip(1).First
            Dim expr1 As ExpressionSyntax = node.Declarators(0).Initializer.Value

            Assert.Equal("c1", expr1.ToString())

            Dim infoP = semanticModel.GetSemanticInfoSummary(expr1)
            Dim node2 = CompilationUtils.FindTokenFromText(tree, "c2").Parent

            Dim declSym = semanticModel.GetDeclaredSymbol(DirectCast(node2, ModifiedIdentifierSyntax))
            Dim IEnumerableOfA As TypeSymbol = DirectCast(declSym, LocalSymbol).Type

            Assert.Equal(IEnumerableOfA, infoP.ConvertedType)
            Assert.Equal(SymbolKind.Local, infoP.Symbol.Kind)
        End Sub

        <Fact(), WorkItem(542861, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542861"), WorkItem(529673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529673")>
        Public Sub GetSymbolInfoForAccessorParameters()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Public Class Test

    Dim _Items(3) As Object
    Default Public Property Item(index As Integer) As Object
        Get
            Return _Items(index)
        End Get

        Set(value As Object)
            _Items(index) = value
        End Set

    End Property
End Class
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim descendants = tree.GetCompilationUnitRoot().DescendantNodes()
            Dim paras = descendants.OfType(Of ParameterSyntax)()
            Dim parasym = semanticModel.GetDeclaredSymbol(paras.First())
            Dim ploc = parasym.Locations(0)

            Dim args = descendants.OfType(Of SimpleArgumentSyntax).Where(Function(s) s.ToString() = "index").Select(Function(s) s)
            Assert.Equal(2, args.Count())
            Dim argsym1 = semanticModel.GetSymbolInfo(args.First().Expression).Symbol
            Dim argsym2 = semanticModel.GetSymbolInfo(args.Last().Expression).Symbol
            Assert.NotNull(argsym1)
            Assert.NotNull(argsym2)

            Assert.Equal(ploc, argsym1.Locations(0))
            Assert.Equal(ploc, argsym2.Locations(0))

            Assert.Equal(parasym.Kind, argsym1.Kind)
            Assert.Equal(parasym.Kind, argsym2.Kind)

            Assert.NotEqual(parasym, argsym1)
            Assert.NotEqual(parasym, argsym2)
        End Sub

        <Fact>
        Public Sub LabelSymbolsAreEquivalentAcrossSemanticModelsFromSameCompilation()
            Dim text = <code>
                           Public Class C
                               Public Sub M()
                           label:
                                   goto label
                               End Sub
                           End Class
                       </code>.Value
            Dim tree = Parse(text)
            Dim comp = CreateCompilationWithMscorlib40({tree})

            Dim model1 = comp.GetSemanticModel(tree)
            Dim model2 = comp.GetSemanticModel(tree)
            Assert.NotEqual(model1, model2)

            Dim statement = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of GoToStatementSyntax)().First()
            Dim symbol1 = model1.GetSymbolInfo(statement.Label).Symbol
            Dim symbol2 = model2.GetSymbolInfo(statement.Label).Symbol

            Assert.Equal(False, symbol1 Is symbol2)
            Assert.Equal(symbol1, symbol2)
        End Sub

#End Region

#Region "GetSpeculativeXXXInfo"

        <Fact()>
        Public Sub BindExpression()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Imports System   

Class B
    Public f1 as Integer
End Class

Class M
    Public Sub Main()
        Dim bInstance As B
        Console.WriteLine("hi") 'BIND:"Console.WriteLine"
    End Sub
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.GlobalNamespace
            Dim classB = DirectCast(globalNS.GetMembers("B").Single(), NamedTypeSymbol)
            Dim fieldF1 = DirectCast(classB.GetMembers("f1").Single(), FieldSymbol)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim position As Integer = CompilationUtils.FindPositionFromText(tree, "WriteLine")
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim treeForExpression = CompilationUtils.CreateParseTree(
                <file name="speculative.vb">
Module Q
   Sub x()
      a = bInstance.GetType()
   End Sub
End Module
                </file>)

            Dim expression = CompilationUtils.FindNodeOfTypeFromText(Of NameSyntax)(treeForExpression, "bInstance")
            Dim semanticInfo As SemanticInfoSummary = semanticModel.GetSpeculativeSemanticInfoSummary(position, expression, SpeculativeBindingOption.BindAsExpression)

            Assert.Equal(SymbolKind.Local, semanticInfo.Symbol.Kind)
            Assert.Equal("bInstance", semanticInfo.Symbol.Name)
            Assert.Equal("B", semanticInfo.Type.ToTestDisplayString())

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BindExpressionWithErrors()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Option Strict On
Imports System   

Class B
    Public f1 as Integer
    Public Sub goo(x as Object)
    End Sub
End Class

Class M
    Public Shared Sub Main()
        Dim o As Object
        Console.WriteLine("hi")
    End Sub
    Public Sub Bar()
        dim zip as Object
    End Sub
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.GlobalNamespace
            Dim classB = DirectCast(globalNS.GetMembers("B").Single(), NamedTypeSymbol)
            Dim fieldF1 = DirectCast(classB.GetMembers("f1").Single(), FieldSymbol)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim position1 = CompilationUtils.FindPositionFromText(tree, "WriteLine")
            Dim position2 = CompilationUtils.FindPositionFromText(tree, "zip")
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim treeForExpression = CompilationUtils.CreateParseTree(
                <file name="speculative.vb">
Class Q
   Sub x()
      o = Me
   End Sub
End Class
                </file>)

            Dim expression = CompilationUtils.FindNodeOfTypeFromText(Of ExpressionSyntax)(treeForExpression, "Me")

            Dim semanticInfo As SemanticInfoSummary = semanticModel.GetSpeculativeSemanticInfoSummary(position1, expression, SpeculativeBindingOption.BindAsExpression)
            Assert.Equal("M", semanticInfo.Type.ToTestDisplayString())

            semanticInfo = semanticModel.GetSpeculativeSemanticInfoSummary(position2, expression, SpeculativeBindingOption.BindAsExpression)
            Assert.Equal(SymbolKind.Parameter, semanticInfo.Symbol.Kind)
            Assert.True(DirectCast(semanticInfo.Symbol, ParameterSymbol).IsMe)
            Assert.Equal("M", semanticInfo.Type.ToTestDisplayString())

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact()>
        Public Sub BindAsExpressionVsBindAsType()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Option Strict On
Imports System   
Imports B = System.Console

Class M
    Public B As Integer
    Public Sub M()
        Console.WriteLine("hi") 
    End Sub
End Class
    </file>
</compilation>)

            Dim globalNS = compilation.GlobalNamespace
            Dim classM = DirectCast(globalNS.GetMembers("M").Single(), NamedTypeSymbol)
            Dim fieldB = DirectCast(classM.GetMembers("B").Single(), FieldSymbol)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position1 = CompilationUtils.FindPositionFromText(tree, "WriteLine")
            Dim expression = SyntaxFactory.ParseExpression("B")

            Dim semanticInfoExpression = semanticModel.GetSpeculativeSemanticInfoSummary(position1, expression, SpeculativeBindingOption.BindAsExpression)
            Assert.Equal(fieldB, semanticInfoExpression.Symbol)
            Assert.Equal("System.Int32", semanticInfoExpression.Type.ToTestDisplayString())
            Assert.Null(semanticInfoExpression.Alias)

            semanticInfoExpression = semanticModel.GetSpeculativeSemanticInfoSummary(position1, expression, SpeculativeBindingOption.BindAsTypeOrNamespace)
            Assert.Equal("System.Console", semanticInfoExpression.Symbol.ToTestDisplayString())
            Assert.Equal("System.Console", semanticInfoExpression.Type.ToTestDisplayString())
            Assert.NotNull(semanticInfoExpression.Alias)
            Assert.Equal("B=System.Console", semanticInfoExpression.Alias.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub BindSpeculativeAttribute()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Option Strict On
Imports System
Imports O = System.ObsoleteAttribute

Class C
    Class DAttribute : Inherits Attribute
    End Class

    Class E
    End Class

    Sub Goo(Of O)()
    End Sub

    &lt;Serializable&gt; Private i As Integer
End Class    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithEmbedVbCoreRuntime(True))

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim position1 = CompilationUtils.FindPositionFromText(tree, "Class C")
            Dim attr1 = ParseAttributeSyntax("<Obsolete>")

            Dim symbolInfo = semanticModel.GetSpeculativeSymbolInfo(position1, attr1)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", symbolInfo.Symbol.ToTestDisplayString())

            Dim attr2 = ParseAttributeSyntax("<ObsoleteAttribute(4)>")
            symbolInfo = semanticModel.GetSpeculativeSymbolInfo(position1, attr2)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.OverloadResolutionFailure)
            Assert.Equal(3, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", symbolInfo.CandidateSymbols(1).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String, [error] As System.Boolean)", symbolInfo.CandidateSymbols(2).ToTestDisplayString())

            Dim attr3 = ParseAttributeSyntax("<O(""hello"")>")
            symbolInfo = semanticModel.GetSpeculativeSymbolInfo(position1, attr3)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", symbolInfo.Symbol.ToTestDisplayString())

            Dim attr4 = ParseAttributeSyntax("<P>")
            symbolInfo = semanticModel.GetSpeculativeSymbolInfo(position1, attr4)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim position2 = CompilationUtils.FindPositionFromText(tree, "Class E")
            Dim attr5 = ParseAttributeSyntax("<D>")
            symbolInfo = semanticModel.GetSpeculativeSymbolInfo(position2, attr5)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub C.DAttribute..ctor()", symbolInfo.Symbol.ToTestDisplayString())

            Dim position3 = CompilationUtils.FindPositionFromText(tree, "Sub Goo")
            Dim attr6 = ParseAttributeSyntax("<O(""hello"")>")
            symbolInfo = semanticModel.GetSpeculativeSymbolInfo(position2, attr6)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", symbolInfo.Symbol.ToTestDisplayString())

            Dim position4 = CompilationUtils.FindPositionFromText(tree, "Serializable")
            Dim attr7 = ParseAttributeSyntax("<D>")
            symbolInfo = semanticModel.GetSpeculativeSymbolInfo(position2, attr5)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub C.DAttribute..ctor()", symbolInfo.Symbol.ToTestDisplayString())
        End Sub

        <Fact>
        <WorkItem(92898, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems?_a=edit&id=92898")>
        <WorkItem(755801, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/755801")>
        Public Sub GetSpeculativeSymbolInfoForQualifiedNameInCref()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="GetSemanticInfo">
    <file name="a.vb"><![CDATA[
Class C
    Public Sub Bar(Of T)(ByVal x As T)
    End Sub

    Public Sub Bar(ByVal x As Integer)
    End Sub

    ''' <summary>
    ''' <see cref="Global.C.Bar(Of T)"/>
    ''' </summary>
    Public Sub F()

    End Sub
End Class]]>
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot
            Dim crefSyntax = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of CrefReferenceSyntax).Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim symbolInfo = semanticModel.GetSymbolInfo(crefSyntax.Name)
            Assert.Equal(SyntaxKind.QualifiedName, crefSyntax.Name.Kind())
            Assert.Equal("Global.C.Bar(Of T)", crefSyntax.Name.ToString())
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal(SymbolKind.Method, symbolInfo.Symbol.Kind)
            Assert.Equal("Sub C.Bar(Of T)(x As T)", symbolInfo.Symbol.ToTestDisplayString())

            Dim speculatedName = DirectCast(SyntaxFactory.ParseName("C.Bar(Of T)"), QualifiedNameSyntax)
            Dim speculativeSymbolInfo = semanticModel.GetSpeculativeSymbolInfo(crefSyntax.Name.Position, speculatedName, SpeculativeBindingOption.BindAsExpression)
            Const bug92898IsFixed = False

            If bug92898IsFixed Then
                Assert.NotNull(speculativeSymbolInfo.Symbol)
                Assert.Equal(SymbolKind.Method, speculativeSymbolInfo.Symbol.Kind)
                Assert.Equal("Sub C.Bar(Of T)(x As T)", speculativeSymbolInfo.Symbol.ToTestDisplayString())
            Else
                Assert.Null(speculativeSymbolInfo.Symbol)
            End If
        End Sub

        <Fact>
        <WorkItem(96477, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems#_a=edit&id=96477")>
        <WorkItem(1015560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015560")>
        Public Sub GetSpeculativeSymbolInfoForGenericNameInCref()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="GetSemanticInfo">
    <file name="a.vb"><![CDATA[Imports System.Collections.Generic
Module Program
    ''' <see cref="System.Collections.Generic.List(Of T).Contains(T)"/>
    Sub Main()
    End Sub
End Module]]>
    </file>
</compilation>)

            Dim tree = compilation.SyntaxTrees.Single()
            Dim root = tree.GetCompilationUnitRoot
            Dim crefSyntax = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of CrefReferenceSyntax).Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node = DirectCast(DirectCast(crefSyntax.Name, QualifiedNameSyntax).Left, QualifiedNameSyntax)
            Assert.Equal("System.Collections.Generic.List(Of T)", node.ToString())
            Dim symbolInfo = semanticModel.GetSymbolInfo(node)
            Dim oldSymbol = symbolInfo.Symbol
            Assert.NotNull(oldSymbol)
            Assert.Equal(SymbolKind.NamedType, oldSymbol.Kind)
            Assert.Equal("System.Collections.Generic.List(Of T)", oldSymbol.ToTestDisplayString())
            Assert.False(DirectCast(oldSymbol, NamedTypeSymbol).TypeArguments.Single.IsErrorType)

            Dim speculatedName = DirectCast(SyntaxFactory.ParseName("List(Of T)"), GenericNameSyntax)
            Dim speculativeSymbolInfo = semanticModel.GetSpeculativeSymbolInfo(crefSyntax.SpanStart, speculatedName, SpeculativeBindingOption.BindAsTypeOrNamespace)
            Dim newSymbol = speculativeSymbolInfo.Symbol
            Assert.NotNull(newSymbol)
            Assert.Equal(SymbolKind.NamedType, newSymbol.Kind)
            Assert.Equal("System.Collections.Generic.List(Of T)", newSymbol.ToTestDisplayString())

            Const bug96477IsFixed = False

            If bug96477IsFixed Then
                Assert.False(DirectCast(newSymbol, NamedTypeSymbol).TypeArguments.Single.IsErrorType)
                Assert.True(newSymbol.Equals(oldSymbol))
            Else
                Assert.True(DirectCast(newSymbol, NamedTypeSymbol).TypeArguments.Single.IsErrorType)
            End If
        End Sub
#End Region

#Region "TryGetSpeculativeSemanticModel"


        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelForExpression_ConstantInfo()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="GetSemanticInfo">
    <file name="a.vb">
Imports System   

Class B
    Public f1 as Integer
End Class

Class M
    Public Sub Main()
        Dim bInstance As B = New B()
        Console.WriteLine("hi")
    End Sub
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(1), TypeBlockSyntax)
            Dim methodBlock = DirectCast(typeBlock.Members(0), MethodBlockSyntax)
            Dim originalStatement = DirectCast(methodBlock.Statements(0), ExecutableStatementSyntax)
            Dim originalExpression = originalStatement.DescendantNodes().Where(Function(syntax) syntax.Kind = SyntaxKind.ObjectCreationExpression).FirstOrDefault()

            Dim speculatedExpression = SyntaxFactory.ParseExpression("DirectCast(3, Integer)")
            Dim speculatedStatement = originalStatement.ReplaceNode(originalExpression, speculatedExpression)
            speculatedExpression = speculatedStatement.DescendantNodes().OfType(Of CastExpressionSyntax).Single()

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Assert.False(semanticModel.IsSpeculativeSemanticModel)
            Assert.Null(semanticModel.ParentModel)
            Assert.Equal(0, semanticModel.OriginalPositionForSpeculation)

            ' Test Speculative binding.
            Dim position As Integer = originalExpression.SpanStart
            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModel(position, speculatedStatement, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Assert.True(speculativeModel.IsSpeculativeSemanticModel)
            Assert.Equal(semanticModel, speculativeModel.ParentModel)
            Assert.Equal(position, speculativeModel.OriginalPositionForSpeculation)

            Dim identifierSyntax = DirectCast(speculatedStatement, LocalDeclarationStatementSyntax).Declarators(0).Names(0)
            Dim symbol = speculativeModel.GetDeclaredSymbol(identifierSyntax)
            Assert.NotNull(symbol)
            Assert.Equal("bInstance", symbol.Name)

            Dim initializerTypeInfo = speculativeModel.GetTypeInfo(speculatedExpression)
            Assert.NotNull(initializerTypeInfo.Type)
            Assert.Equal("System.Int32", initializerTypeInfo.Type.ToTestDisplayString())
            Dim initializerConstantVal = speculativeModel.GetConstantValue(speculatedExpression)
            Assert.True(initializerConstantVal.HasValue, "must be a constant")
            Assert.Equal(3, initializerConstantVal.Value)
        End Sub

        <Fact>
        <WorkItem(680657, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/680657")>
        Public Sub TestGetSpeculativeSemanticModelInFieldInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class C
	Private y As Object = 1
End Class
    </file>
</compilation>)

            TestGetSpeculativeSemanticModelInFieldOrPropertyInitializer(compilation)
        End Sub

        <Fact>
        <WorkItem(680657, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/680657")>
        Public Sub TestGetSpeculativeSemanticModelInPropertyInitializer()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class C
	Property y As Object = 1
End Class
    </file>
</compilation>)

            TestGetSpeculativeSemanticModelInFieldOrPropertyInitializer(compilation)
        End Sub

        Private Sub TestGetSpeculativeSemanticModelInFieldOrPropertyInitializer(compilation As VisualBasicCompilation)
            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position1 = CompilationUtils.FindPositionFromText(tree, "= 1")

            ' Speculate on the EqualsValue syntax
            ' Conversion info available, ConvertedType: Object.
            Dim equalsValue = SyntaxFactory.EqualsValue(SyntaxFactory.ParseExpression(<![CDATA["hi"]]>.Value))
            Dim expression = equalsValue.Value

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModel(position1, equalsValue, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim typeInfo = speculativeModel.GetTypeInfo(expression)
            Assert.Equal("System.String", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Object", typeInfo.ConvertedType.ToTestDisplayString())
            Dim constantInfo = speculativeModel.GetConstantValue(expression)
            Assert.True(constantInfo.HasValue, "must be a constant")
            Assert.Equal("hi", constantInfo.Value)
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelInEnumMemberDecl()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Enum E
	y = 1
End Enum
    </file>
</compilation>)

            TestGetSpeculativeSemanticModelInEnumMemberDeclOrDefaultParameterValue(compilation)
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelInDefaultParameterValue()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class C
	Sub M(Optional x as Integer = 1)
    End Sub
End Class
    </file>
</compilation>)

            TestGetSpeculativeSemanticModelInEnumMemberDeclOrDefaultParameterValue(compilation)
        End Sub

        Private Sub TestGetSpeculativeSemanticModelInEnumMemberDeclOrDefaultParameterValue(compilation As VisualBasicCompilation)
            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position1 = CompilationUtils.FindPositionFromText(tree, "= 1")

            ' Speculate on the EqualsValue syntax
            ' Conversion info available, ConvertedType: Int32.
            Dim initializer = SyntaxFactory.EqualsValue(SyntaxFactory.ParseExpression("CType(0, System.Int16)"))
            Dim expression = initializer.Value

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModel(position1, initializer, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim typeInfo = speculativeModel.GetTypeInfo(expression)
            Assert.Equal("System.Int16", typeInfo.Type.ToTestDisplayString())
            Assert.Equal("System.Int32", typeInfo.ConvertedType.ToTestDisplayString())
            Dim constantInfo = speculativeModel.GetConstantValue(expression)
            Assert.True(constantInfo.HasValue, "must be a constant")
            Assert.Equal(CType(0, System.Int16), constantInfo.Value)
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelForStatement()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class C
	Private Sub M(x As Integer)
		Dim y As Integer = 1000
	End Sub
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim methodBlock = DirectCast(typeBlock.Members(0), MethodBlockSyntax)
            Dim originalStatement = methodBlock.Statements(0)

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position1 = originalStatement.SpanStart
            Dim speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement(<![CDATA[
If y > 0 Then
  Dim z As Integer = 0
  M(z)
  M(y)
End If]]>.Value), ExecutableStatementSyntax)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModel(position1, speculatedStatement, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim ifStatement = DirectCast(speculatedStatement, MultiLineIfBlockSyntax)

            Dim declStatement = DirectCast(ifStatement.Statements(0), LocalDeclarationStatementSyntax)
            Dim varDecl = declStatement.Declarators(0).Names(0)
            Dim local = speculativeModel.GetDeclaredSymbol(varDecl)
            Assert.NotNull(local)
            Assert.Equal("z", local.Name)
            Assert.Equal(SymbolKind.Local, local.Kind)
            Assert.Equal("System.Int32", DirectCast(local, LocalSymbol).Type.ToTestDisplayString())
            Dim typeSyntax = DirectCast(declStatement.Declarators(0).AsClause, SimpleAsClauseSyntax).Type
            Dim typeInfo = speculativeModel.GetTypeInfo(typeSyntax)
            Assert.NotNull(typeInfo.Type)
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())

            Dim call1 = DirectCast(ifStatement.Statements(1), ExpressionStatementSyntax)
            Dim arg = DirectCast(DirectCast(call1.Expression, InvocationExpressionSyntax).ArgumentList.Arguments(0), SimpleArgumentSyntax).Expression
            Dim argSymbolInfo = speculativeModel.GetSymbolInfo(arg)
            Assert.NotNull(argSymbolInfo.Symbol)
            Assert.Equal("z", argSymbolInfo.Symbol.Name)
            Assert.Equal(SymbolKind.Local, argSymbolInfo.Symbol.Kind)

            Dim call2 = DirectCast(ifStatement.Statements(2), ExpressionStatementSyntax)
            arg = DirectCast(DirectCast(call2.Expression, InvocationExpressionSyntax).ArgumentList.Arguments(0), SimpleArgumentSyntax).Expression
            argSymbolInfo = speculativeModel.GetSymbolInfo(arg)
            Assert.NotNull(argSymbolInfo.Symbol)
            Assert.Equal("y", argSymbolInfo.Symbol.Name)
            Assert.Equal(SymbolKind.Local, argSymbolInfo.Symbol.Kind)
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelForStatement_DeclaredLocal()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class C
	Private Sub M(x As Integer)
		Dim y As Integer = 1000
	End Sub
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim methodBlock = DirectCast(typeBlock.Members(0), MethodBlockSyntax)
            Dim originalStatement = DirectCast(methodBlock.Statements(0), ExecutableStatementSyntax)

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position1 = originalStatement.SpanStart

            ' different name local
            Dim speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement("Dim z As Integer = 0"), ExecutableStatementSyntax)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModel(position1, speculatedStatement, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim declStatement = DirectCast(speculatedStatement, LocalDeclarationStatementSyntax)
            Dim varDecl = declStatement.Declarators(0).Names(0)
            Dim local = speculativeModel.GetDeclaredSymbol(varDecl)
            Assert.NotNull(local)
            Assert.Equal("z", local.Name)
            Assert.Equal(SymbolKind.Local, local.Kind)
            Assert.Equal("System.Int32", DirectCast(local, LocalSymbol).Type.ToTestDisplayString())

            ' same name local
            speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement("Dim y As String = Nothing"), ExecutableStatementSyntax)

            success = semanticModel.TryGetSpeculativeSemanticModel(position1, speculatedStatement, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            declStatement = DirectCast(speculatedStatement, LocalDeclarationStatementSyntax)
            varDecl = declStatement.Declarators(0).Names(0)
            local = speculativeModel.GetDeclaredSymbol(varDecl)
            Assert.NotNull(local)
            Assert.Equal("y", local.Name)
            Assert.Equal(SymbolKind.Local, local.Kind)
            Assert.Equal("System.String", DirectCast(local, LocalSymbol).Type.ToTestDisplayString())
        End Sub

        <Fact>
        <WorkItem(97599, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems#_a=edit&id=97599")>
        <WorkItem(1019361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1019361")>
        Public Sub TestGetSpeculativeSemanticModelForStatement_DeclaredLocal_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Imports N
Namespace N
    Class A
        Public Const X As Integer = 1
    End Class
End Namespace

Class Program
    Sub Main()
        Dim x = N.A.X
        Dim a As A = Nothing
    End Sub
End Class
    </file>
</compilation>)

            compilation.AssertTheseDiagnostics()

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = root.Members.OfType(Of TypeBlockSyntax).First
            Dim methodBlock = DirectCast(typeBlock.Members(0), MethodBlockSyntax)
            Dim originalStatement = DirectCast(methodBlock.Statements(0), LocalDeclarationStatementSyntax)

            Assert.Equal("Dim x = N.A.X", originalStatement.ToString())

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim originalX = semanticModel.GetDeclaredSymbol(originalStatement.Declarators(0).Names(0))

            Assert.Equal("x As System.Int32", originalX.ToTestDisplayString())
            Assert.Equal(False, DirectCast(originalX, LocalSymbol).Type.IsErrorType)

            Dim position1 = originalStatement.SpanStart

            ' different initializer for local, whose type should be error type as "A" bounds to the local "a" instead of "N.A"
            Dim speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement("Dim x = A.X"), ExecutableStatementSyntax)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModel(position1, speculatedStatement, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim declStatement = DirectCast(speculatedStatement, LocalDeclarationStatementSyntax)
            Dim varDecl = declStatement.Declarators(0).Names(0)
            Dim local = speculativeModel.GetDeclaredSymbol(varDecl)

            Assert.NotNull(local)
            Assert.Equal("x", local.Name)
            Assert.Equal(SymbolKind.Local, local.Kind)

            Assert.Equal("x As System.Int32", local.ToTestDisplayString())
            Assert.NotEqual(originalX, local)

            Assert.Equal(False, DirectCast(local, LocalSymbol).Type.IsErrorType)
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelForStatement_DeclaredLabel()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class C
	Private Sub M(x As Integer)
		Dim y As Integer = 1000
	End Sub
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim methodBlock = DirectCast(typeBlock.Members(0), MethodBlockSyntax)
            Dim originalStatement = methodBlock.Statements(0)

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position1 = originalStatement.Span.End

            Dim speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement("Label: y = y + 1"), ExecutableStatementSyntax)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModel(position1, speculatedStatement, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim label = speculativeModel.GetDeclaredSymbol(speculatedStatement)
            Assert.NotNull(label)
            Assert.Equal("Label", label.Name)
            Assert.Equal(SymbolKind.Label, label.Kind)
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelForStatement_GetDeclaredLambdaParameterSymbol()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class C
	Private Sub M(x As Integer)
		Dim y As Integer = 0
	End Sub
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim methodBlock = DirectCast(typeBlock.Members(0), MethodBlockSyntax)
            Dim originalStatement = methodBlock.Statements(0)

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position1 = originalStatement.Span.End

            Dim speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement("Dim var As Func(Of Integer, Integer) = Function(z) x + z"), ExecutableStatementSyntax)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModel(position1, speculatedStatement, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim lambdaExpressionHeader = speculatedStatement.DescendantNodes().OfType(Of LambdaHeaderSyntax)().FirstOrDefault()
            Dim lambdaParam = lambdaExpressionHeader.ParameterList.Parameters(0)
            Dim parameterSymbol = speculativeModel.GetDeclaredSymbol(lambdaParam)
            Assert.NotNull(parameterSymbol)
            Assert.Equal("z", parameterSymbol.Name)
        End Sub

        <Fact, WorkItem(1084086, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1084086")>
        Public Sub TestGetSpeculativeSemanticModelForStatement_InEmptyMethodBody()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class C
	Private Sub M(x As Integer)
	End Sub
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim methodBlock = DirectCast(typeBlock.Members(0), MethodBlockSyntax)
            Dim endStatement = methodBlock.EndBlockStatement

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position1 = endStatement.SpanStart

            Dim speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement("Dim y = 0"), LocalDeclarationStatementSyntax)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModel(position1, speculatedStatement, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim local = speculativeModel.GetDeclaredSymbol(speculatedStatement.Declarators.First().Names.First)
            Assert.NotNull(local)
            Assert.Equal("y", local.Name)
            Assert.Equal(SymbolKind.Local, local.Kind)
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelForRangeArgument_InField()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
 Module Program
    ' Extract method
    Dim x(0 To 1 + 2)

    Public Static Function NewMethod() As Integer
        Return 1
    End Function
End Module

    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetRoot()
            Dim rangeArg = root.DescendantNodes().OfType(Of RangeArgumentSyntax).Single()
            Dim position1 = rangeArg.SpanStart
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim speculatedRangeArgument = rangeArg.ReplaceNode(rangeArg.UpperBound, SyntaxFactory.ParseExpression("NewMethod()"))

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModel(position1, speculatedRangeArgument, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim upperBound = speculatedRangeArgument.UpperBound
            Dim symbolInfo = speculativeModel.GetSymbolInfo(upperBound)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal(SymbolKind.Method, symbolInfo.Symbol.Kind)
            Assert.Equal("NewMethod", symbolInfo.Symbol.Name)
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelForRangeArgument_InLocal()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
 Module Program
    Public Static Function Method() As Integer
        ' Extract method
        Dim x(0 To 1 + 2)
    End Function

    Public Static Function NewMethod() As Integer
        Return 1
    End Function
End Module

    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetRoot()
            Dim rangeArg = root.DescendantNodes().OfType(Of RangeArgumentSyntax).Single()
            Dim position1 = rangeArg.SpanStart
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim speculatedRangeArgument = rangeArg.ReplaceNode(rangeArg.UpperBound, SyntaxFactory.ParseExpression("NewMethod()"))

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModel(position1, speculatedRangeArgument, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim upperBound = speculatedRangeArgument.UpperBound
            Dim symbolInfo = speculativeModel.GetSymbolInfo(upperBound)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal(SymbolKind.Method, symbolInfo.Symbol.Kind)
            Assert.Equal("NewMethod", symbolInfo.Symbol.Name)
        End Sub

        <Fact()>
        Public Sub TestArgumentsToGetSpeculativeSemanticModelAPI()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb"><![CDATA[
Class C
	<System.Obsolete>
	Private Sub M(x As Integer)
		Dim y As String = "Hello"
	End Sub
End Class]]>
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim methodBlock = DirectCast(typeBlock.Members(0), MethodBlockSyntax)
            Dim originalStatement = methodBlock.Statements(0)

            Dim model = compilation.GetSemanticModel(tree)
            Dim statement = DirectCast(methodBlock.Statements(0), LocalDeclarationStatementSyntax)
            Dim initializer = statement.Declarators(0).Initializer
            Dim attribute = methodBlock.BlockStatement.AttributeLists(0).Attributes(0)

            Dim speculativeModel As SemanticModel = Nothing
            Assert.Throws(Of ArgumentNullException)(Function() model.TryGetSpeculativeSemanticModel(statement.SpanStart, initializer:=Nothing, speculativeModel:=speculativeModel))
            Assert.Throws(Of ArgumentNullException)(Function() model.TryGetSpeculativeSemanticModel(statement.SpanStart, statement:=Nothing, speculativeModel:=speculativeModel))
            Assert.Throws(Of ArgumentNullException)(Function() model.TryGetSpeculativeSemanticModel(statement.SpanStart, attribute:=Nothing, speculativeModel:=speculativeModel))

            ' Speculate on a node from the same syntax tree.
            Assert.Throws(Of ArgumentException)(Function() model.TryGetSpeculativeSemanticModel(statement.SpanStart, initializer:=initializer, speculativeModel:=speculativeModel))
            Assert.Throws(Of ArgumentException)(Function() model.TryGetSpeculativeSemanticModel(statement.SpanStart, statement:=statement, speculativeModel:=speculativeModel))
            Assert.Throws(Of ArgumentException)(Function() model.TryGetSpeculativeSemanticModel(attribute.SpanStart, attribute:=attribute, speculativeModel:=speculativeModel))

            ' Chaining speculative semantic model is not supported.
            Dim speculatedStatement = statement.ReplaceNode(initializer.Value, SyntaxFactory.ParseExpression("0"))
            model.TryGetSpeculativeSemanticModel(statement.SpanStart, speculatedStatement, speculativeModel)
            Assert.NotNull(speculativeModel)
            Dim newSpeculativeModel As SemanticModel = Nothing
            Assert.Throws(Of InvalidOperationException)(Function() speculativeModel.TryGetSpeculativeSemanticModel(speculatedStatement.SpanStart, speculatedStatement, newSpeculativeModel))
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelOnSpeculativeSemanticModel()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb"><![CDATA[
Class C
	<System.Obsolete>
	Private Sub M(x As Integer)
		Dim y As String = "Hello"
	End Sub
End Class]]>
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim methodBlock = DirectCast(typeBlock.Members(0), MethodBlockSyntax)
            Dim originalStatement = methodBlock.Statements(0)

            Dim model = compilation.GetSemanticModel(tree)
            Dim statement = DirectCast(methodBlock.Statements(0), LocalDeclarationStatementSyntax)
            Dim expression = statement.Declarators(0).Initializer.Value
            Dim attribute = methodBlock.BlockStatement.AttributeLists(0).Attributes(0)

            Dim speculatedStatement = DirectCast(statement.ReplaceNode(expression, SyntaxFactory.ParseExpression("0")), LocalDeclarationStatementSyntax)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = model.TryGetSpeculativeSemanticModel(statement.SpanStart, speculatedStatement, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            '  Chaining speculative semantic model is not supported.
            ' (a) Expression
            Dim newSpeculatedStatement = DirectCast(statement.ReplaceNode(expression, SyntaxFactory.ParseExpression("1.1")), LocalDeclarationStatementSyntax)
            Dim newSpeculativeModel As SemanticModel = Nothing
            Assert.Throws(Of InvalidOperationException)(Sub() speculativeModel.TryGetSpeculativeSemanticModel(speculatedStatement.SpanStart, statement:=newSpeculatedStatement, speculativeModel:=newSpeculativeModel))

            ' (b) Statement
            newSpeculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement("Dim z = 0"), LocalDeclarationStatementSyntax)
            Assert.Throws(Of InvalidOperationException)(Sub() speculativeModel.TryGetSpeculativeSemanticModel(speculatedStatement.SpanStart, newSpeculatedStatement, newSpeculativeModel))
        End Sub

        ' Helper to parse an attribute.
        Private Function ParseAttributeSyntax(source As String) As AttributeSyntax
            Return DirectCast(SyntaxFactory.ParseCompilationUnit(source + " Class X" + vbCrLf + "End Class").Members.First(), TypeBlockSyntax).BlockStatement.AttributeLists.First().Attributes.First()
        End Function

        <Fact>
        Public Sub TestGetSpeculativeSemanticModelForAttribute()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Option Strict On
Imports System
Imports O = System.ObsoleteAttribute

Class C
    Class DAttribute : Inherits Attribute
    End Class

    Class E
    End Class

    Sub Goo(Of O)()
    End Sub

    &lt;Serializable&gt; Private i As Integer
End Class    </file>
</compilation>, options:=TestOptions.ReleaseExe.WithEmbedVbCoreRuntime(True))

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim parentModel = compilation.GetSemanticModel(tree)

            Dim position1 = CompilationUtils.FindPositionFromText(tree, "Class C")
            Dim attr1 = ParseAttributeSyntax("<Obsolete>")

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = parentModel.TryGetSpeculativeSemanticModel(position1, attr1, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim symbolInfo = speculativeModel.GetSymbolInfo(attr1)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", symbolInfo.Symbol.ToTestDisplayString())

            Dim attr2 = ParseAttributeSyntax("<ObsoleteAttribute(4)>")

            success = parentModel.TryGetSpeculativeSemanticModel(position1, attr2, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            symbolInfo = speculativeModel.GetSymbolInfo(attr2)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.OverloadResolutionFailure)
            Assert.Equal(3, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor()", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", symbolInfo.CandidateSymbols(1).ToTestDisplayString())
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String, [error] As System.Boolean)", symbolInfo.CandidateSymbols(2).ToTestDisplayString())

            Dim argument = DirectCast(attr2.ArgumentList.Arguments(0), SimpleArgumentSyntax).Expression
            Dim constantInfo = speculativeModel.GetConstantValue(argument)
            Assert.True(constantInfo.HasValue, "must be constant")
            Assert.Equal(4, constantInfo.Value)

            Dim attr3 = ParseAttributeSyntax("<O(""hello"")>")

            success = parentModel.TryGetSpeculativeSemanticModel(position1, attr3, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            symbolInfo = speculativeModel.GetSymbolInfo(attr3)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", symbolInfo.Symbol.ToTestDisplayString())

            argument = DirectCast(attr3.ArgumentList.Arguments(0), SimpleArgumentSyntax).Expression
            constantInfo = speculativeModel.GetConstantValue(argument)
            Assert.True(constantInfo.HasValue, "must be constant")
            Assert.Equal("hello", constantInfo.Value)

            Dim aliasSymbol = speculativeModel.GetAliasInfo(DirectCast(attr3.Name, IdentifierNameSyntax))
            Assert.NotNull(aliasSymbol)
            Assert.Equal("O", aliasSymbol.Name)
            Assert.NotNull(aliasSymbol.Target)
            Assert.Equal("ObsoleteAttribute", aliasSymbol.Target.Name)

            Dim attr4 = ParseAttributeSyntax("<P>")

            success = parentModel.TryGetSpeculativeSemanticModel(position1, attr4, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            symbolInfo = speculativeModel.GetSymbolInfo(attr4)
            Assert.Null(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)

            Dim position2 = CompilationUtils.FindPositionFromText(tree, "Class E")
            Dim attr5 = ParseAttributeSyntax("<D>")

            success = parentModel.TryGetSpeculativeSemanticModel(position2, attr5, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            symbolInfo = speculativeModel.GetSymbolInfo(attr5)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub C.DAttribute..ctor()", symbolInfo.Symbol.ToTestDisplayString())

            Dim position3 = CompilationUtils.FindPositionFromText(tree, "Sub Goo")
            Dim attr6 = ParseAttributeSyntax("<O(""hello"")>")

            success = parentModel.TryGetSpeculativeSemanticModel(position2, attr6, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            symbolInfo = speculativeModel.GetSymbolInfo(attr6)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", symbolInfo.Symbol.ToTestDisplayString())

            argument = DirectCast(attr6.ArgumentList.Arguments(0), SimpleArgumentSyntax).Expression
            constantInfo = speculativeModel.GetConstantValue(argument)
            Assert.True(constantInfo.HasValue, "must be constant")
            Assert.Equal("hello", constantInfo.Value)

            aliasSymbol = speculativeModel.GetAliasInfo(DirectCast(attr6.Name, IdentifierNameSyntax))
            Assert.NotNull(aliasSymbol)
            Assert.Equal("O", aliasSymbol.Name)
            Assert.NotNull(aliasSymbol.Target)
            Assert.Equal("ObsoleteAttribute", aliasSymbol.Target.Name)

            Dim position4 = CompilationUtils.FindPositionFromText(tree, "Serializable")
            Dim attr7 = ParseAttributeSyntax("<D>")

            success = parentModel.TryGetSpeculativeSemanticModel(position4, attr7, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            symbolInfo = speculativeModel.GetSymbolInfo(attr7)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub C.DAttribute..ctor()", symbolInfo.Symbol.ToTestDisplayString())

            Dim attr8 = SyntaxFactory.ParseCompilationUnit("<Assembly: O(""hello"")>").Attributes(0).AttributeLists(0).Attributes(0)

            success = parentModel.TryGetSpeculativeSemanticModel(position4, attr8, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            symbolInfo = speculativeModel.GetSymbolInfo(attr8)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal(symbolInfo.CandidateReason, CandidateReason.None)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal("Sub System.ObsoleteAttribute..ctor(message As System.String)", symbolInfo.Symbol.ToTestDisplayString())

            argument = DirectCast(attr8.ArgumentList.Arguments(0), SimpleArgumentSyntax).Expression
            constantInfo = speculativeModel.GetConstantValue(argument)
            Assert.True(constantInfo.HasValue, "must be constant")
            Assert.Equal("hello", constantInfo.Value)

            aliasSymbol = speculativeModel.GetAliasInfo(DirectCast(attr8.Name, IdentifierNameSyntax))
            Assert.NotNull(aliasSymbol)
            Assert.Equal("O", aliasSymbol.Name)
            Assert.NotNull(aliasSymbol.Target)
            Assert.Equal("ObsoleteAttribute", aliasSymbol.Target.Name)
        End Sub

        <Fact()>
        Public Sub TestGetSymbolInfoOnSpeculativeModel()
            Dim compilation = CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class A
    Public Function Goo() As String
        Return "Goo"
    End Function
End Class

Module Program
    Public Sub Main(a As A)
        Dim x = a.Goo()
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position As Integer = CompilationUtils.FindPositionFromText(tree, "x = a.Goo()")
            Dim localDecl = tree.GetRoot().DescendantNodes().OfType(Of LocalDeclarationStatementSyntax)().Single()
            Dim parsedInvocation = SyntaxFactory.ParseExpression("a.Goo()")
            Dim newLocalDecl = DirectCast(localDecl.ReplaceNode(localDecl.Declarators(0).Initializer.Value, parsedInvocation), LocalDeclarationStatementSyntax)
            Dim newInitializer = DirectCast(newLocalDecl.Declarators(0).Initializer.Value, InvocationExpressionSyntax)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModel(position, newLocalDecl, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim memberAccess = DirectCast(newInitializer.Expression, MemberAccessExpressionSyntax)
            Dim symbolInfo = speculativeModel.GetSymbolInfo(memberAccess.Name)
            Assert.NotNull(symbolInfo.Symbol)
            Assert.Equal("Goo", symbolInfo.Symbol.Name)
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelForMethodBody()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class C
	Private Sub M(x As Integer)
		Dim y As Integer = 1000
	End Sub
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim methodBlock = DirectCast(typeBlock.Members(0), MethodBlockSyntax)
            Dim originalStatement = methodBlock.Statements(0)

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position1 = originalStatement.SpanStart
            Dim speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement(<![CDATA[
If y > 0 Then
  Dim z As Integer = 0
  M(z)
  M(y)      ' Should generate error here for undefined "y" as we are replacing the method body.
End If]]>.Value), ExecutableStatementSyntax)

            Dim ifStatement = DirectCast(speculatedStatement, MultiLineIfBlockSyntax)

            Dim speculatedMethod = methodBlock.WithStatements(ifStatement.Statements)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModelForMethodBody(position1, speculatedMethod, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            VerifySpeculativeSemanticModelForMethodBody(speculatedMethod, speculativeModel)
        End Sub

        Private Shared Sub VerifySpeculativeSemanticModelForMethodBody(speculatedMethod As MethodBlockBaseSyntax, speculativeModel As SemanticModel)
            Dim declStatement = DirectCast(speculatedMethod.Statements(0), LocalDeclarationStatementSyntax)
            Dim varDecl = declStatement.Declarators(0).Names(0)
            Dim local = speculativeModel.GetDeclaredSymbol(varDecl)
            Assert.NotNull(local)
            Assert.Equal("z", local.Name)
            Assert.Equal(SymbolKind.Local, local.Kind)
            Assert.Equal("System.Int32", DirectCast(local, LocalSymbol).Type.ToTestDisplayString())
            Dim typeSyntax = DirectCast(declStatement.Declarators(0).AsClause, SimpleAsClauseSyntax).Type
            Dim typeInfo = speculativeModel.GetTypeInfo(typeSyntax)
            Assert.NotNull(typeInfo.Type)
            Assert.Equal("System.Int32", typeInfo.Type.ToTestDisplayString())

            Dim call1 = DirectCast(speculatedMethod.Statements(1), ExpressionStatementSyntax)
            Dim arg = DirectCast(DirectCast(call1.Expression, InvocationExpressionSyntax).ArgumentList.Arguments(0), SimpleArgumentSyntax).Expression
            Dim argSymbolInfo = speculativeModel.GetSymbolInfo(arg)
            Assert.NotNull(argSymbolInfo.Symbol)
            Assert.Equal("z", argSymbolInfo.Symbol.Name)
            Assert.Equal(SymbolKind.Local, argSymbolInfo.Symbol.Kind)

            ' Shouldn't bind to local y in the original method as we are replacing the method body.
            Dim call2 = DirectCast(speculatedMethod.Statements(2), ExpressionStatementSyntax)
            arg = DirectCast(DirectCast(call2.Expression, InvocationExpressionSyntax).ArgumentList.Arguments(0), SimpleArgumentSyntax).Expression
            argSymbolInfo = speculativeModel.GetSymbolInfo(arg)
            Assert.Null(argSymbolInfo.Symbol)
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelForPropertyAccessorBody()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class C
    Private WriteOnly Property M(x As Integer) As Integer
        Set(value As Integer)
            Dim y As Integer = 1000
        End Set
    End Property
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim propertyBlock = DirectCast(typeBlock.Members(0), PropertyBlockSyntax)
            Dim methodBlock = propertyBlock.Accessors(0)
            Dim originalStatement = methodBlock.Statements(0)

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position1 = originalStatement.SpanStart
            Dim speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement(<![CDATA[
If y > 0 Then
  Dim z As Integer = 0
  M(z)
  M(y)      ' Should generate error here for undefined "y" as we are replacing the method body.
End If]]>.Value), ExecutableStatementSyntax)

            Dim ifStatement = DirectCast(speculatedStatement, MultiLineIfBlockSyntax)

            Dim speculatedMethod = methodBlock.WithStatements(ifStatement.Statements)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModelForMethodBody(position1, speculatedMethod, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            VerifySpeculativeSemanticModelForMethodBody(speculatedMethod, speculativeModel)
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelForEventAccessorBody()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class C
    Private Custom Event M As System.Action
        AddHandler(x As Action)
            Dim y As Integer = 1000
        End AddHandler
        RemoveHandler(value As Action)

        End RemoveHandler
        RaiseEvent()

        End RaiseEvent
    End Event
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim eventBlock = DirectCast(typeBlock.Members(0), EventBlockSyntax)
            Dim methodBlock = eventBlock.Accessors(0)
            Dim originalStatement = methodBlock.Statements(0)

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position1 = originalStatement.SpanStart
            Dim speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement(<![CDATA[
If y > 0 Then
  Dim z As Integer = 0
  M(z)
  M(y)      ' Should generate error here for undefined "y" as we are replacing the method body.
End If]]>.Value), ExecutableStatementSyntax)

            Dim ifStatement = DirectCast(speculatedStatement, MultiLineIfBlockSyntax)

            Dim speculatedMethod = methodBlock.WithStatements(ifStatement.Statements)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModelForMethodBody(position1, speculatedMethod, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            VerifySpeculativeSemanticModelForMethodBody(speculatedMethod, speculativeModel)
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelForMethodBody_DeclaredLocal()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class C
	Private Sub M(x As Integer)
		Dim y As Integer = 1000
	End Sub
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim methodBlock = DirectCast(typeBlock.Members(0), MethodBlockSyntax)
            Dim originalStatement = DirectCast(methodBlock.Statements(0), ExecutableStatementSyntax)

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position1 = originalStatement.SpanStart

            ' different name local
            Dim speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement("Dim z As Integer = 0"), ExecutableStatementSyntax)
            Dim speculatedMethod = methodBlock.WithStatements(SyntaxFactory.SingletonList(Of StatementSyntax)(speculatedStatement))

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModelForMethodBody(position1, speculatedMethod, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim declStatement = DirectCast(speculatedMethod.Statements(0), LocalDeclarationStatementSyntax)
            Dim varDecl = declStatement.Declarators(0).Names(0)
            Dim local = speculativeModel.GetDeclaredSymbol(varDecl)
            Assert.NotNull(local)
            Assert.Equal("z", local.Name)
            Assert.Equal(SymbolKind.Local, local.Kind)
            Assert.Equal("System.Int32", DirectCast(local, LocalSymbol).Type.ToTestDisplayString())

            ' same name local
            speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement("Dim y As String = Nothing"), ExecutableStatementSyntax)
            speculatedMethod = methodBlock.WithStatements(SyntaxFactory.SingletonList(Of StatementSyntax)(speculatedStatement))

            success = semanticModel.TryGetSpeculativeSemanticModelForMethodBody(position1, speculatedMethod, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            declStatement = DirectCast(speculatedMethod.Statements(0), LocalDeclarationStatementSyntax)
            varDecl = declStatement.Declarators(0).Names(0)
            local = speculativeModel.GetDeclaredSymbol(varDecl)
            Assert.NotNull(local)
            Assert.Equal("y", local.Name)
            Assert.Equal(SymbolKind.Local, local.Kind)
            Assert.Equal("System.String", DirectCast(local, LocalSymbol).Type.ToTestDisplayString())

            ' parameter symbol
            speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement("Dim y = x"), ExecutableStatementSyntax)
            speculatedMethod = methodBlock.WithStatements(SyntaxFactory.SingletonList(Of StatementSyntax)(speculatedStatement))

            success = semanticModel.TryGetSpeculativeSemanticModelForMethodBody(position1, speculatedMethod, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            declStatement = DirectCast(speculatedMethod.Statements(0), LocalDeclarationStatementSyntax)
            varDecl = declStatement.Declarators(0).Names(0)
            local = speculativeModel.GetDeclaredSymbol(varDecl)
            Assert.NotNull(local)
            Assert.Equal("y", local.Name)
            Assert.Equal(SymbolKind.Local, local.Kind)
            Assert.Equal("System.Int32", DirectCast(local, LocalSymbol).Type.ToTestDisplayString())

            Dim param = speculativeModel.GetSymbolInfo(declStatement.Declarators(0).Initializer.Value).Symbol
            Assert.NotNull(param)
            Assert.Equal(SymbolKind.Parameter, param.Kind)
            Dim paramSymbol = DirectCast(param, ParameterSymbol)
            Assert.Equal("x", paramSymbol.Name)
            Assert.Equal("System.Int32", paramSymbol.Type.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelForMethodBody_DeclaredLabel()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class C
	Private Sub M(x As Integer)
		Dim y As Integer = 1000
	End Sub
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim methodBlock = DirectCast(typeBlock.Members(0), MethodBlockSyntax)
            Dim originalStatement = methodBlock.Statements(0)

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position1 = originalStatement.Span.End

            Dim speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement("Label: y = y + 1"), ExecutableStatementSyntax)
            Dim speculatedMethod = methodBlock.WithStatements(SyntaxFactory.SingletonList(Of StatementSyntax)(speculatedStatement))

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModelForMethodBody(position1, speculatedMethod, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim label = speculativeModel.GetDeclaredSymbol(speculatedMethod.Statements(0))
            Assert.NotNull(label)
            Assert.Equal("Label", label.Name)
            Assert.Equal(SymbolKind.Label, label.Kind)
        End Sub

        <Fact()>
        Public Sub TestGetSpeculativeSemanticModelForMethodBody_GetDeclaredLambdaParameterSymbol()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class C
	Private Sub M(x As Integer)
		Dim y As Integer = 0
	End Sub
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim methodBlock = DirectCast(typeBlock.Members(0), MethodBlockSyntax)
            Dim originalStatement = methodBlock.Statements(0)

            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim position1 = originalStatement.Span.End

            Dim speculatedStatement = DirectCast(SyntaxFactory.ParseExecutableStatement("Dim var As Func(Of Integer, Integer) = Function(z) x + z"), ExecutableStatementSyntax)
            Dim speculatedMethod = methodBlock.WithStatements(SyntaxFactory.SingletonList(Of StatementSyntax)(speculatedStatement))

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = semanticModel.TryGetSpeculativeSemanticModelForMethodBody(position1, speculatedMethod, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim lambdaExpressionHeader = speculatedMethod.Statements(0).DescendantNodes().OfType(Of LambdaHeaderSyntax)().FirstOrDefault()
            Dim lambdaParam = lambdaExpressionHeader.ParameterList.Parameters(0)
            Dim parameterSymbol = speculativeModel.GetDeclaredSymbol(lambdaParam)
            Assert.NotNull(parameterSymbol)
            Assert.Equal("z", parameterSymbol.Name)
        End Sub

        Private Shared Sub TestGetSpeculativeSemanticModelForTypeSyntax_Common(model As SemanticModel, position As Integer, speculatedTypeSyntax As TypeSyntax, bindingOption As SpeculativeBindingOption, expectedSymbolKind As SymbolKind, expectedTypeDisplayString As String)
            Assert.False(model.IsSpeculativeSemanticModel)
            Assert.Null(model.ParentModel)
            Assert.Equal(0, model.OriginalPositionForSpeculation)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = model.TryGetSpeculativeSemanticModel(position, speculatedTypeSyntax, speculativeModel, bindingOption)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Assert.True(speculativeModel.IsSpeculativeSemanticModel)
            Assert.Equal(model, speculativeModel.ParentModel)
            Assert.Equal(position, speculativeModel.OriginalPositionForSpeculation)

            Dim symbol = speculativeModel.GetSymbolInfo(speculatedTypeSyntax).Symbol
            Assert.NotNull(symbol)
            Assert.Equal(expectedSymbolKind, symbol.Kind)
            Assert.Equal(expectedTypeDisplayString, symbol.ToDisplayString())

            Dim typeSymbol = speculativeModel.GetTypeInfo(speculatedTypeSyntax).Type
            Assert.NotNull(symbol)
            Assert.Equal(expectedSymbolKind, symbol.Kind)
            Assert.Equal(expectedTypeDisplayString, symbol.ToDisplayString())

            Dim methodGroupInfo = speculativeModel.GetMemberGroup(speculatedTypeSyntax)
            Dim constantInfo = speculativeModel.GetConstantValue(speculatedTypeSyntax)

            If speculatedTypeSyntax.Kind = SyntaxKind.QualifiedName Then
                Dim right = DirectCast(speculatedTypeSyntax, QualifiedNameSyntax).Right
                symbol = speculativeModel.GetSymbolInfo(right).Symbol
                Assert.NotNull(symbol)
                Assert.Equal(expectedSymbolKind, symbol.Kind)
                Assert.Equal(expectedTypeDisplayString, symbol.ToDisplayString())

                typeSymbol = speculativeModel.GetTypeInfo(right).Type
                Assert.NotNull(symbol)
                Assert.Equal(expectedSymbolKind, symbol.Kind)
                Assert.Equal(expectedTypeDisplayString, symbol.ToDisplayString())
            End If
        End Sub

        <Fact>
        Public Sub TestGetSpeculativeSemanticModelForTypeSyntax_InGlobalImports()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Imports System.Runtime
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim importsStatement = root.Imports(0)
            Dim importsClause = DirectCast(importsStatement.ImportsClauses(0), SimpleImportsClauseSyntax)
            Dim model = compilation.GetSemanticModel(tree)

            Dim speculatedTypeExpression = SyntaxFactory.ParseName("System.Collections")
            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, importsClause.Name.Position,
                speculatedTypeExpression, SpeculativeBindingOption.BindAsTypeOrNamespace, SymbolKind.Namespace, "System.Collections")
        End Sub

        <Fact>
        Public Sub TestGetSpeculativeSemanticModelForTypeSyntax_InGlobalAlias()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Imports A = System.Exception
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim importsStatement = root.Imports(0)
            Dim importsClause = DirectCast(importsStatement.ImportsClauses(0), SimpleImportsClauseSyntax)
            Dim model = compilation.GetSemanticModel(tree)

            Dim speculatedTypeExpression = SyntaxFactory.ParseName("System.ArgumentException")
            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, importsClause.Name.Position,
                speculatedTypeExpression, SpeculativeBindingOption.BindAsExpression, SymbolKind.NamedType, "System.ArgumentException")
        End Sub

        <Fact>
        Public Sub TestGetSpeculativeSemanticModelForTypeSyntax_InBaseList()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Imports N

Class MyException
    Inherits System.Exception
    Implements N.I

End Class

Namespace N
    Public Interface I
    End Interface
End Namespace
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim model = compilation.GetSemanticModel(tree)

            Dim speculatedTypeExpression = SyntaxFactory.ParseName("System.ArgumentException")
            Dim inheritsClause = typeBlock.Inherits(0)
            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, inheritsClause.Types.First.Position,
                speculatedTypeExpression, SpeculativeBindingOption.BindAsExpression, SymbolKind.NamedType, "System.ArgumentException")

            speculatedTypeExpression = SyntaxFactory.ParseName("I")
            Dim implementsClause = typeBlock.Implements(0)
            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, implementsClause.Types.First.Position,
                speculatedTypeExpression, SpeculativeBindingOption.BindAsExpression, SymbolKind.NamedType, "N.I")
        End Sub

        <Fact>
        Public Sub TestGetSpeculativeSemanticModelForTypeSyntax_InMemberDeclaration()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class Program
    Implements I

    Private field As System.Exception = Nothing
    Public Function Method(param As System.Exception) As System.Exception
        Return field
    End Function
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim field = DirectCast(typeBlock.Members(0), FieldDeclarationSyntax)
            Dim methodBlock = DirectCast(typeBlock.Members(1), MethodBlockBaseSyntax)
            Dim methodDecl = DirectCast(methodBlock.BlockStatement, MethodStatementSyntax)
            Dim model = compilation.GetSemanticModel(tree)

            Dim speculatedTypeExpression = SyntaxFactory.ParseName("System.ArgumentException")
            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, field.Declarators.First.AsClause.Type.Position,
                speculatedTypeExpression, SpeculativeBindingOption.BindAsExpression, SymbolKind.NamedType, "System.ArgumentException")

            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, methodDecl.AsClause.Type.Position,
                speculatedTypeExpression, SpeculativeBindingOption.BindAsExpression, SymbolKind.NamedType, "System.ArgumentException")

            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, methodDecl.ParameterList.Parameters.First.AsClause.Type.Position,
                speculatedTypeExpression, SpeculativeBindingOption.BindAsExpression, SymbolKind.NamedType, "System.ArgumentException")
        End Sub

        <Fact>
        <WorkItem(120491, "https://devdiv.visualstudio.com/defaultcollection/DevDiv/_workitems#_a=edit&id=120491")>
        <WorkItem(745766, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/745766")>
        Public Sub TestGetSpeculativeSemanticModelForTypeSyntax_InImplementsClauseForMember()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Class Program
    Implements I

    Public Function Method(param As System.Exception) As System.Exception Implements I.Method
        Return field
    End Function
End Class

Interface I
    Function Method(param As System.Exception) As System.Exception

    Function Method2() As System.Exception
    Function Method2(param As System.Exception) As System.Exception
End Interface
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim methodBlock = DirectCast(typeBlock.Members(0), MethodBlockBaseSyntax)
            Dim methodDecl = DirectCast(methodBlock.BlockStatement, MethodStatementSyntax)
            Dim model = compilation.GetSemanticModel(tree)

            Dim implementsClause = methodDecl.ImplementsClause
            Dim implementsName = implementsClause.InterfaceMembers(0)

            Dim symbol = model.GetSymbolInfo(implementsName).Symbol
            Assert.Equal("I.Method", implementsName.ToString())
            Assert.NotNull(symbol)
            Assert.Equal("Function Method(param As System.Exception) As System.Exception", symbol.ToDisplayString())

            Dim speculatedMemberName = SyntaxFactory.ParseName("I.Method2")
            Const bug120491IsFixed = False

            If bug120491IsFixed Then
                TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, implementsName.Position,
                speculatedMemberName, SpeculativeBindingOption.BindAsExpression, SymbolKind.Method, "I.Method2")
            Else
                Dim speculativeModel As SemanticModel = Nothing
                Dim success = model.TryGetSpeculativeSemanticModel(implementsName.Position, speculatedMemberName, speculativeModel, SpeculativeBindingOption.BindAsExpression)
                Assert.True(success)
                symbol = speculativeModel.GetSymbolInfo(speculatedMemberName).Symbol
                Assert.Null(symbol)
            End If
        End Sub

        <Fact>
        Public Sub TestGetSpeculativeSemanticModelForTypeSyntax_AliasName()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Imports A = System.ArgumentException

Class Program
    Private field As System.Exception = Nothing
End Class
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeBlock = DirectCast(root.Members(0), TypeBlockSyntax)
            Dim field = DirectCast(typeBlock.Members(0), FieldDeclarationSyntax)
            Dim position = field.Declarators.First.AsClause.Type.Position
            Dim model = compilation.GetSemanticModel(tree)

            Dim speculatedAliasName = DirectCast(SyntaxFactory.ParseName("A"), IdentifierNameSyntax)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = model.TryGetSpeculativeSemanticModel(position, speculatedAliasName, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim symbol = DirectCast(speculativeModel.GetAliasInfo(speculatedAliasName), AliasSymbol)
            Assert.NotNull(symbol)
            Assert.Equal("A", symbol.ToDisplayString())
            Assert.Equal("System.ArgumentException", symbol.Target.ToDisplayString())
        End Sub

        <Fact, WorkItem(849360, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849360")>
        Public Sub TestGetSpeculativeSemanticModelForLocalDeclaration_Incomplete_1()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Module M
    Sub T()
    Namespace A
        Class B
            Function S()
                Dim c = Me.goo
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim moduleBlock = DirectCast(root.Members(0), ModuleBlockSyntax)
            Dim namespaceBlock = DirectCast(root.Members(1), NamespaceBlockSyntax)
            Dim typeBlockSyntax = DirectCast(namespaceBlock.Members(0), TypeBlockSyntax)
            Dim methodBlockSyntax = DirectCast(typeBlockSyntax.Members(0), MethodBlockSyntax)
            Dim statementSyntax = DirectCast(methodBlockSyntax.Statements(0), LocalDeclarationStatementSyntax)
            Dim initializer = statementSyntax.DescendantNodes().Single(Function(n) n.ToString() = "Me.goo")
            Dim position = statementSyntax.SpanStart
            Dim model = compilation.GetSemanticModel(tree)

            Dim speculatedExpression = DirectCast(SyntaxFactory.ParseExpression("goo"), ExpressionSyntax)
            Dim speculatedStatement = statementSyntax.ReplaceNode(initializer, speculatedExpression)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = model.TryGetSpeculativeSemanticModel(position, speculatedStatement, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)
        End Sub

        <Fact, WorkItem(849360, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849360")>
        Public Sub TestGetSpeculativeSemanticModelForLocalDeclaration_Incomplete_2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="BindAsExpressionVsBindAsType">
    <file name="a.vb">
Module M
    Class D
        Sub T()
        Namespace A
            Class B
                Function S()
                    Dim c = Me.goo
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot()
            Dim moduleBlock = DirectCast(root.Members(0), ModuleBlockSyntax)
            Dim namespaceBlock = DirectCast(root.Members(1), NamespaceBlockSyntax)
            Dim typeBlockSyntax = DirectCast(namespaceBlock.Members(0), TypeBlockSyntax)
            Dim methodBlockSyntax = DirectCast(typeBlockSyntax.Members(0), MethodBlockSyntax)
            Dim statementSyntax = DirectCast(methodBlockSyntax.Statements(0), LocalDeclarationStatementSyntax)
            Dim initializer = statementSyntax.DescendantNodes().Single(Function(n) n.ToString() = "Me.goo")
            Dim position = statementSyntax.SpanStart
            Dim model = compilation.GetSemanticModel(tree)

            Dim speculatedExpression = DirectCast(SyntaxFactory.ParseExpression("goo"), ExpressionSyntax)
            Dim speculatedStatement = statementSyntax.ReplaceNode(initializer, speculatedExpression)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = model.TryGetSpeculativeSemanticModel(position, speculatedStatement, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)
        End Sub
#End Region

#Region "ClassifyConversion"

        ' Check invariants of Conversion.
        Private Sub ValidateConversionInvariants(conv As Conversion)
            ' Exactly 1 of NOT IsConvertible, IsWidening, IsNarrowing must be true
            Assert.Equal(-1, CInt(Not conv.Exists) + CInt(conv.IsWidening) + CInt(conv.IsNarrowing))

            If conv.Exists Then
                ' Exactly 1 of conversion classifications: must be true
                Assert.Equal(-1, CInt(conv.IsIdentity) + CInt(conv.IsDefault) + CInt(conv.IsNumeric) +
                                 CInt(conv.IsBoolean) + CInt(conv.IsReference) + CInt(conv.IsAnonymousDelegate) +
                                 CInt(conv.IsArray) + CInt(conv.IsValueType) + CInt(conv.IsNullableValueType) +
                                 CInt(conv.IsString) + CInt(conv.IsTypeParameter) + CInt(conv.IsUserDefined))
            End If

            ' Method set only if user defined set.
            Assert.True((conv.IsUserDefined And conv.Method IsNot Nothing) Or
                        (Not conv.IsUserDefined And conv.Method Is Nothing),
                        "UserDefinedConversionMethod should be set if and only if IsUserDefined is true.")
        End Sub

        ' Unit tests for ClassifyConversion on Compilation.
        ' We already have very extensive tests on the internal ClassifyConversion API; this just exercises the 
        ' public API to make sure we're mapping correctly to the external interface.
        <Fact()>
        Public Sub ClassifyConversion()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="ClassifyConversion">
    <file name="a.vb">
    Imports System
    Imports System.Collections.Generic

    Enum EEE
       Red
    End Enum

    Class AAA(Of T)
        Implements ICloneable

        Public Function Clone() As Object Implements ICloneable.Clone
            Return Me
        End Function

        Public field_li1 As List(Of Integer)
        Public field_li2 As List(Of Integer)
        public field_enum as EEE
        public field_aaa_array as AAA(Of T)()
        public field_obj_array as Object()
        Public field_null_int as Integer?
        public field_tp As T
    End Class


    </file>
</compilation>)

            Dim conv As Conversion

            Dim globalNS = compilation.GlobalNamespace
            Dim int64 = compilation.GetTypeByMetadataName("System.Int64")
            Dim int32 = compilation.GetTypeByMetadataName("System.Int32")
            Dim int16 = compilation.GetTypeByMetadataName("System.Int16")
            Dim str = compilation.GetTypeByMetadataName("System.String")
            Dim bool = compilation.GetTypeByMetadataName("System.Boolean")
            Dim objType = compilation.GetTypeByMetadataName("System.Object")
            Dim cloneableType = compilation.GetTypeByMetadataName("System.ICloneable")
            Dim classAAA = globalNS.GetTypeMembers("AAA").Single()

            Dim listOfInt32_1 As TypeSymbol = DirectCast(classAAA.GetMembers("field_li1").Single(), FieldSymbol).Type
            Dim listOfInt32_2 As TypeSymbol = DirectCast(classAAA.GetMembers("field_li2").Single(), FieldSymbol).Type
            Dim enumType As TypeSymbol = DirectCast(classAAA.GetMembers("field_enum").Single(), FieldSymbol).Type
            Dim aaaArray As TypeSymbol = DirectCast(classAAA.GetMembers("field_aaa_array").Single(), FieldSymbol).Type
            Dim objArray As TypeSymbol = DirectCast(classAAA.GetMembers("field_obj_array").Single(), FieldSymbol).Type
            Dim nullInt32 As TypeSymbol = DirectCast(classAAA.GetMembers("field_null_int").Single(), FieldSymbol).Type
            Dim typeParam As TypeSymbol = DirectCast(classAAA.GetMembers("field_tp").Single(), FieldSymbol).Type

            ' Convert List(Of Integer) -> List(Of Integer) : Should be widening identity conversion
            conv = compilation.ClassifyConversion(listOfInt32_1, listOfInt32_2)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.True(conv.IsWidening)
            Assert.False(conv.IsNarrowing)
            Assert.True(conv.IsIdentity)
            Assert.True(compilation.HasImplicitConversion(listOfInt32_1, listOfInt32_2))

            ' Convert List(Of Integer) -> Integer : Should be no conversion
            conv = compilation.ClassifyConversion(listOfInt32_1, int32)
            ValidateConversionInvariants(conv)
            Assert.False(conv.Exists)
            Assert.False(conv.IsWidening)
            Assert.False(conv.IsNarrowing)
            Assert.False(compilation.HasImplicitConversion(listOfInt32_1, int32))

            ' Convert String -> Integer: Should be narrowing string conversion
            conv = compilation.ClassifyConversion(str, int32)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.False(conv.IsWidening)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsString)
            Assert.Equal("NarrowingString", conv.ToString())
            Assert.False(compilation.HasImplicitConversion(str, int32))

            ' Convert Enum -> Integer: Should be  widening numeric conversion
            conv = compilation.ClassifyConversion(enumType, int32)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.True(conv.IsWidening)
            Assert.False(conv.IsNarrowing)
            Assert.True(conv.IsNumeric)
            Assert.Equal("WideningNumeric, InvolvesEnumTypeConversions", conv.ToString())
            Assert.True(compilation.HasImplicitConversion(enumType, int32))

            ' Convert Enum -> String: Should be  narrowing string conversion
            conv = compilation.ClassifyConversion(enumType, str)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.False(conv.IsWidening)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsString)
            Assert.False(compilation.HasImplicitConversion(enumType, str))

            ' Convert Long -> Integer: Should be narrowing numeric conversion
            conv = compilation.ClassifyConversion(int64, int32)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.False(conv.IsWidening)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsNumeric)
            Assert.False(compilation.HasImplicitConversion(int64, int32))

            ' Convert Boolean -> Enum: Should be narrowing boolean conversion
            conv = compilation.ClassifyConversion(bool, enumType)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.False(conv.IsWidening)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsBoolean)
            Assert.Equal("NarrowingBoolean, InvolvesEnumTypeConversions", conv.ToString())
            Assert.False(compilation.HasImplicitConversion(bool, enumType))

            ' Convert List(Of Integer) -> Object: Should be widening reference conversion
            conv = compilation.ClassifyConversion(listOfInt32_1, objType)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.True(conv.IsWidening)
            Assert.False(conv.IsNarrowing)
            Assert.True(conv.IsReference)
            Assert.Equal("WideningReference", conv.ToString())
            Assert.True(compilation.HasImplicitConversion(listOfInt32_1, objType))

            ' Convert Object -> List(Of Integer): Should be narrow reference conversion
            conv = compilation.ClassifyConversion(objType, listOfInt32_1)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.False(conv.IsWidening)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsReference)
            Assert.False(compilation.HasImplicitConversion(objType, listOfInt32_1))

            ' Convert AAA -> System.ICloneable: SHould be widening reference conversion
            conv = compilation.ClassifyConversion(classAAA, cloneableType)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.True(conv.IsWidening)
            Assert.False(conv.IsNarrowing)
            Assert.True(conv.IsReference)
            Assert.True(compilation.HasImplicitConversion(classAAA, cloneableType))

            ' Convert AAA() -> Object(): SHould be widening array conversion
            conv = compilation.ClassifyConversion(aaaArray, objArray)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.True(conv.IsWidening)
            Assert.False(conv.IsNarrowing)
            Assert.True(conv.IsArray)
            Assert.True(compilation.HasImplicitConversion(aaaArray, objArray))

            ' Convert Object() -> AAA(): SHould be narrowing array conversion
            conv = compilation.ClassifyConversion(objArray, aaaArray)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.False(conv.IsWidening)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsArray)
            Assert.Equal("NarrowingArray", conv.ToString())
            Assert.False(compilation.HasImplicitConversion(objArray, aaaArray))

            ' Convert Short -> Integer?: Should be widening nullable value type conversion
            conv = compilation.ClassifyConversion(int16, nullInt32)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.True(conv.IsWidening)
            Assert.False(conv.IsNarrowing)
            Assert.True(conv.IsNullableValueType)
            Assert.Equal("WideningNullable", conv.ToString())
            Assert.True(compilation.HasImplicitConversion(int16, nullInt32))

            ' Convert Integer? -> Integer: Should be narrowing nullable value type conversion
            conv = compilation.ClassifyConversion(nullInt32, int32)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.False(conv.IsWidening)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsNullableValueType)
            Assert.False(compilation.HasImplicitConversion(nullInt32, int32))

            ' Convert T -> Object: Widening type parameter conversion
            conv = compilation.ClassifyConversion(typeParam, objType)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.True(conv.IsWidening)
            Assert.False(conv.IsNarrowing)
            Assert.True(conv.IsTypeParameter)
            Assert.True(compilation.HasImplicitConversion(typeParam, objType))

            ' Convert Object -> T : Narrowing type parameter conversion
            conv = compilation.ClassifyConversion(objType, typeParam)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.False(conv.IsWidening)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsTypeParameter)
            Assert.Equal("NarrowingTypeParameter", conv.ToString())
            Assert.False(compilation.HasImplicitConversion(objType, typeParam))

            ' Check equality, hash code.
            Dim conv2 = compilation.ClassifyConversion(objType, typeParam)
            Dim conv3 = compilation.ClassifyConversion(typeParam, objType)

            Assert.True(conv = conv2, "Check equality implementation")
            Assert.False(conv <> conv2, "Check equality implementation")
            Assert.True(conv.Equals(conv2), "Check equality implementation")
            Assert.True(conv.Equals(DirectCast(conv2, Object)), "Check equality implementation")
            Assert.True(conv.GetHashCode() = conv2.GetHashCode(), "Check equality implementation")

            Assert.False(conv3 = conv2, "Check equality implementation")
            Assert.True(conv3 <> conv2, "Check equality implementation")
            Assert.False(conv3.Equals(conv2), "Check equality implementation")
            Assert.False(conv3.Equals(DirectCast(conv2, Object)), "Check equality implementation")

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        ' Unit tests for ClassifyConversion on SemanticModel.
        ' We already have very extensive tests on the internal ClassifyConversion API; this just exercises the 
        ' public API to make sure we're mapping correctly to the external interface.
        <Fact()>
        Public Sub ClassifyConversionSemanticModel()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="ClassifyConversionSemanticModel">
    <file name="a.vb">
    Imports System
    Imports System.Collections.Generic

    Enum EEE
       Red
    End Enum

    Class AAA
        Public Sub Goo() 
            anInt = 0
            anInt = 14
            anObj = Nothing
        End Sub

        Private anInt As Integer
        Private anObj As Object
    End Class


    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim nodeZero As ExpressionSyntax = DirectCast(CompilationUtils.FindNodeFromText(tree, "0"), ExpressionSyntax)
            Dim nodeFourteen As ExpressionSyntax = DirectCast(CompilationUtils.FindNodeFromText(tree, "14"), ExpressionSyntax)
            Dim nodeNothing As ExpressionSyntax = DirectCast(CompilationUtils.FindNodeFromText(tree, "Nothing"), ExpressionSyntax)
            Dim nodeIntField As ExpressionSyntax = DirectCast(CompilationUtils.FindNodeFromText(tree, "anInt"), ExpressionSyntax)
            Dim nodeObjField As ExpressionSyntax = DirectCast(CompilationUtils.FindNodeFromText(tree, "anObj"), ExpressionSyntax)
            Dim conv As Conversion

            Dim globalNS = compilation.GlobalNamespace
            Dim int16 = compilation.GetTypeByMetadataName("System.Int16")
            Dim str = compilation.GetTypeByMetadataName("System.String")
            Dim bool = compilation.GetTypeByMetadataName("System.Boolean")
            Dim objType = compilation.GetTypeByMetadataName("System.Object")
            Dim classAAA = globalNS.GetTypeMembers("AAA").Single()
            Dim enumEEE = globalNS.GetTypeMembers("EEE").Single()

            ' Convert 0 -> Int16 : Should be widening numeric conversion
            conv = semanticModel.ClassifyConversion(nodeZero, int16)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.True(conv.IsWidening)
            Assert.False(conv.IsNarrowing)
            Assert.True(conv.IsNumeric)

            ' Convert 14 -> Int16 : Should be widening numeric conversion
            conv = semanticModel.ClassifyConversion(nodeFourteen, int16)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.True(conv.IsWidening)
            Assert.False(conv.IsNarrowing)
            Assert.True(conv.IsNumeric)

            ' Convert int field -> Int16 : Should be narrowing numeric conversion
            conv = semanticModel.ClassifyConversion(nodeIntField, int16)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.False(conv.IsWidening)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsNumeric)

            ' Converts 0 -> enum: Should be widening numeric
            conv = semanticModel.ClassifyConversion(nodeZero, enumEEE)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.True(conv.IsWidening)
            Assert.False(conv.IsNarrowing)
            Assert.True(conv.IsNumeric)

            ' Converts 14 -> enum: Should be narrowing numeric
            conv = semanticModel.ClassifyConversion(nodeFourteen, enumEEE)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.False(conv.IsWidening)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsNumeric)

            ' Converts int field -> enum: Should be narrowing numeric
            conv = semanticModel.ClassifyConversion(nodeIntField, enumEEE)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.False(conv.IsWidening)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsNumeric)

            ' Convert Nothing to enum: should be widening default
            conv = semanticModel.ClassifyConversion(nodeNothing, enumEEE)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.True(conv.IsWidening)
            Assert.False(conv.IsNarrowing)
            Assert.True(conv.IsDefault)

            ' Convert Object field to enum: should be narrowing value type
            conv = semanticModel.ClassifyConversion(nodeObjField, enumEEE)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.False(conv.IsWidening)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsValueType)

            ' Convert Nothing to string: should be widening default
            conv = semanticModel.ClassifyConversion(nodeNothing, str)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.True(conv.IsWidening)
            Assert.False(conv.IsNarrowing)
            Assert.True(conv.IsDefault)

            ' Convert object field to string: should be narrowing reference
            conv = semanticModel.ClassifyConversion(nodeObjField, str)
            ValidateConversionInvariants(conv)
            Assert.True(conv.Exists)
            Assert.False(conv.IsWidening)
            Assert.True(conv.IsNarrowing)
            Assert.True(conv.IsReference)

            CompilationUtils.AssertNoErrors(compilation)

        End Sub

        <WorkItem(527766, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527766")>
        <Fact()>
        Public Sub ClassifyConversionSemanticModel2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ClassifyConversionSemanticModel2">
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic

Module Program

    Sub Main()
        Dim en As E = 0 'widening numeric
        Dim enullable As E? = 0 'narrowing nullable

        Dim chary(2) As Char
        chary(0) = "0"
        chary(1) = "."
        chary(2) = "2"
        Dim str As String = chary ' widening string
        Dim chary2 As Char() = str ' narrowing string

        Dim float As Single = str ' string->num: narrowing string

        Dim bb As Boolean = True
        str = bb ' narrowing string
    End Sub
End Module

Enum E
  Zero
  One
End Enum
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)


            Dim cunit = tree.GetCompilationUnitRoot()
            Dim v1 = TryCast(cunit.Members(0), TypeBlockSyntax)
            Dim v2 = TryCast(v1.Members(0), MethodBlockSyntax)

            ' Dim en As E = 0 'widening numeric
            Dim v3 = DirectCast(v2.ChildNodesAndTokens()(1).AsNode(), LocalDeclarationStatementSyntax)
            Assert.NotNull(v3)
            Dim expr1 As ExpressionSyntax = v3.Declarators(0).Initializer.Value
            Assert.Equal("0", expr1.ToString())
            Dim infoP = semanticModel.GetSemanticInfoSummary(expr1)
            Dim enumE = compilation.GlobalNamespace.GetTypeMembers("E").Single()
            Assert.Equal(enumE, infoP.ConvertedType)

            Dim conv1 = semanticModel.ClassifyConversion(expr1, infoP.ConvertedType)
            ValidateConversionInvariants(conv1)
            Assert.True(conv1.IsWidening)
            Assert.False(conv1.IsNarrowing)
            Assert.True(conv1.IsNumeric)

            'Dim enullable As E? = 0 'narrowing nullable
            v3 = DirectCast(v2.ChildNodesAndTokens()(2).AsNode(), LocalDeclarationStatementSyntax)
            Assert.NotNull(v3)
            expr1 = v3.Declarators(0).Initializer.Value
            infoP = semanticModel.GetSemanticInfoSummary(expr1)
            Assert.NotNull(infoP.ConvertedType)
            Assert.Equal("E?", infoP.ConvertedType.ToString())
            conv1 = semanticModel.ClassifyConversion(expr1, infoP.ConvertedType)
            ValidateConversionInvariants(conv1)
            ' Bug#5034 (exp Widening + Nullable) & C#: ImplicitEnum
            ' Conversion for C# and VB are very different by nature
            Assert.False(conv1.IsWidening)
            Assert.True(conv1.IsNarrowing)
            Assert.False(conv1.IsNumeric)
            Assert.True(conv1.IsNullableValueType)

            '  Dim str As String = chary
            Dim v4 = DirectCast(v2.ChildNodesAndTokens()(7).AsNode(), LocalDeclarationStatementSyntax)
            Assert.NotNull(v4)
            Dim expr2 As ExpressionSyntax = v4.Declarators(0).Initializer.Value
            Assert.Equal("chary", expr2.ToString())
            infoP = semanticModel.GetSemanticInfoSummary(expr2)
            Assert.NotNull(infoP.ConvertedType)
            Dim conv2 = semanticModel.ClassifyConversion(expr2, infoP.ConvertedType)
            ValidateConversionInvariants(conv2)
            Assert.True(conv2.IsWidening)
            Assert.False(conv2.IsNarrowing)
            Assert.True(conv2.IsString)

            '  Dim chary2 As Char() = str
            v4 = DirectCast(v2.ChildNodesAndTokens()(8).AsNode(), LocalDeclarationStatementSyntax)
            Assert.NotNull(v4)
            expr2 = v4.Declarators(0).Initializer.Value
            Assert.Equal("str", expr2.ToString())
            infoP = semanticModel.GetSemanticInfoSummary(expr2)
            Assert.NotNull(infoP.ConvertedType)
            conv2 = semanticModel.ClassifyConversion(expr2, infoP.ConvertedType)
            ValidateConversionInvariants(conv2)
            Assert.False(conv2.IsWidening)
            Assert.True(conv2.IsNarrowing)
            Assert.True(conv2.IsString)

            '  Dim float As Single = str
            v4 = DirectCast(v2.ChildNodesAndTokens()(9).AsNode(), LocalDeclarationStatementSyntax)
            Assert.NotNull(v4)
            expr2 = v4.Declarators(0).Initializer.Value
            infoP = semanticModel.GetSemanticInfoSummary(expr2)
            Assert.NotNull(infoP.ConvertedType)
            conv2 = semanticModel.ClassifyConversion(expr2, infoP.ConvertedType)
            ValidateConversionInvariants(conv2)
            Assert.False(conv2.IsWidening)
            Assert.True(conv2.IsNarrowing)
            Assert.True(conv2.IsString)

            ' str = bb ' narrowing string
            Dim strSym = compilation.GetTypeByMetadataName("System.String")
            Dim v5 = DirectCast(v2.ChildNodesAndTokens()(11).AsNode(), AssignmentStatementSyntax)
            Assert.NotNull(v5)
            expr2 = v5.Right
            conv2 = semanticModel.ClassifyConversion(expr2, strSym)
            ValidateConversionInvariants(conv2)
            Assert.False(conv2.IsWidening)
            Assert.True(conv2.IsNarrowing)
            Assert.True(conv2.IsString)

            CompilationUtils.AssertNoErrors(compilation)

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub ClassifyConversionSemanticModel2WithStaticLocals()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ClassifyConversionSemanticModel2">
    <file name="a.vb">
Imports System
Imports Microsoft.VisualBasic

Module Module1

    Sub Main()
        Static en As E = 0 'widening numeric
        Static enullable As E? = 0 'narrowing nullable

        Static chary(2) As Char
        chary(0) = "0"
        chary(1) = "."
        chary(2) = "2"
        Static str As String = chary ' widening string
        Static chary2 As Char() = str ' narrowing string

        Static float As Single = str ' string->num: narrowing string

        Static bb As Boolean = True
        str = bb ' narrowing string
    End Sub
End Module

Enum E
    Zero
    One
End Enum
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)


            Dim cunit = tree.GetCompilationUnitRoot()
            Dim v1 = TryCast(cunit.Members(0), TypeBlockSyntax)
            Dim v2 = TryCast(v1.Members(0), MethodBlockSyntax)

            ' Dim en As E = 0 'widening numeric
            Dim v3 = DirectCast(v2.ChildNodesAndTokens()(1).AsNode(), LocalDeclarationStatementSyntax)
            Assert.NotNull(v3)
            Dim expr1 As ExpressionSyntax = v3.Declarators(0).Initializer.Value
            Assert.Equal("0", expr1.ToString())
            Dim infoP = semanticModel.GetSemanticInfoSummary(expr1)
            Dim enumE = compilation.GlobalNamespace.GetTypeMembers("E").Single()
            Assert.Equal(enumE, infoP.ConvertedType)

            Dim conv1 = semanticModel.ClassifyConversion(expr1, infoP.ConvertedType)
            ValidateConversionInvariants(conv1)
            Assert.True(conv1.IsWidening)
            Assert.False(conv1.IsNarrowing)
            Assert.True(conv1.IsNumeric)

            'Dim enullable As E? = 0 'narrowing nullable
            v3 = DirectCast(v2.ChildNodesAndTokens()(2).AsNode(), LocalDeclarationStatementSyntax)
            Assert.NotNull(v3)
            expr1 = v3.Declarators(0).Initializer.Value
            infoP = semanticModel.GetSemanticInfoSummary(expr1)
            Assert.NotNull(infoP.ConvertedType)
            Assert.Equal("E?", infoP.ConvertedType.ToString())
            conv1 = semanticModel.ClassifyConversion(expr1, infoP.ConvertedType)
            ValidateConversionInvariants(conv1)
            Assert.False(conv1.IsWidening)
            Assert.True(conv1.IsNarrowing) ' should be IsWidening (Bug#5034 is out of scope)
            Assert.False(conv1.IsNumeric)
            Assert.True(conv1.IsNullableValueType)

            '  Dim str As String = chary
            Dim v4 = DirectCast(v2.ChildNodesAndTokens()(7).AsNode(), LocalDeclarationStatementSyntax)
            Assert.NotNull(v4)
            Dim expr2 As ExpressionSyntax = v4.Declarators(0).Initializer.Value
            Assert.Equal("chary", expr2.ToString())
            infoP = semanticModel.GetSemanticInfoSummary(expr2)
            Assert.NotNull(infoP.ConvertedType)
            Dim conv2 = semanticModel.ClassifyConversion(expr2, infoP.ConvertedType)
            ValidateConversionInvariants(conv2)
            Assert.True(conv2.IsWidening)
            Assert.False(conv2.IsNarrowing)
            Assert.True(conv2.IsString)

            '  Dim chary2 As Char() = str
            v4 = DirectCast(v2.ChildNodesAndTokens()(8).AsNode(), LocalDeclarationStatementSyntax)
            Assert.NotNull(v4)
            expr2 = v4.Declarators(0).Initializer.Value
            Assert.Equal("str", expr2.ToString())
            infoP = semanticModel.GetSemanticInfoSummary(expr2)
            Assert.NotNull(infoP.ConvertedType)
            conv2 = semanticModel.ClassifyConversion(expr2, infoP.ConvertedType)
            ValidateConversionInvariants(conv2)
            Assert.False(conv2.IsWidening)
            Assert.True(conv2.IsNarrowing)
            Assert.True(conv2.IsString)

            '  Dim float As Single = str
            v4 = DirectCast(v2.ChildNodesAndTokens()(9).AsNode(), LocalDeclarationStatementSyntax)
            Assert.NotNull(v4)
            expr2 = v4.Declarators(0).Initializer.Value
            infoP = semanticModel.GetSemanticInfoSummary(expr2)
            Assert.NotNull(infoP.ConvertedType)
            conv2 = semanticModel.ClassifyConversion(expr2, infoP.ConvertedType)
            ValidateConversionInvariants(conv2)
            Assert.False(conv2.IsWidening)
            Assert.True(conv2.IsNarrowing)
            Assert.True(conv2.IsString)

            ' str = bb ' narrowing string
            Dim strSym = compilation.GetTypeByMetadataName("System.String")
            Dim v5 = DirectCast(v2.ChildNodesAndTokens()(11).AsNode(), AssignmentStatementSyntax)
            Assert.NotNull(v5)
            expr2 = v5.Right
            conv2 = semanticModel.ClassifyConversion(expr2, strSym)
            ValidateConversionInvariants(conv2)
            Assert.False(conv2.IsWidening)
            Assert.True(conv2.IsNarrowing)
            Assert.True(conv2.IsString)

            CompilationUtils.AssertNoErrors(compilation)

        End Sub


        <WorkItem(541564, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541564")>
        <Fact()>
        Public Sub ClassifyConversionForParameter()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="ClassifyConversion">
    <file name="a.vb">
Imports System

Module Test

    Public Property AP As String

    Private s As String
    Public Property P As String
        Get
            Return s
        End Get
        Set(value As String)
            s = value
        End Set
    End Property

    Sub ObjectParameter(o As Object)
    End Sub

    Sub Main()
        P = "123"
        ObjectParameter(P)

        AP = "456"
        ObjectParameter(AP)

    End Sub
End Module
    </file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            ' property
            Dim argList = DirectCast(CompilationUtils.FindNodeFromText(tree, "(P)"), ArgumentListSyntax)
            Dim arg = DirectCast(argList.ChildNodes().First(), SimpleArgumentSyntax).Expression

            Dim semanticInfo = semanticModel.GetSemanticInfoSummary(arg)
            Dim conv As Conversion = semanticModel.ClassifyConversion(arg, semanticInfo.ConvertedType)
            Assert.True(semanticInfo.ImplicitConversion.IsWidening AndAlso semanticInfo.ImplicitConversion.IsReference, "Expected WideningReference")
            Assert.Equal(semanticInfo.ImplicitConversion, conv)

            ' Auto-implemented
            argList = DirectCast(CompilationUtils.FindNodeFromText(tree, "(AP)"), ArgumentListSyntax)
            arg = DirectCast(argList.ChildNodes().First(), SimpleArgumentSyntax).Expression

            semanticInfo = semanticModel.GetSemanticInfoSummary(arg)
            conv = semanticModel.ClassifyConversion(arg, semanticInfo.ConvertedType)
            Assert.True(semanticInfo.ImplicitConversion.IsWidening AndAlso semanticInfo.ImplicitConversion.IsReference, "Expected WideningReference")
            Assert.Equal(semanticInfo.ImplicitConversion, conv)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(541577, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541577")>
        <Fact()>
        Public Sub ClassifyConversionForPropAsBinaryOperand()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Friend Module M
    Sub S()
        If Prop = 12 Then
            Console.WriteLine(1)
        End If
    End Sub

    Public Property Prop As Integer
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node = DirectCast(CompilationUtils.FindNodeFromText(tree, "Prop"), IdentifierNameSyntax)
            Dim info = semanticModel.GetSemanticInfoSummary(node)

            Dim expr = DirectCast(node.Parent, BinaryExpressionSyntax)
            Dim infoP = semanticModel.GetSemanticInfoSummary(expr)

            Dim conv1 As Conversion = semanticModel.ClassifyConversion(node, infoP.Type)
            Dim conv2 As Conversion = compilation.ClassifyConversion(info.Type, infoP.Type)
            Assert.Equal(conv2, conv1)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact(), WorkItem(544251, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544251")>
        Public Sub ClassifyConversionEnumExplicitOn()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit On
Imports System

Friend Module M
    Sub S()
        Dim x = Color.Red
    End Sub

    Enum Color
        Red = 3
    End Enum
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of MemberAccessExpressionSyntax).First()
            Dim expr = node.Name
            Assert.Equal("Red", expr.ToString())
            Dim info = semanticModel.GetTypeInfo(expr)
            Assert.NotNull(info.Type)
            Dim conv1 = semanticModel.GetConversion(expr)
            Assert.True(conv1.IsIdentity, "Identity")

            Dim conv2 = semanticModel.ClassifyConversion(expr, info.Type)
            Assert.Equal(conv1, conv2)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact(), WorkItem(544251, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544251")>
        Public Sub ClassifyConversionEnumExplicitOff()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Explicit Off
Imports System

Friend Module M
    Sub Main()
        Dim x = Color.Red
    End Sub

    Enum Color
        Red = 3
    End Enum
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim node = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of MemberAccessExpressionSyntax).First()
            Dim expr = node.Name

            Dim info = semanticModel.GetTypeInfo(expr)
            Assert.NotNull(info.Type)
            Dim conv1 = semanticModel.GetConversion(expr)

            Dim conv2 = semanticModel.ClassifyConversion(expr, info.Type)
            Assert.Equal(conv1, conv2)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <Fact(), WorkItem(545101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545101")>
        Public Sub ClassifyConversionNarrowingNullableStrictOff()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off
Imports System

Friend Module M
    Sub Main()
        Dim d1 As Double = 1.2
        Dim d2 As Double? = 1.2

        Dim ret = 1 << d1 AndAlso 2 << d2
    End Sub

End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            ' AndAlso
            Dim node = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of BinaryExpressionSyntax).First()
            ' Shift
            Dim expr = DirectCast(node.Left, BinaryExpressionSyntax)
            ' Double
            Dim id = TryCast(expr.Right, IdentifierNameSyntax)
            Assert.Equal("d1", id.ToString())
            Assert.NotNull(id)
            Dim info = semanticModel.GetTypeInfo(id)
            Assert.NotNull(info.Type)
            Dim conv1 = semanticModel.GetConversion(id)
            Assert.True(conv1.IsNarrowing, "Narrowing")
            Dim conv2 = semanticModel.ClassifyConversion(id, info.ConvertedType)
            Assert.Equal(conv1, conv2)

            ' Shift
            expr = DirectCast(node.Right, BinaryExpressionSyntax)
            ' Duble?
            id = TryCast(expr.Right, IdentifierNameSyntax)
            Assert.Equal("d2", id.ToString())
            Assert.NotNull(id)
            info = semanticModel.GetTypeInfo(id)
            Assert.NotNull(info.Type)
            conv1 = semanticModel.GetConversion(id)
            Assert.True(conv1.IsNarrowing, "Narrowing")
            conv2 = semanticModel.ClassifyConversion(id, info.ConvertedType)
            Assert.Equal(conv1, conv2)

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(799045, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/799045")>
        <Fact()>
        Public Sub ClassifyConversionForArrayLiteral()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Public Class Class1
Sub Goo()
Dim a As Object = CType({1, 2, 3}, Integer())
End Sub
End Class
]]></file>
</compilation>)

            AssertNoErrors(compilation)

            Dim tree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim castNode = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of CTypeExpressionSyntax).First()
            Dim expr = castNode.Expression
            Dim castType = DirectCast(model.GetTypeInfo(castNode.Type).Type, TypeSymbol)
            Assert.Equal("System.Int32()", castType.ToTestDisplayString())

            Dim typeInfo = model.GetTypeInfo(expr)

            Assert.Equal("System.Int32()", typeInfo.ConvertedType.ToTestDisplayString())
            Assert.Null(typeInfo.Type)

            Dim conv1 = model.ClassifyConversion(expr, castType)
            Assert.Equal(ConversionKind.Widening, conv1.Kind)

            Dim conv2 = model.ClassifyConversion(castNode.Span.Start, expr, castType)
            Assert.Equal(ConversionKind.Widening, conv2.Kind)
        End Sub

#End Region

#Region "Msic."

        <Fact>
        Public Sub IsUnmanagedType()
            Dim csharpComp = CreateCSharpCompilation("
public struct S1 { }
public struct S2 { public S1 F1; }
public struct S3 { public object F1; }
public struct S4<T> { public T F1; }
public enum E1 { }
")
            Dim tree = SyntaxFactory.ParseSyntaxTree("
Class C
    Sub M()
        Dim s1 = new S1()
        Dim s2 = new S2()
        Dim s3 = new S3()
        Dim s4 = new S4(Of Integer)()
        Dim e1 = new E1()
    End Sub
End Class")
            Dim comp = CreateCompilation(tree, references:={csharpComp.EmitToImageReference()})
            comp.AssertTheseCompileDiagnostics()
            Dim model = comp.GetSemanticModel(tree)
            Dim root = tree.GetRoot()
            Dim getLocalType = Function(name As String) As ITypeSymbol
                                   Dim decl = root.DescendantNodes().
                                   OfType(Of ModifiedIdentifierSyntax)().
                                   Single(Function(n) n.Identifier.ValueText = name)
                                   Return CType(model.GetDeclaredSymbol(decl), ILocalSymbol).Type
                               End Function
            ' VB does not have a concept of a managed type
            Assert.False(getLocalType("s1").IsUnmanagedType)
            Assert.False(getLocalType("s2").IsUnmanagedType)
            Assert.False(getLocalType("s3").IsUnmanagedType)
            Assert.False(getLocalType("s4").IsUnmanagedType)
            Assert.False(getLocalType("e1").IsUnmanagedType)
        End Sub

        <Fact>
        Public Sub IsRefLikeType()
            Dim csharpComp = CreateCSharpCompilation("
public struct S1 { }
public ref struct S2 { public S1 F1; }
public enum E1 { }
", parseOptions:=New CSharp.CSharpParseOptions(CSharp.LanguageVersion.CSharp7_3))
            Dim tree = SyntaxFactory.ParseSyntaxTree("
Structure S3 
    Dim F1 As Object
End Structure
Class C
    Sub M()
        Dim s1 = new S1()
        Dim s2 = new S2()
        Dim s3 = new S3()
        Dim e1 = new E1()
    End Sub
End Class")
            Dim comp = CreateCompilation(tree, references:={csharpComp.EmitToImageReference()})
            comp.AssertTheseDiagnostics(<errors>
BC30668: 'S2' is obsolete: 'Types with embedded references are not supported in this version of your compiler.'.
        Dim s2 = new S2()
                     ~~
                                        </errors>)
            Dim model = comp.GetSemanticModel(tree)
            Dim root = tree.GetRoot()
            Dim getLocalType = Function(name As String) As ITypeSymbol
                                   Dim decl = root.DescendantNodes().
                                   OfType(Of ModifiedIdentifierSyntax)().
                                   Single(Function(n) n.Identifier.ValueText = name)
                                   Return CType(model.GetDeclaredSymbol(decl), ILocalSymbol).Type
                               End Function
            ' VB does not have a concept of a ref-like type
            Assert.False(getLocalType("s1").IsRefLikeType)
            Assert.False(getLocalType("s2").IsRefLikeType)
            Assert.False(getLocalType("s3").IsRefLikeType)
            Assert.False(getLocalType("e1").IsRefLikeType)
        End Sub

        <Fact()>
        Public Sub IsAccessible()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="IsAccessible">
    <file name="a.vb">
    Imports System
    Class A
        Public X As Integer 
        Protected Y As Integer
        Private Protected Z As Integer
    End Class
    Class B
        Inherits A 
        Sub Goo()
            Console.WriteLine() ' in B.Goo
        End Sub
        ' in B class level
        Dim field as Integer
    End Class
    Class C
        Sub Goo()
            Console.WriteLine() ' in C.Goo
        End Sub
    End Class
    Namespace N  ' in N
    End Namespace
    </file>
</compilation>,
            parseOptions:=TestOptions.Regular.WithLanguageVersion(LanguageVersion.VisualBasic15_5))

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim positionInB As Integer = CompilationUtils.FindPositionFromText(tree, "in B class level")
            Dim positionInBGoo As Integer = CompilationUtils.FindPositionFromText(tree, "in B.Goo")
            Dim positionInCGoo As Integer = CompilationUtils.FindPositionFromText(tree, "in C.Goo")
            Dim positionInN As Integer = CompilationUtils.FindPositionFromText(tree, "in N")

            Dim globalNS = compilation.GlobalNamespace
            Dim classA = DirectCast(globalNS.GetMembers("A").Single(), NamedTypeSymbol)
            Dim fieldX = DirectCast(classA.GetMembers("X").Single(), FieldSymbol)
            Dim fieldY = DirectCast(classA.GetMembers("Y").Single(), FieldSymbol)
            Dim fieldZ = DirectCast(classA.GetMembers("Z").Single(), FieldSymbol)

            Assert.True(semanticModel.IsAccessible(positionInN, fieldX))
            Assert.False(semanticModel.IsAccessible(positionInN, fieldY))
            Assert.False(semanticModel.IsAccessible(positionInN, fieldZ))
            Assert.True(semanticModel.IsAccessible(positionInB, fieldX))
            Assert.True(semanticModel.IsAccessible(positionInB, fieldY))
            Assert.True(semanticModel.IsAccessible(positionInB, fieldZ))
            Assert.True(semanticModel.IsAccessible(positionInBGoo, fieldX))
            Assert.True(semanticModel.IsAccessible(positionInBGoo, fieldY))
            Assert.True(semanticModel.IsAccessible(positionInBGoo, fieldZ))
            Assert.True(semanticModel.IsAccessible(positionInCGoo, fieldX))
            Assert.False(semanticModel.IsAccessible(positionInCGoo, fieldY))
            Assert.False(semanticModel.IsAccessible(positionInCGoo, fieldZ))

            CompilationUtils.AssertNoErrors(compilation)
        End Sub

        <WorkItem(652109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/652109")>
        <Fact()>
        Public Sub Bug652109()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="IsAccessible">
    <file name="a.vb">
        <![CDATA[
but specify providing one type on the lamb infer others")

            'INFERENCE nfence by specify a single lambda allowingric Extension Method, Lambda arguments - I--------
            apInitScenario("Gene------------------------------------------   '--------------------------------------Function(c) c, Function(d) d)

          Object)(1, Function(a) a, Function(b) b,  z1d = Target5(Of Integer, Single, Double,tion(c) c, Function(d) d)
            Dimect)(1, Function(a) a, Function(b) b, Func= Target5(Of Integer, Integer, Double, Objc) c, Function(d) d)
            Dim z1c 1, Function(a) a, Function(b) b, Function(get5(Of Integer, Integer, Object, Object)( in the lambdas
            Dim z1b = Tareneric types as well as the variable typesnt 
            'Verify the return type G Different Types using generic Type argument c, Function(d) d)

            'Specify Function(a) a, Function(b) b, Function(c)et5(Of Integer, Object, Object, Object)(1,e - All Object
            Dim z1a = Targ Generic Types which result in no inferenc   'SPECIFY TYPES
            'Specifyingments - Specify Generic Type ")
         rio("Generic Extension Method, Lambda argu-----------------
            apInitScena------------------------------------------            '-----------------------------, Function(c) c, Function(d) d)
#End If
 = sv.Target5(Function(a) a, Function(b) bc) c, Function(d) d)"
            Dim z0at5(Function(a) a, Function(b) b, Function(         'COMPILEERROR: BC36646, "sv.Targeb) b, Function(c) c, Function(d) d)

    z0 = Target5(sv, Function(a) a, Function(ion(c) c, Function(d) d)"
            Dimt5(sv, Function(a) a, Function(b) b, Funct            'COMPILEERROR: BC36645, "TargeTYPES - Shared and Extension Method Call

n(c) c, Function(d) d)
            apCompingle) a, Function(b As Double) b, Functio      Dim z5a = sv.Target5(Function(a As Sype.ToString, "Unexpected Type")

      nc`2[System.Int32,System.Single]", z5.GetTon(d) d)
            apCompare("System.Fution(b As Single) b, Function(c) c, Functi sv.Target5(Function(a As Integer) a, Funcom Func 1st argument
            Dim z5 =pes
            'Infer T and U directy fr     'Provide 2 different types - infer tyin lambda allowing infer others")
        arguments - Infence by specify two types Scenario("Generic Extension Method, Lambda----------------------
            apInit------------------------------------------)

            '------------------------]", z4.GetType.ToString, "Unexpected Type"e("System.Func`2[System.Int32,System.Int32ion(d As Integer) d)
            apCompar(a) a, Function(b) b, Function(c) c, Funct
            Dim z4 = sv.Target5(Function z3.GetType.ToString, "Unexpected Type")
System.Func`2[System.Int32,System.Int32]",c, Function(d) d)
            apCompare(" a, Function(b) b, Function(c As Integer)            Dim z3 = sv.Target5(Function(a).GetType.ToString, "Unexpected Type")

 tem.Func`2[System.Int32,System.Int32]", z2Function(d) d)
            apCompare("Sys Function(b As Integer) b, Function(c) c,         Dim z2 = sv.Target5(Function(a) a,tType.ToString, "Unexpected Type")

    .Func`2[System.Int32,System.Int32]", z1.Gection(d) d)
            apCompare("Systemeger) a, Function(b) b, Function(c) c, Fun     Dim z1 = sv.Target5(Function(a As Inty specifyint a type on the lambda
                   'In this case I define T,U,V be arguments then I can infer other types
So if I provide a single type to any of thda - others will be infered
            '
End Module





        Return x
    End Function



Val w As Func(Of U, V)) As Func(Of T, U)
 T), _
                                By                     ByVal z As Func(Of V,  ByVal y As Func(Of U, T), _
           f T, U), _
                                                       ByVal x As Func(Ot5(Of S, T, U, V)(ByVal a As S, _
       lerServices.Extension()> _
Function TargedTest()
    End Sub

    <Runtime.Compiry
#End If
        End Try
        apEnCatch ex As Exception

            End T
                p.Kill()

            .Diagnostics.Process.GetProcessById(appid)m p As System.Diagnostics.Process = System Then
            Try
                Diap()
        Finally
#If Not ULTRAVIOLETest:
        Catch
            apErrorTr ' Exit test routine
            '
ExitTected Type")

            '
           tem.Single]", z6a.GetType.ToString, "UnexpapCompare("System.Func`2[System.Single,SysAs Double) c, Function(d) d)
            a As Single) a, Function(b) b, Function(c             Dim z6a = sv.Target5(Function(6.GetType.ToString, "Unexpected Type")

stem.Func`2[System.Int32,System.Int32]", z Function(d) d)
            apCompare("Sya, Function(b) b, Function(c As Single) c,im z6 = sv.Target5(Function(a As Integer) or T and U will be the same
            Da result of T
            'result types frovide Types for T and V, U is infered as ring, "Unexpected Type")

            'p.Boolean,System.Double]", z5b.GetType.ToSt           apCompare("System.Func`2[SystemFunction(c) c, Function(d As Double) d)
 (Function(a As Boolean) a, Function(b) b, Type")

            Dim z5b = sv.Target5uble]", z5a.GetType.ToString, "Unexpected are("System.Func`2[System.Single,System.Do
]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim model = compilation.GetSemanticModel(tree)

            For Each name In From x In NameSyntaxFinder.FindNames(tree.GetRoot()) Where x.ToString() = "sv" Select x
                model.GetSymbolInfo(name)
            Next
        End Sub

        <WorkItem(652026, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/652026")>
        <Fact()>
        Public Sub Bug652026()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="IsAccessible">
    <file name="a.vb">
        <![CDATA[
oads Property olp5(ByVal Sng As Single) As    End Property


        Public Overlnfo = "Integer"
            End Set
             I4 = I4 + 1
                strIr, VarType(I4), "Wrong Type", "")
                     apCompare(VariantType.Intege          Set(ByVal Value As String)

   olp5 = "Integer"
            End Get
                I4 = I4 + 1
               nteger, VarType(I4), "Wrong Type", "")
  t
                apCompare(VariantType.Il I4 As Integer) As String
            Ge       Public Overloads Property olp5(ByVa      End Set
        End Property


                 strInfo = "Single"
      ype", "")
                Sng = Sng + 1
VariantType.Single, VarType(Sng), "Wrong Tlue As String)
                apCompare(         End Get
            Set(ByVal Va + 1
                olp4 = "Single"
   ng Type", "")

                Sng = Sngare(VariantType.Single, VarType(Sng), "Wrog
            Get
                apComproperty olp4(ByVal Sng As Single) As Strind Property


        Public Overloads PatedType"
            End Set
        Enenu + 1
                strInfo = "EnumerWrong Type", "")

                enu =       apCompare(VbInteger, VarType(enu), "            Set(ByVal Value)

           = "EnumeratedType"
            End Get
       enu = enu + 1
                olp4rType(enu), "Wrong Type", "")

         t
                apCompare(VbInteger, VaVal enu As EnumeratedType)
            Ge
        Public Overloads Property olp4(By-----------------------------------



------------------------------------------     ' Properties
        '
        '-----------------------------
        '
   ------------------------------------------versions
        '
        '------------
e(Dec), "Wrong Type", "")

                  apCompare(VariantType.Decimal, VarTyp Set(ByVal Value As String)

           Decimal"
            End Get
              Dec = Dec + 1
                olp7 = "e(Dec), "Wrong Type", "")

                  apCompare(VariantType.Decimal, VarTypl) As String
            Get
           verloads Property olp7(ByVal Dec As Decima        End Property


        Public O strInfo = "Single"
            End Set
            Sng = Sng + 1
               e, VarType(Sng), "Wrong Type", "")

                   apCompare(VariantType.Singl           Set(ByVal Value As String)

    olp6 = "Single"
            End Get
               Sng = Sng + 1
             gle, VarType(Sng), "Wrong Type", "")

  
                apCompare(VariantType.SinSng As Single) As String
            Get
     Public Overloads Property olp6(ByVal     End Set
        End Property


                   strInfo = "Long"
        ype", "")

                I8 = I8 + 1
   apCompare(VbLong, VarType(I8), "Wrong Tet(ByVal Value As String)

             "Long"
            End Get
            S      I8 = I8 + 1
                olp6 = rType(I8), "Wrong Type", "")

           Get
                apCompare(VbLong, Va6(ByVal I8 As Long) As String
           


        Public Overloads Property olp            End Set
        End Property
 + 1
                strInfo = "Single"
rong Type", "")
                Sng = Sngmpare(VariantType.Single, VarType(Sng), "Wl Value As String)

                apCo
            End Get
            Set(ByVa Sng + 1
                olp5 = "Single"
, "Wrong Type", "")
                Sng =apCompare(VariantType.Single, VarType(Sng) String
            Get
                
e(Cur), "Wrong Type", "")

                  apCompare(VariantType.Decimal, VarTypl) As String
            Get
           erloads Property olp10(ByVal Cur As Decima       End Property


        Public OvstrInfo = "Single"
            End Set
            Sng = Sng + 1
                , VarType(Sng), "Wrong Type", "")

                   apCompare(VariantType.Single          Set(ByVal Value As String)

    olp9 = "Single"
            End Get
               Sng = Sng + 1
              le, VarType(Sng), "Wrong Type", "")

                   apCompare(VariantType.Singng As Single) As String
            Get
    Public Overloads Property olp9(ByVal S   End Set
        End Property


                 strInfo = "Double"
          "")

                Dbl = Dbl + 1
   ntType.Double, VarType(Dbl), "Wrong Type",String)

                apCompare(Varia  End Get
            Set(ByVal Value As                olp9 = "Double"
          ", "")

                Dbl = Dbl + 1
 iantType.Double, VarType(Dbl), "Wrong Type      Get

                apCompare(Varlp9(ByVal Dbl As Double) As String
      y


        Public Overloads Property o
            End Set
        End Propertng + 1
                strInfo = "Single"rong Type", "")

                Sng = Smpare(VariantType.Single, VarType(Sng), "Wl Value As String)

                apCo
            End Get
            Set(ByVa Sng + 1
                olp7 = "Single"
"Wrong Type", "")

                Sng =Compare(VariantType.Single, VarType(Sng), ing
            Get

                ap Property olp7(ByVal Sng As Single) As StrEnd Property


        Public Overloads= "Decimal"
            End Set
           Dec = Dec + 1
                strInfo 
             strInfo = "Single"
          "")

                Sng = Sng + 1
   ntType.Single, VarType(Sng), "Wrong Type",String)

                apCompare(Varia  End Get
            Set(ByVal Value As               olp12 = "Single"
          , "")

                Sng = Sng + 1
  antType.Single, VarType(Sng), "Wrong Type"     Get

                apCompare(Vari12(ByVal Sng As Single) As String
       


        Public Overloads Property olp          End Set
        End Property

x"
                strInfo = "String"
  ype", "")

                Str = Str & "VariantType.String, VarType(Str), "Wrong Te As String)

                apCompare(       End Get
            Set(ByVal Valu"
                olp12 = "String"
     pe", "")

                Str = Str & "xariantType.String, VarType(Str), "Wrong Ty        Get

                apCompare(Volp12(ByVal Str As String) As String
    ty


        Public Overloads Property "
            End Set
        End ProperSng + 1
                strInfo = "SingleWrong Type", "")

                Sng = ompare(VariantType.Single, VarType(Sng), "al Value As String)

                apC
            End Get
            Set(ByV Sng + 1
                olp10 = "Single""Wrong Type", "")

                Sng =Compare(VariantType.Single, VarType(Sng), tring
            Get
                aps Property olp10(ByVal Sng As Single) As S End Property


        Public Overload = "Decimal"
            End Set
           Cur = Cur + 1
                strInfope(Cur), "Wrong Type", "")

                  apCompare(VariantType.Decimal, VarTy  Set(ByVal Value As String)

          "Decimal"
            End Get
             Cur = Cur + 1
                olp10 = 
, "Wrong Type", "")

                SngapCompare(VariantType.Single, VarType(Sng)tring
            Get

                s Property olp14(ByVal Sng As Single) As S End Property


        Public Overloadnfo = "Date"
            End Set
       nterval.Day, 1, Dte)
                strI"")

                Dte = DateAdd(DateIantType.Date, VarType(Dte), "Wrong Type",  String)

                apCompare(Vari   End Get
            Set(ByVal Value As
                olp14 = "Date"
           Dte = DateAdd(DateInterval.Day, 1, Dte)
(Dte), "Wrong Type", "")

                     apCompare(VariantType.Date, VarTypee) As String
            Get

          Overloads Property olp14(ByVal Dte As Dat        End Property



        Public strInfo = "Single"
            End Set
            Sng = Sng + 1
               e, VarType(Sng), "Wrong Type", "")

                   apCompare(VariantType.Singl           Set(ByVal Value As String)

   olp13 = "Single"
            End Get
              Sng = Sng + 1
              le, VarType(Sng), "Wrong Type", "")

                   apCompare(VariantType.Sing As Single) As String
            Get

 Public Overloads Property olp13(ByVal SngEnd Set
        End Property


                 strInfo = "String"
            

                Str = Str & "x"
      e.String, VarType(Str), "Wrong Type", "")



                apCompare(VariantTypet
            Set(ByVal Value As String)       olp13 = "String"
            End G                Str = Str & "x"
         tring, VarType(Str), "Wrong Type", "")



                apCompare(VariantType.SStr As String) As String
            Get
    Public Overloads Property olp13(ByVal    End Set
        End Property


    
ype", "")

                Dbl = Dbl + 1VariantType.Double, VarType(Dbl), "Wrong Te As String)

                apCompare(       End Get
            Set(ByVal Valu1
                olp9b = "Double"
     Type", "")

                Dbl = Dbl + (VariantType.Double, VarType(Dbl), "Wrong             Get
                apCompares Double, ByVal Dbl As Double) As String
blic Overloads Property olp9b(ByVal Dbl2 A Set
        End Property


        Pu       strInfo = "Single"
            End
                Sng = Sng + 1
         .Single, VarType(Sng), "Wrong Type", "")
)

                apCompare(VariantTypeGet
            Set(ByVal Value As String        olp15 = "Single"
            End 

                Sng = Sng + 1
        e.Single, VarType(Sng), "Wrong Type", "")
et

                apCompare(VariantTypal Sng As Single) As String
            G       Public Overloads Property olp15(ByVnd Set
        End Property
#End If

         strInfo = "object "
            E
                vnt = vnt & "x"
        String, VarType(vnt), "Wrong Type", "")



                apCompare(VariantType.et
            Set(ByVal Value As String)      olp15 = "object "
            End G               vnt = vnt & "x"
          ring, VarType(vnt), "Wrong Type", "")

 
                apCompare(VariantType.Stnt As Object) As String
            Get
   Public Overloads Property olp15(ByVal verty

#If VBCORE=True Then
#Else
     le"
            End Set
        End Prop= Sng + 1
                strInfo = "Sing "Wrong Type", "")

                Sng pCompare(VariantType.Single, VarType(Sng),yVal Value As String)

                ae"
            End Get
            Set(B = Sng + 1
                olp14 = "Singl
nd Namespace

        End Property

    End Class
E   strInfo = "Single"
            End Set              Sng = Sng + 1
             gle, VarType(Sng), "Wrong Type", "")

  
                apCompare(VariantType.Sin
            Set(ByVal Value As String)

   olp14b = "Single"
            End Get
              Sng = Sng + 1
             gle, VarType(Sng), "Wrong Type", "")

  
                apCompare(VariantType.SinDbl As Double) As String
            Get
Val Sng As Single, ByVal C As Char, ByVal        Public Overloads Property olp14b(By      End Set
        End Property


 
                strInfo = "Date"
         Dte = DateAdd(DateInterval.Day, 1, Dte)e(Dte), "Wrong Type", "")

                     apCompare(VariantType.Date, VarTyp    Set(ByVal Value As String)

        4b = "Date"
            End Get
        nterval.Day, 1, Dte)
                olp1"")

                Dte = DateAdd(DateIantType.Date, VarType(Dte), "Wrong Type",        Get
                apCompare(Varihar, ByVal Dbl As Double) As String
     rty olp14b(ByVal Dte As Date, ByVal C As Coperty


        Public Overloads Propengle"
            End Set
        End Prg = Sng + 1
                strInfo = "Si), "Wrong Type", "")

                Sn apCompare(VariantType.Single, VarType(Sng(ByVal Value As String)

               gle"
            End Get
            Setng = Sng + 1
                olp9b = "Sing), "Wrong Type", "")

                S  apCompare(VariantType.Single, VarType(SnAs String
            Get
              yVal Dbl2 As Double, ByVal Sng As Single) 
        Public Overloads Property olp9b(B        End Set
        End Property



                strInfo = "Double"
    
]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim model = compilation.GetSemanticModel(tree)

            For Each name In From x In ExpressionSyntaxFinder.FindExpression(tree.GetRoot())
                             Where x.Kind = SyntaxKind.StringLiteralExpression AndAlso
                                   x.ToString() = """Single""" Select x
                model.GetSymbolInfo(name)
            Next
        End Sub

        <WorkItem(652118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/652118")>
        <Fact()>
        Public Sub Bug652118()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="IsAccessible">
    <file name="a.vb">
        <![CDATA[
b "goo" (ByRef aa@,ByRef  aa@)                                
Declare Sub SUB29 Lio" (ByRef aa@,ByRef  aa&)                                
Declare Sub SUB28 Lib "foyRef aa@,ByRef  aa!)                                
Declare Sub SUB27 Lib "goo" (Baa@,ByRef  aa%)                                
Declare Sub SUB26 Lib "goo" (ByRef yRef  aa$)                                
Declare Sub SUB25 Lib "goo" (ByRef aa@,B           
' currency with all datatypes aa&,ByRef  aa#)                                
Declare Sub SUB24 Lib "goo" (ByRefByRef  aa@)                                
Declare Sub SUB23 Lib "goo" (ByRef aa&,  aa&)                                    clare Sub SUB22 Lib "goo" (ByRef aa&,ByRef)                                     
De Sub SUB21 Lib "goo" (ByRef aa&,ByRef  aa!                                 
DeclareSUB20 Lib "goo" (ByRef aa&,ByRef  aa%)                                
Declare Sub  Lib "goo" (ByRef aa&,ByRef  aa$)         long with all datatypes
Declare Sub SUB19)                                     
'  Sub SUB18 Lib "goo" (ByRef aa!,ByRef  aa#                                 
DeclareSUB17 Lib "goo" (ByRef aa!,ByRef  aa@)                                
Declare Sub  Lib "goo" (ByRef aa!,ByRef  aa&)                                
Declare Sub SUB16"goo" (ByRef aa!,ByRef  aa!)                                
Declare Sub SUB15 Lib  (ByRef aa!,ByRef  aa%)                                
Declare Sub SUB14 Lib "goo"ef aa!,ByRef  aa$)                        atatypes
Declare Sub SUB13 Lib "goo" (ByR                     
' single with all doo" (ByRef aa#,ByRef  aa#)                                
Declare Sub SUB12 Lib "fByRef aa#,ByRef  aa@)                                
Declare Sub SUB11 Lib "goo" (
a As String)
        Declare Sub SUB46 LiB45 Lib "goo" (ByRef aa As Object, ByRef ape w/all datatypes
        Declare Sub SU                      

' default datatynteger, ByRef aa As Object)               Single, ByRef aa As Decimal, ByRef aa As IAs String, ByRef aa As Short, ByRef aa As a As Object, ByRef aa As Double, ByRef aa       Declare Sub SUB44 Lib "goo" (ByRef a aa%, ByRef aa!, ByRef aa@, ByRef aa&)
   aa As Object, ByRef aa#, ByRef aa$, ByRef        Declare Sub SUB43 Lib "goo" (ByRef                       
' all datatypes
"goo" (ByRef aa$,ByRef  aa#)                                
Declare Sub SUB42 Lib  (ByRef aa$,ByRef  aa@)                                
Declare Sub SUB41 Lib "goo"ef aa$,ByRef  aa&)                                
Declare Sub SUB40 Lib "goo" (ByR$,ByRef  aa!)                                
Declare Sub SUB39 Lib "goo" (ByRef aaef  aa%)                                  Declare Sub SUB38 Lib "goo" (ByRef aa$,ByRa$)                                     
re Sub SUB37 Lib "goo" (ByRef aa$,ByRef  a      
' string with all datatypes
DeclaByRef  aa#)                                
Declare Sub SUB36 Lib "goo" (ByRef aa%,  aa@)                                    clare Sub SUB35 Lib "goo" (ByRef aa%,ByRef)                                     
De Sub SUB34 Lib "goo" (ByRef aa%,ByRef  aa&                                 
DeclareSUB33 Lib "goo" (ByRef aa%,ByRef  aa!)                                
Declare Sub  Lib "goo" (ByRef aa%,ByRef  aa%)                                
Declare Sub SUB32"goo" (ByRef aa%,ByRef  aa$)              with all datatypes
Declare Sub SUB31 Lib                               
' integer 30 Lib "goo" (ByRef aa@,ByRef  aa#)                                
Declare Sub SUB
                                          oo" (ByRef aa!,ByRef  aa as Decimal)                      
Declare Sub SUB61 Lib "fByRef  aa as integer)                      
Declare Sub SUB60 Lib "goo" (ByRef aa!,ingle)                                     SUB59 Lib "goo" (ByRef aa!,ByRef  aa as s                             
Declare Suboo" (ByRef aa!,ByRef  aa as short)                        
Declare Sub SUB58 Lib "f,ByRef  aa as string)                     es
Declare Sub SUB57 Lib "goo" (ByRef aa!               
' single with all datatypByRef  aa as object)                      
Declare Sub SUB50_2 Lib "goo" (ByRef aa#,ble)                                     
UB56 Lib "goo" (ByRef aa#,ByRef  aa as dou                           
Declare Sub S                                          goo" (ByRef aa#,ByRef  aa as Decimal)                      
Declare Sub SUB55 Lib ",ByRef  aa as integer)                      
Declare Sub SUB54 Lib "goo" (ByRef aa#single)                                   b SUB53 Lib "goo" (ByRef aa#,ByRef  aa as                               
Declare Sufoo" (ByRef aa#,ByRef  aa as short)                        
Declare Sub SUB52 Lib "#,ByRef  aa as string)                    pes
Declare Sub SUB51 Lib "goo" (ByRef aaef aa As Object)
' double with all datatySUB50_1 Lib "goo" (ByRef aa As Object, ByR ByRef aa As Double)
        Declare Sub e Sub SUB50 Lib "goo" (ByRef aa As Object,                          
        DeclarAs Object, ByRef aa As Decimal)               Declare Sub SUB49 Lib "goo" (ByRef aa f aa As Object, ByRef aa As Integer)
    
        Declare Sub SUB48 Lib "goo" (ByRe (ByRef aa As Object, ByRef aa As Single)
hort)
        Declare Sub SUB47 Lib "goo"b "goo" (ByRef aa As Object, ByRef aa As S
            
' integer with all datatypesef  aa as object)                         clare Sub SUB50_5 Lib "goo" (ByRef aa@,ByR)                                     
De4 Lib "goo" (ByRef aa@,ByRef  aa as double                        
Declare Sub SUB7                                          " (ByRef aa@,ByRef  aa as Decimal)                      
Declare Sub SUB73 Lib "gooRef  aa as integer)                       
Declare Sub SUB72 Lib "goo" (ByRef aa@,Bygle)                                     
UB71 Lib "goo" (ByRef aa@,ByRef  aa as sin                           
Declare Sub S" (ByRef aa@,ByRef  aa as short)                        
Declare Sub SUB70 Lib "gooyRef  aa as string)                       
Declare Sub SUB69 Lib "goo" (ByRef aa@,B           
' currency with all datatypesf  aa as object)                          lare Sub SUB50_4 Lib "goo" (ByRef aa&,ByRe                                     
Dec Lib "goo" (ByRef aa&,ByRef  aa as double)                       
Declare Sub SUB68                                           (ByRef aa&,ByRef  aa as Decimal)                      
Declare Sub SUB67 Lib "goo"ef  aa as integer)                        Declare Sub SUB66 Lib "goo" (ByRef aa&,ByRle)                                     
B65 Lib "goo" (ByRef aa&,ByRef  aa as sing                          
Declare Sub SU (ByRef aa&,ByRef  aa as short)                        
Declare Sub SUB64 Lib "goo"Ref  aa as string)                        
Declare Sub SUB63 Lib "goo" (ByRef aa&,By              
' long with all datatypes
yRef  aa as object)                       Declare Sub SUB50_3 Lib "goo" (ByRef aa!,Ble)                                     
B62 Lib "goo" (ByRef aa!,ByRef  aa as doub                          
Declare Sub SU
     
Declare Sub SUB89 Lib "goo" (ByRef  as short)                                88 Lib "goo" (ByRef aa as double,ByRef  aa                         
Declare Sub SUBas double,ByRef  aa as string)            es
Declare Sub SUB87 Lib "goo" (ByRef aa            


' double with all datatypf  aa as object)                          lare Sub SUB50_7 Lib "goo" (ByRef aa$,ByRe                                     
Dec Lib "goo" (ByRef aa$,ByRef  aa as double)                       
Declare Sub SUB86                                           (ByRef aa$,ByRef  aa as Decimal)                      
Declare Sub SUB85 Lib "goo"ef  aa as integer)                        Declare Sub SUB84 Lib "goo" (ByRef aa$,ByRle)                                     
B83 Lib "goo" (ByRef aa$,ByRef  aa as sing                          
Declare Sub SU (ByRef aa$,ByRef  aa as short)                        
Declare Sub SUB82 Lib "goo"Ref  aa as string)                        
Declare Sub SUB81 Lib "goo" (ByRef aa$,By            
' string with all datatypes
ef  aa as object)                         clare Sub SUB50_6 Lib "goo" (ByRef aa%,ByR)                                     
De0 Lib "goo" (ByRef aa%,ByRef  aa as double                        
Declare Sub SUB8                                          " (ByRef aa%,ByRef  aa as Decimal)                      
Declare Sub SUB79 Lib "gooRef  aa as integer)                       
Declare Sub SUB78 Lib "goo" (ByRef aa%,Bygle)                                     
UB77 Lib "goo" (ByRef aa%,ByRef  aa as sin                           
Declare Sub S" (ByRef aa%,ByRef  aa as short)                        
Declare Sub SUB76 Lib "gooyRef  aa as string)                       
Declare Sub SUB75 Lib "goo" (ByRef aa%,B
                          
Declare Sub SU                                           aa as integer,ByRef  aa as Decimal)           
Declare Sub SUB103 Lib "goo" (ByRefs integer)                                Lib "goo" (ByRef aa as integer,ByRef  aa a                     
Declare Sub SUB102 teger,ByRef  aa as single)                clare Sub SUB101 Lib "goo" (ByRef aa as in)                                     
Deo" (ByRef aa as integer,ByRef  aa as short              
Declare Sub SUB100 Lib "foyRef  aa as string)                       Sub SUB99 Lib "goo" (ByRef aa as integer,B     
' long with all datatypes
Declare as double)                                8 Lib "goo" (ByRef aa as single,ByRef  aa                         
Declare Sub SUB9                                          aa as single,ByRef  aa as Decimal)             
Declare Sub SUB97 Lib "goo" (ByRef s integer)                                 Lib "goo" (ByRef aa as single,ByRef  aa a                       
Declare Sub SUB96 single,ByRef  aa as single)              
Declare Sub SUB95 Lib "goo" (ByRef aa ashort)                                     b "goo" (ByRef aa as single,ByRef  aa as s                    
Declare Sub SUB94 Lingle,ByRef  aa as string)                 eclare Sub SUB93 Lib "goo" (ByRef aa as si          
' single with all datatypes
D  aa as double)                            SUB92 Lib "goo" (ByRef aa as double,ByRef                             
Declare Sub                                          yRef aa as double,ByRef  aa as Decimal)             
Declare Sub SUB91 Lib "goo" (B aa as integer)                           SUB90 Lib "goo" (ByRef aa as double,ByRef                             
Declare Sub aa as double,ByRef  aa as single)         
with all datatypes
Declare Sub SUB117 Lib                               
' string Ref aa as short,ByRef  aa as double)              
Declare Sub SUB116 Lib "goo" (By                                          ef  aa as Decimal)                        ub SUB115 Lib "goo" (ByRef aa as short,ByR                               
Declare Sef aa as short,ByRef  aa as integer)             
Declare Sub SUB114 Lib "goo" (ByRa as single)                              B113 Lib "goo" (ByRef aa as short,ByRef  a                          
Declare Sub SUaa as short,ByRef  aa as short)               
Declare Sub SUB112 Lib "goo" (ByRef s string)                                 1 Lib "goo" (ByRef aa as short,ByRef  aa aeger with all datatypes
Declare Sub SUB11                                   
' intle)                                       oo" (ByRef aa as Decimal,ByRef  aa as doub               
Declare Sub SUB110 Lib "f                                          mal,ByRef  aa as Decimal)                 are Sub SUB109 Lib "goo" (ByRef aa as Deci                                    
Declger)                                      oo" (ByRef aa as Decimal,ByRef  aa as inte               
Declare Sub SUB108 Lib "f                                          imal,ByRef  aa as single)                 lare Sub SUB107 Lib "goo" (ByRef aa as Dec                                     
Dechort)                                      "goo" (ByRef aa as Decimal,ByRef  aa as s                  
Declare Sub SUB106 Lib                                          Decimal,ByRef  aa as string)              Declare Sub SUB105 Lib "goo" (ByRef aa as          
' currency with all datatypes
 aa as double)                            B104 Lib "goo" (ByRef aa as integer,ByRef 
              
End Namespace                                                                     
                    

    End Module    eger, ByRef aa As Object)                 ngle, ByRef aa As Decimal, ByRef aa As Int String, ByRef aa As Short, ByRef aa As SiAs Object, ByRef aa As Double, ByRef aa As   Declare Sub SUB139 Lib "goo" (ByRef aa                               
 

     f aa as object,ByRef  aa as double)             
Declare Sub SUB128 Lib "goo" (ByRe                                            aa as Decimal)                          SUB127 Lib "goo" (ByRef aa as object,ByRef                            
Declare Sub a as object,ByRef  aa as integer)            
Declare Sub SUB126 Lib "goo" (ByRef a single)                                  Lib "goo" (ByRef aa as object,ByRef  aa as                     
Declare Sub SUB125 object,ByRef  aa as short)                Declare Sub SUB124 Lib "goo" (ByRef aa as ng)                                     
goo" (ByRef aa as object,ByRef  aa as strith all datatypes
Declare Sub SUB123 Lib "                                
' ANY wiRef aa as string,ByRef  aa as double)             
Declare Sub SUB122 Lib "goo" (By                                          ef  aa as Decimal)                        b SUB121 Lib "goo" (ByRef aa as string,ByR                              
Declare Su aa as string,ByRef  aa as integer)            
Declare Sub SUB120 Lib "goo" (ByRefas single)                                9 Lib "goo" (ByRef aa as string,ByRef  aa                        
Declare Sub SUB11s string,ByRef  aa as short)              
Declare Sub SUB118 Lib "goo" (ByRef aa aring)                                      "goo" (ByRef aa as string,ByRef  aa as st
]]></file>
</compilation>)

            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model = compilation.GetSemanticModel(tree)

            For Each name In SyntaxNodeFinder.FindNodes(tree.GetRoot(), SyntaxKind.DeclareSubStatement)
                model.GetDeclaredSymbol(DirectCast(name, DeclareStatementSyntax))
            Next
        End Sub

        <Fact>
        Public Sub Codecoverage_Additions()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Coverage">
    <file name="a.vb">
    Public Module M
    Sub SelectCaseExpression()
        Select Case (Function(arg) arg Is Nothing)'BIND:""Function(arg) arg Is Nothing""
        End Select
    End Sub
End Module
    </file>
</compilation>)
            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            Dim syntaxNode1 = tree.FindNodeOrTokenByKind(SyntaxKind.SingleLineFunctionLambdaExpression).AsNode()
            Dim semanticSummary = semanticModel.GetSemanticInfoSummary(DirectCast(syntaxNode1, SingleLineLambdaExpressionSyntax))

            Assert.Null(semanticSummary.Type)
            Assert.Equal("Function <generated method>(arg As System.Object) As System.Boolean", semanticSummary.ConvertedType.ToTestDisplayString())
            Assert.True(semanticSummary.ImplicitConversion.IsLambda)
            Assert.False(semanticSummary.ImplicitConversion.IsAnonymousDelegate)

            Dim typeSymbolList As IList(Of TypeSymbol) = TypeSymbol.EmptyTypeSymbolsList
            Assert.Equal(0, typeSymbolList.Count)
            Assert.Equal(GetType(TypeSymbol).MakeArrayType().FullName, typeSymbolList.GetType.ToString)

            compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation name="Coverage">
    <file name="a.vb">
Public Module M
    Sub SelectCaseExpression()
        Select Case (Function(arg) arg Is Nothing)'BIND:""Function(arg) arg Is Nothing""
        End Select
    End Sub

    Sub Goo1(Of t)(x As t)
    End Sub

    Sub Goo2(Of t)(x As t)
    End Sub

    Sub Goo3(Of t As New)(x As t)
    End Sub

    Sub Goo4(Of t As New)(x As t)
    End Sub

    Function A(Of t)() As Integer
    End Function

    Function B(Of t)() As t
    End Function
End Module

Class C1 
    Sub Goo1(Of t)(x As t)
    End Sub

    Sub Goo2(Of t)(x As t)
    End Sub

    Sub Goo3(Of t As New)(x As t)
    End Sub

    Sub Goo4(Of t As Structure)(x As t)
    End Sub

    Function A(Of t)() As t
    End Function

    Function B(Of t)() As t
    End Function
End Class

Class C2
    Sub Goo1(Of t)(x As t)
    End Sub

    Sub Goo3(Of t As New)(x As t)
    End Sub

    Function A(Of t)() As Integer
    End Function

    Function B(Of t)() As t
    End Function
End Class

    </file>
</compilation>)

            tree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            semanticModel = compilation.GetSemanticModel(tree)
            syntaxNode1 = tree.FindNodeOrTokenByKind(SyntaxKind.SingleLineFunctionLambdaExpression).AsNode()
            semanticSummary = semanticModel.GetSemanticInfoSummary(DirectCast(syntaxNode1, SingleLineLambdaExpressionSyntax))

            Dim methodMember1 As MethodSymbol = Nothing
            Dim methodMember2 As MethodSymbol = Nothing
            Dim methodMember3 As MethodSymbol = Nothing

            'HaveSameSignature / HaveSameSignatureAndConstraintsAndReturnType
            Symbol.HaveSameSignature(methodMember1, methodMember2)
            Symbol.HaveSameSignatureAndConstraintsAndReturnType(methodMember1, methodMember2)

            Dim globalNS = compilation.GlobalNamespace
            methodMember1 = CType(DirectCast(globalNS.GetMembers("M").Single(), NamedTypeSymbol).GetMember("Goo1"), MethodSymbol)
            methodMember2 = CType(DirectCast(globalNS.GetMembers("M").Single(), NamedTypeSymbol).GetMember("Goo2"), MethodSymbol)
            methodMember3 = CType(DirectCast(globalNS.GetMembers("C1").Single(), NamedTypeSymbol).GetMember("Goo1"), MethodSymbol)
            Assert.False(Symbol.HaveSameSignature(methodMember1, methodMember2))
            Assert.True(Symbol.HaveSameSignature(methodMember1, methodMember1))
            Assert.True(Symbol.HaveSameSignature(methodMember1, methodMember3))
            Assert.True(Symbol.HaveSameSignature(CType(DirectCast(globalNS.GetMembers("C2").Single(), NamedTypeSymbol).GetMember("Goo1"), MethodSymbol), methodMember3))

            methodMember2 = CType(DirectCast(globalNS.GetMembers("C1").Single(), NamedTypeSymbol).GetMember("Goo3"), MethodSymbol)
            methodMember3 = CType(DirectCast(globalNS.GetMembers("C2").Single(), NamedTypeSymbol).GetMember("Goo3"), MethodSymbol)
            Assert.False(Symbol.HaveSameSignatureAndConstraintsAndReturnType(methodMember1, methodMember2))
            Assert.True(Symbol.HaveSameSignatureAndConstraintsAndReturnType(methodMember2, methodMember2))
            Assert.True(Symbol.HaveSameSignatureAndConstraintsAndReturnType(methodMember2, methodMember3))

            methodMember1 = CType(DirectCast(globalNS.GetMembers("C1").Single(), NamedTypeSymbol).GetMember("Goo3"), MethodSymbol)
            methodMember2 = CType(DirectCast(globalNS.GetMembers("C2").Single(), NamedTypeSymbol).GetMember("Goo3"), MethodSymbol)
            Assert.True(Symbol.HaveSameSignatureAndConstraintsAndReturnType(methodMember1, methodMember2))

            methodMember1 = CType(DirectCast(globalNS.GetMembers("M").Single(), NamedTypeSymbol).GetMember("Goo4"), MethodSymbol)
            methodMember3 = CType(DirectCast(globalNS.GetMembers("C1").Single(), NamedTypeSymbol).GetMember("Goo4"), MethodSymbol)
            Assert.False(Symbol.HaveSameSignatureAndConstraintsAndReturnType(methodMember1, methodMember3))

            methodMember1 = CType(DirectCast(globalNS.GetMembers("M").Single(), NamedTypeSymbol).GetMember("A"), MethodSymbol)
            methodMember3 = CType(DirectCast(globalNS.GetMembers("C1").Single(), NamedTypeSymbol).GetMember("A"), MethodSymbol)
            Assert.False(Symbol.HaveSameSignatureAndConstraintsAndReturnType(methodMember1, methodMember3))

            methodMember2 = CType(DirectCast(globalNS.GetMembers("C1").Single(), NamedTypeSymbol).GetMember("A"), MethodSymbol)
            Assert.True(Symbol.HaveSameSignatureAndConstraintsAndReturnType(methodMember3, methodMember2))

            methodMember1 = CType(DirectCast(globalNS.GetMembers("C1").Single(), NamedTypeSymbol).GetMember("B"), MethodSymbol)
            methodMember3 = CType(DirectCast(globalNS.GetMembers("C2").Single(), NamedTypeSymbol).GetMember("B"), MethodSymbol)
            Assert.True(Symbol.HaveSameSignatureAndConstraintsAndReturnType(methodMember1, methodMember3))
        End Sub

        <WorkItem(791793, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/791793")>
        <Fact>
        Public Sub SpeculateAboutParamElementOnField()
            Dim source =
<compilation name="xmlAndQueries">
    <file name="sam.vb"><![CDATA[
Class C
    
    ''' <param name='X'/>
    Public F As Integer
End Class
    ]]></file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(source, parseOptions:=New VisualBasicParseOptions(documentationMode:=DocumentationMode.Diagnose))
            comp.AssertTheseDiagnostics(<expected><![CDATA[
BC42306: XML comment tag 'param' is not permitted on a 'variable' language element.
    ''' <param name='X'/>
        ~~~~~~~~~~~~~~~~~
]]></expected>)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim position = tree.ToString().IndexOf("X"c)
            Dim paramName = DirectCast(SyntaxFactory.ParseExpression("Y"), IdentifierNameSyntax)

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = model.TryGetSpeculativeSemanticModel(position, paramName, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim info = speculativeModel.GetSymbolInfo(paramName)
            Assert.Null(info.Symbol)
            Assert.Equal(CandidateReason.None, info.CandidateReason)
            Assert.Equal(0, info.CandidateSymbols.Length)
        End Sub

        <Fact()>
        Public Sub ExpressionInQueryInXml()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                <compilation name="xmlAndQueries">
                    <file name="sam.vb"><![CDATA[
                        Class C
    Public Function ToXml(errors As IEnumerable(Of Diagnostic)) As XElement
        Return <errors><%= From e In errors
                           Select <error id=<%= e.Code %> /> 
                        %>
               </errors>
    End Function
                           End Class
                    ]]></file>
                </compilation>, references:=XmlReferences)
            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "sam.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim nodes = From n In tree.GetCompilationUnitRoot().DescendantNodes() Where n.Kind = SyntaxKind.IdentifierName Select CType(n, IdentifierNameSyntax)
            Dim enode = nodes.First(Function(n) n.ToString() = "e")
            Dim symbol = semanticModel.GetSymbolInfo(enode).Symbol
            Assert.NotNull(symbol)
            Assert.Equal(symbol.Name, "e")

        End Sub

        <Fact()>
        Public Sub PropertyReturnValueVariable()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
                <compilation name="xmlAndQueries">
                    <file name="sam.vb"><![CDATA[
Imports System

Class Program
    Shared Sub Main()
    End Sub

    Shared Property P As Integer
        Get
            P = 1
            Exit Property
        End Get
        Set(ByVal value As Integer)
            P = 1
            Exit Property
        End Set
    End Property
End Class
                    ]]></file>
                </compilation>, references:=XmlReferences)
            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "sam.vb").Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim assignments = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of AssignmentStatementSyntax)()
            Assert.Equal(2, assignments.Count)

            Const propertyName As String = "P"

            Dim pInGetter = assignments.First().Left
            Assert.Equal(propertyName, pInGetter.ToString())

            Dim getterSymbol = model.GetSymbolInfo(pInGetter).Symbol
            Assert.NotNull(getterSymbol)
            Assert.Equal(propertyName, getterSymbol.Name)
            Assert.Equal(SymbolKind.Local, getterSymbol.Kind)
            Assert.True(DirectCast(getterSymbol, LocalSymbol).IsFunctionValue)

            Dim pInSetter = assignments.Last().Left
            Assert.Equal(propertyName, pInSetter.ToString())

            Dim setterSymbol = model.GetSymbolInfo(pInSetter).Symbol
            Assert.NotNull(setterSymbol)
            Assert.Equal(propertyName, setterSymbol.Name)
            Assert.Equal(SymbolKind.Property, setterSymbol.Kind)
        End Sub

        <Fact>
        <WorkItem(654753, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/654753")>
        Public Sub Repro654753()
            Dim source =
                <compilation>
                    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Public Class C
    Private ReadOnly Instance As New C()

    Function M(d As IDisposable) As Boolean
        Using (D)
            Dim any As Boolean = Me.Instance.GetList().OfType(Of D)().Any()
            Return any
        End Using
    End Function

    Function GetList() As IEnumerable(Of C)
        Return Nothing
    End Function

End Class

Public Class D
    Inherits C
End Class
                    ]]></file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40AndReferences(source, {SystemCoreRef})
            comp.VerifyDiagnostics()

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim position = source.Value.IndexOf("Me", StringComparison.Ordinal)
            Dim statement = tree.GetRoot().DescendantNodes().OfType(Of LocalDeclarationStatementSyntax).Single()
            Dim newSyntax = SyntaxFactory.ParseExpression("Instance.GetList().OfType(Of D)().Any()")
            Dim newStatement = statement.ReplaceNode(statement.Declarators(0).Initializer.Value, newSyntax)
            newSyntax = newStatement.Declarators(0).Initializer.Value

            Dim speculativeModel As SemanticModel = Nothing
            Dim success = model.TryGetSpeculativeSemanticModel(position, newStatement, speculativeModel)
            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim newSyntaxMemberAccess = newSyntax.DescendantNodesAndSelf().OfType(Of MemberAccessExpressionSyntax)().
                Single(Function(e) e.ToString() = "Instance.GetList().OfType(Of D)")
            speculativeModel.GetTypeInfo(newSyntaxMemberAccess)
        End Sub

        <Fact>
        Public Sub Test_SemanticLanguage_VB()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
            Imports System
            Friend Module Program
                Sub Main()
                    Dim o2 As Object = "E"
                End Sub
            End Module
                ]]></file>
</compilation>, Nothing, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Off))

            Dim semanticModel = CompilationUtils.GetSemanticModel(compilation, "a.vb")
            Assert.Equal("Visual Basic", semanticModel.Language)
        End Sub

        <Fact>
        Public Sub DiagnosticsInStages()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
            Class Test
                Dim obj01 As Object
                Sub T(obj02 As Test)
                    obj02 = obj01
                End Sub
                Dim b As BindingError
                parse errrrrror
            End Class
                ]]></file>
</compilation>, Nothing, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))

            Dim semanticModel = CompilationUtils.GetSemanticModel(compilation, "a.vb")
            Dim errs = semanticModel.GetDiagnostics()
            Assert.Equal(5, errs.Length())
            errs = semanticModel.GetDeclarationDiagnostics()
            Assert.Equal(2, errs.Length())
            errs = semanticModel.GetMethodBodyDiagnostics()
            Assert.Equal(1, errs.Length())
            Dim treeErrs = compilation.GetParseDiagnostics()
            Assert.Equal(2, treeErrs.Length())
        End Sub

        <Fact()>
        <WorkItem(10211, "https://github.com/dotnet/roslyn/issues/10211")>
        Public Sub GetDependenceChainRegression_10211_working()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
                                Public Class Parent
                                End Class

                                Public Class Child
                                    Inherits Parent
                                End Class
                            ]]></file>
                         </compilation>

            Dim compilation = CreateEmptyCompilation(source)
            Dim semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees(0))

            ' Ensuring that this doesn't throw
            semanticModel.GetMethodBodyDiagnostics()
        End Sub

        <Fact()>
        <WorkItem(10211, "https://github.com/dotnet/roslyn/issues/10211")>
        Public Sub GetDependenceChainRegression_10211()
            Dim source = <compilation>
                             <file name="a.vb"><![CDATA[
                                Public Class Child
                                    Inherits Parent
                                End Class

                                Public Class Parent
                                End Class
                            ]]></file>
                         </compilation>

            Dim compilation = CreateEmptyCompilation(source)
            Dim semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees(0))

            ' Ensuring that this doesn't throw
            semanticModel.GetMethodBodyDiagnostics()
        End Sub

        <WorkItem(859721, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/859721")>
        <Fact()>
        Public Sub TestMethodBodyDiagnostics()
            ' Even with a root namespace, we should still have these diagnostics with or without root namespace specified
            Dim sourceExplicitGlobalNamespace = <compilation>
                                                    <file name="a.vb"><![CDATA[
            Namespace Global
              Class C
                Sub S()
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.
                End Sub
              End Class
            End Namespace
                            ]]></file>
                                                </compilation>

            Dim ExpectedErrors = <expected>BC42024: Unused local variable: '_A'.
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.
                      ~~
BC30002: Type 'A' is not defined.
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.
                            ~
</expected>

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceExplicitGlobalNamespace, Nothing, TestOptions.ReleaseDll)
            Dim semanticModel = GetSemanticModel(compilation, "a.vb")
            Dim errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors)

            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceExplicitGlobalNamespace, Nothing, TestOptions.ReleaseDll.WithRootNamespace("ClassLibrary1"))
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors)

            ' No namespace declared in source code
            Dim sourceNoNamespaceSpecified = <compilation>
                                                 <file name="a.vb"><![CDATA[
              Class C
                Sub S()
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.
                End Sub
              End Class
                            ]]></file>
                                             </compilation>
            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceNoNamespaceSpecified, Nothing)
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors)

            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceNoNamespaceSpecified, Nothing, TestOptions.ReleaseDll.WithRootNamespace("ClassLibrary1"))
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors)

            ' Even with an escaped global namespace  
            Dim sourceEscapedGlobal = <compilation>
                                          <file name="a.vb"><![CDATA[
            Namespace [Global]
              Class C
                Sub S()
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.
                End Sub
              End Class
            End Namespace
                            ]]></file>
                                      </compilation>

            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceEscapedGlobal, Nothing)
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors)

            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceEscapedGlobal, Nothing, TestOptions.ReleaseDll.WithRootNamespace("ClassLibrary1"))
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors)

            'Global namespace as part of namespace specified but no match on root namespace
            Dim sourceWithGlobalAsStartOfNamespace = <compilation>
                                                         <file name="a.vb"><![CDATA[
            Namespace Global.Goo
              Class C
                Sub S()
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.

                End Sub
              End Class
            End Namespace
                            ]]></file>
                                                     </compilation>

            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceWithGlobalAsStartOfNamespace, Nothing)
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors)

            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceWithGlobalAsStartOfNamespace, Nothing, TestOptions.ReleaseDll.WithRootNamespace("ClassLibrary1"))
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors)

            'namespace starting with a string Global but not specifically Global.
            Dim sourceWithANameStartingGlobal = <compilation>
                                                    <file name="a.vb"><![CDATA[
            Namespace GlobalGoo
              Class C
                Sub S()
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.

                End Sub
              End Class
            End Namespace
                            ]]></file>
                                                </compilation>
            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceWithANameStartingGlobal, Nothing)
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors)

            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceWithANameStartingGlobal, Nothing, TestOptions.ReleaseDll.WithRootNamespace("ClassLibrary1"))
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors)

            'Two namespaces in same source file - global is 1st namespace
            Dim sourceWithGlobalAndMultipleNS1 = <compilation>
                                                     <file name="a.vb"><![CDATA[
            Namespace Global
            Class C
                Sub S()
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.
                End Sub
              End Class
            End Namespace

            Namespace NS2
              Class C
                Sub S()
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.
                End Sub
              End Class
            End Namespace
                            ]]></file>
                                                 </compilation>

            Dim ExpectedErrors2 = <Expected>BC42024: Unused local variable: '_A'.
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.
                      ~~
BC30002: Type 'A' is not defined.
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.
                            ~
BC42024: Unused local variable: '_A'.
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.
                      ~~
BC30002: Type 'A' is not defined.
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.
                            ~
</Expected>
            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceWithGlobalAndMultipleNS1, Nothing)
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors2)

            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceWithGlobalAndMultipleNS1, Nothing, TestOptions.ReleaseDll.WithRootNamespace("ClassLibrary1"))
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors2)

            'Two namespaces in same source file - global is 2nd namespace
            Dim sourceWithGlobalAndMultipleNS2 = <compilation>
                                                     <file name="a.vb"><![CDATA[                                                                      
            Namespace NS1
            Class C
                Sub S()
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.

                End Sub
              End Class
            End Namespace

            Namespace Global
              Class C
                Sub S()
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.

                End Sub
              End Class
            End Namespace
                            ]]></file>
                                                 </compilation>
            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceWithGlobalAndMultipleNS2, Nothing)
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors2)

            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceWithGlobalAndMultipleNS2, Nothing, TestOptions.ReleaseDll.WithRootNamespace("ClassLibrary1"))
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors2)

            'Namespace starting Global.xxxx with xxxx matching the rootnamespace
            Dim sourceWithGlobalCombinedNamespace = <compilation>
                                                        <file name="a.vb"><![CDATA[
            Namespace Global.Goo
            Class C
                Sub S()
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.

                End Sub
              End Class
            End Namespace

            Namespace NS2
              Class C
                Sub S()
                  Dim _A As A   ' error BC30002: Type 'A' is not defined.

                End Sub
              End Class
            End Namespace
                            ]]></file>
                                                    </compilation>
            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceWithGlobalCombinedNamespace, Nothing)
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors2)

            compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(sourceWithGlobalCombinedNamespace, Nothing, TestOptions.ReleaseDll.WithRootNamespace("Goo"))
            semanticModel = GetSemanticModel(compilation, "a.vb")
            errs = semanticModel.GetMethodBodyDiagnostics()
            CompilationUtils.AssertTheseDiagnostics(errs, ExpectedErrors2)
        End Sub

        <Fact>
        Public Sub PartialMethodImplementationDiagnostics()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Partial Class MyPartialClass
    Partial Private Sub MyPartialMethod(t As MyUndefinedType)

    End Sub
End Class
]]></file>
    <file name="b.vb"><![CDATA[
Partial Class MyPartialClass
    Private Sub MyPartialMethod(t As MyUndefinedType)
        Dim c = New MyUndefinedType(23, True)
    End Sub
End Class
]]></file>
</compilation>, Nothing, TestOptions.ReleaseDll.WithOptionStrict(OptionStrict.On))

            Dim semanticModel = CompilationUtils.GetSemanticModel(compilation, "b.vb")
            Dim errs = semanticModel.GetDiagnostics()
            Assert.Equal(2, errs.Length())
            errs = semanticModel.GetDeclarationDiagnostics()
            Assert.Equal(1, errs.Length())
            errs = semanticModel.GetMethodBodyDiagnostics()
            Assert.Equal(1, errs.Length())
            Dim treeErrs = compilation.GetParseDiagnostics()
            Assert.Equal(0, treeErrs.Length())
        End Sub
#End Region

        <Fact, WorkItem(1146124, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1146124")>
        Public Sub GetTypeInfoForXmlStringInCref()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb"><![CDATA[
Module Program
    ''' <summary>
    ''' <see cref=""/>
    ''' </summary>
    Sub Main(args As String())

    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot
            Dim xmlString = root.DescendantNodes(descendIntoTrivia:=True).OfType(Of XmlStringSyntax).Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)

            Dim typelInfo = semanticModel.GetTypeInfo(xmlString)
            Assert.Null(typelInfo.Type)
        End Sub

        <WorkItem(1104539, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1104539")>
        <Fact()>
        Public Sub GetDiagnosticsWithRootNamespace()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports System.Threading

Module TestModule
    Sub Main()
        DoesntExist()
    End Sub

    <Extension>
    Public Function ToFullWidth(c As Char) As Char
        Return If(IsHalfWidth(c), MakeFullWidth(c), c)
    End Function
End Module
    ]]></file>
    <file name="b.vb"><![CDATA[
Imports Microsoft.VisualBasic.Strings

Namespace Global.Microsoft.CodeAnalysis.VisualBasic

    Partial Public Class SyntaxFacts

        Friend Shared Function MakeFullWidth(c As Char) As Char
            Return c
        End Function

        Friend Shared Function IsHalfWidth(c As Char) As Boolean
            Return c >= ChrW(&H21S) AndAlso c <= ChrW(&H7ES)
        End Function
    End Class
End Namespace
    ]]></file>
</compilation>, {SystemCoreRef}, options:=TestOptions.DebugDll.WithRootNamespace("Microsoft.CodeAnalysis.VisualBasic.UnitTests"))

            Dim semanticModel = CompilationUtils.GetSemanticModel(compilation, "a.vb")

            semanticModel.GetDiagnostics().AssertTheseDiagnostics(<errors>
BC50001: Unused import statement.
Imports System.Threading
~~~~~~~~~~~~~~~~~~~~~~~~
BC30451: 'DoesntExist' is not declared. It may be inaccessible due to its protection level.
        DoesntExist()
        ~~~~~~~~~~~
                                                                  </errors>, suppressInfos:=False)
        End Sub

        <Fact, WorkItem(976, "https://github.com/dotnet/roslyn/issues/976")>
        Public Sub ConstantValueOfInterpolatedString()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="GetSemanticInfo">
    <file name="a.vb"><![CDATA[
Module Program
    ''' <summary>
    ''' <see cref=""/>
    ''' </summary>
    Sub Main(args As String())
        System.Console.WriteLine($""Hello, world!"");
        System.Console.WriteLine($""{DateTime.Now.ToString()}.{args(0)}"");
    End Sub
End Module
    ]]></file>
</compilation>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim root = tree.GetCompilationUnitRoot
            Dim model = compilation.GetSemanticModel(tree)
            For Each interp In root.DescendantNodes().OfType(Of InterpolatedStringExpressionSyntax)
                Assert.False(model.GetConstantValue(interp).HasValue)
            Next
        End Sub

    End Class
End Namespace
