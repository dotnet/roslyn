' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Basic.Reference.Assemblies
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class CodeGenWinMdEvents
        Inherits BasicTestBase

        <Fact()>
        Public Sub MissingReferences_SynthesizedAccessors()
            Dim source =
                <compilation name="MissingReferences">
                    <file name="a.vb">
Class C
    Event E As System.Action
End Class
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source, OutputKind.WindowsRuntimeMetadata)

            ' 3 for the backing field and each accessor.
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of )", "MissingReferences.winmdobj"),
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", "MissingReferences.winmdobj"),
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", "MissingReferences.winmdobj"))

            ' Throws *test* exception, but does not assert or throw produce exception.
            Assert.Throws(Of CompilationVerifier.EmitException)(Sub() CompileAndVerify(comp))
        End Sub

        <Fact()>
        Public Sub MissingReferences_AddHandler()
            Dim source =
                <compilation name="MissingReferences">
                    <file name="a.vb">
Class C
    Event E As System.Action

    Sub Test()
        AddHandler E, Nothing
    End Sub
End Class
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source, OutputKind.WindowsRuntimeMetadata)

            ' 3 for the backing field and each accessor.
            ' 1 for the AddHandler statement
            comp.VerifyEmitDiagnostics(
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of )", "MissingReferences.winmdobj"),
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", "MissingReferences.winmdobj"),
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", "MissingReferences.winmdobj"))

            ' Throws *test* exception, but does not assert or throw produce exception.
            Assert.Throws(Of CompilationVerifier.EmitException)(Sub() CompileAndVerify(comp))
        End Sub

        <Fact()>
        Public Sub MissingReferences_RemoveHandler()
            Dim source =
                <compilation name="MissingReferences">
                    <file name="a.vb">
Class C
    Event E As System.Action

    Sub Test()
        RemoveHandler E, Nothing
    End Sub
End Class
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source, OutputKind.WindowsRuntimeMetadata)

            ' 3 for the backing field and each accessor.
            ' 1 for the RemoveHandler statement
            comp.VerifyEmitDiagnostics(
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of )", "MissingReferences.winmdobj"),
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", "MissingReferences.winmdobj"),
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", "MissingReferences.winmdobj"))

            ' Throws *test* exception, but does not assert or throw produce exception.
            Assert.Throws(Of CompilationVerifier.EmitException)(Sub() CompileAndVerify(comp))
        End Sub

        <Fact()>
        Public Sub MissingReferences_RaiseEvent()
            Dim source =
                <compilation name="MissingReferences">
                    <file name="a.vb">
Class C
    Event E As System.Action

    Sub Test()
        RaiseEvent E()
    End Sub
End Class
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source, OutputKind.WindowsRuntimeMetadata)

            ' 3 for the backing field and each accessor.
            ' 1 for the RaiseEvent statement
            comp.VerifyEmitDiagnostics(
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of )", "MissingReferences.winmdobj"),
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", "MissingReferences.winmdobj"),
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", "MissingReferences.winmdobj"))

            ' Throws *test* exception, but does not assert or throw produce exception.
            Assert.Throws(Of CompilationVerifier.EmitException)(Sub() CompileAndVerify(comp))
        End Sub

        <Fact()>
        Public Sub MissingReferences_RaiseEvent_MissingAccessor()
            Dim source =
                <compilation>
                    <file name="a.vb">
Class C
    Event E As System.Action

    Sub Test()
        RaiseEvent E()
    End Sub
End Class

Namespace System.Runtime.InteropServices.WindowsRuntime
    Public Structure EventRegistrationToken
    End Structure

    Public Class EventRegistrationTokenTable(Of T)
        Public Shared Function GetOrCreateEventRegistrationTokenTable(ByRef table As EventRegistrationTokenTable(Of T)) As EventRegistrationTokenTable(Of T)
            Return table
        End Function

        Public WriteOnly Property InvocationList as T
            Set (value As T)
            End Set
        End Property

        Public Function AddEventHandler(handler as T) as EventRegistrationToken
            Return Nothing
        End Function

        Public Sub RemoveEventHandler(token as EventRegistrationToken)
        End Sub
    End Class
