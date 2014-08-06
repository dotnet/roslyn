// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;

using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    // Unit tests for programs that use the Windows.winmd file.
    // 
    // Checks to see that types are forwarded correctly, that 
    // metadata files are loaded as they should, etc.
    public class WinMdEventTests : CSharpTestBase
    {
        private const string EventInterfaceIL = @"
.class interface public abstract auto ansi Interface
{
  .method public hidebysig newslot specialname abstract virtual 
          instance void  add_Normal(class [mscorlib]System.Action 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance void  remove_Normal(class [mscorlib]System.Action 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 
          add_WinRT([in] class [mscorlib]System.Action 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance void  remove_WinRT([in] valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 'value') cil managed
  {
  }

  .event class [mscorlib]System.Action Normal
  {
    .addon instance void Interface::add_Normal(class [mscorlib]System.Action)
    .removeon instance void Interface::remove_Normal(class [mscorlib]System.Action)
  }

  .event class [mscorlib]System.Action WinRT
  {
    .addon instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken Interface::add_WinRT(class [mscorlib]System.Action)
    .removeon instance void Interface::remove_WinRT(valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
  }
} // end of class Interface
";

        private const string EventBaseIL = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual 
          instance void  add_Normal(class [mscorlib]System.Action 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig newslot specialname virtual 
          instance void  remove_Normal(class [mscorlib]System.Action 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig newslot specialname virtual 
          instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 
          add_WinRT([in] class [mscorlib]System.Action 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig newslot specialname virtual  
          instance void  remove_WinRT([in] valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .event [mscorlib]System.Action Normal
  {
    .removeon instance void Base::remove_Normal(class [mscorlib]System.Action)
    .addon instance void Base::add_Normal(class [mscorlib]System.Action)
  }

  .event class [mscorlib]System.Action WinRT
  {
    .addon instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken Base::add_WinRT(class [mscorlib]System.Action)
    .removeon instance void Base::remove_WinRT(valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
  }

} // end of class Base

// Same as Base, but Normal is a WinRT event and WinRT is a normal event.
.class public auto ansi beforefieldinit ReversedBase
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual 
          instance void  add_WinRT(class [mscorlib]System.Action 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig newslot specialname virtual 
          instance void  remove_WinRT(class [mscorlib]System.Action 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig newslot specialname virtual 
          instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 
          add_Normal([in] class [mscorlib]System.Action 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig newslot specialname virtual  
          instance void  remove_Normal([in] valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .event [mscorlib]System.Action WinRT
  {
    .removeon instance void ReversedBase::remove_WinRT(class [mscorlib]System.Action)
    .addon instance void ReversedBase::add_WinRT(class [mscorlib]System.Action)
  }

  .event class [mscorlib]System.Action Normal
  {
    .addon instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken ReversedBase::add_Normal(class [mscorlib]System.Action)
    .removeon instance void ReversedBase::remove_Normal(valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
  }

} // end of class ReversedBase
";

        private readonly MetadataReference EventLibRef;

        private const string DynamicCommonSrc =
@"using System.Runtime.InteropServices.WindowsRuntime;
using EventLibrary;

public partial class A : I
{
    public event voidDelegate d1;
    public event genericDelegate<object> d2;
    public event dynamicDelegate d3;
}

public partial class B : I
{
    EventRegistrationTokenTable<voidDelegate> voidDelegateTable;
    EventRegistrationTokenTable<genericDelegate<object>> genericDelegateTable;
    EventRegistrationTokenTable<dynamicDelegate> dynamicDelegateTable;

    public B()
    {
        voidDelegateTable = new EventRegistrationTokenTable<voidDelegate>();
        genericDelegateTable = new EventRegistrationTokenTable<genericDelegate<object>>();
        dynamicDelegateTable = new EventRegistrationTokenTable<dynamicDelegate>();
    }

    public event voidDelegate d1
    {
        add { return voidDelegateTable.AddEventHandler(value); }
        remove { voidDelegateTable.RemoveEventHandler(value); }
    }

    public event genericDelegate<object> d2
    {
        add { return genericDelegateTable.AddEventHandler(value); }
        remove { genericDelegateTable.RemoveEventHandler(value); }
    }

    public event dynamicDelegate d3
    {
        add { return dynamicDelegateTable.AddEventHandler(value); }
        remove { dynamicDelegateTable.RemoveEventHandler(value); }
    }
}";

        public WinMdEventTests()
        {
            // The following two libraries are shrinked code pulled from
            // corresponding files in the csharp5 legacy tests
            const string eventLibSrc =
@"namespace EventLibrary
{
    public delegate void voidDelegate();
    public delegate T genericDelegate<T>(T t);
    public delegate dynamic dynamicDelegate(dynamic D);

    public interface I
    {
        event voidDelegate d1;
        event genericDelegate<object> d2;
        event dynamicDelegate d3;
    }
}";
            EventLibRef = CreateCompilation(
                eventLibSrc,
                references: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef_v4_0_30319_17929 },
                options:
                    new CSharpCompilationOptions(
                        OutputKind.WindowsRuntimeMetadata,
                        allowUnsafe: true),
                assemblyName: "EventLibrary").EmitToImageReference();
        }

        [Fact]
        public void WinMdExternalEventTests()
        {
            var src = 
@"
using EventLibrary;

class C
{
    static void Main()
    {
        var a = new A();
        var b = new B();

        var test = new voidDelegate(() => { ; });
        var generic = new genericDelegate<object>((o) => { return o; });
        var dyn = new dynamicDelegate((d) => { return d.ToString(); });

        a.d1 += test;
        a.d2 += generic;
        a.d3 += dyn;

        a.d1 -= test;
        a.d2 -= generic;
        a.d3 -= dyn;

        b.d1 += test;
        b.d2 += generic;
        b.d3 += dyn;

        b.d1 -= test;
        b.d2 -= generic;
        b.d3 -= dyn;

        dynamic c = a;
        
        c.d1 += test;
        c.d2 += generic;
        c.d3 += dyn;

        c.d1 -= test;
        c.d2 -= generic;
        c.d3 -= dyn;

        // Wrong event type
        c.d1 += generic;

        c = b;

        c.d1 += test;
        c.d2 += generic;
        c.d3 += dyn;

        c.d1 -= test;
        c.d2 -= generic;
        c.d3 -= dyn;

        // Wrong event type
        c.d1 += generic;
    }
}";

            var dynamicCommonRef = CreateCompilation(
                DynamicCommonSrc,
                references: new[] { 
                    MscorlibRef_v4_0_30316_17626,
                    EventLibRef,
                },
                options:
                    new CSharpCompilationOptions(
                        OutputKind.NetModule,
                        allowUnsafe: true)).EmitToImageReference(
// (6,31): warning CS0067: The event 'A.d1' is never used
//     public event voidDelegate d1;
Diagnostic(ErrorCode.WRN_UnreferencedEvent, "d1").WithArguments("A.d1"),
// (8,34): warning CS0067: The event 'A.d3' is never used
//     public event dynamicDelegate d3;
Diagnostic(ErrorCode.WRN_UnreferencedEvent, "d3").WithArguments("A.d3"),
// (7,42): warning CS0067: The event 'A.d2' is never used
//     public event genericDelegate<object> d2;
Diagnostic(ErrorCode.WRN_UnreferencedEvent, "d2").WithArguments("A.d2"));

            var verifer = CompileAndVerifyOnWin8Only(
                src,
                additionalRefs: new[] { 
                    MscorlibRef_v4_0_30316_17626,
                    SystemCoreRef_v4_0_30319_17929,
                    CSharpRef,
                    EventLibRef, 
                    dynamicCommonRef
                },
                emitOptions: EmitOptions.RefEmitBug);
            verifer.VerifyIL("C.Main",
@"
{
  // Code size     6203 (0x183b)
  .maxstack  13
  .locals init (A V_0, //a
  B V_1, //b
  EventLibrary.voidDelegate V_2, //test
  EventLibrary.genericDelegate<object> V_3, //generic
  EventLibrary.dynamicDelegate V_4, //dyn
  object V_5, //c
  object V_6)
  IL_0000:  newobj     ""A..ctor()""
  IL_0005:  stloc.0
  IL_0006:  newobj     ""B..ctor()""
  IL_000b:  stloc.1
  IL_000c:  ldsfld     ""EventLibrary.voidDelegate C.CS$<>9__CachedAnonymousMethodDelegate73""
  IL_0011:  dup
  IL_0012:  brtrue.s   IL_0027
  IL_0014:  pop
  IL_0015:  ldnull
  IL_0016:  ldftn      ""void C.<Main>b__72(object)""
  IL_001c:  newobj     ""EventLibrary.voidDelegate..ctor(object, System.IntPtr)""
  IL_0021:  dup
  IL_0022:  stsfld     ""EventLibrary.voidDelegate C.CS$<>9__CachedAnonymousMethodDelegate73""
  IL_0027:  stloc.2
  IL_0028:  ldsfld     ""EventLibrary.genericDelegate<object> C.CS$<>9__CachedAnonymousMethodDelegate75""
  IL_002d:  dup
  IL_002e:  brtrue.s   IL_0043
  IL_0030:  pop
  IL_0031:  ldnull
  IL_0032:  ldftn      ""object C.<Main>b__74(object, object)""
  IL_0038:  newobj     ""EventLibrary.genericDelegate<object>..ctor(object, System.IntPtr)""
  IL_003d:  dup
  IL_003e:  stsfld     ""EventLibrary.genericDelegate<object> C.CS$<>9__CachedAnonymousMethodDelegate75""
  IL_0043:  stloc.3
  IL_0044:  ldsfld     ""EventLibrary.dynamicDelegate C.CS$<>9__CachedAnonymousMethodDelegate77""
  IL_0049:  dup
  IL_004a:  brtrue.s   IL_005f
  IL_004c:  pop
  IL_004d:  ldnull
  IL_004e:  ldftn      ""dynamic C.<Main>b__76(object, dynamic)""
  IL_0054:  newobj     ""EventLibrary.dynamicDelegate..ctor(object, System.IntPtr)""
  IL_0059:  dup
  IL_005a:  stsfld     ""EventLibrary.dynamicDelegate C.CS$<>9__CachedAnonymousMethodDelegate77""
  IL_005f:  stloc.s    V_4
  IL_0061:  ldloc.0
  IL_0062:  dup
  IL_0063:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d1.add""
  IL_0069:  newobj     ""System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_006e:  ldloc.0
  IL_006f:  dup
  IL_0070:  ldvirtftn  ""void A.d1.remove""
  IL_0076:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_007b:  ldloc.2
  IL_007c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.voidDelegate>(System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0081:  ldloc.0
  IL_0082:  dup
  IL_0083:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d2.add""
  IL_0089:  newobj     ""System.Func<EventLibrary.genericDelegate<object>, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_008e:  ldloc.0
  IL_008f:  dup
  IL_0090:  ldvirtftn  ""void A.d2.remove""
  IL_0096:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_009b:  ldloc.3
  IL_009c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.genericDelegate<object>>(System.Func<EventLibrary.genericDelegate<object>, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.genericDelegate<object>)""
  IL_00a1:  ldloc.0
  IL_00a2:  dup
  IL_00a3:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d3.add""
  IL_00a9:  newobj     ""System.Func<EventLibrary.dynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00ae:  ldloc.0
  IL_00af:  dup
  IL_00b0:  ldvirtftn  ""void A.d3.remove""
  IL_00b6:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00bb:  ldloc.s    V_4
  IL_00bd:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.dynamicDelegate>(System.Func<EventLibrary.dynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.dynamicDelegate)""
  IL_00c2:  ldloc.0
  IL_00c3:  dup
  IL_00c4:  ldvirtftn  ""void A.d1.remove""
  IL_00ca:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00cf:  ldloc.2
  IL_00d0:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.voidDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_00d5:  ldloc.0
  IL_00d6:  dup
  IL_00d7:  ldvirtftn  ""void A.d2.remove""
  IL_00dd:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00e2:  ldloc.3
  IL_00e3:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.genericDelegate<object>>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.genericDelegate<object>)""
  IL_00e8:  ldloc.0
  IL_00e9:  dup
  IL_00ea:  ldvirtftn  ""void A.d3.remove""
  IL_00f0:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00f5:  ldloc.s    V_4
  IL_00f7:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.dynamicDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.dynamicDelegate)""
  IL_00fc:  ldloc.1
  IL_00fd:  dup
  IL_00fe:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken B.d1.add""
  IL_0104:  newobj     ""System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0109:  ldloc.1
  IL_010a:  dup
  IL_010b:  ldvirtftn  ""void B.d1.remove""
  IL_0111:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0116:  ldloc.2
  IL_0117:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.voidDelegate>(System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_011c:  ldloc.1
  IL_011d:  dup
  IL_011e:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken B.d2.add""
  IL_0124:  newobj     ""System.Func<EventLibrary.genericDelegate<object>, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0129:  ldloc.1
  IL_012a:  dup
  IL_012b:  ldvirtftn  ""void B.d2.remove""
  IL_0131:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0136:  ldloc.3
  IL_0137:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.genericDelegate<object>>(System.Func<EventLibrary.genericDelegate<object>, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.genericDelegate<object>)""
  IL_013c:  ldloc.1
  IL_013d:  dup
  IL_013e:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken B.d3.add""
  IL_0144:  newobj     ""System.Func<EventLibrary.dynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0149:  ldloc.1
  IL_014a:  dup
  IL_014b:  ldvirtftn  ""void B.d3.remove""
  IL_0151:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0156:  ldloc.s    V_4
  IL_0158:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.dynamicDelegate>(System.Func<EventLibrary.dynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.dynamicDelegate)""
  IL_015d:  ldloc.1
  IL_015e:  dup
  IL_015f:  ldvirtftn  ""void B.d1.remove""
  IL_0165:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_016a:  ldloc.2
  IL_016b:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.voidDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0170:  ldloc.1
  IL_0171:  dup
  IL_0172:  ldvirtftn  ""void B.d2.remove""
  IL_0178:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_017d:  ldloc.3
  IL_017e:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.genericDelegate<object>>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.genericDelegate<object>)""
  IL_0183:  ldloc.1
  IL_0184:  dup
  IL_0185:  ldvirtftn  ""void B.d3.remove""
  IL_018b:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0190:  ldloc.s    V_4
  IL_0192:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.dynamicDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.dynamicDelegate)""
  IL_0197:  ldloc.0
  IL_0198:  stloc.s    V_5
  IL_019a:  ldloc.s    V_5
  IL_019c:  stloc.s    V_6
  IL_019e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site5""
  IL_01a3:  brtrue.s   IL_01c4
  IL_01a5:  ldc.i4.0
  IL_01a6:  ldstr      ""d1""
  IL_01ab:  ldtoken    ""C""
  IL_01b0:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01b5:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_01ba:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_01bf:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site5""
  IL_01c4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site5""
  IL_01c9:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_01ce:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site5""
  IL_01d3:  ldloc.s    V_6
  IL_01d5:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_01da:  brtrue     IL_02da
  IL_01df:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site4""
  IL_01e4:  brtrue.s   IL_0223
  IL_01e6:  ldc.i4     0x80
  IL_01eb:  ldstr      ""d1""
  IL_01f0:  ldtoken    ""C""
  IL_01f5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01fa:  ldc.i4.2
  IL_01fb:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0200:  dup
  IL_0201:  ldc.i4.0
  IL_0202:  ldc.i4.0
  IL_0203:  ldnull
  IL_0204:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0209:  stelem.ref
  IL_020a:  dup
  IL_020b:  ldc.i4.1
  IL_020c:  ldc.i4.0
  IL_020d:  ldnull
  IL_020e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0213:  stelem.ref
  IL_0214:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0219:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_021e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site4""
  IL_0223:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site4""
  IL_0228:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_022d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site4""
  IL_0232:  ldloc.s    V_6
  IL_0234:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site3""
  IL_0239:  brtrue.s   IL_0271
  IL_023b:  ldc.i4.0
  IL_023c:  ldc.i4.s   63
  IL_023e:  ldtoken    ""C""
  IL_0243:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0248:  ldc.i4.2
  IL_0249:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_024e:  dup
  IL_024f:  ldc.i4.0
  IL_0250:  ldc.i4.0
  IL_0251:  ldnull
  IL_0252:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0257:  stelem.ref
  IL_0258:  dup
  IL_0259:  ldc.i4.1
  IL_025a:  ldc.i4.1
  IL_025b:  ldnull
  IL_025c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0261:  stelem.ref
  IL_0262:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0267:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_026c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site3""
  IL_0271:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site3""
  IL_0276:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_027b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site3""
  IL_0280:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site2""
  IL_0285:  brtrue.s   IL_02b6
  IL_0287:  ldc.i4.0
  IL_0288:  ldstr      ""d1""
  IL_028d:  ldtoken    ""C""
  IL_0292:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0297:  ldc.i4.1
  IL_0298:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_029d:  dup
  IL_029e:  ldc.i4.0
  IL_029f:  ldc.i4.0
  IL_02a0:  ldnull
  IL_02a1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_02a6:  stelem.ref
  IL_02a7:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_02ac:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_02b1:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site2""
  IL_02b6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site2""
  IL_02bb:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_02c0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site2""
  IL_02c5:  ldloc.s    V_6
  IL_02c7:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_02cc:  ldloc.2
  IL_02cd:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_02d2:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_02d7:  pop
  IL_02d8:  br.s       IL_0337
  IL_02da:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site6""
  IL_02df:  brtrue.s   IL_031f
  IL_02e1:  ldc.i4     0x104
  IL_02e6:  ldstr      ""add_d1""
  IL_02eb:  ldnull
  IL_02ec:  ldtoken    ""C""
  IL_02f1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_02f6:  ldc.i4.2
  IL_02f7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_02fc:  dup
  IL_02fd:  ldc.i4.0
  IL_02fe:  ldc.i4.0
  IL_02ff:  ldnull
  IL_0300:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0305:  stelem.ref
  IL_0306:  dup
  IL_0307:  ldc.i4.1
  IL_0308:  ldc.i4.1
  IL_0309:  ldnull
  IL_030a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_030f:  stelem.ref
  IL_0310:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0315:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_031a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site6""
  IL_031f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site6""
  IL_0324:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0329:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site6""
  IL_032e:  ldloc.s    V_6
  IL_0330:  ldloc.2
  IL_0331:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0336:  pop
  IL_0337:  ldloc.s    V_5
  IL_0339:  stloc.s    V_6
  IL_033b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site10""
  IL_0340:  brtrue.s   IL_0361
  IL_0342:  ldc.i4.0
  IL_0343:  ldstr      ""d2""
  IL_0348:  ldtoken    ""C""
  IL_034d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0352:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0357:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_035c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site10""
  IL_0361:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site10""
  IL_0366:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_036b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site10""
  IL_0370:  ldloc.s    V_6
  IL_0372:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0377:  brtrue     IL_0477
  IL_037c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site9""
  IL_0381:  brtrue.s   IL_03c0
  IL_0383:  ldc.i4     0x80
  IL_0388:  ldstr      ""d2""
  IL_038d:  ldtoken    ""C""
  IL_0392:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0397:  ldc.i4.2
  IL_0398:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_039d:  dup
  IL_039e:  ldc.i4.0
  IL_039f:  ldc.i4.0
  IL_03a0:  ldnull
  IL_03a1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_03a6:  stelem.ref
  IL_03a7:  dup
  IL_03a8:  ldc.i4.1
  IL_03a9:  ldc.i4.0
  IL_03aa:  ldnull
  IL_03ab:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_03b0:  stelem.ref
  IL_03b1:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_03b6:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_03bb:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site9""
  IL_03c0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site9""
  IL_03c5:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_03ca:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site9""
  IL_03cf:  ldloc.s    V_6
  IL_03d1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site8""
  IL_03d6:  brtrue.s   IL_040e
  IL_03d8:  ldc.i4.0
  IL_03d9:  ldc.i4.s   63
  IL_03db:  ldtoken    ""C""
  IL_03e0:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_03e5:  ldc.i4.2
  IL_03e6:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_03eb:  dup
  IL_03ec:  ldc.i4.0
  IL_03ed:  ldc.i4.0
  IL_03ee:  ldnull
  IL_03ef:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_03f4:  stelem.ref
  IL_03f5:  dup
  IL_03f6:  ldc.i4.1
  IL_03f7:  ldc.i4.1
  IL_03f8:  ldnull
  IL_03f9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_03fe:  stelem.ref
  IL_03ff:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0404:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0409:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site8""
  IL_040e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site8""
  IL_0413:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0418:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site8""
  IL_041d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site7""
  IL_0422:  brtrue.s   IL_0453
  IL_0424:  ldc.i4.0
  IL_0425:  ldstr      ""d2""
  IL_042a:  ldtoken    ""C""
  IL_042f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0434:  ldc.i4.1
  IL_0435:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_043a:  dup
  IL_043b:  ldc.i4.0
  IL_043c:  ldc.i4.0
  IL_043d:  ldnull
  IL_043e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0443:  stelem.ref
  IL_0444:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0449:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_044e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site7""
  IL_0453:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site7""
  IL_0458:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_045d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site7""
  IL_0462:  ldloc.s    V_6
  IL_0464:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0469:  ldloc.3
  IL_046a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_046f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0474:  pop
  IL_0475:  br.s       IL_04d4
  IL_0477:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site11""
  IL_047c:  brtrue.s   IL_04bc
  IL_047e:  ldc.i4     0x104
  IL_0483:  ldstr      ""add_d2""
  IL_0488:  ldnull
  IL_0489:  ldtoken    ""C""
  IL_048e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0493:  ldc.i4.2
  IL_0494:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0499:  dup
  IL_049a:  ldc.i4.0
  IL_049b:  ldc.i4.0
  IL_049c:  ldnull
  IL_049d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_04a2:  stelem.ref
  IL_04a3:  dup
  IL_04a4:  ldc.i4.1
  IL_04a5:  ldc.i4.1
  IL_04a6:  ldnull
  IL_04a7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_04ac:  stelem.ref
  IL_04ad:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_04b2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_04b7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site11""
  IL_04bc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site11""
  IL_04c1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_04c6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site11""
  IL_04cb:  ldloc.s    V_6
  IL_04cd:  ldloc.3
  IL_04ce:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_04d3:  pop
  IL_04d4:  ldloc.s    V_5
  IL_04d6:  stloc.s    V_6
  IL_04d8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site15""
  IL_04dd:  brtrue.s   IL_04fe
  IL_04df:  ldc.i4.0
  IL_04e0:  ldstr      ""d3""
  IL_04e5:  ldtoken    ""C""
  IL_04ea:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_04ef:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_04f4:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_04f9:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site15""
  IL_04fe:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site15""
  IL_0503:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0508:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site15""
  IL_050d:  ldloc.s    V_6
  IL_050f:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0514:  brtrue     IL_0615
  IL_0519:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site14""
  IL_051e:  brtrue.s   IL_055d
  IL_0520:  ldc.i4     0x80
  IL_0525:  ldstr      ""d3""
  IL_052a:  ldtoken    ""C""
  IL_052f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0534:  ldc.i4.2
  IL_0535:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_053a:  dup
  IL_053b:  ldc.i4.0
  IL_053c:  ldc.i4.0
  IL_053d:  ldnull
  IL_053e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0543:  stelem.ref
  IL_0544:  dup
  IL_0545:  ldc.i4.1
  IL_0546:  ldc.i4.0
  IL_0547:  ldnull
  IL_0548:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_054d:  stelem.ref
  IL_054e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0553:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0558:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site14""
  IL_055d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site14""
  IL_0562:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0567:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site14""
  IL_056c:  ldloc.s    V_6
  IL_056e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site13""
  IL_0573:  brtrue.s   IL_05ab
  IL_0575:  ldc.i4.0
  IL_0576:  ldc.i4.s   63
  IL_0578:  ldtoken    ""C""
  IL_057d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0582:  ldc.i4.2
  IL_0583:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0588:  dup
  IL_0589:  ldc.i4.0
  IL_058a:  ldc.i4.0
  IL_058b:  ldnull
  IL_058c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0591:  stelem.ref
  IL_0592:  dup
  IL_0593:  ldc.i4.1
  IL_0594:  ldc.i4.1
  IL_0595:  ldnull
  IL_0596:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_059b:  stelem.ref
  IL_059c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_05a1:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_05a6:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site13""
  IL_05ab:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site13""
  IL_05b0:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_05b5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site13""
  IL_05ba:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site12""
  IL_05bf:  brtrue.s   IL_05f0
  IL_05c1:  ldc.i4.0
  IL_05c2:  ldstr      ""d3""
  IL_05c7:  ldtoken    ""C""
  IL_05cc:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_05d1:  ldc.i4.1
  IL_05d2:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_05d7:  dup
  IL_05d8:  ldc.i4.0
  IL_05d9:  ldc.i4.0
  IL_05da:  ldnull
  IL_05db:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_05e0:  stelem.ref
  IL_05e1:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_05e6:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_05eb:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site12""
  IL_05f0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site12""
  IL_05f5:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_05fa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site12""
  IL_05ff:  ldloc.s    V_6
  IL_0601:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0606:  ldloc.s    V_4
  IL_0608:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_060d:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0612:  pop
  IL_0613:  br.s       IL_0673
  IL_0615:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site16""
  IL_061a:  brtrue.s   IL_065a
  IL_061c:  ldc.i4     0x104
  IL_0621:  ldstr      ""add_d3""
  IL_0626:  ldnull
  IL_0627:  ldtoken    ""C""
  IL_062c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0631:  ldc.i4.2
  IL_0632:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0637:  dup
  IL_0638:  ldc.i4.0
  IL_0639:  ldc.i4.0
  IL_063a:  ldnull
  IL_063b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0640:  stelem.ref
  IL_0641:  dup
  IL_0642:  ldc.i4.1
  IL_0643:  ldc.i4.1
  IL_0644:  ldnull
  IL_0645:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_064a:  stelem.ref
  IL_064b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0650:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0655:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site16""
  IL_065a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site16""
  IL_065f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_0664:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site16""
  IL_0669:  ldloc.s    V_6
  IL_066b:  ldloc.s    V_4
  IL_066d:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0672:  pop
  IL_0673:  ldloc.s    V_5
  IL_0675:  stloc.s    V_6
  IL_0677:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site20""
  IL_067c:  brtrue.s   IL_069d
  IL_067e:  ldc.i4.0
  IL_067f:  ldstr      ""d1""
  IL_0684:  ldtoken    ""C""
  IL_0689:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_068e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0693:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0698:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site20""
  IL_069d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site20""
  IL_06a2:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_06a7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site20""
  IL_06ac:  ldloc.s    V_6
  IL_06ae:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_06b3:  brtrue     IL_07b3
  IL_06b8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site19""
  IL_06bd:  brtrue.s   IL_06fc
  IL_06bf:  ldc.i4     0x80
  IL_06c4:  ldstr      ""d1""
  IL_06c9:  ldtoken    ""C""
  IL_06ce:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_06d3:  ldc.i4.2
  IL_06d4:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_06d9:  dup
  IL_06da:  ldc.i4.0
  IL_06db:  ldc.i4.0
  IL_06dc:  ldnull
  IL_06dd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_06e2:  stelem.ref
  IL_06e3:  dup
  IL_06e4:  ldc.i4.1
  IL_06e5:  ldc.i4.0
  IL_06e6:  ldnull
  IL_06e7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_06ec:  stelem.ref
  IL_06ed:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_06f2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_06f7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site19""
  IL_06fc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site19""
  IL_0701:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0706:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site19""
  IL_070b:  ldloc.s    V_6
  IL_070d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site18""
  IL_0712:  brtrue.s   IL_074a
  IL_0714:  ldc.i4.0
  IL_0715:  ldc.i4.s   73
  IL_0717:  ldtoken    ""C""
  IL_071c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0721:  ldc.i4.2
  IL_0722:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0727:  dup
  IL_0728:  ldc.i4.0
  IL_0729:  ldc.i4.0
  IL_072a:  ldnull
  IL_072b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0730:  stelem.ref
  IL_0731:  dup
  IL_0732:  ldc.i4.1
  IL_0733:  ldc.i4.1
  IL_0734:  ldnull
  IL_0735:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_073a:  stelem.ref
  IL_073b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0740:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0745:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site18""
  IL_074a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site18""
  IL_074f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0754:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site18""
  IL_0759:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site17""
  IL_075e:  brtrue.s   IL_078f
  IL_0760:  ldc.i4.0
  IL_0761:  ldstr      ""d1""
  IL_0766:  ldtoken    ""C""
  IL_076b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0770:  ldc.i4.1
  IL_0771:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0776:  dup
  IL_0777:  ldc.i4.0
  IL_0778:  ldc.i4.0
  IL_0779:  ldnull
  IL_077a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_077f:  stelem.ref
  IL_0780:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0785:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_078a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site17""
  IL_078f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site17""
  IL_0794:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0799:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site17""
  IL_079e:  ldloc.s    V_6
  IL_07a0:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_07a5:  ldloc.2
  IL_07a6:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_07ab:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_07b0:  pop
  IL_07b1:  br.s       IL_0810
  IL_07b3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site21""
  IL_07b8:  brtrue.s   IL_07f8
  IL_07ba:  ldc.i4     0x104
  IL_07bf:  ldstr      ""remove_d1""
  IL_07c4:  ldnull
  IL_07c5:  ldtoken    ""C""
  IL_07ca:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_07cf:  ldc.i4.2
  IL_07d0:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_07d5:  dup
  IL_07d6:  ldc.i4.0
  IL_07d7:  ldc.i4.0
  IL_07d8:  ldnull
  IL_07d9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_07de:  stelem.ref
  IL_07df:  dup
  IL_07e0:  ldc.i4.1
  IL_07e1:  ldc.i4.1
  IL_07e2:  ldnull
  IL_07e3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_07e8:  stelem.ref
  IL_07e9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_07ee:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_07f3:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site21""
  IL_07f8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site21""
  IL_07fd:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0802:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site21""
  IL_0807:  ldloc.s    V_6
  IL_0809:  ldloc.2
  IL_080a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_080f:  pop
  IL_0810:  ldloc.s    V_5
  IL_0812:  stloc.s    V_6
  IL_0814:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site25""
  IL_0819:  brtrue.s   IL_083a
  IL_081b:  ldc.i4.0
  IL_081c:  ldstr      ""d2""
  IL_0821:  ldtoken    ""C""
  IL_0826:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_082b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0830:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0835:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site25""
  IL_083a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site25""
  IL_083f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0844:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site25""
  IL_0849:  ldloc.s    V_6
  IL_084b:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0850:  brtrue     IL_0950
  IL_0855:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site24""
  IL_085a:  brtrue.s   IL_0899
  IL_085c:  ldc.i4     0x80
  IL_0861:  ldstr      ""d2""
  IL_0866:  ldtoken    ""C""
  IL_086b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0870:  ldc.i4.2
  IL_0871:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0876:  dup
  IL_0877:  ldc.i4.0
  IL_0878:  ldc.i4.0
  IL_0879:  ldnull
  IL_087a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_087f:  stelem.ref
  IL_0880:  dup
  IL_0881:  ldc.i4.1
  IL_0882:  ldc.i4.0
  IL_0883:  ldnull
  IL_0884:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0889:  stelem.ref
  IL_088a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_088f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0894:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site24""
  IL_0899:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site24""
  IL_089e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_08a3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site24""
  IL_08a8:  ldloc.s    V_6
  IL_08aa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site23""
  IL_08af:  brtrue.s   IL_08e7
  IL_08b1:  ldc.i4.0
  IL_08b2:  ldc.i4.s   73
  IL_08b4:  ldtoken    ""C""
  IL_08b9:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_08be:  ldc.i4.2
  IL_08bf:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_08c4:  dup
  IL_08c5:  ldc.i4.0
  IL_08c6:  ldc.i4.0
  IL_08c7:  ldnull
  IL_08c8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_08cd:  stelem.ref
  IL_08ce:  dup
  IL_08cf:  ldc.i4.1
  IL_08d0:  ldc.i4.1
  IL_08d1:  ldnull
  IL_08d2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_08d7:  stelem.ref
  IL_08d8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_08dd:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_08e2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site23""
  IL_08e7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site23""
  IL_08ec:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_08f1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site23""
  IL_08f6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site22""
  IL_08fb:  brtrue.s   IL_092c
  IL_08fd:  ldc.i4.0
  IL_08fe:  ldstr      ""d2""
  IL_0903:  ldtoken    ""C""
  IL_0908:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_090d:  ldc.i4.1
  IL_090e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0913:  dup
  IL_0914:  ldc.i4.0
  IL_0915:  ldc.i4.0
  IL_0916:  ldnull
  IL_0917:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_091c:  stelem.ref
  IL_091d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0922:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0927:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site22""
  IL_092c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site22""
  IL_0931:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0936:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site22""
  IL_093b:  ldloc.s    V_6
  IL_093d:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0942:  ldloc.3
  IL_0943:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0948:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_094d:  pop
  IL_094e:  br.s       IL_09ad
  IL_0950:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site26""
  IL_0955:  brtrue.s   IL_0995
  IL_0957:  ldc.i4     0x104
  IL_095c:  ldstr      ""remove_d2""
  IL_0961:  ldnull
  IL_0962:  ldtoken    ""C""
  IL_0967:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_096c:  ldc.i4.2
  IL_096d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0972:  dup
  IL_0973:  ldc.i4.0
  IL_0974:  ldc.i4.0
  IL_0975:  ldnull
  IL_0976:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_097b:  stelem.ref
  IL_097c:  dup
  IL_097d:  ldc.i4.1
  IL_097e:  ldc.i4.1
  IL_097f:  ldnull
  IL_0980:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0985:  stelem.ref
  IL_0986:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_098b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0990:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site26""
  IL_0995:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site26""
  IL_099a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_099f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site26""
  IL_09a4:  ldloc.s    V_6
  IL_09a6:  ldloc.3
  IL_09a7:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_09ac:  pop
  IL_09ad:  ldloc.s    V_5
  IL_09af:  stloc.s    V_6
  IL_09b1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site30""
  IL_09b6:  brtrue.s   IL_09d7
  IL_09b8:  ldc.i4.0
  IL_09b9:  ldstr      ""d3""
  IL_09be:  ldtoken    ""C""
  IL_09c3:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_09c8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_09cd:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_09d2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site30""
  IL_09d7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site30""
  IL_09dc:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_09e1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site30""
  IL_09e6:  ldloc.s    V_6
  IL_09e8:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_09ed:  brtrue     IL_0aee
  IL_09f2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site29""
  IL_09f7:  brtrue.s   IL_0a36
  IL_09f9:  ldc.i4     0x80
  IL_09fe:  ldstr      ""d3""
  IL_0a03:  ldtoken    ""C""
  IL_0a08:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0a0d:  ldc.i4.2
  IL_0a0e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0a13:  dup
  IL_0a14:  ldc.i4.0
  IL_0a15:  ldc.i4.0
  IL_0a16:  ldnull
  IL_0a17:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0a1c:  stelem.ref
  IL_0a1d:  dup
  IL_0a1e:  ldc.i4.1
  IL_0a1f:  ldc.i4.0
  IL_0a20:  ldnull
  IL_0a21:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0a26:  stelem.ref
  IL_0a27:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0a2c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0a31:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site29""
  IL_0a36:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site29""
  IL_0a3b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0a40:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site29""
  IL_0a45:  ldloc.s    V_6
  IL_0a47:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site28""
  IL_0a4c:  brtrue.s   IL_0a84
  IL_0a4e:  ldc.i4.0
  IL_0a4f:  ldc.i4.s   73
  IL_0a51:  ldtoken    ""C""
  IL_0a56:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0a5b:  ldc.i4.2
  IL_0a5c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0a61:  dup
  IL_0a62:  ldc.i4.0
  IL_0a63:  ldc.i4.0
  IL_0a64:  ldnull
  IL_0a65:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0a6a:  stelem.ref
  IL_0a6b:  dup
  IL_0a6c:  ldc.i4.1
  IL_0a6d:  ldc.i4.1
  IL_0a6e:  ldnull
  IL_0a6f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0a74:  stelem.ref
  IL_0a75:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0a7a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0a7f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site28""
  IL_0a84:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site28""
  IL_0a89:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_0a8e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site28""
  IL_0a93:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site27""
  IL_0a98:  brtrue.s   IL_0ac9
  IL_0a9a:  ldc.i4.0
  IL_0a9b:  ldstr      ""d3""
  IL_0aa0:  ldtoken    ""C""
  IL_0aa5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0aaa:  ldc.i4.1
  IL_0aab:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0ab0:  dup
  IL_0ab1:  ldc.i4.0
  IL_0ab2:  ldc.i4.0
  IL_0ab3:  ldnull
  IL_0ab4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ab9:  stelem.ref
  IL_0aba:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0abf:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0ac4:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site27""
  IL_0ac9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site27""
  IL_0ace:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0ad3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site27""
  IL_0ad8:  ldloc.s    V_6
  IL_0ada:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0adf:  ldloc.s    V_4
  IL_0ae1:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0ae6:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0aeb:  pop
  IL_0aec:  br.s       IL_0b4c
  IL_0aee:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site31""
  IL_0af3:  brtrue.s   IL_0b33
  IL_0af5:  ldc.i4     0x104
  IL_0afa:  ldstr      ""remove_d3""
  IL_0aff:  ldnull
  IL_0b00:  ldtoken    ""C""
  IL_0b05:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0b0a:  ldc.i4.2
  IL_0b0b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0b10:  dup
  IL_0b11:  ldc.i4.0
  IL_0b12:  ldc.i4.0
  IL_0b13:  ldnull
  IL_0b14:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0b19:  stelem.ref
  IL_0b1a:  dup
  IL_0b1b:  ldc.i4.1
  IL_0b1c:  ldc.i4.1
  IL_0b1d:  ldnull
  IL_0b1e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0b23:  stelem.ref
  IL_0b24:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0b29:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0b2e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site31""
  IL_0b33:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site31""
  IL_0b38:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_0b3d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site31""
  IL_0b42:  ldloc.s    V_6
  IL_0b44:  ldloc.s    V_4
  IL_0b46:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0b4b:  pop
  IL_0b4c:  ldloc.s    V_5
  IL_0b4e:  stloc.s    V_6
  IL_0b50:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site35""
  IL_0b55:  brtrue.s   IL_0b76
  IL_0b57:  ldc.i4.0
  IL_0b58:  ldstr      ""d1""
  IL_0b5d:  ldtoken    ""C""
  IL_0b62:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0b67:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0b6c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0b71:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site35""
  IL_0b76:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site35""
  IL_0b7b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0b80:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site35""
  IL_0b85:  ldloc.s    V_6
  IL_0b87:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0b8c:  brtrue     IL_0c8c
  IL_0b91:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site34""
  IL_0b96:  brtrue.s   IL_0bd5
  IL_0b98:  ldc.i4     0x80
  IL_0b9d:  ldstr      ""d1""
  IL_0ba2:  ldtoken    ""C""
  IL_0ba7:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0bac:  ldc.i4.2
  IL_0bad:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0bb2:  dup
  IL_0bb3:  ldc.i4.0
  IL_0bb4:  ldc.i4.0
  IL_0bb5:  ldnull
  IL_0bb6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0bbb:  stelem.ref
  IL_0bbc:  dup
  IL_0bbd:  ldc.i4.1
  IL_0bbe:  ldc.i4.0
  IL_0bbf:  ldnull
  IL_0bc0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0bc5:  stelem.ref
  IL_0bc6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0bcb:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0bd0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site34""
  IL_0bd5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site34""
  IL_0bda:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0bdf:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site34""
  IL_0be4:  ldloc.s    V_6
  IL_0be6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site33""
  IL_0beb:  brtrue.s   IL_0c23
  IL_0bed:  ldc.i4.0
  IL_0bee:  ldc.i4.s   63
  IL_0bf0:  ldtoken    ""C""
  IL_0bf5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0bfa:  ldc.i4.2
  IL_0bfb:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0c00:  dup
  IL_0c01:  ldc.i4.0
  IL_0c02:  ldc.i4.0
  IL_0c03:  ldnull
  IL_0c04:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0c09:  stelem.ref
  IL_0c0a:  dup
  IL_0c0b:  ldc.i4.1
  IL_0c0c:  ldc.i4.1
  IL_0c0d:  ldnull
  IL_0c0e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0c13:  stelem.ref
  IL_0c14:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0c19:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0c1e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site33""
  IL_0c23:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site33""
  IL_0c28:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0c2d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site33""
  IL_0c32:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site32""
  IL_0c37:  brtrue.s   IL_0c68
  IL_0c39:  ldc.i4.0
  IL_0c3a:  ldstr      ""d1""
  IL_0c3f:  ldtoken    ""C""
  IL_0c44:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0c49:  ldc.i4.1
  IL_0c4a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0c4f:  dup
  IL_0c50:  ldc.i4.0
  IL_0c51:  ldc.i4.0
  IL_0c52:  ldnull
  IL_0c53:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0c58:  stelem.ref
  IL_0c59:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0c5e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0c63:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site32""
  IL_0c68:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site32""
  IL_0c6d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0c72:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site32""
  IL_0c77:  ldloc.s    V_6
  IL_0c79:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0c7e:  ldloc.3
  IL_0c7f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0c84:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0c89:  pop
  IL_0c8a:  br.s       IL_0ce9
  IL_0c8c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site36""
  IL_0c91:  brtrue.s   IL_0cd1
  IL_0c93:  ldc.i4     0x104
  IL_0c98:  ldstr      ""add_d1""
  IL_0c9d:  ldnull
  IL_0c9e:  ldtoken    ""C""
  IL_0ca3:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0ca8:  ldc.i4.2
  IL_0ca9:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0cae:  dup
  IL_0caf:  ldc.i4.0
  IL_0cb0:  ldc.i4.0
  IL_0cb1:  ldnull
  IL_0cb2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0cb7:  stelem.ref
  IL_0cb8:  dup
  IL_0cb9:  ldc.i4.1
  IL_0cba:  ldc.i4.1
  IL_0cbb:  ldnull
  IL_0cbc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0cc1:  stelem.ref
  IL_0cc2:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0cc7:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0ccc:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site36""
  IL_0cd1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site36""
  IL_0cd6:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0cdb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site36""
  IL_0ce0:  ldloc.s    V_6
  IL_0ce2:  ldloc.3
  IL_0ce3:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0ce8:  pop
  IL_0ce9:  ldloc.1
  IL_0cea:  stloc.s    V_5
  IL_0cec:  ldloc.s    V_5
  IL_0cee:  stloc.s    V_6
  IL_0cf0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site40""
  IL_0cf5:  brtrue.s   IL_0d16
  IL_0cf7:  ldc.i4.0
  IL_0cf8:  ldstr      ""d1""
  IL_0cfd:  ldtoken    ""C""
  IL_0d02:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0d07:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0d0c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0d11:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site40""
  IL_0d16:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site40""
  IL_0d1b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0d20:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site40""
  IL_0d25:  ldloc.s    V_6
  IL_0d27:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0d2c:  brtrue     IL_0e2c
  IL_0d31:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site39""
  IL_0d36:  brtrue.s   IL_0d75
  IL_0d38:  ldc.i4     0x80
  IL_0d3d:  ldstr      ""d1""
  IL_0d42:  ldtoken    ""C""
  IL_0d47:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0d4c:  ldc.i4.2
  IL_0d4d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0d52:  dup
  IL_0d53:  ldc.i4.0
  IL_0d54:  ldc.i4.0
  IL_0d55:  ldnull
  IL_0d56:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0d5b:  stelem.ref
  IL_0d5c:  dup
  IL_0d5d:  ldc.i4.1
  IL_0d5e:  ldc.i4.0
  IL_0d5f:  ldnull
  IL_0d60:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0d65:  stelem.ref
  IL_0d66:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0d6b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0d70:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site39""
  IL_0d75:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site39""
  IL_0d7a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0d7f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site39""
  IL_0d84:  ldloc.s    V_6
  IL_0d86:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site38""
  IL_0d8b:  brtrue.s   IL_0dc3
  IL_0d8d:  ldc.i4.0
  IL_0d8e:  ldc.i4.s   63
  IL_0d90:  ldtoken    ""C""
  IL_0d95:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0d9a:  ldc.i4.2
  IL_0d9b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0da0:  dup
  IL_0da1:  ldc.i4.0
  IL_0da2:  ldc.i4.0
  IL_0da3:  ldnull
  IL_0da4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0da9:  stelem.ref
  IL_0daa:  dup
  IL_0dab:  ldc.i4.1
  IL_0dac:  ldc.i4.1
  IL_0dad:  ldnull
  IL_0dae:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0db3:  stelem.ref
  IL_0db4:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0db9:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0dbe:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site38""
  IL_0dc3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site38""
  IL_0dc8:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0dcd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site38""
  IL_0dd2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site37""
  IL_0dd7:  brtrue.s   IL_0e08
  IL_0dd9:  ldc.i4.0
  IL_0dda:  ldstr      ""d1""
  IL_0ddf:  ldtoken    ""C""
  IL_0de4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0de9:  ldc.i4.1
  IL_0dea:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0def:  dup
  IL_0df0:  ldc.i4.0
  IL_0df1:  ldc.i4.0
  IL_0df2:  ldnull
  IL_0df3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0df8:  stelem.ref
  IL_0df9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0dfe:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0e03:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site37""
  IL_0e08:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site37""
  IL_0e0d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0e12:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site37""
  IL_0e17:  ldloc.s    V_6
  IL_0e19:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0e1e:  ldloc.2
  IL_0e1f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0e24:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0e29:  pop
  IL_0e2a:  br.s       IL_0e89
  IL_0e2c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site41""
  IL_0e31:  brtrue.s   IL_0e71
  IL_0e33:  ldc.i4     0x104
  IL_0e38:  ldstr      ""add_d1""
  IL_0e3d:  ldnull
  IL_0e3e:  ldtoken    ""C""
  IL_0e43:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0e48:  ldc.i4.2
  IL_0e49:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0e4e:  dup
  IL_0e4f:  ldc.i4.0
  IL_0e50:  ldc.i4.0
  IL_0e51:  ldnull
  IL_0e52:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0e57:  stelem.ref
  IL_0e58:  dup
  IL_0e59:  ldc.i4.1
  IL_0e5a:  ldc.i4.1
  IL_0e5b:  ldnull
  IL_0e5c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0e61:  stelem.ref
  IL_0e62:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0e67:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0e6c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site41""
  IL_0e71:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site41""
  IL_0e76:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0e7b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site41""
  IL_0e80:  ldloc.s    V_6
  IL_0e82:  ldloc.2
  IL_0e83:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0e88:  pop
  IL_0e89:  ldloc.s    V_5
  IL_0e8b:  stloc.s    V_6
  IL_0e8d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site45""
  IL_0e92:  brtrue.s   IL_0eb3
  IL_0e94:  ldc.i4.0
  IL_0e95:  ldstr      ""d2""
  IL_0e9a:  ldtoken    ""C""
  IL_0e9f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0ea4:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0ea9:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0eae:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site45""
  IL_0eb3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site45""
  IL_0eb8:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0ebd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site45""
  IL_0ec2:  ldloc.s    V_6
  IL_0ec4:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0ec9:  brtrue     IL_0fc9
  IL_0ece:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site44""
  IL_0ed3:  brtrue.s   IL_0f12
  IL_0ed5:  ldc.i4     0x80
  IL_0eda:  ldstr      ""d2""
  IL_0edf:  ldtoken    ""C""
  IL_0ee4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0ee9:  ldc.i4.2
  IL_0eea:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0eef:  dup
  IL_0ef0:  ldc.i4.0
  IL_0ef1:  ldc.i4.0
  IL_0ef2:  ldnull
  IL_0ef3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ef8:  stelem.ref
  IL_0ef9:  dup
  IL_0efa:  ldc.i4.1
  IL_0efb:  ldc.i4.0
  IL_0efc:  ldnull
  IL_0efd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f02:  stelem.ref
  IL_0f03:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0f08:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0f0d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site44""
  IL_0f12:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site44""
  IL_0f17:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0f1c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site44""
  IL_0f21:  ldloc.s    V_6
  IL_0f23:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site43""
  IL_0f28:  brtrue.s   IL_0f60
  IL_0f2a:  ldc.i4.0
  IL_0f2b:  ldc.i4.s   63
  IL_0f2d:  ldtoken    ""C""
  IL_0f32:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0f37:  ldc.i4.2
  IL_0f38:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0f3d:  dup
  IL_0f3e:  ldc.i4.0
  IL_0f3f:  ldc.i4.0
  IL_0f40:  ldnull
  IL_0f41:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f46:  stelem.ref
  IL_0f47:  dup
  IL_0f48:  ldc.i4.1
  IL_0f49:  ldc.i4.1
  IL_0f4a:  ldnull
  IL_0f4b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f50:  stelem.ref
  IL_0f51:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0f56:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0f5b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site43""
  IL_0f60:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site43""
  IL_0f65:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0f6a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site43""
  IL_0f6f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site42""
  IL_0f74:  brtrue.s   IL_0fa5
  IL_0f76:  ldc.i4.0
  IL_0f77:  ldstr      ""d2""
  IL_0f7c:  ldtoken    ""C""
  IL_0f81:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0f86:  ldc.i4.1
  IL_0f87:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0f8c:  dup
  IL_0f8d:  ldc.i4.0
  IL_0f8e:  ldc.i4.0
  IL_0f8f:  ldnull
  IL_0f90:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f95:  stelem.ref
  IL_0f96:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0f9b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0fa0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site42""
  IL_0fa5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site42""
  IL_0faa:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0faf:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site42""
  IL_0fb4:  ldloc.s    V_6
  IL_0fb6:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0fbb:  ldloc.3
  IL_0fbc:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0fc1:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0fc6:  pop
  IL_0fc7:  br.s       IL_1026
  IL_0fc9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site46""
  IL_0fce:  brtrue.s   IL_100e
  IL_0fd0:  ldc.i4     0x104
  IL_0fd5:  ldstr      ""add_d2""
  IL_0fda:  ldnull
  IL_0fdb:  ldtoken    ""C""
  IL_0fe0:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0fe5:  ldc.i4.2
  IL_0fe6:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0feb:  dup
  IL_0fec:  ldc.i4.0
  IL_0fed:  ldc.i4.0
  IL_0fee:  ldnull
  IL_0fef:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ff4:  stelem.ref
  IL_0ff5:  dup
  IL_0ff6:  ldc.i4.1
  IL_0ff7:  ldc.i4.1
  IL_0ff8:  ldnull
  IL_0ff9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ffe:  stelem.ref
  IL_0fff:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1004:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1009:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site46""
  IL_100e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site46""
  IL_1013:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_1018:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site46""
  IL_101d:  ldloc.s    V_6
  IL_101f:  ldloc.3
  IL_1020:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_1025:  pop
  IL_1026:  ldloc.s    V_5
  IL_1028:  stloc.s    V_6
  IL_102a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site50""
  IL_102f:  brtrue.s   IL_1050
  IL_1031:  ldc.i4.0
  IL_1032:  ldstr      ""d3""
  IL_1037:  ldtoken    ""C""
  IL_103c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1041:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_1046:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_104b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site50""
  IL_1050:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site50""
  IL_1055:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_105a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site50""
  IL_105f:  ldloc.s    V_6
  IL_1061:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1066:  brtrue     IL_1167
  IL_106b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site49""
  IL_1070:  brtrue.s   IL_10af
  IL_1072:  ldc.i4     0x80
  IL_1077:  ldstr      ""d3""
  IL_107c:  ldtoken    ""C""
  IL_1081:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1086:  ldc.i4.2
  IL_1087:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_108c:  dup
  IL_108d:  ldc.i4.0
  IL_108e:  ldc.i4.0
  IL_108f:  ldnull
  IL_1090:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1095:  stelem.ref
  IL_1096:  dup
  IL_1097:  ldc.i4.1
  IL_1098:  ldc.i4.0
  IL_1099:  ldnull
  IL_109a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_109f:  stelem.ref
  IL_10a0:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_10a5:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_10aa:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site49""
  IL_10af:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site49""
  IL_10b4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_10b9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site49""
  IL_10be:  ldloc.s    V_6
  IL_10c0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site48""
  IL_10c5:  brtrue.s   IL_10fd
  IL_10c7:  ldc.i4.0
  IL_10c8:  ldc.i4.s   63
  IL_10ca:  ldtoken    ""C""
  IL_10cf:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_10d4:  ldc.i4.2
  IL_10d5:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_10da:  dup
  IL_10db:  ldc.i4.0
  IL_10dc:  ldc.i4.0
  IL_10dd:  ldnull
  IL_10de:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_10e3:  stelem.ref
  IL_10e4:  dup
  IL_10e5:  ldc.i4.1
  IL_10e6:  ldc.i4.1
  IL_10e7:  ldnull
  IL_10e8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_10ed:  stelem.ref
  IL_10ee:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_10f3:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_10f8:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site48""
  IL_10fd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site48""
  IL_1102:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_1107:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site48""
  IL_110c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site47""
  IL_1111:  brtrue.s   IL_1142
  IL_1113:  ldc.i4.0
  IL_1114:  ldstr      ""d3""
  IL_1119:  ldtoken    ""C""
  IL_111e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1123:  ldc.i4.1
  IL_1124:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1129:  dup
  IL_112a:  ldc.i4.0
  IL_112b:  ldc.i4.0
  IL_112c:  ldnull
  IL_112d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1132:  stelem.ref
  IL_1133:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1138:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_113d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site47""
  IL_1142:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site47""
  IL_1147:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_114c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site47""
  IL_1151:  ldloc.s    V_6
  IL_1153:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1158:  ldloc.s    V_4
  IL_115a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_115f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_1164:  pop
  IL_1165:  br.s       IL_11c5
  IL_1167:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site51""
  IL_116c:  brtrue.s   IL_11ac
  IL_116e:  ldc.i4     0x104
  IL_1173:  ldstr      ""add_d3""
  IL_1178:  ldnull
  IL_1179:  ldtoken    ""C""
  IL_117e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1183:  ldc.i4.2
  IL_1184:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1189:  dup
  IL_118a:  ldc.i4.0
  IL_118b:  ldc.i4.0
  IL_118c:  ldnull
  IL_118d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1192:  stelem.ref
  IL_1193:  dup
  IL_1194:  ldc.i4.1
  IL_1195:  ldc.i4.1
  IL_1196:  ldnull
  IL_1197:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_119c:  stelem.ref
  IL_119d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_11a2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_11a7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site51""
  IL_11ac:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site51""
  IL_11b1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_11b6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site51""
  IL_11bb:  ldloc.s    V_6
  IL_11bd:  ldloc.s    V_4
  IL_11bf:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_11c4:  pop
  IL_11c5:  ldloc.s    V_5
  IL_11c7:  stloc.s    V_6
  IL_11c9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site55""
  IL_11ce:  brtrue.s   IL_11ef
  IL_11d0:  ldc.i4.0
  IL_11d1:  ldstr      ""d1""
  IL_11d6:  ldtoken    ""C""
  IL_11db:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_11e0:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_11e5:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_11ea:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site55""
  IL_11ef:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site55""
  IL_11f4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_11f9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site55""
  IL_11fe:  ldloc.s    V_6
  IL_1200:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1205:  brtrue     IL_1305
  IL_120a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site54""
  IL_120f:  brtrue.s   IL_124e
  IL_1211:  ldc.i4     0x80
  IL_1216:  ldstr      ""d1""
  IL_121b:  ldtoken    ""C""
  IL_1220:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1225:  ldc.i4.2
  IL_1226:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_122b:  dup
  IL_122c:  ldc.i4.0
  IL_122d:  ldc.i4.0
  IL_122e:  ldnull
  IL_122f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1234:  stelem.ref
  IL_1235:  dup
  IL_1236:  ldc.i4.1
  IL_1237:  ldc.i4.0
  IL_1238:  ldnull
  IL_1239:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_123e:  stelem.ref
  IL_123f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1244:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1249:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site54""
  IL_124e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site54""
  IL_1253:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_1258:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site54""
  IL_125d:  ldloc.s    V_6
  IL_125f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site53""
  IL_1264:  brtrue.s   IL_129c
  IL_1266:  ldc.i4.0
  IL_1267:  ldc.i4.s   73
  IL_1269:  ldtoken    ""C""
  IL_126e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1273:  ldc.i4.2
  IL_1274:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1279:  dup
  IL_127a:  ldc.i4.0
  IL_127b:  ldc.i4.0
  IL_127c:  ldnull
  IL_127d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1282:  stelem.ref
  IL_1283:  dup
  IL_1284:  ldc.i4.1
  IL_1285:  ldc.i4.1
  IL_1286:  ldnull
  IL_1287:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_128c:  stelem.ref
  IL_128d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1292:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1297:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site53""
  IL_129c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site53""
  IL_12a1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_12a6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site53""
  IL_12ab:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site52""
  IL_12b0:  brtrue.s   IL_12e1
  IL_12b2:  ldc.i4.0
  IL_12b3:  ldstr      ""d1""
  IL_12b8:  ldtoken    ""C""
  IL_12bd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_12c2:  ldc.i4.1
  IL_12c3:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_12c8:  dup
  IL_12c9:  ldc.i4.0
  IL_12ca:  ldc.i4.0
  IL_12cb:  ldnull
  IL_12cc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_12d1:  stelem.ref
  IL_12d2:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_12d7:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_12dc:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site52""
  IL_12e1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site52""
  IL_12e6:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_12eb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site52""
  IL_12f0:  ldloc.s    V_6
  IL_12f2:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_12f7:  ldloc.2
  IL_12f8:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_12fd:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_1302:  pop
  IL_1303:  br.s       IL_1362
  IL_1305:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site56""
  IL_130a:  brtrue.s   IL_134a
  IL_130c:  ldc.i4     0x104
  IL_1311:  ldstr      ""remove_d1""
  IL_1316:  ldnull
  IL_1317:  ldtoken    ""C""
  IL_131c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1321:  ldc.i4.2
  IL_1322:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1327:  dup
  IL_1328:  ldc.i4.0
  IL_1329:  ldc.i4.0
  IL_132a:  ldnull
  IL_132b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1330:  stelem.ref
  IL_1331:  dup
  IL_1332:  ldc.i4.1
  IL_1333:  ldc.i4.1
  IL_1334:  ldnull
  IL_1335:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_133a:  stelem.ref
  IL_133b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1340:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1345:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site56""
  IL_134a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site56""
  IL_134f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_1354:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site56""
  IL_1359:  ldloc.s    V_6
  IL_135b:  ldloc.2
  IL_135c:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_1361:  pop
  IL_1362:  ldloc.s    V_5
  IL_1364:  stloc.s    V_6
  IL_1366:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site60""
  IL_136b:  brtrue.s   IL_138c
  IL_136d:  ldc.i4.0
  IL_136e:  ldstr      ""d2""
  IL_1373:  ldtoken    ""C""
  IL_1378:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_137d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_1382:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1387:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site60""
  IL_138c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site60""
  IL_1391:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_1396:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site60""
  IL_139b:  ldloc.s    V_6
  IL_139d:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_13a2:  brtrue     IL_14a2
  IL_13a7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site59""
  IL_13ac:  brtrue.s   IL_13eb
  IL_13ae:  ldc.i4     0x80
  IL_13b3:  ldstr      ""d2""
  IL_13b8:  ldtoken    ""C""
  IL_13bd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_13c2:  ldc.i4.2
  IL_13c3:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_13c8:  dup
  IL_13c9:  ldc.i4.0
  IL_13ca:  ldc.i4.0
  IL_13cb:  ldnull
  IL_13cc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_13d1:  stelem.ref
  IL_13d2:  dup
  IL_13d3:  ldc.i4.1
  IL_13d4:  ldc.i4.0
  IL_13d5:  ldnull
  IL_13d6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_13db:  stelem.ref
  IL_13dc:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_13e1:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_13e6:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site59""
  IL_13eb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site59""
  IL_13f0:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_13f5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site59""
  IL_13fa:  ldloc.s    V_6
  IL_13fc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site58""
  IL_1401:  brtrue.s   IL_1439
  IL_1403:  ldc.i4.0
  IL_1404:  ldc.i4.s   73
  IL_1406:  ldtoken    ""C""
  IL_140b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1410:  ldc.i4.2
  IL_1411:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1416:  dup
  IL_1417:  ldc.i4.0
  IL_1418:  ldc.i4.0
  IL_1419:  ldnull
  IL_141a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_141f:  stelem.ref
  IL_1420:  dup
  IL_1421:  ldc.i4.1
  IL_1422:  ldc.i4.1
  IL_1423:  ldnull
  IL_1424:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1429:  stelem.ref
  IL_142a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_142f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1434:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site58""
  IL_1439:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site58""
  IL_143e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_1443:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site58""
  IL_1448:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site57""
  IL_144d:  brtrue.s   IL_147e
  IL_144f:  ldc.i4.0
  IL_1450:  ldstr      ""d2""
  IL_1455:  ldtoken    ""C""
  IL_145a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_145f:  ldc.i4.1
  IL_1460:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1465:  dup
  IL_1466:  ldc.i4.0
  IL_1467:  ldc.i4.0
  IL_1468:  ldnull
  IL_1469:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_146e:  stelem.ref
  IL_146f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1474:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1479:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site57""
  IL_147e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site57""
  IL_1483:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_1488:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site57""
  IL_148d:  ldloc.s    V_6
  IL_148f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1494:  ldloc.3
  IL_1495:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_149a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_149f:  pop
  IL_14a0:  br.s       IL_14ff
  IL_14a2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site61""
  IL_14a7:  brtrue.s   IL_14e7
  IL_14a9:  ldc.i4     0x104
  IL_14ae:  ldstr      ""remove_d2""
  IL_14b3:  ldnull
  IL_14b4:  ldtoken    ""C""
  IL_14b9:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_14be:  ldc.i4.2
  IL_14bf:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_14c4:  dup
  IL_14c5:  ldc.i4.0
  IL_14c6:  ldc.i4.0
  IL_14c7:  ldnull
  IL_14c8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_14cd:  stelem.ref
  IL_14ce:  dup
  IL_14cf:  ldc.i4.1
  IL_14d0:  ldc.i4.1
  IL_14d1:  ldnull
  IL_14d2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_14d7:  stelem.ref
  IL_14d8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_14dd:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_14e2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site61""
  IL_14e7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site61""
  IL_14ec:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_14f1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site61""
  IL_14f6:  ldloc.s    V_6
  IL_14f8:  ldloc.3
  IL_14f9:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_14fe:  pop
  IL_14ff:  ldloc.s    V_5
  IL_1501:  stloc.s    V_6
  IL_1503:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site65""
  IL_1508:  brtrue.s   IL_1529
  IL_150a:  ldc.i4.0
  IL_150b:  ldstr      ""d3""
  IL_1510:  ldtoken    ""C""
  IL_1515:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_151a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_151f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1524:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site65""
  IL_1529:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site65""
  IL_152e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_1533:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site65""
  IL_1538:  ldloc.s    V_6
  IL_153a:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_153f:  brtrue     IL_1640
  IL_1544:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site64""
  IL_1549:  brtrue.s   IL_1588
  IL_154b:  ldc.i4     0x80
  IL_1550:  ldstr      ""d3""
  IL_1555:  ldtoken    ""C""
  IL_155a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_155f:  ldc.i4.2
  IL_1560:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1565:  dup
  IL_1566:  ldc.i4.0
  IL_1567:  ldc.i4.0
  IL_1568:  ldnull
  IL_1569:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_156e:  stelem.ref
  IL_156f:  dup
  IL_1570:  ldc.i4.1
  IL_1571:  ldc.i4.0
  IL_1572:  ldnull
  IL_1573:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1578:  stelem.ref
  IL_1579:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_157e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1583:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site64""
  IL_1588:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site64""
  IL_158d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_1592:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site64""
  IL_1597:  ldloc.s    V_6
  IL_1599:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site63""
  IL_159e:  brtrue.s   IL_15d6
  IL_15a0:  ldc.i4.0
  IL_15a1:  ldc.i4.s   73
  IL_15a3:  ldtoken    ""C""
  IL_15a8:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_15ad:  ldc.i4.2
  IL_15ae:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_15b3:  dup
  IL_15b4:  ldc.i4.0
  IL_15b5:  ldc.i4.0
  IL_15b6:  ldnull
  IL_15b7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_15bc:  stelem.ref
  IL_15bd:  dup
  IL_15be:  ldc.i4.1
  IL_15bf:  ldc.i4.1
  IL_15c0:  ldnull
  IL_15c1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_15c6:  stelem.ref
  IL_15c7:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_15cc:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_15d1:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site63""
  IL_15d6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site63""
  IL_15db:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_15e0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site63""
  IL_15e5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site62""
  IL_15ea:  brtrue.s   IL_161b
  IL_15ec:  ldc.i4.0
  IL_15ed:  ldstr      ""d3""
  IL_15f2:  ldtoken    ""C""
  IL_15f7:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_15fc:  ldc.i4.1
  IL_15fd:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1602:  dup
  IL_1603:  ldc.i4.0
  IL_1604:  ldc.i4.0
  IL_1605:  ldnull
  IL_1606:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_160b:  stelem.ref
  IL_160c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1611:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1616:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site62""
  IL_161b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site62""
  IL_1620:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_1625:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site62""
  IL_162a:  ldloc.s    V_6
  IL_162c:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1631:  ldloc.s    V_4
  IL_1633:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_1638:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_163d:  pop
  IL_163e:  br.s       IL_169e
  IL_1640:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site66""
  IL_1645:  brtrue.s   IL_1685
  IL_1647:  ldc.i4     0x104
  IL_164c:  ldstr      ""remove_d3""
  IL_1651:  ldnull
  IL_1652:  ldtoken    ""C""
  IL_1657:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_165c:  ldc.i4.2
  IL_165d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1662:  dup
  IL_1663:  ldc.i4.0
  IL_1664:  ldc.i4.0
  IL_1665:  ldnull
  IL_1666:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_166b:  stelem.ref
  IL_166c:  dup
  IL_166d:  ldc.i4.1
  IL_166e:  ldc.i4.1
  IL_166f:  ldnull
  IL_1670:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1675:  stelem.ref
  IL_1676:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_167b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1680:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site66""
  IL_1685:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site66""
  IL_168a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_168f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site66""
  IL_1694:  ldloc.s    V_6
  IL_1696:  ldloc.s    V_4
  IL_1698:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_169d:  pop
  IL_169e:  ldloc.s    V_5
  IL_16a0:  stloc.s    V_6
  IL_16a2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site70""
  IL_16a7:  brtrue.s   IL_16c8
  IL_16a9:  ldc.i4.0
  IL_16aa:  ldstr      ""d1""
  IL_16af:  ldtoken    ""C""
  IL_16b4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_16b9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_16be:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_16c3:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site70""
  IL_16c8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site70""
  IL_16cd:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_16d2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site70""
  IL_16d7:  ldloc.s    V_6
  IL_16d9:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_16de:  brtrue     IL_17dd
  IL_16e3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site69""
  IL_16e8:  brtrue.s   IL_1727
  IL_16ea:  ldc.i4     0x80
  IL_16ef:  ldstr      ""d1""
  IL_16f4:  ldtoken    ""C""
  IL_16f9:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_16fe:  ldc.i4.2
  IL_16ff:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1704:  dup
  IL_1705:  ldc.i4.0
  IL_1706:  ldc.i4.0
  IL_1707:  ldnull
  IL_1708:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_170d:  stelem.ref
  IL_170e:  dup
  IL_170f:  ldc.i4.1
  IL_1710:  ldc.i4.0
  IL_1711:  ldnull
  IL_1712:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1717:  stelem.ref
  IL_1718:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_171d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1722:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site69""
  IL_1727:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site69""
  IL_172c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_1731:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site69""
  IL_1736:  ldloc.s    V_6
  IL_1738:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site68""
  IL_173d:  brtrue.s   IL_1775
  IL_173f:  ldc.i4.0
  IL_1740:  ldc.i4.s   63
  IL_1742:  ldtoken    ""C""
  IL_1747:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_174c:  ldc.i4.2
  IL_174d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1752:  dup
  IL_1753:  ldc.i4.0
  IL_1754:  ldc.i4.0
  IL_1755:  ldnull
  IL_1756:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_175b:  stelem.ref
  IL_175c:  dup
  IL_175d:  ldc.i4.1
  IL_175e:  ldc.i4.1
  IL_175f:  ldnull
  IL_1760:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1765:  stelem.ref
  IL_1766:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_176b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1770:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site68""
  IL_1775:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site68""
  IL_177a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_177f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site68""
  IL_1784:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site67""
  IL_1789:  brtrue.s   IL_17ba
  IL_178b:  ldc.i4.0
  IL_178c:  ldstr      ""d1""
  IL_1791:  ldtoken    ""C""
  IL_1796:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_179b:  ldc.i4.1
  IL_179c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_17a1:  dup
  IL_17a2:  ldc.i4.0
  IL_17a3:  ldc.i4.0
  IL_17a4:  ldnull
  IL_17a5:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_17aa:  stelem.ref
  IL_17ab:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_17b0:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_17b5:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site67""
  IL_17ba:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site67""
  IL_17bf:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_17c4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site67""
  IL_17c9:  ldloc.s    V_6
  IL_17cb:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_17d0:  ldloc.3
  IL_17d1:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_17d6:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_17db:  pop
  IL_17dc:  ret
  IL_17dd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site71""
  IL_17e2:  brtrue.s   IL_1822
  IL_17e4:  ldc.i4     0x104
  IL_17e9:  ldstr      ""add_d1""
  IL_17ee:  ldnull
  IL_17ef:  ldtoken    ""C""
  IL_17f4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_17f9:  ldc.i4.2
  IL_17fa:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_17ff:  dup
  IL_1800:  ldc.i4.0
  IL_1801:  ldc.i4.0
  IL_1802:  ldnull
  IL_1803:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1808:  stelem.ref
  IL_1809:  dup
  IL_180a:  ldc.i4.1
  IL_180b:  ldc.i4.1
  IL_180c:  ldnull
  IL_180d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1812:  stelem.ref
  IL_1813:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1818:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_181d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site71""
  IL_1822:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site71""
  IL_1827:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_182c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site71""
  IL_1831:  ldloc.s    V_6
  IL_1833:  ldloc.3
  IL_1834:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_1839:  pop
  IL_183a:  ret
}
");
        }

        [Fact]
        public void WinMdEventInternalStaticAccess()
        {
            var src =
@"  
using EventLibrary;

public partial class A : I
{
    // Remove a delegate from inside of the class
    public static bool Scenario1(A d)
    {
        var testDelegate = new voidDelegate(() => { ; });

        // Setup
        d.d1 = testDelegate;
        d.d1 -= testDelegate;
        return d.d1 == null;
    }

    // Remove a delegate from inside of the class
    public bool Scenario2()
    {
        A d = this;
        var testDelegate = new voidDelegate(() => { ; });

        // Setup
        d.d1 = testDelegate;
        d.d1 -= testDelegate;
        return d.d1 == null;
    }
}";
            var verifier = CompileAndVerifyOnWin8Only(
                new[] { src, DynamicCommonSrc },
                additionalRefs: new[] {
                    MscorlibRef_v4_0_30316_17626,
                    SystemCoreRef_v4_0_30319_17929,
                    EventLibRef,
                },
                emitOptions: EmitOptions.RefEmitBug);
            verifier.VerifyDiagnostics(
                // (6,42): warning CS0067: The event 'A.d2' is never used
                //     public event genericDelegate<object> d2;
    Diagnostic(ErrorCode.WRN_UnreferencedEvent, "d2").WithArguments("A.d2"),
                // (7,34): warning CS0067: The event 'A.d3' is never used
                //     public event dynamicDelegate d3;
    Diagnostic(ErrorCode.WRN_UnreferencedEvent, "d3").WithArguments("A.d3"));
            verifier.VerifyIL("A.Scenario1",
@"
{
  // Code size      117 (0x75)
  .maxstack  3
  .locals init (EventLibrary.voidDelegate V_0) //testDelegate
  IL_0000:  ldsfld     ""EventLibrary.voidDelegate A.CS$<>9__CachedAnonymousMethodDelegate1""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void A.<Scenario1>b__0(object)""
  IL_0010:  newobj     ""EventLibrary.voidDelegate..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""EventLibrary.voidDelegate A.CS$<>9__CachedAnonymousMethodDelegate1""
  IL_001b:  stloc.0
  IL_001c:  ldarg.0
  IL_001d:  dup
  IL_001e:  ldvirtftn  ""void A.d1.remove""
  IL_0024:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0029:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveAllEventHandlers(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>)""
  IL_002e:  ldarg.0
  IL_002f:  dup
  IL_0030:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d1.add""
  IL_0036:  newobj     ""System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_003b:  ldarg.0
  IL_003c:  dup
  IL_003d:  ldvirtftn  ""void A.d1.remove""
  IL_0043:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0048:  ldloc.0
  IL_0049:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.voidDelegate>(System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_004e:  ldarg.0
  IL_004f:  dup
  IL_0050:  ldvirtftn  ""void A.d1.remove""
  IL_0056:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_005b:  ldloc.0
  IL_005c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.voidDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0061:  ldarg.0
  IL_0062:  ldflda     ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate> A.d1""
  IL_0067:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>)""
  IL_006c:  callvirt   ""EventLibrary.voidDelegate System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>.InvocationList.get""
  IL_0071:  ldnull
  IL_0072:  ceq
  IL_0074:  ret
}
");
            verifier.VerifyIL("A.Scenario2",
@"
{
  // Code size      119 (0x77)
  .maxstack  3
  .locals init (A V_0, //d
  EventLibrary.voidDelegate V_1) //testDelegate
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldsfld     ""EventLibrary.voidDelegate A.CS$<>9__CachedAnonymousMethodDelegate3""
  IL_0007:  dup
  IL_0008:  brtrue.s   IL_001d
  IL_000a:  pop
  IL_000b:  ldnull
  IL_000c:  ldftn      ""void A.<Scenario2>b__2(object)""
  IL_0012:  newobj     ""EventLibrary.voidDelegate..ctor(object, System.IntPtr)""
  IL_0017:  dup
  IL_0018:  stsfld     ""EventLibrary.voidDelegate A.CS$<>9__CachedAnonymousMethodDelegate3""
  IL_001d:  stloc.1
  IL_001e:  ldloc.0
  IL_001f:  dup
  IL_0020:  ldvirtftn  ""void A.d1.remove""
  IL_0026:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_002b:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveAllEventHandlers(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>)""
  IL_0030:  ldloc.0
  IL_0031:  dup
  IL_0032:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d1.add""
  IL_0038:  newobj     ""System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_003d:  ldloc.0
  IL_003e:  dup
  IL_003f:  ldvirtftn  ""void A.d1.remove""
  IL_0045:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_004a:  ldloc.1
  IL_004b:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.voidDelegate>(System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0050:  ldloc.0
  IL_0051:  dup
  IL_0052:  ldvirtftn  ""void A.d1.remove""
  IL_0058:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_005d:  ldloc.1
  IL_005e:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.voidDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0063:  ldloc.0
  IL_0064:  ldflda     ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate> A.d1""
  IL_0069:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>)""
  IL_006e:  callvirt   ""EventLibrary.voidDelegate System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>.InvocationList.get""
  IL_0073:  ldnull
  IL_0074:  ceq
  IL_0076:  ret
}
");
        }

        [Fact]
        public void WinMdEventLambda()
        {
            var text = @"
                            using Windows.UI.Xaml;
                            using Windows.ApplicationModel;
                            
                            public class abcdef{
                                public void foo(){
                                    Application x = null; 
                                    x.Suspending += (object sender, SuspendingEventArgs e) => {};
                                }

                                public static void Main(){
                                        var a = new abcdef();
                                        a.foo();
                                }
                            } ";

            CSharpCompilation comp = CreateWinRtCompilation(text);
            var cv = CompileAndVerifyOnWin8Only(comp, emitOptions: EmitOptions.RefEmitBug);

            var ExpectedIl = @"
{
  // Code size       61 (0x3d)
  .maxstack  4
  .locals init (Windows.UI.Xaml.Application V_0) //x
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  dup
  IL_0004:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken Windows.UI.Xaml.Application.Suspending.add""
  IL_000a:  newobj     ""System.Func<Windows.UI.Xaml.SuspendingEventHandler, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000f:  ldloc.0
  IL_0010:  dup
  IL_0011:  ldvirtftn  ""void Windows.UI.Xaml.Application.Suspending.remove""
  IL_0017:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_001c:  ldsfld     ""Windows.UI.Xaml.SuspendingEventHandler abcdef.CS$<>9__CachedAnonymousMethodDelegate1""
  IL_0021:  dup
  IL_0022:  brtrue.s   IL_0037
  IL_0024:  pop
  IL_0025:  ldnull
  IL_0026:  ldftn      ""void abcdef.<foo>b__0(object, object, Windows.ApplicationModel.SuspendingEventArgs)""
  IL_002c:  newobj     ""Windows.UI.Xaml.SuspendingEventHandler..ctor(object, System.IntPtr)""
  IL_0031:  dup
  IL_0032:  stsfld     ""Windows.UI.Xaml.SuspendingEventHandler abcdef.CS$<>9__CachedAnonymousMethodDelegate1""
  IL_0037:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<Windows.UI.Xaml.SuspendingEventHandler>(System.Func<Windows.UI.Xaml.SuspendingEventHandler, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, Windows.UI.Xaml.SuspendingEventHandler)""
  IL_003c:  ret
}
";

            cv.VerifyIL("abcdef.foo()", ExpectedIl);
        }

        /// <summary>
        /// Make sure that consuming a WinRT type event produces the expected
        /// IL output.
        /// </summary>
        [Fact]
        public void WinMdEventTest()
        {
            var text = @"
                            using Windows.UI.Xaml;
                            using Windows.ApplicationModel;
                            
                            public class abcdef{
                                private void OnSuspending(object sender, SuspendingEventArgs e)
                                {  
                                }

                                public void foo(){
                                    Application x = null; 
                                    x.Suspending += OnSuspending;
                                    x.Suspending -= OnSuspending;
                                }

                                public static void Main(){
                                        var a = new abcdef();
                                        a.foo();
                                }
                            } ";

            CSharpCompilation comp = CreateWinRtCompilation(text);
            var cv = CompileAndVerifyOnWin8Only(comp, emitOptions: EmitOptions.RefEmitBug);

            var ExpectedIl =
@"
  {
  // Code size       76 (0x4c)
  .maxstack  4
  .locals init (Windows.UI.Xaml.Application V_0) //x
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  dup
  IL_0004:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken Windows.UI.Xaml.Application.Suspending.add""
  IL_000a:  newobj     ""System.Func<Windows.UI.Xaml.SuspendingEventHandler, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000f:  ldloc.0
  IL_0010:  dup
  IL_0011:  ldvirtftn  ""void Windows.UI.Xaml.Application.Suspending.remove""
  IL_0017:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_001c:  ldarg.0
  IL_001d:  ldftn      ""void abcdef.OnSuspending(object, Windows.ApplicationModel.SuspendingEventArgs)""
  IL_0023:  newobj     ""Windows.UI.Xaml.SuspendingEventHandler..ctor(object, System.IntPtr)""
  IL_0028:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<Windows.UI.Xaml.SuspendingEventHandler>(System.Func<Windows.UI.Xaml.SuspendingEventHandler, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, Windows.UI.Xaml.SuspendingEventHandler)""
  IL_002d:  ldloc.0
  IL_002e:  dup
  IL_002f:  ldvirtftn  ""void Windows.UI.Xaml.Application.Suspending.remove""
  IL_0035:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_003a:  ldarg.0
  IL_003b:  ldftn      ""void abcdef.OnSuspending(object, Windows.ApplicationModel.SuspendingEventArgs)""
  IL_0041:  newobj     ""Windows.UI.Xaml.SuspendingEventHandler..ctor(object, System.IntPtr)""
  IL_0046:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<Windows.UI.Xaml.SuspendingEventHandler>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, Windows.UI.Xaml.SuspendingEventHandler)""
  IL_004b:  ret
}
";
            cv.VerifyIL("abcdef.foo()", ExpectedIl);
        }

        /// <summary>
        /// Make sure that consuming a WinRT type event produces a local when required.
        /// </summary>
        [Fact]
        public void WinMdEventTestLocalGeneration()
        {
            var text = @"
using Windows.UI.Xaml;
using Windows.ApplicationModel;
                            
public class abcdef{
    private void OnSuspending(object sender, SuspendingEventArgs e)
    {  
    }

    private Application getApplication(){return null;}

    public void foo(){
        getApplication().Suspending += OnSuspending;
        getApplication().Suspending -= OnSuspending;
    }

    public static void Main(){
            var a = new abcdef();
            a.foo();
    }
}";

            CSharpCompilation comp = CreateWinRtCompilation(text);
            var cv = CompileAndVerifyOnWin8Only(comp, emitOptions: EmitOptions.RefEmitBug);

            CSharpCompilation a = CreateWinRtCompilation(text);

            cv.VerifyIL("abcdef.foo()", @"
{
  // Code size       86 (0x56)
  .maxstack  4
  .locals init (Windows.UI.Xaml.Application V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""Windows.UI.Xaml.Application abcdef.getApplication()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  dup
  IL_0009:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken Windows.UI.Xaml.Application.Suspending.add""
  IL_000f:  newobj     ""System.Func<Windows.UI.Xaml.SuspendingEventHandler, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0014:  ldloc.0
  IL_0015:  dup
  IL_0016:  ldvirtftn  ""void Windows.UI.Xaml.Application.Suspending.remove""
  IL_001c:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0021:  ldarg.0
  IL_0022:  ldftn      ""void abcdef.OnSuspending(object, Windows.ApplicationModel.SuspendingEventArgs)""
  IL_0028:  newobj     ""Windows.UI.Xaml.SuspendingEventHandler..ctor(object, System.IntPtr)""
  IL_002d:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<Windows.UI.Xaml.SuspendingEventHandler>(System.Func<Windows.UI.Xaml.SuspendingEventHandler, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, Windows.UI.Xaml.SuspendingEventHandler)""
  IL_0032:  ldarg.0
  IL_0033:  call       ""Windows.UI.Xaml.Application abcdef.getApplication()""
  IL_0038:  dup
  IL_0039:  ldvirtftn  ""void Windows.UI.Xaml.Application.Suspending.remove""
  IL_003f:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0044:  ldarg.0
  IL_0045:  ldftn      ""void abcdef.OnSuspending(object, Windows.ApplicationModel.SuspendingEventArgs)""
  IL_004b:  newobj     ""Windows.UI.Xaml.SuspendingEventHandler..ctor(object, System.IntPtr)""
  IL_0050:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<Windows.UI.Xaml.SuspendingEventHandler>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, Windows.UI.Xaml.SuspendingEventHandler)""
  IL_0055:  ret
}");
        }

        /// <summary>
        /// Test to make sure that Windows.UI.Xaml.Application.Suspending is considered
        /// by the compiler to be a regular-looking event even though it is a WinRT event
        /// and returns an EventRegistrationToken.
        /// </summary>
        [Fact]
        public void VerifySignatures()
        {
            var text = "public class A{};";
            var comp = CreateWinRtCompilation(text);

            var winmdlib = comp.ExternalReferences[1];
            var winmdNS = comp.GetReferencedAssemblySymbol(winmdlib);

            var ns1 = comp.GlobalNamespace.GetMember<NamespaceSymbol>("Windows");
            ns1 = ns1.GetMember<NamespaceSymbol>("Foundation");
            var ert = ns1.GetMember<TypeSymbol>("EventRegistrationToken");

            var wns1 = winmdNS.GlobalNamespace.GetMember<NamespaceSymbol>("Windows");
            wns1 = wns1.GetMember<NamespaceSymbol>("UI");
            wns1 = wns1.GetMember<NamespaceSymbol>("Xaml");
            var itextrange = wns1.GetMember<PENamedTypeSymbol>("Application");
            var @event = itextrange.GetMember<PEEventSymbol>("Suspending");

            Assert.True(@event.IsWindowsRuntimeEvent, "Failed to detect winrt type event");
            Assert.True(!@event.MustCallMethodsDirectly, "Failed to override call methods directly");
        }

        [Fact]
        public void IsWindowsRuntimeEvent_EventSymbolSubtypes()
        {
            var il = @"
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
";

            var source = @"
class C : Interface<int>
{
    event Event Interface<int>.Normal 
    { 
        add { throw null; }
        remove { throw null; }
    }

    event Event Interface<int>.WinRT 
    { 
        add { throw null; }
        remove { throw null; }
    }
}
";

            var ilRef = CompileIL(il);
            var comp = CreateCompilation(source, WinRtRefs.Concat(new[] { ilRef }));
            comp.VerifyDiagnostics();

            var interfaceType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Interface");
            var interfaceNormalEvent = interfaceType.GetMember<EventSymbol>("Normal");
            var interfaceWinRTEvent = interfaceType.GetMember<EventSymbol>("WinRT");

            Assert.IsType<PEEventSymbol>(interfaceNormalEvent);
            Assert.IsType<PEEventSymbol>(interfaceWinRTEvent);

            // Only depends on accessor signatures - doesn't care if it's in a windowsruntime type.
            Assert.False(interfaceNormalEvent.IsWindowsRuntimeEvent);
            Assert.True(interfaceWinRTEvent.IsWindowsRuntimeEvent);

            var implementingType = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var implementingNormalEvent = implementingType.GetMembers().OfType<EventSymbol>().Single(e => e.Name.Contains("Normal"));
            var implementingWinRTEvent = implementingType.GetMembers().OfType<EventSymbol>().Single(e => e.Name.Contains("WinRT"));

            Assert.IsType<SourceCustomEventSymbol>(implementingNormalEvent);
            Assert.IsType<SourceCustomEventSymbol>(implementingWinRTEvent);
            
            // Based on kind of explicitly implemented interface event (other checks to be tested separately).
            Assert.False(implementingNormalEvent.IsWindowsRuntimeEvent);
            Assert.True(implementingWinRTEvent.IsWindowsRuntimeEvent);

            var subsitutedNormalEvent = implementingNormalEvent.ExplicitInterfaceImplementations.Single();
            var subsitutedWinRTEvent = implementingWinRTEvent.ExplicitInterfaceImplementations.Single();

            Assert.IsType<SubstitutedEventSymbol>(subsitutedNormalEvent);
            Assert.IsType<SubstitutedEventSymbol>(subsitutedWinRTEvent);

            // Based on original definition.
            Assert.False(subsitutedNormalEvent.IsWindowsRuntimeEvent);
            Assert.True(subsitutedWinRTEvent.IsWindowsRuntimeEvent);

            var retargetingAssembly = new RetargetingAssemblySymbol((SourceAssemblySymbol)comp.Assembly, isLinked: false);
            retargetingAssembly.SetCorLibrary(comp.Assembly.CorLibrary);

            var retargetingType = retargetingAssembly.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var retargetingNormalEvent = retargetingType.GetMembers().OfType<EventSymbol>().Single(e => e.Name.Contains("Normal"));
            var retargetingWinRTEvent = retargetingType.GetMembers().OfType<EventSymbol>().Single(e => e.Name.Contains("WinRT"));

            Assert.IsType<RetargetingEventSymbol>(retargetingNormalEvent);
            Assert.IsType<RetargetingEventSymbol>(retargetingWinRTEvent);

            // Based on underlying symbol.
            Assert.False(retargetingNormalEvent.IsWindowsRuntimeEvent);
            Assert.True(retargetingWinRTEvent.IsWindowsRuntimeEvent);
        }

        [Fact]
        public void IsWindowsRuntimeEvent_Source_OutputKind()
        {
            // OutputKind only matters when the event does not override or implement
            // (implicitly or explicitly) another event.
            var source = @"
class C
{
    event System.Action E 
    { 
        add { throw null; }
        remove { throw null; }
    }

    static void Main() { }
}

interface I
{
    event System.Action E;
}
";

            foreach (OutputKind kind in Enum.GetValues(typeof(OutputKind)))
            {
                var comp = CreateCompilation(source, WinRtRefs, new CSharpCompilationOptions(kind));
                comp.VerifyDiagnostics();

                var @class = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var classEvent = @class.GetMember<EventSymbol>("E");

                // Specifically test interfaces because they follow a different code path.
                var @interface = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("I");
                var interfaceEvent = @interface.GetMember<EventSymbol>("E");

                Assert.Equal(kind.IsWindowsRuntime(), classEvent.IsWindowsRuntimeEvent);
                Assert.Equal(kind.IsWindowsRuntime(), interfaceEvent.IsWindowsRuntimeEvent);
            }
        }

        [Fact]
        public void IsWindowsRuntimeEvent_Source_ImplicitImplementation()
        {
            // If an event implicitly implements an interface event on behalf
            // of the containing type (rather than a subtype), then IsWindowsRuntimeEvent
            // is copied from the interface event.
            // NOTE: The case where one source event implements multiple interface events
            // will be tested separately.
            var source = @"
class C : Interface
{
    public event System.Action Normal 
    { 
        add { throw null; }
        remove { throw null; }
    }

    public event System.Action WinRT 
    { 
        add { throw null; }
        remove { throw null; }
    }
}
";

            var ilRef = CompileIL(EventInterfaceIL);

            foreach (OutputKind kind in new[] { OutputKind.DynamicallyLinkedLibrary, OutputKind.WindowsRuntimeMetadata })
            {
                var comp = CreateCompilation(source, WinRtRefs.Concat(new[] { ilRef }), new CSharpCompilationOptions(kind));
                comp.VerifyDiagnostics();

                var @class = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var normalEvent = @class.GetMember<EventSymbol>("Normal");
                var winRTEvent = @class.GetMember<EventSymbol>("WinRT");

                Assert.False(normalEvent.IsWindowsRuntimeEvent);
                Assert.True(winRTEvent.IsWindowsRuntimeEvent);
            }
        }

        [Fact]
        public void IsWindowsRuntimeEvent_Source_Overriding()
        {
            // Overriding trumps output kind and implicit implementation.

            var source = @"
class OverrideNoImpl : Base
{
    public override event System.Action Normal
    { 
        add { throw null; }
        remove { throw null; }
    }

    public override event System.Action WinRT
    { 
        add { throw null; }
        remove { throw null; }
    }
}

class OverrideAndImplCorrectly : Base, Interface
{
    public override event System.Action Normal
    { 
        add { throw null; }
        remove { throw null; }
    }

    public override event System.Action WinRT
    { 
        add { throw null; }
        remove { throw null; }
    }
}

class OverrideAndImplIncorrectly : ReversedBase, Interface
{
    public override event System.Action Normal
    { 
        add { throw null; }
        remove { throw null; }
    }

    public override event System.Action WinRT
    { 
        add { throw null; }
        remove { throw null; }
    }
}
";

            var interfaceILRef = CompileIL(EventInterfaceIL);
            var baseILRef = CompileIL(EventBaseIL);

            foreach (OutputKind kind in new[] { OutputKind.DynamicallyLinkedLibrary, OutputKind.WindowsRuntimeMetadata })
            {
                var comp = CreateCompilation(source, WinRtRefs.Concat(new[] { interfaceILRef, baseILRef }), new CSharpCompilationOptions(kind));
                comp.VerifyDiagnostics(
                    // (40,41): error CS1991: 'OverrideAndImplIncorrectly.WinRT' cannot implement 'Interface.WinRT' because 'Interface.WinRT' is a Windows Runtime event and 'OverrideAndImplIncorrectly.WinRT' is a regular .NET event.
                    //     public override event System.Action WinRT
                    Diagnostic(ErrorCode.ERR_MixingWinRTEventWithRegular, "WinRT").WithArguments("OverrideAndImplIncorrectly.WinRT", "Interface.WinRT", "Interface.WinRT", "OverrideAndImplIncorrectly.WinRT"),
                    // (34,41): error CS1991: 'OverrideAndImplIncorrectly.Normal' cannot implement 'Interface.Normal' because 'OverrideAndImplIncorrectly.Normal' is a Windows Runtime event and 'Interface.Normal' is a regular .NET event.
                    //     public override event System.Action Normal
                    Diagnostic(ErrorCode.ERR_MixingWinRTEventWithRegular, "Normal").WithArguments("OverrideAndImplIncorrectly.Normal", "Interface.Normal", "OverrideAndImplIncorrectly.Normal", "Interface.Normal"));

                {
                    var overrideNoImplClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("OverrideNoImpl");
                    var normalEvent = overrideNoImplClass.GetMember<EventSymbol>("Normal");
                    var winRTEvent = overrideNoImplClass.GetMember<EventSymbol>("WinRT");

                    Assert.False(normalEvent.IsWindowsRuntimeEvent);
                    Assert.True(winRTEvent.IsWindowsRuntimeEvent);
                }

                {
                    var overrideAndImplCorrectlyClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("OverrideAndImplCorrectly");
                    var normalEvent = overrideAndImplCorrectlyClass.GetMember<EventSymbol>("Normal");
                    var winRTEvent = overrideAndImplCorrectlyClass.GetMember<EventSymbol>("WinRT");

                    Assert.False(normalEvent.IsWindowsRuntimeEvent);
                    Assert.True(winRTEvent.IsWindowsRuntimeEvent);
                }

                {
                    var overrideAndImplIncorrectlyClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("OverrideAndImplIncorrectly");
                    var normalEvent = overrideAndImplIncorrectlyClass.GetMember<EventSymbol>("Normal");
                    var winRTEvent = overrideAndImplIncorrectlyClass.GetMember<EventSymbol>("WinRT");

                    // NB: reversed
                    Assert.True(normalEvent.IsWindowsRuntimeEvent);
                    Assert.False(winRTEvent.IsWindowsRuntimeEvent);
                }
            }
        }

        [Fact]
        public void IsWindowsRuntimeEvent_Source_ExplicitImplementation()
        {
            // Explicit implementation trumps output kind.  It does not interact
            // with the rules for overriding or implicit implementation, because
            // an explicit implementation (in source) can do neither of those things.
            var source = @"
class C : Interface
{
    event System.Action Interface.Normal 
    { 
        add { throw null; }
        remove { throw null; }
    }

    event System.Action Interface.WinRT 
    { 
        add { throw null; }
        remove { throw null; }
    }
}
";

            var ilRef = CompileIL(EventInterfaceIL);

            foreach (OutputKind kind in new[] { OutputKind.DynamicallyLinkedLibrary, OutputKind.WindowsRuntimeMetadata })
            {
                var comp = CreateCompilation(source, WinRtRefs.Concat(new[] { ilRef }), new CSharpCompilationOptions(kind));
                comp.VerifyDiagnostics();

                var @class = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var normalEvent = @class.GetMembers().OfType<EventSymbol>().Single(e => e.Name.Contains("Normal"));
                var winRTEvent = @class.GetMembers().OfType<EventSymbol>().Single(e => e.Name.Contains("WinRT"));

                Assert.False(normalEvent.IsWindowsRuntimeEvent);
                Assert.True(winRTEvent.IsWindowsRuntimeEvent);
            }
        }

        [Fact]
        public void ERR_MixingWinRTEventWithRegular_TwoInterfaces()
        {
            var il = @"
.class interface public abstract auto ansi INormal
{
  .method public hidebysig newslot specialname abstract virtual 
          instance void  add_E(class [mscorlib]System.Action 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance void  remove_E(class [mscorlib]System.Action 'value') cil managed
  {
  }

  .event class [mscorlib]System.Action E
  {
    .addon instance void INormal::add_E(class [mscorlib]System.Action)
    .removeon instance void INormal::remove_E(class [mscorlib]System.Action)
  }

} // end of class INormal

.class interface public abstract auto ansi IWinRT
{
  .method public hidebysig newslot specialname abstract virtual 
          instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 
          add_E([in] class [mscorlib]System.Action 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual 
          instance void  remove_E([in] valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 'value') cil managed
  {
  }

  .event class [mscorlib]System.Action E
  {
    .addon instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken IWinRT::add_E(class [mscorlib]System.Action)
    .removeon instance void IWinRT::remove_E(valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
  }
} // end of class IWinRT
";

            // List WinRT interface first.
            {
                var source = @"
class C : IWinRT, INormal
{
    public event System.Action E 
    { 
        add { throw null; }
        remove { throw null; }
    }
}
";

                var ilRef = CompileIL(il);

                var comp = CreateCompilation(source, WinRtRefs.Concat(new[] {ilRef}));
                comp.VerifyDiagnostics(
                    // (4,32): error CS1991: 'C.E' cannot implement 'INormal.E' because 'C.E' is a Windows Runtime event and 'INormal.E' is a regular .NET event.
                    //     public event System.Action E 
                    Diagnostic(ErrorCode.ERR_MixingWinRTEventWithRegular, "E").WithArguments("C.E", "INormal.E", "C.E", "INormal.E"));

                var @class = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var @event = @class.GetMember<EventSymbol>("E");

                Assert.True(@event.IsWindowsRuntimeEvent); //Implemented at least one WinRT event.
            }

            // List normal interface first.
            {
                var source = @"
class C : INormal, IWinRT
{
    public event System.Action E 
    { 
        add { throw null; }
        remove { throw null; }
    }
}
";

                var ilRef = CompileIL(il);

                var comp = CreateCompilation(source, WinRtRefs.Concat(new[] { ilRef }));
                comp.VerifyDiagnostics(
                    // (4,32): error CS1991: 'C.E' cannot implement 'INormal.E' because 'C.E' is a Windows Runtime event and 'INormal.E' is a regular .NET event.
                    //     public event System.Action E 
                    Diagnostic(ErrorCode.ERR_MixingWinRTEventWithRegular, "E").WithArguments("C.E", "INormal.E", "C.E", "INormal.E"));

                var @class = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var @event = @class.GetMember<EventSymbol>("E");

                Assert.True(@event.IsWindowsRuntimeEvent); //Implemented at least one WinRT event.
            }
        }

        [WorkItem(547321, "DevDiv")]
        [Fact(Skip="547321")]
        public void ERR_MixingWinRTEventWithRegular_BaseTypeImplementsInterface()
        {
            var source = @"
class Derived : ReversedBase, Interface
{
}
";

            var interfaceILRef = CompileIL(EventInterfaceIL);
            var baseILRef = CompileIL(EventBaseIL);

            var comp = CreateCompilation(source, WinRtRefs.Concat(new[] { interfaceILRef, baseILRef }));
            comp.VerifyDiagnostics(
                // 53b0a0ee-4ca7-4106-89d3-972416f701c6.dll: error CS1991: 'ReversedBase.WinRT' cannot implement 'Interface.WinRT' because 'Interface.WinRT' is a Windows Runtime event and 'ReversedBase.WinRT' is a regular .NET event.
                Diagnostic(ErrorCode.ERR_MixingWinRTEventWithRegular).WithArguments("ReversedBase.WinRT", "Interface.WinRT", "Interface.WinRT", "ReversedBase.WinRT"),
                // 53b0a0ee-4ca7-4106-89d3-972416f701c6.dll: error CS1991: 'ReversedBase.Normal' cannot implement 'Interface.Normal' because 'ReversedBase.Normal' is a Windows Runtime event and 'Interface.Normal' is a regular .NET event.
                Diagnostic(ErrorCode.ERR_MixingWinRTEventWithRegular).WithArguments("ReversedBase.Normal", "Interface.Normal", "ReversedBase.Normal", "Interface.Normal"));
        }

        [Fact]
        public void ERR_MixingWinRTEventWithRegular_OverrideVsImplementation()
        {
            var source = @"
class Derived : ReversedBase, Interface
{
    public override event System.Action Normal
    { 
        add { throw null; }
        remove { throw null; }
    }

    public override event System.Action WinRT
    { 
        add { throw null; }
        remove { throw null; }
    }
}
";

            var interfaceILRef = CompileIL(EventInterfaceIL);
            var baseILRef = CompileIL(EventBaseIL);

            var comp = CreateCompilation(source, WinRtRefs.Concat(new[] { interfaceILRef, baseILRef }));
            // BREAK: dev11 doesn't catch these conflicts.
            comp.VerifyDiagnostics(
                // (10,41): error CS1991: 'Derived.WinRT' cannot implement 'Interface.WinRT' because 'Interface.WinRT' is a Windows Runtime event and 'Derived.WinRT' is a regular .NET event.
                //     public override event System.Action WinRT
                Diagnostic(ErrorCode.ERR_MixingWinRTEventWithRegular, "WinRT").WithArguments("Derived.WinRT", "Interface.WinRT", "Interface.WinRT", "Derived.WinRT"),
                // (4,41): error CS1991: 'Derived.Normal' cannot implement 'Interface.Normal' because 'Derived.Normal' is a Windows Runtime event and 'Interface.Normal' is a regular .NET event.
                //     public override event System.Action Normal
                Diagnostic(ErrorCode.ERR_MixingWinRTEventWithRegular, "Normal").WithArguments("Derived.Normal", "Interface.Normal", "Derived.Normal", "Interface.Normal"));

            var derivedClass = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var normalEvent = derivedClass.GetMember<EventSymbol>("Normal");
            var winRTEvent = derivedClass.GetMember<EventSymbol>("WinRT");

            // NB: reversed, since overriding beats implicitly implementing.
            Assert.True(normalEvent.IsWindowsRuntimeEvent);
            Assert.False(winRTEvent.IsWindowsRuntimeEvent);
        }

        [Fact]
        public void AccessorSignatures()
        {
            var source = @"
class C
{
    event System.Action E 
    { 
        add { throw null; }
        remove { throw null; }
    }

    event System.Action F;

    static void Main() { }
}
";

            foreach (OutputKind kind in new[] { OutputKind.DynamicallyLinkedLibrary, OutputKind.WindowsRuntimeMetadata })
            {
                var comp = CreateCompilation(source, WinRtRefs, new CSharpCompilationOptions(kind));
                comp.VerifyDiagnostics(
                    // (10,25): warning CS0067: The event 'C.F' is never used
                    //     event System.Action F;
                    Diagnostic(ErrorCode.WRN_UnreferencedEvent, "F").WithArguments("C.F"));

                var @class = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var customEvent = @class.GetMember<EventSymbol>("E");
                var fieldLikeEvent = @class.GetMember<EventSymbol>("F");

                if (kind.IsWindowsRuntime())
                {
                    VerifyWinRTEventShape(customEvent, comp);
                    VerifyWinRTEventShape(fieldLikeEvent, comp);
                }
                else
                {
                    VerifyNormalEventShape(customEvent, comp);
                    VerifyNormalEventShape(fieldLikeEvent, comp);
                }
            }
        }

        [Fact]
        public void MissingEventRegistrationTokenType()
        {
            var source = @"
class C
{
    event System.Action E;
}
";

            // NB: not referencing WinRtRefs
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseWinMD);
            comp.VerifyDiagnostics(
                // Add accessor signature:
                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"),

                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"),

                // Backing field type:
                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1"),

                // Uninteresting:
                // (4,25): warning CS0067: The event 'C.E' is never used
                //     event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E"));

            var @class = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var @event = @class.GetMember<EventSymbol>("E");
            var field = @event.AssociatedField;
            var fieldType = (NamedTypeSymbol)field.Type;
            Assert.Equal(TypeKind.Error, fieldType.TypeKind);
            Assert.Equal("EventRegistrationTokenTable", fieldType.Name);
            Assert.Equal(@event.Type, fieldType.TypeArguments.Single());
        }

        [Fact]
        public void EventAccess_RefKind()
        {
            var source = @"
class C
{
    public event System.Action Instance;
    public static event System.Action Static;

    void Test()
    {
        Ref(ref Instance);
        Out(out Instance);
        Ref(ref Static);
        Out(out Static);
    }

    void Ref(ref System.Action a)
    {
    }

    void Out(out System.Action a)
    {
        a = null;
    }
}
";
            CreateCompilationWithMscorlib(source, WinRtRefs, TestOptions.ReleaseWinMD).VerifyDiagnostics(
                // (9,17): error CS7084: A Windows Runtime event may not be passed as an out or ref parameter.
                //         Ref(ref Instance);
                Diagnostic(ErrorCode.ERR_WinRtEventPassedByRef, "Instance").WithArguments("C.Instance"),
                // (10,17): error CS7084: A Windows Runtime event may not be passed as an out or ref parameter.
                //         Out(out Instance);
                Diagnostic(ErrorCode.ERR_WinRtEventPassedByRef, "Instance").WithArguments("C.Instance"),
                // (11,17): error CS7084: A Windows Runtime event may not be passed as an out or ref parameter.
                //         Ref(ref Static);
                Diagnostic(ErrorCode.ERR_WinRtEventPassedByRef, "Static").WithArguments("C.Static"),
                // (12,17): error CS7084: A Windows Runtime event may not be passed as an out or ref parameter.
                //         Out(out Static);
                Diagnostic(ErrorCode.ERR_WinRtEventPassedByRef, "Static").WithArguments("C.Static"));
        }

        [Fact]
        public void EventAccess_MissingInvocationListAccessor()
        {
            var source = @"
class C
{
    public event System.Action E;

    void Test()
    {
        E();
    }
}

namespace System.Runtime.InteropServices.WindowsRuntime
{
    public struct EventRegistrationToken
    {
    }

    public class EventRegistrationTokenTable<T>
    {
        public static EventRegistrationTokenTable<T> GetOrCreateEventRegistrationTokenTable(ref EventRegistrationTokenTable<T> t)
        {
            return t;
        }

        public T InvocationList
        {
            set { }
        }
    }
}
";
            CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseWinMD).VerifyEmitDiagnostics(
                // (4,32): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1.AddEventHandler'
                //     public event System.Action E;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1", "AddEventHandler"),
                // (4,32): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1.RemoveEventHandler'
                //     public event System.Action E;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1", "RemoveEventHandler"),
                // (8,9): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<T>.get_InvocationList'
                //         E();
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<T>", "get_InvocationList"));
        }

        [Fact]
        public void CallMethodsDirectly_Static()
        {
            var il = @"
.class public auto ansi sealed beforefieldinit Events
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual final 
          instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 
          add_Instance(class [mscorlib]System.Action 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig newslot specialname virtual final 
          instance void  remove_Instance(valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname static 
          valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 
          add_Static([in] class [mscorlib]System.Action 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname static 
          void  remove_Static([in] valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 'value') cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  // Swapped removers in Instance and Static events.

  .event [mscorlib]System.Action Instance
  {
    .addon instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken Events::add_Instance(class [mscorlib]System.Action)
    .removeon void Events::remove_Static(valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
  }

  .event [mscorlib]System.Action Static
  {
    .addon valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken Events::add_Static(class [mscorlib]System.Action)
    .removeon instance void Events::remove_Instance(valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
  }
}
";

            var compilation = CreateCompilationWithCustomILSource("", il, WinRtRefs);

            var type = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Events");
            var instanceEvent = type.GetMember<EventSymbol>("Instance");
            var staticEvent = type.GetMember<EventSymbol>("Static");

            Assert.True(instanceEvent.IsWindowsRuntimeEvent);
            Assert.False(instanceEvent.AddMethod.IsStatic);
            Assert.True(instanceEvent.RemoveMethod.IsStatic);
            Assert.True(instanceEvent.MustCallMethodsDirectly);

            Assert.True(staticEvent.IsWindowsRuntimeEvent);
            Assert.True(staticEvent.AddMethod.IsStatic);
            Assert.False(staticEvent.RemoveMethod.IsStatic);
            Assert.True(staticEvent.MustCallMethodsDirectly);
        }

        private static void VerifyWinRTEventShape(EventSymbol @event, CSharpCompilation compilation)
        {
            Assert.True(@event.IsWindowsRuntimeEvent);

            var eventType = @event.Type;
            var tokenType = compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken);
            Assert.NotNull(tokenType);
            var voidType = compilation.GetSpecialType(SpecialType.System_Void);
            Assert.NotNull(voidType);

            var addMethod = @event.AddMethod;
            Assert.Equal(tokenType, addMethod.ReturnType);
            Assert.False(addMethod.ReturnsVoid);
            Assert.Equal(1, addMethod.ParameterCount);
            Assert.Equal(eventType, addMethod.ParameterTypes.Single());

            var removeMethod = @event.RemoveMethod;
            Assert.Equal(voidType, removeMethod.ReturnType);
            Assert.True(removeMethod.ReturnsVoid);
            Assert.Equal(1, removeMethod.ParameterCount);
            Assert.Equal(tokenType, removeMethod.ParameterTypes.Single());

            if (@event.HasAssociatedField)
            {
                var expectedFieldType = compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T).Construct(eventType);
                Assert.Equal(expectedFieldType, @event.AssociatedField.Type);
            }
            else
            {
                Assert.Null(@event.AssociatedField);
            }
        }

        private static void VerifyNormalEventShape(EventSymbol @event, CSharpCompilation compilation)
        {
            Assert.False(@event.IsWindowsRuntimeEvent);

            var eventType = @event.Type;
            var voidType = compilation.GetSpecialType(SpecialType.System_Void);
            Assert.NotNull(voidType);

            var addMethod = @event.AddMethod;
            Assert.Equal(voidType, addMethod.ReturnType);
            Assert.True(addMethod.ReturnsVoid);
            Assert.Equal(1, addMethod.ParameterCount);
            Assert.Equal(eventType, addMethod.ParameterTypes.Single());

            var removeMethod = @event.RemoveMethod;
            Assert.Equal(voidType, removeMethod.ReturnType);
            Assert.True(removeMethod.ReturnsVoid);
            Assert.Equal(1, removeMethod.ParameterCount);
            Assert.Equal(eventType, removeMethod.ParameterTypes.Single());

            if (@event.HasAssociatedField)
            {
                Assert.Equal(eventType, @event.AssociatedField.Type);
            }
            else
            {
                Assert.Null(@event.AssociatedField);
            }
        }
    }
}
