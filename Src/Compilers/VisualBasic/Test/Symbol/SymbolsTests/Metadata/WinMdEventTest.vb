' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata

    Public Class WinMdEventTest
        Inherits BasicTestBase

        Private EventInterfaceILTemplate As String = <![CDATA[
.class interface public abstract auto ansi {0}
{{
  .method public hidebysig newslot specialname abstract virtual 
          instance void  add_Normal(class [mscorlib]System.Action 'value') cil managed
  {{
  }}

  .method public hidebysig newslot specialname abstract virtual 
          instance void  remove_Normal(class [mscorlib]System.Action 'value') cil managed
  {{
  }}

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 
          add_WinRT([in] class [mscorlib]System.Action 'value') cil managed
  {{
  }}

  .method public hidebysig newslot specialname abstract virtual 
          instance void  remove_WinRT([in] valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 'value') cil managed
  {{
  }}

  .event class [mscorlib]System.Action Normal
  {{
    .addon instance void {0}::add_Normal(class [mscorlib]System.Action)
    .removeon instance void {0}::remove_Normal(class [mscorlib]System.Action)
  }}

  .event class [mscorlib]System.Action WinRT
  {{
    .addon instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken {0}::add_WinRT(class [mscorlib]System.Action)
    .removeon instance void {0}::remove_WinRT(valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
  }}
}} // end of class {0}
]]>.Value
        Private ReadOnly EventLibRef As MetadataReference

        Private DynamicCommonSrc As XElement =
            <compilation>
                <file name="dynamic_common.vb">
                    <![CDATA[
Imports System.Runtime.InteropServices.WindowsRuntime
Imports EventLibrary

Public Partial Class A
    Implements I
    Public Event d1 As voidVoidDelegate Implements I.d1
    Public Event d2 As voidStringDelegate Implements I.d2
    Public Event d3 As voidDynamicDelegate Implements I.d3
    Public Event d4 As voidDelegateDelegate Implements I.d4
End Class

Public Partial Class B
    Implements I

    Private voidTable As New EventRegistrationTokenTable(Of voidVoidDelegate)()
    Private voidStringTable As New EventRegistrationTokenTable(Of voidStringDelegate)()
    Private voidDynamicTable As New EventRegistrationTokenTable(Of voidDynamicDelegate)()
    Private voidDelegateTable As New EventRegistrationTokenTable(Of voidDelegateDelegate)()

    Public Custom Event d1 As voidVoidDelegate Implements I.d1
        AddHandler(value As voidVoidDelegate)
            Return voidTable.AddEventHandler(value)
        End AddHandler

        RemoveHandler(value As EventRegistrationToken)
            voidTable.RemoveEventHandler(value)
        End RemoveHandler
        RaiseEvent()
        End RaiseEvent
    End Event

    Public Custom Event d2 As voidStringDelegate Implements I.d2
        AddHandler(value As voidStringDelegate)
            Return voidStringTable.AddEventHandler(value)
        End AddHandler
        
        RemoveHandler(value As EventRegistrationToken)
            voidStringTable.RemoveEventHandler(value)
        End RemoveHandler
        RaiseEvent(s As String)
        End RaiseEvent
    End Event

    Public Custom Event d3 As voidDynamicDelegate Implements I.d3
        AddHandler(value As voidDynamicDelegate)
            Return voidDynamicTable.AddEventHandler(value)
        End AddHandler

        RemoveHandler(value As EventRegistrationToken)
            voidDynamicTable.RemoveEventHandler(value)
        End RemoveHandler
        RaiseEvent(d As Object)
        End RaiseEvent
    End Event

    Public Custom Event d4 As voidDelegateDelegate Implements I.d4
        AddHandler(value As voidDelegateDelegate)
            Return voidDelegateTable.AddEventHandler(value)
        End AddHandler

        RemoveHandler(value As EventRegistrationToken)
            voidDelegateTable.RemoveEventHandler(value)
        End RemoveHandler
        RaiseEvent([delegate] As voidVoidDelegate)
        End RaiseEvent
    End Event
End Class
]]>
                </file>
            </compilation>


        Public Sub New()
            ' The following two libraries are shrinked code pulled from
            ' corresponding files in the csharp5 legacy tests
            Dim eventLibSrc =
                <compilation><file name="EventLibrary.vb">
                    <![CDATA[
Namespace EventLibrary
    Public Delegate Sub voidVoidDelegate()
    Public Delegate Sub voidStringDelegate(s As String)
    Public Delegate Sub voidDynamicDelegate(d As Object)
    Public Delegate Sub voidDelegateDelegate([delegate] As voidVoidDelegate)

    Public Interface I
        Event d1 As voidVoidDelegate
        Event d2 As voidStringDelegate
        Event d3 As voidDynamicDelegate
        Event d4 As voidDelegateDelegate
    End Interface
End Namespace
]]>
                    </file></compilation>
            EventLibRef = CreateCompilationWithReferences(
                eventLibSrc,
                references:={MscorlibRef_v4_0_30316_17626, SystemCoreRef_v4_0_30319_17929},
                options:=TestOptions.ReleaseWinMD).EmitToImageReference()
        End Sub

        <Fact()>
        Public Sub WinMdExternalEventTests()
            Dim src =
                <compilation><file name="c.vb">
                    <![CDATA[
Imports EventLibrary

Class C
    Sub Main()
        Dim a = new A()
        Dim b = new B()

        Dim void = Sub()
                   End Sub
        Dim str = Sub(s As String) 
                  End Sub
        Dim dyn = Sub(d As Object)
                  End Sub
        Dim del = Sub([delegate] As voidVoidDelegate)
                  End Sub
        
        AddHandler a.d1, void
        AddHandler a.d2, str
        AddHandler a.d3, dyn
        AddHandler a.d4, del

        RemoveHandler a.d1, void
        RemoveHandler a.d2, str
        RemoveHandler a.d3, dyn
        RemoveHandler a.d4, del

        AddHandler b.d1, void
        AddHandler b.d2, str
        AddHandler b.d3, dyn
        AddHandler b.d4, del

        RemoveHandler b.d1, void
        RemoveHandler b.d2, str
        RemoveHandler b.d3, dyn
        RemoveHandler b.d4, del
    End Sub
End Class
]]>
                    </file></compilation>
            Dim dynamicCommonRef As MetadataReference = CreateCompilationWithReferences(
                DynamicCommonSrc,
                references:={
                    MscorlibRef_v4_0_30316_17626,
                    EventLibRef},
                options:=TestOptions.ReleaseModule).EmitToImageReference()

            Dim verifer = CompileAndVerifyOnWin8Only(
                src,
                allReferences:={
                    MscorlibRef_v4_0_30316_17626,
                    SystemCoreRef_v4_0_30319_17929,
                    CSharpRef,
                    EventLibRef,
                    dynamicCommonRef},
                emitOptions:=TestEmitters.RefEmitBug)
            verifer.VerifyIL("C.Main", <![CDATA[
{
  // Code size      915 (0x393)
  .maxstack  4
  .locals init (A V_0, //a
                B V_1, //b
                VB$AnonymousDelegate_0 V_2, //void
                VB$AnonymousDelegate_1(Of String) V_3, //str
                VB$AnonymousDelegate_2(Of Object) V_4, //dyn
                VB$AnonymousDelegate_3(Of EventLibrary.voidVoidDelegate) V_5, //del
                VB$AnonymousDelegate_0 V_6,
                VB$AnonymousDelegate_1(Of String) V_7,
                VB$AnonymousDelegate_2(Of Object) V_8,
                VB$AnonymousDelegate_3(Of EventLibrary.voidVoidDelegate) V_9)
  IL_0000:  newobj     "Sub A..ctor()"
  IL_0005:  stloc.0
  IL_0006:  newobj     "Sub B..ctor()"
  IL_000b:  stloc.1
  IL_000c:  ldsfld     "C._ClosureCache$__2 As <generated method>"
  IL_0011:  brfalse.s  IL_001a
  IL_0013:  ldsfld     "C._ClosureCache$__2 As <generated method>"
  IL_0018:  br.s       IL_002c
  IL_001a:  ldnull
  IL_001b:  ldftn      "Sub C._Lambda$__1(Object)"
  IL_0021:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_0026:  dup
  IL_0027:  stsfld     "C._ClosureCache$__2 As <generated method>"
  IL_002c:  stloc.2
  IL_002d:  ldsfld     "C._ClosureCache$__4 As <generated method>"
  IL_0032:  brfalse.s  IL_003b
  IL_0034:  ldsfld     "C._ClosureCache$__4 As <generated method>"
  IL_0039:  br.s       IL_004d
  IL_003b:  ldnull
  IL_003c:  ldftn      "Sub C._Lambda$__3(Object, String)"
  IL_0042:  newobj     "Sub VB$AnonymousDelegate_1(Of String)..ctor(Object, System.IntPtr)"
  IL_0047:  dup
  IL_0048:  stsfld     "C._ClosureCache$__4 As <generated method>"
  IL_004d:  stloc.3
  IL_004e:  ldsfld     "C._ClosureCache$__6 As <generated method>"
  IL_0053:  brfalse.s  IL_005c
  IL_0055:  ldsfld     "C._ClosureCache$__6 As <generated method>"
  IL_005a:  br.s       IL_006e
  IL_005c:  ldnull
  IL_005d:  ldftn      "Sub C._Lambda$__5(Object, Object)"
  IL_0063:  newobj     "Sub VB$AnonymousDelegate_2(Of Object)..ctor(Object, System.IntPtr)"
  IL_0068:  dup
  IL_0069:  stsfld     "C._ClosureCache$__6 As <generated method>"
  IL_006e:  stloc.s    V_4
  IL_0070:  ldsfld     "C._ClosureCache$__8 As <generated method>"
  IL_0075:  brfalse.s  IL_007e
  IL_0077:  ldsfld     "C._ClosureCache$__8 As <generated method>"
  IL_007c:  br.s       IL_0090
  IL_007e:  ldnull
  IL_007f:  ldftn      "Sub C._Lambda$__7(Object, EventLibrary.voidVoidDelegate)"
  IL_0085:  newobj     "Sub VB$AnonymousDelegate_3(Of EventLibrary.voidVoidDelegate)..ctor(Object, System.IntPtr)"
  IL_008a:  dup
  IL_008b:  stsfld     "C._ClosureCache$__8 As <generated method>"
  IL_0090:  stloc.s    V_5
  IL_0092:  ldloc.0
  IL_0093:  dup
  IL_0094:  ldvirtftn  "Sub A.add_d1(EventLibrary.voidVoidDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_009a:  newobj     "Sub System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_009f:  ldloc.0
  IL_00a0:  dup
  IL_00a1:  ldvirtftn  "Sub A.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_00a7:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00ac:  ldloc.2
  IL_00ad:  stloc.s    V_6
  IL_00af:  ldloc.s    V_6
  IL_00b1:  brfalse.s  IL_00c2
  IL_00b3:  ldloc.s    V_6
  IL_00b5:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_00bb:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_00c0:  br.s       IL_00c3
  IL_00c2:  ldnull
  IL_00c3:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidVoidDelegate)(System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_00c8:  ldloc.0
  IL_00c9:  dup
  IL_00ca:  ldvirtftn  "Sub A.add_d2(EventLibrary.voidStringDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_00d0:  newobj     "Sub System.Func(Of EventLibrary.voidStringDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00d5:  ldloc.0
  IL_00d6:  dup
  IL_00d7:  ldvirtftn  "Sub A.remove_d2(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_00dd:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00e2:  ldloc.3
  IL_00e3:  stloc.s    V_7
  IL_00e5:  ldloc.s    V_7
  IL_00e7:  brfalse.s  IL_00f8
  IL_00e9:  ldloc.s    V_7
  IL_00eb:  ldftn      "Sub VB$AnonymousDelegate_1(Of String).Invoke(String)"
  IL_00f1:  newobj     "Sub EventLibrary.voidStringDelegate..ctor(Object, System.IntPtr)"
  IL_00f6:  br.s       IL_00f9
  IL_00f8:  ldnull
  IL_00f9:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidStringDelegate)(System.Func(Of EventLibrary.voidStringDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidStringDelegate)"
  IL_00fe:  ldloc.0
  IL_00ff:  dup
  IL_0100:  ldvirtftn  "Sub A.add_d3(EventLibrary.voidDynamicDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0106:  newobj     "Sub System.Func(Of EventLibrary.voidDynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_010b:  ldloc.0
  IL_010c:  dup
  IL_010d:  ldvirtftn  "Sub A.remove_d3(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0113:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0118:  ldloc.s    V_4
  IL_011a:  stloc.s    V_8
  IL_011c:  ldloc.s    V_8
  IL_011e:  brfalse.s  IL_012f
  IL_0120:  ldloc.s    V_8
  IL_0122:  ldftn      "Sub VB$AnonymousDelegate_2(Of Object).Invoke(Object)"
  IL_0128:  newobj     "Sub EventLibrary.voidDynamicDelegate..ctor(Object, System.IntPtr)"
  IL_012d:  br.s       IL_0130
  IL_012f:  ldnull
  IL_0130:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidDynamicDelegate)(System.Func(Of EventLibrary.voidDynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDynamicDelegate)"
  IL_0135:  ldloc.0
  IL_0136:  dup
  IL_0137:  ldvirtftn  "Sub A.add_d4(EventLibrary.voidDelegateDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_013d:  newobj     "Sub System.Func(Of EventLibrary.voidDelegateDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0142:  ldloc.0
  IL_0143:  dup
  IL_0144:  ldvirtftn  "Sub A.remove_d4(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_014a:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_014f:  ldloc.s    V_5
  IL_0151:  stloc.s    V_9
  IL_0153:  ldloc.s    V_9
  IL_0155:  brfalse.s  IL_0166
  IL_0157:  ldloc.s    V_9
  IL_0159:  ldftn      "Sub VB$AnonymousDelegate_3(Of EventLibrary.voidVoidDelegate).Invoke(EventLibrary.voidVoidDelegate)"
  IL_015f:  newobj     "Sub EventLibrary.voidDelegateDelegate..ctor(Object, System.IntPtr)"
  IL_0164:  br.s       IL_0167
  IL_0166:  ldnull
  IL_0167:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidDelegateDelegate)(System.Func(Of EventLibrary.voidDelegateDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDelegateDelegate)"
  IL_016c:  ldloc.0
  IL_016d:  dup
  IL_016e:  ldvirtftn  "Sub A.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0174:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0179:  ldloc.2
  IL_017a:  stloc.s    V_6
  IL_017c:  ldloc.s    V_6
  IL_017e:  brfalse.s  IL_018f
  IL_0180:  ldloc.s    V_6
  IL_0182:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_0188:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_018d:  br.s       IL_0190
  IL_018f:  ldnull
  IL_0190:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidVoidDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_0195:  ldloc.0
  IL_0196:  dup
  IL_0197:  ldvirtftn  "Sub A.remove_d2(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_019d:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_01a2:  ldloc.3
  IL_01a3:  stloc.s    V_7
  IL_01a5:  ldloc.s    V_7
  IL_01a7:  brfalse.s  IL_01b8
  IL_01a9:  ldloc.s    V_7
  IL_01ab:  ldftn      "Sub VB$AnonymousDelegate_1(Of String).Invoke(String)"
  IL_01b1:  newobj     "Sub EventLibrary.voidStringDelegate..ctor(Object, System.IntPtr)"
  IL_01b6:  br.s       IL_01b9
  IL_01b8:  ldnull
  IL_01b9:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidStringDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidStringDelegate)"
  IL_01be:  ldloc.0
  IL_01bf:  dup
  IL_01c0:  ldvirtftn  "Sub A.remove_d3(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_01c6:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_01cb:  ldloc.s    V_4
  IL_01cd:  stloc.s    V_8
  IL_01cf:  ldloc.s    V_8
  IL_01d1:  brfalse.s  IL_01e2
  IL_01d3:  ldloc.s    V_8
  IL_01d5:  ldftn      "Sub VB$AnonymousDelegate_2(Of Object).Invoke(Object)"
  IL_01db:  newobj     "Sub EventLibrary.voidDynamicDelegate..ctor(Object, System.IntPtr)"
  IL_01e0:  br.s       IL_01e3
  IL_01e2:  ldnull
  IL_01e3:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidDynamicDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDynamicDelegate)"
  IL_01e8:  ldloc.0
  IL_01e9:  dup
  IL_01ea:  ldvirtftn  "Sub A.remove_d4(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_01f0:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_01f5:  ldloc.s    V_5
  IL_01f7:  stloc.s    V_9
  IL_01f9:  ldloc.s    V_9
  IL_01fb:  brfalse.s  IL_020c
  IL_01fd:  ldloc.s    V_9
  IL_01ff:  ldftn      "Sub VB$AnonymousDelegate_3(Of EventLibrary.voidVoidDelegate).Invoke(EventLibrary.voidVoidDelegate)"
  IL_0205:  newobj     "Sub EventLibrary.voidDelegateDelegate..ctor(Object, System.IntPtr)"
  IL_020a:  br.s       IL_020d
  IL_020c:  ldnull
  IL_020d:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidDelegateDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDelegateDelegate)"
  IL_0212:  ldloc.1
  IL_0213:  dup
  IL_0214:  ldvirtftn  "Sub B.add_d1(EventLibrary.voidVoidDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_021a:  newobj     "Sub System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_021f:  ldloc.1
  IL_0220:  dup
  IL_0221:  ldvirtftn  "Sub B.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0227:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_022c:  ldloc.2
  IL_022d:  stloc.s    V_6
  IL_022f:  ldloc.s    V_6
  IL_0231:  brfalse.s  IL_0242
  IL_0233:  ldloc.s    V_6
  IL_0235:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_023b:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_0240:  br.s       IL_0243
  IL_0242:  ldnull
  IL_0243:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidVoidDelegate)(System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_0248:  ldloc.1
  IL_0249:  dup
  IL_024a:  ldvirtftn  "Sub B.add_d2(EventLibrary.voidStringDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0250:  newobj     "Sub System.Func(Of EventLibrary.voidStringDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0255:  ldloc.1
  IL_0256:  dup
  IL_0257:  ldvirtftn  "Sub B.remove_d2(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_025d:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0262:  ldloc.3
  IL_0263:  stloc.s    V_7
  IL_0265:  ldloc.s    V_7
  IL_0267:  brfalse.s  IL_0278
  IL_0269:  ldloc.s    V_7
  IL_026b:  ldftn      "Sub VB$AnonymousDelegate_1(Of String).Invoke(String)"
  IL_0271:  newobj     "Sub EventLibrary.voidStringDelegate..ctor(Object, System.IntPtr)"
  IL_0276:  br.s       IL_0279
  IL_0278:  ldnull
  IL_0279:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidStringDelegate)(System.Func(Of EventLibrary.voidStringDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidStringDelegate)"
  IL_027e:  ldloc.1
  IL_027f:  dup
  IL_0280:  ldvirtftn  "Sub B.add_d3(EventLibrary.voidDynamicDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0286:  newobj     "Sub System.Func(Of EventLibrary.voidDynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_028b:  ldloc.1
  IL_028c:  dup
  IL_028d:  ldvirtftn  "Sub B.remove_d3(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0293:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0298:  ldloc.s    V_4
  IL_029a:  stloc.s    V_8
  IL_029c:  ldloc.s    V_8
  IL_029e:  brfalse.s  IL_02af
  IL_02a0:  ldloc.s    V_8
  IL_02a2:  ldftn      "Sub VB$AnonymousDelegate_2(Of Object).Invoke(Object)"
  IL_02a8:  newobj     "Sub EventLibrary.voidDynamicDelegate..ctor(Object, System.IntPtr)"
  IL_02ad:  br.s       IL_02b0
  IL_02af:  ldnull
  IL_02b0:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidDynamicDelegate)(System.Func(Of EventLibrary.voidDynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDynamicDelegate)"
  IL_02b5:  ldloc.1
  IL_02b6:  dup
  IL_02b7:  ldvirtftn  "Sub B.add_d4(EventLibrary.voidDelegateDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_02bd:  newobj     "Sub System.Func(Of EventLibrary.voidDelegateDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_02c2:  ldloc.1
  IL_02c3:  dup
  IL_02c4:  ldvirtftn  "Sub B.remove_d4(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_02ca:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_02cf:  ldloc.s    V_5
  IL_02d1:  stloc.s    V_9
  IL_02d3:  ldloc.s    V_9
  IL_02d5:  brfalse.s  IL_02e6
  IL_02d7:  ldloc.s    V_9
  IL_02d9:  ldftn      "Sub VB$AnonymousDelegate_3(Of EventLibrary.voidVoidDelegate).Invoke(EventLibrary.voidVoidDelegate)"
  IL_02df:  newobj     "Sub EventLibrary.voidDelegateDelegate..ctor(Object, System.IntPtr)"
  IL_02e4:  br.s       IL_02e7
  IL_02e6:  ldnull
  IL_02e7:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidDelegateDelegate)(System.Func(Of EventLibrary.voidDelegateDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDelegateDelegate)"
  IL_02ec:  ldloc.1
  IL_02ed:  dup
  IL_02ee:  ldvirtftn  "Sub B.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_02f4:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_02f9:  ldloc.2
  IL_02fa:  stloc.s    V_6
  IL_02fc:  ldloc.s    V_6
  IL_02fe:  brfalse.s  IL_030f
  IL_0300:  ldloc.s    V_6
  IL_0302:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_0308:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_030d:  br.s       IL_0310
  IL_030f:  ldnull
  IL_0310:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidVoidDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_0315:  ldloc.1
  IL_0316:  dup
  IL_0317:  ldvirtftn  "Sub B.remove_d2(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_031d:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0322:  ldloc.3
  IL_0323:  stloc.s    V_7
  IL_0325:  ldloc.s    V_7
  IL_0327:  brfalse.s  IL_0338
  IL_0329:  ldloc.s    V_7
  IL_032b:  ldftn      "Sub VB$AnonymousDelegate_1(Of String).Invoke(String)"
  IL_0331:  newobj     "Sub EventLibrary.voidStringDelegate..ctor(Object, System.IntPtr)"
  IL_0336:  br.s       IL_0339
  IL_0338:  ldnull
  IL_0339:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidStringDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidStringDelegate)"
  IL_033e:  ldloc.1
  IL_033f:  dup
  IL_0340:  ldvirtftn  "Sub B.remove_d3(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0346:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_034b:  ldloc.s    V_4
  IL_034d:  stloc.s    V_8
  IL_034f:  ldloc.s    V_8
  IL_0351:  brfalse.s  IL_0362
  IL_0353:  ldloc.s    V_8
  IL_0355:  ldftn      "Sub VB$AnonymousDelegate_2(Of Object).Invoke(Object)"
  IL_035b:  newobj     "Sub EventLibrary.voidDynamicDelegate..ctor(Object, System.IntPtr)"
  IL_0360:  br.s       IL_0363
  IL_0362:  ldnull
  IL_0363:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidDynamicDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDynamicDelegate)"
  IL_0368:  ldloc.1
  IL_0369:  dup
  IL_036a:  ldvirtftn  "Sub B.remove_d4(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0370:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0375:  ldloc.s    V_5
  IL_0377:  stloc.s    V_9
  IL_0379:  ldloc.s    V_9
  IL_037b:  brfalse.s  IL_038c
  IL_037d:  ldloc.s    V_9
  IL_037f:  ldftn      "Sub VB$AnonymousDelegate_3(Of EventLibrary.voidVoidDelegate).Invoke(EventLibrary.voidVoidDelegate)"
  IL_0385:  newobj     "Sub EventLibrary.voidDelegateDelegate..ctor(Object, System.IntPtr)"
  IL_038a:  br.s       IL_038d
  IL_038c:  ldnull
  IL_038d:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidDelegateDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDelegateDelegate)"
  IL_0392:  ret
}
]]>.Value)
        End Sub

        <Fact()>
        Public Sub WinMdEventInternalStaticAccess()
            Dim src =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports EventLibrary

Public Partial Class A
    Implements I
            ' Remove a delegate from inside of the class
        Public Shared Function Scenario1(a As A) As Boolean
            Dim testDelegate = Sub()
                               End Sub

            ' Setup
            AddHandler a.d1, testDelegate
            RemoveHandler a.d1, testDelegate
            Return a.d1Event Is Nothing
        End Function

        ' Remove a delegate from inside of the class
        Public Function Scenario2() As Boolean
            Dim b As A = Me
            Dim testDelegate = Sub()
                               End Sub

            ' Setup
            AddHandler b.d1, testDelegate
            RemoveHandler b.d1, testDelegate
            Return b.d1Event Is Nothing
        End Function
    End Class
]]>
                    </file>
                    <file name="b.vb">
                        <%= DynamicCommonSrc %>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerifyOnWin8Only(
                src,
                allReferences:={
                    MscorlibRef_v4_0_30316_17626,
                    SystemCoreRef_v4_0_30319_17929,
                    EventLibRef},
                emitOptions:=TestEmitters.RefEmitBug)
            verifier.VerifyDiagnostics()
            verifier.VerifyIL("A.Scenario1", <![CDATA[
{
  // Code size      132 (0x84)
  .maxstack  4
  .locals init (VB$AnonymousDelegate_0 V_0, //testDelegate
                VB$AnonymousDelegate_0 V_1)
  IL_0000:  ldsfld     "A._ClosureCache$__2 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "A._ClosureCache$__2 As <generated method>"
  IL_000c:  br.s       IL_0020
  IL_000e:  ldnull
  IL_000f:  ldftn      "Sub A._Lambda$__1(Object)"
  IL_0015:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_001a:  dup
  IL_001b:  stsfld     "A._ClosureCache$__2 As <generated method>"
  IL_0020:  stloc.0
  IL_0021:  ldarg.0
  IL_0022:  dup
  IL_0023:  ldvirtftn  "Sub A.add_d1(EventLibrary.voidVoidDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0029:  newobj     "Sub System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_002e:  ldarg.0
  IL_002f:  dup
  IL_0030:  ldvirtftn  "Sub A.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0036:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_003b:  ldloc.0
  IL_003c:  stloc.1
  IL_003d:  ldloc.1
  IL_003e:  brfalse.s  IL_004e
  IL_0040:  ldloc.1
  IL_0041:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_0047:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_004c:  br.s       IL_004f
  IL_004e:  ldnull
  IL_004f:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidVoidDelegate)(System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_0054:  ldarg.0
  IL_0055:  dup
  IL_0056:  ldvirtftn  "Sub A.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_005c:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0061:  ldloc.0
  IL_0062:  stloc.1
  IL_0063:  ldloc.1
  IL_0064:  brfalse.s  IL_0074
  IL_0066:  ldloc.1
  IL_0067:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_006d:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_0072:  br.s       IL_0075
  IL_0074:  ldnull
  IL_0075:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidVoidDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_007a:  ldarg.0
  IL_007b:  ldfld      "A.d1Event As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of EventLibrary.voidVoidDelegate)"
  IL_0080:  ldnull
  IL_0081:  ceq
  IL_0083:  ret
}
]]>.Value)
            verifier.VerifyIL("A.Scenario2", <![CDATA[
{
  // Code size      134 (0x86)
  .maxstack  4
  .locals init (A V_0, //b
                VB$AnonymousDelegate_0 V_1, //testDelegate
                VB$AnonymousDelegate_0 V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldsfld     "A._ClosureCache$__4 As <generated method>"
  IL_0007:  brfalse.s  IL_0010
  IL_0009:  ldsfld     "A._ClosureCache$__4 As <generated method>"
  IL_000e:  br.s       IL_0022
  IL_0010:  ldnull
  IL_0011:  ldftn      "Sub A._Lambda$__3(Object)"
  IL_0017:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_001c:  dup
  IL_001d:  stsfld     "A._ClosureCache$__4 As <generated method>"
  IL_0022:  stloc.1
  IL_0023:  ldloc.0
  IL_0024:  dup
  IL_0025:  ldvirtftn  "Sub A.add_d1(EventLibrary.voidVoidDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_002b:  newobj     "Sub System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0030:  ldloc.0
  IL_0031:  dup
  IL_0032:  ldvirtftn  "Sub A.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0038:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_003d:  ldloc.1
  IL_003e:  stloc.2
  IL_003f:  ldloc.2
  IL_0040:  brfalse.s  IL_0050
  IL_0042:  ldloc.2
  IL_0043:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_0049:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_004e:  br.s       IL_0051
  IL_0050:  ldnull
  IL_0051:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidVoidDelegate)(System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_0056:  ldloc.0
  IL_0057:  dup
  IL_0058:  ldvirtftn  "Sub A.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_005e:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0063:  ldloc.1
  IL_0064:  stloc.2
  IL_0065:  ldloc.2
  IL_0066:  brfalse.s  IL_0076
  IL_0068:  ldloc.2
  IL_0069:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_006f:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_0074:  br.s       IL_0077
  IL_0076:  ldnull
  IL_0077:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidVoidDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_007c:  ldloc.0
  IL_007d:  ldfld      "A.d1Event As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of EventLibrary.voidVoidDelegate)"
  IL_0082:  ldnull
  IL_0083:  ceq
  IL_0085:  ret
}
]]>.Value)
        End Sub

        ''' <summary>
        ''' Verify that WinRT events compile into the IL that we 
        ''' would expect.
        ''' </summary>
        <ConditionalFact(GetType(OSVersionWin8))>
        Public Sub WinMdEvent()

            Dim source =
<compilation>
    <file name="a.vb">
    Imports System
    Imports Windows.ApplicationModel
    Imports Windows.UI.Xaml
    Public Class abcdef
	    Private Sub OnSuspending(sender As Object, e As SuspendingEventArgs)
	    End Sub

	    Public Sub foo()
		    Dim application As Application = Nothing
            AddHandler application.Suspending, AddressOf Me.OnSuspending
            RemoveHandler application.Suspending, AddressOf Me.OnSuspending
	    End Sub

	    Public Shared Sub Main()
		    Dim abcdef As abcdef = New abcdef()
		    abcdef.foo()
	    End Sub
    End Class
        </file>
</compilation>
            Dim compilation = CreateWinRtCompilation(source)

            Dim expectedIL = <output>
{
  // Code size       76 (0x4c)
  .maxstack  4
  .locals init (Windows.UI.Xaml.Application V_0) //application
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  dup
  IL_0004:  ldvirtftn  "Sub Windows.UI.Xaml.Application.add_Suspending(Windows.UI.Xaml.SuspendingEventHandler) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_000a:  newobj     "Sub System.Func(Of Windows.UI.Xaml.SuspendingEventHandler, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_000f:  ldloc.0
  IL_0010:  dup
  IL_0011:  ldvirtftn  "Sub Windows.UI.Xaml.Application.remove_Suspending(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0017:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_001c:  ldarg.0
  IL_001d:  ldftn      "Sub abcdef.OnSuspending(Object, Windows.ApplicationModel.SuspendingEventArgs)"
  IL_0023:  newobj     "Sub Windows.UI.Xaml.SuspendingEventHandler..ctor(Object, System.IntPtr)"
  IL_0028:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of Windows.UI.Xaml.SuspendingEventHandler)(System.Func(Of Windows.UI.Xaml.SuspendingEventHandler, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), Windows.UI.Xaml.SuspendingEventHandler)"
  IL_002d:  ldloc.0
  IL_002e:  dup
  IL_002f:  ldvirtftn  "Sub Windows.UI.Xaml.Application.remove_Suspending(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0035:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_003a:  ldarg.0
  IL_003b:  ldftn      "Sub abcdef.OnSuspending(Object, Windows.ApplicationModel.SuspendingEventArgs)"
  IL_0041:  newobj     "Sub Windows.UI.Xaml.SuspendingEventHandler..ctor(Object, System.IntPtr)"
  IL_0046:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of Windows.UI.Xaml.SuspendingEventHandler)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), Windows.UI.Xaml.SuspendingEventHandler)"
  IL_004b:  ret
}
                    </output>
            CompileAndVerify(compilation).VerifyIL("abcdef.foo", expectedIL.Value())
        End Sub

        <Fact()>
        Public Sub WinMdSynthesizedEventDelegate()
            Dim src =
            <compilation>
                <file name="c.vb">
