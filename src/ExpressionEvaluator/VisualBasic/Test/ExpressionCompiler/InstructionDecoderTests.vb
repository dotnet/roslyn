Imports System.Reflection.Metadata.Ecma335
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    '// TODO: ref/out
    '// TODO: constructors
    '// TODO: keyword identifiers
    '// TODO: containing type and parameter types that are nested types
    '// TODO: anonymous methods
    '// TODO: iterator/async lambda
    '// TODO: parameter generic(Of generic)
    '// TODO: generic state machines (generic source method/source type)
    '// TODO: string argument values
    '// TODO: string argument values requiring quotes
    '// TODO: argument flags == names only, types only, values only
    '// TODO: params Argument values
    '// TODO: GetFrameReturnType primitive types
    '// TODO: GetFrameReturnType non-primitive types (nested namespace/class)
    '// TODO: GetFrameReturnType generic(Of non-primitive, nested)
    '// TODO: GetFrameReturnType generic(Of generic)
    '// TODO: GetFrameReturnType generic(Of primitive)
    Public Class InstructionDecoderTests : Inherits ExpressionCompilerTestBase

        <Fact>
        Sub GetNameArgumentCounts()
            Dim source = "
Imports System
Module Module1
    Sub NoArgs()
    End Sub
    Sub OneArg(one As Int32)
    End Sub
    Sub TwoArgs(one As Object, two As Exception)
    End Sub
End Module"

            Assert.Equal(
                "Module1.NoArgs()",
                GetName(source, "Module1.NoArgs", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))

            Assert.Equal(
                "Module1.OneArg(Integer one)",
                GetName(source, "Module1.OneArg", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))

            Assert.Equal(
                "Module1.TwoArgs(Object one, System.Exception two)",
                GetName(source, "Module1.TwoArgs", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))
        End Sub

        <Fact>
        Sub GetNameNullable()
            Dim source = "
Imports System
Module Module1
    Sub M1(n As Nullable(Of Int32))
    End Sub
    Sub M2(n As Int64?)
    End Sub
End Module"

            Assert.Equal(
                "Module1.M1(Integer? n)",
                GetName(source, "Module1.M1", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))

            Assert.Equal(
                "Module1.M2(Long? n)",
                GetName(source, "Module1.M2", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))
        End Sub

        <Fact>
        Sub GetNameGenerics()
            Dim source = "
Imports System
Class Class1(Of T)
    Sub M1(Of U)(a As Action(Of Int32))
    End Sub
    Sub M2(Of U)(a As Action(Of T))
    End Sub
    Sub M3(Of U)(a As Action(Of U))
    End Sub
End Class"
            ' TODO: Type parameters should be substituted with type arguments once we have an API to retrieve them.

            Assert.Equal(
                "Class1(Of T).M1(Of U)(System.Action(Of Integer) a)",
                GetName(source, "Class1.M1", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))

            Assert.Equal(
                "Class1(Of T).M2(Of U)(System.Action(Of T) a)",
                GetName(source, "Class1.M2", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))

            Assert.Equal(
                "Class1(Of T).M3(Of U)(System.Action(Of U) a)",
                GetName(source, "Class1.M3", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))
        End Sub

        <Fact>
        Sub GetNameAsync()
            Dim source = "
Imports System.Threading.Tasks
Module Module1
    Async Function M() As Task
        Await MAsync()
    End Function
    Async Function MAsync() As Task(Of Integer)
        Return 3
    End Function
End Module"

            Assert.Equal(
                "Module1.M()",
                GetName(source, "Module1.VB$StateMachine_0_M.MoveNext", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))
        End Sub

        <Fact, WorkItem(1107977)>
        Sub GetNameGenericAsync()
            Dim source = "
Imports System.Threading.Tasks
Class C
    Shared Async Function M(Of T)(x As T) As Task(Of T)
        Await Task.Yield()
        Return x
    End Function
End Class"

            Assert.Equal(
                "C.M(Of T)(T x)",
                GetName(source, "C.VB$StateMachine_1_M.MoveNext", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))
        End Sub

        <Fact>
        Sub GetNameIterator()
            Dim source = "
Imports System.Collections.Generic
Module Module1
    Iterator Function M() As IEnumerable(Of Integer)
        Yield 1
        Yield 3
        Yield 5
        Yield 7
    End Function
End Module"

            Assert.Equal(
                "Module1.M()",
                GetName(source, "Module1.VB$StateMachine_0_M.MoveNext", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))
        End Sub

        <Fact>
        Sub GetNameLambda()
            Dim source = "
Module Module1
    Sub M()
        Dim f = Function() 3
    End Sub
End Module"

            Assert.Equal(
                "Module1.<closure>.<lambda0-1>()",
                GetName(source, "Module1._Closure$__._Lambda$__0-1", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))
        End Sub

        <Fact>
        Sub GetNameGenericLambda()
            Dim source = "
