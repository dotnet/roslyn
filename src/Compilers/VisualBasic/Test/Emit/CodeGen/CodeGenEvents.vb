' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenEvents
        Inherits BasicTestBase

        <Fact()>
        Public Sub SimpleAddHandler()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim del As System.EventHandler =
            Sub(sender As Object, a As EventArgs) Console.Write("unload")

        Dim v = AppDomain.CreateDomain("qq")

        AddHandler (v.DomainUnload), del

        AppDomain.Unload(v)    
    End Sub
End Module
    </file>
    </compilation>).
                VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       60 (0x3c)
  .maxstack  3
  .locals init (System.EventHandler V_0) //del
  IL_0000:  ldsfld     "MyClass1._Closure$__.$I0-0 As System.EventHandler"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "MyClass1._Closure$__.$I0-0 As System.EventHandler"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "MyClass1._Closure$__.$I As MyClass1._Closure$__"
  IL_0013:  ldftn      "Sub MyClass1._Closure$__._Lambda$__0-0(Object, System.EventArgs)"
  IL_0019:  newobj     "Sub System.EventHandler..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "MyClass1._Closure$__.$I0-0 As System.EventHandler"
  IL_0024:  stloc.0
  IL_0025:  ldstr      "qq"
  IL_002a:  call       "Function System.AppDomain.CreateDomain(String) As System.AppDomain"
  IL_002f:  dup
  IL_0030:  ldloc.0
  IL_0031:  callvirt   "Sub System.AppDomain.add_DomainUnload(System.EventHandler)"
  IL_0036:  call       "Sub System.AppDomain.Unload(System.AppDomain)"
  IL_003b:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleRemoveHandler()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System

Module MyClass1
    Sub Main(args As String())
        Dim del As System.EventHandler =
            Sub(sender As Object, a As EventArgs) Console.Write("unload")

        Dim v = AppDomain.CreateDomain("qq")

        AddHandler (v.DomainUnload), del
        RemoveHandler (v.DomainUnload), del

        AppDomain.Unload(v)    
    End Sub
End Module
    </file>
    </compilation>).
                VerifyIL("MyClass1.Main",
            <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  3
  .locals init (System.EventHandler V_0) //del
  IL_0000:  ldsfld     "MyClass1._Closure$__.$I0-0 As System.EventHandler"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "MyClass1._Closure$__.$I0-0 As System.EventHandler"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "MyClass1._Closure$__.$I As MyClass1._Closure$__"
  IL_0013:  ldftn      "Sub MyClass1._Closure$__._Lambda$__0-0(Object, System.EventArgs)"
  IL_0019:  newobj     "Sub System.EventHandler..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "MyClass1._Closure$__.$I0-0 As System.EventHandler"
  IL_0024:  stloc.0
  IL_0025:  ldstr      "qq"
  IL_002a:  call       "Function System.AppDomain.CreateDomain(String) As System.AppDomain"
  IL_002f:  dup
  IL_0030:  ldloc.0
  IL_0031:  callvirt   "Sub System.AppDomain.add_DomainUnload(System.EventHandler)"
  IL_0036:  dup
  IL_0037:  ldloc.0
  IL_0038:  callvirt   "Sub System.AppDomain.remove_DomainUnload(System.EventHandler)"
  IL_003d:  call       "Sub System.AppDomain.Unload(System.AppDomain)"
  IL_0042:  ret
}
    ]]>)
        End Sub

        <Fact()>
        Public Sub AddHandlerWithLambdaConversion()
            Dim csdllCompilation = CreateCSharpCompilation("CSDll",
            <![CDATA[
public class CSDllClass
{
    public event System.Action E1;

    public void Raise()
    {
        E1();
    }
}

public class CSDllClasDerived : CSDllClass
{

}

]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csdllCompilation.VerifyDiagnostics()

            Dim vbexeCompilation = CreateVisualBasicCompilation("VBExe",
            <![CDATA[Imports System

Public Class VBExeClass

End Class

Public Module Program
    Sub Main(args As String())
        Dim o As New CSDllClasDerived

        AddHandler o.E1, Sub() Console.Write("hi ")
        AddHandler o.E1, AddressOf H1

        o.Raise
    End Sub

    Sub H1()
        Console.Write("bye")
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csdllCompilation})
            Dim vbexeVerifier = CompileAndVerify(vbexeCompilation,
                expectedOutput:=<![CDATA[hi bye]]>)
            vbexeVerifier.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub AddHandlerWithDelegateRelaxation()
            Dim csdllCompilation = CreateCSharpCompilation("CSDll",
            <![CDATA[
public class CSDllClass
{
    public event System.Action<int> E1;

    public void Raise()
    {
        E1(42);
    }
}

public class CSDllClasDerived : CSDllClass
{

}

]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csdllCompilation.VerifyDiagnostics()

            Dim vbexeCompilation = CreateVisualBasicCompilation("VBExe",
            <![CDATA[
Imports System

Public Class VBExeClass

End Class

Public Module Program
    Sub Main(args As String())
        Dim o As New CSDllClasDerived

        AddHandler o.E1, AddressOf H1

        o.Raise
    End Sub

    Sub H1()
        Console.Write("bye")
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csdllCompilation})
            Dim vbexeVerifier = CompileAndVerify(vbexeCompilation,
                expectedOutput:=<![CDATA[bye]]>)
            vbexeVerifier.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub SimpleEvent()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System

Module Module1

    Private Event e1 As Action

    Sub Main(args As String())
        Dim h As Action = Sub() Console.Write("hello ")
        AddHandler e1, h
        e1Event.Invoke()

        RemoveHandler e1, h
        Console.Write(e1Event Is Nothing)
    End Sub
End Module

    </file>
    </compilation>, expectedOutput:="hello True").
                VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       71 (0x47)
  .maxstack  2
  IL_0000:  ldsfld     "Module1._Closure$__.$I4-0 As System.Action"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Module1._Closure$__.$I4-0 As System.Action"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Module1._Closure$__.$I As Module1._Closure$__"
  IL_0013:  ldftn      "Sub Module1._Closure$__._Lambda$__4-0()"
  IL_0019:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Module1._Closure$__.$I4-0 As System.Action"
  IL_0024:  dup
  IL_0025:  call       "Sub Module1.add_e1(System.Action)"
  IL_002a:  ldsfld     "Module1.e1Event As System.Action"
  IL_002f:  callvirt   "Sub System.Action.Invoke()"
  IL_0034:  call       "Sub Module1.remove_e1(System.Action)"
  IL_0039:  ldsfld     "Module1.e1Event As System.Action"
  IL_003e:  ldnull
  IL_003f:  ceq
  IL_0041:  call       "Sub System.Console.Write(Boolean)"
  IL_0046:  ret
}
    ]]>)
        End Sub

        <Fact()>
        Public Sub SimpleEvent1()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System

Module Module1

    Class Nested
    End Class

    Private Event e1

    Private field as e1EventHandler

    Sub Main(args As String())
        Dim h As e1EventHandler = Sub() Console.Write("hello ")
        AddHandler e1, h
        e1Event.Invoke()

        RemoveHandler e1, h
        Console.Write(e1Event Is Nothing)
    End Sub
End Module

    </file>
    </compilation>, expectedOutput:="hello True").
                VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       71 (0x47)
  .maxstack  2
  IL_0000:  ldsfld     "Module1._Closure$__.$I7-0 As Module1.e1EventHandler"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Module1._Closure$__.$I7-0 As Module1.e1EventHandler"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Module1._Closure$__.$I As Module1._Closure$__"
  IL_0013:  ldftn      "Sub Module1._Closure$__._Lambda$__7-0()"
  IL_0019:  newobj     "Sub Module1.e1EventHandler..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Module1._Closure$__.$I7-0 As Module1.e1EventHandler"
  IL_0024:  dup
  IL_0025:  call       "Sub Module1.add_e1(Module1.e1EventHandler)"
  IL_002a:  ldsfld     "Module1.e1Event As Module1.e1EventHandler"
  IL_002f:  callvirt   "Sub Module1.e1EventHandler.Invoke()"
  IL_0034:  call       "Sub Module1.remove_e1(Module1.e1EventHandler)"
  IL_0039:  ldsfld     "Module1.e1Event As Module1.e1EventHandler"
  IL_003e:  ldnull
  IL_003f:  ceq
  IL_0041:  call       "Sub System.Console.Write(Boolean)"
  IL_0046:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleEvent2()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System

Module Module1

    Class Nested
    End Class

    Private Event e1(x As e1EventHandler)

    Sub Main(args As String())
        Dim h As e1EventHandler = Sub() Console.Write("hello ")
        AddHandler e1, h
        e1Event.Invoke(h)

        RemoveHandler e1, h
        Console.Write(e1Event Is Nothing)
    End Sub