Class C
    Event E(a As Integer)
End Class
                    </file>
            </compilation>

            Dim comp = CreateCompilationWithReferences(
            src,
            references:={MscorlibRef_v4_0_30316_17626},
            options:=TestOptions.ReleaseWinMD)
            comp.VerifyEmitDiagnostics(Diagnostic(ERRID.ERR_WinRTEventWithoutDelegate, "E"))
        End Sub


        ''' <summary>
        ''' Verify that WinRT events compile into the IL that we 
        ''' would expect.
        ''' </summary>
        Public Sub WinMdEventLambda()

            Dim source =
<compilation>
    <file name="a.vb">
    Imports System
    Imports Windows.ApplicationModel
    Imports Windows.UI.Xaml
    Public Class abcdef
	    Private Sub OnSuspending(sender As Object, e As SuspendingEventArgs)
	    End Sub

	    Public Sub foo()
		    Dim application As Application = Nothing
            AddHandler application.Suspending, Sub(sender as Object, e As SuspendingEventArgs)
                                            End Sub
	    End Sub

	    Public Shared Sub Main()
		    Dim abcdef As abcdef = New abcdef()
		    abcdef.foo()
	    End Sub
    End Class
        </file>
</compilation>
            Dim compilation = CreateWinRtCompilation(source)

            Dim expectedIL =
            <output>
{
  // Code size       66 (0x42)
  .maxstack  4
  .locals init (Windows.UI.Xaml.Application V_0) //application
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  dup
  IL_0004:  ldvirtftn  "Sub Windows.UI.Xaml.Application.add_Suspending(Windows.UI.Xaml.SuspendingEventHandler) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_000a:  newobj     "Sub System.Func(Of Windows.UI.Xaml.SuspendingEventHandler, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_000f:  ldloc.0
  IL_0010:  dup
  IL_0011:  ldvirtftn  "Sub Windows.UI.Xaml.Application.remove_Suspending(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0017:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_001c:  ldsfld     "abcdef._ClosureCache$__2 As Windows.UI.Xaml.SuspendingEventHandler"
  IL_0021:  brfalse.s  IL_002a
  IL_0023:  ldsfld     "abcdef._ClosureCache$__2 As Windows.UI.Xaml.SuspendingEventHandler"
  IL_0028:  br.s       IL_003c
  IL_002a:  ldnull
  IL_002b:  ldftn      "Sub abcdef._Lambda$__1(Object, Object, Windows.ApplicationModel.SuspendingEventArgs)"
  IL_0031:  newobj     "Sub Windows.UI.Xaml.SuspendingEventHandler..ctor(Object, System.IntPtr)"
  IL_0036:  dup
  IL_0037:  stsfld     "abcdef._ClosureCache$__2 As Windows.UI.Xaml.SuspendingEventHandler"
  IL_003c:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of Windows.UI.Xaml.SuspendingEventHandler)(System.Func(Of Windows.UI.Xaml.SuspendingEventHandler, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), Windows.UI.Xaml.SuspendingEventHandler)"
  IL_0041:  ret
}
                    </output>

            CompileAndVerify(compilation, verify:=OSVersion.IsWin8).VerifyIL("abcdef.foo", expectedIL.Value())
        End Sub

        <Fact>
        Public Sub IsWindowsRuntimeEvent_EventSymbolSubtypes()
            Dim il = <![CDATA[
                    .class public auto ansi sealed Event
                           extends [mscorlib]System.MulticastDelegate
                    {
                      .method private hidebysig specialname rtspecialname 
                              instance void  .ctor(object 'object',
                                                   native int 'method') runtime managed
                      {
                      }

                      .method public hidebysig newslot specialname virtual 
                              instance void  Invoke() runtime managed
                      {
                      }

                    } // end of class Event

                    .class interface public abstract auto ansi Interface`1<T>
                    {
                      .method public hidebysig newslot specialname abstract virtual 
                              instance void  add_Normal(class Event 'value') cil managed
                      {
                      }

                      .method public hidebysig newslot specialname abstract virtual 
                              instance void  remove_Normal(class Event 'value') cil managed
                      {
                      }

                      .method public hidebysig newslot specialname abstract virtual 
                              instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 
                              add_WinRT([in] class Event 'value') cil managed
                      {
                      }

                      .method public hidebysig newslot specialname abstract virtual 
                              instance void  remove_WinRT([in] valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 'value') cil managed
                      {
                      }

                      .event Event Normal
                      {
                        .addon instance void Interface`1::add_Normal(class Event)
                        .removeon instance void Interface`1::remove_Normal(class Event)
                      } // end of event I`1::Normal

                      .event Event WinRT
                      {
                        .addon instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken Interface`1::add_WinRT(class Event)
                        .removeon instance void Interface`1::remove_WinRT(valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
                      }
                    } // end of class Interface
                    ]]>
            Dim source =