Imports System
Class Class1(Of T)
    Sub M(Of U As T)()
        Dim f As Func(Of U, T) = Function(u2 As U) u2
    End Sub
End Class"
            ' TODO: Type parameter $CLS0 should be substituted with a type argument once we have an API to retrieve it.
            Assert.Equal(
                "Class1(Of T).<closure>.<lambda1-1>($CLS0 u2)",
                GetName(source, "Class1._Closure$__1._Lambda$__1-1", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))
        End Sub

        <Fact>
        Sub GetNameOptionalParameter()
            Dim source = "
Module Module1
    Function M(Optional d As Date = #1/1/1970#) As Integer
        Return 42
    End Function
End Module"

            Assert.Equal(
                "Module1.M(Date d)",
                GetName(source, "Module1.M", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))

            Assert.Equal(
                "Module1.M(Date d = #6/23/1912#)",
                GetName(source, "Module1.M", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types, "#6/23/1912#"))
        End Sub

        <Fact>
        Sub GetNameProperties()
            Dim source = "
Class Class1
    Property P As Integer
        Get
            Return 1
        End Get
        Set
        End Set
    End Property
    Default Property D(x As Object) As Integer
        Get
            Return 42
        End Get
        Set(i As Integer)
        End Set
    End Property
End Class"

            Assert.Equal(
                "Class1.get_P()",
                GetName(source, "Class1.get_P", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))

            Assert.Equal(
                "Class1.set_P(Integer Value)",
                GetName(source, "Class1.set_P", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))

            Assert.Equal(
                "Class1.get_D(Object x)",
                GetName(source, "Class1.get_D", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))

            Assert.Equal(
                "Class1.set_D(Object x, Integer i)",
                GetName(source, "Class1.set_D", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))
        End Sub

        <Fact>
        Sub GetNameInterfaceImplementation()
            Dim source = "
Imports System
Class C : Implements IDisposable
    Sub Dispoze() Implements IDisposable.Dispose
    End Sub
End Class"

            Assert.Equal(
                "C.Dispoze()",
                GetName(source, "C.Dispoze", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))
        End Sub

        <Fact>
        Sub GetNameExtensionMethod()
            Dim source = "
Imports System.Runtime.CompilerServices
Module Extensions
    <Extension>
    Sub M([Me] As String)
    End Sub
End Module"

            Assert.Equal(
                "Extensions.M(String Me)",
                GetName(source, "Extensions.M", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))
        End Sub

        <Fact>
        Sub GetNameArgumentFlagsNone()
            Dim source = "
Module Module1
    Sub M1()
    End Sub
    Sub M2(x, y)
    End Sub
End Module"

            Assert.Equal(
                "Module1.M1",
                GetName(source, "Module1.M1", DkmVariableInfoFlags.None))

            Assert.Equal(
                "Module1.M2",
                GetName(source, "Module1.M2", DkmVariableInfoFlags.None))
        End Sub

        Private Function GetName(source As String, methodName As String, argumentFlags As DkmVariableInfoFlags, ParamArray argumentValues() As String) As String
            Debug.Assert((argumentFlags And (DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types)) = argumentFlags,
                "Unexpected argumentFlags", "argumentFlags = {0}", argumentFlags)

            Dim compilation = CreateCompilationWithReferences(
                {VisualBasicSyntaxTree.ParseText(source)},
                references:={MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929},
                options:=TestOptions.DebugDll)
            Dim runtime = CreateRuntimeInstance(compilation)
            Dim moduleInstances = runtime.Modules
            Dim blocks = moduleInstances.SelectAsArray(Function(m) m.MetadataBlock)
            compilation = blocks.ToCompilation()
            Dim frame = DirectCast(GetMethodOrTypeBySignature(compilation, methodName), PEMethodSymbol)

            ' Once we have the method token, we want to look up the method (again)
            ' using the same helper as the product code.  This helper will also map
            ' async/ iterator "MoveNext" methods to the original source method.
            Dim method = compilation.GetSourceMethod(
                DirectCast(frame.ContainingModule, PEModuleSymbol).Module.GetModuleVersionIdOrThrow(),
                MetadataTokens.GetToken(frame.Handle))
            Dim includeParameterTypes = argumentFlags.Includes(DkmVariableInfoFlags.Types)
            Dim includeParameterNames = argumentFlags.Includes(DkmVariableInfoFlags.Names)
            Dim builder As ArrayBuilder(Of String) = Nothing
            If argumentValues.Length > 0 Then
                builder = ArrayBuilder(Of String).GetInstance()
                builder.AddRange(argumentValues)
            End If

            Dim frameDecoder = VisualBasicInstructionDecoder.Instance
            Dim frameName = frameDecoder.GetName(method, includeParameterTypes, includeParameterNames, builder)
            If builder IsNot Nothing Then
                builder.Free()
            End If

            Return frameName
        End Function

    End Class

End Namespace
