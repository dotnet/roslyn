' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Basic.Reference.Assemblies
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class WithBlockSemanticModelTests
        Inherits FlowTestBase

#Region "Symbol / Type Info"

        <Fact>
        Public Sub WithAliasedStaticField()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Imports Alias1 = ClassWithField
Class ClassWithField
    Public Shared field1 As String = "a"
End Class
Module WithAliasedStaticField
    Sub Main()
        With Alias1.field1 'BIND:"Alias1.field1"
            Dim newString = .Replace("a", "b")
        End With
    End Sub
End Module
    </file>
</compilation>)
            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)
            Dim withExpression = DirectCast(tree.GetCompilationUnitRoot().DescendantNodes().Where(Function(n) n.Kind = SyntaxKind.SimpleMemberAccessExpression).First(), MemberAccessExpressionSyntax)

            Assert.Equal("Alias1", model.GetAliasInfo(DirectCast(withExpression.Expression, IdentifierNameSyntax)).ToDisplayString())
            Assert.False(model.GetConstantValue(withExpression).HasValue)
            Dim typeInfo = model.GetTypeInfo(withExpression)
            Assert.Equal("String", typeInfo.Type.ToDisplayString())
            Assert.Equal("String", typeInfo.ConvertedType.ToDisplayString())
            Dim conv = model.GetConversion(withExpression)
            Assert.Equal(ConversionKind.Identity, conv.Kind)
            Dim symbolInfo = model.GetSymbolInfo(withExpression)
            Assert.Equal("Public Shared field1 As String", symbolInfo.Symbol.ToDisplayString())
            Assert.Equal(SymbolKind.Field, symbolInfo.Symbol.Kind)
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length)
            Assert.Equal(0, model.GetMemberGroup(withExpression).Length)
        End Sub

        <Fact>
        Public Sub WithDeclaresAnonymousLocalSymbolAndTypeInfo()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Module WithDeclaresAnonymousLocalSymbolAndTypeInfo
    Sub Main()
        With New With {.A = 1, .B = "2"} 'BIND:"New With {.A = 1, .B = "2"}"
            .A = .B
        End With
    End Sub
End Module
    </file>
</compilation>)
            Dim semanticInfo = CompilationUtils.GetSemanticInfoSummary(Of AnonymousObjectCreationExpressionSyntax)(compilation, "a.vb")

            Assert.Null(semanticInfo.Alias)
            Assert.False(semanticInfo.ConstantValue.HasValue)
            Assert.Equal("<anonymous type: A As Integer, B As String>", semanticInfo.Type.ToDisplayString())
            Assert.Equal("<anonymous type: A As Integer, B As String>", semanticInfo.ConvertedType.ToDisplayString())
            Assert.Equal(ConversionKind.Identity, semanticInfo.ImplicitConversion.Kind)
            Assert.Equal("Public Sub New(A As Integer, B As String)", semanticInfo.Symbol.ToDisplayString()) ' should get constructor for anonymous type
            Assert.Equal(SymbolKind.Method, semanticInfo.Symbol.Kind)
            Assert.Equal(0, semanticInfo.CandidateSymbols.Length)
            Assert.Equal(0, semanticInfo.MemberGroup.Length)
        End Sub

        <Fact(), WorkItem(544083, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544083")>
        Public Sub WithSpeculativeSymbolInfo()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class C1
    Property Property1 As Integer
    Property Property2 As String
End Class
Module Module1
    Sub Main()
        Dim x As New C1()
        With x
            Dim f = Function() .Property1 'BINDHERE
        End With
    End Sub    
End Module
    </file>
</compilation>)
            Dim semanticModel = GetSemanticModel(compilation, "a.vb")
            Dim position = compilation.SyntaxTrees.Single().ToString().IndexOf("'BINDHERE", StringComparison.Ordinal)

            Dim expr = SyntaxFactory.ParseExpression(".property2")
            Dim speculativeTypeInfo = semanticModel.GetSpeculativeTypeInfo(position, expr, SpeculativeBindingOption.BindAsExpression)
            Assert.Equal("String", speculativeTypeInfo.ConvertedType.ToDisplayString())
            Dim conv = semanticModel.GetSpeculativeConversion(position, expr, SpeculativeBindingOption.BindAsExpression)
            Assert.Equal(ConversionKind.Identity, conv.Kind)
            Assert.Equal("String", speculativeTypeInfo.Type.ToDisplayString())

            Dim speculativeSymbolInfo = semanticModel.GetSpeculativeSymbolInfo(position, SyntaxFactory.ParseExpression(".property2"), SpeculativeBindingOption.BindAsExpression)
            Assert.Equal("Public Property Property2 As String", speculativeSymbolInfo.Symbol.ToDisplayString())
            Assert.Equal(SymbolKind.Property, speculativeSymbolInfo.Symbol.Kind)
        End Sub