<compilation>
    <file name="a.vb">
Class C
    Implements [Interface](Of Integer)

    Public Event Normal() Implements [Interface](Of Integer).Normal

    Public Event WinRT() Implements [Interface](Of Integer).WinRT

End Class
        </file>
</compilation>

            Dim ilRef = CompileIL(il.Value)
            Dim comp = CreateCompilationWithReferences(source, WinRtRefs.Concat({ilRef}))
            comp.VerifyDiagnostics()

            Dim interfaceType = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Interface")
            Dim interfaceNormalEvent = interfaceType.GetMember(Of EventSymbol)("Normal")
            Dim interfaceWinRTEvent = interfaceType.GetMember(Of EventSymbol)("WinRT")

            Assert.IsType(Of PEEventSymbol)(interfaceNormalEvent)
            Assert.IsType(Of PEEventSymbol)(interfaceWinRTEvent)

            ' Only depends on accessor signatures - doesn't care if it's in a windowsruntime type.
            Assert.False(interfaceNormalEvent.IsWindowsRuntimeEvent)
            Assert.True(interfaceWinRTEvent.IsWindowsRuntimeEvent)

            Dim implementingType = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Dim implementingNormalEvent = implementingType.GetMembers().OfType(Of EventSymbol)().Single(Function(e) e.Name.Contains("Normal"))
            Dim implementingWinRTEvent = implementingType.GetMembers().OfType(Of EventSymbol)().Single(Function(e) e.Name.Contains("WinRT"))

            Assert.IsType(Of SourceEventSymbol)(implementingNormalEvent)
            Assert.IsType(Of SourceEventSymbol)(implementingWinRTEvent)

            ' Based on kind of explicitly implemented interface event (other checks to be tested separately).
            Assert.False(implementingNormalEvent.IsWindowsRuntimeEvent)
            Assert.True(implementingWinRTEvent.IsWindowsRuntimeEvent)

            Dim subsitutedNormalEvent = implementingNormalEvent.ExplicitInterfaceImplementations.Single()
            Dim subsitutedWinRTEvent = implementingWinRTEvent.ExplicitInterfaceImplementations.Single()

            Assert.IsType(Of SubstitutedEventSymbol)(subsitutedNormalEvent)
            Assert.IsType(Of SubstitutedEventSymbol)(subsitutedWinRTEvent)

            ' Based on original definition.
            Assert.False(subsitutedNormalEvent.IsWindowsRuntimeEvent)
            Assert.True(subsitutedWinRTEvent.IsWindowsRuntimeEvent)

            Dim retargetingAssembly = New RetargetingAssemblySymbol(DirectCast(comp.Assembly, SourceAssemblySymbol), isLinked:=False)
            retargetingAssembly.SetCorLibrary(comp.Assembly.CorLibrary)

            Dim retargetingType = retargetingAssembly.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Dim retargetingNormalEvent = retargetingType.GetMembers().OfType(Of EventSymbol)().Single(Function(e) e.Name.Contains("Normal"))
            Dim retargetingWinRTEvent = retargetingType.GetMembers().OfType(Of EventSymbol)().Single(Function(e) e.Name.Contains("WinRT"))

            Assert.IsType(Of RetargetingEventSymbol)(retargetingNormalEvent)
            Assert.IsType(Of RetargetingEventSymbol)(retargetingWinRTEvent)

            ' Based on underlying symbol.
            Assert.False(retargetingNormalEvent.IsWindowsRuntimeEvent)
            Assert.True(retargetingWinRTEvent.IsWindowsRuntimeEvent)
        End Sub

        <Fact>
        Public Sub IsWindowsRuntimeEvent_Source_OutputKind()
            Dim source =
