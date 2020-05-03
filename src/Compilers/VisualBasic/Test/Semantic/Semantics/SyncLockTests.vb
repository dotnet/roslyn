' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Public Class SyncLockTests
        Inherits FlowTestBase

#Region "ControlFlowPass and DataflowAnalysis"

        <Fact()>
        Public Sub SyncLockInSelect()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
<compilation name="SyncLockInSelect">
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        Select ""
            Case "a"
                [|
                SyncLock New Object()
                    GoTo lab1
                End SyncLock
                |]
        End Select
lab1:
    End Sub
End Class
    </file>
</compilation>)
            Dim analysisControlflow = analysis.Item1
            Dim analysisDataflow = analysis.Item2
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.WrittenOutside))

            Assert.Equal(0, analysisControlflow.EntryPoints.Count())
            Assert.Equal(1, analysisControlflow.ExitPoints.Count())
        End Sub

        <Fact()>
        Public Sub UnreachableCode()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
<compilation name="UnreachableCode">
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        [|Dim x1 As Object
        SyncLock x1
            Return
        End SyncLock|]
        System.Threading.Monitor.Exit(x1)
    End Sub
End Class
    </file>
</compilation>)

            Dim analysisControlflow = analysis.Item1
            Dim analysisDataflow = analysis.Item2
            Assert.Equal("x1", GetSymbolNamesJoined(analysisDataflow.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.DataFlowsOut))
            Assert.Equal("x1", GetSymbolNamesJoined(analysisDataflow.ReadInside))
            Assert.Equal("x1", GetSymbolNamesJoined(analysisDataflow.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.WrittenOutside))

            Assert.Equal(0, analysisControlflow.EntryPoints.Count())
            Assert.Equal(1, analysisControlflow.ExitPoints.Count())
        End Sub

        <Fact()>
        Public Sub AssignmentInSyncLock()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
<compilation name="AssignmentInSyncLock">
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        Dim myLock As Object
        [|SyncLock Nothing
            myLock = New Object()
        End SyncLock|]
        System.Console.WriteLine(myLock)
    End Sub
End Class
    </file>