End Module

    </file>
    </compilation>, expectedOutput:="hello True").
                VerifyIL("Module1.Main",
            <![CDATA[
{
  // Code size       74 (0x4a)
  .maxstack  2
  .locals init (Module1.e1EventHandler V_0) //h
  IL_0000:  ldsfld     "Module1._Closure$__.$IR6-1 As Module1.e1EventHandler"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Module1._Closure$__.$IR6-1 As Module1.e1EventHandler"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Module1._Closure$__.$I As Module1._Closure$__"
  IL_0013:  ldftn      "Sub Module1._Closure$__._Lambda$__R6-1(Module1.e1EventHandler)"
  IL_0019:  newobj     "Sub Module1.e1EventHandler..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Module1._Closure$__.$IR6-1 As Module1.e1EventHandler"
  IL_0024:  stloc.0
  IL_0025:  ldloc.0
  IL_0026:  call       "Sub Module1.add_e1(Module1.e1EventHandler)"
  IL_002b:  ldsfld     "Module1.e1Event As Module1.e1EventHandler"
  IL_0030:  ldloc.0
  IL_0031:  callvirt   "Sub Module1.e1EventHandler.Invoke(Module1.e1EventHandler)"
  IL_0036:  ldloc.0
  IL_0037:  call       "Sub Module1.remove_e1(Module1.e1EventHandler)"
  IL_003c:  ldsfld     "Module1.e1Event As Module1.e1EventHandler"
  IL_0041:  ldnull
  IL_0042:  ceq
  IL_0044:  call       "Sub System.Console.Write(Boolean)"
  IL_0049:  ret
}
    ]]>)
        End Sub

        <Fact()>
        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(23282, "https://github.com/dotnet/roslyn/issues/23282")>
        Public Sub SimpleRaiseHandlerWithBlockEvent_01()
            Dim verifier = CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System

Module Program

    Delegate Sub del1(ByRef x As Integer)

    Custom Event E As del1
        AddHandler(value As del1)
            System.Console.Write("Add")
        End AddHandler

        RemoveHandler(value As del1)
            System.Console.Write("Remove")
        End RemoveHandler

        RaiseEvent(ByRef x As Integer)
            System.Console.Write("Raise")
        End RaiseEvent
    End Event

    Sub Main(args As String())
        AddHandler E, Nothing
        RemoveHandler E, Nothing
        RaiseEvent E(42)
    End Sub
End Module

    </file>
    </compilation>, expectedOutput:="AddRemoveRaise").
                VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (Integer V_0)
  IL_0000:  ldnull
  IL_0001:  call       "Sub Program.add_E(Program.del1)"
  IL_0006:  ldnull
  IL_0007:  call       "Sub Program.remove_E(Program.del1)"
  IL_000c:  ldc.i4.s   42
  IL_000e:  stloc.0
  IL_000f:  ldloca.s   V_0
  IL_0011:  call       "Sub Program.raise_E(ByRef Integer)"
  IL_0016:  ret
}
    ]]>)

            Dim compilation = verifier.Compilation
            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim add = tree.GetRoot().DescendantNodes().OfType(Of AddRemoveHandlerStatementSyntax)().First()

            Assert.Equal("AddHandler E, Nothing", add.ToString())

            compilation.VerifyOperationTree(add, expectedOperationTree:=
            <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler E, Nothing')
  Expression: 
    IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler E, Nothing')
      Event Reference: 
        IEventReferenceOperation: Event Program.E As Program.del1 (Static) (OperationKind.EventReference, Type: Program.del1) (Syntax: 'E')
          Instance Receiver: 
            null
      Handler: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.del1, Constant: null, IsImplicit) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value)

            Assert.Equal("Event Program.E As Program.del1", model.GetSymbolInfo(add.EventExpression).Symbol.ToTestDisplayString())

            Dim remove = tree.GetRoot().DescendantNodes().OfType(Of AddRemoveHandlerStatementSyntax)().Last()

            Assert.Equal("RemoveHandler E, Nothing", remove.ToString())

            compilation.VerifyOperationTree(remove, expectedOperationTree:=
            <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'RemoveHandler E, Nothing')
  Expression: 
    IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'RemoveHandler E, Nothing')
      Event Reference: 
        IEventReferenceOperation: Event Program.E As Program.del1 (Static) (OperationKind.EventReference, Type: Program.del1) (Syntax: 'E')
          Instance Receiver: 
            null
      Handler: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.del1, Constant: null, IsImplicit) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value)

            Assert.Equal("Event Program.E As Program.del1", model.GetSymbolInfo(remove.EventExpression).Symbol.ToTestDisplayString())

            Dim raise = tree.GetRoot().DescendantNodes().OfType(Of RaiseEventStatementSyntax)().Single()

            Assert.Equal("RaiseEvent E(42)", raise.ToString())

            compilation.VerifyOperationTree(raise, expectedOperationTree:=
            <![CDATA[
IRaiseEventOperation (OperationKind.RaiseEvent, Type: null) (Syntax: 'RaiseEvent E(42)')
  Event Reference: 
    IEventReferenceOperation: Event Program.E As Program.del1 (Static) (OperationKind.EventReference, Type: Program.del1) (Syntax: 'E')
      Instance Receiver: 
        null
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '42')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value)

            Assert.Equal("Event Program.E As Program.del1", model.GetSymbolInfo(raise.Name).Symbol.ToTestDisplayString())
        End Sub

        <Fact()>
        <CompilerTrait(CompilerFeature.IOperation)>
        <WorkItem(23282, "https://github.com/dotnet/roslyn/issues/23282")>
        Public Sub SimpleRaiseHandlerWithBlockEvent_02()
            Dim verifier = CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System

Class Program

    Delegate Sub del1(ByRef x As Integer)

    Custom Event E As del1
        AddHandler(value As del1)
            System.Console.Write("Add")
        End AddHandler

        RemoveHandler(value As del1)
            System.Console.Write("Remove")
        End RemoveHandler

        RaiseEvent(ByRef x As Integer)
            System.Console.Write("Raise")
        End RaiseEvent
    End Event

    Shared Sub Main()
        Call New Program().Test()
    End Sub

    Sub Test()
        AddHandler E, Nothing
        RemoveHandler E, Nothing
        RaiseEvent E(42)
    End Sub
End Class

    </file>
    </compilation>, expectedOutput:="AddRemoveRaise").
                VerifyIL("Program.Test",
            <![CDATA[
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (Integer V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  call       "Sub Program.add_E(Program.del1)"
  IL_0007:  ldarg.0
  IL_0008:  ldnull
  IL_0009:  call       "Sub Program.remove_E(Program.del1)"
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.s   42
  IL_0011:  stloc.0
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       "Sub Program.raise_E(ByRef Integer)"
  IL_0019:  ret
}
    ]]>)

            Dim compilation = verifier.Compilation
            Dim tree = compilation.SyntaxTrees.Single()
            Dim model = compilation.GetSemanticModel(tree)

            Dim add = tree.GetRoot().DescendantNodes().OfType(Of AddRemoveHandlerStatementSyntax)().First()

            Assert.Equal("AddHandler E, Nothing", add.ToString())

            compilation.VerifyOperationTree(add, expectedOperationTree:=
            <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'AddHandler E, Nothing')
  Expression: 
    IEventAssignmentOperation (EventAdd) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'AddHandler E, Nothing')
      Event Reference: 
        IEventReferenceOperation: Event Program.E As Program.del1 (OperationKind.EventReference, Type: Program.del1) (Syntax: 'E')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsImplicit) (Syntax: 'E')
      Handler: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.del1, Constant: null, IsImplicit) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value)

            Assert.Equal("Event Program.E As Program.del1", model.GetSymbolInfo(add.EventExpression).Symbol.ToTestDisplayString())

            Dim remove = tree.GetRoot().DescendantNodes().OfType(Of AddRemoveHandlerStatementSyntax)().Last()

            Assert.Equal("RemoveHandler E, Nothing", remove.ToString())

            compilation.VerifyOperationTree(remove, expectedOperationTree:=
            <![CDATA[
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'RemoveHandler E, Nothing')
  Expression: 
    IEventAssignmentOperation (EventRemove) (OperationKind.EventAssignment, Type: null, IsImplicit) (Syntax: 'RemoveHandler E, Nothing')
      Event Reference: 
        IEventReferenceOperation: Event Program.E As Program.del1 (OperationKind.EventReference, Type: Program.del1) (Syntax: 'E')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsImplicit) (Syntax: 'E')
      Handler: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program.del1, Constant: null, IsImplicit) (Syntax: 'Nothing')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
]]>.Value)

            Assert.Equal("Event Program.E As Program.del1", model.GetSymbolInfo(remove.EventExpression).Symbol.ToTestDisplayString())

            Dim raise = tree.GetRoot().DescendantNodes().OfType(Of RaiseEventStatementSyntax)().Single()

            Assert.Equal("RaiseEvent E(42)", raise.ToString())

            compilation.VerifyOperationTree(raise, expectedOperationTree:=
            <![CDATA[
IRaiseEventOperation (OperationKind.RaiseEvent, Type: null) (Syntax: 'RaiseEvent E(42)')
  Event Reference: 
    IEventReferenceOperation: Event Program.E As Program.del1 (OperationKind.EventReference, Type: Program.del1) (Syntax: 'E')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Program, IsImplicit) (Syntax: 'E')
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '42')
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value)

            Assert.Equal("Event Program.E As Program.del1", model.GetSymbolInfo(raise.Name).Symbol.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub SimpleRaiseHandlerWithFieldEvent()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">

Imports System

Module Program

    Delegate Sub del1(ByRef x As String)
    Event E As del1
    Property str As String

    Sub Main(args As String())
        Dim lambda As del1 =
            Sub(ByRef s As String)
                Console.Write(s)
                s = "bye"
            End Sub

        str = "hello "
        RaiseEvent E(str)
        AddHandler E, lambda
        RaiseEvent E(x:=str)
        RemoveHandler E, lambda
        RaiseEvent E(str)

        Console.Write(str)
    End Sub
End Module

    </file>
    </compilation>, expectedOutput:="hello bye").
                VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size      155 (0x9b)
  .maxstack  3
  .locals init (Program.del1 V_0,
                String V_1,
                Program.del1 V_2,
                Program.del1 V_3)
  IL_0000:  ldsfld     "Program._Closure$__.$I9-0 As Program.del1"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "Program._Closure$__.$I9-0 As Program.del1"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0013:  ldftn      "Sub Program._Closure$__._Lambda$__9-0(ByRef String)"
  IL_0019:  newobj     "Sub Program.del1..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "Program._Closure$__.$I9-0 As Program.del1"
  IL_0024:  ldstr      "hello "
  IL_0029:  call       "Sub Program.set_str(String)"
  IL_002e:  ldsfld     "Program.EEvent As Program.del1"
  IL_0033:  stloc.0
  IL_0034:  ldloc.0
  IL_0035:  brfalse.s  IL_004b
  IL_0037:  ldloc.0
  IL_0038:  call       "Function Program.get_str() As String"
  IL_003d:  stloc.1
  IL_003e:  ldloca.s   V_1
  IL_0040:  callvirt   "Sub Program.del1.Invoke(ByRef String)"
  IL_0045:  ldloc.1
  IL_0046:  call       "Sub Program.set_str(String)"
  IL_004b:  dup
  IL_004c:  call       "Sub Program.add_E(Program.del1)"
  IL_0051:  ldsfld     "Program.EEvent As Program.del1"
  IL_0056:  stloc.2
  IL_0057:  ldloc.2
  IL_0058:  brfalse.s  IL_006e
  IL_005a:  ldloc.2
  IL_005b:  call       "Function Program.get_str() As String"
  IL_0060:  stloc.1
  IL_0061:  ldloca.s   V_1
  IL_0063:  callvirt   "Sub Program.del1.Invoke(ByRef String)"
  IL_0068:  ldloc.1
  IL_0069:  call       "Sub Program.set_str(String)"
  IL_006e:  call       "Sub Program.remove_E(Program.del1)"
  IL_0073:  ldsfld     "Program.EEvent As Program.del1"
  IL_0078:  stloc.3
  IL_0079:  ldloc.3
  IL_007a:  brfalse.s  IL_0090
  IL_007c:  ldloc.3
  IL_007d:  call       "Function Program.get_str() As String"
  IL_0082:  stloc.1
  IL_0083:  ldloca.s   V_1
  IL_0085:  callvirt   "Sub Program.del1.Invoke(ByRef String)"
  IL_008a:  ldloc.1
  IL_008b:  call       "Sub Program.set_str(String)"
  IL_0090:  call       "Function Program.get_str() As String"
  IL_0095:  call       "Sub System.Console.Write(String)"
  IL_009a:  ret
}
    ]]>)
        End Sub

        <Fact()>
        Public Sub SimpleRaiseHandlerWithFieldEventInStruct()
            Dim c = CompileAndVerify(
    <compilation>
        <file name="a.vb">


Imports System


Module Program
    Delegate Sub del1()

    Event E As del1

    Sub Main(args As String())
        Dim s As New s1(Nothing)
    End Sub
End Module

Structure s1
    Delegate Sub del1(x As Object)

    Event E As del1

    Sub New(args As String())
        RaiseEvent E(1)
    End Sub
End Structure

    </file>
    </compilation>, expectedOutput:="")

            c.VerifyIL("s1..ctor", <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  2
  .locals init (s1.del1 V_0)
  IL_0000:  ldarg.0
  IL_0001:  initobj    "s1"
  IL_0007:  ldarg.0
  IL_0008:  ldfld      "s1.EEvent As s1.del1"
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  brfalse.s  IL_001d
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.1
  IL_0013:  box        "Integer"
  IL_0018:  callvirt   "Sub s1.del1.Invoke(Object)"
  IL_001d:  ret
}
    ]]>)
        End Sub

        <Fact()>
        Public Sub SimpleRaiseHandlerWithImplementedEvent()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">