<compilation>
    <file name="a.vb">
Class C
    Public Event E As System.Action

    Shared Sub Main()
    End Sub
End Class

Interface I
    Event E As System.Action
End Interface
        </file>
</compilation>

            For Each kind As OutputKind In [Enum].GetValues(GetType(OutputKind))
                Dim comp = CreateCompilationWithReferences(source, WinRtRefs, New VBCompilationOptions(kind))
                comp.VerifyDiagnostics()

                Dim [class] = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                Dim classEvent = [class].GetMember(Of EventSymbol)("E")

                ' Specifically test interfaces because they follow a different code path.
                Dim [interface] = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("I")
                Dim interfaceEvent = [interface].GetMember(Of EventSymbol)("E")

                Assert.Equal(kind.IsWindowsRuntime(), classEvent.IsWindowsRuntimeEvent)
                Assert.Equal(kind.IsWindowsRuntime(), interfaceEvent.IsWindowsRuntimeEvent)
            Next
        End Sub

        <Fact>
        Public Sub IsWindowsRuntimeEvent_Source_InterfaceImplementation()
            Dim source =
<compilation>
    <file name="a.vb">
Class C
    Implements I

    Public Event Normal Implements I.Normal
    Public Event WinRT Implements I.WinRT

    Shared Sub Main()
    End Sub