#End Region

#Region "FlowAnalysis"

        <Fact>
        Public Sub UseWithVariableInNestedLambda()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
<compilation>
    <file name="a.vb">
Class C1
    Property Property1 As Integer
End Class
Module Module1
    Sub Main()
        Dim x As New C1()
        With x
            Dim f = Function()
                        [|Return .Property1|]
                    End Function
        End With
    End Sub    
End Module
    </file>
</compilation>)
            Dim controlFlowResults = analysis.Item1
            Dim dataFlowResults = analysis.Item2

            Assert.Empty(dataFlowResults.VariablesDeclared)
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Empty(dataFlowResults.DataFlowsIn)
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Empty(dataFlowResults.ReadInside)
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Equal("x, f", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))

            Assert.Empty(controlFlowResults.EntryPoints)
            Assert.False(controlFlowResults.EndPointIsReachable)
            Assert.True(controlFlowResults.StartPointIsReachable)
            Assert.Equal(1, controlFlowResults.ExitPoints.Count)
        End Sub

        <Fact>
        Public Sub WithDeclaresAnonymousLocalDataFlow()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
<compilation>
    <file name="a.vb">
Module WithDeclaresAnonymousLocal
    Sub Main()
        With New With {.A = 1, .B = "2"}
            [|.A = .B|]
        End With
    End Sub
End Module
    </file>
</compilation>)
            Dim controlFlowResults = analysis.Item1
            Dim dataFlowResults = analysis.Item2

            Assert.Empty(dataFlowResults.VariablesDeclared)
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Empty(dataFlowResults.DataFlowsIn) ' assume anonymous locals don't show
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Empty(dataFlowResults.ReadInside) ' assume anonymous locals don't show
            Assert.Empty(dataFlowResults.ReadOutside)
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Empty(dataFlowResults.WrittenOutside) ' assume anonymous locals don't show

            Assert.Empty(controlFlowResults.ExitPoints)
            Assert.Empty(controlFlowResults.EntryPoints)
            Assert.True(controlFlowResults.EndPointIsReachable)
            Assert.True(controlFlowResults.StartPointIsReachable)
        End Sub

        <Fact>
        Public Sub EmptyWith()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
<compilation>
    <file name="a.vb">
Module Module1
    Sub Main()
        Dim x As Object()
        [|With x
        End With|]
    End Sub    
End Module
    </file>
</compilation>)
            Dim controlFlowResults = analysis.Item1
            Dim dataFlowResults = analysis.Item2

            Assert.Empty(dataFlowResults.VariablesDeclared)
            Assert.Empty(dataFlowResults.AlwaysAssigned)
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Empty(dataFlowResults.DataFlowsOut)
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Empty(dataFlowResults.ReadOutside)
            Assert.Empty(dataFlowResults.WrittenInside)
            Assert.Empty(dataFlowResults.WrittenOutside)

            Assert.Empty(controlFlowResults.ExitPoints)
            Assert.Empty(controlFlowResults.EntryPoints)
            Assert.True(controlFlowResults.EndPointIsReachable)
            Assert.True(controlFlowResults.StartPointIsReachable)
        End Sub

#End Region

        <Fact, WorkItem(2662, "https://github.com/dotnet/roslyn/issues/2662")>
        Public Sub Issue2662()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices

Module Program
    Sub Main(args As String())

    End Sub
    Private Sub AddCustomer()
        Dim theCustomer As New Customer

        With theCustomer
            .Name = "Abc"
            .URL = "http://www.microsoft.com/"
            .City = "Redmond"
            .Print(.Name)
        End With
    End Sub

    <Extension()>
    Public Sub Print(ByVal cust As Customer, str As String)
        Console.WriteLine(str)
    End Sub

    Public Class Customer
        Public Property Name As String
        Public Property City As String
        Public Property URL As String

        Public Property Comments As New List(Of String)
    End Class
End Module
    ]]></file>
</compilation>, {Net40.References.SystemCore})

            compilation.AssertNoDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)
            Dim withBlock = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of WithBlockSyntax)().Single()

            Dim name = withBlock.Statements(3).DescendantNodes().OfType(Of IdentifierNameSyntax).Where(Function(i) i.Identifier.ValueText = "Name").Single()
            model.GetAliasInfo(name)

            Dim result2 = model.AnalyzeDataFlow(withBlock, withBlock)
            Assert.True(result2.Succeeded)

            model = compilation.GetSemanticModel(tree)
            model.GetAliasInfo(name)

            Assert.Equal("theCustomer As Program.Customer", model.GetSymbolInfo(withBlock.WithStatement.Expression).Symbol.ToTestDisplayString())
        End Sub

        <Fact>
        <WorkItem(187910, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=187910&_a=edit")>
        Public Sub Bug187910()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="a.vb">
Class ClassWithField
    Public field1 As String = "a"
End Class
Class WithAliasedStaticField
    Sub Test(parameter as ClassWithField)
        With parameter
            System.Console.WriteLine(.field1)
        End With
    End Sub
End Class
    </file>
</compilation>)

            Dim compilationB = CompilationUtils.CreateCompilationWithMscorlib40(
<compilation>
    <file name="b.vb">
Class WithAliasedStaticField1
    Sub Test(parameter as ClassWithField)
        With parameter
            System.Console.WriteLine(.field1)
        End With
    End Sub
End Class
    </file>
</compilation>)

            compilation.AssertTheseDiagnostics()

            Dim treeA = compilation.SyntaxTrees.Single()
            Dim modelA = compilation.GetSemanticModel(treeA)
            Dim parameter = treeA.GetCompilationUnitRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "parameter").First()

            Assert.Equal("Sub WithAliasedStaticField.Test(parameter As ClassWithField)", modelA.GetEnclosingSymbol(parameter.SpanStart).ToTestDisplayString())

            Dim treeB = compilationB.SyntaxTrees.Single()
            Dim withBlockB = treeB.GetCompilationUnitRoot().DescendantNodes().OfType(Of WithBlockSyntax)().Single()

            Dim modelAB As SemanticModel = Nothing
            Assert.True(modelA.TryGetSpeculativeSemanticModel(parameter.Parent.Parent.SpanStart, withBlockB, modelAB))

            Assert.Equal("Sub WithAliasedStaticField.Test(parameter As ClassWithField)", modelAB.GetEnclosingSymbol(withBlockB.WithStatement.Expression.SpanStart).ToTestDisplayString())
        End Sub

        <Fact>
        <WorkItem(10929, "https://github.com/dotnet/roslyn/issues/10929")>
        Public Sub WithTargetAsArgument_01()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Class Base
End Class

Class Derived
    Inherits Base

    Public Function Contains(node As Base) As Boolean
        Return True
    End Function
End Class

Module Ext
    Sub M(vbNode As Derived)
        With vbNode
            If .Contains(vbNode) Then
            End If
        End With
    End Sub
End Module
    </file>
</compilation>)

            compilation.AssertTheseDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "vbNode").ToArray()

            Dim symbolInfo1 = model.GetSymbolInfo(nodes(0))
            Assert.Equal("vbNode As Derived", symbolInfo1.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, symbolInfo1.Symbol.Kind)

            Dim symbolInfo2 = model.GetSymbolInfo(nodes(1))
            Assert.Equal("vbNode As Derived", symbolInfo2.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, symbolInfo2.Symbol.Kind)

            Assert.Same(symbolInfo1.Symbol, symbolInfo2.Symbol)
        End Sub

        <Fact>
        <WorkItem(10929, "https://github.com/dotnet/roslyn/issues/10929")>
        Public Sub WithTargetAsArgument_02()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class Base
End Class

