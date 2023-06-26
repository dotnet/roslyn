// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Linq;
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

        private readonly MetadataReference _eventLibRef;

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
            // The following two libraries are shrunk code pulled from
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
            _eventLibRef = CreateEmptyCompilation(
                eventLibSrc,
                references: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef_v4_0_30319_17929 },
                options: TestOptions.DebugWinMD.WithAllowUnsafe(true),
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

            var dynamicCommon = CreateEmptyCompilation(
                DynamicCommonSrc,
                references: new[] {
                    MscorlibRef_v4_0_30316_17626,
                    _eventLibRef,
                },
                options: TestOptions.DebugModule.WithAllowUnsafe(true));

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

            var verifier = this.CompileAndVerify(
                src,
                targetFramework: TargetFramework.Empty,
                references: new[] {
                    MscorlibRef_v4_0_30316_17626,
                    SystemCoreRef_v4_0_30319_17929,
                    CSharpRef,
                    _eventLibRef,
                    dynamicCommonRef
                });
            verifier.VerifyIL("C.Main",
@"
{
  // Code size     6468 (0x1944)
  .maxstack  11
  .locals init (B V_0, //b
                EventLibrary.voidDelegate V_1, //test
                EventLibrary.genericDelegate<object> V_2, //generic
                EventLibrary.dynamicDelegate V_3, //dyn
                object V_4, //c
                A V_5,
                B V_6,
                object V_7,
                bool V_8,
                object V_9,
                EventLibrary.voidDelegate V_10,
                EventLibrary.genericDelegate<object> V_11,
                EventLibrary.dynamicDelegate V_12)
  IL_0000:  newobj     ""A..ctor()""
  IL_0005:  newobj     ""B..ctor()""
  IL_000a:  stloc.0
  IL_000b:  ldsfld     ""EventLibrary.voidDelegate C.<>c.<>9__0_0""
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_002a
  IL_0013:  pop
  IL_0014:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0019:  ldftn      ""void C.<>c.<Main>b__0_0()""
  IL_001f:  newobj     ""EventLibrary.voidDelegate..ctor(object, System.IntPtr)""
  IL_0024:  dup
  IL_0025:  stsfld     ""EventLibrary.voidDelegate C.<>c.<>9__0_0""
  IL_002a:  stloc.1
  IL_002b:  ldsfld     ""EventLibrary.genericDelegate<object> C.<>c.<>9__0_1""
  IL_0030:  dup
  IL_0031:  brtrue.s   IL_004a
  IL_0033:  pop
  IL_0034:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0039:  ldftn      ""object C.<>c.<Main>b__0_1(object)""
  IL_003f:  newobj     ""EventLibrary.genericDelegate<object>..ctor(object, System.IntPtr)""
  IL_0044:  dup
  IL_0045:  stsfld     ""EventLibrary.genericDelegate<object> C.<>c.<>9__0_1""
  IL_004a:  stloc.2
  IL_004b:  ldsfld     ""EventLibrary.dynamicDelegate C.<>c.<>9__0_2""
  IL_0050:  dup
  IL_0051:  brtrue.s   IL_006a
  IL_0053:  pop
  IL_0054:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0059:  ldftn      ""dynamic C.<>c.<Main>b__0_2(dynamic)""
  IL_005f:  newobj     ""EventLibrary.dynamicDelegate..ctor(object, System.IntPtr)""
  IL_0064:  dup
  IL_0065:  stsfld     ""EventLibrary.dynamicDelegate C.<>c.<>9__0_2""
  IL_006a:  stloc.3
  IL_006b:  dup
  IL_006c:  stloc.s    V_5
  IL_006e:  ldloc.s    V_5
  IL_0070:  dup
  IL_0071:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d1.add""
  IL_0077:  newobj     ""System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_007c:  ldloc.s    V_5
  IL_007e:  dup
  IL_007f:  ldvirtftn  ""void A.d1.remove""
  IL_0085:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_008a:  ldloc.1
  IL_008b:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.voidDelegate>(System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0090:  dup
  IL_0091:  stloc.s    V_5
  IL_0093:  ldloc.s    V_5
  IL_0095:  dup
  IL_0096:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d2.add""
  IL_009c:  newobj     ""System.Func<EventLibrary.genericDelegate<object>, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00a1:  ldloc.s    V_5
  IL_00a3:  dup
  IL_00a4:  ldvirtftn  ""void A.d2.remove""
  IL_00aa:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00af:  ldloc.2
  IL_00b0:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.genericDelegate<object>>(System.Func<EventLibrary.genericDelegate<object>, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.genericDelegate<object>)""
  IL_00b5:  dup
  IL_00b6:  stloc.s    V_5
  IL_00b8:  ldloc.s    V_5
  IL_00ba:  dup
  IL_00bb:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d3.add""
  IL_00c1:  newobj     ""System.Func<EventLibrary.dynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00c6:  ldloc.s    V_5
  IL_00c8:  dup
  IL_00c9:  ldvirtftn  ""void A.d3.remove""
  IL_00cf:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00d4:  ldloc.3
  IL_00d5:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.dynamicDelegate>(System.Func<EventLibrary.dynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.dynamicDelegate)""
  IL_00da:  dup
  IL_00db:  dup
  IL_00dc:  ldvirtftn  ""void A.d1.remove""
  IL_00e2:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00e7:  ldloc.1
  IL_00e8:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.voidDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_00ed:  dup
  IL_00ee:  dup
  IL_00ef:  ldvirtftn  ""void A.d2.remove""
  IL_00f5:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_00fa:  ldloc.2
  IL_00fb:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.genericDelegate<object>>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.genericDelegate<object>)""
  IL_0100:  dup
  IL_0101:  dup
  IL_0102:  ldvirtftn  ""void A.d3.remove""
  IL_0108:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_010d:  ldloc.3
  IL_010e:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.dynamicDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.dynamicDelegate)""
  IL_0113:  ldloc.0
  IL_0114:  stloc.s    V_6
  IL_0116:  ldloc.s    V_6
  IL_0118:  dup
  IL_0119:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken B.d1.add""
  IL_011f:  newobj     ""System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0124:  ldloc.s    V_6
  IL_0126:  dup
  IL_0127:  ldvirtftn  ""void B.d1.remove""
  IL_012d:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0132:  ldloc.1
  IL_0133:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.voidDelegate>(System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0138:  ldloc.0
  IL_0139:  stloc.s    V_6
  IL_013b:  ldloc.s    V_6
  IL_013d:  dup
  IL_013e:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken B.d2.add""
  IL_0144:  newobj     ""System.Func<EventLibrary.genericDelegate<object>, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0149:  ldloc.s    V_6
  IL_014b:  dup
  IL_014c:  ldvirtftn  ""void B.d2.remove""
  IL_0152:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0157:  ldloc.2
  IL_0158:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.genericDelegate<object>>(System.Func<EventLibrary.genericDelegate<object>, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.genericDelegate<object>)""
  IL_015d:  ldloc.0
  IL_015e:  stloc.s    V_6
  IL_0160:  ldloc.s    V_6
  IL_0162:  dup
  IL_0163:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken B.d3.add""
  IL_0169:  newobj     ""System.Func<EventLibrary.dynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_016e:  ldloc.s    V_6
  IL_0170:  dup
  IL_0171:  ldvirtftn  ""void B.d3.remove""
  IL_0177:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_017c:  ldloc.3
  IL_017d:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.dynamicDelegate>(System.Func<EventLibrary.dynamicDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.dynamicDelegate)""
  IL_0182:  ldloc.0
  IL_0183:  dup
  IL_0184:  ldvirtftn  ""void B.d1.remove""
  IL_018a:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_018f:  ldloc.1
  IL_0190:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.voidDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0195:  ldloc.0
  IL_0196:  dup
  IL_0197:  ldvirtftn  ""void B.d2.remove""
  IL_019d:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_01a2:  ldloc.2
  IL_01a3:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.genericDelegate<object>>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.genericDelegate<object>)""
  IL_01a8:  ldloc.0
  IL_01a9:  dup
  IL_01aa:  ldvirtftn  ""void B.d3.remove""
  IL_01b0:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_01b5:  ldloc.3
  IL_01b6:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.dynamicDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.dynamicDelegate)""
  IL_01bb:  stloc.s    V_4
  IL_01bd:  ldloc.s    V_4
  IL_01bf:  stloc.s    V_7
  IL_01c1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__2""
  IL_01c6:  brtrue.s   IL_01e7
  IL_01c8:  ldc.i4.0
  IL_01c9:  ldstr      ""d1""
  IL_01ce:  ldtoken    ""C""
  IL_01d3:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01d8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_01dd:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_01e2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__2""
  IL_01e7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__2""
  IL_01ec:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_01f1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__2""
  IL_01f6:  ldloc.s    V_7
  IL_01f8:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_01fd:  stloc.s    V_8
  IL_01ff:  ldloc.s    V_8
  IL_0201:  brtrue.s   IL_0251
  IL_0203:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__1""
  IL_0208:  brtrue.s   IL_0239
  IL_020a:  ldc.i4.0
  IL_020b:  ldstr      ""d1""
  IL_0210:  ldtoken    ""C""
  IL_0215:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_021a:  ldc.i4.1
  IL_021b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0220:  dup
  IL_0221:  ldc.i4.0
  IL_0222:  ldc.i4.0
  IL_0223:  ldnull
  IL_0224:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0229:  stelem.ref
  IL_022a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_022f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0234:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__1""
  IL_0239:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__1""
  IL_023e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0243:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__1""
  IL_0248:  ldloc.s    V_7
  IL_024a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_024f:  stloc.s    V_9
  IL_0251:  ldloc.1
  IL_0252:  stloc.s    V_10
  IL_0254:  ldloc.s    V_8
  IL_0256:  brtrue     IL_030d
  IL_025b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__5""
  IL_0260:  brtrue.s   IL_029f
  IL_0262:  ldc.i4     0x80
  IL_0267:  ldstr      ""d1""
  IL_026c:  ldtoken    ""C""
  IL_0271:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0276:  ldc.i4.2
  IL_0277:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_027c:  dup
  IL_027d:  ldc.i4.0
  IL_027e:  ldc.i4.0
  IL_027f:  ldnull
  IL_0280:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0285:  stelem.ref
  IL_0286:  dup
  IL_0287:  ldc.i4.1
  IL_0288:  ldc.i4.0
  IL_0289:  ldnull
  IL_028a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_028f:  stelem.ref
  IL_0290:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0295:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_029a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__5""
  IL_029f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__5""
  IL_02a4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_02a9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__5""
  IL_02ae:  ldloc.s    V_7
  IL_02b0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__4""
  IL_02b5:  brtrue.s   IL_02ed
  IL_02b7:  ldc.i4.0
  IL_02b8:  ldc.i4.s   63
  IL_02ba:  ldtoken    ""C""
  IL_02bf:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_02c4:  ldc.i4.2
  IL_02c5:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_02ca:  dup
  IL_02cb:  ldc.i4.0
  IL_02cc:  ldc.i4.0
  IL_02cd:  ldnull
  IL_02ce:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_02d3:  stelem.ref
  IL_02d4:  dup
  IL_02d5:  ldc.i4.1
  IL_02d6:  ldc.i4.1
  IL_02d7:  ldnull
  IL_02d8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_02dd:  stelem.ref
  IL_02de:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_02e3:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_02e8:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__4""
  IL_02ed:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__4""
  IL_02f2:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_02f7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__4""
  IL_02fc:  ldloc.s    V_9
  IL_02fe:  ldloc.s    V_10
  IL_0300:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0305:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_030a:  pop
  IL_030b:  br.s       IL_036b
  IL_030d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__3""
  IL_0312:  brtrue.s   IL_0352
  IL_0314:  ldc.i4     0x104
  IL_0319:  ldstr      ""add_d1""
  IL_031e:  ldnull
  IL_031f:  ldtoken    ""C""
  IL_0324:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0329:  ldc.i4.2
  IL_032a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_032f:  dup
  IL_0330:  ldc.i4.0
  IL_0331:  ldc.i4.0
  IL_0332:  ldnull
  IL_0333:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0338:  stelem.ref
  IL_0339:  dup
  IL_033a:  ldc.i4.1
  IL_033b:  ldc.i4.1
  IL_033c:  ldnull
  IL_033d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0342:  stelem.ref
  IL_0343:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0348:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_034d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__3""
  IL_0352:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__3""
  IL_0357:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_035c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__3""
  IL_0361:  ldloc.s    V_7
  IL_0363:  ldloc.s    V_10
  IL_0365:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_036a:  pop
  IL_036b:  ldloc.s    V_4
  IL_036d:  stloc.s    V_7
  IL_036f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__7""
  IL_0374:  brtrue.s   IL_0395
  IL_0376:  ldc.i4.0
  IL_0377:  ldstr      ""d2""
  IL_037c:  ldtoken    ""C""
  IL_0381:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0386:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_038b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0390:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__7""
  IL_0395:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__7""
  IL_039a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_039f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__7""
  IL_03a4:  ldloc.s    V_7
  IL_03a6:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_03ab:  stloc.s    V_8
  IL_03ad:  ldloc.s    V_8
  IL_03af:  brtrue.s   IL_03ff
  IL_03b1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__6""
  IL_03b6:  brtrue.s   IL_03e7
  IL_03b8:  ldc.i4.0
  IL_03b9:  ldstr      ""d2""
  IL_03be:  ldtoken    ""C""
  IL_03c3:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_03c8:  ldc.i4.1
  IL_03c9:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_03ce:  dup
  IL_03cf:  ldc.i4.0
  IL_03d0:  ldc.i4.0
  IL_03d1:  ldnull
  IL_03d2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_03d7:  stelem.ref
  IL_03d8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_03dd:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_03e2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__6""
  IL_03e7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__6""
  IL_03ec:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_03f1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__6""
  IL_03f6:  ldloc.s    V_7
  IL_03f8:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_03fd:  stloc.s    V_9
  IL_03ff:  ldloc.2
  IL_0400:  stloc.s    V_11
  IL_0402:  ldloc.s    V_8
  IL_0404:  brtrue     IL_04bb
  IL_0409:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__10""
  IL_040e:  brtrue.s   IL_044d
  IL_0410:  ldc.i4     0x80
  IL_0415:  ldstr      ""d2""
  IL_041a:  ldtoken    ""C""
  IL_041f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0424:  ldc.i4.2
  IL_0425:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_042a:  dup
  IL_042b:  ldc.i4.0
  IL_042c:  ldc.i4.0
  IL_042d:  ldnull
  IL_042e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0433:  stelem.ref
  IL_0434:  dup
  IL_0435:  ldc.i4.1
  IL_0436:  ldc.i4.0
  IL_0437:  ldnull
  IL_0438:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_043d:  stelem.ref
  IL_043e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0443:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0448:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__10""
  IL_044d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__10""
  IL_0452:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0457:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__10""
  IL_045c:  ldloc.s    V_7
  IL_045e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__9""
  IL_0463:  brtrue.s   IL_049b
  IL_0465:  ldc.i4.0
  IL_0466:  ldc.i4.s   63
  IL_0468:  ldtoken    ""C""
  IL_046d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0472:  ldc.i4.2
  IL_0473:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0478:  dup
  IL_0479:  ldc.i4.0
  IL_047a:  ldc.i4.0
  IL_047b:  ldnull
  IL_047c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0481:  stelem.ref
  IL_0482:  dup
  IL_0483:  ldc.i4.1
  IL_0484:  ldc.i4.1
  IL_0485:  ldnull
  IL_0486:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_048b:  stelem.ref
  IL_048c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0491:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0496:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__9""
  IL_049b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__9""
  IL_04a0:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_04a5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__9""
  IL_04aa:  ldloc.s    V_9
  IL_04ac:  ldloc.s    V_11
  IL_04ae:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_04b3:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_04b8:  pop
  IL_04b9:  br.s       IL_0519
  IL_04bb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__8""
  IL_04c0:  brtrue.s   IL_0500
  IL_04c2:  ldc.i4     0x104
  IL_04c7:  ldstr      ""add_d2""
  IL_04cc:  ldnull
  IL_04cd:  ldtoken    ""C""
  IL_04d2:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_04d7:  ldc.i4.2
  IL_04d8:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_04dd:  dup
  IL_04de:  ldc.i4.0
  IL_04df:  ldc.i4.0
  IL_04e0:  ldnull
  IL_04e1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_04e6:  stelem.ref
  IL_04e7:  dup
  IL_04e8:  ldc.i4.1
  IL_04e9:  ldc.i4.1
  IL_04ea:  ldnull
  IL_04eb:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_04f0:  stelem.ref
  IL_04f1:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_04f6:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_04fb:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__8""
  IL_0500:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__8""
  IL_0505:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_050a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__8""
  IL_050f:  ldloc.s    V_7
  IL_0511:  ldloc.s    V_11
  IL_0513:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0518:  pop
  IL_0519:  ldloc.s    V_4
  IL_051b:  stloc.s    V_7
  IL_051d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__12""
  IL_0522:  brtrue.s   IL_0543
  IL_0524:  ldc.i4.0
  IL_0525:  ldstr      ""d3""
  IL_052a:  ldtoken    ""C""
  IL_052f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0534:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0539:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_053e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__12""
  IL_0543:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__12""
  IL_0548:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_054d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__12""
  IL_0552:  ldloc.s    V_7
  IL_0554:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0559:  stloc.s    V_8
  IL_055b:  ldloc.s    V_8
  IL_055d:  brtrue.s   IL_05ad
  IL_055f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__11""
  IL_0564:  brtrue.s   IL_0595
  IL_0566:  ldc.i4.0
  IL_0567:  ldstr      ""d3""
  IL_056c:  ldtoken    ""C""
  IL_0571:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0576:  ldc.i4.1
  IL_0577:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_057c:  dup
  IL_057d:  ldc.i4.0
  IL_057e:  ldc.i4.0
  IL_057f:  ldnull
  IL_0580:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0585:  stelem.ref
  IL_0586:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_058b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0590:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__11""
  IL_0595:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__11""
  IL_059a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_059f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__11""
  IL_05a4:  ldloc.s    V_7
  IL_05a6:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_05ab:  stloc.s    V_9
  IL_05ad:  ldloc.3
  IL_05ae:  stloc.s    V_12
  IL_05b0:  ldloc.s    V_8
  IL_05b2:  brtrue     IL_0669
  IL_05b7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__15""
  IL_05bc:  brtrue.s   IL_05fb
  IL_05be:  ldc.i4     0x80
  IL_05c3:  ldstr      ""d3""
  IL_05c8:  ldtoken    ""C""
  IL_05cd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_05d2:  ldc.i4.2
  IL_05d3:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_05d8:  dup
  IL_05d9:  ldc.i4.0
  IL_05da:  ldc.i4.0
  IL_05db:  ldnull
  IL_05dc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_05e1:  stelem.ref
  IL_05e2:  dup
  IL_05e3:  ldc.i4.1
  IL_05e4:  ldc.i4.0
  IL_05e5:  ldnull
  IL_05e6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_05eb:  stelem.ref
  IL_05ec:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_05f1:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_05f6:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__15""
  IL_05fb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__15""
  IL_0600:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0605:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__15""
  IL_060a:  ldloc.s    V_7
  IL_060c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__14""
  IL_0611:  brtrue.s   IL_0649
  IL_0613:  ldc.i4.0
  IL_0614:  ldc.i4.s   63
  IL_0616:  ldtoken    ""C""
  IL_061b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0620:  ldc.i4.2
  IL_0621:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0626:  dup
  IL_0627:  ldc.i4.0
  IL_0628:  ldc.i4.0
  IL_0629:  ldnull
  IL_062a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_062f:  stelem.ref
  IL_0630:  dup
  IL_0631:  ldc.i4.1
  IL_0632:  ldc.i4.1
  IL_0633:  ldnull
  IL_0634:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0639:  stelem.ref
  IL_063a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_063f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0644:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__14""
  IL_0649:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__14""
  IL_064e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_0653:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__14""
  IL_0658:  ldloc.s    V_9
  IL_065a:  ldloc.s    V_12
  IL_065c:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0661:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0666:  pop
  IL_0667:  br.s       IL_06c7
  IL_0669:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__13""
  IL_066e:  brtrue.s   IL_06ae
  IL_0670:  ldc.i4     0x104
  IL_0675:  ldstr      ""add_d3""
  IL_067a:  ldnull
  IL_067b:  ldtoken    ""C""
  IL_0680:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0685:  ldc.i4.2
  IL_0686:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_068b:  dup
  IL_068c:  ldc.i4.0
  IL_068d:  ldc.i4.0
  IL_068e:  ldnull
  IL_068f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0694:  stelem.ref
  IL_0695:  dup
  IL_0696:  ldc.i4.1
  IL_0697:  ldc.i4.1
  IL_0698:  ldnull
  IL_0699:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_069e:  stelem.ref
  IL_069f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_06a4:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_06a9:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__13""
  IL_06ae:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__13""
  IL_06b3:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_06b8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__13""
  IL_06bd:  ldloc.s    V_7
  IL_06bf:  ldloc.s    V_12
  IL_06c1:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_06c6:  pop
  IL_06c7:  ldloc.s    V_4
  IL_06c9:  stloc.s    V_7
  IL_06cb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__17""
  IL_06d0:  brtrue.s   IL_06f1
  IL_06d2:  ldc.i4.0
  IL_06d3:  ldstr      ""d1""
  IL_06d8:  ldtoken    ""C""
  IL_06dd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_06e2:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_06e7:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_06ec:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__17""
  IL_06f1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__17""
  IL_06f6:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_06fb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__17""
  IL_0700:  ldloc.s    V_7
  IL_0702:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0707:  stloc.s    V_8
  IL_0709:  ldloc.s    V_8
  IL_070b:  brtrue.s   IL_075b
  IL_070d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__16""
  IL_0712:  brtrue.s   IL_0743
  IL_0714:  ldc.i4.0
  IL_0715:  ldstr      ""d1""
  IL_071a:  ldtoken    ""C""
  IL_071f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0724:  ldc.i4.1
  IL_0725:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_072a:  dup
  IL_072b:  ldc.i4.0
  IL_072c:  ldc.i4.0
  IL_072d:  ldnull
  IL_072e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0733:  stelem.ref
  IL_0734:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0739:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_073e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__16""
  IL_0743:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__16""
  IL_0748:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_074d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__16""
  IL_0752:  ldloc.s    V_7
  IL_0754:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0759:  stloc.s    V_9
  IL_075b:  ldloc.1
  IL_075c:  stloc.s    V_10
  IL_075e:  ldloc.s    V_8
  IL_0760:  brtrue     IL_0817
  IL_0765:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__20""
  IL_076a:  brtrue.s   IL_07a9
  IL_076c:  ldc.i4     0x80
  IL_0771:  ldstr      ""d1""
  IL_0776:  ldtoken    ""C""
  IL_077b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0780:  ldc.i4.2
  IL_0781:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0786:  dup
  IL_0787:  ldc.i4.0
  IL_0788:  ldc.i4.0
  IL_0789:  ldnull
  IL_078a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_078f:  stelem.ref
  IL_0790:  dup
  IL_0791:  ldc.i4.1
  IL_0792:  ldc.i4.0
  IL_0793:  ldnull
  IL_0794:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0799:  stelem.ref
  IL_079a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_079f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_07a4:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__20""
  IL_07a9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__20""
  IL_07ae:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_07b3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__20""
  IL_07b8:  ldloc.s    V_7
  IL_07ba:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__19""
  IL_07bf:  brtrue.s   IL_07f7
  IL_07c1:  ldc.i4.0
  IL_07c2:  ldc.i4.s   73
  IL_07c4:  ldtoken    ""C""
  IL_07c9:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_07ce:  ldc.i4.2
  IL_07cf:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_07d4:  dup
  IL_07d5:  ldc.i4.0
  IL_07d6:  ldc.i4.0
  IL_07d7:  ldnull
  IL_07d8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_07dd:  stelem.ref
  IL_07de:  dup
  IL_07df:  ldc.i4.1
  IL_07e0:  ldc.i4.1
  IL_07e1:  ldnull
  IL_07e2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_07e7:  stelem.ref
  IL_07e8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_07ed:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_07f2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__19""
  IL_07f7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__19""
  IL_07fc:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0801:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__19""
  IL_0806:  ldloc.s    V_9
  IL_0808:  ldloc.s    V_10
  IL_080a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_080f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0814:  pop
  IL_0815:  br.s       IL_0875
  IL_0817:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__18""
  IL_081c:  brtrue.s   IL_085c
  IL_081e:  ldc.i4     0x104
  IL_0823:  ldstr      ""remove_d1""
  IL_0828:  ldnull
  IL_0829:  ldtoken    ""C""
  IL_082e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0833:  ldc.i4.2
  IL_0834:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0839:  dup
  IL_083a:  ldc.i4.0
  IL_083b:  ldc.i4.0
  IL_083c:  ldnull
  IL_083d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0842:  stelem.ref
  IL_0843:  dup
  IL_0844:  ldc.i4.1
  IL_0845:  ldc.i4.1
  IL_0846:  ldnull
  IL_0847:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_084c:  stelem.ref
  IL_084d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0852:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0857:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__18""
  IL_085c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__18""
  IL_0861:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0866:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__18""
  IL_086b:  ldloc.s    V_7
  IL_086d:  ldloc.s    V_10
  IL_086f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0874:  pop
  IL_0875:  ldloc.s    V_4
  IL_0877:  stloc.s    V_7
  IL_0879:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__22""
  IL_087e:  brtrue.s   IL_089f
  IL_0880:  ldc.i4.0
  IL_0881:  ldstr      ""d2""
  IL_0886:  ldtoken    ""C""
  IL_088b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0890:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0895:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_089a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__22""
  IL_089f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__22""
  IL_08a4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_08a9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__22""
  IL_08ae:  ldloc.s    V_7
  IL_08b0:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_08b5:  stloc.s    V_8
  IL_08b7:  ldloc.s    V_8
  IL_08b9:  brtrue.s   IL_0909
  IL_08bb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__21""
  IL_08c0:  brtrue.s   IL_08f1
  IL_08c2:  ldc.i4.0
  IL_08c3:  ldstr      ""d2""
  IL_08c8:  ldtoken    ""C""
  IL_08cd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_08d2:  ldc.i4.1
  IL_08d3:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_08d8:  dup
  IL_08d9:  ldc.i4.0
  IL_08da:  ldc.i4.0
  IL_08db:  ldnull
  IL_08dc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_08e1:  stelem.ref
  IL_08e2:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_08e7:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_08ec:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__21""
  IL_08f1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__21""
  IL_08f6:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_08fb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__21""
  IL_0900:  ldloc.s    V_7
  IL_0902:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0907:  stloc.s    V_9
  IL_0909:  ldloc.2
  IL_090a:  stloc.s    V_11
  IL_090c:  ldloc.s    V_8
  IL_090e:  brtrue     IL_09c5
  IL_0913:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__25""
  IL_0918:  brtrue.s   IL_0957
  IL_091a:  ldc.i4     0x80
  IL_091f:  ldstr      ""d2""
  IL_0924:  ldtoken    ""C""
  IL_0929:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_092e:  ldc.i4.2
  IL_092f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0934:  dup
  IL_0935:  ldc.i4.0
  IL_0936:  ldc.i4.0
  IL_0937:  ldnull
  IL_0938:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_093d:  stelem.ref
  IL_093e:  dup
  IL_093f:  ldc.i4.1
  IL_0940:  ldc.i4.0
  IL_0941:  ldnull
  IL_0942:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0947:  stelem.ref
  IL_0948:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_094d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0952:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__25""
  IL_0957:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__25""
  IL_095c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0961:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__25""
  IL_0966:  ldloc.s    V_7
  IL_0968:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__24""
  IL_096d:  brtrue.s   IL_09a5
  IL_096f:  ldc.i4.0
  IL_0970:  ldc.i4.s   73
  IL_0972:  ldtoken    ""C""
  IL_0977:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_097c:  ldc.i4.2
  IL_097d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0982:  dup
  IL_0983:  ldc.i4.0
  IL_0984:  ldc.i4.0
  IL_0985:  ldnull
  IL_0986:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_098b:  stelem.ref
  IL_098c:  dup
  IL_098d:  ldc.i4.1
  IL_098e:  ldc.i4.1
  IL_098f:  ldnull
  IL_0990:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0995:  stelem.ref
  IL_0996:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_099b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_09a0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__24""
  IL_09a5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__24""
  IL_09aa:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_09af:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__24""
  IL_09b4:  ldloc.s    V_9
  IL_09b6:  ldloc.s    V_11
  IL_09b8:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_09bd:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_09c2:  pop
  IL_09c3:  br.s       IL_0a23
  IL_09c5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__23""
  IL_09ca:  brtrue.s   IL_0a0a
  IL_09cc:  ldc.i4     0x104
  IL_09d1:  ldstr      ""remove_d2""
  IL_09d6:  ldnull
  IL_09d7:  ldtoken    ""C""
  IL_09dc:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_09e1:  ldc.i4.2
  IL_09e2:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_09e7:  dup
  IL_09e8:  ldc.i4.0
  IL_09e9:  ldc.i4.0
  IL_09ea:  ldnull
  IL_09eb:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_09f0:  stelem.ref
  IL_09f1:  dup
  IL_09f2:  ldc.i4.1
  IL_09f3:  ldc.i4.1
  IL_09f4:  ldnull
  IL_09f5:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_09fa:  stelem.ref
  IL_09fb:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0a00:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0a05:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__23""
  IL_0a0a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__23""
  IL_0a0f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0a14:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__23""
  IL_0a19:  ldloc.s    V_7
  IL_0a1b:  ldloc.s    V_11
  IL_0a1d:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0a22:  pop
  IL_0a23:  ldloc.s    V_4
  IL_0a25:  stloc.s    V_7
  IL_0a27:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__27""
  IL_0a2c:  brtrue.s   IL_0a4d
  IL_0a2e:  ldc.i4.0
  IL_0a2f:  ldstr      ""d3""
  IL_0a34:  ldtoken    ""C""
  IL_0a39:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0a3e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0a43:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0a48:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__27""
  IL_0a4d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__27""
  IL_0a52:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0a57:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__27""
  IL_0a5c:  ldloc.s    V_7
  IL_0a5e:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0a63:  stloc.s    V_8
  IL_0a65:  ldloc.s    V_8
  IL_0a67:  brtrue.s   IL_0ab7
  IL_0a69:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__26""
  IL_0a6e:  brtrue.s   IL_0a9f
  IL_0a70:  ldc.i4.0
  IL_0a71:  ldstr      ""d3""
  IL_0a76:  ldtoken    ""C""
  IL_0a7b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0a80:  ldc.i4.1
  IL_0a81:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0a86:  dup
  IL_0a87:  ldc.i4.0
  IL_0a88:  ldc.i4.0
  IL_0a89:  ldnull
  IL_0a8a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0a8f:  stelem.ref
  IL_0a90:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0a95:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0a9a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__26""
  IL_0a9f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__26""
  IL_0aa4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0aa9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__26""
  IL_0aae:  ldloc.s    V_7
  IL_0ab0:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0ab5:  stloc.s    V_9
  IL_0ab7:  ldloc.3
  IL_0ab8:  stloc.s    V_12
  IL_0aba:  ldloc.s    V_8
  IL_0abc:  brtrue     IL_0b73
  IL_0ac1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__30""
  IL_0ac6:  brtrue.s   IL_0b05
  IL_0ac8:  ldc.i4     0x80
  IL_0acd:  ldstr      ""d3""
  IL_0ad2:  ldtoken    ""C""
  IL_0ad7:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0adc:  ldc.i4.2
  IL_0add:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0ae2:  dup
  IL_0ae3:  ldc.i4.0
  IL_0ae4:  ldc.i4.0
  IL_0ae5:  ldnull
  IL_0ae6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0aeb:  stelem.ref
  IL_0aec:  dup
  IL_0aed:  ldc.i4.1
  IL_0aee:  ldc.i4.0
  IL_0aef:  ldnull
  IL_0af0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0af5:  stelem.ref
  IL_0af6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0afb:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0b00:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__30""
  IL_0b05:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__30""
  IL_0b0a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0b0f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__30""
  IL_0b14:  ldloc.s    V_7
  IL_0b16:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__29""
  IL_0b1b:  brtrue.s   IL_0b53
  IL_0b1d:  ldc.i4.0
  IL_0b1e:  ldc.i4.s   73
  IL_0b20:  ldtoken    ""C""
  IL_0b25:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0b2a:  ldc.i4.2
  IL_0b2b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0b30:  dup
  IL_0b31:  ldc.i4.0
  IL_0b32:  ldc.i4.0
  IL_0b33:  ldnull
  IL_0b34:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0b39:  stelem.ref
  IL_0b3a:  dup
  IL_0b3b:  ldc.i4.1
  IL_0b3c:  ldc.i4.1
  IL_0b3d:  ldnull
  IL_0b3e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0b43:  stelem.ref
  IL_0b44:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0b49:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0b4e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__29""
  IL_0b53:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__29""
  IL_0b58:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_0b5d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__29""
  IL_0b62:  ldloc.s    V_9
  IL_0b64:  ldloc.s    V_12
  IL_0b66:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0b6b:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0b70:  pop
  IL_0b71:  br.s       IL_0bd1
  IL_0b73:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__28""
  IL_0b78:  brtrue.s   IL_0bb8
  IL_0b7a:  ldc.i4     0x104
  IL_0b7f:  ldstr      ""remove_d3""
  IL_0b84:  ldnull
  IL_0b85:  ldtoken    ""C""
  IL_0b8a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0b8f:  ldc.i4.2
  IL_0b90:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0b95:  dup
  IL_0b96:  ldc.i4.0
  IL_0b97:  ldc.i4.0
  IL_0b98:  ldnull
  IL_0b99:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0b9e:  stelem.ref
  IL_0b9f:  dup
  IL_0ba0:  ldc.i4.1
  IL_0ba1:  ldc.i4.1
  IL_0ba2:  ldnull
  IL_0ba3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ba8:  stelem.ref
  IL_0ba9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0bae:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0bb3:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__28""
  IL_0bb8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__28""
  IL_0bbd:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_0bc2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__28""
  IL_0bc7:  ldloc.s    V_7
  IL_0bc9:  ldloc.s    V_12
  IL_0bcb:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0bd0:  pop
  IL_0bd1:  ldloc.s    V_4
  IL_0bd3:  stloc.s    V_7
  IL_0bd5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__32""
  IL_0bda:  brtrue.s   IL_0bfb
  IL_0bdc:  ldc.i4.0
  IL_0bdd:  ldstr      ""d1""
  IL_0be2:  ldtoken    ""C""
  IL_0be7:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0bec:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0bf1:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0bf6:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__32""
  IL_0bfb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__32""
  IL_0c00:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0c05:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__32""
  IL_0c0a:  ldloc.s    V_7
  IL_0c0c:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0c11:  stloc.s    V_8
  IL_0c13:  ldloc.s    V_8
  IL_0c15:  brtrue.s   IL_0c65
  IL_0c17:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__31""
  IL_0c1c:  brtrue.s   IL_0c4d
  IL_0c1e:  ldc.i4.0
  IL_0c1f:  ldstr      ""d1""
  IL_0c24:  ldtoken    ""C""
  IL_0c29:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0c2e:  ldc.i4.1
  IL_0c2f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0c34:  dup
  IL_0c35:  ldc.i4.0
  IL_0c36:  ldc.i4.0
  IL_0c37:  ldnull
  IL_0c38:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0c3d:  stelem.ref
  IL_0c3e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0c43:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0c48:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__31""
  IL_0c4d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__31""
  IL_0c52:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0c57:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__31""
  IL_0c5c:  ldloc.s    V_7
  IL_0c5e:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0c63:  stloc.s    V_9
  IL_0c65:  ldloc.2
  IL_0c66:  stloc.s    V_11
  IL_0c68:  ldloc.s    V_8
  IL_0c6a:  brtrue     IL_0d21
  IL_0c6f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__35""
  IL_0c74:  brtrue.s   IL_0cb3
  IL_0c76:  ldc.i4     0x80
  IL_0c7b:  ldstr      ""d1""
  IL_0c80:  ldtoken    ""C""
  IL_0c85:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0c8a:  ldc.i4.2
  IL_0c8b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0c90:  dup
  IL_0c91:  ldc.i4.0
  IL_0c92:  ldc.i4.0
  IL_0c93:  ldnull
  IL_0c94:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0c99:  stelem.ref
  IL_0c9a:  dup
  IL_0c9b:  ldc.i4.1
  IL_0c9c:  ldc.i4.0
  IL_0c9d:  ldnull
  IL_0c9e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ca3:  stelem.ref
  IL_0ca4:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0ca9:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0cae:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__35""
  IL_0cb3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__35""
  IL_0cb8:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0cbd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__35""
  IL_0cc2:  ldloc.s    V_7
  IL_0cc4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__34""
  IL_0cc9:  brtrue.s   IL_0d01
  IL_0ccb:  ldc.i4.0
  IL_0ccc:  ldc.i4.s   63
  IL_0cce:  ldtoken    ""C""
  IL_0cd3:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0cd8:  ldc.i4.2
  IL_0cd9:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0cde:  dup
  IL_0cdf:  ldc.i4.0
  IL_0ce0:  ldc.i4.0
  IL_0ce1:  ldnull
  IL_0ce2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ce7:  stelem.ref
  IL_0ce8:  dup
  IL_0ce9:  ldc.i4.1
  IL_0cea:  ldc.i4.1
  IL_0ceb:  ldnull
  IL_0cec:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0cf1:  stelem.ref
  IL_0cf2:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0cf7:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0cfc:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__34""
  IL_0d01:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__34""
  IL_0d06:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0d0b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__34""
  IL_0d10:  ldloc.s    V_9
  IL_0d12:  ldloc.s    V_11
  IL_0d14:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0d19:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0d1e:  pop
  IL_0d1f:  br.s       IL_0d7f
  IL_0d21:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__33""
  IL_0d26:  brtrue.s   IL_0d66
  IL_0d28:  ldc.i4     0x104
  IL_0d2d:  ldstr      ""add_d1""
  IL_0d32:  ldnull
  IL_0d33:  ldtoken    ""C""
  IL_0d38:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0d3d:  ldc.i4.2
  IL_0d3e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0d43:  dup
  IL_0d44:  ldc.i4.0
  IL_0d45:  ldc.i4.0
  IL_0d46:  ldnull
  IL_0d47:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0d4c:  stelem.ref
  IL_0d4d:  dup
  IL_0d4e:  ldc.i4.1
  IL_0d4f:  ldc.i4.1
  IL_0d50:  ldnull
  IL_0d51:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0d56:  stelem.ref
  IL_0d57:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0d5c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0d61:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__33""
  IL_0d66:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__33""
  IL_0d6b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0d70:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__33""
  IL_0d75:  ldloc.s    V_7
  IL_0d77:  ldloc.s    V_11
  IL_0d79:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0d7e:  pop
  IL_0d7f:  ldloc.0
  IL_0d80:  stloc.s    V_4
  IL_0d82:  ldloc.s    V_4
  IL_0d84:  stloc.s    V_7
  IL_0d86:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__37""
  IL_0d8b:  brtrue.s   IL_0dac
  IL_0d8d:  ldc.i4.0
  IL_0d8e:  ldstr      ""d1""
  IL_0d93:  ldtoken    ""C""
  IL_0d98:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0d9d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0da2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0da7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__37""
  IL_0dac:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__37""
  IL_0db1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0db6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__37""
  IL_0dbb:  ldloc.s    V_7
  IL_0dbd:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0dc2:  stloc.s    V_8
  IL_0dc4:  ldloc.s    V_8
  IL_0dc6:  brtrue.s   IL_0e16
  IL_0dc8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__36""
  IL_0dcd:  brtrue.s   IL_0dfe
  IL_0dcf:  ldc.i4.0
  IL_0dd0:  ldstr      ""d1""
  IL_0dd5:  ldtoken    ""C""
  IL_0dda:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0ddf:  ldc.i4.1
  IL_0de0:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0de5:  dup
  IL_0de6:  ldc.i4.0
  IL_0de7:  ldc.i4.0
  IL_0de8:  ldnull
  IL_0de9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0dee:  stelem.ref
  IL_0def:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0df4:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0df9:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__36""
  IL_0dfe:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__36""
  IL_0e03:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0e08:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__36""
  IL_0e0d:  ldloc.s    V_7
  IL_0e0f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0e14:  stloc.s    V_9
  IL_0e16:  ldloc.1
  IL_0e17:  stloc.s    V_10
  IL_0e19:  ldloc.s    V_8
  IL_0e1b:  brtrue     IL_0ed2
  IL_0e20:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__40""
  IL_0e25:  brtrue.s   IL_0e64
  IL_0e27:  ldc.i4     0x80
  IL_0e2c:  ldstr      ""d1""
  IL_0e31:  ldtoken    ""C""
  IL_0e36:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0e3b:  ldc.i4.2
  IL_0e3c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0e41:  dup
  IL_0e42:  ldc.i4.0
  IL_0e43:  ldc.i4.0
  IL_0e44:  ldnull
  IL_0e45:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0e4a:  stelem.ref
  IL_0e4b:  dup
  IL_0e4c:  ldc.i4.1
  IL_0e4d:  ldc.i4.0
  IL_0e4e:  ldnull
  IL_0e4f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0e54:  stelem.ref
  IL_0e55:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0e5a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0e5f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__40""
  IL_0e64:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__40""
  IL_0e69:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0e6e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__40""
  IL_0e73:  ldloc.s    V_7
  IL_0e75:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__39""
  IL_0e7a:  brtrue.s   IL_0eb2
  IL_0e7c:  ldc.i4.0
  IL_0e7d:  ldc.i4.s   63
  IL_0e7f:  ldtoken    ""C""
  IL_0e84:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0e89:  ldc.i4.2
  IL_0e8a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0e8f:  dup
  IL_0e90:  ldc.i4.0
  IL_0e91:  ldc.i4.0
  IL_0e92:  ldnull
  IL_0e93:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0e98:  stelem.ref
  IL_0e99:  dup
  IL_0e9a:  ldc.i4.1
  IL_0e9b:  ldc.i4.1
  IL_0e9c:  ldnull
  IL_0e9d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ea2:  stelem.ref
  IL_0ea3:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0ea8:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0ead:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__39""
  IL_0eb2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__39""
  IL_0eb7:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0ebc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__39""
  IL_0ec1:  ldloc.s    V_9
  IL_0ec3:  ldloc.s    V_10
  IL_0ec5:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0eca:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0ecf:  pop
  IL_0ed0:  br.s       IL_0f30
  IL_0ed2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__38""
  IL_0ed7:  brtrue.s   IL_0f17
  IL_0ed9:  ldc.i4     0x104
  IL_0ede:  ldstr      ""add_d1""
  IL_0ee3:  ldnull
  IL_0ee4:  ldtoken    ""C""
  IL_0ee9:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0eee:  ldc.i4.2
  IL_0eef:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0ef4:  dup
  IL_0ef5:  ldc.i4.0
  IL_0ef6:  ldc.i4.0
  IL_0ef7:  ldnull
  IL_0ef8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0efd:  stelem.ref
  IL_0efe:  dup
  IL_0eff:  ldc.i4.1
  IL_0f00:  ldc.i4.1
  IL_0f01:  ldnull
  IL_0f02:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f07:  stelem.ref
  IL_0f08:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0f0d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0f12:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__38""
  IL_0f17:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__38""
  IL_0f1c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0f21:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__38""
  IL_0f26:  ldloc.s    V_7
  IL_0f28:  ldloc.s    V_10
  IL_0f2a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0f2f:  pop
  IL_0f30:  ldloc.s    V_4
  IL_0f32:  stloc.s    V_7
  IL_0f34:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__42""
  IL_0f39:  brtrue.s   IL_0f5a
  IL_0f3b:  ldc.i4.0
  IL_0f3c:  ldstr      ""d2""
  IL_0f41:  ldtoken    ""C""
  IL_0f46:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0f4b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0f50:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0f55:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__42""
  IL_0f5a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__42""
  IL_0f5f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0f64:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__42""
  IL_0f69:  ldloc.s    V_7
  IL_0f6b:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0f70:  stloc.s    V_8
  IL_0f72:  ldloc.s    V_8
  IL_0f74:  brtrue.s   IL_0fc4
  IL_0f76:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__41""
  IL_0f7b:  brtrue.s   IL_0fac
  IL_0f7d:  ldc.i4.0
  IL_0f7e:  ldstr      ""d2""
  IL_0f83:  ldtoken    ""C""
  IL_0f88:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0f8d:  ldc.i4.1
  IL_0f8e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0f93:  dup
  IL_0f94:  ldc.i4.0
  IL_0f95:  ldc.i4.0
  IL_0f96:  ldnull
  IL_0f97:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f9c:  stelem.ref
  IL_0f9d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0fa2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0fa7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__41""
  IL_0fac:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__41""
  IL_0fb1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0fb6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__41""
  IL_0fbb:  ldloc.s    V_7
  IL_0fbd:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0fc2:  stloc.s    V_9
  IL_0fc4:  ldloc.2
  IL_0fc5:  stloc.s    V_11
  IL_0fc7:  ldloc.s    V_8
  IL_0fc9:  brtrue     IL_1080
  IL_0fce:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__45""
  IL_0fd3:  brtrue.s   IL_1012
  IL_0fd5:  ldc.i4     0x80
  IL_0fda:  ldstr      ""d2""
  IL_0fdf:  ldtoken    ""C""
  IL_0fe4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0fe9:  ldc.i4.2
  IL_0fea:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0fef:  dup
  IL_0ff0:  ldc.i4.0
  IL_0ff1:  ldc.i4.0
  IL_0ff2:  ldnull
  IL_0ff3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ff8:  stelem.ref
  IL_0ff9:  dup
  IL_0ffa:  ldc.i4.1
  IL_0ffb:  ldc.i4.0
  IL_0ffc:  ldnull
  IL_0ffd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1002:  stelem.ref
  IL_1003:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1008:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_100d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__45""
  IL_1012:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__45""
  IL_1017:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_101c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__45""
  IL_1021:  ldloc.s    V_7
  IL_1023:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__44""
  IL_1028:  brtrue.s   IL_1060
  IL_102a:  ldc.i4.0
  IL_102b:  ldc.i4.s   63
  IL_102d:  ldtoken    ""C""
  IL_1032:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1037:  ldc.i4.2
  IL_1038:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_103d:  dup
  IL_103e:  ldc.i4.0
  IL_103f:  ldc.i4.0
  IL_1040:  ldnull
  IL_1041:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1046:  stelem.ref
  IL_1047:  dup
  IL_1048:  ldc.i4.1
  IL_1049:  ldc.i4.1
  IL_104a:  ldnull
  IL_104b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1050:  stelem.ref
  IL_1051:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1056:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_105b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__44""
  IL_1060:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__44""
  IL_1065:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_106a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__44""
  IL_106f:  ldloc.s    V_9
  IL_1071:  ldloc.s    V_11
  IL_1073:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_1078:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_107d:  pop
  IL_107e:  br.s       IL_10de
  IL_1080:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__43""
  IL_1085:  brtrue.s   IL_10c5
  IL_1087:  ldc.i4     0x104
  IL_108c:  ldstr      ""add_d2""
  IL_1091:  ldnull
  IL_1092:  ldtoken    ""C""
  IL_1097:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_109c:  ldc.i4.2
  IL_109d:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_10a2:  dup
  IL_10a3:  ldc.i4.0
  IL_10a4:  ldc.i4.0
  IL_10a5:  ldnull
  IL_10a6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_10ab:  stelem.ref
  IL_10ac:  dup
  IL_10ad:  ldc.i4.1
  IL_10ae:  ldc.i4.1
  IL_10af:  ldnull
  IL_10b0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_10b5:  stelem.ref
  IL_10b6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_10bb:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_10c0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__43""
  IL_10c5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__43""
  IL_10ca:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_10cf:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__43""
  IL_10d4:  ldloc.s    V_7
  IL_10d6:  ldloc.s    V_11
  IL_10d8:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_10dd:  pop
  IL_10de:  ldloc.s    V_4
  IL_10e0:  stloc.s    V_7
  IL_10e2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__47""
  IL_10e7:  brtrue.s   IL_1108
  IL_10e9:  ldc.i4.0
  IL_10ea:  ldstr      ""d3""
  IL_10ef:  ldtoken    ""C""
  IL_10f4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_10f9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_10fe:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1103:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__47""
  IL_1108:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__47""
  IL_110d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_1112:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__47""
  IL_1117:  ldloc.s    V_7
  IL_1119:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_111e:  stloc.s    V_8
  IL_1120:  ldloc.s    V_8
  IL_1122:  brtrue.s   IL_1172
  IL_1124:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__46""
  IL_1129:  brtrue.s   IL_115a
  IL_112b:  ldc.i4.0
  IL_112c:  ldstr      ""d3""
  IL_1131:  ldtoken    ""C""
  IL_1136:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_113b:  ldc.i4.1
  IL_113c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1141:  dup
  IL_1142:  ldc.i4.0
  IL_1143:  ldc.i4.0
  IL_1144:  ldnull
  IL_1145:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_114a:  stelem.ref
  IL_114b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1150:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1155:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__46""
  IL_115a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__46""
  IL_115f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_1164:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__46""
  IL_1169:  ldloc.s    V_7
  IL_116b:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1170:  stloc.s    V_9
  IL_1172:  ldloc.3
  IL_1173:  stloc.s    V_12
  IL_1175:  ldloc.s    V_8
  IL_1177:  brtrue     IL_122e
  IL_117c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__50""
  IL_1181:  brtrue.s   IL_11c0
  IL_1183:  ldc.i4     0x80
  IL_1188:  ldstr      ""d3""
  IL_118d:  ldtoken    ""C""
  IL_1192:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1197:  ldc.i4.2
  IL_1198:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_119d:  dup
  IL_119e:  ldc.i4.0
  IL_119f:  ldc.i4.0
  IL_11a0:  ldnull
  IL_11a1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_11a6:  stelem.ref
  IL_11a7:  dup
  IL_11a8:  ldc.i4.1
  IL_11a9:  ldc.i4.0
  IL_11aa:  ldnull
  IL_11ab:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_11b0:  stelem.ref
  IL_11b1:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_11b6:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_11bb:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__50""
  IL_11c0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__50""
  IL_11c5:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_11ca:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__50""
  IL_11cf:  ldloc.s    V_7
  IL_11d1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__49""
  IL_11d6:  brtrue.s   IL_120e
  IL_11d8:  ldc.i4.0
  IL_11d9:  ldc.i4.s   63
  IL_11db:  ldtoken    ""C""
  IL_11e0:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_11e5:  ldc.i4.2
  IL_11e6:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_11eb:  dup
  IL_11ec:  ldc.i4.0
  IL_11ed:  ldc.i4.0
  IL_11ee:  ldnull
  IL_11ef:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_11f4:  stelem.ref
  IL_11f5:  dup
  IL_11f6:  ldc.i4.1
  IL_11f7:  ldc.i4.1
  IL_11f8:  ldnull
  IL_11f9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_11fe:  stelem.ref
  IL_11ff:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1204:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1209:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__49""
  IL_120e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__49""
  IL_1213:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_1218:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__49""
  IL_121d:  ldloc.s    V_9
  IL_121f:  ldloc.s    V_12
  IL_1221:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_1226:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_122b:  pop
  IL_122c:  br.s       IL_128c
  IL_122e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__48""
  IL_1233:  brtrue.s   IL_1273
  IL_1235:  ldc.i4     0x104
  IL_123a:  ldstr      ""add_d3""
  IL_123f:  ldnull
  IL_1240:  ldtoken    ""C""
  IL_1245:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_124a:  ldc.i4.2
  IL_124b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1250:  dup
  IL_1251:  ldc.i4.0
  IL_1252:  ldc.i4.0
  IL_1253:  ldnull
  IL_1254:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1259:  stelem.ref
  IL_125a:  dup
  IL_125b:  ldc.i4.1
  IL_125c:  ldc.i4.1
  IL_125d:  ldnull
  IL_125e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1263:  stelem.ref
  IL_1264:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1269:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_126e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__48""
  IL_1273:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__48""
  IL_1278:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_127d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__48""
  IL_1282:  ldloc.s    V_7
  IL_1284:  ldloc.s    V_12
  IL_1286:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_128b:  pop
  IL_128c:  ldloc.s    V_4
  IL_128e:  stloc.s    V_7
  IL_1290:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__52""
  IL_1295:  brtrue.s   IL_12b6
  IL_1297:  ldc.i4.0
  IL_1298:  ldstr      ""d1""
  IL_129d:  ldtoken    ""C""
  IL_12a2:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_12a7:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_12ac:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_12b1:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__52""
  IL_12b6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__52""
  IL_12bb:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_12c0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__52""
  IL_12c5:  ldloc.s    V_7
  IL_12c7:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_12cc:  stloc.s    V_8
  IL_12ce:  ldloc.s    V_8
  IL_12d0:  brtrue.s   IL_1320
  IL_12d2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__51""
  IL_12d7:  brtrue.s   IL_1308
  IL_12d9:  ldc.i4.0
  IL_12da:  ldstr      ""d1""
  IL_12df:  ldtoken    ""C""
  IL_12e4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_12e9:  ldc.i4.1
  IL_12ea:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_12ef:  dup
  IL_12f0:  ldc.i4.0
  IL_12f1:  ldc.i4.0
  IL_12f2:  ldnull
  IL_12f3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_12f8:  stelem.ref
  IL_12f9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_12fe:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1303:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__51""
  IL_1308:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__51""
  IL_130d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_1312:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__51""
  IL_1317:  ldloc.s    V_7
  IL_1319:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_131e:  stloc.s    V_9
  IL_1320:  ldloc.1
  IL_1321:  stloc.s    V_10
  IL_1323:  ldloc.s    V_8
  IL_1325:  brtrue     IL_13dc
  IL_132a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__55""
  IL_132f:  brtrue.s   IL_136e
  IL_1331:  ldc.i4     0x80
  IL_1336:  ldstr      ""d1""
  IL_133b:  ldtoken    ""C""
  IL_1340:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1345:  ldc.i4.2
  IL_1346:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_134b:  dup
  IL_134c:  ldc.i4.0
  IL_134d:  ldc.i4.0
  IL_134e:  ldnull
  IL_134f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1354:  stelem.ref
  IL_1355:  dup
  IL_1356:  ldc.i4.1
  IL_1357:  ldc.i4.0
  IL_1358:  ldnull
  IL_1359:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_135e:  stelem.ref
  IL_135f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1364:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1369:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__55""
  IL_136e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__55""
  IL_1373:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_1378:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__55""
  IL_137d:  ldloc.s    V_7
  IL_137f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__54""
  IL_1384:  brtrue.s   IL_13bc
  IL_1386:  ldc.i4.0
  IL_1387:  ldc.i4.s   73
  IL_1389:  ldtoken    ""C""
  IL_138e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1393:  ldc.i4.2
  IL_1394:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1399:  dup
  IL_139a:  ldc.i4.0
  IL_139b:  ldc.i4.0
  IL_139c:  ldnull
  IL_139d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_13a2:  stelem.ref
  IL_13a3:  dup
  IL_13a4:  ldc.i4.1
  IL_13a5:  ldc.i4.1
  IL_13a6:  ldnull
  IL_13a7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_13ac:  stelem.ref
  IL_13ad:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_13b2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_13b7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__54""
  IL_13bc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__54""
  IL_13c1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_13c6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__54""
  IL_13cb:  ldloc.s    V_9
  IL_13cd:  ldloc.s    V_10
  IL_13cf:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_13d4:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_13d9:  pop
  IL_13da:  br.s       IL_143a
  IL_13dc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__53""
  IL_13e1:  brtrue.s   IL_1421
  IL_13e3:  ldc.i4     0x104
  IL_13e8:  ldstr      ""remove_d1""
  IL_13ed:  ldnull
  IL_13ee:  ldtoken    ""C""
  IL_13f3:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_13f8:  ldc.i4.2
  IL_13f9:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_13fe:  dup
  IL_13ff:  ldc.i4.0
  IL_1400:  ldc.i4.0
  IL_1401:  ldnull
  IL_1402:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1407:  stelem.ref
  IL_1408:  dup
  IL_1409:  ldc.i4.1
  IL_140a:  ldc.i4.1
  IL_140b:  ldnull
  IL_140c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1411:  stelem.ref
  IL_1412:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1417:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_141c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__53""
  IL_1421:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__53""
  IL_1426:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_142b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__53""
  IL_1430:  ldloc.s    V_7
  IL_1432:  ldloc.s    V_10
  IL_1434:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_1439:  pop
  IL_143a:  ldloc.s    V_4
  IL_143c:  stloc.s    V_7
  IL_143e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__57""
  IL_1443:  brtrue.s   IL_1464
  IL_1445:  ldc.i4.0
  IL_1446:  ldstr      ""d2""
  IL_144b:  ldtoken    ""C""
  IL_1450:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1455:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_145a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_145f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__57""
  IL_1464:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__57""
  IL_1469:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_146e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__57""
  IL_1473:  ldloc.s    V_7
  IL_1475:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_147a:  stloc.s    V_8
  IL_147c:  ldloc.s    V_8
  IL_147e:  brtrue.s   IL_14ce
  IL_1480:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__56""
  IL_1485:  brtrue.s   IL_14b6
  IL_1487:  ldc.i4.0
  IL_1488:  ldstr      ""d2""
  IL_148d:  ldtoken    ""C""
  IL_1492:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1497:  ldc.i4.1
  IL_1498:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_149d:  dup
  IL_149e:  ldc.i4.0
  IL_149f:  ldc.i4.0
  IL_14a0:  ldnull
  IL_14a1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_14a6:  stelem.ref
  IL_14a7:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_14ac:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_14b1:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__56""
  IL_14b6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__56""
  IL_14bb:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_14c0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__56""
  IL_14c5:  ldloc.s    V_7
  IL_14c7:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_14cc:  stloc.s    V_9
  IL_14ce:  ldloc.2
  IL_14cf:  stloc.s    V_11
  IL_14d1:  ldloc.s    V_8
  IL_14d3:  brtrue     IL_158a
  IL_14d8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__60""
  IL_14dd:  brtrue.s   IL_151c
  IL_14df:  ldc.i4     0x80
  IL_14e4:  ldstr      ""d2""
  IL_14e9:  ldtoken    ""C""
  IL_14ee:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_14f3:  ldc.i4.2
  IL_14f4:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_14f9:  dup
  IL_14fa:  ldc.i4.0
  IL_14fb:  ldc.i4.0
  IL_14fc:  ldnull
  IL_14fd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1502:  stelem.ref
  IL_1503:  dup
  IL_1504:  ldc.i4.1
  IL_1505:  ldc.i4.0
  IL_1506:  ldnull
  IL_1507:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_150c:  stelem.ref
  IL_150d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1512:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1517:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__60""
  IL_151c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__60""
  IL_1521:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_1526:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__60""
  IL_152b:  ldloc.s    V_7
  IL_152d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__59""
  IL_1532:  brtrue.s   IL_156a
  IL_1534:  ldc.i4.0
  IL_1535:  ldc.i4.s   73
  IL_1537:  ldtoken    ""C""
  IL_153c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1541:  ldc.i4.2
  IL_1542:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1547:  dup
  IL_1548:  ldc.i4.0
  IL_1549:  ldc.i4.0
  IL_154a:  ldnull
  IL_154b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1550:  stelem.ref
  IL_1551:  dup
  IL_1552:  ldc.i4.1
  IL_1553:  ldc.i4.1
  IL_1554:  ldnull
  IL_1555:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_155a:  stelem.ref
  IL_155b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1560:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1565:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__59""
  IL_156a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__59""
  IL_156f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_1574:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__59""
  IL_1579:  ldloc.s    V_9
  IL_157b:  ldloc.s    V_11
  IL_157d:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_1582:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_1587:  pop
  IL_1588:  br.s       IL_15e8
  IL_158a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__58""
  IL_158f:  brtrue.s   IL_15cf
  IL_1591:  ldc.i4     0x104
  IL_1596:  ldstr      ""remove_d2""
  IL_159b:  ldnull
  IL_159c:  ldtoken    ""C""
  IL_15a1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_15a6:  ldc.i4.2
  IL_15a7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_15ac:  dup
  IL_15ad:  ldc.i4.0
  IL_15ae:  ldc.i4.0
  IL_15af:  ldnull
  IL_15b0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_15b5:  stelem.ref
  IL_15b6:  dup
  IL_15b7:  ldc.i4.1
  IL_15b8:  ldc.i4.1
  IL_15b9:  ldnull
  IL_15ba:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_15bf:  stelem.ref
  IL_15c0:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_15c5:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_15ca:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__58""
  IL_15cf:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__58""
  IL_15d4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_15d9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__58""
  IL_15de:  ldloc.s    V_7
  IL_15e0:  ldloc.s    V_11
  IL_15e2:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_15e7:  pop
  IL_15e8:  ldloc.s    V_4
  IL_15ea:  stloc.s    V_7
  IL_15ec:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__62""
  IL_15f1:  brtrue.s   IL_1612
  IL_15f3:  ldc.i4.0
  IL_15f4:  ldstr      ""d3""
  IL_15f9:  ldtoken    ""C""
  IL_15fe:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1603:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_1608:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_160d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__62""
  IL_1612:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__62""
  IL_1617:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_161c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__62""
  IL_1621:  ldloc.s    V_7
  IL_1623:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1628:  stloc.s    V_8
  IL_162a:  ldloc.s    V_8
  IL_162c:  brtrue.s   IL_167c
  IL_162e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__61""
  IL_1633:  brtrue.s   IL_1664
  IL_1635:  ldc.i4.0
  IL_1636:  ldstr      ""d3""
  IL_163b:  ldtoken    ""C""
  IL_1640:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1645:  ldc.i4.1
  IL_1646:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_164b:  dup
  IL_164c:  ldc.i4.0
  IL_164d:  ldc.i4.0
  IL_164e:  ldnull
  IL_164f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1654:  stelem.ref
  IL_1655:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_165a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_165f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__61""
  IL_1664:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__61""
  IL_1669:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_166e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__61""
  IL_1673:  ldloc.s    V_7
  IL_1675:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_167a:  stloc.s    V_9
  IL_167c:  ldloc.3
  IL_167d:  stloc.s    V_12
  IL_167f:  ldloc.s    V_8
  IL_1681:  brtrue     IL_1738
  IL_1686:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__65""
  IL_168b:  brtrue.s   IL_16ca
  IL_168d:  ldc.i4     0x80
  IL_1692:  ldstr      ""d3""
  IL_1697:  ldtoken    ""C""
  IL_169c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_16a1:  ldc.i4.2
  IL_16a2:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_16a7:  dup
  IL_16a8:  ldc.i4.0
  IL_16a9:  ldc.i4.0
  IL_16aa:  ldnull
  IL_16ab:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_16b0:  stelem.ref
  IL_16b1:  dup
  IL_16b2:  ldc.i4.1
  IL_16b3:  ldc.i4.0
  IL_16b4:  ldnull
  IL_16b5:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_16ba:  stelem.ref
  IL_16bb:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_16c0:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_16c5:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__65""
  IL_16ca:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__65""
  IL_16cf:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_16d4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__65""
  IL_16d9:  ldloc.s    V_7
  IL_16db:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__64""
  IL_16e0:  brtrue.s   IL_1718
  IL_16e2:  ldc.i4.0
  IL_16e3:  ldc.i4.s   73
  IL_16e5:  ldtoken    ""C""
  IL_16ea:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_16ef:  ldc.i4.2
  IL_16f0:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_16f5:  dup
  IL_16f6:  ldc.i4.0
  IL_16f7:  ldc.i4.0
  IL_16f8:  ldnull
  IL_16f9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_16fe:  stelem.ref
  IL_16ff:  dup
  IL_1700:  ldc.i4.1
  IL_1701:  ldc.i4.1
  IL_1702:  ldnull
  IL_1703:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1708:  stelem.ref
  IL_1709:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_170e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1713:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__64""
  IL_1718:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__64""
  IL_171d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_1722:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__64""
  IL_1727:  ldloc.s    V_9
  IL_1729:  ldloc.s    V_12
  IL_172b:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_1730:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_1735:  pop
  IL_1736:  br.s       IL_1796
  IL_1738:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__63""
  IL_173d:  brtrue.s   IL_177d
  IL_173f:  ldc.i4     0x104
  IL_1744:  ldstr      ""remove_d3""
  IL_1749:  ldnull
  IL_174a:  ldtoken    ""C""
  IL_174f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1754:  ldc.i4.2
  IL_1755:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_175a:  dup
  IL_175b:  ldc.i4.0
  IL_175c:  ldc.i4.0
  IL_175d:  ldnull
  IL_175e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1763:  stelem.ref
  IL_1764:  dup
  IL_1765:  ldc.i4.1
  IL_1766:  ldc.i4.1
  IL_1767:  ldnull
  IL_1768:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_176d:  stelem.ref
  IL_176e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1773:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1778:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__63""
  IL_177d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__63""
  IL_1782:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_1787:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__63""
  IL_178c:  ldloc.s    V_7
  IL_178e:  ldloc.s    V_12
  IL_1790:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_1795:  pop
  IL_1796:  ldloc.s    V_4
  IL_1798:  stloc.s    V_7
  IL_179a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__67""
  IL_179f:  brtrue.s   IL_17c0
  IL_17a1:  ldc.i4.0
  IL_17a2:  ldstr      ""d1""
  IL_17a7:  ldtoken    ""C""
  IL_17ac:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_17b1:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_17b6:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_17bb:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__67""
  IL_17c0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__67""
  IL_17c5:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_17ca:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__67""
  IL_17cf:  ldloc.s    V_7
  IL_17d1:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_17d6:  stloc.s    V_8
  IL_17d8:  ldloc.s    V_8
  IL_17da:  brtrue.s   IL_182a
  IL_17dc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__66""
  IL_17e1:  brtrue.s   IL_1812
  IL_17e3:  ldc.i4.0
  IL_17e4:  ldstr      ""d1""
  IL_17e9:  ldtoken    ""C""
  IL_17ee:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_17f3:  ldc.i4.1
  IL_17f4:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_17f9:  dup
  IL_17fa:  ldc.i4.0
  IL_17fb:  ldc.i4.0
  IL_17fc:  ldnull
  IL_17fd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1802:  stelem.ref
  IL_1803:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1808:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_180d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__66""
  IL_1812:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__66""
  IL_1817:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_181c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__66""
  IL_1821:  ldloc.s    V_7
  IL_1823:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1828:  stloc.s    V_9
  IL_182a:  ldloc.2
  IL_182b:  stloc.s    V_11
  IL_182d:  ldloc.s    V_8
  IL_182f:  brtrue     IL_18e5
  IL_1834:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__70""
  IL_1839:  brtrue.s   IL_1878
  IL_183b:  ldc.i4     0x80
  IL_1840:  ldstr      ""d1""
  IL_1845:  ldtoken    ""C""
  IL_184a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_184f:  ldc.i4.2
  IL_1850:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1855:  dup
  IL_1856:  ldc.i4.0
  IL_1857:  ldc.i4.0
  IL_1858:  ldnull
  IL_1859:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_185e:  stelem.ref
  IL_185f:  dup
  IL_1860:  ldc.i4.1
  IL_1861:  ldc.i4.0
  IL_1862:  ldnull
  IL_1863:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1868:  stelem.ref
  IL_1869:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_186e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1873:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__70""
  IL_1878:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__70""
  IL_187d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_1882:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__70""
  IL_1887:  ldloc.s    V_7
  IL_1889:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__69""
  IL_188e:  brtrue.s   IL_18c6
  IL_1890:  ldc.i4.0
  IL_1891:  ldc.i4.s   63
  IL_1893:  ldtoken    ""C""
  IL_1898:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_189d:  ldc.i4.2
  IL_189e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_18a3:  dup
  IL_18a4:  ldc.i4.0
  IL_18a5:  ldc.i4.0
  IL_18a6:  ldnull
  IL_18a7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_18ac:  stelem.ref
  IL_18ad:  dup
  IL_18ae:  ldc.i4.1
  IL_18af:  ldc.i4.1
  IL_18b0:  ldnull
  IL_18b1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_18b6:  stelem.ref
  IL_18b7:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_18bc:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_18c1:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__69""
  IL_18c6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__69""
  IL_18cb:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_18d0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__69""
  IL_18d5:  ldloc.s    V_9
  IL_18d7:  ldloc.s    V_11
  IL_18d9:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_18de:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_18e3:  pop
  IL_18e4:  ret
  IL_18e5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__68""
  IL_18ea:  brtrue.s   IL_192a
  IL_18ec:  ldc.i4     0x104
  IL_18f1:  ldstr      ""add_d1""
  IL_18f6:  ldnull
  IL_18f7:  ldtoken    ""C""
  IL_18fc:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1901:  ldc.i4.2
  IL_1902:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1907:  dup
  IL_1908:  ldc.i4.0
  IL_1909:  ldc.i4.0
  IL_190a:  ldnull
  IL_190b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1910:  stelem.ref
  IL_1911:  dup
  IL_1912:  ldc.i4.1
  IL_1913:  ldc.i4.1
  IL_1914:  ldnull
  IL_1915:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_191a:  stelem.ref
  IL_191b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1920:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1925:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__68""
  IL_192a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__68""
  IL_192f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_1934:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__68""
  IL_1939:  ldloc.s    V_7
  IL_193b:  ldloc.s    V_11
  IL_193d:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_1942:  pop
  IL_1943:  ret
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
            var verifier = CompileAndVerify(
                new[] { src, DynamicCommonSrc },
                targetFramework: TargetFramework.Empty,
                references: new[] {
                    MscorlibRef_v4_0_30316_17626,
                    SystemCoreRef_v4_0_30319_17929,
                    _eventLibRef,
                },
                verify: OSVersion.IsWin8 ? Verification.Passes : Verification.Fails);

            verifier.VerifyDiagnostics(
                // (7,42): warning CS0067: The event 'A.d2' is never used
                //     public event genericDelegate<object> d2;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "d2").WithArguments("A.d2").WithLocation(7, 42),
                // (8,34): warning CS0067: The event 'A.d3' is never used
                //     public event dynamicDelegate d3;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "d3").WithArguments("A.d3").WithLocation(8, 34));
            verifier.VerifyIL("A.Scenario1",
@"
{
  // Code size      123 (0x7b)
  .maxstack  3
  .locals init (EventLibrary.voidDelegate V_0, //testDelegate
                A V_1)
  IL_0000:  ldsfld     ""EventLibrary.voidDelegate A.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""A.<>c A.<>c.<>9""
  IL_000e:  ldftn      ""void A.<>c.<Scenario1>b__0_0()""
  IL_0014:  newobj     ""EventLibrary.voidDelegate..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""EventLibrary.voidDelegate A.<>c.<>9__0_0""
  IL_001f:  stloc.0
  IL_0020:  ldloc.1
  IL_0021:  dup
  IL_0022:  ldvirtftn  ""void A.d1.remove""
  IL_0028:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_002d:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveAllEventHandlers(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>)""
  IL_0032:  ldarg.0
  IL_0033:  stloc.1
  IL_0034:  ldloc.1
  IL_0035:  dup
  IL_0036:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d1.add""
  IL_003c:  newobj     ""System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0041:  ldloc.1
  IL_0042:  dup
  IL_0043:  ldvirtftn  ""void A.d1.remove""
  IL_0049:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_004e:  ldloc.0
  IL_004f:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.voidDelegate>(System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0054:  ldarg.0
  IL_0055:  dup
  IL_0056:  ldvirtftn  ""void A.d1.remove""
  IL_005c:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0061:  ldloc.0
  IL_0062:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.voidDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0067:  ldarg.0
  IL_0068:  ldflda     ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate> A.d1""
  IL_006d:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>)""
  IL_0072:  callvirt   ""EventLibrary.voidDelegate System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>.InvocationList.get""
  IL_0077:  ldnull
  IL_0078:  ceq
  IL_007a:  ret
}
");
            verifier.VerifyIL("A.Scenario2",
@"
{
  // Code size      123 (0x7b)
  .maxstack  4
  .locals init (EventLibrary.voidDelegate V_0, //testDelegate
                A V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""EventLibrary.voidDelegate A.<>c.<>9__1_0""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0020
  IL_0009:  pop
  IL_000a:  ldsfld     ""A.<>c A.<>c.<>9""
  IL_000f:  ldftn      ""void A.<>c.<Scenario2>b__1_0()""
  IL_0015:  newobj     ""EventLibrary.voidDelegate..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""EventLibrary.voidDelegate A.<>c.<>9__1_0""
  IL_0020:  stloc.0
  IL_0021:  ldloc.1
  IL_0022:  dup
  IL_0023:  ldvirtftn  ""void A.d1.remove""
  IL_0029:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_002e:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveAllEventHandlers(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>)""
  IL_0033:  dup
  IL_0034:  stloc.1
  IL_0035:  ldloc.1
  IL_0036:  dup
  IL_0037:  ldvirtftn  ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken A.d1.add""
  IL_003d:  newobj     ""System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0042:  ldloc.1
  IL_0043:  dup
  IL_0044:  ldvirtftn  ""void A.d1.remove""
  IL_004a:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_004f:  ldloc.0
  IL_0050:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<EventLibrary.voidDelegate>(System.Func<EventLibrary.voidDelegate, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0055:  dup
  IL_0056:  dup
  IL_0057:  ldvirtftn  ""void A.d1.remove""
  IL_005d:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0062:  ldloc.0
  IL_0063:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<EventLibrary.voidDelegate>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, EventLibrary.voidDelegate)""
  IL_0068:  ldflda     ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate> A.d1""
  IL_006d:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>)""
  IL_0072:  callvirt   ""EventLibrary.voidDelegate System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<EventLibrary.voidDelegate>.InvocationList.get""
  IL_0077:  ldnull
  IL_0078:  ceq
  IL_007a:  ret
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
    public void goo(){
        Application x = null; 
        x.Suspending += (object sender, SuspendingEventArgs e) => {};
    }

    public static void Main(){
            var a = new abcdef();
            a.goo();
    }
} ";

            var cv = this.CompileAndVerifyOnWin8Only(text);

            cv.VerifyIL("abcdef.goo()", @"
{
  // Code size       65 (0x41)
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
  IL_001c:  ldsfld     ""Windows.UI.Xaml.SuspendingEventHandler abcdef.<>c.<>9__0_0""
  IL_0021:  dup
  IL_0022:  brtrue.s   IL_003b
  IL_0024:  pop
  IL_0025:  ldsfld     ""abcdef.<>c abcdef.<>c.<>9""
  IL_002a:  ldftn      ""void abcdef.<>c.<goo>b__0_0(object, Windows.ApplicationModel.SuspendingEventArgs)""
  IL_0030:  newobj     ""Windows.UI.Xaml.SuspendingEventHandler..ctor(object, System.IntPtr)""
  IL_0035:  dup
  IL_0036:  stsfld     ""Windows.UI.Xaml.SuspendingEventHandler abcdef.<>c.<>9__0_0""
  IL_003b:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<Windows.UI.Xaml.SuspendingEventHandler>(System.Func<Windows.UI.Xaml.SuspendingEventHandler, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, Windows.UI.Xaml.SuspendingEventHandler)""
  IL_0040:  ret
}
");
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

                                public void goo(){
                                    Application x = null; 
                                    x.Suspending += OnSuspending;
                                    x.Suspending -= OnSuspending;
                                }

                                public static void Main(){
                                        var a = new abcdef();
                                        a.goo();
                                }
                            } ";

            var cv = this.CompileAndVerifyOnWin8Only(text);

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
            cv.VerifyIL("abcdef.goo()", ExpectedIl);
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

    public void goo(){
        getApplication().Suspending += OnSuspending;
        getApplication().Suspending -= OnSuspending;
    }

    public static void Main(){
            var a = new abcdef();
            a.goo();
    }
}";

            var cv = this.CompileAndVerifyOnWin8Only(text);

            cv.VerifyIL("abcdef.goo()", @"
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
            var comp = CreateCompilationWithWinRT(text);

            var winmdlib = comp.ExternalReferences.Where(r => r.Display == "Windows").Single();
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
            var comp = CreateEmptyCompilation(source, WinRtRefs.Concat(new[] { ilRef }));
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

            var substitutedNormalEvent = implementingNormalEvent.ExplicitInterfaceImplementations.Single();
            var substitutedWinRTEvent = implementingWinRTEvent.ExplicitInterfaceImplementations.Single();

            Assert.IsType<SubstitutedEventSymbol>(substitutedNormalEvent);
            Assert.IsType<SubstitutedEventSymbol>(substitutedWinRTEvent);

            // Based on original definition.
            Assert.False(substitutedNormalEvent.IsWindowsRuntimeEvent);
            Assert.True(substitutedWinRTEvent.IsWindowsRuntimeEvent);

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
                var comp = CreateEmptyCompilation(source, WinRtRefs, TestOptions.CreateTestOptions(kind, OptimizationLevel.Debug));
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
                var comp = CreateEmptyCompilation(source, WinRtRefs.Concat(new[] { ilRef }), TestOptions.CreateTestOptions(kind, OptimizationLevel.Debug));
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
                var comp = CreateEmptyCompilation(source, WinRtRefs.Concat(new[] { interfaceILRef, baseILRef }), TestOptions.CreateTestOptions(kind, OptimizationLevel.Debug));
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
                var comp = CreateEmptyCompilation(source, WinRtRefs.Concat(new[] { ilRef }), TestOptions.CreateTestOptions(kind, OptimizationLevel.Debug));
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

                var comp = CreateEmptyCompilation(source, WinRtRefs.Concat(new[] { ilRef }));
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

                var comp = CreateEmptyCompilation(source, WinRtRefs.Concat(new[] { ilRef }));
                comp.VerifyDiagnostics(
                    // (4,32): error CS1991: 'C.E' cannot implement 'INormal.E' because 'C.E' is a Windows Runtime event and 'INormal.E' is a regular .NET event.
                    //     public event System.Action E 
                    Diagnostic(ErrorCode.ERR_MixingWinRTEventWithRegular, "E").WithArguments("C.E", "INormal.E", "C.E", "INormal.E"));

                var @class = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                var @event = @class.GetMember<EventSymbol>("E");

                Assert.True(@event.IsWindowsRuntimeEvent); //Implemented at least one WinRT event.
            }
        }

        [WorkItem(547321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547321")]
        [Fact(Skip = "547321")]
        public void ERR_MixingWinRTEventWithRegular_BaseTypeImplementsInterface()
        {
            var source = @"
class Derived : ReversedBase, Interface
{
}
";

            var interfaceILRef = CompileIL(EventInterfaceIL);
            var baseILRef = CompileIL(EventBaseIL);

            var comp = CreateEmptyCompilation(source, WinRtRefs.Concat(new[] { interfaceILRef, baseILRef }));
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

            var comp = CreateEmptyCompilation(source, WinRtRefs.Concat(new[] { interfaceILRef, baseILRef }));
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
                var comp = CreateEmptyCompilation(source, WinRtRefs, TestOptions.CreateTestOptions(kind, OptimizationLevel.Debug));
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
            var comp = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseWinMD);
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
            Assert.Equal(@event.Type, fieldType.TypeArguments().Single());
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
            CreateEmptyCompilation(source, WinRtRefs, TestOptions.ReleaseWinMD).VerifyDiagnostics(
                // (9,17): error CS1510: A ref or out value must be an assignable variable
                //         Ref(ref Instance);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "Instance").WithLocation(9, 17),
                // (10,17): error CS7084: A Windows Runtime event may not be passed as an out or ref parameter.
                //         Out(out Instance);
                Diagnostic(ErrorCode.ERR_WinRtEventPassedByRef, "Instance").WithLocation(10, 17),
                // (11,17): error CS1510: A ref or out value must be an assignable variable
                //         Ref(ref Static);
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "Static").WithLocation(11, 17),
                // (12,17): error CS7084: A Windows Runtime event may not be passed as an out or ref parameter.
                //         Out(out Static);
                Diagnostic(ErrorCode.ERR_WinRtEventPassedByRef, "Static").WithLocation(12, 17));
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
            CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseWinMD).VerifyEmitDiagnostics(
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

            var compilation = CreateCompilationWithIL("", il, targetFramework: TargetFramework.Empty, references: WinRtRefs);

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

        [Fact]
        [WorkItem(1055825, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1055825")]
        public void AssociatedField()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .field private class [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1<class [mscorlib]System.Action> E

  .method public hidebysig specialname instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 
          add_E(class [mscorlib]System.Action 'value') cil managed
  {
    ldnull
    throw
  }

  .method public hidebysig specialname instance void 
          remove_E(valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken 'value') cil managed
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

  .event [mscorlib]System.Action E
  {
    .addon instance valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C::add_E(class [mscorlib]System.Action)
    .removeon instance void C::remove_E(valuetype [mscorlib]System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)
  } // end of event C::E
} // end of class C
";
            var ilRef = CompileIL(ilSource);
            var comp = CreateEmptyCompilation("", WinRtRefs.Concat(new[] { ilRef }), TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
            comp.VerifyDiagnostics();

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var @event = type.GetMember<PEEventSymbol>("E");
            Assert.True(@event.HasAssociatedField);

            var field = @event.AssociatedField;
            Assert.NotNull(field);

            Assert.Equal(@event, field.AssociatedSymbol);
        }

        private static void VerifyWinRTEventShape(EventSymbol @event, CSharpCompilation compilation)
        {
            Assert.True(@event.IsWindowsRuntimeEvent);

            var eventType = @event.TypeWithAnnotations;
            var tokenType = compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken);
            Assert.NotNull(tokenType);
            var voidType = compilation.GetSpecialType(SpecialType.System_Void);
            Assert.NotNull(voidType);

            var addMethod = @event.AddMethod;
            Assert.Equal(tokenType, addMethod.ReturnType);
            Assert.False(addMethod.ReturnsVoid);
            Assert.Equal(1, addMethod.ParameterCount);
            Assert.Equal(eventType.Type, addMethod.ParameterTypesWithAnnotations.Single().Type);

            var removeMethod = @event.RemoveMethod;
            Assert.Equal(voidType, removeMethod.ReturnType);
            Assert.True(removeMethod.ReturnsVoid);
            Assert.Equal(1, removeMethod.ParameterCount);
            Assert.Equal(tokenType, removeMethod.ParameterTypesWithAnnotations.Single().Type);

            if (@event.HasAssociatedField)
            {
                var expectedFieldType = compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T).Construct(eventType.Type);
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
            Assert.Equal(eventType, addMethod.ParameterTypesWithAnnotations.Single().Type);

            var removeMethod = @event.RemoveMethod;
            Assert.Equal(voidType, removeMethod.ReturnType);
            Assert.True(removeMethod.ReturnsVoid);
            Assert.Equal(1, removeMethod.ParameterCount);
            Assert.Equal(eventType, removeMethod.ParameterTypesWithAnnotations.Single().Type);

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