End Class
        </file>
</compilation>

            Dim ilRef = CompileIL(String.Format(EventInterfaceILTemplate, "I"))

            For Each kind As OutputKind In [Enum].GetValues(GetType(OutputKind))
                Dim comp = CreateCompilationWithReferences(source, WinRtRefs.Concat({ilRef}), New VBCompilationOptions(kind))
                comp.VerifyDiagnostics()

                Dim [class] = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                Dim normalEvent = [class].GetMember(Of EventSymbol)("Normal")
                Dim winRTEvent = [class].GetMember(Of EventSymbol)("WinRT")

                Assert.False(normalEvent.IsWindowsRuntimeEvent)
                Assert.True(winRTEvent.IsWindowsRuntimeEvent)
            Next
        End Sub

        <Fact>
        Public Sub ERR_MixingWinRTAndNETEvents()
            Dim source =
<compilation>
    <file name="a.vb">
' Fine to implement more than one interface of the same WinRT-ness
Class C1
    Implements I1, I2

    Public Event Normal Implements I1.Normal, I2.Normal
    Public Event WinRT Implements I1.WinRT, I2.WinRT
End Class

' Error to implement two interfaces of different WinRT-ness
Class C2
    Implements I1, I2

    Public Event Normal Implements I1.Normal, I2.WinRT
    Public Event WinRT Implements I1.WinRT, I2.Normal
