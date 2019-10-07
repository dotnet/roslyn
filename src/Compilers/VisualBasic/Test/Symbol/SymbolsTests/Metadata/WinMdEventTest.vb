' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        Private ReadOnly _eventInterfaceILTemplate As String = <![CDATA[
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
        Private ReadOnly _eventLibRef As MetadataReference

        Private ReadOnly _dynamicCommonSrc As XElement =
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
            ' The following two libraries are shrunk code pulled from
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
            _eventLibRef = CreateEmptyCompilationWithReferences(
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
            Dim dynamicCommonRef As MetadataReference = CreateEmptyCompilationWithReferences(
                _dynamicCommonSrc,
                references:={
                    MscorlibRef_v4_0_30316_17626,
                    _eventLibRef},
                options:=TestOptions.ReleaseModule).EmitToImageReference()

            Dim verifier = CompileAndVerifyOnWin8Only(
                src,
                allReferences:={
                    MscorlibRef_v4_0_30316_17626,
                    SystemCoreRef_v4_0_30319_17929,
                    CSharpRef,
                    _eventLibRef,
                    dynamicCommonRef})
            verifier.VerifyIL("C.Main", <![CDATA[
{
  // Code size      931 (0x3a3)
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
  IL_000c:  ldsfld     "C._Closure$__.$I1-0 As <generated method>"
  IL_0011:  brfalse.s  IL_001a
  IL_0013:  ldsfld     "C._Closure$__.$I1-0 As <generated method>"
  IL_0018:  br.s       IL_0030
  IL_001a:  ldsfld     "C._Closure$__.$I As C._Closure$__"
  IL_001f:  ldftn      "Sub C._Closure$__._Lambda$__1-0()"
  IL_0025:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_002a:  dup
  IL_002b:  stsfld     "C._Closure$__.$I1-0 As <generated method>"
  IL_0030:  stloc.2
  IL_0031:  ldsfld     "C._Closure$__.$I1-1 As <generated method>"
  IL_0036:  brfalse.s  IL_003f
  IL_0038:  ldsfld     "C._Closure$__.$I1-1 As <generated method>"
  IL_003d:  br.s       IL_0055
  IL_003f:  ldsfld     "C._Closure$__.$I As C._Closure$__"
  IL_0044:  ldftn      "Sub C._Closure$__._Lambda$__1-1(String)"
  IL_004a:  newobj     "Sub VB$AnonymousDelegate_1(Of String)..ctor(Object, System.IntPtr)"
  IL_004f:  dup
  IL_0050:  stsfld     "C._Closure$__.$I1-1 As <generated method>"
  IL_0055:  stloc.3
  IL_0056:  ldsfld     "C._Closure$__.$I1-2 As <generated method>"
  IL_005b:  brfalse.s  IL_0064
  IL_005d:  ldsfld     "C._Closure$__.$I1-2 As <generated method>"
  IL_0062:  br.s       IL_007a
  IL_0064:  ldsfld     "C._Closure$__.$I As C._Closure$__"
  IL_0069:  ldftn      "Sub C._Closure$__._Lambda$__1-2(Object)"
  IL_006f:  newobj     "Sub VB$AnonymousDelegate_2(Of Object)..ctor(Object, System.IntPtr)"
  IL_0074:  dup
  IL_0075:  stsfld     "C._Closure$__.$I1-2 As <generated method>"
  IL_007a:  stloc.s    V_4
  IL_007c:  ldsfld     "C._Closure$__.$I1-3 As <generated method>"
  IL_0081:  brfalse.s  IL_008a
  IL_0083:  ldsfld     "C._Closure$__.$I1-3 As <generated method>"
  IL_0088:  br.s       IL_00a0
  IL_008a:  ldsfld     "C._Closure$__.$I As C._Closure$__"
  IL_008f:  ldftn      "Sub C._Closure$__._Lambda$__1-3(EventLibrary.voidVoidDelegate)"
  IL_0095:  newobj     "Sub VB$AnonymousDelegate_3(Of EventLibrary.voidVoidDelegate)..ctor(Object, System.IntPtr)"
  IL_009a:  dup
  IL_009b:  stsfld     "C._Closure$__.$I1-3 As <generated method>"
  IL_00a0:  stloc.s    V_5
  IL_00a2:  ldloc.0
  IL_00a3:  dup
  IL_00a4:  ldvirtftn  "Sub A.add_d1(EventLibrary.voidVoidDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_00aa:  newobj     "Sub System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00af:  ldloc.0
  IL_00b0:  dup
  IL_00b1:  ldvirtftn  "Sub A.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_00b7:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00bc:  ldloc.2
  IL_00bd:  stloc.s    V_6
  IL_00bf:  ldloc.s    V_6
  IL_00c1:  brfalse.s  IL_00d2
  IL_00c3:  ldloc.s    V_6
  IL_00c5:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_00cb:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_00d0:  br.s       IL_00d3
  IL_00d2:  ldnull
  IL_00d3:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidVoidDelegate)(System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_00d8:  ldloc.0
  IL_00d9:  dup
  IL_00da:  ldvirtftn  "Sub A.add_d2(EventLibrary.voidStringDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_00e0:  newobj     "Sub System.Func(Of EventLibrary.voidStringDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00e5:  ldloc.0
  IL_00e6:  dup
  IL_00e7:  ldvirtftn  "Sub A.remove_d2(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_00ed:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_00f2:  ldloc.3
  IL_00f3:  stloc.s    V_7
  IL_00f5:  ldloc.s    V_7
  IL_00f7:  brfalse.s  IL_0108
  IL_00f9:  ldloc.s    V_7
  IL_00fb:  ldftn      "Sub VB$AnonymousDelegate_1(Of String).Invoke(String)"
  IL_0101:  newobj     "Sub EventLibrary.voidStringDelegate..ctor(Object, System.IntPtr)"
  IL_0106:  br.s       IL_0109
  IL_0108:  ldnull
  IL_0109:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidStringDelegate)(System.Func(Of EventLibrary.voidStringDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidStringDelegate)"
  IL_010e:  ldloc.0
  IL_010f:  dup
  IL_0110:  ldvirtftn  "Sub A.add_d3(EventLibrary.voidDynamicDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0116:  newobj     "Sub System.Func(Of EventLibrary.voidDynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_011b:  ldloc.0
  IL_011c:  dup
  IL_011d:  ldvirtftn  "Sub A.remove_d3(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0123:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0128:  ldloc.s    V_4
  IL_012a:  stloc.s    V_8
  IL_012c:  ldloc.s    V_8
  IL_012e:  brfalse.s  IL_013f
  IL_0130:  ldloc.s    V_8
  IL_0132:  ldftn      "Sub VB$AnonymousDelegate_2(Of Object).Invoke(Object)"
  IL_0138:  newobj     "Sub EventLibrary.voidDynamicDelegate..ctor(Object, System.IntPtr)"
  IL_013d:  br.s       IL_0140
  IL_013f:  ldnull
  IL_0140:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidDynamicDelegate)(System.Func(Of EventLibrary.voidDynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDynamicDelegate)"
  IL_0145:  ldloc.0
  IL_0146:  dup
  IL_0147:  ldvirtftn  "Sub A.add_d4(EventLibrary.voidDelegateDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_014d:  newobj     "Sub System.Func(Of EventLibrary.voidDelegateDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0152:  ldloc.0
  IL_0153:  dup
  IL_0154:  ldvirtftn  "Sub A.remove_d4(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_015a:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_015f:  ldloc.s    V_5
  IL_0161:  stloc.s    V_9
  IL_0163:  ldloc.s    V_9
  IL_0165:  brfalse.s  IL_0176
  IL_0167:  ldloc.s    V_9
  IL_0169:  ldftn      "Sub VB$AnonymousDelegate_3(Of EventLibrary.voidVoidDelegate).Invoke(EventLibrary.voidVoidDelegate)"
  IL_016f:  newobj     "Sub EventLibrary.voidDelegateDelegate..ctor(Object, System.IntPtr)"
  IL_0174:  br.s       IL_0177
  IL_0176:  ldnull
  IL_0177:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidDelegateDelegate)(System.Func(Of EventLibrary.voidDelegateDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDelegateDelegate)"
  IL_017c:  ldloc.0
  IL_017d:  dup
  IL_017e:  ldvirtftn  "Sub A.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0184:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0189:  ldloc.2
  IL_018a:  stloc.s    V_6
  IL_018c:  ldloc.s    V_6
  IL_018e:  brfalse.s  IL_019f
  IL_0190:  ldloc.s    V_6
  IL_0192:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_0198:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_019d:  br.s       IL_01a0
  IL_019f:  ldnull
  IL_01a0:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidVoidDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_01a5:  ldloc.0
  IL_01a6:  dup
  IL_01a7:  ldvirtftn  "Sub A.remove_d2(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_01ad:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_01b2:  ldloc.3
  IL_01b3:  stloc.s    V_7
  IL_01b5:  ldloc.s    V_7
  IL_01b7:  brfalse.s  IL_01c8
  IL_01b9:  ldloc.s    V_7
  IL_01bb:  ldftn      "Sub VB$AnonymousDelegate_1(Of String).Invoke(String)"
  IL_01c1:  newobj     "Sub EventLibrary.voidStringDelegate..ctor(Object, System.IntPtr)"
  IL_01c6:  br.s       IL_01c9
  IL_01c8:  ldnull
  IL_01c9:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidStringDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidStringDelegate)"
  IL_01ce:  ldloc.0
  IL_01cf:  dup
  IL_01d0:  ldvirtftn  "Sub A.remove_d3(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_01d6:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_01db:  ldloc.s    V_4
  IL_01dd:  stloc.s    V_8
  IL_01df:  ldloc.s    V_8
  IL_01e1:  brfalse.s  IL_01f2
  IL_01e3:  ldloc.s    V_8
  IL_01e5:  ldftn      "Sub VB$AnonymousDelegate_2(Of Object).Invoke(Object)"
  IL_01eb:  newobj     "Sub EventLibrary.voidDynamicDelegate..ctor(Object, System.IntPtr)"
  IL_01f0:  br.s       IL_01f3
  IL_01f2:  ldnull
  IL_01f3:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidDynamicDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDynamicDelegate)"
  IL_01f8:  ldloc.0
  IL_01f9:  dup
  IL_01fa:  ldvirtftn  "Sub A.remove_d4(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0200:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0205:  ldloc.s    V_5
  IL_0207:  stloc.s    V_9
  IL_0209:  ldloc.s    V_9
  IL_020b:  brfalse.s  IL_021c
  IL_020d:  ldloc.s    V_9
  IL_020f:  ldftn      "Sub VB$AnonymousDelegate_3(Of EventLibrary.voidVoidDelegate).Invoke(EventLibrary.voidVoidDelegate)"
  IL_0215:  newobj     "Sub EventLibrary.voidDelegateDelegate..ctor(Object, System.IntPtr)"
  IL_021a:  br.s       IL_021d
  IL_021c:  ldnull
  IL_021d:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidDelegateDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDelegateDelegate)"
  IL_0222:  ldloc.1
  IL_0223:  dup
  IL_0224:  ldvirtftn  "Sub B.add_d1(EventLibrary.voidVoidDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_022a:  newobj     "Sub System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_022f:  ldloc.1
  IL_0230:  dup
  IL_0231:  ldvirtftn  "Sub B.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0237:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_023c:  ldloc.2
  IL_023d:  stloc.s    V_6
  IL_023f:  ldloc.s    V_6
  IL_0241:  brfalse.s  IL_0252
  IL_0243:  ldloc.s    V_6
  IL_0245:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_024b:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_0250:  br.s       IL_0253
  IL_0252:  ldnull
  IL_0253:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidVoidDelegate)(System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_0258:  ldloc.1
  IL_0259:  dup
  IL_025a:  ldvirtftn  "Sub B.add_d2(EventLibrary.voidStringDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0260:  newobj     "Sub System.Func(Of EventLibrary.voidStringDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0265:  ldloc.1
  IL_0266:  dup
  IL_0267:  ldvirtftn  "Sub B.remove_d2(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_026d:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0272:  ldloc.3
  IL_0273:  stloc.s    V_7
  IL_0275:  ldloc.s    V_7
  IL_0277:  brfalse.s  IL_0288
  IL_0279:  ldloc.s    V_7
  IL_027b:  ldftn      "Sub VB$AnonymousDelegate_1(Of String).Invoke(String)"
  IL_0281:  newobj     "Sub EventLibrary.voidStringDelegate..ctor(Object, System.IntPtr)"
  IL_0286:  br.s       IL_0289
  IL_0288:  ldnull
  IL_0289:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidStringDelegate)(System.Func(Of EventLibrary.voidStringDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidStringDelegate)"
  IL_028e:  ldloc.1
  IL_028f:  dup
  IL_0290:  ldvirtftn  "Sub B.add_d3(EventLibrary.voidDynamicDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_0296:  newobj     "Sub System.Func(Of EventLibrary.voidDynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_029b:  ldloc.1
  IL_029c:  dup
  IL_029d:  ldvirtftn  "Sub B.remove_d3(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_02a3:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_02a8:  ldloc.s    V_4
  IL_02aa:  stloc.s    V_8
  IL_02ac:  ldloc.s    V_8
  IL_02ae:  brfalse.s  IL_02bf
  IL_02b0:  ldloc.s    V_8
  IL_02b2:  ldftn      "Sub VB$AnonymousDelegate_2(Of Object).Invoke(Object)"
  IL_02b8:  newobj     "Sub EventLibrary.voidDynamicDelegate..ctor(Object, System.IntPtr)"
  IL_02bd:  br.s       IL_02c0
  IL_02bf:  ldnull
  IL_02c0:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidDynamicDelegate)(System.Func(Of EventLibrary.voidDynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDynamicDelegate)"
  IL_02c5:  ldloc.1
  IL_02c6:  dup
  IL_02c7:  ldvirtftn  "Sub B.add_d4(EventLibrary.voidDelegateDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_02cd:  newobj     "Sub System.Func(Of EventLibrary.voidDelegateDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_02d2:  ldloc.1
  IL_02d3:  dup
  IL_02d4:  ldvirtftn  "Sub B.remove_d4(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_02da:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_02df:  ldloc.s    V_5
  IL_02e1:  stloc.s    V_9
  IL_02e3:  ldloc.s    V_9
  IL_02e5:  brfalse.s  IL_02f6
  IL_02e7:  ldloc.s    V_9
  IL_02e9:  ldftn      "Sub VB$AnonymousDelegate_3(Of EventLibrary.voidVoidDelegate).Invoke(EventLibrary.voidVoidDelegate)"
  IL_02ef:  newobj     "Sub EventLibrary.voidDelegateDelegate..ctor(Object, System.IntPtr)"
  IL_02f4:  br.s       IL_02f7
  IL_02f6:  ldnull
  IL_02f7:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidDelegateDelegate)(System.Func(Of EventLibrary.voidDelegateDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDelegateDelegate)"
  IL_02fc:  ldloc.1
  IL_02fd:  dup
  IL_02fe:  ldvirtftn  "Sub B.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0304:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0309:  ldloc.2
  IL_030a:  stloc.s    V_6
  IL_030c:  ldloc.s    V_6
  IL_030e:  brfalse.s  IL_031f
  IL_0310:  ldloc.s    V_6
  IL_0312:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_0318:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_031d:  br.s       IL_0320
  IL_031f:  ldnull
  IL_0320:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidVoidDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_0325:  ldloc.1
  IL_0326:  dup
  IL_0327:  ldvirtftn  "Sub B.remove_d2(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_032d:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0332:  ldloc.3
  IL_0333:  stloc.s    V_7
  IL_0335:  ldloc.s    V_7
  IL_0337:  brfalse.s  IL_0348
  IL_0339:  ldloc.s    V_7
  IL_033b:  ldftn      "Sub VB$AnonymousDelegate_1(Of String).Invoke(String)"
  IL_0341:  newobj     "Sub EventLibrary.voidStringDelegate..ctor(Object, System.IntPtr)"
  IL_0346:  br.s       IL_0349
  IL_0348:  ldnull
  IL_0349:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidStringDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidStringDelegate)"
  IL_034e:  ldloc.1
  IL_034f:  dup
  IL_0350:  ldvirtftn  "Sub B.remove_d3(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0356:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_035b:  ldloc.s    V_4
  IL_035d:  stloc.s    V_8
  IL_035f:  ldloc.s    V_8
  IL_0361:  brfalse.s  IL_0372
  IL_0363:  ldloc.s    V_8
  IL_0365:  ldftn      "Sub VB$AnonymousDelegate_2(Of Object).Invoke(Object)"
  IL_036b:  newobj     "Sub EventLibrary.voidDynamicDelegate..ctor(Object, System.IntPtr)"
  IL_0370:  br.s       IL_0373
  IL_0372:  ldnull
  IL_0373:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidDynamicDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDynamicDelegate)"
  IL_0378:  ldloc.1
  IL_0379:  dup
  IL_037a:  ldvirtftn  "Sub B.remove_d4(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0380:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0385:  ldloc.s    V_5
  IL_0387:  stloc.s    V_9
  IL_0389:  ldloc.s    V_9
  IL_038b:  brfalse.s  IL_039c
  IL_038d:  ldloc.s    V_9
  IL_038f:  ldftn      "Sub VB$AnonymousDelegate_3(Of EventLibrary.voidVoidDelegate).Invoke(EventLibrary.voidVoidDelegate)"
  IL_0395:  newobj     "Sub EventLibrary.voidDelegateDelegate..ctor(Object, System.IntPtr)"
  IL_039a:  br.s       IL_039d
  IL_039c:  ldnull
  IL_039d:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidDelegateDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidDelegateDelegate)"
  IL_03a2:  ret
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
                        <%= _dynamicCommonSrc %>
                    </file>
                </compilation>

            Dim verifier = CompileAndVerifyOnWin8Only(
                src,
                allReferences:={
                    MscorlibRef_v4_0_30316_17626,
                    SystemCoreRef_v4_0_30319_17929,
                    _eventLibRef})
            verifier.VerifyDiagnostics()
            verifier.VerifyIL("A.Scenario1", <![CDATA[
{
  // Code size      136 (0x88)
  .maxstack  4
  .locals init (VB$AnonymousDelegate_0 V_0, //testDelegate
                VB$AnonymousDelegate_0 V_1)
  IL_0000:  ldsfld     "A._Closure$__.$I1-0 As <generated method>"
  IL_0005:  brfalse.s  IL_000e
  IL_0007:  ldsfld     "A._Closure$__.$I1-0 As <generated method>"
  IL_000c:  br.s       IL_0024
  IL_000e:  ldsfld     "A._Closure$__.$I As A._Closure$__"
  IL_0013:  ldftn      "Sub A._Closure$__._Lambda$__1-0()"
  IL_0019:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_001e:  dup
  IL_001f:  stsfld     "A._Closure$__.$I1-0 As <generated method>"
  IL_0024:  stloc.0
  IL_0025:  ldarg.0
  IL_0026:  dup
  IL_0027:  ldvirtftn  "Sub A.add_d1(EventLibrary.voidVoidDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_002d:  newobj     "Sub System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0032:  ldarg.0
  IL_0033:  dup
  IL_0034:  ldvirtftn  "Sub A.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_003a:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_003f:  ldloc.0
  IL_0040:  stloc.1
  IL_0041:  ldloc.1
  IL_0042:  brfalse.s  IL_0052
  IL_0044:  ldloc.1
  IL_0045:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_004b:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_0050:  br.s       IL_0053
  IL_0052:  ldnull
  IL_0053:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidVoidDelegate)(System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_0058:  ldarg.0
  IL_0059:  dup
  IL_005a:  ldvirtftn  "Sub A.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0060:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0065:  ldloc.0
  IL_0066:  stloc.1
  IL_0067:  ldloc.1
  IL_0068:  brfalse.s  IL_0078
  IL_006a:  ldloc.1
  IL_006b:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_0071:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_0076:  br.s       IL_0079
  IL_0078:  ldnull
  IL_0079:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidVoidDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_007e:  ldarg.0
  IL_007f:  ldfld      "A.d1Event As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of EventLibrary.voidVoidDelegate)"
  IL_0084:  ldnull
  IL_0085:  ceq
  IL_0087:  ret
}
]]>.Value)
            verifier.VerifyIL("A.Scenario2", <![CDATA[
{
  // Code size      138 (0x8a)
  .maxstack  4
  .locals init (A V_0, //b
                VB$AnonymousDelegate_0 V_1, //testDelegate
                VB$AnonymousDelegate_0 V_2)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldsfld     "A._Closure$__.$I2-0 As <generated method>"
  IL_0007:  brfalse.s  IL_0010
  IL_0009:  ldsfld     "A._Closure$__.$I2-0 As <generated method>"
  IL_000e:  br.s       IL_0026
  IL_0010:  ldsfld     "A._Closure$__.$I As A._Closure$__"
  IL_0015:  ldftn      "Sub A._Closure$__._Lambda$__2-0()"
  IL_001b:  newobj     "Sub VB$AnonymousDelegate_0..ctor(Object, System.IntPtr)"
  IL_0020:  dup
  IL_0021:  stsfld     "A._Closure$__.$I2-0 As <generated method>"
  IL_0026:  stloc.1
  IL_0027:  ldloc.0
  IL_0028:  dup
  IL_0029:  ldvirtftn  "Sub A.add_d1(EventLibrary.voidVoidDelegate) As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"
  IL_002f:  newobj     "Sub System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0034:  ldloc.0
  IL_0035:  dup
  IL_0036:  ldvirtftn  "Sub A.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_003c:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0041:  ldloc.1
  IL_0042:  stloc.2
  IL_0043:  ldloc.2
  IL_0044:  brfalse.s  IL_0054
  IL_0046:  ldloc.2
  IL_0047:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_004d:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_0052:  br.s       IL_0055
  IL_0054:  ldnull
  IL_0055:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler(Of EventLibrary.voidVoidDelegate)(System.Func(Of EventLibrary.voidVoidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_005a:  ldloc.0
  IL_005b:  dup
  IL_005c:  ldvirtftn  "Sub A.remove_d1(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)"
  IL_0062:  newobj     "Sub System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)..ctor(Object, System.IntPtr)"
  IL_0067:  ldloc.1
  IL_0068:  stloc.2
  IL_0069:  ldloc.2
  IL_006a:  brfalse.s  IL_007a
  IL_006c:  ldloc.2
  IL_006d:  ldftn      "Sub VB$AnonymousDelegate_0.Invoke()"
  IL_0073:  newobj     "Sub EventLibrary.voidVoidDelegate..ctor(Object, System.IntPtr)"
  IL_0078:  br.s       IL_007b
  IL_007a:  ldnull
  IL_007b:  call       "Sub System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler(Of EventLibrary.voidVoidDelegate)(System.Action(Of System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken), EventLibrary.voidVoidDelegate)"
  IL_0080:  ldloc.0
  IL_0081:  ldfld      "A.d1Event As System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable(Of EventLibrary.voidVoidDelegate)"
  IL_0086:  ldnull
  IL_0087:  ceq
  IL_0089:  ret
}
]]>.Value)
        End Sub

        ''' <summary>
        ''' Verify that WinRT events compile into the IL that we 
        ''' would expect.
        ''' </summary>
        <ConditionalFact(GetType(OSVersionWin8))>
        <WorkItem(18092, "https://github.com/dotnet/roslyn/issues/18092")>
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

	    Public Sub goo()
		    Dim application As Application = Nothing
            AddHandler application.Suspending, AddressOf Me.OnSuspending
            RemoveHandler application.Suspending, AddressOf Me.OnSuspending
	    End Sub

	    Public Shared Sub Main()
		    Dim abcdef As abcdef = New abcdef()
		    abcdef.goo()
	    End Sub
    End Class
        </file>
</compilation>
            Dim compilation = CreateCompilationWithWinRt(source)

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
            CompileAndVerify(compilation).VerifyIL("abcdef.goo", expectedIL.Value())
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

            Dim comp = CreateEmptyCompilationWithReferences(
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

	    Public Sub goo()
		    Dim application As Application = Nothing
            AddHandler application.Suspending, Sub(sender as Object, e As SuspendingEventArgs)
                                            End Sub
	    End Sub

	    Public Shared Sub Main()
		    Dim abcdef As abcdef = New abcdef()
		    abcdef.goo()
	    End Sub
    End Class
        </file>
</compilation>
            Dim compilation = CreateCompilationWithWinRt(source)

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

            CompileAndVerify(compilation, verify:=If(OSVersion.IsWin8, Verification.Passes, Verification.Skipped)).VerifyIL("abcdef.goo", expectedIL.Value())
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
            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs.Concat({ilRef}))
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

            Dim substitutedNormalEvent = implementingNormalEvent.ExplicitInterfaceImplementations.Single()
            Dim substitutedWinRTEvent = implementingWinRTEvent.ExplicitInterfaceImplementations.Single()

            Assert.IsType(Of SubstitutedEventSymbol)(substitutedNormalEvent)
            Assert.IsType(Of SubstitutedEventSymbol)(substitutedWinRTEvent)

            ' Based on original definition.
            Assert.False(substitutedNormalEvent.IsWindowsRuntimeEvent)
            Assert.True(substitutedWinRTEvent.IsWindowsRuntimeEvent)

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
                Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, New VisualBasicCompilationOptions(kind))
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

            Dim ilRef = CompileIL(String.Format(_eventInterfaceILTemplate, "I"))

            For Each kind As OutputKind In [Enum].GetValues(GetType(OutputKind))
                Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs.Concat({ilRef}), New VisualBasicCompilationOptions(kind))
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

            Dim ilRef = CompileIL(String.Format(_eventInterfaceILTemplate, "I1") + String.Format(_eventInterfaceILTemplate, "I2"))

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs.Concat({ilRef}), TestOptions.ReleaseDll)
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

            Dim ilRef = CompileIL(String.Format(_eventInterfaceILTemplate, "I1") + String.Format(_eventInterfaceILTemplate, "I2"))

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs.Concat({ilRef}), TestOptions.ReleaseDll)

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

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)
            comp.VerifyDiagnostics(
            Diagnostic(ERRID.ERR_WinRTEventWithoutDelegate, "E"))

            Dim type = comp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Test")
            Assert.True(type.GetMember(Of EventSymbol)("E").IsWindowsRuntimeEvent)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestExecutionNeedsWindowsTypes)>
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

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)
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

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)

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

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)

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

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)

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

            Dim comp = CreateEmptyCompilationWithReferences(source, {MscorlibRef}, TestOptions.ReleaseWinMD)

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

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)

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

            Dim comp = CreateEmptyCompilationWithReferences(source, {MscorlibRef}, TestOptions.ReleaseWinMD)

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

            Dim comp = CreateEmptyCompilationWithReferences(source, {MscorlibRef}, TestOptions.ReleaseWinMD)
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

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)
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
                Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, New VisualBasicCompilationOptions(kind))

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

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, options:=TestOptions.ReleaseWinMD)
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

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)
            ' Note the distinct new error code.
            comp.VerifyDiagnostics(
            Diagnostic(ERRID.WRN_DefAsgNoRetValWinRtEventVal1, "End AddHandler").WithArguments("E"))
        End Sub

        Private Shared Sub VerifyWinRTEventShape([event] As EventSymbol, compilation As VisualBasicCompilation)
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

        Private Shared Sub VerifyNormalEventShape([event] As EventSymbol, compilation As VisualBasicCompilation)
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

            Dim comp = CreateEmptyCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD)
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
            CreateEmptyCompilationWithReferences(source, WinRtRefs, TestOptions.ReleaseWinMD).VerifyDiagnostics()
        End Sub

    End Class
End Namespace