</compilation>)
            Dim analysisControlflow = analysis.Item1
            Dim analysisDataflow = analysis.Item2
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.VariablesDeclared))
            Assert.Equal("myLock", GetSymbolNamesJoined(analysisDataflow.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.DataFlowsIn))
            Assert.Equal("myLock", GetSymbolNamesJoined(analysisDataflow.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.ReadInside))
            Assert.Equal("myLock", GetSymbolNamesJoined(analysisDataflow.ReadOutside))
            Assert.Equal("myLock", GetSymbolNamesJoined(analysisDataflow.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.WrittenOutside))

            Assert.Equal(0, analysisControlflow.EntryPoints.Count())
            Assert.Equal(0, analysisControlflow.ExitPoints.Count())
        End Sub

        <Fact()>
        Public Sub SyncLock_AssignmentInInLambda()
            Dim analysis = CompileAndAnalyzeControlAndDataFlow(
<compilation name="SyncLock_AssignmentInInLambda">
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        Dim myLock As Object
[|
        SyncLock Sub()
                     myLock = New Object()
                 End Sub
        End SyncLock|]
        System.Console.WriteLine(myLock)
    End Sub
End Class
    </file>
</compilation>)
            Dim analysisControlflow = analysis.Item1
            Dim analysisDataflow = analysis.Item2
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.DataFlowsIn))
            Assert.Equal("myLock", GetSymbolNamesJoined(analysisDataflow.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.ReadInside))
            Assert.Equal("myLock", GetSymbolNamesJoined(analysisDataflow.ReadOutside))
            Assert.Equal("myLock", GetSymbolNamesJoined(analysisDataflow.WrittenInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.WrittenOutside))

            Assert.Equal(0, analysisControlflow.EntryPoints.Count())
            Assert.Equal(0, analysisControlflow.ExitPoints.Count())
        End Sub

        <Fact()>
        Public Sub NestedSyncLock()
            Dim analysisDataflow = CompileAndAnalyzeDataFlow(
<compilation name="NestedSyncLock">
    <file name="a.vb">
Public Class Program
    Public Sub goo()
        Dim syncroot As Object = New Object
        SyncLock syncroot
            [|SyncLock syncroot.ToString()
                GoTo lab1
                syncroot = Nothing
            End SyncLock|]
lab1:
        End SyncLock
        System.Threading.Monitor.Enter(syncroot)
    End Sub
End Class
    </file>
</compilation>)
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.AlwaysAssigned))
            Assert.Equal("syncroot", GetSymbolNamesJoined(analysisDataflow.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.DataFlowsOut))
            Assert.Equal("syncroot", GetSymbolNamesJoined(analysisDataflow.ReadInside))
            Assert.Equal("syncroot", GetSymbolNamesJoined(analysisDataflow.ReadOutside))
            Assert.Equal("syncroot", GetSymbolNamesJoined(analysisDataflow.WrittenInside))
            Assert.Equal("Me, syncroot", GetSymbolNamesJoined(analysisDataflow.WrittenOutside))

        End Sub

        <Fact()>
        Public Sub DataflowOfInnerStatement()
            Dim analysisDataflow = CompileAndAnalyzeDataFlow(
<compilation name="DataflowOfInnerStatement">
    <file name="a.vb">
Public Class Program
    Public Sub goo()
        Dim syncroot As Object = New Object
        SyncLock syncroot.ToString()
            [|Dim x As Integer
            Return|]
        End SyncLock
        System.Threading.Monitor.Enter(syncroot)
    End Sub
End Class
    </file>
</compilation>)
            Assert.Equal("x", GetSymbolNamesJoined(analysisDataflow.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.DataFlowsOut))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.ReadInside))
            Assert.Equal("syncroot", GetSymbolNamesJoined(analysisDataflow.ReadOutside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(analysisDataflow.WrittenInside))
            Assert.Equal("Me, syncroot", GetSymbolNamesJoined(analysisDataflow.WrittenOutside))

        End Sub

#End Region

#Region "Semantic API"

        <WorkItem(545364, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545364")>
        <Fact()>
        Public Sub SyncLockLambda()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation name="SyncLockLambda">
    <file name="a.vb">
Option Infer On
Imports System
Class Program
    Shared Sub Main()
        Dim myLock As Object
        SyncLock Sub()
                     myLock = New Object()
                 End Sub
        End SyncLock
    End Sub
End Class
    </file>
</compilation>)
            Dim expression = GetExpressionFromSyncLock(compilation)
            Dim semanticSummary = GetSemanticInfoSummary(compilation, expression)
            Assert.Null(semanticSummary.Type)
            Assert.Equal(TypeKind.Delegate, semanticSummary.ConvertedType.TypeKind)
            Assert.Equal("Sub <generated method>()", semanticSummary.ConvertedType.ToDisplayString())
            Assert.Equal(ConversionKind.Widening Or ConversionKind.Lambda, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Sub ()", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Method, semanticSummary.Symbol.Kind)
            Assert.Equal(True, semanticSummary.Symbol.IsLambdaMethod)

            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)
            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub SyncLockQuery()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="SyncLockQuery">
    <file name="a.vb">
Option Strict On
Imports System.Linq
Class Program
    Shared Sub Main()
        SyncLock From w In From x In New Integer() {1, 2, 3}
                           From y In New Char() {"a"c, "b"c}
                           Let bOdd = (x And 1) = 1
                           Where
                               bOdd Where y > "a"c Let z = x.ToString() &amp; y.ToString()
        End SyncLock
    End Sub
End Class
    </file>
</compilation>, {SystemCoreRef})

            Dim expression = GetExpressionFromSyncLock(compilation)
            Dim semanticSummary = GetSemanticInfoSummary(compilation, expression)

            Assert.NotNull(semanticSummary.Type)
            Assert.NotNull(semanticSummary.ConvertedType)
            Assert.True(semanticSummary.Type.IsReferenceType)
            Assert.Equal("System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As Integer, Key y As Char, Key bOdd As Boolean, Key z As String>)", semanticSummary.ConvertedType.ToDisplayString())
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.False(semanticSummary.ConstantValue.HasValue)
        End Sub

        <Fact()>
        Public Sub SyncLockGenericType()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="SyncLockGenericType">
    <file name="a.vb">
Option Infer ON
Class Program
    Private Shared Sub Goo(Of T As D)(x As T)
        SyncLock x
        End SyncLock
    End Sub
End Class
Class D
End Class
    </file>
</compilation>)

            Dim expression = GetExpressionFromSyncLock(compilation)
            Dim semanticSummary = GetSemanticInfoSummary(compilation, expression)

            Assert.Equal("T", semanticSummary.Type.ToDisplayString())
            Assert.True(semanticSummary.ConvertedType.IsReferenceType)
            Assert.Equal("T", semanticSummary.ConvertedType.ToDisplayString())
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("x As T", semanticSummary.Symbol.ToDisplayString())
            Assert.Equal(0, semanticSummary.CandidateSymbols.Length)

            Assert.Null(semanticSummary.Alias)
            Assert.Equal(0, semanticSummary.MemberGroup.Length)
            Assert.False(semanticSummary.ConstantValue.HasValue)

        End Sub

        <Fact()>
        Public Sub SyncLockAnonymous()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="SyncLockAnonymous">
    <file name="a.vb">
Module M1
    Sub Main()
        SyncLock New With {Key .p1 = 10.0}
        End SyncLock
    End Sub
End Module
    </file>
