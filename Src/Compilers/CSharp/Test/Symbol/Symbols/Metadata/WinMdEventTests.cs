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

            var dynamicCommon = CreateCompilation(
                DynamicCommonSrc,
                references: new[] {
                    MscorlibRef_v4_0_30316_17626,
                    EventLibRef,
                },
                options: new CSharpCompilationOptions(OutputKind.NetModule, allowUnsafe: true));
            
            var dynamicCommonRef = dynamicCommon.EmitToImageReference(expectedWarnings: new[]
            {
                // (6,31): warning CS0067: The event 'A.d1' is never used
                //     public event voidDelegate d1;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "d1").WithArguments("A.d1"),
                // (8,34): warning CS0067: The event 'A.d3' is never used
                //     public event dynamicDelegate d3;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "d3").WithArguments("A.d3"),
                // (7,42): warning CS0067: The event 'A.d2' is never used
                //     public event genericDelegate<object> d2;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "d2").WithArguments("A.d2")
            });

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
  // Code size     6288 (0x1890)
  .maxstack  13
  .locals init (B V_0, //b
                EventLibrary.voidDelegate V_1, //test
                EventLibrary.genericDelegate<object> V_2, //generic
                EventLibrary.dynamicDelegate V_3, //dyn
                object V_4, //c
                A V_5,
                B V_6,
                EventLibrary.voidDelegate V_7,
                object V_8,
                EventLibrary.genericDelegate<object> V_9,
                EventLibrary.dynamicDelegate V_10)
  IL_0000:  newobj     ""A..ctor()""
  IL_0005:  newobj     ""B..ctor()""
  IL_000a:  stloc.0
  IL_000b:  ldsfld     ""EventLibrary.voidDelegate C.CS$<>9__CachedAnonymousMethodDelegate73""
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_0026
  IL_0013:  pop
  IL_0014:  ldnull
  IL_0015:  ldftn      ""void C.<Main>b__72(object)""
  IL_001b:  newobj     ""EventLibrary.voidDelegate..ctor(object, System.IntPtr)""
  IL_0020:  dup
  IL_0021:  stsfld     ""EventLibrary.voidDelegate C.CS$<>9__CachedAnonymousMethodDelegate73""
  IL_0026:  stloc.1
  IL_0027:  ldsfld     ""EventLibrary.genericDelegate<object> C.CS$<>9__CachedAnonymousMethodDelegate75""
  IL_002c:  dup
  IL_002d:  brtrue.s   IL_0042
  IL_002f:  pop
  IL_0030:  ldnull
  IL_0031:  ldftn      ""object C.<Main>b__74(object, object)""
  IL_0037:  newobj     ""EventLibrary.genericDelegate<object>..ctor(object, System.IntPtr)""
  IL_003c:  dup
  IL_003d:  stsfld     ""EventLibrary.genericDelegate<object> C.CS$<>9__CachedAnonymousMethodDelegate75""
  IL_0042:  stloc.2
  IL_0043:  ldsfld     ""EventLibrary.dynamicDelegate C.CS$<>9__CachedAnonymousMethodDelegate77""
  IL_0048:  dup
  IL_0049:  brtrue.s   IL_005e
  IL_004b:  pop
  IL_004c:  ldnull
  IL_004d:  ldftn      ""dynamic C.<Main>b__76(object, dynamic)""
  IL_0053:  newobj     ""EventLibrary.dynamicDelegate..ctor(object, System.IntPtr)""
  IL_0058:  dup
  IL_0059:  stsfld     ""EventLibrary.dynamicDelegate C.CS$<>9__CachedAnonymousMethodDelegate77""
  IL_005e:  stloc.3
  IL_005f:  dup
  IL_0060:  stloc.s    V_5
  IL_0062:  ldloc.s    V_5
  IL_0064:  dup
  IL_0065:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d1.add""
  IL_006b:  newobj     ""System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0070:  ldloc.s    V_5
  IL_0072:  dup
  IL_0073:  ldvirtftn  ""void A.d1.remove""
  IL_0079:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_007e:  ldloc.1
  IL_007f:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.voidDelegate>(System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0084:  dup
  IL_0085:  stloc.s    V_5
  IL_0087:  ldloc.s    V_5
  IL_0089:  dup
  IL_008a:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d2.add""
  IL_0090:  newobj     ""System.Func<EventLibrary.genericDelegate<object>, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0095:  ldloc.s    V_5
  IL_0097:  dup
  IL_0098:  ldvirtftn  ""void A.d2.remove""
  IL_009e:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00a3:  ldloc.2
  IL_00a4:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.genericDelegate<object>>(System.Func<EventLibrary.genericDelegate<object>, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.genericDelegate<object>)""
  IL_00a9:  dup
  IL_00aa:  stloc.s    V_5
  IL_00ac:  ldloc.s    V_5
  IL_00ae:  dup
  IL_00af:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d3.add""
  IL_00b5:  newobj     ""System.Func<EventLibrary.dynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00ba:  ldloc.s    V_5
  IL_00bc:  dup
  IL_00bd:  ldvirtftn  ""void A.d3.remove""
  IL_00c3:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00c8:  ldloc.3
  IL_00c9:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.dynamicDelegate>(System.Func<EventLibrary.dynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.dynamicDelegate)""
  IL_00ce:  dup
  IL_00cf:  dup
  IL_00d0:  ldvirtftn  ""void A.d1.remove""
  IL_00d6:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00db:  ldloc.1
  IL_00dc:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.voidDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_00e1:  dup
  IL_00e2:  dup
  IL_00e3:  ldvirtftn  ""void A.d2.remove""
  IL_00e9:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00ee:  ldloc.2
  IL_00ef:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.genericDelegate<object>>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.genericDelegate<object>)""
  IL_00f4:  dup
  IL_00f5:  dup
  IL_00f6:  ldvirtftn  ""void A.d3.remove""
  IL_00fc:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0101:  ldloc.3
  IL_0102:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.dynamicDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.dynamicDelegate)""
  IL_0107:  ldloc.0
  IL_0108:  stloc.s    V_6
  IL_010a:  ldloc.s    V_6
  IL_010c:  dup
  IL_010d:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken B.d1.add""
  IL_0113:  newobj     ""System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0118:  ldloc.s    V_6
  IL_011a:  dup
  IL_011b:  ldvirtftn  ""void B.d1.remove""
  IL_0121:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0126:  ldloc.1
  IL_0127:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.voidDelegate>(System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_012c:  ldloc.0
  IL_012d:  stloc.s    V_6
  IL_012f:  ldloc.s    V_6
  IL_0131:  dup
  IL_0132:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken B.d2.add""
  IL_0138:  newobj     ""System.Func<EventLibrary.genericDelegate<object>, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_013d:  ldloc.s    V_6
  IL_013f:  dup
  IL_0140:  ldvirtftn  ""void B.d2.remove""
  IL_0146:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_014b:  ldloc.2
  IL_014c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.genericDelegate<object>>(System.Func<EventLibrary.genericDelegate<object>, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.genericDelegate<object>)""
  IL_0151:  ldloc.0
  IL_0152:  stloc.s    V_6
  IL_0154:  ldloc.s    V_6
  IL_0156:  dup
  IL_0157:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken B.d3.add""
  IL_015d:  newobj     ""System.Func<EventLibrary.dynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0162:  ldloc.s    V_6
  IL_0164:  dup
  IL_0165:  ldvirtftn  ""void B.d3.remove""
  IL_016b:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0170:  ldloc.3
  IL_0171:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.dynamicDelegate>(System.Func<EventLibrary.dynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.dynamicDelegate)""
  IL_0176:  ldloc.0
  IL_0177:  dup
  IL_0178:  ldvirtftn  ""void B.d1.remove""
  IL_017e:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0183:  ldloc.1
  IL_0184:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.voidDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0189:  ldloc.0
  IL_018a:  dup
  IL_018b:  ldvirtftn  ""void B.d2.remove""
  IL_0191:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0196:  ldloc.2
  IL_0197:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.genericDelegate<object>>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.genericDelegate<object>)""
  IL_019c:  ldloc.0
  IL_019d:  dup
  IL_019e:  ldvirtftn  ""void B.d3.remove""
  IL_01a4:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_01a9:  ldloc.3
  IL_01aa:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.dynamicDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.dynamicDelegate)""
  IL_01af:  stloc.s    V_4
  IL_01b1:  ldloc.1
  IL_01b2:  stloc.s    V_7
  IL_01b4:  ldloc.s    V_4
  IL_01b6:  stloc.s    V_8
  IL_01b8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site5""
  IL_01bd:  brtrue.s   IL_01de
  IL_01bf:  ldc.i4.0
  IL_01c0:  ldstr      ""d1""
  IL_01c5:  ldtoken    ""C""
  IL_01ca:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01cf:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_01d4:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_01d9:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site5""
  IL_01de:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site5""
  IL_01e3:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_01e8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site5""
  IL_01ed:  ldloc.s    V_8
  IL_01ef:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_01f4:  brtrue     IL_02f5
  IL_01f9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site4""
  IL_01fe:  brtrue.s   IL_023d
  IL_0200:  ldc.i4     0x80
  IL_0205:  ldstr      ""d1""
  IL_020a:  ldtoken    ""C""
  IL_020f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0214:  ldc.i4.2
  IL_0215:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_021a:  dup
  IL_021b:  ldc.i4.0
  IL_021c:  ldc.i4.0
  IL_021d:  ldnull
  IL_021e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0223:  stelem.ref
  IL_0224:  dup
  IL_0225:  ldc.i4.1
  IL_0226:  ldc.i4.0
  IL_0227:  ldnull
  IL_0228:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_022d:  stelem.ref
  IL_022e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0233:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0238:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site4""
  IL_023d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site4""
  IL_0242:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0247:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site4""
  IL_024c:  ldloc.s    V_8
  IL_024e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site3""
  IL_0253:  brtrue.s   IL_028b
  IL_0255:  ldc.i4.0
  IL_0256:  ldc.i4.s   63
  IL_0258:  ldtoken    ""C""
  IL_025d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0262:  ldc.i4.2
  IL_0263:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0268:  dup
  IL_0269:  ldc.i4.0
  IL_026a:  ldc.i4.0
  IL_026b:  ldnull
  IL_026c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0271:  stelem.ref
  IL_0272:  dup
  IL_0273:  ldc.i4.1
  IL_0274:  ldc.i4.1
  IL_0275:  ldnull
  IL_0276:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_027b:  stelem.ref
  IL_027c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0281:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0286:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site3""
  IL_028b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site3""
  IL_0290:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0295:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site3""
  IL_029a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site2""
  IL_029f:  brtrue.s   IL_02d0
  IL_02a1:  ldc.i4.0
  IL_02a2:  ldstr      ""d1""
  IL_02a7:  ldtoken    ""C""
  IL_02ac:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_02b1:  ldc.i4.1
  IL_02b2:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_02b7:  dup
  IL_02b8:  ldc.i4.0
  IL_02b9:  ldc.i4.0
  IL_02ba:  ldnull
  IL_02bb:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_02c0:  stelem.ref
  IL_02c1:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_02c6:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_02cb:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site2""
  IL_02d0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site2""
  IL_02d5:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_02da:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site2""
  IL_02df:  ldloc.s    V_8
  IL_02e1:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_02e6:  ldloc.s    V_7
  IL_02e8:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_02ed:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_02f2:  pop
  IL_02f3:  br.s       IL_0353
  IL_02f5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site6""
  IL_02fa:  brtrue.s   IL_033a
  IL_02fc:  ldc.i4     0x104
  IL_0301:  ldstr      ""add_d1""
  IL_0306:  ldnull
  IL_0307:  ldtoken    ""C""
  IL_030c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0311:  ldc.i4.2
  IL_0312:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0317:  dup
  IL_0318:  ldc.i4.0
  IL_0319:  ldc.i4.0
  IL_031a:  ldnull
  IL_031b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0320:  stelem.ref
  IL_0321:  dup
  IL_0322:  ldc.i4.1
  IL_0323:  ldc.i4.1
  IL_0324:  ldnull
  IL_0325:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_032a:  stelem.ref
  IL_032b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0330:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0335:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site6""
  IL_033a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site6""
  IL_033f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0344:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site6""
  IL_0349:  ldloc.s    V_8
  IL_034b:  ldloc.s    V_7
  IL_034d:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0352:  pop
  IL_0353:  ldloc.2
  IL_0354:  stloc.s    V_9
  IL_0356:  ldloc.s    V_4
  IL_0358:  stloc.s    V_8
  IL_035a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site10""
  IL_035f:  brtrue.s   IL_0380
  IL_0361:  ldc.i4.0
  IL_0362:  ldstr      ""d2""
  IL_0367:  ldtoken    ""C""
  IL_036c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0371:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0376:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_037b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site10""
  IL_0380:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site10""
  IL_0385:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_038a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site10""
  IL_038f:  ldloc.s    V_8
  IL_0391:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0396:  brtrue     IL_0497
  IL_039b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site9""
  IL_03a0:  brtrue.s   IL_03df
  IL_03a2:  ldc.i4     0x80
  IL_03a7:  ldstr      ""d2""
  IL_03ac:  ldtoken    ""C""
  IL_03b1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_03b6:  ldc.i4.2
  IL_03b7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_03bc:  dup
  IL_03bd:  ldc.i4.0
  IL_03be:  ldc.i4.0
  IL_03bf:  ldnull
  IL_03c0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_03c5:  stelem.ref
  IL_03c6:  dup
  IL_03c7:  ldc.i4.1
  IL_03c8:  ldc.i4.0
  IL_03c9:  ldnull
  IL_03ca:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_03cf:  stelem.ref
  IL_03d0:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_03d5:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_03da:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site9""
  IL_03df:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site9""
  IL_03e4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_03e9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site9""
  IL_03ee:  ldloc.s    V_8
  IL_03f0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site8""
  IL_03f5:  brtrue.s   IL_042d
  IL_03f7:  ldc.i4.0
  IL_03f8:  ldc.i4.s   63
  IL_03fa:  ldtoken    ""C""
  IL_03ff:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0404:  ldc.i4.2
  IL_0405:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_040a:  dup
  IL_040b:  ldc.i4.0
  IL_040c:  ldc.i4.0
  IL_040d:  ldnull
  IL_040e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0413:  stelem.ref
  IL_0414:  dup
  IL_0415:  ldc.i4.1
  IL_0416:  ldc.i4.1
  IL_0417:  ldnull
  IL_0418:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_041d:  stelem.ref
  IL_041e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0423:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0428:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site8""
  IL_042d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site8""
  IL_0432:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0437:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site8""
  IL_043c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site7""
  IL_0441:  brtrue.s   IL_0472
  IL_0443:  ldc.i4.0
  IL_0444:  ldstr      ""d2""
  IL_0449:  ldtoken    ""C""
  IL_044e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0453:  ldc.i4.1
  IL_0454:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0459:  dup
  IL_045a:  ldc.i4.0
  IL_045b:  ldc.i4.0
  IL_045c:  ldnull
  IL_045d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0462:  stelem.ref
  IL_0463:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0468:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_046d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site7""
  IL_0472:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site7""
  IL_0477:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_047c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site7""
  IL_0481:  ldloc.s    V_8
  IL_0483:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0488:  ldloc.s    V_9
  IL_048a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_048f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0494:  pop
  IL_0495:  br.s       IL_04f5
  IL_0497:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site11""
  IL_049c:  brtrue.s   IL_04dc
  IL_049e:  ldc.i4     0x104
  IL_04a3:  ldstr      ""add_d2""
  IL_04a8:  ldnull
  IL_04a9:  ldtoken    ""C""
  IL_04ae:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_04b3:  ldc.i4.2
  IL_04b4:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_04b9:  dup
  IL_04ba:  ldc.i4.0
  IL_04bb:  ldc.i4.0
  IL_04bc:  ldnull
  IL_04bd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_04c2:  stelem.ref
  IL_04c3:  dup
  IL_04c4:  ldc.i4.1
  IL_04c5:  ldc.i4.1
  IL_04c6:  ldnull
  IL_04c7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_04cc:  stelem.ref
  IL_04cd:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_04d2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_04d7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site11""
  IL_04dc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site11""
  IL_04e1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_04e6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site11""
  IL_04eb:  ldloc.s    V_8
  IL_04ed:  ldloc.s    V_9
  IL_04ef:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_04f4:  pop
  IL_04f5:  ldloc.3
  IL_04f6:  stloc.s    V_10
  IL_04f8:  ldloc.s    V_4
  IL_04fa:  stloc.s    V_8
  IL_04fc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site15""
  IL_0501:  brtrue.s   IL_0522
  IL_0503:  ldc.i4.0
  IL_0504:  ldstr      ""d3""
  IL_0509:  ldtoken    ""C""
  IL_050e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0513:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0518:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_051d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site15""
  IL_0522:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site15""
  IL_0527:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_052c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site15""
  IL_0531:  ldloc.s    V_8
  IL_0533:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0538:  brtrue     IL_0639
  IL_053d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site14""
  IL_0542:  brtrue.s   IL_0581
  IL_0544:  ldc.i4     0x80
  IL_0549:  ldstr      ""d3""
  IL_054e:  ldtoken    ""C""
  IL_0553:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0558:  ldc.i4.2
  IL_0559:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_055e:  dup
  IL_055f:  ldc.i4.0
  IL_0560:  ldc.i4.0
  IL_0561:  ldnull
  IL_0562:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0567:  stelem.ref
  IL_0568:  dup
  IL_0569:  ldc.i4.1
  IL_056a:  ldc.i4.0
  IL_056b:  ldnull
  IL_056c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0571:  stelem.ref
  IL_0572:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0577:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_057c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site14""
  IL_0581:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site14""
  IL_0586:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_058b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site14""
  IL_0590:  ldloc.s    V_8
  IL_0592:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site13""
  IL_0597:  brtrue.s   IL_05cf
  IL_0599:  ldc.i4.0
  IL_059a:  ldc.i4.s   63
  IL_059c:  ldtoken    ""C""
  IL_05a1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_05a6:  ldc.i4.2
  IL_05a7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_05ac:  dup
  IL_05ad:  ldc.i4.0
  IL_05ae:  ldc.i4.0
  IL_05af:  ldnull
  IL_05b0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_05b5:  stelem.ref
  IL_05b6:  dup
  IL_05b7:  ldc.i4.1
  IL_05b8:  ldc.i4.1
  IL_05b9:  ldnull
  IL_05ba:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_05bf:  stelem.ref
  IL_05c0:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_05c5:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_05ca:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site13""
  IL_05cf:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site13""
  IL_05d4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_05d9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site13""
  IL_05de:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site12""
  IL_05e3:  brtrue.s   IL_0614
  IL_05e5:  ldc.i4.0
  IL_05e6:  ldstr      ""d3""
  IL_05eb:  ldtoken    ""C""
  IL_05f0:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_05f5:  ldc.i4.1
  IL_05f6:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_05fb:  dup
  IL_05fc:  ldc.i4.0
  IL_05fd:  ldc.i4.0
  IL_05fe:  ldnull
  IL_05ff:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0604:  stelem.ref
  IL_0605:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_060a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_060f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site12""
  IL_0614:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site12""
  IL_0619:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_061e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site12""
  IL_0623:  ldloc.s    V_8
  IL_0625:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_062a:  ldloc.s    V_10
  IL_062c:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0631:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0636:  pop
  IL_0637:  br.s       IL_0697
  IL_0639:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site16""
  IL_063e:  brtrue.s   IL_067e
  IL_0640:  ldc.i4     0x104
  IL_0645:  ldstr      ""add_d3""
  IL_064a:  ldnull
  IL_064b:  ldtoken    ""C""
  IL_0650:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0655:  ldc.i4.2
  IL_0656:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_065b:  dup
  IL_065c:  ldc.i4.0
  IL_065d:  ldc.i4.0
  IL_065e:  ldnull
  IL_065f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0664:  stelem.ref
  IL_0665:  dup
  IL_0666:  ldc.i4.1
  IL_0667:  ldc.i4.1
  IL_0668:  ldnull
  IL_0669:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_066e:  stelem.ref
  IL_066f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0674:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0679:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site16""
  IL_067e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site16""
  IL_0683:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_0688:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site16""
  IL_068d:  ldloc.s    V_8
  IL_068f:  ldloc.s    V_10
  IL_0691:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0696:  pop
  IL_0697:  ldloc.1
  IL_0698:  stloc.s    V_7
  IL_069a:  ldloc.s    V_4
  IL_069c:  stloc.s    V_8
  IL_069e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site20""
  IL_06a3:  brtrue.s   IL_06c4
  IL_06a5:  ldc.i4.0
  IL_06a6:  ldstr      ""d1""
  IL_06ab:  ldtoken    ""C""
  IL_06b0:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_06b5:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_06ba:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_06bf:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site20""
  IL_06c4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site20""
  IL_06c9:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_06ce:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site20""
  IL_06d3:  ldloc.s    V_8
  IL_06d5:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_06da:  brtrue     IL_07db
  IL_06df:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site19""
  IL_06e4:  brtrue.s   IL_0723
  IL_06e6:  ldc.i4     0x80
  IL_06eb:  ldstr      ""d1""
  IL_06f0:  ldtoken    ""C""
  IL_06f5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_06fa:  ldc.i4.2
  IL_06fb:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0700:  dup
  IL_0701:  ldc.i4.0
  IL_0702:  ldc.i4.0
  IL_0703:  ldnull
  IL_0704:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0709:  stelem.ref
  IL_070a:  dup
  IL_070b:  ldc.i4.1
  IL_070c:  ldc.i4.0
  IL_070d:  ldnull
  IL_070e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0713:  stelem.ref
  IL_0714:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0719:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_071e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site19""
  IL_0723:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site19""
  IL_0728:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_072d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site19""
  IL_0732:  ldloc.s    V_8
  IL_0734:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site18""
  IL_0739:  brtrue.s   IL_0771
  IL_073b:  ldc.i4.0
  IL_073c:  ldc.i4.s   73
  IL_073e:  ldtoken    ""C""
  IL_0743:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0748:  ldc.i4.2
  IL_0749:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_074e:  dup
  IL_074f:  ldc.i4.0
  IL_0750:  ldc.i4.0
  IL_0751:  ldnull
  IL_0752:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0757:  stelem.ref
  IL_0758:  dup
  IL_0759:  ldc.i4.1
  IL_075a:  ldc.i4.1
  IL_075b:  ldnull
  IL_075c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0761:  stelem.ref
  IL_0762:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0767:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_076c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site18""
  IL_0771:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site18""
  IL_0776:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_077b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site18""
  IL_0780:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site17""
  IL_0785:  brtrue.s   IL_07b6
  IL_0787:  ldc.i4.0
  IL_0788:  ldstr      ""d1""
  IL_078d:  ldtoken    ""C""
  IL_0792:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0797:  ldc.i4.1
  IL_0798:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_079d:  dup
  IL_079e:  ldc.i4.0
  IL_079f:  ldc.i4.0
  IL_07a0:  ldnull
  IL_07a1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_07a6:  stelem.ref
  IL_07a7:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_07ac:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_07b1:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site17""
  IL_07b6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site17""
  IL_07bb:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_07c0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site17""
  IL_07c5:  ldloc.s    V_8
  IL_07c7:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_07cc:  ldloc.s    V_7
  IL_07ce:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_07d3:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_07d8:  pop
  IL_07d9:  br.s       IL_0839
  IL_07db:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site21""
  IL_07e0:  brtrue.s   IL_0820
  IL_07e2:  ldc.i4     0x104
  IL_07e7:  ldstr      ""remove_d1""
  IL_07ec:  ldnull
  IL_07ed:  ldtoken    ""C""
  IL_07f2:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_07f7:  ldc.i4.2
  IL_07f8:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_07fd:  dup
  IL_07fe:  ldc.i4.0
  IL_07ff:  ldc.i4.0
  IL_0800:  ldnull
  IL_0801:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0806:  stelem.ref
  IL_0807:  dup
  IL_0808:  ldc.i4.1
  IL_0809:  ldc.i4.1
  IL_080a:  ldnull
  IL_080b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0810:  stelem.ref
  IL_0811:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0816:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_081b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site21""
  IL_0820:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site21""
  IL_0825:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_082a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site21""
  IL_082f:  ldloc.s    V_8
  IL_0831:  ldloc.s    V_7
  IL_0833:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0838:  pop
  IL_0839:  ldloc.2
  IL_083a:  stloc.s    V_9
  IL_083c:  ldloc.s    V_4
  IL_083e:  stloc.s    V_8
  IL_0840:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site25""
  IL_0845:  brtrue.s   IL_0866
  IL_0847:  ldc.i4.0
  IL_0848:  ldstr      ""d2""
  IL_084d:  ldtoken    ""C""
  IL_0852:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0857:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_085c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0861:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site25""
  IL_0866:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site25""
  IL_086b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0870:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site25""
  IL_0875:  ldloc.s    V_8
  IL_0877:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_087c:  brtrue     IL_097d
  IL_0881:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site24""
  IL_0886:  brtrue.s   IL_08c5
  IL_0888:  ldc.i4     0x80
  IL_088d:  ldstr      ""d2""
  IL_0892:  ldtoken    ""C""
  IL_0897:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_089c:  ldc.i4.2
  IL_089d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_08a2:  dup
  IL_08a3:  ldc.i4.0
  IL_08a4:  ldc.i4.0
  IL_08a5:  ldnull
  IL_08a6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_08ab:  stelem.ref
  IL_08ac:  dup
  IL_08ad:  ldc.i4.1
  IL_08ae:  ldc.i4.0
  IL_08af:  ldnull
  IL_08b0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_08b5:  stelem.ref
  IL_08b6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_08bb:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_08c0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site24""
  IL_08c5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site24""
  IL_08ca:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_08cf:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site24""
  IL_08d4:  ldloc.s    V_8
  IL_08d6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site23""
  IL_08db:  brtrue.s   IL_0913
  IL_08dd:  ldc.i4.0
  IL_08de:  ldc.i4.s   73
  IL_08e0:  ldtoken    ""C""
  IL_08e5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_08ea:  ldc.i4.2
  IL_08eb:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_08f0:  dup
  IL_08f1:  ldc.i4.0
  IL_08f2:  ldc.i4.0
  IL_08f3:  ldnull
  IL_08f4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_08f9:  stelem.ref
  IL_08fa:  dup
  IL_08fb:  ldc.i4.1
  IL_08fc:  ldc.i4.1
  IL_08fd:  ldnull
  IL_08fe:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0903:  stelem.ref
  IL_0904:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0909:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_090e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site23""
  IL_0913:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site23""
  IL_0918:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_091d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site23""
  IL_0922:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site22""
  IL_0927:  brtrue.s   IL_0958
  IL_0929:  ldc.i4.0
  IL_092a:  ldstr      ""d2""
  IL_092f:  ldtoken    ""C""
  IL_0934:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0939:  ldc.i4.1
  IL_093a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_093f:  dup
  IL_0940:  ldc.i4.0
  IL_0941:  ldc.i4.0
  IL_0942:  ldnull
  IL_0943:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0948:  stelem.ref
  IL_0949:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_094e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0953:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site22""
  IL_0958:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site22""
  IL_095d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0962:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site22""
  IL_0967:  ldloc.s    V_8
  IL_0969:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_096e:  ldloc.s    V_9
  IL_0970:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0975:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_097a:  pop
  IL_097b:  br.s       IL_09db
  IL_097d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site26""
  IL_0982:  brtrue.s   IL_09c2
  IL_0984:  ldc.i4     0x104
  IL_0989:  ldstr      ""remove_d2""
  IL_098e:  ldnull
  IL_098f:  ldtoken    ""C""
  IL_0994:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0999:  ldc.i4.2
  IL_099a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_099f:  dup
  IL_09a0:  ldc.i4.0
  IL_09a1:  ldc.i4.0
  IL_09a2:  ldnull
  IL_09a3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_09a8:  stelem.ref
  IL_09a9:  dup
  IL_09aa:  ldc.i4.1
  IL_09ab:  ldc.i4.1
  IL_09ac:  ldnull
  IL_09ad:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_09b2:  stelem.ref
  IL_09b3:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_09b8:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_09bd:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site26""
  IL_09c2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site26""
  IL_09c7:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_09cc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site26""
  IL_09d1:  ldloc.s    V_8
  IL_09d3:  ldloc.s    V_9
  IL_09d5:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_09da:  pop
  IL_09db:  ldloc.3
  IL_09dc:  stloc.s    V_10
  IL_09de:  ldloc.s    V_4
  IL_09e0:  stloc.s    V_8
  IL_09e2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site30""
  IL_09e7:  brtrue.s   IL_0a08
  IL_09e9:  ldc.i4.0
  IL_09ea:  ldstr      ""d3""
  IL_09ef:  ldtoken    ""C""
  IL_09f4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_09f9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_09fe:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0a03:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site30""
  IL_0a08:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site30""
  IL_0a0d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0a12:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site30""
  IL_0a17:  ldloc.s    V_8
  IL_0a19:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0a1e:  brtrue     IL_0b1f
  IL_0a23:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site29""
  IL_0a28:  brtrue.s   IL_0a67
  IL_0a2a:  ldc.i4     0x80
  IL_0a2f:  ldstr      ""d3""
  IL_0a34:  ldtoken    ""C""
  IL_0a39:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0a3e:  ldc.i4.2
  IL_0a3f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0a44:  dup
  IL_0a45:  ldc.i4.0
  IL_0a46:  ldc.i4.0
  IL_0a47:  ldnull
  IL_0a48:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0a4d:  stelem.ref
  IL_0a4e:  dup
  IL_0a4f:  ldc.i4.1
  IL_0a50:  ldc.i4.0
  IL_0a51:  ldnull
  IL_0a52:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0a57:  stelem.ref
  IL_0a58:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0a5d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0a62:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site29""
  IL_0a67:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site29""
  IL_0a6c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0a71:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site29""
  IL_0a76:  ldloc.s    V_8
  IL_0a78:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site28""
  IL_0a7d:  brtrue.s   IL_0ab5
  IL_0a7f:  ldc.i4.0
  IL_0a80:  ldc.i4.s   73
  IL_0a82:  ldtoken    ""C""
  IL_0a87:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0a8c:  ldc.i4.2
  IL_0a8d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0a92:  dup
  IL_0a93:  ldc.i4.0
  IL_0a94:  ldc.i4.0
  IL_0a95:  ldnull
  IL_0a96:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0a9b:  stelem.ref
  IL_0a9c:  dup
  IL_0a9d:  ldc.i4.1
  IL_0a9e:  ldc.i4.1
  IL_0a9f:  ldnull
  IL_0aa0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0aa5:  stelem.ref
  IL_0aa6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0aab:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0ab0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site28""
  IL_0ab5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site28""
  IL_0aba:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_0abf:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site28""
  IL_0ac4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site27""
  IL_0ac9:  brtrue.s   IL_0afa
  IL_0acb:  ldc.i4.0
  IL_0acc:  ldstr      ""d3""
  IL_0ad1:  ldtoken    ""C""
  IL_0ad6:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0adb:  ldc.i4.1
  IL_0adc:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0ae1:  dup
  IL_0ae2:  ldc.i4.0
  IL_0ae3:  ldc.i4.0
  IL_0ae4:  ldnull
  IL_0ae5:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0aea:  stelem.ref
  IL_0aeb:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0af0:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0af5:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site27""
  IL_0afa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site27""
  IL_0aff:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0b04:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site27""
  IL_0b09:  ldloc.s    V_8
  IL_0b0b:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0b10:  ldloc.s    V_10
  IL_0b12:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0b17:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0b1c:  pop
  IL_0b1d:  br.s       IL_0b7d
  IL_0b1f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site31""
  IL_0b24:  brtrue.s   IL_0b64
  IL_0b26:  ldc.i4     0x104
  IL_0b2b:  ldstr      ""remove_d3""
  IL_0b30:  ldnull
  IL_0b31:  ldtoken    ""C""
  IL_0b36:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0b3b:  ldc.i4.2
  IL_0b3c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0b41:  dup
  IL_0b42:  ldc.i4.0
  IL_0b43:  ldc.i4.0
  IL_0b44:  ldnull
  IL_0b45:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0b4a:  stelem.ref
  IL_0b4b:  dup
  IL_0b4c:  ldc.i4.1
  IL_0b4d:  ldc.i4.1
  IL_0b4e:  ldnull
  IL_0b4f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0b54:  stelem.ref
  IL_0b55:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0b5a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0b5f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site31""
  IL_0b64:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site31""
  IL_0b69:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_0b6e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site31""
  IL_0b73:  ldloc.s    V_8
  IL_0b75:  ldloc.s    V_10
  IL_0b77:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0b7c:  pop
  IL_0b7d:  ldloc.2
  IL_0b7e:  stloc.s    V_9
  IL_0b80:  ldloc.s    V_4
  IL_0b82:  stloc.s    V_8
  IL_0b84:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site35""
  IL_0b89:  brtrue.s   IL_0baa
  IL_0b8b:  ldc.i4.0
  IL_0b8c:  ldstr      ""d1""
  IL_0b91:  ldtoken    ""C""
  IL_0b96:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0b9b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0ba0:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0ba5:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site35""
  IL_0baa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site35""
  IL_0baf:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0bb4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site35""
  IL_0bb9:  ldloc.s    V_8
  IL_0bbb:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0bc0:  brtrue     IL_0cc1
  IL_0bc5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site34""
  IL_0bca:  brtrue.s   IL_0c09
  IL_0bcc:  ldc.i4     0x80
  IL_0bd1:  ldstr      ""d1""
  IL_0bd6:  ldtoken    ""C""
  IL_0bdb:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0be0:  ldc.i4.2
  IL_0be1:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0be6:  dup
  IL_0be7:  ldc.i4.0
  IL_0be8:  ldc.i4.0
  IL_0be9:  ldnull
  IL_0bea:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0bef:  stelem.ref
  IL_0bf0:  dup
  IL_0bf1:  ldc.i4.1
  IL_0bf2:  ldc.i4.0
  IL_0bf3:  ldnull
  IL_0bf4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0bf9:  stelem.ref
  IL_0bfa:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0bff:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0c04:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site34""
  IL_0c09:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site34""
  IL_0c0e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0c13:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site34""
  IL_0c18:  ldloc.s    V_8
  IL_0c1a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site33""
  IL_0c1f:  brtrue.s   IL_0c57
  IL_0c21:  ldc.i4.0
  IL_0c22:  ldc.i4.s   63
  IL_0c24:  ldtoken    ""C""
  IL_0c29:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0c2e:  ldc.i4.2
  IL_0c2f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0c34:  dup
  IL_0c35:  ldc.i4.0
  IL_0c36:  ldc.i4.0
  IL_0c37:  ldnull
  IL_0c38:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0c3d:  stelem.ref
  IL_0c3e:  dup
  IL_0c3f:  ldc.i4.1
  IL_0c40:  ldc.i4.1
  IL_0c41:  ldnull
  IL_0c42:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0c47:  stelem.ref
  IL_0c48:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0c4d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0c52:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site33""
  IL_0c57:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site33""
  IL_0c5c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0c61:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site33""
  IL_0c66:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site32""
  IL_0c6b:  brtrue.s   IL_0c9c
  IL_0c6d:  ldc.i4.0
  IL_0c6e:  ldstr      ""d1""
  IL_0c73:  ldtoken    ""C""
  IL_0c78:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0c7d:  ldc.i4.1
  IL_0c7e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0c83:  dup
  IL_0c84:  ldc.i4.0
  IL_0c85:  ldc.i4.0
  IL_0c86:  ldnull
  IL_0c87:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0c8c:  stelem.ref
  IL_0c8d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0c92:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0c97:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site32""
  IL_0c9c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site32""
  IL_0ca1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0ca6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site32""
  IL_0cab:  ldloc.s    V_8
  IL_0cad:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0cb2:  ldloc.s    V_9
  IL_0cb4:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0cb9:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0cbe:  pop
  IL_0cbf:  br.s       IL_0d1f
  IL_0cc1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site36""
  IL_0cc6:  brtrue.s   IL_0d06
  IL_0cc8:  ldc.i4     0x104
  IL_0ccd:  ldstr      ""add_d1""
  IL_0cd2:  ldnull
  IL_0cd3:  ldtoken    ""C""
  IL_0cd8:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0cdd:  ldc.i4.2
  IL_0cde:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0ce3:  dup
  IL_0ce4:  ldc.i4.0
  IL_0ce5:  ldc.i4.0
  IL_0ce6:  ldnull
  IL_0ce7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0cec:  stelem.ref
  IL_0ced:  dup
  IL_0cee:  ldc.i4.1
  IL_0cef:  ldc.i4.1
  IL_0cf0:  ldnull
  IL_0cf1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0cf6:  stelem.ref
  IL_0cf7:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0cfc:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0d01:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site36""
  IL_0d06:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site36""
  IL_0d0b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0d10:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site36""
  IL_0d15:  ldloc.s    V_8
  IL_0d17:  ldloc.s    V_9
  IL_0d19:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0d1e:  pop
  IL_0d1f:  ldloc.0
  IL_0d20:  stloc.s    V_4
  IL_0d22:  ldloc.1
  IL_0d23:  stloc.s    V_7
  IL_0d25:  ldloc.s    V_4
  IL_0d27:  stloc.s    V_8
  IL_0d29:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site40""
  IL_0d2e:  brtrue.s   IL_0d4f
  IL_0d30:  ldc.i4.0
  IL_0d31:  ldstr      ""d1""
  IL_0d36:  ldtoken    ""C""
  IL_0d3b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0d40:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0d45:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0d4a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site40""
  IL_0d4f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site40""
  IL_0d54:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0d59:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site40""
  IL_0d5e:  ldloc.s    V_8
  IL_0d60:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0d65:  brtrue     IL_0e66
  IL_0d6a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site39""
  IL_0d6f:  brtrue.s   IL_0dae
  IL_0d71:  ldc.i4     0x80
  IL_0d76:  ldstr      ""d1""
  IL_0d7b:  ldtoken    ""C""
  IL_0d80:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0d85:  ldc.i4.2
  IL_0d86:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0d8b:  dup
  IL_0d8c:  ldc.i4.0
  IL_0d8d:  ldc.i4.0
  IL_0d8e:  ldnull
  IL_0d8f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0d94:  stelem.ref
  IL_0d95:  dup
  IL_0d96:  ldc.i4.1
  IL_0d97:  ldc.i4.0
  IL_0d98:  ldnull
  IL_0d99:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0d9e:  stelem.ref
  IL_0d9f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0da4:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0da9:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site39""
  IL_0dae:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site39""
  IL_0db3:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0db8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site39""
  IL_0dbd:  ldloc.s    V_8
  IL_0dbf:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site38""
  IL_0dc4:  brtrue.s   IL_0dfc
  IL_0dc6:  ldc.i4.0
  IL_0dc7:  ldc.i4.s   63
  IL_0dc9:  ldtoken    ""C""
  IL_0dce:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0dd3:  ldc.i4.2
  IL_0dd4:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0dd9:  dup
  IL_0dda:  ldc.i4.0
  IL_0ddb:  ldc.i4.0
  IL_0ddc:  ldnull
  IL_0ddd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0de2:  stelem.ref
  IL_0de3:  dup
  IL_0de4:  ldc.i4.1
  IL_0de5:  ldc.i4.1
  IL_0de6:  ldnull
  IL_0de7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0dec:  stelem.ref
  IL_0ded:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0df2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0df7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site38""
  IL_0dfc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site38""
  IL_0e01:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0e06:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site38""
  IL_0e0b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site37""
  IL_0e10:  brtrue.s   IL_0e41
  IL_0e12:  ldc.i4.0
  IL_0e13:  ldstr      ""d1""
  IL_0e18:  ldtoken    ""C""
  IL_0e1d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0e22:  ldc.i4.1
  IL_0e23:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0e28:  dup
  IL_0e29:  ldc.i4.0
  IL_0e2a:  ldc.i4.0
  IL_0e2b:  ldnull
  IL_0e2c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0e31:  stelem.ref
  IL_0e32:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0e37:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0e3c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site37""
  IL_0e41:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site37""
  IL_0e46:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0e4b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site37""
  IL_0e50:  ldloc.s    V_8
  IL_0e52:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0e57:  ldloc.s    V_7
  IL_0e59:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0e5e:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0e63:  pop
  IL_0e64:  br.s       IL_0ec4
  IL_0e66:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site41""
  IL_0e6b:  brtrue.s   IL_0eab
  IL_0e6d:  ldc.i4     0x104
  IL_0e72:  ldstr      ""add_d1""
  IL_0e77:  ldnull
  IL_0e78:  ldtoken    ""C""
  IL_0e7d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0e82:  ldc.i4.2
  IL_0e83:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0e88:  dup
  IL_0e89:  ldc.i4.0
  IL_0e8a:  ldc.i4.0
  IL_0e8b:  ldnull
  IL_0e8c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0e91:  stelem.ref
  IL_0e92:  dup
  IL_0e93:  ldc.i4.1
  IL_0e94:  ldc.i4.1
  IL_0e95:  ldnull
  IL_0e96:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0e9b:  stelem.ref
  IL_0e9c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0ea1:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0ea6:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site41""
  IL_0eab:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site41""
  IL_0eb0:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0eb5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site41""
  IL_0eba:  ldloc.s    V_8
  IL_0ebc:  ldloc.s    V_7
  IL_0ebe:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0ec3:  pop
  IL_0ec4:  ldloc.2
  IL_0ec5:  stloc.s    V_9
  IL_0ec7:  ldloc.s    V_4
  IL_0ec9:  stloc.s    V_8
  IL_0ecb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site45""
  IL_0ed0:  brtrue.s   IL_0ef1
  IL_0ed2:  ldc.i4.0
  IL_0ed3:  ldstr      ""d2""
  IL_0ed8:  ldtoken    ""C""
  IL_0edd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0ee2:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0ee7:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0eec:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site45""
  IL_0ef1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site45""
  IL_0ef6:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0efb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site45""
  IL_0f00:  ldloc.s    V_8
  IL_0f02:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0f07:  brtrue     IL_1008
  IL_0f0c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site44""
  IL_0f11:  brtrue.s   IL_0f50
  IL_0f13:  ldc.i4     0x80
  IL_0f18:  ldstr      ""d2""
  IL_0f1d:  ldtoken    ""C""
  IL_0f22:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0f27:  ldc.i4.2
  IL_0f28:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0f2d:  dup
  IL_0f2e:  ldc.i4.0
  IL_0f2f:  ldc.i4.0
  IL_0f30:  ldnull
  IL_0f31:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f36:  stelem.ref
  IL_0f37:  dup
  IL_0f38:  ldc.i4.1
  IL_0f39:  ldc.i4.0
  IL_0f3a:  ldnull
  IL_0f3b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f40:  stelem.ref
  IL_0f41:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0f46:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0f4b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site44""
  IL_0f50:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site44""
  IL_0f55:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0f5a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site44""
  IL_0f5f:  ldloc.s    V_8
  IL_0f61:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site43""
  IL_0f66:  brtrue.s   IL_0f9e
  IL_0f68:  ldc.i4.0
  IL_0f69:  ldc.i4.s   63
  IL_0f6b:  ldtoken    ""C""
  IL_0f70:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0f75:  ldc.i4.2
  IL_0f76:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0f7b:  dup
  IL_0f7c:  ldc.i4.0
  IL_0f7d:  ldc.i4.0
  IL_0f7e:  ldnull
  IL_0f7f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f84:  stelem.ref
  IL_0f85:  dup
  IL_0f86:  ldc.i4.1
  IL_0f87:  ldc.i4.1
  IL_0f88:  ldnull
  IL_0f89:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f8e:  stelem.ref
  IL_0f8f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0f94:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0f99:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site43""
  IL_0f9e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site43""
  IL_0fa3:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0fa8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site43""
  IL_0fad:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site42""
  IL_0fb2:  brtrue.s   IL_0fe3
  IL_0fb4:  ldc.i4.0
  IL_0fb5:  ldstr      ""d2""
  IL_0fba:  ldtoken    ""C""
  IL_0fbf:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0fc4:  ldc.i4.1
  IL_0fc5:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0fca:  dup
  IL_0fcb:  ldc.i4.0
  IL_0fcc:  ldc.i4.0
  IL_0fcd:  ldnull
  IL_0fce:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0fd3:  stelem.ref
  IL_0fd4:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0fd9:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0fde:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site42""
  IL_0fe3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site42""
  IL_0fe8:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0fed:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site42""
  IL_0ff2:  ldloc.s    V_8
  IL_0ff4:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0ff9:  ldloc.s    V_9
  IL_0ffb:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_1000:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_1005:  pop
  IL_1006:  br.s       IL_1066
  IL_1008:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site46""
  IL_100d:  brtrue.s   IL_104d
  IL_100f:  ldc.i4     0x104
  IL_1014:  ldstr      ""add_d2""
  IL_1019:  ldnull
  IL_101a:  ldtoken    ""C""
  IL_101f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1024:  ldc.i4.2
  IL_1025:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_102a:  dup
  IL_102b:  ldc.i4.0
  IL_102c:  ldc.i4.0
  IL_102d:  ldnull
  IL_102e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1033:  stelem.ref
  IL_1034:  dup
  IL_1035:  ldc.i4.1
  IL_1036:  ldc.i4.1
  IL_1037:  ldnull
  IL_1038:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_103d:  stelem.ref
  IL_103e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1043:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1048:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site46""
  IL_104d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site46""
  IL_1052:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_1057:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site46""
  IL_105c:  ldloc.s    V_8
  IL_105e:  ldloc.s    V_9
  IL_1060:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_1065:  pop
  IL_1066:  ldloc.3
  IL_1067:  stloc.s    V_10
  IL_1069:  ldloc.s    V_4
  IL_106b:  stloc.s    V_8
  IL_106d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site50""
  IL_1072:  brtrue.s   IL_1093
  IL_1074:  ldc.i4.0
  IL_1075:  ldstr      ""d3""
  IL_107a:  ldtoken    ""C""
  IL_107f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1084:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_1089:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_108e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site50""
  IL_1093:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site50""
  IL_1098:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_109d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site50""
  IL_10a2:  ldloc.s    V_8
  IL_10a4:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_10a9:  brtrue     IL_11aa
  IL_10ae:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site49""
  IL_10b3:  brtrue.s   IL_10f2
  IL_10b5:  ldc.i4     0x80
  IL_10ba:  ldstr      ""d3""
  IL_10bf:  ldtoken    ""C""
  IL_10c4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_10c9:  ldc.i4.2
  IL_10ca:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_10cf:  dup
  IL_10d0:  ldc.i4.0
  IL_10d1:  ldc.i4.0
  IL_10d2:  ldnull
  IL_10d3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_10d8:  stelem.ref
  IL_10d9:  dup
  IL_10da:  ldc.i4.1
  IL_10db:  ldc.i4.0
  IL_10dc:  ldnull
  IL_10dd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_10e2:  stelem.ref
  IL_10e3:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_10e8:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_10ed:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site49""
  IL_10f2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site49""
  IL_10f7:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_10fc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site49""
  IL_1101:  ldloc.s    V_8
  IL_1103:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site48""
  IL_1108:  brtrue.s   IL_1140
  IL_110a:  ldc.i4.0
  IL_110b:  ldc.i4.s   63
  IL_110d:  ldtoken    ""C""
  IL_1112:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1117:  ldc.i4.2
  IL_1118:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_111d:  dup
  IL_111e:  ldc.i4.0
  IL_111f:  ldc.i4.0
  IL_1120:  ldnull
  IL_1121:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1126:  stelem.ref
  IL_1127:  dup
  IL_1128:  ldc.i4.1
  IL_1129:  ldc.i4.1
  IL_112a:  ldnull
  IL_112b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1130:  stelem.ref
  IL_1131:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1136:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_113b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site48""
  IL_1140:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site48""
  IL_1145:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_114a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site48""
  IL_114f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site47""
  IL_1154:  brtrue.s   IL_1185
  IL_1156:  ldc.i4.0
  IL_1157:  ldstr      ""d3""
  IL_115c:  ldtoken    ""C""
  IL_1161:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1166:  ldc.i4.1
  IL_1167:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_116c:  dup
  IL_116d:  ldc.i4.0
  IL_116e:  ldc.i4.0
  IL_116f:  ldnull
  IL_1170:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1175:  stelem.ref
  IL_1176:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_117b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1180:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site47""
  IL_1185:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site47""
  IL_118a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_118f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site47""
  IL_1194:  ldloc.s    V_8
  IL_1196:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_119b:  ldloc.s    V_10
  IL_119d:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_11a2:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_11a7:  pop
  IL_11a8:  br.s       IL_1208
  IL_11aa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site51""
  IL_11af:  brtrue.s   IL_11ef
  IL_11b1:  ldc.i4     0x104
  IL_11b6:  ldstr      ""add_d3""
  IL_11bb:  ldnull
  IL_11bc:  ldtoken    ""C""
  IL_11c1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_11c6:  ldc.i4.2
  IL_11c7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_11cc:  dup
  IL_11cd:  ldc.i4.0
  IL_11ce:  ldc.i4.0
  IL_11cf:  ldnull
  IL_11d0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_11d5:  stelem.ref
  IL_11d6:  dup
  IL_11d7:  ldc.i4.1
  IL_11d8:  ldc.i4.1
  IL_11d9:  ldnull
  IL_11da:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_11df:  stelem.ref
  IL_11e0:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_11e5:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_11ea:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site51""
  IL_11ef:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site51""
  IL_11f4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_11f9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site51""
  IL_11fe:  ldloc.s    V_8
  IL_1200:  ldloc.s    V_10
  IL_1202:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_1207:  pop
  IL_1208:  ldloc.1
  IL_1209:  stloc.s    V_7
  IL_120b:  ldloc.s    V_4
  IL_120d:  stloc.s    V_8
  IL_120f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site55""
  IL_1214:  brtrue.s   IL_1235
  IL_1216:  ldc.i4.0
  IL_1217:  ldstr      ""d1""
  IL_121c:  ldtoken    ""C""
  IL_1221:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1226:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_122b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1230:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site55""
  IL_1235:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site55""
  IL_123a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_123f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site55""
  IL_1244:  ldloc.s    V_8
  IL_1246:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_124b:  brtrue     IL_134c
  IL_1250:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site54""
  IL_1255:  brtrue.s   IL_1294
  IL_1257:  ldc.i4     0x80
  IL_125c:  ldstr      ""d1""
  IL_1261:  ldtoken    ""C""
  IL_1266:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_126b:  ldc.i4.2
  IL_126c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1271:  dup
  IL_1272:  ldc.i4.0
  IL_1273:  ldc.i4.0
  IL_1274:  ldnull
  IL_1275:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_127a:  stelem.ref
  IL_127b:  dup
  IL_127c:  ldc.i4.1
  IL_127d:  ldc.i4.0
  IL_127e:  ldnull
  IL_127f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1284:  stelem.ref
  IL_1285:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_128a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_128f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site54""
  IL_1294:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site54""
  IL_1299:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_129e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site54""
  IL_12a3:  ldloc.s    V_8
  IL_12a5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site53""
  IL_12aa:  brtrue.s   IL_12e2
  IL_12ac:  ldc.i4.0
  IL_12ad:  ldc.i4.s   73
  IL_12af:  ldtoken    ""C""
  IL_12b4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_12b9:  ldc.i4.2
  IL_12ba:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_12bf:  dup
  IL_12c0:  ldc.i4.0
  IL_12c1:  ldc.i4.0
  IL_12c2:  ldnull
  IL_12c3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_12c8:  stelem.ref
  IL_12c9:  dup
  IL_12ca:  ldc.i4.1
  IL_12cb:  ldc.i4.1
  IL_12cc:  ldnull
  IL_12cd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_12d2:  stelem.ref
  IL_12d3:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_12d8:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_12dd:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site53""
  IL_12e2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site53""
  IL_12e7:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_12ec:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site53""
  IL_12f1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site52""
  IL_12f6:  brtrue.s   IL_1327
  IL_12f8:  ldc.i4.0
  IL_12f9:  ldstr      ""d1""
  IL_12fe:  ldtoken    ""C""
  IL_1303:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1308:  ldc.i4.1
  IL_1309:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_130e:  dup
  IL_130f:  ldc.i4.0
  IL_1310:  ldc.i4.0
  IL_1311:  ldnull
  IL_1312:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1317:  stelem.ref
  IL_1318:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_131d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1322:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site52""
  IL_1327:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site52""
  IL_132c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_1331:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site52""
  IL_1336:  ldloc.s    V_8
  IL_1338:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_133d:  ldloc.s    V_7
  IL_133f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_1344:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_1349:  pop
  IL_134a:  br.s       IL_13aa
  IL_134c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site56""
  IL_1351:  brtrue.s   IL_1391
  IL_1353:  ldc.i4     0x104
  IL_1358:  ldstr      ""remove_d1""
  IL_135d:  ldnull
  IL_135e:  ldtoken    ""C""
  IL_1363:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1368:  ldc.i4.2
  IL_1369:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_136e:  dup
  IL_136f:  ldc.i4.0
  IL_1370:  ldc.i4.0
  IL_1371:  ldnull
  IL_1372:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1377:  stelem.ref
  IL_1378:  dup
  IL_1379:  ldc.i4.1
  IL_137a:  ldc.i4.1
  IL_137b:  ldnull
  IL_137c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1381:  stelem.ref
  IL_1382:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1387:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_138c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site56""
  IL_1391:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site56""
  IL_1396:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_139b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site56""
  IL_13a0:  ldloc.s    V_8
  IL_13a2:  ldloc.s    V_7
  IL_13a4:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_13a9:  pop
  IL_13aa:  ldloc.2
  IL_13ab:  stloc.s    V_9
  IL_13ad:  ldloc.s    V_4
  IL_13af:  stloc.s    V_8
  IL_13b1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site60""
  IL_13b6:  brtrue.s   IL_13d7
  IL_13b8:  ldc.i4.0
  IL_13b9:  ldstr      ""d2""
  IL_13be:  ldtoken    ""C""
  IL_13c3:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_13c8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_13cd:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_13d2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site60""
  IL_13d7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site60""
  IL_13dc:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_13e1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site60""
  IL_13e6:  ldloc.s    V_8
  IL_13e8:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_13ed:  brtrue     IL_14ee
  IL_13f2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site59""
  IL_13f7:  brtrue.s   IL_1436
  IL_13f9:  ldc.i4     0x80
  IL_13fe:  ldstr      ""d2""
  IL_1403:  ldtoken    ""C""
  IL_1408:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_140d:  ldc.i4.2
  IL_140e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1413:  dup
  IL_1414:  ldc.i4.0
  IL_1415:  ldc.i4.0
  IL_1416:  ldnull
  IL_1417:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_141c:  stelem.ref
  IL_141d:  dup
  IL_141e:  ldc.i4.1
  IL_141f:  ldc.i4.0
  IL_1420:  ldnull
  IL_1421:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1426:  stelem.ref
  IL_1427:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_142c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1431:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site59""
  IL_1436:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site59""
  IL_143b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_1440:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site59""
  IL_1445:  ldloc.s    V_8
  IL_1447:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site58""
  IL_144c:  brtrue.s   IL_1484
  IL_144e:  ldc.i4.0
  IL_144f:  ldc.i4.s   73
  IL_1451:  ldtoken    ""C""
  IL_1456:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_145b:  ldc.i4.2
  IL_145c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1461:  dup
  IL_1462:  ldc.i4.0
  IL_1463:  ldc.i4.0
  IL_1464:  ldnull
  IL_1465:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_146a:  stelem.ref
  IL_146b:  dup
  IL_146c:  ldc.i4.1
  IL_146d:  ldc.i4.1
  IL_146e:  ldnull
  IL_146f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1474:  stelem.ref
  IL_1475:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_147a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_147f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site58""
  IL_1484:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site58""
  IL_1489:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_148e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site58""
  IL_1493:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site57""
  IL_1498:  brtrue.s   IL_14c9
  IL_149a:  ldc.i4.0
  IL_149b:  ldstr      ""d2""
  IL_14a0:  ldtoken    ""C""
  IL_14a5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_14aa:  ldc.i4.1
  IL_14ab:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_14b0:  dup
  IL_14b1:  ldc.i4.0
  IL_14b2:  ldc.i4.0
  IL_14b3:  ldnull
  IL_14b4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_14b9:  stelem.ref
  IL_14ba:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_14bf:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_14c4:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site57""
  IL_14c9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site57""
  IL_14ce:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_14d3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site57""
  IL_14d8:  ldloc.s    V_8
  IL_14da:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_14df:  ldloc.s    V_9
  IL_14e1:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_14e6:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_14eb:  pop
  IL_14ec:  br.s       IL_154c
  IL_14ee:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site61""
  IL_14f3:  brtrue.s   IL_1533
  IL_14f5:  ldc.i4     0x104
  IL_14fa:  ldstr      ""remove_d2""
  IL_14ff:  ldnull
  IL_1500:  ldtoken    ""C""
  IL_1505:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_150a:  ldc.i4.2
  IL_150b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1510:  dup
  IL_1511:  ldc.i4.0
  IL_1512:  ldc.i4.0
  IL_1513:  ldnull
  IL_1514:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1519:  stelem.ref
  IL_151a:  dup
  IL_151b:  ldc.i4.1
  IL_151c:  ldc.i4.1
  IL_151d:  ldnull
  IL_151e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1523:  stelem.ref
  IL_1524:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1529:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_152e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site61""
  IL_1533:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site61""
  IL_1538:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_153d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site61""
  IL_1542:  ldloc.s    V_8
  IL_1544:  ldloc.s    V_9
  IL_1546:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_154b:  pop
  IL_154c:  ldloc.3
  IL_154d:  stloc.s    V_10
  IL_154f:  ldloc.s    V_4
  IL_1551:  stloc.s    V_8
  IL_1553:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site65""
  IL_1558:  brtrue.s   IL_1579
  IL_155a:  ldc.i4.0
  IL_155b:  ldstr      ""d3""
  IL_1560:  ldtoken    ""C""
  IL_1565:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_156a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_156f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1574:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site65""
  IL_1579:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site65""
  IL_157e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_1583:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site65""
  IL_1588:  ldloc.s    V_8
  IL_158a:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_158f:  brtrue     IL_1690
  IL_1594:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site64""
  IL_1599:  brtrue.s   IL_15d8
  IL_159b:  ldc.i4     0x80
  IL_15a0:  ldstr      ""d3""
  IL_15a5:  ldtoken    ""C""
  IL_15aa:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_15af:  ldc.i4.2
  IL_15b0:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_15b5:  dup
  IL_15b6:  ldc.i4.0
  IL_15b7:  ldc.i4.0
  IL_15b8:  ldnull
  IL_15b9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_15be:  stelem.ref
  IL_15bf:  dup
  IL_15c0:  ldc.i4.1
  IL_15c1:  ldc.i4.0
  IL_15c2:  ldnull
  IL_15c3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_15c8:  stelem.ref
  IL_15c9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_15ce:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_15d3:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site64""
  IL_15d8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site64""
  IL_15dd:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_15e2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site64""
  IL_15e7:  ldloc.s    V_8
  IL_15e9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site63""
  IL_15ee:  brtrue.s   IL_1626
  IL_15f0:  ldc.i4.0
  IL_15f1:  ldc.i4.s   73
  IL_15f3:  ldtoken    ""C""
  IL_15f8:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_15fd:  ldc.i4.2
  IL_15fe:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1603:  dup
  IL_1604:  ldc.i4.0
  IL_1605:  ldc.i4.0
  IL_1606:  ldnull
  IL_1607:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_160c:  stelem.ref
  IL_160d:  dup
  IL_160e:  ldc.i4.1
  IL_160f:  ldc.i4.1
  IL_1610:  ldnull
  IL_1611:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1616:  stelem.ref
  IL_1617:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_161c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1621:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site63""
  IL_1626:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site63""
  IL_162b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_1630:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site63""
  IL_1635:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site62""
  IL_163a:  brtrue.s   IL_166b
  IL_163c:  ldc.i4.0
  IL_163d:  ldstr      ""d3""
  IL_1642:  ldtoken    ""C""
  IL_1647:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_164c:  ldc.i4.1
  IL_164d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1652:  dup
  IL_1653:  ldc.i4.0
  IL_1654:  ldc.i4.0
  IL_1655:  ldnull
  IL_1656:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_165b:  stelem.ref
  IL_165c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1661:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1666:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site62""
  IL_166b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site62""
  IL_1670:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_1675:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site62""
  IL_167a:  ldloc.s    V_8
  IL_167c:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1681:  ldloc.s    V_10
  IL_1683:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_1688:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_168d:  pop
  IL_168e:  br.s       IL_16ee
  IL_1690:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site66""
  IL_1695:  brtrue.s   IL_16d5
  IL_1697:  ldc.i4     0x104
  IL_169c:  ldstr      ""remove_d3""
  IL_16a1:  ldnull
  IL_16a2:  ldtoken    ""C""
  IL_16a7:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_16ac:  ldc.i4.2
  IL_16ad:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_16b2:  dup
  IL_16b3:  ldc.i4.0
  IL_16b4:  ldc.i4.0
  IL_16b5:  ldnull
  IL_16b6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_16bb:  stelem.ref
  IL_16bc:  dup
  IL_16bd:  ldc.i4.1
  IL_16be:  ldc.i4.1
  IL_16bf:  ldnull
  IL_16c0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_16c5:  stelem.ref
  IL_16c6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_16cb:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_16d0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site66""
  IL_16d5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site66""
  IL_16da:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_16df:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<Main>o__SiteContainer0.<>p__Site66""
  IL_16e4:  ldloc.s    V_8
  IL_16e6:  ldloc.s    V_10
  IL_16e8:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_16ed:  pop
  IL_16ee:  ldloc.2
  IL_16ef:  stloc.s    V_9
  IL_16f1:  ldloc.s    V_4
  IL_16f3:  stloc.s    V_8
  IL_16f5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site70""
  IL_16fa:  brtrue.s   IL_171b
  IL_16fc:  ldc.i4.0
  IL_16fd:  ldstr      ""d1""
  IL_1702:  ldtoken    ""C""
  IL_1707:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_170c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_1711:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1716:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site70""
  IL_171b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site70""
  IL_1720:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_1725:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<Main>o__SiteContainer0.<>p__Site70""
  IL_172a:  ldloc.s    V_8
  IL_172c:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1731:  brtrue     IL_1831
  IL_1736:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site69""
  IL_173b:  brtrue.s   IL_177a
  IL_173d:  ldc.i4     0x80
  IL_1742:  ldstr      ""d1""
  IL_1747:  ldtoken    ""C""
  IL_174c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1751:  ldc.i4.2
  IL_1752:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1757:  dup
  IL_1758:  ldc.i4.0
  IL_1759:  ldc.i4.0
  IL_175a:  ldnull
  IL_175b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1760:  stelem.ref
  IL_1761:  dup
  IL_1762:  ldc.i4.1
  IL_1763:  ldc.i4.0
  IL_1764:  ldnull
  IL_1765:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_176a:  stelem.ref
  IL_176b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1770:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1775:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site69""
  IL_177a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site69""
  IL_177f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_1784:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site69""
  IL_1789:  ldloc.s    V_8
  IL_178b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site68""
  IL_1790:  brtrue.s   IL_17c8
  IL_1792:  ldc.i4.0
  IL_1793:  ldc.i4.s   63
  IL_1795:  ldtoken    ""C""
  IL_179a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_179f:  ldc.i4.2
  IL_17a0:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_17a5:  dup
  IL_17a6:  ldc.i4.0
  IL_17a7:  ldc.i4.0
  IL_17a8:  ldnull
  IL_17a9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_17ae:  stelem.ref
  IL_17af:  dup
  IL_17b0:  ldc.i4.1
  IL_17b1:  ldc.i4.1
  IL_17b2:  ldnull
  IL_17b3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_17b8:  stelem.ref
  IL_17b9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_17be:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_17c3:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site68""
  IL_17c8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site68""
  IL_17cd:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_17d2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site68""
  IL_17d7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site67""
  IL_17dc:  brtrue.s   IL_180d
  IL_17de:  ldc.i4.0
  IL_17df:  ldstr      ""d1""
  IL_17e4:  ldtoken    ""C""
  IL_17e9:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_17ee:  ldc.i4.1
  IL_17ef:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_17f4:  dup
  IL_17f5:  ldc.i4.0
  IL_17f6:  ldc.i4.0
  IL_17f7:  ldnull
  IL_17f8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_17fd:  stelem.ref
  IL_17fe:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1803:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1808:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site67""
  IL_180d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site67""
  IL_1812:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_1817:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<Main>o__SiteContainer0.<>p__Site67""
  IL_181c:  ldloc.s    V_8
  IL_181e:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1823:  ldloc.s    V_9
  IL_1825:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_182a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_182f:  pop
  IL_1830:  ret
  IL_1831:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site71""
  IL_1836:  brtrue.s   IL_1876
  IL_1838:  ldc.i4     0x104
  IL_183d:  ldstr      ""add_d1""
  IL_1842:  ldnull
  IL_1843:  ldtoken    ""C""
  IL_1848:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_184d:  ldc.i4.2
  IL_184e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1853:  dup
  IL_1854:  ldc.i4.0
  IL_1855:  ldc.i4.0
  IL_1856:  ldnull
  IL_1857:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_185c:  stelem.ref
  IL_185d:  dup
  IL_185e:  ldc.i4.1
  IL_185f:  ldc.i4.1
  IL_1860:  ldnull
  IL_1861:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1866:  stelem.ref
  IL_1867:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_186c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1871:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site71""
  IL_1876:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site71""
  IL_187b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_1880:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<Main>o__SiteContainer0.<>p__Site71""
  IL_1885:  ldloc.s    V_8
  IL_1887:  ldloc.s    V_9
  IL_1889:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_188e:  pop
  IL_188f:  ret
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
  // Code size      119 (0x77)
  .maxstack  3
  .locals init (EventLibrary.voidDelegate V_0, //testDelegate
                A V_1)
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
  IL_001c:  ldloc.1
  IL_001d:  dup
  IL_001e:  ldvirtftn  ""void A.d1.remove""
  IL_0024:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0029:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveAllEventHandlers(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>)""
  IL_002e:  ldarg.0
  IL_002f:  stloc.1
  IL_0030:  ldloc.1
  IL_0031:  dup
  IL_0032:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d1.add""
  IL_0038:  newobj     ""System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_003d:  ldloc.1
  IL_003e:  dup
  IL_003f:  ldvirtftn  ""void A.d1.remove""
  IL_0045:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_004a:  ldloc.0
  IL_004b:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.voidDelegate>(System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0050:  ldarg.0
  IL_0051:  dup
  IL_0052:  ldvirtftn  ""void A.d1.remove""
  IL_0058:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_005d:  ldloc.0
  IL_005e:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.voidDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0063:  ldarg.0
  IL_0064:  ldflda     ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate> A.d1""
  IL_0069:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>)""
  IL_006e:  callvirt   ""EventLibrary.voidDelegate System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>.InvocationList.get""
  IL_0073:  ldnull
  IL_0074:  ceq
  IL_0076:  ret
}
");
            verifier.VerifyIL("A.Scenario2",
@"
{
  // Code size      119 (0x77)
  .maxstack  4
  .locals init (EventLibrary.voidDelegate V_0, //testDelegate
                A V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""EventLibrary.voidDelegate A.CS$<>9__CachedAnonymousMethodDelegate3""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""void A.<Scenario2>b__2(object)""
  IL_0011:  newobj     ""EventLibrary.voidDelegate..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""EventLibrary.voidDelegate A.CS$<>9__CachedAnonymousMethodDelegate3""
  IL_001c:  stloc.0
  IL_001d:  ldloc.1
  IL_001e:  dup
  IL_001f:  ldvirtftn  ""void A.d1.remove""
  IL_0025:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_002a:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveAllEventHandlers(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>)""
  IL_002f:  dup
  IL_0030:  stloc.1
  IL_0031:  ldloc.1
  IL_0032:  dup
  IL_0033:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d1.add""
  IL_0039:  newobj     ""System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_003e:  ldloc.1
  IL_003f:  dup
  IL_0040:  ldvirtftn  ""void A.d1.remove""
  IL_0046:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_004b:  ldloc.0
  IL_004c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.voidDelegate>(System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0051:  dup
  IL_0052:  dup
  IL_0053:  ldvirtftn  ""void A.d1.remove""
  IL_0059:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_005e:  ldloc.0
  IL_005f:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.voidDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
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
  .locals init (Windows.UI.Xaml.Application V_0)
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
  .maxstack  5
  .locals init (Windows.UI.Xaml.Application V_0)
  IL_0000:  ldnull
  IL_0001:  dup
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  dup
  IL_0005:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken Windows.UI.Xaml.Application.Suspending.add""
  IL_000b:  newobj     ""System.Func<Windows.UI.Xaml.SuspendingEventHandler, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0010:  ldloc.0
  IL_0011:  dup
  IL_0012:  ldvirtftn  ""void Windows.UI.Xaml.Application.Suspending.remove""
  IL_0018:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_001d:  ldarg.0
  IL_001e:  ldftn      ""void abcdef.OnSuspending(object, Windows.ApplicationModel.SuspendingEventArgs)""
  IL_0024:  newobj     ""Windows.UI.Xaml.SuspendingEventHandler..ctor(object, System.IntPtr)""
  IL_0029:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<Windows.UI.Xaml.SuspendingEventHandler>(System.Func<Windows.UI.Xaml.SuspendingEventHandler, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, Windows.UI.Xaml.SuspendingEventHandler)""
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