End Namespace
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib40(source, OutputKind.WindowsRuntimeMetadata)

            ' 1 for the RaiseEvent statement
            comp.VerifyEmitDiagnostics(
                Diagnostic(ERRID.ERR_MissingRuntimeHelper, "RaiseEvent E()").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1.get_InvocationList"))

            ' Throws *test* exception, but does not assert or throw produce exception.
            Assert.Throws(Of CompilationVerifier.EmitException)(Sub() CompileAndVerify(comp))
        End Sub

        <Fact>
        Public Sub MissingReferences_HandlesClause()

            Dim source =
<compilation name="test">
    <file name="a.vb">
Imports System.Runtime.InteropServices.WindowsRuntime

Delegate Sub EventDelegate()

Class Test

    WithEvents T As Test

    Public Event E As EventDelegate

    Sub Handler() Handles Me.E, T.E

    End Sub

End Class
    </file>
</compilation>

            Dim comp = CreateCompilationWithMscorlib40(source, OutputKind.WindowsRuntimeMetadata)

            ' This test is specifically interested in the ERR_MissingRuntimeHelper errors: one for each helper times one for each handled event
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UndefinedOrEmptyNamespaceOrClass1, "System.Runtime.InteropServices.WindowsRuntime").WithArguments("System.Runtime.InteropServices.WindowsRuntime"),
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of )", "test.winmdobj"),
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", "test.winmdobj"),
                Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", "test.winmdobj"),
                Diagnostic(ERRID.ERR_MissingRuntimeHelper, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1.AddEventHandler"),
                Diagnostic(ERRID.ERR_MissingRuntimeHelper, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1.RemoveEventHandler"),
                Diagnostic(ERRID.ERR_MissingRuntimeHelper, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1.AddEventHandler"),
                Diagnostic(ERRID.ERR_MissingRuntimeHelper, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1.RemoveEventHandler"),
                Diagnostic(ERRID.HDN_UnusedImportStatement, "Imports System.Runtime.InteropServices.WindowsRuntime"))

            ' Throws *test* exception, but does not assert or throw produce exception.
            Assert.Throws(Of CompilationVerifier.EmitException)(Sub() CompileAndVerify(comp))
        End Sub

        <Fact()>
        Public Sub InstanceFieldLikeEventAccessors()
            Dim verifier = CompileAndVerify(
    <compilation>
        <file name="a.vb">
Class C
    Event E As System.Action
End Class
    </file>
    </compilation>, WinRtRefs, options:=TestOptions.ReleaseWinMD)

            verifier.VerifyIL("C.add_E", <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "C.EEvent As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action)"
  IL_0006:  call       "Function System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action).GetOrCreateEventRegistrationTokenTable(ByRef System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action)) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action)"
  IL_000b:  ldarg.1
  IL_000c:  callvirt   "Function System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action).AddEventHandler(System.Action) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0011:  ret
}
]]>)

            verifier.VerifyIL("C.remove_E", <![CDATA[
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "C.EEvent As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action)"
  IL_0006:  call       "Function System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action).GetOrCreateEventRegistrationTokenTable(ByRef System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action)) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action)"
  IL_000b:  ldarg.1
  IL_000c:  callvirt   "Sub System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action).RemoveEventHandler(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0011:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub SharedFieldLikeEventAccessors()
            Dim verifier = CompileAndVerify(
    <compilation>
        <file name="a.vb">
Class C
    Shared Event E As System.Action
End Class
    </file>
    </compilation>, WinRtRefs, options:=TestOptions.ReleaseWinMD)

            verifier.VerifyIL("C.add_E", <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldsflda    "C.EEvent As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action)"
  IL_0005:  call       "Function System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action).GetOrCreateEventRegistrationTokenTable(ByRef System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action)) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action)"
  IL_000a:  ldarg.0
  IL_000b:  callvirt   "Function System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action).AddEventHandler(System.Action) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0010:  ret
}
]]>)

            verifier.VerifyIL("C.remove_E", <![CDATA[
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldsflda    "C.EEvent As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action)"
  IL_0005:  call       "Function System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action).GetOrCreateEventRegistrationTokenTable(ByRef System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action)) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action)"
  IL_000a:  ldarg.0
  IL_000b:  callvirt   "Sub System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action).RemoveEventHandler(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0010:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub AddAndRemoveHandlerStatements()
            Dim verifier = CompileAndVerify(
    <compilation>
        <file name="a.vb">
Class C
    Public Event InstanceEvent As System.Action
    Public Shared Event SharedEvent As System.Action
End Class

Class D
    Private c1 as C

    Sub InstanceAdd()
        AddHandler c1.InstanceEvent, AddressOf Action
    End Sub

    Sub InstanceRemove()
        RemoveHandler c1.InstanceEvent, AddressOf Action
    End Sub

    Sub SharedAdd()
        AddHandler C.SharedEvent, AddressOf Action
    End Sub

    Sub SharedRemove()
        RemoveHandler C.SharedEvent, AddressOf Action
    End Sub

    Shared Sub Action()
    End Sub
End Class
    </file>
    </compilation>, WinRtRefs, options:=TestOptions.ReleaseWinMD)

            verifier.VerifyIL("D.InstanceAdd", <![CDATA[
{
  // Code size       49 (0x31)
  .maxstack  4
  .locals init (C V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "D.c1 As C"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldftn      "Sub C.add_InstanceEvent(System.Action) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_000e:  newobj     "Sub System.Func(Of System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0013:  ldloc.0
  IL_0014:  ldftn      "Sub C.remove_InstanceEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_001a:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_001f:  ldnull
  IL_0020:  ldftn      "Sub D.Action()"
  IL_0026:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_002b:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of System.Action)(System.Func(Of System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action)"
  IL_0030:  ret
}
]]>)

            verifier.VerifyIL("D.InstanceRemove", <![CDATA[
{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "D.c1 As C"
  IL_0006:  ldftn      "Sub C.remove_InstanceEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_000c:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0011:  ldnull
  IL_0012:  ldftn      "Sub D.Action()"
  IL_0018:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_001d:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of System.Action)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action)"
  IL_0022:  ret
}
]]>)

            verifier.VerifyIL("D.SharedAdd", <![CDATA[
{
  // Code size       42 (0x2a)
  .maxstack  4
  IL_0000:  ldnull
  IL_0001:  ldftn      "Sub C.add_SharedEvent(System.Action) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0007:  newobj     "Sub System.Func(Of System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_000c:  ldnull
  IL_000d:  ldftn      "Sub C.remove_SharedEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0013:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0018:  ldnull
  IL_0019:  ldftn      "Sub D.Action()"
  IL_001f:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0024:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of System.Action)(System.Func(Of System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action)"
  IL_0029:  ret
}
]]>)

            verifier.VerifyIL("D.SharedRemove", <![CDATA[
{
  // Code size       30 (0x1e)
  .maxstack  3
  IL_0000:  ldnull
  IL_0001:  ldftn      "Sub C.remove_SharedEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0007:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_000c:  ldnull
  IL_000d:  ldftn      "Sub D.Action()"
  IL_0013:  newobj     "Sub System.Action..ctor(Object, System.IntPtr)"
  IL_0018:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of System.Action)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action)"
  IL_001d:  ret
}
]]>)
        End Sub

        <Fact()>
        Public Sub [RaiseEvent]()
            Dim verifier = CompileAndVerify(
    <compilation>
        <file name="a.vb">
Class C
    Public Event InstanceEvent As System.Action(Of Integer)
    Public Shared Event SharedEvent As System.Action(Of Integer)

    Sub InstanceRaise()
        RaiseEvent InstanceEvent(1)
    End Sub

    Sub SharedRaise()
        RaiseEvent SharedEvent(2)
    End Sub

    Shared Sub Action()
    End Sub
End Class
    </file>
    </compilation>, WinRtRefs, options:=TestOptions.ReleaseWinMD)

            verifier.VerifyIL("C.InstanceRaise", <![CDATA[
{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (System.Action(Of Integer) V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     "C.InstanceEventEvent As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action(Of Integer))"
  IL_0006:  call       "Function System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action(Of Integer)).GetOrCreateEventRegistrationTokenTable(ByRef System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action(Of Integer))) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action(Of Integer))"
  IL_000b:  callvirt   "Function System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action(Of Integer)).get_InvocationList() As System.Action(Of Integer)"
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  brfalse.s  IL_001b
  IL_0014:  ldloc.0
  IL_0015:  ldc.i4.1
  IL_0016:  callvirt   "Sub System.Action(Of Integer).Invoke(Integer)"
  IL_001b:  ret
}
]]>)

            verifier.VerifyIL("C.SharedRaise", <![CDATA[
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (System.Action(Of Integer) V_0)
  IL_0000:  ldsflda    "C.SharedEventEvent As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action(Of Integer))"
  IL_0005:  call       "Function System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action(Of Integer)).GetOrCreateEventRegistrationTokenTable(ByRef System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action(Of Integer))) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action(Of Integer))"
  IL_000a:  callvirt   "Function System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of System.Action(Of Integer)).get_InvocationList() As System.Action(Of Integer)"
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  brfalse.s  IL_001a
  IL_0013:  ldloc.0
  IL_0014:  ldc.i4.2
  IL_0015:  callvirt   "Sub System.Action(Of Integer).Invoke(Integer)"
  IL_001a:  ret
}
]]>)
        End Sub

        ''' <summary>
        ''' Dev11 had bugs in this area (e.g. 281866, 298564), but Roslyn shouldn't be affected.
        ''' </summary>
        ''' <remarks>
        ''' I'm assuming this is why the final dev11 impl uses GetOrCreateEventRegistrationTokenTable.
        ''' </remarks>
        <WorkItem(1003209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1003209")>
        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:=ConditionalSkipReason.WinRTNeedsWindowsDesktop)>
        Public Sub FieldLikeEventSerialization()

            Dim source1 =
<compilation>
    <file name="a.vb">
Namespace EventDeserialization

    Public Delegate Sub EventDelegate

    Public Interface IEvent
        Event E as EventDelegate
    End Interface

End Namespace
    </file>
</compilation>

            Dim source2 =
<compilation>
    <file name="b.vb">
Imports System
Imports System.IO
Imports System.Runtime.Serialization

Namespace EventDeserialization

    Module MainPage

        Public Sub Main()
            Dim m1 as new Model()
            AddHandler m1.E, Sub() Console.Write("A")
            m1.Invoke()

            Dim bytes = Serialize(m1)
    
            Dim m2 as Model = Deserialize(bytes)
            Console.WriteLine(m1 is m2)
            m2.Invoke()

            AddHandler m2.E, Sub() Console.Write("B")
            m2.Invoke()
        End Sub

        Function Serialize(m as Model) as Byte()
            Dim ser as new DataContractSerializer(GetType(Model))
            Using stream = new MemoryStream()
                ser.WriteObject(stream, m)
                Return stream.ToArray()
            End Using
        End Function

        Function Deserialize(b as Byte()) As Model
            Dim ser as new DataContractSerializer(GetType(Model))
            Using stream = new MemoryStream(b)
                Return DirectCast(ser.ReadObject(stream), Model)
            End Using
        End Function

    End Module

    &lt;DataContract&gt;
    Public NotInheritable Class Model
        Implements IEvent
        
        Public Event E as EventDelegate Implements IEvent.E

        Public Sub Invoke()
            RaiseEvent E()
            Console.WriteLine()
        End Sub
    End Class

End Namespace
    </file>
</compilation>

            Dim comp1 = CreateEmptyCompilationWithReferences(source1, WinRtRefs, options:=TestOptions.ReleaseWinMD)
            comp1.VerifyDiagnostics()

            Dim serializationRef = Net461.References.SystemRuntimeSerialization
            Dim comp2 = CreateEmptyCompilationWithReferences(source2, WinRtRefs.Concat({New VisualBasicCompilationReference(comp1), serializationRef, MsvbRef, SystemXmlRef}), options:=TestOptions.ReleaseExe)
            CompileAndVerify(comp2, expectedOutput:=<![CDATA[
A
False

B
]]>)
        End Sub

        ' Receiver can be MyBase, MyClass, Me, or the name of a WithEvents member (instance or shared).
        <Fact>
        Public Sub HandlesClauses_ReceiverKinds()

            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Runtime.InteropServices.WindowsRuntime

Delegate Sub EventDelegate()

Class Base
    Public Event InstanceEvent As EventDelegate
    Public Shared Event SharedEvent As EventDelegate
End Class

Class Derived
    Inherits Base

    WithEvents B As Base
    Shared WithEvents BX As Base

    Public Shadows Event InstanceEvent As EventDelegate
    Public Shadows Shared Event SharedEvent As EventDelegate

    Sub InstanceHandler() Handles _
        MyBase.InstanceEvent,
        MyBase.SharedEvent,
        MyClass.InstanceEvent,
        MyClass.SharedEvent,
        Me.InstanceEvent,
        Me.SharedEvent,
        B.InstanceEvent,
        B.SharedEvent

    End Sub

    Shared Sub SharedHandler() Handles _
        MyBase.InstanceEvent,
        MyBase.SharedEvent,
        MyClass.InstanceEvent,
        MyClass.SharedEvent,
        Me.InstanceEvent,
        Me.SharedEvent,
        B.InstanceEvent,
        B.SharedEvent,
        BX.InstanceEvent,
        BX.SharedEvent

    End Sub

End Class
    </file>
</compilation>

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, options:=TestOptions.ReleaseWinMD)
            Dim verifier = CompileAndVerify(comp)

            ' Attach Me.InstanceHandler to {Base/Derived/Derived}.{InstanceEvent/SharedEvent} (from {MyBase/MyClass/Me}.{InstanceEvent/SharedEvent}).
            ' Attach Derived.SharedHandler to {Base/Derived/Derived}.InstanceEvent (from {MyBase/MyClass/Me}.InstanceEvent).
            verifier.VerifyIL("Derived..ctor", <![CDATA[
{
  // Code size      376 (0x178)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Base..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldftn      "Sub Base.add_InstanceEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_000d:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0012:  ldarg.0
  IL_0013:  ldftn      "Sub Base.remove_InstanceEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0019:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_001e:  ldarg.0
  IL_001f:  ldftn      "Sub Derived.InstanceHandler()"
  IL_0025:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_002a:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_002f:  ldnull
  IL_0030:  ldftn      "Sub Base.add_SharedEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0036:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_003b:  ldnull
  IL_003c:  ldftn      "Sub Base.remove_SharedEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0042:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0047:  ldarg.0
  IL_0048:  ldftn      "Sub Derived.InstanceHandler()"
  IL_004e:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_0053:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0058:  ldarg.0
  IL_0059:  ldftn      "Sub Derived.add_InstanceEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_005f:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0064:  ldarg.0
  IL_0065:  ldftn      "Sub Derived.remove_InstanceEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_006b:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0070:  ldarg.0
  IL_0071:  ldftn      "Sub Derived.InstanceHandler()"
  IL_0077:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_007c:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0081:  ldnull
  IL_0082:  ldftn      "Sub Derived.add_SharedEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0088:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_008d:  ldnull
  IL_008e:  ldftn      "Sub Derived.remove_SharedEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0094:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0099:  ldarg.0
  IL_009a:  ldftn      "Sub Derived.InstanceHandler()"
  IL_00a0:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_00a5:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_00aa:  ldarg.0
  IL_00ab:  ldftn      "Sub Derived.add_InstanceEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_00b1:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00b6:  ldarg.0
  IL_00b7:  ldftn      "Sub Derived.remove_InstanceEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_00bd:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00c2:  ldarg.0
  IL_00c3:  ldftn      "Sub Derived.InstanceHandler()"
  IL_00c9:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_00ce:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_00d3:  ldnull
  IL_00d4:  ldftn      "Sub Derived.add_SharedEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_00da:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00df:  ldnull
  IL_00e0:  ldftn      "Sub Derived.remove_SharedEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_00e6:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00eb:  ldarg.0
  IL_00ec:  ldftn      "Sub Derived.InstanceHandler()"
  IL_00f2:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_00f7:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_00fc:  ldarg.0
  IL_00fd:  ldftn      "Sub Base.add_InstanceEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0103:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0108:  ldarg.0
  IL_0109:  ldftn      "Sub Base.remove_InstanceEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_010f:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0114:  ldnull
  IL_0115:  ldftn      "Sub Derived.SharedHandler()"
  IL_011b:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_0120:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0125:  ldarg.0
  IL_0126:  ldftn      "Sub Derived.add_InstanceEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_012c:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0131:  ldarg.0
  IL_0132:  ldftn      "Sub Derived.remove_InstanceEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0138:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_013d:  ldnull
  IL_013e:  ldftn      "Sub Derived.SharedHandler()"
  IL_0144:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_0149:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_014e:  ldarg.0
  IL_014f:  ldftn      "Sub Derived.add_InstanceEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0155:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_015a:  ldarg.0
  IL_015b:  ldftn      "Sub Derived.remove_InstanceEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0161:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0166:  ldnull
  IL_0167:  ldftn      "Sub Derived.SharedHandler()"
  IL_016d:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_0172:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0177:  ret
}
]]>)

            ' Attach Derived.SharedHandler to from {Base/Derived/Derived}.SharedEvent (from {MyBase/MyClass/Me}.SharedEvent).
            verifier.VerifyIL("Derived..cctor", <![CDATA[
{
  // Code size      124 (0x7c)
  .maxstack  4
  IL_0000:  ldnull
  IL_0001:  ldftn      "Sub Base.add_SharedEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0007:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_000c:  ldnull
  IL_000d:  ldftn      "Sub Base.remove_SharedEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0013:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0018:  ldnull
  IL_0019:  ldftn      "Sub Derived.SharedHandler()"
  IL_001f:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_0024:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0029:  ldnull
  IL_002a:  ldftn      "Sub Derived.add_SharedEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0030:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0035:  ldnull
  IL_0036:  ldftn      "Sub Derived.remove_SharedEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_003c:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0041:  ldnull
  IL_0042:  ldftn      "Sub Derived.SharedHandler()"
  IL_0048:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_004d:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0052:  ldnull
  IL_0053:  ldftn      "Sub Derived.add_SharedEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0059:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_005e:  ldnull
  IL_005f:  ldftn      "Sub Derived.remove_SharedEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0065:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_006a:  ldnull
  IL_006b:  ldftn      "Sub Derived.SharedHandler()"
  IL_0071:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_0076:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_007b:  ret
}
]]>)

            ' Wire up Me.InstanceHandler to Me.B.InstanceEvent and Base.SharedEvent.
            ' Wire up Derived.SharedHandler to Me.B.InstanceEvent and Base.SharedEvent.
            verifier.VerifyIL("Derived.put_B", <![CDATA[
{
  // Code size      282 (0x11a)
  .maxstack  3
  .locals init (EventDelegate V_0,
    EventDelegate V_1,
    EventDelegate V_2,
    EventDelegate V_3,
    Base V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub Derived.InstanceHandler()"
  IL_0007:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldftn      "Sub Derived.InstanceHandler()"
  IL_0014:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_0019:  stloc.1
  IL_001a:  ldnull
  IL_001b:  ldftn      "Sub Derived.SharedHandler()"
  IL_0021:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_0026:  stloc.2
  IL_0027:  ldnull
  IL_0028:  ldftn      "Sub Derived.SharedHandler()"
  IL_002e:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_0033:  stloc.3
  IL_0034:  ldarg.0
  IL_0035:  ldfld      "Derived._B As Base"
  IL_003a:  stloc.s    V_4
  IL_003c:  ldloc.s    V_4
  IL_003e:  brfalse.s  IL_008a
  IL_0040:  ldloc.s    V_4
  IL_0042:  ldftn      "Sub Base.remove_InstanceEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0048:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_004d:  ldloc.0
  IL_004e:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0053:  ldnull
  IL_0054:  ldftn      "Sub Base.remove_SharedEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_005a:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_005f:  ldloc.1
  IL_0060:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0065:  ldloc.s    V_4
  IL_0067:  ldftn      "Sub Base.remove_InstanceEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_006d:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0072:  ldloc.2
  IL_0073:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0078:  ldnull
  IL_0079:  ldftn      "Sub Base.remove_SharedEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_007f:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0084:  ldloc.3
  IL_0085:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_008a:  ldarg.0
  IL_008b:  ldarg.1
  IL_008c:  stfld      "Derived._B As Base"
  IL_0091:  ldarg.0
  IL_0092:  ldfld      "Derived._B As Base"
  IL_0097:  stloc.s    V_4
  IL_0099:  ldloc.s    V_4
  IL_009b:  brfalse.s  IL_0119
  IL_009d:  ldloc.s    V_4
  IL_009f:  ldftn      "Sub Base.add_InstanceEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_00a5:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00aa:  ldloc.s    V_4
  IL_00ac:  ldftn      "Sub Base.remove_InstanceEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_00b2:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00b7:  ldloc.0
  IL_00b8:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_00bd:  ldnull
  IL_00be:  ldftn      "Sub Base.add_SharedEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_00c4:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00c9:  ldnull
  IL_00ca:  ldftn      "Sub Base.remove_SharedEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_00d0:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00d5:  ldloc.1
  IL_00d6:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_00db:  ldloc.s    V_4
  IL_00dd:  ldftn      "Sub Base.add_InstanceEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_00e3:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00e8:  ldloc.s    V_4
  IL_00ea:  ldftn      "Sub Base.remove_InstanceEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_00f0:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00f5:  ldloc.2
  IL_00f6:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_00fb:  ldnull
  IL_00fc:  ldftn      "Sub Base.add_SharedEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0102:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0107:  ldnull
  IL_0108:  ldftn      "Sub Base.remove_SharedEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_010e:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0113:  ldloc.3
  IL_0114:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0119:  ret
}
]]>)

            ' Wire up Derived.SharedHandler to Derived.BX.InstanceEvent and Base.SharedEvent.
            verifier.VerifyIL("Derived.put_BX", <![CDATA[
{
  // Code size      147 (0x93)
  .maxstack  3
  .locals init (EventDelegate V_0,
    EventDelegate V_1,
    Base V_2)
  IL_0000:  ldnull
  IL_0001:  ldftn      "Sub Derived.SharedHandler()"
  IL_0007:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldnull
  IL_000e:  ldftn      "Sub Derived.SharedHandler()"
  IL_0014:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_0019:  stloc.1
  IL_001a:  ldsfld     "Derived._BX As Base"
  IL_001f:  stloc.2
  IL_0020:  ldloc.2
  IL_0021:  brfalse.s  IL_0047
  IL_0023:  ldloc.2
  IL_0024:  ldftn      "Sub Base.remove_InstanceEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_002a:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_002f:  ldloc.0
  IL_0030:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0035:  ldnull
  IL_0036:  ldftn      "Sub Base.remove_SharedEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_003c:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0041:  ldloc.1
  IL_0042:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0047:  ldarg.0
  IL_0048:  stsfld     "Derived._BX As Base"
  IL_004d:  ldsfld     "Derived._BX As Base"
  IL_0052:  stloc.2
  IL_0053:  ldloc.2
  IL_0054:  brfalse.s  IL_0092
  IL_0056:  ldloc.2
  IL_0057:  ldftn      "Sub Base.add_InstanceEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_005d:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0062:  ldloc.2
  IL_0063:  ldftn      "Sub Base.remove_InstanceEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0069:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_006e:  ldloc.0
  IL_006f:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0074:  ldnull
  IL_0075:  ldftn      "Sub Base.add_SharedEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_007b:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0080:  ldnull
  IL_0081:  ldftn      "Sub Base.remove_SharedEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0087:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_008c:  ldloc.1
  IL_008d:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0092:  ret
}
]]>)
        End Sub

        <Fact(), WorkItem(6313, "https://github.com/dotnet/roslyn/issues/6313")>
        Public Sub CustomEventWinMd()
            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Runtime.InteropServices.WindowsRuntime

Class Test
    Public Custom Event CustomEvent As System.Action(Of Integer)
        AddHandler(value As System.Action(Of Integer))
            Return Nothing		
        End AddHandler

        RemoveHandler(value As EventRegistrationToken)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event
End Class
    </file>
</compilation>

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, options:=TestOptions.DebugWinMD)
            Dim verifier = CompileAndVerify(comp)
        End Sub

        ' Field-like and custom events are not treated differently.
        <Fact(), WorkItem(1003209, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1003209")>
        Public Sub HandlesClauses_EventKinds()

            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Runtime.InteropServices.WindowsRuntime

Delegate Sub EventDelegate()

Class Test

    WithEvents T As Test

    Public Event FieldLikeEvent As EventDelegate

    Public Custom Event CustomEvent As EventDelegate
        AddHandler(value As EventDelegate)
            Return Nothing		
        End AddHandler

        RemoveHandler(value As EventRegistrationToken)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event

    Sub Handler() Handles _
        Me.FieldLikeEvent,
        Me.CustomEvent,
        T.FieldLikeEvent,
        T.CustomEvent

    End Sub

End Class
    </file>
</compilation>

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, options:=TestOptions.ReleaseWinMD)
            Dim verifier = CompileAndVerify(comp)

            verifier.VerifyIL("Test..ctor", <![CDATA[
{
  // Code size       89 (0x59)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldftn      "Sub Test.add_FieldLikeEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_000d:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0012:  ldarg.0
  IL_0013:  ldftn      "Sub Test.remove_FieldLikeEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0019:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_001e:  ldarg.0
  IL_001f:  ldftn      "Sub Test.Handler()"
  IL_0025:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_002a:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_002f:  ldarg.0
  IL_0030:  ldftn      "Sub Test.add_CustomEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0036:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_003b:  ldarg.0
  IL_003c:  ldftn      "Sub Test.remove_CustomEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0042:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0047:  ldarg.0
  IL_0048:  ldftn      "Sub Test.Handler()"
  IL_004e:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_0053:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0058:  ret
}
]]>)

            verifier.VerifyIL("Test.put_T", <![CDATA[
{
  // Code size      150 (0x96)
  .maxstack  3
  .locals init (EventDelegate V_0,
  EventDelegate V_1,
  Test V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub Test.Handler()"
  IL_0007:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldftn      "Sub Test.Handler()"
  IL_0014:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_0019:  stloc.1
  IL_001a:  ldarg.0
  IL_001b:  ldfld      "Test._T As Test"
  IL_0020:  stloc.2
  IL_0021:  ldloc.2
  IL_0022:  brfalse.s  IL_0048
  IL_0024:  ldloc.2
  IL_0025:  ldftn      "Sub Test.remove_FieldLikeEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_002b:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0030:  ldloc.0
  IL_0031:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0036:  ldloc.2
  IL_0037:  ldftn      "Sub Test.remove_CustomEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_003d:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0042:  ldloc.1
  IL_0043:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0048:  ldarg.0
  IL_0049:  ldarg.1
  IL_004a:  stfld      "Test._T As Test"
  IL_004f:  ldarg.0
  IL_0050:  ldfld      "Test._T As Test"
  IL_0055:  stloc.2
  IL_0056:  ldloc.2
  IL_0057:  brfalse.s  IL_0095
  IL_0059:  ldloc.2
  IL_005a:  ldftn      "Sub Test.add_FieldLikeEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0060:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0065:  ldloc.2
  IL_0066:  ldftn      "Sub Test.remove_FieldLikeEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_006c:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0071:  ldloc.0
  IL_0072:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0077:  ldloc.2
  IL_0078:  ldftn      "Sub Test.add_CustomEvent(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_007e:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0083:  ldloc.2
  IL_0084:  ldftn      "Sub Test.remove_CustomEvent(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_008a:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_008f:  ldloc.1
  IL_0090:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0095:  ret
}
]]>)
        End Sub

        ' The handler is allowed to have zero arguments (event if using AddHandler would be illegal).
        <Fact>
        Public Sub HandlesClauses_ZeroArgumentHandler()

            Dim source =
<compilation>
    <file name="a.vb">
Imports System.Runtime.InteropServices.WindowsRuntime

Delegate Sub EventDelegate(x as Integer)

Class Test

    WithEvents T As Test

    Public Event E As EventDelegate

    Sub Handler() Handles Me.E, T.E

    End Sub

End Class
    </file>
</compilation>

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, options:=TestOptions.ReleaseWinMD)
            Dim verifier = CompileAndVerify(comp)

            ' Note: actually attaching a lambda.
            verifier.VerifyIL("Test..ctor", <![CDATA[
{
  // Code size       48 (0x30)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  call       "Sub Object..ctor()"
  IL_0006:  ldarg.0
  IL_0007:  ldftn      "Sub Test.add_E(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_000d:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0012:  ldarg.0
  IL_0013:  ldftn      "Sub Test.remove_E(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0019:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_001e:  ldarg.0
  IL_001f:  ldftn      "Sub Test._Lambda$__R0-1(Integer)"
  IL_0025:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_002a:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_002f:  ret
}
]]>)

            ' Note: actually attaching a lambda.
            verifier.VerifyIL("Test.put_T", <![CDATA[
{
  // Code size       89 (0x59)
  .maxstack  3
  .locals init (EventDelegate V_0,
  Test V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldftn      "Sub Test._Lambda$__R3-2(Integer)"
  IL_0007:  newobj     "Sub EventDelegate..ctor(Object, System.IntPtr)"
  IL_000c:  stloc.0
  IL_000d:  ldarg.0
  IL_000e:  ldfld      "Test._T As Test"
  IL_0013:  stloc.1
  IL_0014:  ldloc.1
  IL_0015:  brfalse.s  IL_0029
  IL_0017:  ldloc.1
  IL_0018:  ldftn      "Sub Test.remove_E(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_001e:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0023:  ldloc.0
  IL_0024:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0029:  ldarg.0
  IL_002a:  ldarg.1
  IL_002b:  stfld      "Test._T As Test"
  IL_0030:  ldarg.0
  IL_0031:  ldfld      "Test._T As Test"
  IL_0036:  stloc.1
  IL_0037:  ldloc.1
  IL_0038:  brfalse.s  IL_0058
  IL_003a:  ldloc.1
  IL_003b:  ldftn      "Sub Test.add_E(EventDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0041:  newobj     "Sub System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0046:  ldloc.1
  IL_0047:  ldftn      "Sub Test.remove_E(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_004d:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0052:  ldloc.0
  IL_0053:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventDelegate)(System.Func(Of EventDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventDelegate)"
  IL_0058:  ret
}
]]>)
        End Sub

    End Class
End Namespace