End Class
        </file>
</compilation>

            Dim ilRef = CompileIL(String.Format(EventInterfaceILTemplate, "I1") + String.Format(EventInterfaceILTemplate, "I2"))

            Dim comp = CreateCompilationWithReferences(source, WinRtRefs.Concat({ilRef}), TestOptions.ReleaseDll)
            comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_MixingWinRTAndNETEvents, "I2.WinRT").WithArguments("Normal", "I2.WinRT", "I1.Normal"),
            Diagnostic(ERRID.ERR_MixingWinRTAndNETEvents, "I2.Normal").WithArguments("WinRT", "I1.WinRT", "I2.Normal"))

            Dim c1 = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C1")
            Assert.False(c1.GetMember(Of EventSymbol)("Normal").IsWindowsRuntimeEvent)
            Assert.True(c1.GetMember(Of EventSymbol)("WinRT").IsWindowsRuntimeEvent)

            Dim c2 = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C2")
            Assert.False(c2.GetMember(Of EventSymbol)("Normal").IsWindowsRuntimeEvent)
            Assert.True(c2.GetMember(Of EventSymbol)("WinRT").IsWindowsRuntimeEvent)
        End Sub

        <Fact>
        Public Sub ERR_MixingWinRTAndNETEvents_Multiple()
            Dim source =
<compilation>
    <file name="a.vb">
' Try going back and forth
Class C3
    Implements I1, I2

    Public Event Normal Implements I1.Normal, I1.WinRT, I2.Normal, I2.WinRT
End Class
        </file>
</compilation>

            Dim ilRef = CompileIL(String.Format(EventInterfaceILTemplate, "I1") + String.Format(EventInterfaceILTemplate, "I2"))

            Dim comp = CreateCompilationWithReferences(source, WinRtRefs.Concat({ilRef}), TestOptions.ReleaseDll)

            ' CONSIDER: This is not how dev11 handles this scenario: it reports the first diagnostic, but then reports
            ' ERR_IdentNotMemberOfInterface4 (BC30401) and ERR_UnimplementedMember3 (BC30149) for all subsequent implemented members
            ' (side-effects of calling SetIsBad on the implementing event).
            comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_MixingWinRTAndNETEvents, "I1.WinRT").WithArguments("Normal", "I1.WinRT", "I1.Normal"),
            Diagnostic(ERRID.ERR_MixingWinRTAndNETEvents, "I2.WinRT").WithArguments("Normal", "I2.WinRT", "I1.Normal"))

            Dim c3 = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C3")
            Assert.False(c3.GetMember(Of EventSymbol)("Normal").IsWindowsRuntimeEvent)
        End Sub

        <Fact>
        Public Sub ERR_WinRTEventWithoutDelegate_FieldLike()
            Dim source =
<compilation>
    <file name="a.vb">
Class Test
    Public Event E ' As System.Action
End Class
        </file>
</compilation>

            Dim comp = CreateCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)
            comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_WinRTEventWithoutDelegate, "E"))

            Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.True(type.GetMember(Of EventSymbol)("E").IsWindowsRuntimeEvent)
        End Sub

        <Fact>
        Public Sub ERR_WinRTEventWithoutDelegate_Custom()
            Dim source =
<compilation>
    <file name="a.vb">
Class Test
    Public Custom Event E ' As System.Action
        AddHandler(value As System.Action)
            Return Nothing
        End AddHandler

        RemoveHandler(value As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event
End Class
        </file>
</compilation>

            Dim comp = CreateCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)
            ' Once the as-clause is missing, the event is parsed as a field-like event, hence the many syntax errors.
            comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_WinRTEventWithoutDelegate, "E"),
            Diagnostic(ERRID.ERR_CustomEventRequiresAs, "Public Custom Event E ' As System.Action" + Environment.NewLine),
            Diagnostic(ERRID.ERR_ExpectedDeclaration, "AddHandler(value As System.Action)"),
            Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Return Nothing"),
            Diagnostic(ERRID.ERR_InvalidEndAddHandler, "End AddHandler"),
            Diagnostic(ERRID.ERR_ExpectedDeclaration, "RemoveHandler(value As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"),
            Diagnostic(ERRID.ERR_InvalidEndRemoveHandler, "End RemoveHandler"),
            Diagnostic(ERRID.ERR_ExpectedDeclaration, "RaiseEvent()"),
            Diagnostic(ERRID.ERR_InvalidEndRaiseEvent, "End RaiseEvent"),
            Diagnostic(ERRID.ERR_InvalidEndEvent, "End Event"))

            Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.True(type.GetMember(Of EventSymbol)("E").IsWindowsRuntimeEvent)
        End Sub

        <Fact>
        Public Sub ERR_WinRTEventWithoutDelegate_Implements()
            Dim source =