Imports System

Module Program

    Interface i1
        Delegate Sub del1(ByRef x As String)
        Event E As del1

        Sub Raise(ByRef x As String)
    End Interface

    Class cls1
        Implements i1

        Private Event E as i1.del1 Implements i1.E

        Private Sub Raise(ByRef x As String) Implements i1.Raise
            RaiseEvent E(x)
        End Sub
    End Class

    Sub Main()

        Dim i As i1 = New cls1

        AddHandler i.E, Sub(ByRef s As String)
                            Console.Write(s)
                            s = "bye"
                        End Sub

        Dim Str As String = "hello "
        i.Raise(Str)

        Console.Write(Str)
    End Sub
End Module


    </file>
    </compilation>, expectedOutput:="hello bye").
                VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  4
  .locals init (String V_0) //Str
  IL_0000:  newobj     "Sub Program.cls1..ctor()"
  IL_0005:  dup
  IL_0006:  ldsfld     "Program._Closure$__.$I2-0 As Program.i1.del1"
  IL_000b:  brfalse.s  IL_0014
  IL_000d:  ldsfld     "Program._Closure$__.$I2-0 As Program.i1.del1"
  IL_0012:  br.s       IL_002a
  IL_0014:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0019:  ldftn      "Sub Program._Closure$__._Lambda$__2-0(ByRef String)"
  IL_001f:  newobj     "Sub Program.i1.del1..ctor(Object, System.IntPtr)"
  IL_0024:  dup
  IL_0025:  stsfld     "Program._Closure$__.$I2-0 As Program.i1.del1"
  IL_002a:  callvirt   "Sub Program.i1.add_E(Program.i1.del1)"
  IL_002f:  ldstr      "hello "
  IL_0034:  stloc.0
  IL_0035:  ldloca.s   V_0
  IL_0037:  callvirt   "Sub Program.i1.Raise(ByRef String)"
  IL_003c:  ldloc.0
  IL_003d:  call       "Sub System.Console.Write(String)"
  IL_0042:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SimpleRaiseHandlerWithTwoImplementedEvents()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">

Imports System

Module Program

    Interface i1
        Delegate Sub del1(ByRef x As String)
        Event E As del1
        Event E1 As del1

        Sub Raise(ByRef x As String)
    End Interface

    Class cls1
        Implements i1

        Private Event E(ByRef x As String) Implements i1.E, i1.E1

        Private Sub Raise(ByRef x As String) Implements i1.Raise
            RaiseEvent E(x)
        End Sub
    End Class

    Sub Main()

        Dim i As i1 = New cls1

        ' NOTE!!   Adding to E1
        AddHandler i.E1, Sub(ByRef s As String)
                             Console.Write(s)
                             s = "bye"
                         End Sub

        Dim Str As String = "hello "
        i.Raise(Str)

        Console.Write(Str)
    End Sub
