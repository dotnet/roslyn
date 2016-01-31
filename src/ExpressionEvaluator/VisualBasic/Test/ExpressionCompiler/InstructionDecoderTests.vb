' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Reflection.Metadata.Ecma335
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

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
    '// TODO: generic class/method with 2 or more type parameters
    '// TODO: generic argument type that is not from a referenced assembly
    Public Class InstructionDecoderTests : Inherits ExpressionCompilerTestBase

        <Fact>
        Public Sub GetNameArgumentCounts()
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
        Public Sub GetNameNullable()
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
        Public Sub GetNameGenerics()
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

            Assert.Equal(
                "Class1(Of T).M1(Of U)(System.Action(Of Integer) a)",
                GetName(source, "Class1.M1", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))

            Assert.Equal(
                "Class1(Of T).M2(Of U)(System.Action(Of T) a)",
                GetName(source, "Class1.M2", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))

            Assert.Equal(
                "Class1(Of T).M3(Of U)(System.Action(Of U) a)",
                GetName(source, "Class1.M3", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))

            Assert.Equal(
                "Class1(Of String).M1(Of Decimal)(System.Action(Of Integer) a)",
                GetName(source, "Class1.M1", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types, typeArguments:={GetType(String), GetType(Decimal)}))

            Assert.Equal(
                "Class1(Of String).M2(Of Decimal)(System.Action(Of String) a)",
                GetName(source, "Class1.M2", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types, typeArguments:={GetType(String), GetType(Decimal)}))

            Assert.Equal(
                "Class1(Of String).M3(Of Decimal)(System.Action(Of Decimal) a)",
                GetName(source, "Class1.M3", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types, typeArguments:={GetType(String), GetType(Decimal)}))
        End Sub

        <Fact>
        Public Sub GetNameNullTypeArguments()
            Dim source = "
Imports System
Class Class1(Of T)
    Sub M(Of U)(a As Action(Of U))
    End Sub
End Class"

            Assert.Equal(
                "Class1(Of T).M(Of U)(System.Action(Of U) a)",
                GetName(source, "Class1.M", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types, typeArguments:=New Type() {Nothing, Nothing}))

            Assert.Equal(
                "Class1(Of T).M(Of U)(System.Action(Of U) a)",
                GetName(source, "Class1.M", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types, typeArguments:={GetType(String), Nothing}))

            Assert.Equal(
                "Class1(Of T).M(Of U)(System.Action(Of U) a)",
                GetName(source, "Class1.M", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types, typeArguments:={Nothing, GetType(Decimal)}))
        End Sub

        <Fact>
        Public Sub GetNameGenericArgumentTypeNotInReferences()
            Dim source = "