<compilation>
    <file name="a.vb">
Interface I

    Event E1 As System.Action
    Event E2 As System.Action

End Interface

Class Test
    Implements I

    Public Event E1 Implements I.E1

    Public Custom Event E2 Implements I.E2
        AddHandler(value As System.Action)
            Return Nothing
        End AddHandler

        RemoveHandler(value As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event
End Class
        </file>
</compilation>

            Dim comp = CreateCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)

            ' Everything goes sideways for E2 since it custom events are required to have as-clauses.
            ' The key thing is that neither event reports ERR_WinRTEventWithoutDelegate.
            comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_CustomEventRequiresAs, "Public Custom Event E2 Implements I.E2"),
            Diagnostic(ERRID.ERR_ExpectedDeclaration, "AddHandler(value As System.Action)"),
            Diagnostic(ERRID.ERR_ExecutableAsDeclaration, "Return Nothing"),
            Diagnostic(ERRID.ERR_InvalidEndAddHandler, "End AddHandler"),
            Diagnostic(ERRID.ERR_ExpectedDeclaration, "RemoveHandler(value As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"),
            Diagnostic(ERRID.ERR_InvalidEndRemoveHandler, "End RemoveHandler"),
            Diagnostic(ERRID.ERR_ExpectedDeclaration, "RaiseEvent()"),
            Diagnostic(ERRID.ERR_InvalidEndRaiseEvent, "End RaiseEvent"),
            Diagnostic(ERRID.ERR_InvalidEndEvent, "End Event"),
            Diagnostic(ERRID.ERR_EventImplMismatch5, "I.E2").WithArguments("Public Event E2 As ?", "Event E2 As System.Action", "I", "?", "System.Action"))

            Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.True(type.GetMember(Of EventSymbol)("E1").IsWindowsRuntimeEvent)
            Assert.True(type.GetMember(Of EventSymbol)("E2").IsWindowsRuntimeEvent)
        End Sub

        <Fact>
        Public Sub ERR_AddParamWrongForWinRT()
            Dim source =
<compilation>
    <file name="a.vb">
Class Test
    Public Custom Event E As System.Action
        AddHandler(value As System.Action(Of Integer))
            Return Nothing
        End AddHandler

        RemoveHandler(value As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event
End Class
        </file>
</compilation>

            Dim comp = CreateCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)

            comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_AddParamWrongForWinRT, "AddHandler(value As System.Action(Of Integer))"))

            Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.True(type.GetMember(Of EventSymbol)("E").IsWindowsRuntimeEvent)
        End Sub

        <Fact>
        Public Sub ERR_RemoveParamWrongForWinRT()
            Dim source =
<compilation>
    <file name="a.vb">
Class Test
    Public Custom Event E As System.Action
        AddHandler(value As System.Action)
            Return Nothing
        End AddHandler

        RemoveHandler(value As System.Action)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event
End Class
        </file>
</compilation>

            Dim comp = CreateCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)

            comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_RemoveParamWrongForWinRT, "RemoveHandler(value As System.Action)"))

            Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.True(type.GetMember(Of EventSymbol)("E").IsWindowsRuntimeEvent)
        End Sub

        <Fact>
        Public Sub ERR_RemoveParamWrongForWinRT_MissingTokenType()
            Dim source =
<compilation>
    <file name="a.vb">
Class Test
    Public Custom Event E As System.Action
        AddHandler(value As System.Action)
            Return Nothing
        End AddHandler

        RemoveHandler(value As System.Action)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event
End Class
        </file>
</compilation>

            Dim comp = CreateCompilationWithReferences(source, {MscorlibRef}, TestOptions.ReleaseWinMD)

            ' This diagnostic is from binding the return type of the AddHandler, not from checking the parameter type
            ' of the ReturnHandler.  The key point is that a cascading ERR_RemoveParamWrongForWinRT is not reported.
            comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_TypeRefResolutionError3, <![CDATA[AddHandler(value As System.Action)
            Return Nothing
        End AddHandler]]>.Value.Replace(vbLf, vbCrLf)).WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", comp.AssemblyName + ".winmdobj"))

            Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.True(type.GetMember(Of EventSymbol)("E").IsWindowsRuntimeEvent)
        End Sub

        <Fact>
        Public Sub ERR_EventImplRemoveHandlerParamWrong()
            Dim source =
<compilation>
    <file name="a.vb">
Interface I
    Event E As System.Action
end Interface

Class Test
    Implements I

    Public Custom Event F As System.Action Implements I.E
        AddHandler(value As System.Action)
            Return Nothing
        End AddHandler

        RemoveHandler(value As System.Action)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event
End Class
        </file>
</compilation>

            Dim comp = CreateCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)

            comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_EventImplRemoveHandlerParamWrong, "RemoveHandler(value As System.Action)").WithArguments("F", "E", "I"))

            Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.True(type.GetMember(Of EventSymbol)("F").IsWindowsRuntimeEvent)
        End Sub

        <Fact>
        Public Sub ERR_EventImplRemoveHandlerParamWrong_MissingTokenType()
            Dim source =
<compilation>
    <file name="a.vb">
Interface I
    Event E As System.Action
end Interface

Class Test
    Implements I

    Public Custom Event F As System.Action Implements I.E
        AddHandler(value As System.Action)
            Return Nothing
        End AddHandler

        RemoveHandler(value As System.Action)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event
End Class
        </file>