</compilation>)
            Dim expression = GetExpressionFromSyncLock(compilation)
            Dim semanticSummary = GetSemanticInfoSummary(compilation, expression)

            Assert.True(semanticSummary.Type.IsReferenceType)
            Assert.Equal("<anonymous type: Key p1 As Double>", semanticSummary.Type.ToDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.True(semanticSummary.ConvertedType.IsReferenceType)
            Assert.Equal("<anonymous type: Key p1 As Double>", semanticSummary.ConvertedType.ToDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Public Sub New(p1 As Double)", semanticSummary.Symbol.ToDisplayString())

        End Sub

        <Fact()>
        Public Sub SyncLockCreateObject()

            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="SyncLockCreateObject">
    <file name="a.vb">
Module M1
    Sub Main()
        SyncLock New object()
        End SyncLock
    End Sub
End Module
    </file>
</compilation>)
            Dim expression = GetExpressionFromSyncLock(compilation)
            Dim semanticSummary = GetSemanticInfoSummary(compilation, expression)

            Assert.True(semanticSummary.Type.IsReferenceType)
            Assert.Equal("Object", semanticSummary.Type.ToDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Dim symbol = compilation.GetTypeByMetadataName("System.Object")
            Assert.Equal(symbol, semanticSummary.Type)
            Assert.True(semanticSummary.ConvertedType.IsReferenceType)
            Assert.Equal("Object", semanticSummary.ConvertedType.ToDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Public Overloads Sub New()", semanticSummary.Symbol.ToDisplayString())

        End Sub

        <Fact()>
        Public Sub SimpleSyncLockNothing()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="SimpleSyncLockNothing">
    <file name="a.vb">
Option Strict ON
Imports System
Class Program
    Shared Sub Main()
        SyncLock Nothing
            Exit Sub
        End SyncLock
    End Sub
End Class
    </file>
</compilation>)
            Dim expression = GetExpressionFromSyncLock(compilation)
            Dim semanticSummary = GetSemanticInfoSummary(compilation, expression)

            Assert.Null(semanticSummary.Type)
            Assert.Equal("Object", semanticSummary.ConvertedType.ToDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.ConvertedType.TypeKind)
            Dim symbol = compilation.GetTypeByMetadataName("System.Object")
            Assert.Equal(symbol, semanticSummary.ConvertedType)
            Assert.Equal(ConversionKind.WideningNothingLiteral, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
        End Sub

        <Fact()>
        Public Sub SimpleSyncLockDelegate()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="SimpleSyncLockDelegate">
    <file name="a.vb">
Delegate Sub D(p1 As Integer)
Class Program
    Public Shared Sub Main(args As String())
        SyncLock New D(AddressOf PM)
        End SyncLock
    End Sub
    Private Shared Sub PM(p1 As Integer)
    End Sub
End Class
    </file>
</compilation>)

            Dim expression = GetExpressionFromSyncLock(compilation)
            Dim semanticSummary = GetSemanticInfoSummary(compilation, expression)

            Assert.True(semanticSummary.Type.IsReferenceType)
            Assert.Equal("D", semanticSummary.Type.ToDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticSummary.Type.TypeKind)
            Dim symbol = compilation.GetTypeByMetadataName("D")
            Assert.Equal(symbol, semanticSummary.Type)
            Assert.True(semanticSummary.ConvertedType.IsReferenceType)
            Assert.Equal("D", semanticSummary.ConvertedType.ToDisplayString())
            Assert.Equal(TypeKind.Delegate, semanticSummary.Type.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Null(semanticSummary.Symbol)
        End Sub

        <Fact()>
        Public Sub SyncLockMe()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntimeAndReferences(
<compilation name="SyncLockMe">
    <file name="a.vb">
Class Program
    Sub goo()
        SyncLock Me
        End SyncLock
    End Sub
End Class
    </file>
</compilation>)

            Dim expression = GetExpressionFromSyncLock(compilation)
            Dim semanticSummary = GetSemanticInfoSummary(compilation, expression)

            Assert.True(semanticSummary.Type.IsReferenceType)
            Assert.Equal("Program", semanticSummary.Type.ToDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Dim symbol = compilation.GetTypeByMetadataName("Program")
            Assert.Equal(symbol, semanticSummary.Type)
            Assert.True(semanticSummary.ConvertedType.IsReferenceType)
            Assert.Equal("Program", semanticSummary.ConvertedType.ToDisplayString())
            Assert.Equal(TypeKind.Class, semanticSummary.Type.TypeKind)
            Assert.Equal(ConversionKind.Identity, semanticSummary.ImplicitConversion.Kind)

            Assert.Equal("Me As Program", semanticSummary.Symbol.ToTestDisplayString())
            Assert.Equal(SymbolKind.Parameter, semanticSummary.Symbol.Kind)

        End Sub
#End Region

#Region "Help Method"

        Private Function GetExpressionFromSyncLock(Compilation As VisualBasicCompilation, Optional which As Integer = 1) As ExpressionSyntax
            Dim tree = Compilation.SyntaxTrees.[Single]()
            Dim model = Compilation.GetSemanticModel(tree)
            Dim SyncLockBlock = tree.GetCompilationUnitRoot().DescendantNodes().OfType(Of SyncLockStatementSyntax)().ToList()
            Return SyncLockBlock(which - 1).Expression
        End Function

#End Region
    End Class
End Namespace
