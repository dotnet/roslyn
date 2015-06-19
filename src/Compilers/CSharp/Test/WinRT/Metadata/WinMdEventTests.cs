// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            _eventLibRef = CreateCompilation(
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
                    _eventLibRef,
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

            var verifier = CompileAndVerifyOnWin8Only(
                src,
                additionalRefs: new[] {
                    MscorlibRef_v4_0_30316_17626,
                    SystemCoreRef_v4_0_30319_17929,
                    CSharpRef,
                    _eventLibRef,
                    dynamicCommonRef
                });
            verifier.VerifyIL("C.Main",
@"
{
  // Code size     6300 (0x189c)
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
  IL_01bd:  ldloc.1
  IL_01be:  stloc.s    V_7
  IL_01c0:  ldloc.s    V_4
  IL_01c2:  stloc.s    V_8
  IL_01c4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__4""
  IL_01c9:  brtrue.s   IL_01ea
  IL_01cb:  ldc.i4.0
  IL_01cc:  ldstr      ""d1""
  IL_01d1:  ldtoken    ""C""
  IL_01d6:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01db:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_01e0:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_01e5:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__4""
  IL_01ea:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__4""
  IL_01ef:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_01f4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__4""
  IL_01f9:  ldloc.s    V_8
  IL_01fb:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0200:  brtrue     IL_0301
  IL_0205:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__3""
  IL_020a:  brtrue.s   IL_0249
  IL_020c:  ldc.i4     0x80
  IL_0211:  ldstr      ""d1""
  IL_0216:  ldtoken    ""C""
  IL_021b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0220:  ldc.i4.2
  IL_0221:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0226:  dup
  IL_0227:  ldc.i4.0
  IL_0228:  ldc.i4.0
  IL_0229:  ldnull
  IL_022a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_022f:  stelem.ref
  IL_0230:  dup
  IL_0231:  ldc.i4.1
  IL_0232:  ldc.i4.0
  IL_0233:  ldnull
  IL_0234:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0239:  stelem.ref
  IL_023a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_023f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0244:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__3""
  IL_0249:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__3""
  IL_024e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0253:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__3""
  IL_0258:  ldloc.s    V_8
  IL_025a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__2""
  IL_025f:  brtrue.s   IL_0297
  IL_0261:  ldc.i4.0
  IL_0262:  ldc.i4.s   63
  IL_0264:  ldtoken    ""C""
  IL_0269:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_026e:  ldc.i4.2
  IL_026f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0274:  dup
  IL_0275:  ldc.i4.0
  IL_0276:  ldc.i4.0
  IL_0277:  ldnull
  IL_0278:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_027d:  stelem.ref
  IL_027e:  dup
  IL_027f:  ldc.i4.1
  IL_0280:  ldc.i4.1
  IL_0281:  ldnull
  IL_0282:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0287:  stelem.ref
  IL_0288:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_028d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0292:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__2""
  IL_0297:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__2""
  IL_029c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_02a1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__2""
  IL_02a6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__1""
  IL_02ab:  brtrue.s   IL_02dc
  IL_02ad:  ldc.i4.0
  IL_02ae:  ldstr      ""d1""
  IL_02b3:  ldtoken    ""C""
  IL_02b8:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_02bd:  ldc.i4.1
  IL_02be:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_02c3:  dup
  IL_02c4:  ldc.i4.0
  IL_02c5:  ldc.i4.0
  IL_02c6:  ldnull
  IL_02c7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_02cc:  stelem.ref
  IL_02cd:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_02d2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_02d7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__1""
  IL_02dc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__1""
  IL_02e1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_02e6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__1""
  IL_02eb:  ldloc.s    V_8
  IL_02ed:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_02f2:  ldloc.s    V_7
  IL_02f4:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_02f9:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_02fe:  pop
  IL_02ff:  br.s       IL_035f
  IL_0301:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__5""
  IL_0306:  brtrue.s   IL_0346
  IL_0308:  ldc.i4     0x104
  IL_030d:  ldstr      ""add_d1""
  IL_0312:  ldnull
  IL_0313:  ldtoken    ""C""
  IL_0318:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_031d:  ldc.i4.2
  IL_031e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0323:  dup
  IL_0324:  ldc.i4.0
  IL_0325:  ldc.i4.0
  IL_0326:  ldnull
  IL_0327:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_032c:  stelem.ref
  IL_032d:  dup
  IL_032e:  ldc.i4.1
  IL_032f:  ldc.i4.1
  IL_0330:  ldnull
  IL_0331:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0336:  stelem.ref
  IL_0337:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_033c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0341:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__5""
  IL_0346:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__5""
  IL_034b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0350:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__5""
  IL_0355:  ldloc.s    V_8
  IL_0357:  ldloc.s    V_7
  IL_0359:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_035e:  pop
  IL_035f:  ldloc.2
  IL_0360:  stloc.s    V_9
  IL_0362:  ldloc.s    V_4
  IL_0364:  stloc.s    V_8
  IL_0366:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__9""
  IL_036b:  brtrue.s   IL_038c
  IL_036d:  ldc.i4.0
  IL_036e:  ldstr      ""d2""
  IL_0373:  ldtoken    ""C""
  IL_0378:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_037d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0382:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0387:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__9""
  IL_038c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__9""
  IL_0391:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0396:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__9""
  IL_039b:  ldloc.s    V_8
  IL_039d:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_03a2:  brtrue     IL_04a3
  IL_03a7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__8""
  IL_03ac:  brtrue.s   IL_03eb
  IL_03ae:  ldc.i4     0x80
  IL_03b3:  ldstr      ""d2""
  IL_03b8:  ldtoken    ""C""
  IL_03bd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_03c2:  ldc.i4.2
  IL_03c3:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_03c8:  dup
  IL_03c9:  ldc.i4.0
  IL_03ca:  ldc.i4.0
  IL_03cb:  ldnull
  IL_03cc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_03d1:  stelem.ref
  IL_03d2:  dup
  IL_03d3:  ldc.i4.1
  IL_03d4:  ldc.i4.0
  IL_03d5:  ldnull
  IL_03d6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_03db:  stelem.ref
  IL_03dc:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_03e1:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_03e6:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__8""
  IL_03eb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__8""
  IL_03f0:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_03f5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__8""
  IL_03fa:  ldloc.s    V_8
  IL_03fc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__7""
  IL_0401:  brtrue.s   IL_0439
  IL_0403:  ldc.i4.0
  IL_0404:  ldc.i4.s   63
  IL_0406:  ldtoken    ""C""
  IL_040b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0410:  ldc.i4.2
  IL_0411:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0416:  dup
  IL_0417:  ldc.i4.0
  IL_0418:  ldc.i4.0
  IL_0419:  ldnull
  IL_041a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_041f:  stelem.ref
  IL_0420:  dup
  IL_0421:  ldc.i4.1
  IL_0422:  ldc.i4.1
  IL_0423:  ldnull
  IL_0424:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0429:  stelem.ref
  IL_042a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_042f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0434:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__7""
  IL_0439:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__7""
  IL_043e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0443:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__7""
  IL_0448:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__6""
  IL_044d:  brtrue.s   IL_047e
  IL_044f:  ldc.i4.0
  IL_0450:  ldstr      ""d2""
  IL_0455:  ldtoken    ""C""
  IL_045a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_045f:  ldc.i4.1
  IL_0460:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0465:  dup
  IL_0466:  ldc.i4.0
  IL_0467:  ldc.i4.0
  IL_0468:  ldnull
  IL_0469:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_046e:  stelem.ref
  IL_046f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0474:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0479:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__6""
  IL_047e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__6""
  IL_0483:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0488:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__6""
  IL_048d:  ldloc.s    V_8
  IL_048f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0494:  ldloc.s    V_9
  IL_0496:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_049b:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_04a0:  pop
  IL_04a1:  br.s       IL_0501
  IL_04a3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__10""
  IL_04a8:  brtrue.s   IL_04e8
  IL_04aa:  ldc.i4     0x104
  IL_04af:  ldstr      ""add_d2""
  IL_04b4:  ldnull
  IL_04b5:  ldtoken    ""C""
  IL_04ba:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_04bf:  ldc.i4.2
  IL_04c0:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_04c5:  dup
  IL_04c6:  ldc.i4.0
  IL_04c7:  ldc.i4.0
  IL_04c8:  ldnull
  IL_04c9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_04ce:  stelem.ref
  IL_04cf:  dup
  IL_04d0:  ldc.i4.1
  IL_04d1:  ldc.i4.1
  IL_04d2:  ldnull
  IL_04d3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_04d8:  stelem.ref
  IL_04d9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_04de:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_04e3:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__10""
  IL_04e8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__10""
  IL_04ed:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_04f2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__10""
  IL_04f7:  ldloc.s    V_8
  IL_04f9:  ldloc.s    V_9
  IL_04fb:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0500:  pop
  IL_0501:  ldloc.3
  IL_0502:  stloc.s    V_10
  IL_0504:  ldloc.s    V_4
  IL_0506:  stloc.s    V_8
  IL_0508:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__14""
  IL_050d:  brtrue.s   IL_052e
  IL_050f:  ldc.i4.0
  IL_0510:  ldstr      ""d3""
  IL_0515:  ldtoken    ""C""
  IL_051a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_051f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0524:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0529:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__14""
  IL_052e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__14""
  IL_0533:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0538:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__14""
  IL_053d:  ldloc.s    V_8
  IL_053f:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0544:  brtrue     IL_0645
  IL_0549:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__13""
  IL_054e:  brtrue.s   IL_058d
  IL_0550:  ldc.i4     0x80
  IL_0555:  ldstr      ""d3""
  IL_055a:  ldtoken    ""C""
  IL_055f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0564:  ldc.i4.2
  IL_0565:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_056a:  dup
  IL_056b:  ldc.i4.0
  IL_056c:  ldc.i4.0
  IL_056d:  ldnull
  IL_056e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0573:  stelem.ref
  IL_0574:  dup
  IL_0575:  ldc.i4.1
  IL_0576:  ldc.i4.0
  IL_0577:  ldnull
  IL_0578:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_057d:  stelem.ref
  IL_057e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0583:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0588:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__13""
  IL_058d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__13""
  IL_0592:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0597:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__13""
  IL_059c:  ldloc.s    V_8
  IL_059e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__12""
  IL_05a3:  brtrue.s   IL_05db
  IL_05a5:  ldc.i4.0
  IL_05a6:  ldc.i4.s   63
  IL_05a8:  ldtoken    ""C""
  IL_05ad:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_05b2:  ldc.i4.2
  IL_05b3:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_05b8:  dup
  IL_05b9:  ldc.i4.0
  IL_05ba:  ldc.i4.0
  IL_05bb:  ldnull
  IL_05bc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_05c1:  stelem.ref
  IL_05c2:  dup
  IL_05c3:  ldc.i4.1
  IL_05c4:  ldc.i4.1
  IL_05c5:  ldnull
  IL_05c6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_05cb:  stelem.ref
  IL_05cc:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_05d1:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_05d6:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__12""
  IL_05db:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__12""
  IL_05e0:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_05e5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__12""
  IL_05ea:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__11""
  IL_05ef:  brtrue.s   IL_0620
  IL_05f1:  ldc.i4.0
  IL_05f2:  ldstr      ""d3""
  IL_05f7:  ldtoken    ""C""
  IL_05fc:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0601:  ldc.i4.1
  IL_0602:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0607:  dup
  IL_0608:  ldc.i4.0
  IL_0609:  ldc.i4.0
  IL_060a:  ldnull
  IL_060b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0610:  stelem.ref
  IL_0611:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0616:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_061b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__11""
  IL_0620:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__11""
  IL_0625:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_062a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__11""
  IL_062f:  ldloc.s    V_8
  IL_0631:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0636:  ldloc.s    V_10
  IL_0638:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_063d:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0642:  pop
  IL_0643:  br.s       IL_06a3
  IL_0645:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__15""
  IL_064a:  brtrue.s   IL_068a
  IL_064c:  ldc.i4     0x104
  IL_0651:  ldstr      ""add_d3""
  IL_0656:  ldnull
  IL_0657:  ldtoken    ""C""
  IL_065c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0661:  ldc.i4.2
  IL_0662:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0667:  dup
  IL_0668:  ldc.i4.0
  IL_0669:  ldc.i4.0
  IL_066a:  ldnull
  IL_066b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0670:  stelem.ref
  IL_0671:  dup
  IL_0672:  ldc.i4.1
  IL_0673:  ldc.i4.1
  IL_0674:  ldnull
  IL_0675:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_067a:  stelem.ref
  IL_067b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0680:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0685:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__15""
  IL_068a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__15""
  IL_068f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_0694:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__15""
  IL_0699:  ldloc.s    V_8
  IL_069b:  ldloc.s    V_10
  IL_069d:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_06a2:  pop
  IL_06a3:  ldloc.1
  IL_06a4:  stloc.s    V_7
  IL_06a6:  ldloc.s    V_4
  IL_06a8:  stloc.s    V_8
  IL_06aa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__19""
  IL_06af:  brtrue.s   IL_06d0
  IL_06b1:  ldc.i4.0
  IL_06b2:  ldstr      ""d1""
  IL_06b7:  ldtoken    ""C""
  IL_06bc:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_06c1:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_06c6:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_06cb:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__19""
  IL_06d0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__19""
  IL_06d5:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_06da:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__19""
  IL_06df:  ldloc.s    V_8
  IL_06e1:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_06e6:  brtrue     IL_07e7
  IL_06eb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__18""
  IL_06f0:  brtrue.s   IL_072f
  IL_06f2:  ldc.i4     0x80
  IL_06f7:  ldstr      ""d1""
  IL_06fc:  ldtoken    ""C""
  IL_0701:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0706:  ldc.i4.2
  IL_0707:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_070c:  dup
  IL_070d:  ldc.i4.0
  IL_070e:  ldc.i4.0
  IL_070f:  ldnull
  IL_0710:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0715:  stelem.ref
  IL_0716:  dup
  IL_0717:  ldc.i4.1
  IL_0718:  ldc.i4.0
  IL_0719:  ldnull
  IL_071a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_071f:  stelem.ref
  IL_0720:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0725:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_072a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__18""
  IL_072f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__18""
  IL_0734:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0739:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__18""
  IL_073e:  ldloc.s    V_8
  IL_0740:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__17""
  IL_0745:  brtrue.s   IL_077d
  IL_0747:  ldc.i4.0
  IL_0748:  ldc.i4.s   73
  IL_074a:  ldtoken    ""C""
  IL_074f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0754:  ldc.i4.2
  IL_0755:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_075a:  dup
  IL_075b:  ldc.i4.0
  IL_075c:  ldc.i4.0
  IL_075d:  ldnull
  IL_075e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0763:  stelem.ref
  IL_0764:  dup
  IL_0765:  ldc.i4.1
  IL_0766:  ldc.i4.1
  IL_0767:  ldnull
  IL_0768:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_076d:  stelem.ref
  IL_076e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0773:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0778:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__17""
  IL_077d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__17""
  IL_0782:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0787:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__17""
  IL_078c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__16""
  IL_0791:  brtrue.s   IL_07c2
  IL_0793:  ldc.i4.0
  IL_0794:  ldstr      ""d1""
  IL_0799:  ldtoken    ""C""
  IL_079e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_07a3:  ldc.i4.1
  IL_07a4:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_07a9:  dup
  IL_07aa:  ldc.i4.0
  IL_07ab:  ldc.i4.0
  IL_07ac:  ldnull
  IL_07ad:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_07b2:  stelem.ref
  IL_07b3:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_07b8:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_07bd:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__16""
  IL_07c2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__16""
  IL_07c7:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_07cc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__16""
  IL_07d1:  ldloc.s    V_8
  IL_07d3:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_07d8:  ldloc.s    V_7
  IL_07da:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_07df:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_07e4:  pop
  IL_07e5:  br.s       IL_0845
  IL_07e7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__20""
  IL_07ec:  brtrue.s   IL_082c
  IL_07ee:  ldc.i4     0x104
  IL_07f3:  ldstr      ""remove_d1""
  IL_07f8:  ldnull
  IL_07f9:  ldtoken    ""C""
  IL_07fe:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0803:  ldc.i4.2
  IL_0804:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0809:  dup
  IL_080a:  ldc.i4.0
  IL_080b:  ldc.i4.0
  IL_080c:  ldnull
  IL_080d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0812:  stelem.ref
  IL_0813:  dup
  IL_0814:  ldc.i4.1
  IL_0815:  ldc.i4.1
  IL_0816:  ldnull
  IL_0817:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_081c:  stelem.ref
  IL_081d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0822:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0827:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__20""
  IL_082c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__20""
  IL_0831:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0836:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__20""
  IL_083b:  ldloc.s    V_8
  IL_083d:  ldloc.s    V_7
  IL_083f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0844:  pop
  IL_0845:  ldloc.2
  IL_0846:  stloc.s    V_9
  IL_0848:  ldloc.s    V_4
  IL_084a:  stloc.s    V_8
  IL_084c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__24""
  IL_0851:  brtrue.s   IL_0872
  IL_0853:  ldc.i4.0
  IL_0854:  ldstr      ""d2""
  IL_0859:  ldtoken    ""C""
  IL_085e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0863:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0868:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_086d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__24""
  IL_0872:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__24""
  IL_0877:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_087c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__24""
  IL_0881:  ldloc.s    V_8
  IL_0883:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0888:  brtrue     IL_0989
  IL_088d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__23""
  IL_0892:  brtrue.s   IL_08d1
  IL_0894:  ldc.i4     0x80
  IL_0899:  ldstr      ""d2""
  IL_089e:  ldtoken    ""C""
  IL_08a3:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_08a8:  ldc.i4.2
  IL_08a9:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_08ae:  dup
  IL_08af:  ldc.i4.0
  IL_08b0:  ldc.i4.0
  IL_08b1:  ldnull
  IL_08b2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_08b7:  stelem.ref
  IL_08b8:  dup
  IL_08b9:  ldc.i4.1
  IL_08ba:  ldc.i4.0
  IL_08bb:  ldnull
  IL_08bc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_08c1:  stelem.ref
  IL_08c2:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_08c7:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_08cc:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__23""
  IL_08d1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__23""
  IL_08d6:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_08db:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__23""
  IL_08e0:  ldloc.s    V_8
  IL_08e2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__22""
  IL_08e7:  brtrue.s   IL_091f
  IL_08e9:  ldc.i4.0
  IL_08ea:  ldc.i4.s   73
  IL_08ec:  ldtoken    ""C""
  IL_08f1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_08f6:  ldc.i4.2
  IL_08f7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_08fc:  dup
  IL_08fd:  ldc.i4.0
  IL_08fe:  ldc.i4.0
  IL_08ff:  ldnull
  IL_0900:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0905:  stelem.ref
  IL_0906:  dup
  IL_0907:  ldc.i4.1
  IL_0908:  ldc.i4.1
  IL_0909:  ldnull
  IL_090a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_090f:  stelem.ref
  IL_0910:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0915:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_091a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__22""
  IL_091f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__22""
  IL_0924:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0929:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__22""
  IL_092e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__21""
  IL_0933:  brtrue.s   IL_0964
  IL_0935:  ldc.i4.0
  IL_0936:  ldstr      ""d2""
  IL_093b:  ldtoken    ""C""
  IL_0940:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0945:  ldc.i4.1
  IL_0946:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_094b:  dup
  IL_094c:  ldc.i4.0
  IL_094d:  ldc.i4.0
  IL_094e:  ldnull
  IL_094f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0954:  stelem.ref
  IL_0955:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_095a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_095f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__21""
  IL_0964:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__21""
  IL_0969:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_096e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__21""
  IL_0973:  ldloc.s    V_8
  IL_0975:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_097a:  ldloc.s    V_9
  IL_097c:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0981:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0986:  pop
  IL_0987:  br.s       IL_09e7
  IL_0989:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__25""
  IL_098e:  brtrue.s   IL_09ce
  IL_0990:  ldc.i4     0x104
  IL_0995:  ldstr      ""remove_d2""
  IL_099a:  ldnull
  IL_099b:  ldtoken    ""C""
  IL_09a0:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_09a5:  ldc.i4.2
  IL_09a6:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_09ab:  dup
  IL_09ac:  ldc.i4.0
  IL_09ad:  ldc.i4.0
  IL_09ae:  ldnull
  IL_09af:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_09b4:  stelem.ref
  IL_09b5:  dup
  IL_09b6:  ldc.i4.1
  IL_09b7:  ldc.i4.1
  IL_09b8:  ldnull
  IL_09b9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_09be:  stelem.ref
  IL_09bf:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_09c4:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_09c9:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__25""
  IL_09ce:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__25""
  IL_09d3:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_09d8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__25""
  IL_09dd:  ldloc.s    V_8
  IL_09df:  ldloc.s    V_9
  IL_09e1:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_09e6:  pop
  IL_09e7:  ldloc.3
  IL_09e8:  stloc.s    V_10
  IL_09ea:  ldloc.s    V_4
  IL_09ec:  stloc.s    V_8
  IL_09ee:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__29""
  IL_09f3:  brtrue.s   IL_0a14
  IL_09f5:  ldc.i4.0
  IL_09f6:  ldstr      ""d3""
  IL_09fb:  ldtoken    ""C""
  IL_0a00:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0a05:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0a0a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0a0f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__29""
  IL_0a14:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__29""
  IL_0a19:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0a1e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__29""
  IL_0a23:  ldloc.s    V_8
  IL_0a25:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0a2a:  brtrue     IL_0b2b
  IL_0a2f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__28""
  IL_0a34:  brtrue.s   IL_0a73
  IL_0a36:  ldc.i4     0x80
  IL_0a3b:  ldstr      ""d3""
  IL_0a40:  ldtoken    ""C""
  IL_0a45:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0a4a:  ldc.i4.2
  IL_0a4b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0a50:  dup
  IL_0a51:  ldc.i4.0
  IL_0a52:  ldc.i4.0
  IL_0a53:  ldnull
  IL_0a54:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0a59:  stelem.ref
  IL_0a5a:  dup
  IL_0a5b:  ldc.i4.1
  IL_0a5c:  ldc.i4.0
  IL_0a5d:  ldnull
  IL_0a5e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0a63:  stelem.ref
  IL_0a64:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0a69:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0a6e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__28""
  IL_0a73:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__28""
  IL_0a78:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0a7d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__28""
  IL_0a82:  ldloc.s    V_8
  IL_0a84:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__27""
  IL_0a89:  brtrue.s   IL_0ac1
  IL_0a8b:  ldc.i4.0
  IL_0a8c:  ldc.i4.s   73
  IL_0a8e:  ldtoken    ""C""
  IL_0a93:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0a98:  ldc.i4.2
  IL_0a99:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0a9e:  dup
  IL_0a9f:  ldc.i4.0
  IL_0aa0:  ldc.i4.0
  IL_0aa1:  ldnull
  IL_0aa2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0aa7:  stelem.ref
  IL_0aa8:  dup
  IL_0aa9:  ldc.i4.1
  IL_0aaa:  ldc.i4.1
  IL_0aab:  ldnull
  IL_0aac:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ab1:  stelem.ref
  IL_0ab2:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0ab7:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0abc:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__27""
  IL_0ac1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__27""
  IL_0ac6:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_0acb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__27""
  IL_0ad0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__26""
  IL_0ad5:  brtrue.s   IL_0b06
  IL_0ad7:  ldc.i4.0
  IL_0ad8:  ldstr      ""d3""
  IL_0add:  ldtoken    ""C""
  IL_0ae2:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0ae7:  ldc.i4.1
  IL_0ae8:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0aed:  dup
  IL_0aee:  ldc.i4.0
  IL_0aef:  ldc.i4.0
  IL_0af0:  ldnull
  IL_0af1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0af6:  stelem.ref
  IL_0af7:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0afc:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0b01:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__26""
  IL_0b06:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__26""
  IL_0b0b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0b10:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__26""
  IL_0b15:  ldloc.s    V_8
  IL_0b17:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0b1c:  ldloc.s    V_10
  IL_0b1e:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0b23:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0b28:  pop
  IL_0b29:  br.s       IL_0b89
  IL_0b2b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__30""
  IL_0b30:  brtrue.s   IL_0b70
  IL_0b32:  ldc.i4     0x104
  IL_0b37:  ldstr      ""remove_d3""
  IL_0b3c:  ldnull
  IL_0b3d:  ldtoken    ""C""
  IL_0b42:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0b47:  ldc.i4.2
  IL_0b48:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0b4d:  dup
  IL_0b4e:  ldc.i4.0
  IL_0b4f:  ldc.i4.0
  IL_0b50:  ldnull
  IL_0b51:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0b56:  stelem.ref
  IL_0b57:  dup
  IL_0b58:  ldc.i4.1
  IL_0b59:  ldc.i4.1
  IL_0b5a:  ldnull
  IL_0b5b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0b60:  stelem.ref
  IL_0b61:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0b66:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0b6b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__30""
  IL_0b70:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__30""
  IL_0b75:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_0b7a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__30""
  IL_0b7f:  ldloc.s    V_8
  IL_0b81:  ldloc.s    V_10
  IL_0b83:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0b88:  pop
  IL_0b89:  ldloc.2
  IL_0b8a:  stloc.s    V_9
  IL_0b8c:  ldloc.s    V_4
  IL_0b8e:  stloc.s    V_8
  IL_0b90:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__34""
  IL_0b95:  brtrue.s   IL_0bb6
  IL_0b97:  ldc.i4.0
  IL_0b98:  ldstr      ""d1""
  IL_0b9d:  ldtoken    ""C""
  IL_0ba2:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0ba7:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0bac:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0bb1:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__34""
  IL_0bb6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__34""
  IL_0bbb:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0bc0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__34""
  IL_0bc5:  ldloc.s    V_8
  IL_0bc7:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0bcc:  brtrue     IL_0ccd
  IL_0bd1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__33""
  IL_0bd6:  brtrue.s   IL_0c15
  IL_0bd8:  ldc.i4     0x80
  IL_0bdd:  ldstr      ""d1""
  IL_0be2:  ldtoken    ""C""
  IL_0be7:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0bec:  ldc.i4.2
  IL_0bed:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0bf2:  dup
  IL_0bf3:  ldc.i4.0
  IL_0bf4:  ldc.i4.0
  IL_0bf5:  ldnull
  IL_0bf6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0bfb:  stelem.ref
  IL_0bfc:  dup
  IL_0bfd:  ldc.i4.1
  IL_0bfe:  ldc.i4.0
  IL_0bff:  ldnull
  IL_0c00:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0c05:  stelem.ref
  IL_0c06:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0c0b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0c10:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__33""
  IL_0c15:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__33""
  IL_0c1a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0c1f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__33""
  IL_0c24:  ldloc.s    V_8
  IL_0c26:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__32""
  IL_0c2b:  brtrue.s   IL_0c63
  IL_0c2d:  ldc.i4.0
  IL_0c2e:  ldc.i4.s   63
  IL_0c30:  ldtoken    ""C""
  IL_0c35:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0c3a:  ldc.i4.2
  IL_0c3b:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0c40:  dup
  IL_0c41:  ldc.i4.0
  IL_0c42:  ldc.i4.0
  IL_0c43:  ldnull
  IL_0c44:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0c49:  stelem.ref
  IL_0c4a:  dup
  IL_0c4b:  ldc.i4.1
  IL_0c4c:  ldc.i4.1
  IL_0c4d:  ldnull
  IL_0c4e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0c53:  stelem.ref
  IL_0c54:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0c59:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0c5e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__32""
  IL_0c63:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__32""
  IL_0c68:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0c6d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__32""
  IL_0c72:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__31""
  IL_0c77:  brtrue.s   IL_0ca8
  IL_0c79:  ldc.i4.0
  IL_0c7a:  ldstr      ""d1""
  IL_0c7f:  ldtoken    ""C""
  IL_0c84:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0c89:  ldc.i4.1
  IL_0c8a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0c8f:  dup
  IL_0c90:  ldc.i4.0
  IL_0c91:  ldc.i4.0
  IL_0c92:  ldnull
  IL_0c93:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0c98:  stelem.ref
  IL_0c99:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0c9e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0ca3:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__31""
  IL_0ca8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__31""
  IL_0cad:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0cb2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__31""
  IL_0cb7:  ldloc.s    V_8
  IL_0cb9:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0cbe:  ldloc.s    V_9
  IL_0cc0:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0cc5:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0cca:  pop
  IL_0ccb:  br.s       IL_0d2b
  IL_0ccd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__35""
  IL_0cd2:  brtrue.s   IL_0d12
  IL_0cd4:  ldc.i4     0x104
  IL_0cd9:  ldstr      ""add_d1""
  IL_0cde:  ldnull
  IL_0cdf:  ldtoken    ""C""
  IL_0ce4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0ce9:  ldc.i4.2
  IL_0cea:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0cef:  dup
  IL_0cf0:  ldc.i4.0
  IL_0cf1:  ldc.i4.0
  IL_0cf2:  ldnull
  IL_0cf3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0cf8:  stelem.ref
  IL_0cf9:  dup
  IL_0cfa:  ldc.i4.1
  IL_0cfb:  ldc.i4.1
  IL_0cfc:  ldnull
  IL_0cfd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0d02:  stelem.ref
  IL_0d03:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0d08:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0d0d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__35""
  IL_0d12:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__35""
  IL_0d17:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0d1c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__35""
  IL_0d21:  ldloc.s    V_8
  IL_0d23:  ldloc.s    V_9
  IL_0d25:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0d2a:  pop
  IL_0d2b:  ldloc.0
  IL_0d2c:  stloc.s    V_4
  IL_0d2e:  ldloc.1
  IL_0d2f:  stloc.s    V_7
  IL_0d31:  ldloc.s    V_4
  IL_0d33:  stloc.s    V_8
  IL_0d35:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__39""
  IL_0d3a:  brtrue.s   IL_0d5b
  IL_0d3c:  ldc.i4.0
  IL_0d3d:  ldstr      ""d1""
  IL_0d42:  ldtoken    ""C""
  IL_0d47:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0d4c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0d51:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0d56:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__39""
  IL_0d5b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__39""
  IL_0d60:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0d65:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__39""
  IL_0d6a:  ldloc.s    V_8
  IL_0d6c:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0d71:  brtrue     IL_0e72
  IL_0d76:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__38""
  IL_0d7b:  brtrue.s   IL_0dba
  IL_0d7d:  ldc.i4     0x80
  IL_0d82:  ldstr      ""d1""
  IL_0d87:  ldtoken    ""C""
  IL_0d8c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0d91:  ldc.i4.2
  IL_0d92:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0d97:  dup
  IL_0d98:  ldc.i4.0
  IL_0d99:  ldc.i4.0
  IL_0d9a:  ldnull
  IL_0d9b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0da0:  stelem.ref
  IL_0da1:  dup
  IL_0da2:  ldc.i4.1
  IL_0da3:  ldc.i4.0
  IL_0da4:  ldnull
  IL_0da5:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0daa:  stelem.ref
  IL_0dab:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0db0:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0db5:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__38""
  IL_0dba:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__38""
  IL_0dbf:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0dc4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__38""
  IL_0dc9:  ldloc.s    V_8
  IL_0dcb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__37""
  IL_0dd0:  brtrue.s   IL_0e08
  IL_0dd2:  ldc.i4.0
  IL_0dd3:  ldc.i4.s   63
  IL_0dd5:  ldtoken    ""C""
  IL_0dda:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0ddf:  ldc.i4.2
  IL_0de0:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0de5:  dup
  IL_0de6:  ldc.i4.0
  IL_0de7:  ldc.i4.0
  IL_0de8:  ldnull
  IL_0de9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0dee:  stelem.ref
  IL_0def:  dup
  IL_0df0:  ldc.i4.1
  IL_0df1:  ldc.i4.1
  IL_0df2:  ldnull
  IL_0df3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0df8:  stelem.ref
  IL_0df9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0dfe:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0e03:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__37""
  IL_0e08:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__37""
  IL_0e0d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0e12:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__37""
  IL_0e17:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__36""
  IL_0e1c:  brtrue.s   IL_0e4d
  IL_0e1e:  ldc.i4.0
  IL_0e1f:  ldstr      ""d1""
  IL_0e24:  ldtoken    ""C""
  IL_0e29:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0e2e:  ldc.i4.1
  IL_0e2f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0e34:  dup
  IL_0e35:  ldc.i4.0
  IL_0e36:  ldc.i4.0
  IL_0e37:  ldnull
  IL_0e38:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0e3d:  stelem.ref
  IL_0e3e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0e43:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0e48:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__36""
  IL_0e4d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__36""
  IL_0e52:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0e57:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__36""
  IL_0e5c:  ldloc.s    V_8
  IL_0e5e:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0e63:  ldloc.s    V_7
  IL_0e65:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0e6a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0e6f:  pop
  IL_0e70:  br.s       IL_0ed0
  IL_0e72:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__40""
  IL_0e77:  brtrue.s   IL_0eb7
  IL_0e79:  ldc.i4     0x104
  IL_0e7e:  ldstr      ""add_d1""
  IL_0e83:  ldnull
  IL_0e84:  ldtoken    ""C""
  IL_0e89:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0e8e:  ldc.i4.2
  IL_0e8f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0e94:  dup
  IL_0e95:  ldc.i4.0
  IL_0e96:  ldc.i4.0
  IL_0e97:  ldnull
  IL_0e98:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0e9d:  stelem.ref
  IL_0e9e:  dup
  IL_0e9f:  ldc.i4.1
  IL_0ea0:  ldc.i4.1
  IL_0ea1:  ldnull
  IL_0ea2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ea7:  stelem.ref
  IL_0ea8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0ead:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0eb2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__40""
  IL_0eb7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__40""
  IL_0ebc:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0ec1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__40""
  IL_0ec6:  ldloc.s    V_8
  IL_0ec8:  ldloc.s    V_7
  IL_0eca:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0ecf:  pop
  IL_0ed0:  ldloc.2
  IL_0ed1:  stloc.s    V_9
  IL_0ed3:  ldloc.s    V_4
  IL_0ed5:  stloc.s    V_8
  IL_0ed7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__44""
  IL_0edc:  brtrue.s   IL_0efd
  IL_0ede:  ldc.i4.0
  IL_0edf:  ldstr      ""d2""
  IL_0ee4:  ldtoken    ""C""
  IL_0ee9:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0eee:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0ef3:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0ef8:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__44""
  IL_0efd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__44""
  IL_0f02:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0f07:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__44""
  IL_0f0c:  ldloc.s    V_8
  IL_0f0e:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0f13:  brtrue     IL_1014
  IL_0f18:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__43""
  IL_0f1d:  brtrue.s   IL_0f5c
  IL_0f1f:  ldc.i4     0x80
  IL_0f24:  ldstr      ""d2""
  IL_0f29:  ldtoken    ""C""
  IL_0f2e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0f33:  ldc.i4.2
  IL_0f34:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0f39:  dup
  IL_0f3a:  ldc.i4.0
  IL_0f3b:  ldc.i4.0
  IL_0f3c:  ldnull
  IL_0f3d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f42:  stelem.ref
  IL_0f43:  dup
  IL_0f44:  ldc.i4.1
  IL_0f45:  ldc.i4.0
  IL_0f46:  ldnull
  IL_0f47:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f4c:  stelem.ref
  IL_0f4d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0f52:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0f57:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__43""
  IL_0f5c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__43""
  IL_0f61:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0f66:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__43""
  IL_0f6b:  ldloc.s    V_8
  IL_0f6d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__42""
  IL_0f72:  brtrue.s   IL_0faa
  IL_0f74:  ldc.i4.0
  IL_0f75:  ldc.i4.s   63
  IL_0f77:  ldtoken    ""C""
  IL_0f7c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0f81:  ldc.i4.2
  IL_0f82:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0f87:  dup
  IL_0f88:  ldc.i4.0
  IL_0f89:  ldc.i4.0
  IL_0f8a:  ldnull
  IL_0f8b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f90:  stelem.ref
  IL_0f91:  dup
  IL_0f92:  ldc.i4.1
  IL_0f93:  ldc.i4.1
  IL_0f94:  ldnull
  IL_0f95:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f9a:  stelem.ref
  IL_0f9b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0fa0:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0fa5:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__42""
  IL_0faa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__42""
  IL_0faf:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0fb4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__42""
  IL_0fb9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__41""
  IL_0fbe:  brtrue.s   IL_0fef
  IL_0fc0:  ldc.i4.0
  IL_0fc1:  ldstr      ""d2""
  IL_0fc6:  ldtoken    ""C""
  IL_0fcb:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0fd0:  ldc.i4.1
  IL_0fd1:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0fd6:  dup
  IL_0fd7:  ldc.i4.0
  IL_0fd8:  ldc.i4.0
  IL_0fd9:  ldnull
  IL_0fda:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0fdf:  stelem.ref
  IL_0fe0:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0fe5:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0fea:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__41""
  IL_0fef:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__41""
  IL_0ff4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0ff9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__41""
  IL_0ffe:  ldloc.s    V_8
  IL_1000:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1005:  ldloc.s    V_9
  IL_1007:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_100c:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_1011:  pop
  IL_1012:  br.s       IL_1072
  IL_1014:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__45""
  IL_1019:  brtrue.s   IL_1059
  IL_101b:  ldc.i4     0x104
  IL_1020:  ldstr      ""add_d2""
  IL_1025:  ldnull
  IL_1026:  ldtoken    ""C""
  IL_102b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1030:  ldc.i4.2
  IL_1031:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1036:  dup
  IL_1037:  ldc.i4.0
  IL_1038:  ldc.i4.0
  IL_1039:  ldnull
  IL_103a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_103f:  stelem.ref
  IL_1040:  dup
  IL_1041:  ldc.i4.1
  IL_1042:  ldc.i4.1
  IL_1043:  ldnull
  IL_1044:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1049:  stelem.ref
  IL_104a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_104f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1054:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__45""
  IL_1059:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__45""
  IL_105e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_1063:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__45""
  IL_1068:  ldloc.s    V_8
  IL_106a:  ldloc.s    V_9
  IL_106c:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_1071:  pop
  IL_1072:  ldloc.3
  IL_1073:  stloc.s    V_10
  IL_1075:  ldloc.s    V_4
  IL_1077:  stloc.s    V_8
  IL_1079:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__49""
  IL_107e:  brtrue.s   IL_109f
  IL_1080:  ldc.i4.0
  IL_1081:  ldstr      ""d3""
  IL_1086:  ldtoken    ""C""
  IL_108b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1090:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_1095:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_109a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__49""
  IL_109f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__49""
  IL_10a4:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_10a9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__49""
  IL_10ae:  ldloc.s    V_8
  IL_10b0:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_10b5:  brtrue     IL_11b6
  IL_10ba:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__48""
  IL_10bf:  brtrue.s   IL_10fe
  IL_10c1:  ldc.i4     0x80
  IL_10c6:  ldstr      ""d3""
  IL_10cb:  ldtoken    ""C""
  IL_10d0:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_10d5:  ldc.i4.2
  IL_10d6:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_10db:  dup
  IL_10dc:  ldc.i4.0
  IL_10dd:  ldc.i4.0
  IL_10de:  ldnull
  IL_10df:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_10e4:  stelem.ref
  IL_10e5:  dup
  IL_10e6:  ldc.i4.1
  IL_10e7:  ldc.i4.0
  IL_10e8:  ldnull
  IL_10e9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_10ee:  stelem.ref
  IL_10ef:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_10f4:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_10f9:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__48""
  IL_10fe:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__48""
  IL_1103:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_1108:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__48""
  IL_110d:  ldloc.s    V_8
  IL_110f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__47""
  IL_1114:  brtrue.s   IL_114c
  IL_1116:  ldc.i4.0
  IL_1117:  ldc.i4.s   63
  IL_1119:  ldtoken    ""C""
  IL_111e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1123:  ldc.i4.2
  IL_1124:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1129:  dup
  IL_112a:  ldc.i4.0
  IL_112b:  ldc.i4.0
  IL_112c:  ldnull
  IL_112d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1132:  stelem.ref
  IL_1133:  dup
  IL_1134:  ldc.i4.1
  IL_1135:  ldc.i4.1
  IL_1136:  ldnull
  IL_1137:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_113c:  stelem.ref
  IL_113d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1142:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1147:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__47""
  IL_114c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__47""
  IL_1151:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_1156:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__47""
  IL_115b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__46""
  IL_1160:  brtrue.s   IL_1191
  IL_1162:  ldc.i4.0
  IL_1163:  ldstr      ""d3""
  IL_1168:  ldtoken    ""C""
  IL_116d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1172:  ldc.i4.1
  IL_1173:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1178:  dup
  IL_1179:  ldc.i4.0
  IL_117a:  ldc.i4.0
  IL_117b:  ldnull
  IL_117c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1181:  stelem.ref
  IL_1182:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1187:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_118c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__46""
  IL_1191:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__46""
  IL_1196:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_119b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__46""
  IL_11a0:  ldloc.s    V_8
  IL_11a2:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_11a7:  ldloc.s    V_10
  IL_11a9:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_11ae:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_11b3:  pop
  IL_11b4:  br.s       IL_1214
  IL_11b6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__50""
  IL_11bb:  brtrue.s   IL_11fb
  IL_11bd:  ldc.i4     0x104
  IL_11c2:  ldstr      ""add_d3""
  IL_11c7:  ldnull
  IL_11c8:  ldtoken    ""C""
  IL_11cd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_11d2:  ldc.i4.2
  IL_11d3:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_11d8:  dup
  IL_11d9:  ldc.i4.0
  IL_11da:  ldc.i4.0
  IL_11db:  ldnull
  IL_11dc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_11e1:  stelem.ref
  IL_11e2:  dup
  IL_11e3:  ldc.i4.1
  IL_11e4:  ldc.i4.1
  IL_11e5:  ldnull
  IL_11e6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_11eb:  stelem.ref
  IL_11ec:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_11f1:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_11f6:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__50""
  IL_11fb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__50""
  IL_1200:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_1205:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__50""
  IL_120a:  ldloc.s    V_8
  IL_120c:  ldloc.s    V_10
  IL_120e:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_1213:  pop
  IL_1214:  ldloc.1
  IL_1215:  stloc.s    V_7
  IL_1217:  ldloc.s    V_4
  IL_1219:  stloc.s    V_8
  IL_121b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__54""
  IL_1220:  brtrue.s   IL_1241
  IL_1222:  ldc.i4.0
  IL_1223:  ldstr      ""d1""
  IL_1228:  ldtoken    ""C""
  IL_122d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1232:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_1237:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_123c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__54""
  IL_1241:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__54""
  IL_1246:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_124b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__54""
  IL_1250:  ldloc.s    V_8
  IL_1252:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1257:  brtrue     IL_1358
  IL_125c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__53""
  IL_1261:  brtrue.s   IL_12a0
  IL_1263:  ldc.i4     0x80
  IL_1268:  ldstr      ""d1""
  IL_126d:  ldtoken    ""C""
  IL_1272:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1277:  ldc.i4.2
  IL_1278:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_127d:  dup
  IL_127e:  ldc.i4.0
  IL_127f:  ldc.i4.0
  IL_1280:  ldnull
  IL_1281:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1286:  stelem.ref
  IL_1287:  dup
  IL_1288:  ldc.i4.1
  IL_1289:  ldc.i4.0
  IL_128a:  ldnull
  IL_128b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1290:  stelem.ref
  IL_1291:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1296:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_129b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__53""
  IL_12a0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__53""
  IL_12a5:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_12aa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__53""
  IL_12af:  ldloc.s    V_8
  IL_12b1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__52""
  IL_12b6:  brtrue.s   IL_12ee
  IL_12b8:  ldc.i4.0
  IL_12b9:  ldc.i4.s   73
  IL_12bb:  ldtoken    ""C""
  IL_12c0:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_12c5:  ldc.i4.2
  IL_12c6:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_12cb:  dup
  IL_12cc:  ldc.i4.0
  IL_12cd:  ldc.i4.0
  IL_12ce:  ldnull
  IL_12cf:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_12d4:  stelem.ref
  IL_12d5:  dup
  IL_12d6:  ldc.i4.1
  IL_12d7:  ldc.i4.1
  IL_12d8:  ldnull
  IL_12d9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_12de:  stelem.ref
  IL_12df:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_12e4:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_12e9:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__52""
  IL_12ee:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__52""
  IL_12f3:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_12f8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__52""
  IL_12fd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__51""
  IL_1302:  brtrue.s   IL_1333
  IL_1304:  ldc.i4.0
  IL_1305:  ldstr      ""d1""
  IL_130a:  ldtoken    ""C""
  IL_130f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1314:  ldc.i4.1
  IL_1315:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_131a:  dup
  IL_131b:  ldc.i4.0
  IL_131c:  ldc.i4.0
  IL_131d:  ldnull
  IL_131e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1323:  stelem.ref
  IL_1324:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1329:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_132e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__51""
  IL_1333:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__51""
  IL_1338:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_133d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__51""
  IL_1342:  ldloc.s    V_8
  IL_1344:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1349:  ldloc.s    V_7
  IL_134b:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_1350:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_1355:  pop
  IL_1356:  br.s       IL_13b6
  IL_1358:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__55""
  IL_135d:  brtrue.s   IL_139d
  IL_135f:  ldc.i4     0x104
  IL_1364:  ldstr      ""remove_d1""
  IL_1369:  ldnull
  IL_136a:  ldtoken    ""C""
  IL_136f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1374:  ldc.i4.2
  IL_1375:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_137a:  dup
  IL_137b:  ldc.i4.0
  IL_137c:  ldc.i4.0
  IL_137d:  ldnull
  IL_137e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1383:  stelem.ref
  IL_1384:  dup
  IL_1385:  ldc.i4.1
  IL_1386:  ldc.i4.1
  IL_1387:  ldnull
  IL_1388:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_138d:  stelem.ref
  IL_138e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1393:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1398:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__55""
  IL_139d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__55""
  IL_13a2:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_13a7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__55""
  IL_13ac:  ldloc.s    V_8
  IL_13ae:  ldloc.s    V_7
  IL_13b0:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_13b5:  pop
  IL_13b6:  ldloc.2
  IL_13b7:  stloc.s    V_9
  IL_13b9:  ldloc.s    V_4
  IL_13bb:  stloc.s    V_8
  IL_13bd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__59""
  IL_13c2:  brtrue.s   IL_13e3
  IL_13c4:  ldc.i4.0
  IL_13c5:  ldstr      ""d2""
  IL_13ca:  ldtoken    ""C""
  IL_13cf:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_13d4:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_13d9:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_13de:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__59""
  IL_13e3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__59""
  IL_13e8:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_13ed:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__59""
  IL_13f2:  ldloc.s    V_8
  IL_13f4:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_13f9:  brtrue     IL_14fa
  IL_13fe:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__58""
  IL_1403:  brtrue.s   IL_1442
  IL_1405:  ldc.i4     0x80
  IL_140a:  ldstr      ""d2""
  IL_140f:  ldtoken    ""C""
  IL_1414:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1419:  ldc.i4.2
  IL_141a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_141f:  dup
  IL_1420:  ldc.i4.0
  IL_1421:  ldc.i4.0
  IL_1422:  ldnull
  IL_1423:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1428:  stelem.ref
  IL_1429:  dup
  IL_142a:  ldc.i4.1
  IL_142b:  ldc.i4.0
  IL_142c:  ldnull
  IL_142d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1432:  stelem.ref
  IL_1433:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1438:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_143d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__58""
  IL_1442:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__58""
  IL_1447:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_144c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__58""
  IL_1451:  ldloc.s    V_8
  IL_1453:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__57""
  IL_1458:  brtrue.s   IL_1490
  IL_145a:  ldc.i4.0
  IL_145b:  ldc.i4.s   73
  IL_145d:  ldtoken    ""C""
  IL_1462:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1467:  ldc.i4.2
  IL_1468:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_146d:  dup
  IL_146e:  ldc.i4.0
  IL_146f:  ldc.i4.0
  IL_1470:  ldnull
  IL_1471:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1476:  stelem.ref
  IL_1477:  dup
  IL_1478:  ldc.i4.1
  IL_1479:  ldc.i4.1
  IL_147a:  ldnull
  IL_147b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1480:  stelem.ref
  IL_1481:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1486:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_148b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__57""
  IL_1490:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__57""
  IL_1495:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_149a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__57""
  IL_149f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__56""
  IL_14a4:  brtrue.s   IL_14d5
  IL_14a6:  ldc.i4.0
  IL_14a7:  ldstr      ""d2""
  IL_14ac:  ldtoken    ""C""
  IL_14b1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_14b6:  ldc.i4.1
  IL_14b7:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_14bc:  dup
  IL_14bd:  ldc.i4.0
  IL_14be:  ldc.i4.0
  IL_14bf:  ldnull
  IL_14c0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_14c5:  stelem.ref
  IL_14c6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_14cb:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_14d0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__56""
  IL_14d5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__56""
  IL_14da:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_14df:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__56""
  IL_14e4:  ldloc.s    V_8
  IL_14e6:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_14eb:  ldloc.s    V_9
  IL_14ed:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_14f2:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_14f7:  pop
  IL_14f8:  br.s       IL_1558
  IL_14fa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__60""
  IL_14ff:  brtrue.s   IL_153f
  IL_1501:  ldc.i4     0x104
  IL_1506:  ldstr      ""remove_d2""
  IL_150b:  ldnull
  IL_150c:  ldtoken    ""C""
  IL_1511:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1516:  ldc.i4.2
  IL_1517:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_151c:  dup
  IL_151d:  ldc.i4.0
  IL_151e:  ldc.i4.0
  IL_151f:  ldnull
  IL_1520:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1525:  stelem.ref
  IL_1526:  dup
  IL_1527:  ldc.i4.1
  IL_1528:  ldc.i4.1
  IL_1529:  ldnull
  IL_152a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_152f:  stelem.ref
  IL_1530:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1535:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_153a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__60""
  IL_153f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__60""
  IL_1544:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_1549:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__60""
  IL_154e:  ldloc.s    V_8
  IL_1550:  ldloc.s    V_9
  IL_1552:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_1557:  pop
  IL_1558:  ldloc.3
  IL_1559:  stloc.s    V_10
  IL_155b:  ldloc.s    V_4
  IL_155d:  stloc.s    V_8
  IL_155f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__64""
  IL_1564:  brtrue.s   IL_1585
  IL_1566:  ldc.i4.0
  IL_1567:  ldstr      ""d3""
  IL_156c:  ldtoken    ""C""
  IL_1571:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1576:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_157b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1580:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__64""
  IL_1585:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__64""
  IL_158a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_158f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__64""
  IL_1594:  ldloc.s    V_8
  IL_1596:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_159b:  brtrue     IL_169c
  IL_15a0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__63""
  IL_15a5:  brtrue.s   IL_15e4
  IL_15a7:  ldc.i4     0x80
  IL_15ac:  ldstr      ""d3""
  IL_15b1:  ldtoken    ""C""
  IL_15b6:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_15bb:  ldc.i4.2
  IL_15bc:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_15c1:  dup
  IL_15c2:  ldc.i4.0
  IL_15c3:  ldc.i4.0
  IL_15c4:  ldnull
  IL_15c5:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_15ca:  stelem.ref
  IL_15cb:  dup
  IL_15cc:  ldc.i4.1
  IL_15cd:  ldc.i4.0
  IL_15ce:  ldnull
  IL_15cf:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_15d4:  stelem.ref
  IL_15d5:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_15da:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_15df:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__63""
  IL_15e4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__63""
  IL_15e9:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_15ee:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__63""
  IL_15f3:  ldloc.s    V_8
  IL_15f5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__62""
  IL_15fa:  brtrue.s   IL_1632
  IL_15fc:  ldc.i4.0
  IL_15fd:  ldc.i4.s   73
  IL_15ff:  ldtoken    ""C""
  IL_1604:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1609:  ldc.i4.2
  IL_160a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_160f:  dup
  IL_1610:  ldc.i4.0
  IL_1611:  ldc.i4.0
  IL_1612:  ldnull
  IL_1613:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1618:  stelem.ref
  IL_1619:  dup
  IL_161a:  ldc.i4.1
  IL_161b:  ldc.i4.1
  IL_161c:  ldnull
  IL_161d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1622:  stelem.ref
  IL_1623:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1628:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_162d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__62""
  IL_1632:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__62""
  IL_1637:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_163c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__62""
  IL_1641:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__61""
  IL_1646:  brtrue.s   IL_1677
  IL_1648:  ldc.i4.0
  IL_1649:  ldstr      ""d3""
  IL_164e:  ldtoken    ""C""
  IL_1653:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1658:  ldc.i4.1
  IL_1659:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_165e:  dup
  IL_165f:  ldc.i4.0
  IL_1660:  ldc.i4.0
  IL_1661:  ldnull
  IL_1662:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1667:  stelem.ref
  IL_1668:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_166d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1672:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__61""
  IL_1677:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__61""
  IL_167c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_1681:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__61""
  IL_1686:  ldloc.s    V_8
  IL_1688:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_168d:  ldloc.s    V_10
  IL_168f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_1694:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_1699:  pop
  IL_169a:  br.s       IL_16fa
  IL_169c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__65""
  IL_16a1:  brtrue.s   IL_16e1
  IL_16a3:  ldc.i4     0x104
  IL_16a8:  ldstr      ""remove_d3""
  IL_16ad:  ldnull
  IL_16ae:  ldtoken    ""C""
  IL_16b3:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_16b8:  ldc.i4.2
  IL_16b9:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_16be:  dup
  IL_16bf:  ldc.i4.0
  IL_16c0:  ldc.i4.0
  IL_16c1:  ldnull
  IL_16c2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_16c7:  stelem.ref
  IL_16c8:  dup
  IL_16c9:  ldc.i4.1
  IL_16ca:  ldc.i4.1
  IL_16cb:  ldnull
  IL_16cc:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_16d1:  stelem.ref
  IL_16d2:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_16d7:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_16dc:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__65""
  IL_16e1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__65""
  IL_16e6:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_16eb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__65""
  IL_16f0:  ldloc.s    V_8
  IL_16f2:  ldloc.s    V_10
  IL_16f4:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_16f9:  pop
  IL_16fa:  ldloc.2
  IL_16fb:  stloc.s    V_9
  IL_16fd:  ldloc.s    V_4
  IL_16ff:  stloc.s    V_8
  IL_1701:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__69""
  IL_1706:  brtrue.s   IL_1727
  IL_1708:  ldc.i4.0
  IL_1709:  ldstr      ""d1""
  IL_170e:  ldtoken    ""C""
  IL_1713:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1718:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_171d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1722:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__69""
  IL_1727:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__69""
  IL_172c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_1731:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__69""
  IL_1736:  ldloc.s    V_8
  IL_1738:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_173d:  brtrue     IL_183d
  IL_1742:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__68""
  IL_1747:  brtrue.s   IL_1786
  IL_1749:  ldc.i4     0x80
  IL_174e:  ldstr      ""d1""
  IL_1753:  ldtoken    ""C""
  IL_1758:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_175d:  ldc.i4.2
  IL_175e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1763:  dup
  IL_1764:  ldc.i4.0
  IL_1765:  ldc.i4.0
  IL_1766:  ldnull
  IL_1767:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_176c:  stelem.ref
  IL_176d:  dup
  IL_176e:  ldc.i4.1
  IL_176f:  ldc.i4.0
  IL_1770:  ldnull
  IL_1771:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1776:  stelem.ref
  IL_1777:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_177c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1781:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__68""
  IL_1786:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__68""
  IL_178b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_1790:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__68""
  IL_1795:  ldloc.s    V_8
  IL_1797:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__67""
  IL_179c:  brtrue.s   IL_17d4
  IL_179e:  ldc.i4.0
  IL_179f:  ldc.i4.s   63
  IL_17a1:  ldtoken    ""C""
  IL_17a6:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_17ab:  ldc.i4.2
  IL_17ac:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_17b1:  dup
  IL_17b2:  ldc.i4.0
  IL_17b3:  ldc.i4.0
  IL_17b4:  ldnull
  IL_17b5:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_17ba:  stelem.ref
  IL_17bb:  dup
  IL_17bc:  ldc.i4.1
  IL_17bd:  ldc.i4.1
  IL_17be:  ldnull
  IL_17bf:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_17c4:  stelem.ref
  IL_17c5:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_17ca:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_17cf:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__67""
  IL_17d4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__67""
  IL_17d9:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_17de:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__67""
  IL_17e3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__66""
  IL_17e8:  brtrue.s   IL_1819
  IL_17ea:  ldc.i4.0
  IL_17eb:  ldstr      ""d1""
  IL_17f0:  ldtoken    ""C""
  IL_17f5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_17fa:  ldc.i4.1
  IL_17fb:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1800:  dup
  IL_1801:  ldc.i4.0
  IL_1802:  ldc.i4.0
  IL_1803:  ldnull
  IL_1804:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1809:  stelem.ref
  IL_180a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_180f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1814:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__66""
  IL_1819:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__66""
  IL_181e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_1823:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__66""
  IL_1828:  ldloc.s    V_8
  IL_182a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_182f:  ldloc.s    V_9
  IL_1831:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_1836:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_183b:  pop
  IL_183c:  ret
  IL_183d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__70""
  IL_1842:  brtrue.s   IL_1882
  IL_1844:  ldc.i4     0x104
  IL_1849:  ldstr      ""add_d1""
  IL_184e:  ldnull
  IL_184f:  ldtoken    ""C""
  IL_1854:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1859:  ldc.i4.2
  IL_185a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_185f:  dup
  IL_1860:  ldc.i4.0
  IL_1861:  ldc.i4.0
  IL_1862:  ldnull
  IL_1863:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1868:  stelem.ref
  IL_1869:  dup
  IL_186a:  ldc.i4.1
  IL_186b:  ldc.i4.1
  IL_186c:  ldnull
  IL_186d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1872:  stelem.ref
  IL_1873:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1878:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_187d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__70""
  IL_1882:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__70""
  IL_1887:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_188c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__70""
  IL_1891:  ldloc.s    V_8
  IL_1893:  ldloc.s    V_9
  IL_1895:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_189a:  pop
  IL_189b:  ret
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
                additionalRefs: new[] {
                    MscorlibRef_v4_0_30316_17626,
                    SystemCoreRef_v4_0_30319_17929,
                    _eventLibRef,
                },
                verify: OSVersion.IsWin8);

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
    public void foo(){
        Application x = null; 
        x.Suspending += (object sender, SuspendingEventArgs e) => {};
    }

    public static void Main(){
            var a = new abcdef();
            a.foo();
    }
} ";

            var cv = CompileAndVerifyOnWin8Only(text);

            cv.VerifyIL("abcdef.foo()", @"
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
  IL_002a:  ldftn      ""void abcdef.<>c.<foo>b__0_0(object, Windows.ApplicationModel.SuspendingEventArgs)""
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

            var cv = CompileAndVerifyOnWin8Only(text);

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

            var cv = CompileAndVerifyOnWin8Only(text);

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

                var comp = CreateCompilation(source, WinRtRefs.Concat(new[] { ilRef }));
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

        [Fact]
        [WorkItem(1055825)]
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
            var comp = CreateCompilation("", WinRtRefs.Concat(new[] { ilRef }), TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All));
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