Class Class1
End Class"

            Dim serializedTypeArgumentName = "Class1, " & NameOf(InstructionDecoderTests) & ", Culture=neutral, PublicKeyToken=null"
            Assert.Equal(
                "System.Collections.Generic.Comparer(Of Class1).Create(System.Comparison(Of Class1) comparison)",
                GetName(source, "System.Collections.Generic.Comparer.Create", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types, typeArguments:={serializedTypeArgumentName}))
        End Sub

        <Fact>
        Public Sub GetNameAsync()
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

        <Fact, WorkItem(1107977, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107977")>
        Public Sub GetNameGenericAsync()
            Dim source = "
Imports System.Threading.Tasks
Class C
    Shared Async Function M(Of T)(x As T) As Task(Of T)
        Await Task.Yield()
        Return x
    End Function
End Class"

            Assert.Equal(
                "C.M(Of Long)(Long x)",
                GetName(source, "C.VB$StateMachine_1_M.MoveNext", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types, typeArguments:={GetType(Long)}))
        End Sub

        <Fact>
        Public Sub GetNameIterator()
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
        Public Sub GetNameLambda()
            Dim source = "
Module Module1
    Sub M()
        Dim f = Function() 3
    End Sub
End Module"

            Assert.Equal(
                "Module1.<closure>.<lambda0-0>()",
                GetName(source, "Module1._Closure$__._Lambda$__0-0", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types))
        End Sub

        <Fact>
        Public Sub GetNameGenericLambda()
            Dim source = "
Imports System
Class Class1(Of T)
    Sub M(Of U As T)()
        Dim f As Func(Of U, T) = Function(u2 As U) u2
    End Sub
End Class"

            Assert.Equal(
                "Class1(Of System.Exception).<closure>.<lambda1-0>(System.ArgumentException u2)",
                GetName(source, "Class1._Closure$__1._Lambda$__1-0", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types, typeArguments:={GetType(Exception), GetType(ArgumentException)}))
        End Sub

        <Fact>
        Public Sub GetNameOptionalParameter()
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
                GetName(source, "Module1.M", DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types, argumentValues:={"#6/23/1912#"}))
        End Sub

        <Fact>
        Public Sub GetNameProperties()
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
        Public Sub GetNameInterfaceImplementation()
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
        Public Sub GetNameExtensionMethod()
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
        Public Sub GetNameArgumentFlagsNone()
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

        <Fact, WorkItem(1107978, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107978")>
        Public Sub GetNameRefAndOutParameters()
            Dim source = "
Imports System.Runtime.InteropServices
Class C
    Shared Sub M(ByRef x As Integer, <Out> ByRef y As Integer)
        y = x
    End Sub
End Class"

            Assert.Equal(
                "C.M",
                GetName(source, "C.M", DkmVariableInfoFlags.None))

            Assert.Equal(
                "C.M(1, 2)",
                GetName(source, "C.M", DkmVariableInfoFlags.None, argumentValues:={"1", "2"}))

            Assert.Equal(
                "C.M(Integer, Integer)",
                GetName(source, "C.M", DkmVariableInfoFlags.Types))

            Assert.Equal(
                "C.M(x, y)",
                GetName(source, "C.M", DkmVariableInfoFlags.Names))

            Assert.Equal(
                "C.M(Integer x, Integer y)",
                GetName(source, "C.M", DkmVariableInfoFlags.Types Or DkmVariableInfoFlags.Names))
        End Sub

        <Fact>
        Public Sub GetNameParamsParameters()
            Dim source = "
Class C
    Shared Sub M(ParamArray x() As Integer)
    End Sub
End Class"

            Assert.Equal(
                "C.M(Integer() x)",
                GetName(source, "C.M", DkmVariableInfoFlags.Types Or DkmVariableInfoFlags.Names))
        End Sub

        <Fact, WorkItem(1154945, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1154945")>
        Public Sub GetNameIncorrectNumberOfArgumentValues()
            Dim source = "
Class C
    Sub M(x As Integer, y As Integer)
    End Sub
End Class"
            Dim expected = "C.M(Integer x, Integer y)"

            Assert.Equal(expected,
                GetName(source, "C.M", DkmVariableInfoFlags.Types Or DkmVariableInfoFlags.Names, argumentValues:={}))

            Assert.Equal(expected,
                GetName(source, "C.M", DkmVariableInfoFlags.Types Or DkmVariableInfoFlags.Names, argumentValues:={"1"}))

            Assert.Equal(expected,
                GetName(source, "C.M", DkmVariableInfoFlags.Types Or DkmVariableInfoFlags.Names, argumentValues:={"1", "2", "3"}))
        End Sub

        <Fact>
        Public Sub GetReturnTypeNamePrimitive()
            Dim source = "
Class C
    Function M1() As UInteger
        Return 42
    End Function
End Class"

            Assert.Equal("UInteger", GetReturnTypeName(source, "C.M1"))
        End Sub

        <Fact>
        Public Sub GetReturnTypeNameNested()
            Dim source = "
Class C
    Function M1() As N.D.E
        Return Nothing
    End Function
End Class
Namespace N
    Class D
        Friend Structure E
        End Structure
    End Class
End Namespace"

            Assert.Equal("N.D.E", GetReturnTypeName(source, "C.M1"))
        End Sub

        <Fact>
        Public Sub GetReturnTypeNameGenericOfPrimitive()
            Dim source = "
Imports System
Class C
    Function M1() As Action(Of Int32)
        Return Nothing
    End Function
End Class"

            Assert.Equal("System.Action(Of Integer)", GetReturnTypeName(source, "C.M1"))
        End Sub

        <Fact>
        Public Sub GetReturnTypeNameGenericOfNested()
            Dim source = "
Imports System
Class C
    Function M1() As Action(Of D)
        Return Nothing
    End Function
    Class D
    End Class
End Class"

            Assert.Equal("System.Action(Of C.D)", GetReturnTypeName(source, "C.M1"))
        End Sub

        <Fact>
        Public Sub GetReturnTypeNameGenericOfGeneric()
            Dim source = "
Imports System
Class C
    Function M1(Of T)() As Action(Of Func(Of T))
        Return Nothing
    End Function
End Class"

            Assert.Equal("System.Action(Of System.Func(Of Object))", GetReturnTypeName(source, "C.M1", typeArguments:={GetType(Object)}))
        End Sub

        Private Function GetName(source As String, methodName As String, argumentFlags As DkmVariableInfoFlags, Optional typeArguments() As Type = Nothing, Optional argumentValues() As String = Nothing) As String
            Dim serializedTypeArgumentNames = typeArguments?.Select(Function(t) t?.AssemblyQualifiedName).ToArray()
            Return GetName(source, methodName, argumentFlags, serializedTypeArgumentNames, argumentValues)
        End Function

        Private Function GetName(source As String, methodName As String, argumentFlags As DkmVariableInfoFlags, typeArguments() As String, Optional argumentValues() As String = Nothing) As String
            Debug.Assert((argumentFlags And (DkmVariableInfoFlags.Names Or DkmVariableInfoFlags.Types)) = argumentFlags,
                "Unexpected argumentFlags", "argumentFlags = {0}", argumentFlags)

            Dim instructionDecoder = VisualBasicInstructionDecoder.Instance
            Dim method = GetConstructedMethod(source, methodName, typeArguments, instructionDecoder)

            Dim includeParameterTypes = argumentFlags.Includes(DkmVariableInfoFlags.Types)
            Dim includeParameterNames = argumentFlags.Includes(DkmVariableInfoFlags.Names)
            Dim builder As ArrayBuilder(Of String) = Nothing
            If argumentValues IsNot Nothing Then
                builder = ArrayBuilder(Of String).GetInstance()
                builder.AddRange(argumentValues)
            End If

            Dim name = instructionDecoder.GetName(method, includeParameterTypes, includeParameterNames, builder)
            If builder IsNot Nothing Then
                builder.Free()
            End If

            Return name
        End Function

        Private Function GetReturnTypeName(source As String, methodName As String, Optional typeArguments() As Type = Nothing) As String
            Dim instructionDecoder = VisualBasicInstructionDecoder.Instance
            Dim serializedTypeArgumentNames = typeArguments?.Select(Function(t) t?.AssemblyQualifiedName).ToArray()
            Dim method = GetConstructedMethod(source, methodName, serializedTypeArgumentNames, instructionDecoder)

            Return instructionDecoder.GetReturnTypeName(method)
        End Function

        Private Function GetConstructedMethod(source As String, methodName As String, serializedTypeArgumentNames() As String, instructionDecoder As VisualBasicInstructionDecoder) As MethodSymbol
            Dim compilation = CreateCompilationWithReferences(
                {VisualBasicSyntaxTree.ParseText(source)},
                references:={MscorlibRef_v4_0_30316_17626, MsvbRef_v4_0_30319_17929},
                options:=TestOptions.DebugDll,
                assemblyName:=NameOf(InstructionDecoderTests))
            Dim runtime = CreateRuntimeInstance(compilation)
            Dim moduleInstances = runtime.Modules
            Dim blocks = moduleInstances.SelectAsArray(Function(m) m.MetadataBlock)
            compilation = blocks.ToCompilation()
            Dim frame = DirectCast(GetMethodOrTypeBySignature(compilation, methodName), PEMethodSymbol)

            ' Once we have the method token, we want to look up the method (again)
            ' using the same helper as the product code.  This helper will also map
            ' async/ iterator "MoveNext" methods to the original source method.
            Dim method As MethodSymbol = compilation.GetSourceMethod(
                DirectCast(frame.ContainingModule, PEModuleSymbol).Module.GetModuleVersionIdOrThrow(),
                MetadataTokens.GetToken(frame.Handle))
            If serializedTypeArgumentNames IsNot Nothing Then
                Assert.NotEmpty(serializedTypeArgumentNames)
                Dim typeParameters = instructionDecoder.GetAllTypeParameters(method)
                Assert.NotEmpty(typeParameters)
                Dim typeNameDecoder = New EETypeNameDecoder(compilation, DirectCast(method.ContainingModule, PEModuleSymbol))
                ' Use the same helper method as the FrameDecoder to get the TypeSymbols for the
                ' generic type arguments (rather than using EETypeNameDecoder directly).
                Dim typeArgumentSymbols = instructionDecoder.GetTypeSymbols(compilation, method, serializedTypeArgumentNames)
                If Not typeArgumentSymbols.IsEmpty Then
                    method = instructionDecoder.ConstructMethod(method, typeParameters, typeArgumentSymbols)
                End If
            End If

            Return method
        End Function
    End Class

End Namespace