</compilation>

            Dim comp = CreateCompilationWithReferences(source, {MscorlibRef}, TestOptions.ReleaseWinMD)

            ' This diagnostic is from binding the return type of the AddHandler, not from checking the parameter type
            ' of the ReturnHandler.  The key point is that a cascading ERR_RemoveParamWrongForWinRT is not reported.
            Dim outputName As String = comp.AssemblyName + ".winmdobj"
            comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_TypeRefResolutionError3, <![CDATA[AddHandler(value As System.Action)
            Return Nothing
        End AddHandler]]>.Value.Replace(vbLf, vbCrLf)).WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", outputName),
            Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", outputName),
            Diagnostic(ERRID.ERR_TypeRefResolutionError3, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken", outputName))

            Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.True(type.GetMember(Of EventSymbol)("F").IsWindowsRuntimeEvent)
        End Sub

        ' Confirms that we're getting decl errors from the backing field.
        <Fact>
        Public Sub MissingTokenTableType()
            Dim source =
<compilation name="test">
    <file name="a.vb">
Class Test
    Event E As System.Action    
End Class

Namespace System.Runtime.InteropServices.WindowsRuntime
    Public Structure EventRegistrationToken
    End Structure
End Namespace
        </file>
</compilation>

            Dim comp = CreateCompilationWithReferences(source, {MscorlibRef}, TestOptions.ReleaseWinMD)
            AssertTheseDeclarationDiagnostics(comp, <errors><![CDATA[
BC31091: Import of type 'EventRegistrationTokenTable(Of )' from assembly or module 'test.winmdobj' failed.
    Event E As System.Action    
          ~
]]></errors>)
        End Sub

        <Fact>
        Public Sub ReturnLocal()
            Dim source =
<compilation>
    <file name="a.vb">
Class Test
    Public Custom Event E As System.Action
        AddHandler(value As System.Action)
            add_E = Nothing
        End AddHandler

        RemoveHandler(value As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event
End Class
        </file>
</compilation>

            Dim comp = CreateCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)
            Dim v = CompileAndVerify(comp)

            Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Dim eventSymbol As EventSymbol = type.GetMember(Of EventSymbol)("E")
            Assert.True(eventSymbol.IsWindowsRuntimeEvent)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim syntax = tree.GetRoot().DescendantNodes().OfType(Of AssignmentStatementSyntax).Single().Left

            Dim symbol = model.GetSymbolInfo(syntax).Symbol
            Assert.Equal(SymbolKind.Local, symbol.Kind)
            Assert.Equal(eventSymbol.AddMethod.ReturnType, DirectCast(symbol, LocalSymbol).Type)
            Assert.Equal(eventSymbol.AddMethod.Name, symbol.Name)

            v.VerifyIL("Test.add_E", "
{
  // Code size       10 (0xa)
  .maxstack  1
  .locals init (System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken V_0) //add_E
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken""
  IL_0008:  ldloc.0
  IL_0009:  ret
}
")
        End Sub

        <Fact>
        Public Sub AccessorSignatures()
            Dim source =
<compilation>
    <file name="a.vb">
Class Test
    public event FieldLike As System.Action

    Public Custom Event Custom As System.Action
        AddHandler(value As System.Action)
            Throw New System.Exception()
        End AddHandler

        RemoveHandler(value As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event

    Public Shared Sub Main()
    End Sub
End Class
        </file>
</compilation>

            For Each kind As OutputKind In [Enum].GetValues(GetType(OutputKind))
                Dim comp = CreateCompilationWithReferences(source, WinRtRefs, New VBCompilationOptions(kind))

                Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
                Dim fieldLikeEvent = type.GetMember(Of EventSymbol)("FieldLike")
                Dim customEvent = type.GetMember(Of EventSymbol)("Custom")

                If kind.IsWindowsRuntime() Then
                    comp.VerifyDiagnostics()

                    VerifyWinRTEventShape(customEvent, comp)
                    VerifyWinRTEventShape(fieldLikeEvent, comp)
                Else
                    comp.VerifyDiagnostics(
                    Diagnostic(ERRID.ERR_AddRemoveParamNotEventType, "RemoveHandler(value As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"))

                    VerifyNormalEventShape(customEvent, comp)
                    VerifyNormalEventShape(fieldLikeEvent, comp)
                End If
            Next
        End Sub

        <Fact()>
        Public Sub HandlerSemanticInfo()
            Dim source =
            <compilation>
                <file name="a.vb">
Class C
    Event QQQ As System.Action

    Sub Test()
        AddHandler QQQ, Nothing
        RemoveHandler QQQ, Nothing
        RaiseEvent QQQ()
    End Sub
End Class
                    </file>
            </compilation>

            Dim comp = CreateCompilationWithReferences(source, WinRtRefs, options:=TestOptions.ReleaseWinMD)
            comp.VerifyDiagnostics()

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim references = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax).Where(Function(id) id.Identifier.ValueText = "QQQ").ToArray()
            Assert.Equal(3, references.Count) ' Decl is just a token

            Dim eventSymbol = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").GetMember(Of EventSymbol)("QQQ")
            AssertEx.All(references, Function(ref) model.GetSymbolInfo(ref).Symbol.Equals(eventSymbol))

            Dim actionType = comp.GetWellKnownType(WellKnownType.System_Action)
            Assert.Equal(actionType, eventSymbol.Type)
            AssertEx.All(references, Function(ref) model.GetTypeInfo(ref).Type.Equals(actionType))
        End Sub

        <Fact>
        Public Sub NoReturnFromAddHandler()
            Dim source =
<compilation>
    <file name="a.vb">
Delegate Sub EventDelegate()

Class Events
    Custom Event E As eventdelegate
        AddHandler(value As eventdelegate)

        End AddHandler

        RemoveHandler(value As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event
End Class
        </file>
</compilation>

            Dim comp = CreateCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)
            ' Note the distinct new error code.
            comp.VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgNoRetValWinRtEventVal1, "End AddHandler").WithArguments("E"))
        End Sub

        Private Shared Sub VerifyWinRTEventShape([event] As EventSymbol, compilation As VBCompilation)
            Assert.True([event].IsWindowsRuntimeEvent)

            Dim eventType = [event].Type
            Dim tokenType = compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken)
            Assert.NotNull(tokenType)
            Dim voidType = compilation.GetSpecialType(SpecialType.System_Void)
            Assert.NotNull(voidType)

            Dim addMethod = [event].AddMethod
            Assert.Equal(tokenType, addMethod.ReturnType)
            Assert.False(addMethod.IsSub)
            Assert.Equal(1, addMethod.ParameterCount)
            Assert.Equal(eventType, addMethod.Parameters.Single().Type)

            Dim removeMethod = [event].RemoveMethod
            Assert.Equal(voidType, removeMethod.ReturnType)
            Assert.True(removeMethod.IsSub)
            Assert.Equal(1, removeMethod.ParameterCount)
            Assert.Equal(tokenType, removeMethod.Parameters.Single().Type)

            If [event].HasAssociatedField Then
                Dim expectedFieldType = compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T).Construct(eventType)
                Assert.Equal(expectedFieldType, [event].AssociatedField.Type)
            Else
                Assert.Null([event].AssociatedField)
            End If
        End Sub

        Private Shared Sub VerifyNormalEventShape([event] As EventSymbol, compilation As VBCompilation)
            Assert.False([event].IsWindowsRuntimeEvent)

            Dim eventType = [event].Type
            Dim voidType = compilation.GetSpecialType(SpecialType.System_Void)
            Assert.NotNull(voidType)

            Dim addMethod = [event].AddMethod
            Assert.Equal(voidType, addMethod.ReturnType)
            Assert.True(addMethod.IsSub)
            Assert.Equal(1, addMethod.ParameterCount)
            Assert.Equal(eventType, addMethod.Parameters.Single().Type)

            Dim removeMethod = [event].RemoveMethod
            Assert.Equal(voidType, removeMethod.ReturnType)
            Assert.True(removeMethod.IsSub)
            Assert.Equal(1, removeMethod.ParameterCount)
            If [event].HasAssociatedField Then
                ' Otherwise, we had to be explicit and we favored WinRT because that's what we're testing.
                Assert.Equal(eventType, removeMethod.Parameters.Single().Type)
            End If

            If [event].HasAssociatedField Then
                Assert.Equal(eventType, [event].AssociatedField.Type)
            Else
                Assert.Null([event].AssociatedField)
            End If
        End Sub

        <Fact>
        Public Sub BackingField()
            Dim source =
<compilation>
    <file name="a.vb">
Class Test
    Public Custom Event CustomEvent As System.Action
        AddHandler(value As System.Action)
            Return Nothing
        End AddHandler

        RemoveHandler(value As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)

        End RemoveHandler

        RaiseEvent()

        End RaiseEvent
    End Event

    Public Event FieldLikeEvent As System.Action

    Sub Test()
        dim f1 = CustomEventEvent
        dim f2 = FieldLikeEventEvent
    End Sub
End Class
        </file>
</compilation>

            Dim comp = CreateCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)
            ' No backing field for custom event.
            comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_NameNotDeclared1, "CustomEventEvent").WithArguments("CustomEventEvent"))

            Dim fieldLikeEvent = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test").GetMember(Of EventSymbol)("FieldLikeEvent")
            Dim tokenTableType = comp.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T)

            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)

            Dim syntax = tree.GetRoot().DescendantNodes().OfType(Of IdentifierNameSyntax)().Single(Function(id) id.Identifier.ValueText = "FieldLikeEventEvent")

            Dim symbol = model.GetSymbolInfo(syntax).Symbol
            Assert.Equal(SymbolKind.Field, symbol.Kind)
            Assert.Equal(fieldLikeEvent, DirectCast(symbol, FieldSymbol).AssociatedSymbol)

            Dim type = model.GetTypeInfo(syntax).Type
            Assert.Equal(tokenTableType, type.OriginalDefinition)
            Assert.Equal(fieldLikeEvent.Type, DirectCast(type, NamedTypeSymbol).TypeArguments.Single())
        End Sub

        <Fact()>
        Public Sub RaiseBaseEventedFromDerivedNestedTypes()
            Dim source =
<compilation>
    <file name="filename.vb">
Delegate Sub D()

Class C1
    Event HelloWorld As D
    Class C2
        Inherits C1
        Sub t
            RaiseEvent HelloWorld
        End Sub
    End Class
End Class
    </file>
</compilation>
            CreateCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD).VerifyDiagnostics()
        End Sub

    End Class
End Namespace