Class Derived
    Inherits Base
End Class

Module Ext
    <System.Runtime.CompilerServices.Extension()>
    Public Function GetCurrent(Of TNode As Base)(root As Base, node As TNode) As TNode
        Return Nothing
    End Function

    Sub M(vbNode As Derived)
        With vbNode
            If .GetCurrent(vbNode) Is Nothing Then
            End If
        End With
    End Sub
End Module
    ]]></file>
</compilation>, additionalRefs:={Net40.References.SystemCore})

            compilation.AssertTheseDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "vbNode").ToArray()

            Dim symbolInfo1 = model.GetSymbolInfo(nodes(0))
            Assert.Equal("vbNode As Derived", symbolInfo1.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, symbolInfo1.Symbol.Kind)

            Dim symbolInfo2 = model.GetSymbolInfo(nodes(1))
            Assert.Equal("vbNode As Derived", symbolInfo2.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, symbolInfo2.Symbol.Kind)

            Assert.Same(symbolInfo1.Symbol, symbolInfo2.Symbol)
        End Sub

        <Fact>
        <WorkItem(10929, "https://github.com/dotnet/roslyn/issues/10929")>
        Public Sub WithTargetAsArgument_03()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class Base
End Class

Class Derived
    Inherits Base

    Public Function Contains(node As Base) As Boolean
        Return True
    End Function
End Class

Module Ext
    <System.Runtime.CompilerServices.Extension()>
    Public Function GetCurrent(Of TNode As Base)(root As Base, node As TNode) As TNode
        Return Nothing
    End Function

    Sub M(vbNode As Derived)
        With vbNode
            If .GetCurrent(vbNode) Is Nothing Then
            End If
            If .Contains(vbNode) Then
            End If
        End With
    End Sub
End Module
    ]]></file>
</compilation>, additionalRefs:={Net40.References.SystemCore})

            compilation.AssertTheseDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "vbNode").ToArray()

            Dim symbolInfo1 = model.GetSymbolInfo(nodes(0))
            Assert.Equal("vbNode As Derived", symbolInfo1.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, symbolInfo1.Symbol.Kind)

            Dim symbolInfo2 = model.GetSymbolInfo(nodes(1))
            Assert.Equal("vbNode As Derived", symbolInfo2.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, symbolInfo2.Symbol.Kind)

            Dim symbolInfo3 = model.GetSymbolInfo(nodes(2))
            Assert.Equal("vbNode As Derived", symbolInfo3.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, symbolInfo3.Symbol.Kind)

            Assert.Same(symbolInfo1.Symbol, symbolInfo2.Symbol)
            Assert.Same(symbolInfo1.Symbol, symbolInfo3.Symbol)
        End Sub

        <Fact>
        <WorkItem(10929, "https://github.com/dotnet/roslyn/issues/10929")>
        Public Sub WithTargetAsArgument_04()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class Base
End Class

Class Derived
    Inherits Base
End Class

Module Ext
    <System.Runtime.CompilerServices.Extension()>
    Public Function GetCurrent(Of TNode As Base)(root As Base, node As TNode) As TNode
        Return Nothing
    End Function

    readonly property vbNode As Derived
        Get
            return nothing
        End Get
    End Property

    Sub M()
        With vbNode
            If .GetCurrent(vbNode) Is Nothing Then
            End If
        End With
    End Sub
End Module
    ]]></file>
</compilation>, additionalRefs:={Net40.References.SystemCore})

            compilation.AssertTheseDiagnostics()

            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)
            Dim nodes = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Where(Function(n) n.Identifier.ValueText = "vbNode").ToArray()

            Dim symbolInfo1 = model.GetSymbolInfo(nodes(0))
            Assert.Equal("ReadOnly Property Ext.vbNode As Derived", symbolInfo1.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, symbolInfo1.Symbol.Kind)

            Dim symbolInfo2 = model.GetSymbolInfo(nodes(1))
            Assert.Equal("ReadOnly Property Ext.vbNode As Derived", symbolInfo2.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Property, symbolInfo2.Symbol.Kind)

            Assert.Same(symbolInfo1.Symbol, symbolInfo2.Symbol)
        End Sub

    End Class

End Namespace