End Module

    </file>
    </compilation>, expectedOutput:="hello bye").
                VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       67 (0x43)
  .maxstack  4
  .locals init (String V_0) //Str
  IL_0000:  newobj     "Sub Program.cls1..ctor()"
  IL_0005:  dup
  IL_0006:  ldsfld     "Program._Closure$__.$I2-0 As Program.i1.del1"
  IL_000b:  brfalse.s  IL_0014
  IL_000d:  ldsfld     "Program._Closure$__.$I2-0 As Program.i1.del1"
  IL_0012:  br.s       IL_002a
  IL_0014:  ldsfld     "Program._Closure$__.$I As Program._Closure$__"
  IL_0019:  ldftn      "Sub Program._Closure$__._Lambda$__2-0(ByRef String)"
  IL_001f:  newobj     "Sub Program.i1.del1..ctor(Object, System.IntPtr)"
  IL_0024:  dup
  IL_0025:  stsfld     "Program._Closure$__.$I2-0 As Program.i1.del1"
  IL_002a:  callvirt   "Sub Program.i1.add_E1(Program.i1.del1)"
  IL_002f:  ldstr      "hello "
  IL_0034:  stloc.0
  IL_0035:  ldloca.s   V_0
  IL_0037:  callvirt   "Sub Program.i1.Raise(ByRef String)"
  IL_003c:  ldloc.0
  IL_003d:  call       "Sub System.Console.Write(String)"
  IL_0042:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub EventOverridesInCS()
            Dim csdllCompilation = CreateCSharpCompilation("CSDll",
            <![CDATA[
using System;

public abstract class CSDllClass
{
    public abstract event System.Action E1;

    public abstract void Raise();
}

public class CSDllClasDerived : CSDllClass
{
    public override event Action E1;
    
    public override void Raise()
    {
        E1();
    }
}
]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csdllCompilation.VerifyDiagnostics()

            Dim vbexeCompilation = CreateVisualBasicCompilation("VBExe",
            <![CDATA[
Imports System

Public Class VBExeClass
    inherits CSDllClasDerived
End Class

Public Module Program
    Sub Main(args As String())
        Dim o As New VBExeClass

        AddHandler o.E1, Sub() Console.Write("hi ")
        AddHandler o.E1, AddressOf H1

        o.Raise
    End Sub

    Sub H1()
        Console.Write("bye")
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csdllCompilation})

            vbexeCompilation.VerifyDiagnostics()

            Dim treeA = CompilationUtils.GetTree(vbexeCompilation, "")
            Dim bindingsA = vbexeCompilation.GetSemanticModel(treeA)

            ' Find "Class VBExeClass".
            Dim typeVBExeClass = CompilationUtils.GetTypeSymbol(vbexeCompilation, bindingsA, "", "VBExeClass")
            Dim VBExeClassBase1 = typeVBExeClass.BaseType

            Dim ev = DirectCast(VBExeClassBase1.GetMembers("E1").First, EventSymbol)
            Assert.Equal(True, ev.IsOverrides)

            Dim overrideList = ev.OverriddenOrHiddenMembers.OverriddenMembers
            Assert.Equal("Action", DirectCast(ev.OverriddenMember, EventSymbol).Type.Name)

            Dim vbexeVerifier = CompileAndVerify(vbexeCompilation,
                expectedOutput:=<![CDATA[hi bye]]>)

            vbexeVerifier.VerifyDiagnostics()

        End Sub

        <Fact(), WorkItem(543612, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543612")>
        Public Sub CallEventHandlerThroughWithEvent01()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Option Explicit On
Imports System

Module Program
    Sub Main()
        Dim Var8_7 As New InitOnly8_7()
        Var8_7.x.Spark()
        Console.Write(Var8_7.y)
    End Sub
End Module

Public Class InitOnly8_7
    Public Class InitOnly8_7_1
        Public Event Flash()

        Public Sub Spark()
            RaiseEvent Flash()
        End Sub
    End Class

    Public WithEvents x As InitOnly8_7_1

    Sub New()
        x = New InitOnly8_7_1()
    End Sub

    Public y As Boolean = False
    Public Sub Blink() Handles x.Flash
        y = True
    End Sub
End Class
    </file>
    </compilation>, expectedOutput:="True").
                VerifyIL("Program.Main",
            <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  2
  IL_0000:  newobj     "Sub InitOnly8_7..ctor()"
  IL_0005:  dup
  IL_0006:  callvirt   "Function InitOnly8_7.get_x() As InitOnly8_7.InitOnly8_7_1"
  IL_000b:  callvirt   "Sub InitOnly8_7.InitOnly8_7_1.Spark()"
  IL_0010:  ldfld      "InitOnly8_7.y As Boolean"
  IL_0015:  call       "Sub System.Console.Write(Boolean)"
  IL_001a:  ret
}
    ]]>)
        End Sub

        <Fact(), WorkItem(543612, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543612")>
        Public Sub CallEventHandlerThroughWithEvent02()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
Option Explicit On
Imports System

Module Program
    Sub Main()
        Dim var4_14 As New InitOnly4_14()
    End Sub
End Module

Class InitOnly4_14
    ReadOnly x As Byte = 0
    Event Explosion(ByRef b As Byte)
    Dim WithEvents Cl As InitOnly4_14
    Sub New()
        Cl = Me
        RaiseEvent Explosion(x)
        Console.Write(x) ' "shared member on shared new
    End Sub

    Sub Bang(ByRef b As Byte) Handles Cl.Explosion
        b = 1
    End Sub
End Class
    </file>
    </compilation>, expectedOutput:="1")
        End Sub

        <Fact(), WorkItem(543612, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543612")>
        Public Sub ObsoleteRaiseEvent()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
            <![CDATA[
Option Explicit On
Imports System

Delegate Sub D1(ByVal a As Integer)
Class C0
    Class C1
        Inherits C0
        <Obsolete("Event Obsolete")>
        Custom Event E3 As D1
            AddHandler(ByVal value As D1)
            End AddHandler
            RemoveHandler(ByVal value As D1)
            End RemoveHandler
            RaiseEvent(ByVal a As Integer)
            End RaiseEvent
        End Event
    End Class
End Class
Module Module1
    Sub Main()
        Dim o1 As New C0.C1
        Dim E3Info As Reflection.EventInfo = GetType(C0.C1).GetEvent("E3")
        If E3Info.GetRaiseMethod(True) Is Nothing Then
            Console.WriteLine("FAILED")
        Else
            For Each Attr As Attribute In E3Info.GetRaiseMethod(True).GetCustomAttributes(False)
                Console.WriteLine("Raise - " & Attr.ToString & CType(Attr, ObsoleteAttribute).Message)
            Next
        End If
    End Sub
End Module
]]>
        </file>
    </compilation>, expectedOutput:="")
        End Sub

        <Fact(), WorkItem(545428, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545428")>
        Public Sub AddHandlerConflictingLocal()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
            <![CDATA[
Option Explicit On
Imports System

Module Module1
    Event e As Action(Of String)
 
    Sub Main()
        ' Handler Case
        AddHandler e, New Action(Of Object)(Function(o As Object) o)
 
        Dim e As Integer
        Console.WriteLine(e.GetType)
    End Sub
End Module

]]>
        </file>
    </compilation>, expectedOutput:="System.Int32")
        End Sub

        <Fact(), WorkItem(546055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546055")>
        Public Sub AddHandlerEventNameLookupViaImport()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">
            <![CDATA[
Option Explicit Off
Option Strict Off

Imports System
Imports cls

Class cls
    Public Shared Event ev1(x As String)

    Public Shared Sub RaiseEv1()
        RaiseEvent ev1("hello from ev1")
    End Sub
End Class

Module Program
    Sub goo(ByVal x As String)
        Console.WriteLine("{0}", x)
    End Sub

    Sub Main(args As String())
        AddHandler ev1, AddressOf goo 
        AddHandler ev1, AddressOf goo
        RaiseEv1()
    End Sub
End Module

]]>
        </file>
    </compilation>, expectedOutput:=<![CDATA[
hello from ev1
hello from ev1
]]>)
        End Sub

        <Fact>
        <WorkItem(529574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems?_a=edit&id=529574")>
        Public Sub TestCrossLanguageOptionalAndParamarray1()
            Dim csCompilation = CreateCSharpCompilation("CS",
            <![CDATA[public class CSClass
{
    public delegate int bar(string x = "", params int[] y);
    public event bar ev;
    public void raise()
    {
        ev("hi", 1, 2, 3);
    }
}]]>, compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

            csCompilation.VerifyDiagnostics()
            Dim vbCompilation = CreateVisualBasicCompilation("VB",
            <![CDATA[
option strict off

Imports System
Public Class VBClass : Inherits CSClass
    Public WithEvents w As CSClass = New CSClass
    Function Goo(x As String) Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
    Function Goo(x As String, ParamArray y() As Integer) Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
    Function Goo2(Optional x As String = "") Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
    Function Goo2(x As String, y() As Integer) Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
End Class
Public Module Program
    Sub Main()
        Dim x = New VBClass
        x.raise()
        x.w.raise()
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csCompilation})
            ' WARNING: Roslyn compiler produced errors while Native compiler didn't. This is an intentional breaking change, see associated bug. 
            vbCompilation.AssertTheseDiagnostics(
<expected>
BC31029: Method 'Goo' cannot handle event 'ev' because they do not have a compatible signature.
    Function Goo(x As String) Handles w.ev, MyBase.ev, MyClass.ev
                                        ~~
BC31029: Method 'Goo' cannot handle event 'ev' because they do not have a compatible signature.
    Function Goo(x As String) Handles w.ev, MyBase.ev, MyClass.ev
                                                   ~~
BC31029: Method 'Goo' cannot handle event 'ev' because they do not have a compatible signature.
    Function Goo(x As String) Handles w.ev, MyBase.ev, MyClass.ev
                                                               ~~
BC31029: Method 'Goo2' cannot handle event 'ev' because they do not have a compatible signature.
    Function Goo2(Optional x As String = "") Handles w.ev, MyBase.ev, MyClass.ev
                                                       ~~
BC31029: Method 'Goo2' cannot handle event 'ev' because they do not have a compatible signature.
    Function Goo2(Optional x As String = "") Handles w.ev, MyBase.ev, MyClass.ev
                                                                  ~~
BC31029: Method 'Goo2' cannot handle event 'ev' because they do not have a compatible signature.
    Function Goo2(Optional x As String = "") Handles w.ev, MyBase.ev, MyClass.ev
                                                                              ~~
</expected>)
        End Sub

        <Fact()>
        Public Sub TestCrossLanguageOptionalAndParamarray2()
            Dim csCompilation = CreateCSharpCompilation("CS",
            <![CDATA[public class CSClass
{
    public delegate int bar(string x = "");
    public event bar ev;
    public void raise()
    {
        ev("hi");
    }
}]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()
            Dim vbCompilation = CreateVisualBasicCompilation("VB",
            <![CDATA[Imports System
Public Class VBClass : Inherits CSClass
    Public WithEvents w As CSClass = New CSClass
    Function Goo(x As String) Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
    Function Goo(x As String, ParamArray y() As Integer) Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
    Function Goo2(Optional x As String = "") Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
    Function Goo2(ParamArray x() As String) Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
    Function Goo2(x As String, Optional y As Integer = 0) Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
    Function Goo3(Optional x As String = "", Optional y As Integer = 0) Handles w.ev, MyBase.ev, MyClass.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
End Class
Public Module Program
    Sub Main()
        Dim x = New VBClass
        x.raise()
        x.w.raise()
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csCompilation})
            ' WARNING: Binaries compiled with Native and Roslyn compilers produced different outputs.
            ' Below baseline is the output produced by the binary compiled with the Roslyn since
            ' in the native case output is not documented (depends on hashtable ordering).
            Dim vbVerifier = CompileAndVerify(vbCompilation,
                expectedOutput:=<![CDATA[hi
PASS
hi
PASS
hi
PASS
hi
PASS
hi
PASS
hi
PASS
System.String[]
PASS
System.String[]
PASS
hi
PASS
hi
PASS
hi
PASS
hi
PASS
hi
PASS
hi
PASS
hi
PASS
System.String[]
PASS
hi
PASS
hi
PASS
]]>)
            vbVerifier.VerifyDiagnostics()
        End Sub

        <Fact()>
        Public Sub TestCrossLanguageOptionalAndPAramarray3()
            Dim csCompilation = CreateCSharpCompilation("CS",
            <![CDATA[public class CSClass
{
    public delegate int bar(params int[] y);
    public event bar ev;
    public void raise()
    {
        ev(1, 2, 3);
    }
}]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()
            Dim vbCompilation = CreateVisualBasicCompilation("VB",
            <![CDATA[Imports System
Public Class VBClass : Inherits CSClass
    Public WithEvents w As CSClass = New CSClass
    Function Goo(x As Integer()) Handles w.ev, MyBase.ev, Me.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
    Function Goo2(ParamArray x As Integer()) Handles w.ev, MyBase.ev, Me.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
End Class
Public Module Program
    Sub Main()
        Dim x = New VBClass
        x.raise()
        x.w.Raise()
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csCompilation})
            Dim vbVerifier = CompileAndVerify(vbCompilation,
                expectedOutput:=<![CDATA[System.Int32[]
PASS
System.Int32[]
PASS
System.Int32[]
PASS
System.Int32[]
PASS
System.Int32[]
PASS
System.Int32[]
PASS
]]>)
            vbVerifier.VerifyDiagnostics()
        End Sub

        <Fact, WorkItem(545257, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545257")>
        Public Sub TestCrossLanguageOptionalAndParamarray_Error1()
            Dim csCompilation = CreateCSharpCompilation("CS",
            <![CDATA[public class CSClass
{
    public delegate int bar(params int[] y);
    public event bar ev;
    public void raise()
    {
        ev(1, 2, 3);
    }
}]]>,
                compilationOptions:=New Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            csCompilation.VerifyDiagnostics()
            Dim vbCompilation = CreateVisualBasicCompilation("VB",
            <![CDATA[Imports System
Public Class VBClass : Inherits CSClass
    Public WithEvents w As CSClass = New CSClass
    Function Goo2(x As Integer) Handles w.ev, MyBase.ev, Me.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
    Function Goo2(x As Integer, Optional y As Integer = 1) Handles w.ev, MyBase.ev, Me.ev
        Console.WriteLine(x)
        Console.WriteLine("PASS")
        Return 0
    End Function
End Class
Public Module Program
    Sub Main()
        Dim x = New VBClass
        x.raise()
        x.w.Raise()
    End Sub
End Module]]>,
                compilationOptions:=New VisualBasicCompilationOptions(OutputKind.ConsoleApplication),
                referencedCompilations:={csCompilation})
            vbCompilation.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo2", "ev"),
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo2", "ev"),
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo2", "ev"),
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo2", "ev"),
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo2", "ev"),
                Diagnostic(ERRID.ERR_EventHandlerSignatureIncompatible2, "ev").WithArguments("Goo2", "ev"))
        End Sub

        <Fact()>
        Public Sub WithEventsProperty()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">

Imports System
Imports System.ComponentModel

Namespace Project1
    Module m1
        Public Sub main()
            Dim c = New Sink
            Dim s = New OuterClass
            c.x = s
            s.Test()
        End Sub
    End Module

    Class EventSource
        Public Event MyEvent()
        Sub test()
            RaiseEvent MyEvent()
        End Sub
    End Class


    Class OuterClass

        Private SubObject As New EventSource

        &lt;DesignOnly(True)> _
        &lt;DesignerSerializationVisibility(DesignerSerializationVisibility.Content)> _
        Public Property SomeProperty() As EventSource
            Get
                Console.Write("#Get#")
                Return SubObject
            End Get
            Set(value As EventSource)

            End Set
        End Property


        Sub Test()
            SubObject.test()
        End Sub
    End Class

    Class Sink

        Public WithEvents x As OuterClass
        Sub goo() Handles x.SomeProperty.MyEvent

            Console.Write("Handled Event On SubObject!")
        End Sub

        Sub test()
            x.Test()
        End Sub
        Sub New()
            x = New OuterClass
        End Sub
    End Class
    '.....
End Namespace


    </file>
    </compilation>, expectedOutput:="#Get##Get##Get#Handled Event On SubObject!").
                VerifyIL("Project1.Sink.set_x(Project1.OuterClass)",
            <![CDATA[
{
  // Code size       65 (0x41)
  .maxstack  2
  .locals init (Project1.EventSource.MyEventEventHandler V_0,
  Project1.OuterClass V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub Project1.Sink.goo()"
  IL_0007:  newobj     "Sub Project1.EventSource.MyEventEventHandler..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldfld      "Project1.Sink._x As Project1.OuterClass"
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  brfalse.s  IL_0023
  IL_0017:  ldloc.1
  IL_0018:  callvirt   "Function Project1.OuterClass.get_SomeProperty() As Project1.EventSource"
  IL_001d:  ldloc.0
  IL_001e:  callvirt   "Sub Project1.EventSource.remove_MyEvent(Project1.EventSource.MyEventEventHandler)"
  IL_0023:  ldarg.0
  IL_0024:  ldarg.1
  IL_0025:  stfld      "Project1.Sink._x As Project1.OuterClass"
  IL_002a:  ldarg.0
  IL_002b:  ldfld      "Project1.Sink._x As Project1.OuterClass"
  IL_0030:  stloc.1
  IL_0031:  ldloc.1
  IL_0032:  brfalse.s  IL_0040
  IL_0034:  ldloc.1
  IL_0035:  callvirt   "Function Project1.OuterClass.get_SomeProperty() As Project1.EventSource"
  IL_003a:  ldloc.0
  IL_003b:  callvirt   "Sub Project1.EventSource.add_MyEvent(Project1.EventSource.MyEventEventHandler)"
  IL_0040:  ret
}
    ]]>)
        End Sub

        <Fact()>
        Public Sub WithEventsPropertySharedEvent()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">

Imports System
Imports System.ComponentModel

Namespace Project1
    Module m1
        Public Sub main()
            Dim c = New Sink
            Dim s = New OuterClass
            c.x = s
            s.Test()
        End Sub
    End Module

    Class EventSource
        Public Shared Event MyEvent()
        Sub test()
            RaiseEvent MyEvent()
        End Sub
    End Class


    Class OuterClass

        Private SubObject As New EventSource

        &lt;DesignOnly(True)> _
        &lt;DesignerSerializationVisibility(DesignerSerializationVisibility.Content)> _
        Public Property SomeProperty() As EventSource
            Get
                Console.Write("#Get#")
                Return SubObject
            End Get
            Set(value As EventSource)

            End Set
        End Property


        Sub Test()
            SubObject.test()
        End Sub
    End Class

    Class Sink

        Public WithEvents x As OuterClass
        Sub goo() Handles x.SomeProperty.MyEvent

            Console.Write("Handled Event On SubObject!")
        End Sub

        Sub test()
            x.Test()
        End Sub
        Sub New()
            x = New OuterClass
        End Sub
    End Class
    '.....
End Namespace


    </file>
    </compilation>, expectedOutput:="Handled Event On SubObject!").
                VerifyIL("Project1.Sink.set_x(Project1.OuterClass)",
            <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (Project1.EventSource.MyEventEventHandler V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub Project1.Sink.goo()"
  IL_0007:  newobj     "Sub Project1.EventSource.MyEventEventHandler..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldfld      "Project1.Sink._x As Project1.OuterClass"
  IL_0013:  brfalse.s  IL_001b
  IL_0015:  ldloc.0
  IL_0016:  call       "Sub Project1.EventSource.remove_MyEvent(Project1.EventSource.MyEventEventHandler)"
  IL_001b:  ldarg.0
  IL_001c:  ldarg.1
  IL_001d:  stfld      "Project1.Sink._x As Project1.OuterClass"
  IL_0022:  ldarg.0
  IL_0023:  ldfld      "Project1.Sink._x As Project1.OuterClass"
  IL_0028:  brfalse.s  IL_0030
  IL_002a:  ldloc.0
  IL_002b:  call       "Sub Project1.EventSource.add_MyEvent(Project1.EventSource.MyEventEventHandler)"
  IL_0030:  ret
}
    ]]>)
        End Sub

        <Fact()>
        Public Sub WithEventsPropertySharedProperty()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">

Imports System
Imports System.ComponentModel

Namespace Project1
    Module m1
        Public Sub main()
            Dim c = New Sink
            Dim s = New OuterClass
            c.x = s
            s.Test()
        End Sub
    End Module

    Class EventSource
        Public Event MyEvent()
        Sub test()
            RaiseEvent MyEvent()
        End Sub
    End Class


    Class OuterClass

        Private shared SubObject As New EventSource

        &lt;DesignOnly(True)> _
        &lt;DesignerSerializationVisibility(DesignerSerializationVisibility.Content)> _
        Public shared Property SomeProperty() As EventSource
            Get
                Console.Write("#Get#")
                Return SubObject
            End Get
            Set(value As EventSource)

            End Set
        End Property


        Sub Test()
            SubObject.test()
        End Sub
    End Class

    Class Sink

        Public WithEvents x As OuterClass
        Sub goo() Handles x.SomeProperty.MyEvent

            Console.Write("Handled Event On SubObject!")
        End Sub

        Sub test()
            x.Test()
        End Sub
        Sub New()
            x = New OuterClass
        End Sub
    End Class
    '.....
End Namespace


    </file>
    </compilation>, expectedOutput:="#Get##Get##Get#Handled Event On SubObject!").
                VerifyIL("Project1.Sink.set_x(Project1.OuterClass)",
            <![CDATA[
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (Project1.EventSource.MyEventEventHandler V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub Project1.Sink.goo()"
  IL_0007:  newobj     "Sub Project1.EventSource.MyEventEventHandler..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldfld      "Project1.Sink._x As Project1.OuterClass"
  IL_0013:  brfalse.s  IL_0020
  IL_0015:  call       "Function Project1.OuterClass.get_SomeProperty() As Project1.EventSource"
  IL_001a:  ldloc.0
  IL_001b:  callvirt   "Sub Project1.EventSource.remove_MyEvent(Project1.EventSource.MyEventEventHandler)"
  IL_0020:  ldarg.0
  IL_0021:  ldarg.1
  IL_0022:  stfld      "Project1.Sink._x As Project1.OuterClass"
  IL_0027:  ldarg.0
  IL_0028:  ldfld      "Project1.Sink._x As Project1.OuterClass"
  IL_002d:  brfalse.s  IL_003a
  IL_002f:  call       "Function Project1.OuterClass.get_SomeProperty() As Project1.EventSource"
  IL_0034:  ldloc.0
  IL_0035:  callvirt   "Sub Project1.EventSource.add_MyEvent(Project1.EventSource.MyEventEventHandler)"
  IL_003a:  ret
}
    ]]>)
        End Sub

        <Fact()>
        Public Sub WithEventsPropertyAllShared()
            CompileAndVerify(
    <compilation>
        <file name="a.vb">

Imports System
Imports System.ComponentModel

Namespace Project1
    Module m1
        Public Sub main()
            Dim c = New Sink
            Dim s = New OuterClass
            c.x = s
            s.Test()
        End Sub
    End Module

    Class EventSource
        Public shared Event MyEvent()
        Sub test()
            RaiseEvent MyEvent()
        End Sub
    End Class


    Class OuterClass

        Private SubObject As New EventSource

        &lt;DesignOnly(True)> _
        &lt;DesignerSerializationVisibility(DesignerSerializationVisibility.Content)> _
        Public Property SomeProperty() As EventSource
            Get
                Console.Write("#Get#")
                Return SubObject
            End Get
            Set(value As EventSource)

            End Set
        End Property


        Sub Test()
            SubObject.test()
        End Sub
    End Class

    Class Sink

        Public WithEvents x As OuterClass
        Sub goo() Handles x.SomeProperty.MyEvent

            Console.Write("Handled Event On SubObject!")
        End Sub

        Sub test()
            x.Test()
        End Sub
        Sub New()
            x = New OuterClass
        End Sub
    End Class
    '.....
End Namespace


    </file>
    </compilation>, expectedOutput:="Handled Event On SubObject!").
                VerifyIL("Project1.Sink.set_x(Project1.OuterClass)",
            <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (Project1.EventSource.MyEventEventHandler V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub Project1.Sink.goo()"
  IL_0007:  newobj     "Sub Project1.EventSource.MyEventEventHandler..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldfld      "Project1.Sink._x As Project1.OuterClass"
  IL_0013:  brfalse.s  IL_001b
  IL_0015:  ldloc.0
  IL_0016:  call       "Sub Project1.EventSource.remove_MyEvent(Project1.EventSource.MyEventEventHandler)"
  IL_001b:  ldarg.0
  IL_001c:  ldarg.1
  IL_001d:  stfld      "Project1.Sink._x As Project1.OuterClass"
  IL_0022:  ldarg.0
  IL_0023:  ldfld      "Project1.Sink._x As Project1.OuterClass"
  IL_0028:  brfalse.s  IL_0030
  IL_002a:  ldloc.0
  IL_002b:  call       "Sub Project1.EventSource.add_MyEvent(Project1.EventSource.MyEventEventHandler)"
  IL_0030:  ret
}
    ]]>)
        End Sub

        <WorkItem(1069554, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1069554")>
        <Fact>
        Public Sub LocalDefinitionInEventHandler()
            Dim c = CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System

Class C
    Public Custom Event E As Action
        AddHandler(value As Action)
            Dim f = 1
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
    </file>
    </compilation>, options:=TestOptions.DebugDll)

            c.VerifyIL("C.add_E", <![CDATA[
{
  // Code size        4 (0x4)
  .maxstack  1
  .locals init (Integer V_0) //f
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  ret
}
]]>)
        End Sub

        <WorkItem(1069554, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1069554")>
        <Fact>
        Public Sub ClosureInEventHandler()
            Dim c = CompileAndVerify(
    <compilation>
        <file name="a.vb">
Imports System

Class C
    Public Custom Event E As Action
        AddHandler(value As Action)
            Dim f = Sub() 
                        value()
                    End Sub
        End AddHandler
        RemoveHandler(value As Action)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event
End Class
    </file>
    </compilation>, options:=TestOptions.DebugDll)

            c.VerifyIL("C.add_E", <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (C._Closure$__2-0 V_0, //$VB$Closure_0
                VB$AnonymousDelegate_0 V_1) //f
  IL_0000:  nop
  IL_0001:  newobj     "Sub C._Closure$__2-0..ctor()"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      "C._Closure$__2-0.$VB$Local_value As System.Action"
  IL_000e:  ldloc.0
  IL_000f:  ldftn      "Sub C._Closure$__2-0._Lambda$__0()"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_001a:  stloc.1
  IL_001b:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(7659, "https://github.com/dotnet/roslyn/issues/7659")>
        Public Sub HandlesOnMultipleLevels_01()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Button
    Event Click As System.Action

    Sub Raise()
        RaiseEvent Click()
    End Sub
End Class

Public Class MainBase1

    Protected WithEvents Button1 As New Button()

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(1)
    End Sub
End Class

Public Class MainBase2
    Inherits MainBase1

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(2)
    End Sub
End Class

Public Class MainBase3
    Inherits MainBase2

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(3)
    End Sub
End Class

Public Class Main
    Inherits MainBase3

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine("4")
    End Sub

    Shared Sub Main()
        Dim m = New Main()
        m.Button1.Raise()
    End Sub
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
"1
2
3
4")

            verifier.VerifyIL("MainBase2.set_Button1", <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (System.Action V_0,
                Button V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub MainBase2.Button1_Click()"
  IL_0007:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  call       "Function MainBase1.get_Button1() As Button"
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  brfalse.s  IL_001e
  IL_0017:  ldloc.1
  IL_0018:  ldloc.0
  IL_0019:  callvirt   "Sub Button.remove_Click(System.Action)"
  IL_001e:  ldarg.0
  IL_001f:  ldarg.1
  IL_0020:  call       "Sub MainBase1.set_Button1(Button)"
  IL_0025:  ldarg.0
  IL_0026:  call       "Function MainBase1.get_Button1() As Button"
  IL_002b:  stloc.1
  IL_002c:  ldloc.1
  IL_002d:  brfalse.s  IL_0036
  IL_002f:  ldloc.1
  IL_0030:  ldloc.0
  IL_0031:  callvirt   "Sub Button.add_Click(System.Action)"
  IL_0036:  ret
}
]]>)

            verifier.VerifyIL("MainBase3.set_Button1", <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (System.Action V_0,
                Button V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub MainBase3.Button1_Click()"
  IL_0007:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  call       "Function MainBase2.get_Button1() As Button"
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  brfalse.s  IL_001e
  IL_0017:  ldloc.1
  IL_0018:  ldloc.0
  IL_0019:  callvirt   "Sub Button.remove_Click(System.Action)"
  IL_001e:  ldarg.0
  IL_001f:  ldarg.1
  IL_0020:  call       "Sub MainBase2.set_Button1(Button)"
  IL_0025:  ldarg.0
  IL_0026:  call       "Function MainBase2.get_Button1() As Button"
  IL_002b:  stloc.1
  IL_002c:  ldloc.1
  IL_002d:  brfalse.s  IL_0036
  IL_002f:  ldloc.1
  IL_0030:  ldloc.0
  IL_0031:  callvirt   "Sub Button.add_Click(System.Action)"
  IL_0036:  ret
}
]]>)

            verifier.VerifyIL("Main.set_Button1", <![CDATA[
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (System.Action V_0,
                Button V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub Main.Button1_Click()"
  IL_0007:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  call       "Function MainBase3.get_Button1() As Button"
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  brfalse.s  IL_001e
  IL_0017:  ldloc.1
  IL_0018:  ldloc.0
  IL_0019:  callvirt   "Sub Button.remove_Click(System.Action)"
  IL_001e:  ldarg.0
  IL_001f:  ldarg.1
  IL_0020:  call       "Sub MainBase3.set_Button1(Button)"
  IL_0025:  ldarg.0
  IL_0026:  call       "Function MainBase3.get_Button1() As Button"
  IL_002b:  stloc.1
  IL_002c:  ldloc.1
  IL_002d:  brfalse.s  IL_0036
  IL_002f:  ldloc.1
  IL_0030:  ldloc.0
  IL_0031:  callvirt   "Sub Button.add_Click(System.Action)"
  IL_0036:  ret
}
]]>)
            verifier.VerifyIL("MainBase2.get_Button1", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Button V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function MainBase1.get_Button1() As Button"
  IL_0006:  ret
}
]]>)
            verifier.VerifyIL("MainBase3.get_Button1", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Button V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function MainBase2.get_Button1() As Button"
  IL_0006:  ret
}
]]>)
            verifier.VerifyIL("Main.get_Button1", <![CDATA[
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Button V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       "Function MainBase3.get_Button1() As Button"
  IL_0006:  ret
}
]]>)
        End Sub

        <Fact>
        <WorkItem(7659, "https://github.com/dotnet/roslyn/issues/7659")>
        Public Sub HandlesOnMultipleLevels_02()

            Dim source1 =
