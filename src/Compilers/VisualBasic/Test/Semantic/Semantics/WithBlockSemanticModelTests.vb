' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class WithBlockSemanticModelTests
        Inherits FlowTestBase

#Region "Symbol / Type Info"

        <Fact>
        Public Sub WithAliasedStaticField()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
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
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
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
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
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
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(
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
</compilation>, {SystemCoreRef})

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

    End Class

End Namespace