<compilation>
    <file name="a.vb">
Public Class Button
    Event Click As System.Action

    Sub Raise()
        RaiseEvent Click()
    End Sub
End Class

Public Class MainBase1

    Protected WithEvents Button1 As New Button()

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(1)
    End Sub
End Class

Public Class MainBase2
    Inherits MainBase1

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(2)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(source1, TestOptions.ReleaseDll)

            Dim source2 =
<compilation>
    <file name="a.vb">
Public Class MainBase3
    Inherits MainBase2

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(3)
    End Sub
End Class

Public Class Main
    Inherits MainBase3

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine("4")
    End Sub

    Shared Sub Main()
        Dim m = New Main()
        m.Button1.Raise()
    End Sub
End Class
    </file>
</compilation>

            Dim compilation2 = CreateCompilationWithMscorlib40AndVBRuntime(source2, {compilation1.EmitToImageReference()}, TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
"1
2
3
4")

            compilation2 = CreateCompilationWithMscorlib45AndVBRuntime(source2, {compilation1.ToMetadataReference()}, TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
"1
2
3
4")

            compilation2 = CreateCompilationWithMscorlib40AndVBRuntime(source2, {compilation1.ToMetadataReference()}, TestOptions.ReleaseExe)

            CompileAndVerify(compilation2, expectedOutput:=
"1
2
3
4")
        End Sub

        <Fact>
        <WorkItem(7659, "https://github.com/dotnet/roslyn/issues/7659")>
        Public Sub HandlesOnMultipleLevels_03()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Imports System
Imports System.ComponentModel

Public Class EventSource
    Public Event MyEvent()
    Sub test()
        RaiseEvent MyEvent()
    End Sub
End Class


Public Class OuterClass

    Private SubObject As New EventSource

    &lt;DesignOnly(True)>
    &lt;DesignerSerializationVisibility(DesignerSerializationVisibility.Content)>
    Public Property SomeProperty() As EventSource
        Get
            Return SubObject
        End Get
        Set(value As EventSource)

        End Set
    End Property


    Sub Test()
        SubObject.test()
    End Sub
End Class


Public Class MainBase1

    Public WithEvents Button1 As New OuterClass()

    Private Sub Button1_Click() Handles Button1.SomeProperty.MyEvent
        System.Console.WriteLine(1)
    End Sub
End Class

Public Class MainBase2
    Inherits MainBase1

    Private Sub Button1_Click() Handles Button1.SomeProperty.MyEvent
        System.Console.WriteLine(2)
    End Sub
End Class

Public Class MainBase3
    Inherits MainBase2

    Private Sub Button1_Click() Handles Button1.SomeProperty.MyEvent
        System.Console.WriteLine(3)
    End Sub
End Class

Public Class Main
    Inherits MainBase3

    Private Sub Button1_Click() Handles Button1.SomeProperty.MyEvent
        System.Console.WriteLine("4")
    End Sub
End Class

Module Module1
    Sub Main()
        Dim m = New Main()
        m.Button1.Test()
    End Sub
End Module
    </file>
</compilation>, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:=
"1
2
3
4")
        End Sub

        <Fact>
        <WorkItem(7659, "https://github.com/dotnet/roslyn/issues/7659")>
        Public Sub HandlesOnMultipleLevels_04()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Button
    Event Click As System.Action

    Sub Raise()
        RaiseEvent Click()
    End Sub
End Class

Public Class MainBase1

    Protected WithEvents Button1 As New Button()

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(1)
    End Sub
End Class

Public Class MainBase2
    Inherits MainBase1

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(2)
    End Sub
End Class

Public Class MainBase3
    Inherits MainBase2

    Protected Overrides Property Button1 As Button
        Get
            Return MyBase.Button1
        End Get
        Set
            System.Console.WriteLine("3")
            MyBase.Button1 = Value
        End Set
    End Property
End Class

Public Class Main
    Inherits MainBase3

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine("4")
    End Sub

    Shared Sub Main()
        Dim m = New Main()
        m.Button1.Raise()
    End Sub
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            compilation.AssertTheseDiagnostics(
<expected>
BC30284: property 'Button1' cannot be declared 'Overrides' because it does not override a property in a base class.
    Protected Overrides Property Button1 As Button
                                 ~~~~~~~
BC40004: property 'Button1' conflicts with WithEvents variable 'Button1' in the base class 'MainBase1' and should be declared 'Shadows'.
    Protected Overrides Property Button1 As Button
                                 ~~~~~~~
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
    Private Sub Button1_Click() Handles Button1.Click
                                        ~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(7659, "https://github.com/dotnet/roslyn/issues/7659")>
        Public Sub HandlesOnMultipleLevels_05()

            Dim source1 =
<compilation>
    <file name="a.vb">
Public Class Button
    Event Click As System.Action

    Sub Raise()
        RaiseEvent Click()
    End Sub
End Class

Public Class MainBase1

    Protected WithEvents Button1 As New Button()

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(1)
    End Sub
End Class

Public Class MainBase2
    Inherits MainBase1

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(2)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(source1, TestOptions.ReleaseDll)

            Dim source2 =
<compilation>
    <file name="a.vb">
Public Class MainBase3
    Inherits MainBase2

    Protected Overrides Property Button1 As Button
        Get
            Return MyBase.Button1
        End Get
        Set
            System.Console.WriteLine("3")
            MyBase.Button1 = Value
        End Set
    End Property
End Class

Public Class Main
    Inherits MainBase3

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine("4")
    End Sub

    Shared Sub Main()
        Dim m = New Main()
        m.Button1.Raise()
    End Sub
End Class
    </file>
</compilation>

            Dim compilation2 = CreateCompilationWithMscorlib40AndVBRuntime(source2, {compilation1.EmitToImageReference()}, TestOptions.ReleaseExe)

            compilation2.AssertTheseDiagnostics(
<expected>
BC30284: property 'Button1' cannot be declared 'Overrides' because it does not override a property in a base class.
    Protected Overrides Property Button1 As Button
                                 ~~~~~~~
BC40004: property 'Button1' conflicts with WithEvents variable 'Button1' in the base class 'MainBase2' and should be declared 'Shadows'.
    Protected Overrides Property Button1 As Button
                                 ~~~~~~~
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
    Private Sub Button1_Click() Handles Button1.Click
                                        ~~~~~~~
</expected>)

            compilation2 = CreateCompilationWithMscorlib45AndVBRuntime(source2, {compilation1.ToMetadataReference()}, TestOptions.ReleaseExe)
            Dim expected =
<expected>
BC30284: property 'Button1' cannot be declared 'Overrides' because it does not override a property in a base class.
    Protected Overrides Property Button1 As Button
                                 ~~~~~~~
BC40004: property 'Button1' conflicts with WithEvents variable 'Button1' in the base class 'MainBase1' and should be declared 'Shadows'.
    Protected Overrides Property Button1 As Button
                                 ~~~~~~~
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
    Private Sub Button1_Click() Handles Button1.Click
                                        ~~~~~~~
</expected>
            compilation2.AssertTheseDiagnostics(expected)

            compilation2 = CreateCompilationWithMscorlib40AndVBRuntime(source2, {compilation1.ToMetadataReference()}, TestOptions.ReleaseExe)
            compilation2.AssertTheseDiagnostics(expected)
        End Sub

        <Fact>
        <WorkItem(7659, "https://github.com/dotnet/roslyn/issues/7659")>
        Public Sub HandlesOnMultipleLevels_06()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Button
    Event Click As System.Action

    Sub Raise()
        RaiseEvent Click()
    End Sub
End Class

Public Class MainBase1

    Protected WithEvents Button1 As New Button()

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(1)
    End Sub
End Class

Public Class MainBase3
    Inherits MainBase1

    Protected Overrides Property Button1 As Button
        Get
            Return MyBase.Button1
        End Get
        Set
            System.Console.WriteLine("3")
            MyBase.Button1 = Value
        End Set
    End Property
End Class

Public Class Main
    Inherits MainBase3

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine("4")
    End Sub

    Shared Sub Main()
        Dim m = New Main()
        m.Button1.Raise()
    End Sub
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            compilation.AssertTheseDiagnostics(
<expected>
BC30284: property 'Button1' cannot be declared 'Overrides' because it does not override a property in a base class.
    Protected Overrides Property Button1 As Button
                                 ~~~~~~~
BC40004: property 'Button1' conflicts with WithEvents variable 'Button1' in the base class 'MainBase1' and should be declared 'Shadows'.
    Protected Overrides Property Button1 As Button
                                 ~~~~~~~
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
    Private Sub Button1_Click() Handles Button1.Click
                                        ~~~~~~~
</expected>)
        End Sub

        <Fact>
        <WorkItem(7659, "https://github.com/dotnet/roslyn/issues/7659")>
        Public Sub HandlesOnMultipleLevels_07()

            Dim source1 =
<compilation>
    <file name="a.vb">
Public Class Button
    Event Click As System.Action

    Sub Raise()
        RaiseEvent Click()
    End Sub
End Class

Public Class MainBase1

    Protected WithEvents Button1 As New Button()

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(1)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation1 = CreateCompilationWithMscorlib40AndVBRuntime(source1, TestOptions.ReleaseDll)

            Dim source2 =
<compilation>
    <file name="a.vb">
Public Class MainBase3
    Inherits MainBase1

    Protected Overrides Property Button1 As Button
        Get
            Return MyBase.Button1
        End Get
        Set
            System.Console.WriteLine("3")
            MyBase.Button1 = Value
        End Set
    End Property
End Class

Public Class Main
    Inherits MainBase3

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine("4")
    End Sub

    Shared Sub Main()
        Dim m = New Main()
        m.Button1.Raise()
    End Sub
End Class
    </file>
</compilation>

            Dim compilation2 = CreateCompilationWithMscorlib40AndVBRuntime(source2, {compilation1.EmitToImageReference()}, TestOptions.ReleaseExe)
            Dim expected =
<expected>
BC30284: property 'Button1' cannot be declared 'Overrides' because it does not override a property in a base class.
    Protected Overrides Property Button1 As Button
                                 ~~~~~~~
BC40004: property 'Button1' conflicts with WithEvents variable 'Button1' in the base class 'MainBase1' and should be declared 'Shadows'.
    Protected Overrides Property Button1 As Button
                                 ~~~~~~~
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
    Private Sub Button1_Click() Handles Button1.Click
                                        ~~~~~~~
</expected>

            compilation2.AssertTheseDiagnostics(expected)

            compilation2 = CreateCompilationWithMscorlib45AndVBRuntime(source2, {compilation1.ToMetadataReference()}, TestOptions.ReleaseExe)
            compilation2.AssertTheseDiagnostics(expected)

            compilation2 = CreateCompilationWithMscorlib40AndVBRuntime(source2, {compilation1.ToMetadataReference()}, TestOptions.ReleaseExe)
            compilation2.AssertTheseDiagnostics(expected)
        End Sub

        <Fact>
        <WorkItem(7659, "https://github.com/dotnet/roslyn/issues/7659")>
        Public Sub HandlesOnMultipleLevels_08()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Button
    Event Click As System.Action

    Sub Raise()
        RaiseEvent Click()
    End Sub
End Class

Public Class MainBase1(Of T1)

    Protected WithEvents Button1 As New Button()

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(1)
    End Sub
End Class

Public Class MainBase2(Of T2)
    Inherits MainBase1(Of MainBase2(Of T2))

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(2)
    End Sub
End Class

Public Class MainBase3(Of T3)
    Inherits MainBase2(Of MainBase3(Of T3))

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(3)
    End Sub
End Class

Public Class Main(Of T4)
    Inherits MainBase3(Of Main(Of T4))

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine("4")
    End Sub

    Shared Sub Test()
        Dim m = New Main(Of T4)()
        m.Button1.Raise()
    End Sub
End Class

Public Class Main
    Shared Sub Main()
        Global.Main(Of Integer).Test()
    End Sub
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
"1
2
3
4")
        End Sub

        <Fact>
        <WorkItem(7659, "https://github.com/dotnet/roslyn/issues/7659")>
        <WorkItem(14104, "https://github.com/dotnet/roslyn/issues/14104")>
        <CompilerTrait(CompilerFeature.Tuples)>
        Public Sub HandlesOnMultipleLevels_09()
            Dim compilation = CreateCompilationWithMscorlib40AndVBRuntime(
<compilation>
    <file name="a.vb">
Public Class Button
    Event Click As System.Action

    Sub Raise()
        RaiseEvent Click()
    End Sub
End Class

Public Class MainBase1

    Protected WithEvents Button1 As New Button()

    Private Sub Button1_Click() Handles Button1.Click
        System.Console.WriteLine(1)
    End Sub
End Class

Namespace System
    Public Class ValueTuple(Of T1, T2)
        Inherits MainBase1

        Public Dim Item1 As T1
        Public Dim Item2 As T2

        Public Sub New(item1 As T1, item2 As T2)
            me.Item1 = item1
            me.Item2 = item2
        End Sub

        Public Sub New()
        End Sub

        Private Sub Button1_Click() Handles Button1.Click
            System.Console.WriteLine(2)
        End Sub
    End Class
End Namespace

Public Class Main
    Inherits System.ValueTuple(Of Integer, Integer)

    Private Sub Button1_Click() Handles Button1.Click ' 3
        System.Console.WriteLine("3")
    End Sub

    Shared Sub Main()
        Dim m = New Main()
        m.Button1.Raise()
    End Sub
End Class
    </file>
</compilation>, TestOptions.ReleaseExe)

#If Not ISSUE_14104_IS_FIXED Then
            compilation.AssertTheseDiagnostics(
<expected>
BC30258: Classes can inherit only from other classes.
    Inherits System.ValueTuple(Of Integer, Integer)
             ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30506: Handles clause requires a WithEvents variable defined in the containing type or one of its base types.
    Private Sub Button1_Click() Handles Button1.Click ' 3
                                        ~~~~~~~
BC30456: 'Button1' is not a member of 'Main'.
        m.Button1.Raise()
        ~~~~~~~~~
</expected>)
#Else
            Dim verifier = CompileAndVerify(compilation, expectedOutput:=
"1
2
3")
#End If
        End Sub
    End Class
End Namespace

