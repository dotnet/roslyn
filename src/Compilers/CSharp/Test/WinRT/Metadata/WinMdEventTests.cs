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
            _eventLibRef = CreateEmptyCompilation(
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

            var dynamicCommon = CreateEmptyCompilation(
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
  // Code size     6356 (0x18d4)
  .maxstack  11
  .locals init (B V_0, //b
                EventLibrary.voidDelegate V_1, //test
                EventLibrary.genericDelegate<object> V_2, //generic
                EventLibrary.dynamicDelegate V_3, //dyn
                object V_4, //c
                A V_5,
                B V_6,
                object V_7,
                object V_8,
                EventLibrary.voidDelegate V_9,
                EventLibrary.genericDelegate<object> V_10,
                EventLibrary.dynamicDelegate V_11)
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
  IL_01c1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__1""
  IL_01c6:  brtrue.s   IL_01f7
  IL_01c8:  ldc.i4.0
  IL_01c9:  ldstr      ""d1""
  IL_01ce:  ldtoken    ""C""
  IL_01d3:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_01d8:  ldc.i4.1
  IL_01d9:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_01de:  dup
  IL_01df:  ldc.i4.0
  IL_01e0:  ldc.i4.0
  IL_01e1:  ldnull
  IL_01e2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_01e7:  stelem.ref
  IL_01e8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_01ed:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_01f2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__1""
  IL_01f7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__1""
  IL_01fc:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0201:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__1""
  IL_0206:  ldloc.s    V_7
  IL_0208:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_020d:  stloc.s    V_8
  IL_020f:  ldloc.1
  IL_0210:  stloc.s    V_9
  IL_0212:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__4""
  IL_0217:  brtrue.s   IL_0238
  IL_0219:  ldc.i4.0
  IL_021a:  ldstr      ""d1""
  IL_021f:  ldtoken    ""C""
  IL_0224:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0229:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_022e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0233:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__4""
  IL_0238:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__4""
  IL_023d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0242:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__4""
  IL_0247:  ldloc.s    V_7
  IL_0249:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_024e:  brtrue     IL_0305
  IL_0253:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__3""
  IL_0258:  brtrue.s   IL_0297
  IL_025a:  ldc.i4     0x80
  IL_025f:  ldstr      ""d1""
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
  IL_0280:  ldc.i4.0
  IL_0281:  ldnull
  IL_0282:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0287:  stelem.ref
  IL_0288:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_028d:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0292:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__3""
  IL_0297:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__3""
  IL_029c:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_02a1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__3""
  IL_02a6:  ldloc.s    V_7
  IL_02a8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__2""
  IL_02ad:  brtrue.s   IL_02e5
  IL_02af:  ldc.i4.0
  IL_02b0:  ldc.i4.s   63
  IL_02b2:  ldtoken    ""C""
  IL_02b7:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_02bc:  ldc.i4.2
  IL_02bd:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_02c2:  dup
  IL_02c3:  ldc.i4.0
  IL_02c4:  ldc.i4.0
  IL_02c5:  ldnull
  IL_02c6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_02cb:  stelem.ref
  IL_02cc:  dup
  IL_02cd:  ldc.i4.1
  IL_02ce:  ldc.i4.1
  IL_02cf:  ldnull
  IL_02d0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_02d5:  stelem.ref
  IL_02d6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_02db:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_02e0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__2""
  IL_02e5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__2""
  IL_02ea:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_02ef:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__2""
  IL_02f4:  ldloc.s    V_8
  IL_02f6:  ldloc.s    V_9
  IL_02f8:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_02fd:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0302:  pop
  IL_0303:  br.s       IL_0363
  IL_0305:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__5""
  IL_030a:  brtrue.s   IL_034a
  IL_030c:  ldc.i4     0x104
  IL_0311:  ldstr      ""add_d1""
  IL_0316:  ldnull
  IL_0317:  ldtoken    ""C""
  IL_031c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0321:  ldc.i4.2
  IL_0322:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0327:  dup
  IL_0328:  ldc.i4.0
  IL_0329:  ldc.i4.0
  IL_032a:  ldnull
  IL_032b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0330:  stelem.ref
  IL_0331:  dup
  IL_0332:  ldc.i4.1
  IL_0333:  ldc.i4.1
  IL_0334:  ldnull
  IL_0335:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_033a:  stelem.ref
  IL_033b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0340:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0345:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__5""
  IL_034a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__5""
  IL_034f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0354:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__5""
  IL_0359:  ldloc.s    V_7
  IL_035b:  ldloc.s    V_9
  IL_035d:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0362:  pop
  IL_0363:  ldloc.s    V_4
  IL_0365:  stloc.s    V_8
  IL_0367:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__6""
  IL_036c:  brtrue.s   IL_039d
  IL_036e:  ldc.i4.0
  IL_036f:  ldstr      ""d2""
  IL_0374:  ldtoken    ""C""
  IL_0379:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_037e:  ldc.i4.1
  IL_037f:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0384:  dup
  IL_0385:  ldc.i4.0
  IL_0386:  ldc.i4.0
  IL_0387:  ldnull
  IL_0388:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_038d:  stelem.ref
  IL_038e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0393:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0398:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__6""
  IL_039d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__6""
  IL_03a2:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_03a7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__6""
  IL_03ac:  ldloc.s    V_8
  IL_03ae:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_03b3:  stloc.s    V_7
  IL_03b5:  ldloc.2
  IL_03b6:  stloc.s    V_10
  IL_03b8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__9""
  IL_03bd:  brtrue.s   IL_03de
  IL_03bf:  ldc.i4.0
  IL_03c0:  ldstr      ""d2""
  IL_03c5:  ldtoken    ""C""
  IL_03ca:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_03cf:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_03d4:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_03d9:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__9""
  IL_03de:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__9""
  IL_03e3:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_03e8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__9""
  IL_03ed:  ldloc.s    V_8
  IL_03ef:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_03f4:  brtrue     IL_04ab
  IL_03f9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__8""
  IL_03fe:  brtrue.s   IL_043d
  IL_0400:  ldc.i4     0x80
  IL_0405:  ldstr      ""d2""
  IL_040a:  ldtoken    ""C""
  IL_040f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0414:  ldc.i4.2
  IL_0415:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_041a:  dup
  IL_041b:  ldc.i4.0
  IL_041c:  ldc.i4.0
  IL_041d:  ldnull
  IL_041e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0423:  stelem.ref
  IL_0424:  dup
  IL_0425:  ldc.i4.1
  IL_0426:  ldc.i4.0
  IL_0427:  ldnull
  IL_0428:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_042d:  stelem.ref
  IL_042e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0433:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0438:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__8""
  IL_043d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__8""
  IL_0442:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0447:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__8""
  IL_044c:  ldloc.s    V_8
  IL_044e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__7""
  IL_0453:  brtrue.s   IL_048b
  IL_0455:  ldc.i4.0
  IL_0456:  ldc.i4.s   63
  IL_0458:  ldtoken    ""C""
  IL_045d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0462:  ldc.i4.2
  IL_0463:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0468:  dup
  IL_0469:  ldc.i4.0
  IL_046a:  ldc.i4.0
  IL_046b:  ldnull
  IL_046c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0471:  stelem.ref
  IL_0472:  dup
  IL_0473:  ldc.i4.1
  IL_0474:  ldc.i4.1
  IL_0475:  ldnull
  IL_0476:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_047b:  stelem.ref
  IL_047c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0481:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0486:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__7""
  IL_048b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__7""
  IL_0490:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0495:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__7""
  IL_049a:  ldloc.s    V_7
  IL_049c:  ldloc.s    V_10
  IL_049e:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_04a3:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_04a8:  pop
  IL_04a9:  br.s       IL_0509
  IL_04ab:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__10""
  IL_04b0:  brtrue.s   IL_04f0
  IL_04b2:  ldc.i4     0x104
  IL_04b7:  ldstr      ""add_d2""
  IL_04bc:  ldnull
  IL_04bd:  ldtoken    ""C""
  IL_04c2:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_04c7:  ldc.i4.2
  IL_04c8:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_04cd:  dup
  IL_04ce:  ldc.i4.0
  IL_04cf:  ldc.i4.0
  IL_04d0:  ldnull
  IL_04d1:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_04d6:  stelem.ref
  IL_04d7:  dup
  IL_04d8:  ldc.i4.1
  IL_04d9:  ldc.i4.1
  IL_04da:  ldnull
  IL_04db:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_04e0:  stelem.ref
  IL_04e1:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_04e6:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_04eb:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__10""
  IL_04f0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__10""
  IL_04f5:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_04fa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__10""
  IL_04ff:  ldloc.s    V_8
  IL_0501:  ldloc.s    V_10
  IL_0503:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0508:  pop
  IL_0509:  ldloc.s    V_4
  IL_050b:  stloc.s    V_7
  IL_050d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__11""
  IL_0512:  brtrue.s   IL_0543
  IL_0514:  ldc.i4.0
  IL_0515:  ldstr      ""d3""
  IL_051a:  ldtoken    ""C""
  IL_051f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0524:  ldc.i4.1
  IL_0525:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_052a:  dup
  IL_052b:  ldc.i4.0
  IL_052c:  ldc.i4.0
  IL_052d:  ldnull
  IL_052e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0533:  stelem.ref
  IL_0534:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0539:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_053e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__11""
  IL_0543:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__11""
  IL_0548:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_054d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__11""
  IL_0552:  ldloc.s    V_7
  IL_0554:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0559:  stloc.s    V_8
  IL_055b:  ldloc.3
  IL_055c:  stloc.s    V_11
  IL_055e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__14""
  IL_0563:  brtrue.s   IL_0584
  IL_0565:  ldc.i4.0
  IL_0566:  ldstr      ""d3""
  IL_056b:  ldtoken    ""C""
  IL_0570:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0575:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_057a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_057f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__14""
  IL_0584:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__14""
  IL_0589:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_058e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__14""
  IL_0593:  ldloc.s    V_7
  IL_0595:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_059a:  brtrue     IL_0651
  IL_059f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__13""
  IL_05a4:  brtrue.s   IL_05e3
  IL_05a6:  ldc.i4     0x80
  IL_05ab:  ldstr      ""d3""
  IL_05b0:  ldtoken    ""C""
  IL_05b5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_05ba:  ldc.i4.2
  IL_05bb:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_05c0:  dup
  IL_05c1:  ldc.i4.0
  IL_05c2:  ldc.i4.0
  IL_05c3:  ldnull
  IL_05c4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_05c9:  stelem.ref
  IL_05ca:  dup
  IL_05cb:  ldc.i4.1
  IL_05cc:  ldc.i4.0
  IL_05cd:  ldnull
  IL_05ce:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_05d3:  stelem.ref
  IL_05d4:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_05d9:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_05de:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__13""
  IL_05e3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__13""
  IL_05e8:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_05ed:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__13""
  IL_05f2:  ldloc.s    V_7
  IL_05f4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__12""
  IL_05f9:  brtrue.s   IL_0631
  IL_05fb:  ldc.i4.0
  IL_05fc:  ldc.i4.s   63
  IL_05fe:  ldtoken    ""C""
  IL_0603:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0608:  ldc.i4.2
  IL_0609:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_060e:  dup
  IL_060f:  ldc.i4.0
  IL_0610:  ldc.i4.0
  IL_0611:  ldnull
  IL_0612:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0617:  stelem.ref
  IL_0618:  dup
  IL_0619:  ldc.i4.1
  IL_061a:  ldc.i4.1
  IL_061b:  ldnull
  IL_061c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0621:  stelem.ref
  IL_0622:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0627:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_062c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__12""
  IL_0631:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__12""
  IL_0636:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_063b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__12""
  IL_0640:  ldloc.s    V_8
  IL_0642:  ldloc.s    V_11
  IL_0644:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0649:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_064e:  pop
  IL_064f:  br.s       IL_06af
  IL_0651:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__15""
  IL_0656:  brtrue.s   IL_0696
  IL_0658:  ldc.i4     0x104
  IL_065d:  ldstr      ""add_d3""
  IL_0662:  ldnull
  IL_0663:  ldtoken    ""C""
  IL_0668:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_066d:  ldc.i4.2
  IL_066e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0673:  dup
  IL_0674:  ldc.i4.0
  IL_0675:  ldc.i4.0
  IL_0676:  ldnull
  IL_0677:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_067c:  stelem.ref
  IL_067d:  dup
  IL_067e:  ldc.i4.1
  IL_067f:  ldc.i4.1
  IL_0680:  ldnull
  IL_0681:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0686:  stelem.ref
  IL_0687:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_068c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0691:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__15""
  IL_0696:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__15""
  IL_069b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_06a0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__15""
  IL_06a5:  ldloc.s    V_7
  IL_06a7:  ldloc.s    V_11
  IL_06a9:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_06ae:  pop
  IL_06af:  ldloc.s    V_4
  IL_06b1:  stloc.s    V_8
  IL_06b3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__16""
  IL_06b8:  brtrue.s   IL_06e9
  IL_06ba:  ldc.i4.0
  IL_06bb:  ldstr      ""d1""
  IL_06c0:  ldtoken    ""C""
  IL_06c5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_06ca:  ldc.i4.1
  IL_06cb:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_06d0:  dup
  IL_06d1:  ldc.i4.0
  IL_06d2:  ldc.i4.0
  IL_06d3:  ldnull
  IL_06d4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_06d9:  stelem.ref
  IL_06da:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_06df:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_06e4:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__16""
  IL_06e9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__16""
  IL_06ee:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_06f3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__16""
  IL_06f8:  ldloc.s    V_8
  IL_06fa:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_06ff:  stloc.s    V_7
  IL_0701:  ldloc.1
  IL_0702:  stloc.s    V_9
  IL_0704:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__19""
  IL_0709:  brtrue.s   IL_072a
  IL_070b:  ldc.i4.0
  IL_070c:  ldstr      ""d1""
  IL_0711:  ldtoken    ""C""
  IL_0716:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_071b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0720:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0725:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__19""
  IL_072a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__19""
  IL_072f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0734:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__19""
  IL_0739:  ldloc.s    V_8
  IL_073b:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0740:  brtrue     IL_07f7
  IL_0745:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__18""
  IL_074a:  brtrue.s   IL_0789
  IL_074c:  ldc.i4     0x80
  IL_0751:  ldstr      ""d1""
  IL_0756:  ldtoken    ""C""
  IL_075b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0760:  ldc.i4.2
  IL_0761:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0766:  dup
  IL_0767:  ldc.i4.0
  IL_0768:  ldc.i4.0
  IL_0769:  ldnull
  IL_076a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_076f:  stelem.ref
  IL_0770:  dup
  IL_0771:  ldc.i4.1
  IL_0772:  ldc.i4.0
  IL_0773:  ldnull
  IL_0774:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0779:  stelem.ref
  IL_077a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_077f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0784:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__18""
  IL_0789:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__18""
  IL_078e:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0793:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__18""
  IL_0798:  ldloc.s    V_8
  IL_079a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__17""
  IL_079f:  brtrue.s   IL_07d7
  IL_07a1:  ldc.i4.0
  IL_07a2:  ldc.i4.s   73
  IL_07a4:  ldtoken    ""C""
  IL_07a9:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_07ae:  ldc.i4.2
  IL_07af:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_07b4:  dup
  IL_07b5:  ldc.i4.0
  IL_07b6:  ldc.i4.0
  IL_07b7:  ldnull
  IL_07b8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_07bd:  stelem.ref
  IL_07be:  dup
  IL_07bf:  ldc.i4.1
  IL_07c0:  ldc.i4.1
  IL_07c1:  ldnull
  IL_07c2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_07c7:  stelem.ref
  IL_07c8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_07cd:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_07d2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__17""
  IL_07d7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__17""
  IL_07dc:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_07e1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__17""
  IL_07e6:  ldloc.s    V_7
  IL_07e8:  ldloc.s    V_9
  IL_07ea:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_07ef:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_07f4:  pop
  IL_07f5:  br.s       IL_0855
  IL_07f7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__20""
  IL_07fc:  brtrue.s   IL_083c
  IL_07fe:  ldc.i4     0x104
  IL_0803:  ldstr      ""remove_d1""
  IL_0808:  ldnull
  IL_0809:  ldtoken    ""C""
  IL_080e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0813:  ldc.i4.2
  IL_0814:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0819:  dup
  IL_081a:  ldc.i4.0
  IL_081b:  ldc.i4.0
  IL_081c:  ldnull
  IL_081d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0822:  stelem.ref
  IL_0823:  dup
  IL_0824:  ldc.i4.1
  IL_0825:  ldc.i4.1
  IL_0826:  ldnull
  IL_0827:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_082c:  stelem.ref
  IL_082d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0832:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0837:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__20""
  IL_083c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__20""
  IL_0841:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0846:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__20""
  IL_084b:  ldloc.s    V_8
  IL_084d:  ldloc.s    V_9
  IL_084f:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0854:  pop
  IL_0855:  ldloc.s    V_4
  IL_0857:  stloc.s    V_7
  IL_0859:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__21""
  IL_085e:  brtrue.s   IL_088f
  IL_0860:  ldc.i4.0
  IL_0861:  ldstr      ""d2""
  IL_0866:  ldtoken    ""C""
  IL_086b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0870:  ldc.i4.1
  IL_0871:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0876:  dup
  IL_0877:  ldc.i4.0
  IL_0878:  ldc.i4.0
  IL_0879:  ldnull
  IL_087a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_087f:  stelem.ref
  IL_0880:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0885:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_088a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__21""
  IL_088f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__21""
  IL_0894:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0899:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__21""
  IL_089e:  ldloc.s    V_7
  IL_08a0:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_08a5:  stloc.s    V_8
  IL_08a7:  ldloc.2
  IL_08a8:  stloc.s    V_10
  IL_08aa:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__24""
  IL_08af:  brtrue.s   IL_08d0
  IL_08b1:  ldc.i4.0
  IL_08b2:  ldstr      ""d2""
  IL_08b7:  ldtoken    ""C""
  IL_08bc:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_08c1:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_08c6:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_08cb:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__24""
  IL_08d0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__24""
  IL_08d5:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_08da:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__24""
  IL_08df:  ldloc.s    V_7
  IL_08e1:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_08e6:  brtrue     IL_099d
  IL_08eb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__23""
  IL_08f0:  brtrue.s   IL_092f
  IL_08f2:  ldc.i4     0x80
  IL_08f7:  ldstr      ""d2""
  IL_08fc:  ldtoken    ""C""
  IL_0901:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0906:  ldc.i4.2
  IL_0907:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_090c:  dup
  IL_090d:  ldc.i4.0
  IL_090e:  ldc.i4.0
  IL_090f:  ldnull
  IL_0910:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0915:  stelem.ref
  IL_0916:  dup
  IL_0917:  ldc.i4.1
  IL_0918:  ldc.i4.0
  IL_0919:  ldnull
  IL_091a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_091f:  stelem.ref
  IL_0920:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0925:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_092a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__23""
  IL_092f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__23""
  IL_0934:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0939:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__23""
  IL_093e:  ldloc.s    V_7
  IL_0940:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__22""
  IL_0945:  brtrue.s   IL_097d
  IL_0947:  ldc.i4.0
  IL_0948:  ldc.i4.s   73
  IL_094a:  ldtoken    ""C""
  IL_094f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0954:  ldc.i4.2
  IL_0955:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_095a:  dup
  IL_095b:  ldc.i4.0
  IL_095c:  ldc.i4.0
  IL_095d:  ldnull
  IL_095e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0963:  stelem.ref
  IL_0964:  dup
  IL_0965:  ldc.i4.1
  IL_0966:  ldc.i4.1
  IL_0967:  ldnull
  IL_0968:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_096d:  stelem.ref
  IL_096e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0973:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0978:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__22""
  IL_097d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__22""
  IL_0982:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0987:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__22""
  IL_098c:  ldloc.s    V_8
  IL_098e:  ldloc.s    V_10
  IL_0990:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0995:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_099a:  pop
  IL_099b:  br.s       IL_09fb
  IL_099d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__25""
  IL_09a2:  brtrue.s   IL_09e2
  IL_09a4:  ldc.i4     0x104
  IL_09a9:  ldstr      ""remove_d2""
  IL_09ae:  ldnull
  IL_09af:  ldtoken    ""C""
  IL_09b4:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_09b9:  ldc.i4.2
  IL_09ba:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_09bf:  dup
  IL_09c0:  ldc.i4.0
  IL_09c1:  ldc.i4.0
  IL_09c2:  ldnull
  IL_09c3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_09c8:  stelem.ref
  IL_09c9:  dup
  IL_09ca:  ldc.i4.1
  IL_09cb:  ldc.i4.1
  IL_09cc:  ldnull
  IL_09cd:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_09d2:  stelem.ref
  IL_09d3:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_09d8:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_09dd:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__25""
  IL_09e2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__25""
  IL_09e7:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_09ec:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__25""
  IL_09f1:  ldloc.s    V_7
  IL_09f3:  ldloc.s    V_10
  IL_09f5:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_09fa:  pop
  IL_09fb:  ldloc.s    V_4
  IL_09fd:  stloc.s    V_8
  IL_09ff:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__26""
  IL_0a04:  brtrue.s   IL_0a35
  IL_0a06:  ldc.i4.0
  IL_0a07:  ldstr      ""d3""
  IL_0a0c:  ldtoken    ""C""
  IL_0a11:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0a16:  ldc.i4.1
  IL_0a17:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0a1c:  dup
  IL_0a1d:  ldc.i4.0
  IL_0a1e:  ldc.i4.0
  IL_0a1f:  ldnull
  IL_0a20:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0a25:  stelem.ref
  IL_0a26:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0a2b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0a30:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__26""
  IL_0a35:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__26""
  IL_0a3a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0a3f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__26""
  IL_0a44:  ldloc.s    V_8
  IL_0a46:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0a4b:  stloc.s    V_7
  IL_0a4d:  ldloc.3
  IL_0a4e:  stloc.s    V_11
  IL_0a50:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__29""
  IL_0a55:  brtrue.s   IL_0a76
  IL_0a57:  ldc.i4.0
  IL_0a58:  ldstr      ""d3""
  IL_0a5d:  ldtoken    ""C""
  IL_0a62:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0a67:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0a6c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0a71:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__29""
  IL_0a76:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__29""
  IL_0a7b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0a80:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__29""
  IL_0a85:  ldloc.s    V_8
  IL_0a87:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0a8c:  brtrue     IL_0b43
  IL_0a91:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__28""
  IL_0a96:  brtrue.s   IL_0ad5
  IL_0a98:  ldc.i4     0x80
  IL_0a9d:  ldstr      ""d3""
  IL_0aa2:  ldtoken    ""C""
  IL_0aa7:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0aac:  ldc.i4.2
  IL_0aad:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0ab2:  dup
  IL_0ab3:  ldc.i4.0
  IL_0ab4:  ldc.i4.0
  IL_0ab5:  ldnull
  IL_0ab6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0abb:  stelem.ref
  IL_0abc:  dup
  IL_0abd:  ldc.i4.1
  IL_0abe:  ldc.i4.0
  IL_0abf:  ldnull
  IL_0ac0:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ac5:  stelem.ref
  IL_0ac6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0acb:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0ad0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__28""
  IL_0ad5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__28""
  IL_0ada:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0adf:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__28""
  IL_0ae4:  ldloc.s    V_8
  IL_0ae6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__27""
  IL_0aeb:  brtrue.s   IL_0b23
  IL_0aed:  ldc.i4.0
  IL_0aee:  ldc.i4.s   73
  IL_0af0:  ldtoken    ""C""
  IL_0af5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0afa:  ldc.i4.2
  IL_0afb:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0b00:  dup
  IL_0b01:  ldc.i4.0
  IL_0b02:  ldc.i4.0
  IL_0b03:  ldnull
  IL_0b04:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0b09:  stelem.ref
  IL_0b0a:  dup
  IL_0b0b:  ldc.i4.1
  IL_0b0c:  ldc.i4.1
  IL_0b0d:  ldnull
  IL_0b0e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0b13:  stelem.ref
  IL_0b14:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0b19:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0b1e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__27""
  IL_0b23:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__27""
  IL_0b28:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_0b2d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__27""
  IL_0b32:  ldloc.s    V_7
  IL_0b34:  ldloc.s    V_11
  IL_0b36:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0b3b:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0b40:  pop
  IL_0b41:  br.s       IL_0ba1
  IL_0b43:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__30""
  IL_0b48:  brtrue.s   IL_0b88
  IL_0b4a:  ldc.i4     0x104
  IL_0b4f:  ldstr      ""remove_d3""
  IL_0b54:  ldnull
  IL_0b55:  ldtoken    ""C""
  IL_0b5a:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0b5f:  ldc.i4.2
  IL_0b60:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0b65:  dup
  IL_0b66:  ldc.i4.0
  IL_0b67:  ldc.i4.0
  IL_0b68:  ldnull
  IL_0b69:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0b6e:  stelem.ref
  IL_0b6f:  dup
  IL_0b70:  ldc.i4.1
  IL_0b71:  ldc.i4.1
  IL_0b72:  ldnull
  IL_0b73:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0b78:  stelem.ref
  IL_0b79:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0b7e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0b83:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__30""
  IL_0b88:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__30""
  IL_0b8d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_0b92:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__30""
  IL_0b97:  ldloc.s    V_8
  IL_0b99:  ldloc.s    V_11
  IL_0b9b:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_0ba0:  pop
  IL_0ba1:  ldloc.s    V_4
  IL_0ba3:  stloc.s    V_7
  IL_0ba5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__31""
  IL_0baa:  brtrue.s   IL_0bdb
  IL_0bac:  ldc.i4.0
  IL_0bad:  ldstr      ""d1""
  IL_0bb2:  ldtoken    ""C""
  IL_0bb7:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0bbc:  ldc.i4.1
  IL_0bbd:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0bc2:  dup
  IL_0bc3:  ldc.i4.0
  IL_0bc4:  ldc.i4.0
  IL_0bc5:  ldnull
  IL_0bc6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0bcb:  stelem.ref
  IL_0bcc:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0bd1:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0bd6:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__31""
  IL_0bdb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__31""
  IL_0be0:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0be5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__31""
  IL_0bea:  ldloc.s    V_7
  IL_0bec:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0bf1:  stloc.s    V_8
  IL_0bf3:  ldloc.2
  IL_0bf4:  stloc.s    V_10
  IL_0bf6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__34""
  IL_0bfb:  brtrue.s   IL_0c1c
  IL_0bfd:  ldc.i4.0
  IL_0bfe:  ldstr      ""d1""
  IL_0c03:  ldtoken    ""C""
  IL_0c08:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0c0d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0c12:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0c17:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__34""
  IL_0c1c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__34""
  IL_0c21:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0c26:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__34""
  IL_0c2b:  ldloc.s    V_7
  IL_0c2d:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0c32:  brtrue     IL_0ce9
  IL_0c37:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__33""
  IL_0c3c:  brtrue.s   IL_0c7b
  IL_0c3e:  ldc.i4     0x80
  IL_0c43:  ldstr      ""d1""
  IL_0c48:  ldtoken    ""C""
  IL_0c4d:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0c52:  ldc.i4.2
  IL_0c53:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0c58:  dup
  IL_0c59:  ldc.i4.0
  IL_0c5a:  ldc.i4.0
  IL_0c5b:  ldnull
  IL_0c5c:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0c61:  stelem.ref
  IL_0c62:  dup
  IL_0c63:  ldc.i4.1
  IL_0c64:  ldc.i4.0
  IL_0c65:  ldnull
  IL_0c66:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0c6b:  stelem.ref
  IL_0c6c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0c71:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0c76:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__33""
  IL_0c7b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__33""
  IL_0c80:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0c85:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__33""
  IL_0c8a:  ldloc.s    V_7
  IL_0c8c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__32""
  IL_0c91:  brtrue.s   IL_0cc9
  IL_0c93:  ldc.i4.0
  IL_0c94:  ldc.i4.s   63
  IL_0c96:  ldtoken    ""C""
  IL_0c9b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0ca0:  ldc.i4.2
  IL_0ca1:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0ca6:  dup
  IL_0ca7:  ldc.i4.0
  IL_0ca8:  ldc.i4.0
  IL_0ca9:  ldnull
  IL_0caa:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0caf:  stelem.ref
  IL_0cb0:  dup
  IL_0cb1:  ldc.i4.1
  IL_0cb2:  ldc.i4.1
  IL_0cb3:  ldnull
  IL_0cb4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0cb9:  stelem.ref
  IL_0cba:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0cbf:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0cc4:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__32""
  IL_0cc9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__32""
  IL_0cce:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0cd3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__32""
  IL_0cd8:  ldloc.s    V_8
  IL_0cda:  ldloc.s    V_10
  IL_0cdc:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0ce1:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0ce6:  pop
  IL_0ce7:  br.s       IL_0d47
  IL_0ce9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__35""
  IL_0cee:  brtrue.s   IL_0d2e
  IL_0cf0:  ldc.i4     0x104
  IL_0cf5:  ldstr      ""add_d1""
  IL_0cfa:  ldnull
  IL_0cfb:  ldtoken    ""C""
  IL_0d00:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0d05:  ldc.i4.2
  IL_0d06:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0d0b:  dup
  IL_0d0c:  ldc.i4.0
  IL_0d0d:  ldc.i4.0
  IL_0d0e:  ldnull
  IL_0d0f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0d14:  stelem.ref
  IL_0d15:  dup
  IL_0d16:  ldc.i4.1
  IL_0d17:  ldc.i4.1
  IL_0d18:  ldnull
  IL_0d19:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0d1e:  stelem.ref
  IL_0d1f:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0d24:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0d29:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__35""
  IL_0d2e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__35""
  IL_0d33:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_0d38:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__35""
  IL_0d3d:  ldloc.s    V_7
  IL_0d3f:  ldloc.s    V_10
  IL_0d41:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_0d46:  pop
  IL_0d47:  ldloc.0
  IL_0d48:  stloc.s    V_4
  IL_0d4a:  ldloc.s    V_4
  IL_0d4c:  stloc.s    V_8
  IL_0d4e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__36""
  IL_0d53:  brtrue.s   IL_0d84
  IL_0d55:  ldc.i4.0
  IL_0d56:  ldstr      ""d1""
  IL_0d5b:  ldtoken    ""C""
  IL_0d60:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0d65:  ldc.i4.1
  IL_0d66:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0d6b:  dup
  IL_0d6c:  ldc.i4.0
  IL_0d6d:  ldc.i4.0
  IL_0d6e:  ldnull
  IL_0d6f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0d74:  stelem.ref
  IL_0d75:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0d7a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0d7f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__36""
  IL_0d84:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__36""
  IL_0d89:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0d8e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__36""
  IL_0d93:  ldloc.s    V_8
  IL_0d95:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0d9a:  stloc.s    V_7
  IL_0d9c:  ldloc.1
  IL_0d9d:  stloc.s    V_9
  IL_0d9f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__39""
  IL_0da4:  brtrue.s   IL_0dc5
  IL_0da6:  ldc.i4.0
  IL_0da7:  ldstr      ""d1""
  IL_0dac:  ldtoken    ""C""
  IL_0db1:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0db6:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0dbb:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0dc0:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__39""
  IL_0dc5:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__39""
  IL_0dca:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0dcf:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__39""
  IL_0dd4:  ldloc.s    V_8
  IL_0dd6:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0ddb:  brtrue     IL_0e92
  IL_0de0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__38""
  IL_0de5:  brtrue.s   IL_0e24
  IL_0de7:  ldc.i4     0x80
  IL_0dec:  ldstr      ""d1""
  IL_0df1:  ldtoken    ""C""
  IL_0df6:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0dfb:  ldc.i4.2
  IL_0dfc:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0e01:  dup
  IL_0e02:  ldc.i4.0
  IL_0e03:  ldc.i4.0
  IL_0e04:  ldnull
  IL_0e05:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0e0a:  stelem.ref
  IL_0e0b:  dup
  IL_0e0c:  ldc.i4.1
  IL_0e0d:  ldc.i4.0
  IL_0e0e:  ldnull
  IL_0e0f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0e14:  stelem.ref
  IL_0e15:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0e1a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0e1f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__38""
  IL_0e24:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__38""
  IL_0e29:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0e2e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__38""
  IL_0e33:  ldloc.s    V_8
  IL_0e35:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__37""
  IL_0e3a:  brtrue.s   IL_0e72
  IL_0e3c:  ldc.i4.0
  IL_0e3d:  ldc.i4.s   63
  IL_0e3f:  ldtoken    ""C""
  IL_0e44:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0e49:  ldc.i4.2
  IL_0e4a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0e4f:  dup
  IL_0e50:  ldc.i4.0
  IL_0e51:  ldc.i4.0
  IL_0e52:  ldnull
  IL_0e53:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0e58:  stelem.ref
  IL_0e59:  dup
  IL_0e5a:  ldc.i4.1
  IL_0e5b:  ldc.i4.1
  IL_0e5c:  ldnull
  IL_0e5d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0e62:  stelem.ref
  IL_0e63:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0e68:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0e6d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__37""
  IL_0e72:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__37""
  IL_0e77:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0e7c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__37""
  IL_0e81:  ldloc.s    V_7
  IL_0e83:  ldloc.s    V_9
  IL_0e85:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0e8a:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_0e8f:  pop
  IL_0e90:  br.s       IL_0ef0
  IL_0e92:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__40""
  IL_0e97:  brtrue.s   IL_0ed7
  IL_0e99:  ldc.i4     0x104
  IL_0e9e:  ldstr      ""add_d1""
  IL_0ea3:  ldnull
  IL_0ea4:  ldtoken    ""C""
  IL_0ea9:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0eae:  ldc.i4.2
  IL_0eaf:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0eb4:  dup
  IL_0eb5:  ldc.i4.0
  IL_0eb6:  ldc.i4.0
  IL_0eb7:  ldnull
  IL_0eb8:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ebd:  stelem.ref
  IL_0ebe:  dup
  IL_0ebf:  ldc.i4.1
  IL_0ec0:  ldc.i4.1
  IL_0ec1:  ldnull
  IL_0ec2:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ec7:  stelem.ref
  IL_0ec8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0ecd:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0ed2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__40""
  IL_0ed7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__40""
  IL_0edc:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_0ee1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__40""
  IL_0ee6:  ldloc.s    V_8
  IL_0ee8:  ldloc.s    V_9
  IL_0eea:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_0eef:  pop
  IL_0ef0:  ldloc.s    V_4
  IL_0ef2:  stloc.s    V_7
  IL_0ef4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__41""
  IL_0ef9:  brtrue.s   IL_0f2a
  IL_0efb:  ldc.i4.0
  IL_0efc:  ldstr      ""d2""
  IL_0f01:  ldtoken    ""C""
  IL_0f06:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0f0b:  ldc.i4.1
  IL_0f0c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0f11:  dup
  IL_0f12:  ldc.i4.0
  IL_0f13:  ldc.i4.0
  IL_0f14:  ldnull
  IL_0f15:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0f1a:  stelem.ref
  IL_0f1b:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0f20:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0f25:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__41""
  IL_0f2a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__41""
  IL_0f2f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_0f34:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__41""
  IL_0f39:  ldloc.s    V_7
  IL_0f3b:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0f40:  stloc.s    V_8
  IL_0f42:  ldloc.2
  IL_0f43:  stloc.s    V_10
  IL_0f45:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__44""
  IL_0f4a:  brtrue.s   IL_0f6b
  IL_0f4c:  ldc.i4.0
  IL_0f4d:  ldstr      ""d2""
  IL_0f52:  ldtoken    ""C""
  IL_0f57:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0f5c:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_0f61:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0f66:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__44""
  IL_0f6b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__44""
  IL_0f70:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_0f75:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__44""
  IL_0f7a:  ldloc.s    V_7
  IL_0f7c:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_0f81:  brtrue     IL_1038
  IL_0f86:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__43""
  IL_0f8b:  brtrue.s   IL_0fca
  IL_0f8d:  ldc.i4     0x80
  IL_0f92:  ldstr      ""d2""
  IL_0f97:  ldtoken    ""C""
  IL_0f9c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0fa1:  ldc.i4.2
  IL_0fa2:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0fa7:  dup
  IL_0fa8:  ldc.i4.0
  IL_0fa9:  ldc.i4.0
  IL_0faa:  ldnull
  IL_0fab:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0fb0:  stelem.ref
  IL_0fb1:  dup
  IL_0fb2:  ldc.i4.1
  IL_0fb3:  ldc.i4.0
  IL_0fb4:  ldnull
  IL_0fb5:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0fba:  stelem.ref
  IL_0fbb:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_0fc0:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_0fc5:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__43""
  IL_0fca:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__43""
  IL_0fcf:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_0fd4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__43""
  IL_0fd9:  ldloc.s    V_7
  IL_0fdb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__42""
  IL_0fe0:  brtrue.s   IL_1018
  IL_0fe2:  ldc.i4.0
  IL_0fe3:  ldc.i4.s   63
  IL_0fe5:  ldtoken    ""C""
  IL_0fea:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0fef:  ldc.i4.2
  IL_0ff0:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_0ff5:  dup
  IL_0ff6:  ldc.i4.0
  IL_0ff7:  ldc.i4.0
  IL_0ff8:  ldnull
  IL_0ff9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_0ffe:  stelem.ref
  IL_0fff:  dup
  IL_1000:  ldc.i4.1
  IL_1001:  ldc.i4.1
  IL_1002:  ldnull
  IL_1003:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1008:  stelem.ref
  IL_1009:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_100e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1013:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__42""
  IL_1018:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__42""
  IL_101d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_1022:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__42""
  IL_1027:  ldloc.s    V_8
  IL_1029:  ldloc.s    V_10
  IL_102b:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_1030:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_1035:  pop
  IL_1036:  br.s       IL_1096
  IL_1038:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__45""
  IL_103d:  brtrue.s   IL_107d
  IL_103f:  ldc.i4     0x104
  IL_1044:  ldstr      ""add_d2""
  IL_1049:  ldnull
  IL_104a:  ldtoken    ""C""
  IL_104f:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1054:  ldc.i4.2
  IL_1055:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_105a:  dup
  IL_105b:  ldc.i4.0
  IL_105c:  ldc.i4.0
  IL_105d:  ldnull
  IL_105e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1063:  stelem.ref
  IL_1064:  dup
  IL_1065:  ldc.i4.1
  IL_1066:  ldc.i4.1
  IL_1067:  ldnull
  IL_1068:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_106d:  stelem.ref
  IL_106e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1073:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1078:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__45""
  IL_107d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__45""
  IL_1082:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_1087:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__45""
  IL_108c:  ldloc.s    V_7
  IL_108e:  ldloc.s    V_10
  IL_1090:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_1095:  pop
  IL_1096:  ldloc.s    V_4
  IL_1098:  stloc.s    V_8
  IL_109a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__46""
  IL_109f:  brtrue.s   IL_10d0
  IL_10a1:  ldc.i4.0
  IL_10a2:  ldstr      ""d3""
  IL_10a7:  ldtoken    ""C""
  IL_10ac:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_10b1:  ldc.i4.1
  IL_10b2:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_10b7:  dup
  IL_10b8:  ldc.i4.0
  IL_10b9:  ldc.i4.0
  IL_10ba:  ldnull
  IL_10bb:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_10c0:  stelem.ref
  IL_10c1:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_10c6:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_10cb:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__46""
  IL_10d0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__46""
  IL_10d5:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_10da:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__46""
  IL_10df:  ldloc.s    V_8
  IL_10e1:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_10e6:  stloc.s    V_7
  IL_10e8:  ldloc.3
  IL_10e9:  stloc.s    V_11
  IL_10eb:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__49""
  IL_10f0:  brtrue.s   IL_1111
  IL_10f2:  ldc.i4.0
  IL_10f3:  ldstr      ""d3""
  IL_10f8:  ldtoken    ""C""
  IL_10fd:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1102:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_1107:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_110c:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__49""
  IL_1111:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__49""
  IL_1116:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_111b:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__49""
  IL_1120:  ldloc.s    V_8
  IL_1122:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1127:  brtrue     IL_11de
  IL_112c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__48""
  IL_1131:  brtrue.s   IL_1170
  IL_1133:  ldc.i4     0x80
  IL_1138:  ldstr      ""d3""
  IL_113d:  ldtoken    ""C""
  IL_1142:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1147:  ldc.i4.2
  IL_1148:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_114d:  dup
  IL_114e:  ldc.i4.0
  IL_114f:  ldc.i4.0
  IL_1150:  ldnull
  IL_1151:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1156:  stelem.ref
  IL_1157:  dup
  IL_1158:  ldc.i4.1
  IL_1159:  ldc.i4.0
  IL_115a:  ldnull
  IL_115b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1160:  stelem.ref
  IL_1161:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1166:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_116b:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__48""
  IL_1170:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__48""
  IL_1175:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_117a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__48""
  IL_117f:  ldloc.s    V_8
  IL_1181:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__47""
  IL_1186:  brtrue.s   IL_11be
  IL_1188:  ldc.i4.0
  IL_1189:  ldc.i4.s   63
  IL_118b:  ldtoken    ""C""
  IL_1190:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1195:  ldc.i4.2
  IL_1196:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_119b:  dup
  IL_119c:  ldc.i4.0
  IL_119d:  ldc.i4.0
  IL_119e:  ldnull
  IL_119f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_11a4:  stelem.ref
  IL_11a5:  dup
  IL_11a6:  ldc.i4.1
  IL_11a7:  ldc.i4.1
  IL_11a8:  ldnull
  IL_11a9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_11ae:  stelem.ref
  IL_11af:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_11b4:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_11b9:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__47""
  IL_11be:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__47""
  IL_11c3:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_11c8:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__47""
  IL_11cd:  ldloc.s    V_7
  IL_11cf:  ldloc.s    V_11
  IL_11d1:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_11d6:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_11db:  pop
  IL_11dc:  br.s       IL_123c
  IL_11de:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__50""
  IL_11e3:  brtrue.s   IL_1223
  IL_11e5:  ldc.i4     0x104
  IL_11ea:  ldstr      ""add_d3""
  IL_11ef:  ldnull
  IL_11f0:  ldtoken    ""C""
  IL_11f5:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_11fa:  ldc.i4.2
  IL_11fb:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1200:  dup
  IL_1201:  ldc.i4.0
  IL_1202:  ldc.i4.0
  IL_1203:  ldnull
  IL_1204:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1209:  stelem.ref
  IL_120a:  dup
  IL_120b:  ldc.i4.1
  IL_120c:  ldc.i4.1
  IL_120d:  ldnull
  IL_120e:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1213:  stelem.ref
  IL_1214:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1219:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_121e:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__50""
  IL_1223:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__50""
  IL_1228:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_122d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__50""
  IL_1232:  ldloc.s    V_8
  IL_1234:  ldloc.s    V_11
  IL_1236:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_123b:  pop
  IL_123c:  ldloc.s    V_4
  IL_123e:  stloc.s    V_7
  IL_1240:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__51""
  IL_1245:  brtrue.s   IL_1276
  IL_1247:  ldc.i4.0
  IL_1248:  ldstr      ""d1""
  IL_124d:  ldtoken    ""C""
  IL_1252:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1257:  ldc.i4.1
  IL_1258:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_125d:  dup
  IL_125e:  ldc.i4.0
  IL_125f:  ldc.i4.0
  IL_1260:  ldnull
  IL_1261:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1266:  stelem.ref
  IL_1267:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_126c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1271:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__51""
  IL_1276:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__51""
  IL_127b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_1280:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__51""
  IL_1285:  ldloc.s    V_7
  IL_1287:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_128c:  stloc.s    V_8
  IL_128e:  ldloc.1
  IL_128f:  stloc.s    V_9
  IL_1291:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__54""
  IL_1296:  brtrue.s   IL_12b7
  IL_1298:  ldc.i4.0
  IL_1299:  ldstr      ""d1""
  IL_129e:  ldtoken    ""C""
  IL_12a3:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_12a8:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_12ad:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_12b2:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__54""
  IL_12b7:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__54""
  IL_12bc:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_12c1:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__54""
  IL_12c6:  ldloc.s    V_7
  IL_12c8:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_12cd:  brtrue     IL_1384
  IL_12d2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__53""
  IL_12d7:  brtrue.s   IL_1316
  IL_12d9:  ldc.i4     0x80
  IL_12de:  ldstr      ""d1""
  IL_12e3:  ldtoken    ""C""
  IL_12e8:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_12ed:  ldc.i4.2
  IL_12ee:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_12f3:  dup
  IL_12f4:  ldc.i4.0
  IL_12f5:  ldc.i4.0
  IL_12f6:  ldnull
  IL_12f7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_12fc:  stelem.ref
  IL_12fd:  dup
  IL_12fe:  ldc.i4.1
  IL_12ff:  ldc.i4.0
  IL_1300:  ldnull
  IL_1301:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1306:  stelem.ref
  IL_1307:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_130c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1311:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__53""
  IL_1316:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__53""
  IL_131b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_1320:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__53""
  IL_1325:  ldloc.s    V_7
  IL_1327:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__52""
  IL_132c:  brtrue.s   IL_1364
  IL_132e:  ldc.i4.0
  IL_132f:  ldc.i4.s   73
  IL_1331:  ldtoken    ""C""
  IL_1336:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_133b:  ldc.i4.2
  IL_133c:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1341:  dup
  IL_1342:  ldc.i4.0
  IL_1343:  ldc.i4.0
  IL_1344:  ldnull
  IL_1345:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_134a:  stelem.ref
  IL_134b:  dup
  IL_134c:  ldc.i4.1
  IL_134d:  ldc.i4.1
  IL_134e:  ldnull
  IL_134f:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1354:  stelem.ref
  IL_1355:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_135a:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_135f:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__52""
  IL_1364:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__52""
  IL_1369:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_136e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__52""
  IL_1373:  ldloc.s    V_8
  IL_1375:  ldloc.s    V_9
  IL_1377:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_137c:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_1381:  pop
  IL_1382:  br.s       IL_13e2
  IL_1384:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__55""
  IL_1389:  brtrue.s   IL_13c9
  IL_138b:  ldc.i4     0x104
  IL_1390:  ldstr      ""remove_d1""
  IL_1395:  ldnull
  IL_1396:  ldtoken    ""C""
  IL_139b:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_13a0:  ldc.i4.2
  IL_13a1:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_13a6:  dup
  IL_13a7:  ldc.i4.0
  IL_13a8:  ldc.i4.0
  IL_13a9:  ldnull
  IL_13aa:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_13af:  stelem.ref
  IL_13b0:  dup
  IL_13b1:  ldc.i4.1
  IL_13b2:  ldc.i4.1
  IL_13b3:  ldnull
  IL_13b4:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_13b9:  stelem.ref
  IL_13ba:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_13bf:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_13c4:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__55""
  IL_13c9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__55""
  IL_13ce:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>>.Target""
  IL_13d3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>> C.<>o__0.<>p__55""
  IL_13d8:  ldloc.s    V_7
  IL_13da:  ldloc.s    V_9
  IL_13dc:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.voidDelegate)""
  IL_13e1:  pop
  IL_13e2:  ldloc.s    V_4
  IL_13e4:  stloc.s    V_8
  IL_13e6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__56""
  IL_13eb:  brtrue.s   IL_141c
  IL_13ed:  ldc.i4.0
  IL_13ee:  ldstr      ""d2""
  IL_13f3:  ldtoken    ""C""
  IL_13f8:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_13fd:  ldc.i4.1
  IL_13fe:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1403:  dup
  IL_1404:  ldc.i4.0
  IL_1405:  ldc.i4.0
  IL_1406:  ldnull
  IL_1407:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_140c:  stelem.ref
  IL_140d:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1412:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1417:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__56""
  IL_141c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__56""
  IL_1421:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_1426:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__56""
  IL_142b:  ldloc.s    V_8
  IL_142d:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1432:  stloc.s    V_7
  IL_1434:  ldloc.2
  IL_1435:  stloc.s    V_10
  IL_1437:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__59""
  IL_143c:  brtrue.s   IL_145d
  IL_143e:  ldc.i4.0
  IL_143f:  ldstr      ""d2""
  IL_1444:  ldtoken    ""C""
  IL_1449:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_144e:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_1453:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1458:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__59""
  IL_145d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__59""
  IL_1462:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_1467:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__59""
  IL_146c:  ldloc.s    V_8
  IL_146e:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1473:  brtrue     IL_152a
  IL_1478:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__58""
  IL_147d:  brtrue.s   IL_14bc
  IL_147f:  ldc.i4     0x80
  IL_1484:  ldstr      ""d2""
  IL_1489:  ldtoken    ""C""
  IL_148e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1493:  ldc.i4.2
  IL_1494:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1499:  dup
  IL_149a:  ldc.i4.0
  IL_149b:  ldc.i4.0
  IL_149c:  ldnull
  IL_149d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_14a2:  stelem.ref
  IL_14a3:  dup
  IL_14a4:  ldc.i4.1
  IL_14a5:  ldc.i4.0
  IL_14a6:  ldnull
  IL_14a7:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_14ac:  stelem.ref
  IL_14ad:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_14b2:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_14b7:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__58""
  IL_14bc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__58""
  IL_14c1:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_14c6:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__58""
  IL_14cb:  ldloc.s    V_8
  IL_14cd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__57""
  IL_14d2:  brtrue.s   IL_150a
  IL_14d4:  ldc.i4.0
  IL_14d5:  ldc.i4.s   73
  IL_14d7:  ldtoken    ""C""
  IL_14dc:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_14e1:  ldc.i4.2
  IL_14e2:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_14e7:  dup
  IL_14e8:  ldc.i4.0
  IL_14e9:  ldc.i4.0
  IL_14ea:  ldnull
  IL_14eb:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_14f0:  stelem.ref
  IL_14f1:  dup
  IL_14f2:  ldc.i4.1
  IL_14f3:  ldc.i4.1
  IL_14f4:  ldnull
  IL_14f5:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_14fa:  stelem.ref
  IL_14fb:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1500:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1505:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__57""
  IL_150a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__57""
  IL_150f:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_1514:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__57""
  IL_1519:  ldloc.s    V_7
  IL_151b:  ldloc.s    V_10
  IL_151d:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_1522:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_1527:  pop
  IL_1528:  br.s       IL_1588
  IL_152a:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__60""
  IL_152f:  brtrue.s   IL_156f
  IL_1531:  ldc.i4     0x104
  IL_1536:  ldstr      ""remove_d2""
  IL_153b:  ldnull
  IL_153c:  ldtoken    ""C""
  IL_1541:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1546:  ldc.i4.2
  IL_1547:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_154c:  dup
  IL_154d:  ldc.i4.0
  IL_154e:  ldc.i4.0
  IL_154f:  ldnull
  IL_1550:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1555:  stelem.ref
  IL_1556:  dup
  IL_1557:  ldc.i4.1
  IL_1558:  ldc.i4.1
  IL_1559:  ldnull
  IL_155a:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_155f:  stelem.ref
  IL_1560:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1565:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_156a:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__60""
  IL_156f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__60""
  IL_1574:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_1579:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__60""
  IL_157e:  ldloc.s    V_8
  IL_1580:  ldloc.s    V_10
  IL_1582:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_1587:  pop
  IL_1588:  ldloc.s    V_4
  IL_158a:  stloc.s    V_7
  IL_158c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__61""
  IL_1591:  brtrue.s   IL_15c2
  IL_1593:  ldc.i4.0
  IL_1594:  ldstr      ""d3""
  IL_1599:  ldtoken    ""C""
  IL_159e:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_15a3:  ldc.i4.1
  IL_15a4:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_15a9:  dup
  IL_15aa:  ldc.i4.0
  IL_15ab:  ldc.i4.0
  IL_15ac:  ldnull
  IL_15ad:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_15b2:  stelem.ref
  IL_15b3:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_15b8:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_15bd:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__61""
  IL_15c2:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__61""
  IL_15c7:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_15cc:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__61""
  IL_15d1:  ldloc.s    V_7
  IL_15d3:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_15d8:  stloc.s    V_8
  IL_15da:  ldloc.3
  IL_15db:  stloc.s    V_11
  IL_15dd:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__64""
  IL_15e2:  brtrue.s   IL_1603
  IL_15e4:  ldc.i4.0
  IL_15e5:  ldstr      ""d3""
  IL_15ea:  ldtoken    ""C""
  IL_15ef:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_15f4:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_15f9:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_15fe:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__64""
  IL_1603:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__64""
  IL_1608:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_160d:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__64""
  IL_1612:  ldloc.s    V_7
  IL_1614:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_1619:  brtrue     IL_16d0
  IL_161e:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__63""
  IL_1623:  brtrue.s   IL_1662
  IL_1625:  ldc.i4     0x80
  IL_162a:  ldstr      ""d3""
  IL_162f:  ldtoken    ""C""
  IL_1634:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1639:  ldc.i4.2
  IL_163a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_163f:  dup
  IL_1640:  ldc.i4.0
  IL_1641:  ldc.i4.0
  IL_1642:  ldnull
  IL_1643:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1648:  stelem.ref
  IL_1649:  dup
  IL_164a:  ldc.i4.1
  IL_164b:  ldc.i4.0
  IL_164c:  ldnull
  IL_164d:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1652:  stelem.ref
  IL_1653:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_1658:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_165d:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__63""
  IL_1662:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__63""
  IL_1667:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_166c:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__63""
  IL_1671:  ldloc.s    V_7
  IL_1673:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__62""
  IL_1678:  brtrue.s   IL_16b0
  IL_167a:  ldc.i4.0
  IL_167b:  ldc.i4.s   73
  IL_167d:  ldtoken    ""C""
  IL_1682:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1687:  ldc.i4.2
  IL_1688:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_168d:  dup
  IL_168e:  ldc.i4.0
  IL_168f:  ldc.i4.0
  IL_1690:  ldnull
  IL_1691:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1696:  stelem.ref
  IL_1697:  dup
  IL_1698:  ldc.i4.1
  IL_1699:  ldc.i4.1
  IL_169a:  ldnull
  IL_169b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_16a0:  stelem.ref
  IL_16a1:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_16a6:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_16ab:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__62""
  IL_16b0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__62""
  IL_16b5:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_16ba:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__62""
  IL_16bf:  ldloc.s    V_8
  IL_16c1:  ldloc.s    V_11
  IL_16c3:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_16c8:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_16cd:  pop
  IL_16ce:  br.s       IL_172e
  IL_16d0:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__65""
  IL_16d5:  brtrue.s   IL_1715
  IL_16d7:  ldc.i4     0x104
  IL_16dc:  ldstr      ""remove_d3""
  IL_16e1:  ldnull
  IL_16e2:  ldtoken    ""C""
  IL_16e7:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_16ec:  ldc.i4.2
  IL_16ed:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_16f2:  dup
  IL_16f3:  ldc.i4.0
  IL_16f4:  ldc.i4.0
  IL_16f5:  ldnull
  IL_16f6:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_16fb:  stelem.ref
  IL_16fc:  dup
  IL_16fd:  ldc.i4.1
  IL_16fe:  ldc.i4.1
  IL_16ff:  ldnull
  IL_1700:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1705:  stelem.ref
  IL_1706:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_170b:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1710:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__65""
  IL_1715:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__65""
  IL_171a:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>>.Target""
  IL_171f:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>> C.<>o__0.<>p__65""
  IL_1724:  ldloc.s    V_7
  IL_1726:  ldloc.s    V_11
  IL_1728:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.dynamicDelegate)""
  IL_172d:  pop
  IL_172e:  ldloc.s    V_4
  IL_1730:  stloc.s    V_8
  IL_1732:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__66""
  IL_1737:  brtrue.s   IL_1768
  IL_1739:  ldc.i4.0
  IL_173a:  ldstr      ""d1""
  IL_173f:  ldtoken    ""C""
  IL_1744:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1749:  ldc.i4.1
  IL_174a:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_174f:  dup
  IL_1750:  ldc.i4.0
  IL_1751:  ldc.i4.0
  IL_1752:  ldnull
  IL_1753:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1758:  stelem.ref
  IL_1759:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.GetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_175e:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1763:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__66""
  IL_1768:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__66""
  IL_176d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>>.Target""
  IL_1772:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>> C.<>o__0.<>p__66""
  IL_1777:  ldloc.s    V_8
  IL_1779:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_177e:  stloc.s    V_7
  IL_1780:  ldloc.2
  IL_1781:  stloc.s    V_10
  IL_1783:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__69""
  IL_1788:  brtrue.s   IL_17a9
  IL_178a:  ldc.i4.0
  IL_178b:  ldstr      ""d1""
  IL_1790:  ldtoken    ""C""
  IL_1795:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_179a:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.IsEvent(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type)""
  IL_179f:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_17a4:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__69""
  IL_17a9:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__69""
  IL_17ae:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>>.Target""
  IL_17b3:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>> C.<>o__0.<>p__69""
  IL_17b8:  ldloc.s    V_8
  IL_17ba:  callvirt   ""bool System.Func<System.Runtime.CompilerServices.CallSite, dynamic, bool>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic)""
  IL_17bf:  brtrue     IL_1875
  IL_17c4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__68""
  IL_17c9:  brtrue.s   IL_1808
  IL_17cb:  ldc.i4     0x80
  IL_17d0:  ldstr      ""d1""
  IL_17d5:  ldtoken    ""C""
  IL_17da:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_17df:  ldc.i4.2
  IL_17e0:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_17e5:  dup
  IL_17e6:  ldc.i4.0
  IL_17e7:  ldc.i4.0
  IL_17e8:  ldnull
  IL_17e9:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_17ee:  stelem.ref
  IL_17ef:  dup
  IL_17f0:  ldc.i4.1
  IL_17f1:  ldc.i4.0
  IL_17f2:  ldnull
  IL_17f3:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_17f8:  stelem.ref
  IL_17f9:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.SetMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_17fe:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1803:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__68""
  IL_1808:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__68""
  IL_180d:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>>.Target""
  IL_1812:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>> C.<>o__0.<>p__68""
  IL_1817:  ldloc.s    V_8
  IL_1819:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__67""
  IL_181e:  brtrue.s   IL_1856
  IL_1820:  ldc.i4.0
  IL_1821:  ldc.i4.s   63
  IL_1823:  ldtoken    ""C""
  IL_1828:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_182d:  ldc.i4.2
  IL_182e:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1833:  dup
  IL_1834:  ldc.i4.0
  IL_1835:  ldc.i4.0
  IL_1836:  ldnull
  IL_1837:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_183c:  stelem.ref
  IL_183d:  dup
  IL_183e:  ldc.i4.1
  IL_183f:  ldc.i4.1
  IL_1840:  ldnull
  IL_1841:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_1846:  stelem.ref
  IL_1847:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.BinaryOperation(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, System.Linq.Expressions.ExpressionType, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_184c:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_1851:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__67""
  IL_1856:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__67""
  IL_185b:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_1860:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__67""
  IL_1865:  ldloc.s    V_7
  IL_1867:  ldloc.s    V_10
  IL_1869:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_186e:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, dynamic, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, dynamic)""
  IL_1873:  pop
  IL_1874:  ret
  IL_1875:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__70""
  IL_187a:  brtrue.s   IL_18ba
  IL_187c:  ldc.i4     0x104
  IL_1881:  ldstr      ""add_d1""
  IL_1886:  ldnull
  IL_1887:  ldtoken    ""C""
  IL_188c:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_1891:  ldc.i4.2
  IL_1892:  newarr     ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo""
  IL_1897:  dup
  IL_1898:  ldc.i4.0
  IL_1899:  ldc.i4.0
  IL_189a:  ldnull
  IL_189b:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_18a0:  stelem.ref
  IL_18a1:  dup
  IL_18a2:  ldc.i4.1
  IL_18a3:  ldc.i4.1
  IL_18a4:  ldnull
  IL_18a5:  call       ""Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags, string)""
  IL_18aa:  stelem.ref
  IL_18ab:  call       ""System.Runtime.CompilerServices.CallSiteBinder Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags, string, System.Collections.Generic.IEnumerable<System.Type>, System.Type, System.Collections.Generic.IEnumerable<Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo>)""
  IL_18b0:  call       ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Create(System.Runtime.CompilerServices.CallSiteBinder)""
  IL_18b5:  stsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__70""
  IL_18ba:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__70""
  IL_18bf:  ldfld      ""System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic> System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>>.Target""
  IL_18c4:  ldsfld     ""System.Runtime.CompilerServices.CallSite<System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>> C.<>o__0.<>p__70""
  IL_18c9:  ldloc.s    V_8
  IL_18cb:  ldloc.s    V_10
  IL_18cd:  callvirt   ""dynamic System.Func<System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>, dynamic>.Invoke(System.Runtime.CompilerServices.CallSite, dynamic, EventLibrary.genericDelegate<object>)""
  IL_18d2:  pop
  IL_18d3:  ret
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
                var comp = CreateEmptyCompilation(source, WinRtRefs, new CSharpCompilationOptions(kind));
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
                var comp = CreateEmptyCompilation(source, WinRtRefs.Concat(new[] { ilRef }), new CSharpCompilationOptions(kind));
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
                var comp = CreateEmptyCompilation(source, WinRtRefs.Concat(new[] { interfaceILRef, baseILRef }), new CSharpCompilationOptions(kind));
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
                var comp = CreateEmptyCompilation(source, WinRtRefs.Concat(new[] { ilRef }), new CSharpCompilationOptions(kind));
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
                var comp = CreateEmptyCompilation(source, WinRtRefs, new CSharpCompilationOptions(kind));
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
            var fieldType = (NamedTypeSymbol)field.Type.TypeSymbol;
            Assert.Equal(TypeKind.Error, fieldType.TypeKind);
            Assert.Equal("EventRegistrationTokenTable", fieldType.Name);
            Assert.Equal(@event.Type.TypeSymbol, fieldType.TypeArguments().Single());
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

            var eventType = @event.Type;
            var tokenType = compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken);
            Assert.NotNull(tokenType);
            var voidType = compilation.GetSpecialType(SpecialType.System_Void);
            Assert.NotNull(voidType);

            var addMethod = @event.AddMethod;
            Assert.Equal(tokenType, addMethod.ReturnType.TypeSymbol);
            Assert.False(addMethod.ReturnsVoid);
            Assert.Equal(1, addMethod.ParameterCount);
            Assert.Equal(eventType.TypeSymbol, addMethod.ParameterTypes.Single().TypeSymbol);

            var removeMethod = @event.RemoveMethod;
            Assert.Equal(voidType, removeMethod.ReturnType.TypeSymbol);
            Assert.True(removeMethod.ReturnsVoid);
            Assert.Equal(1, removeMethod.ParameterCount);
            Assert.Equal(tokenType, removeMethod.ParameterTypes.Single().TypeSymbol);

            if (@event.HasAssociatedField)
            {
                var expectedFieldType = compilation.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T).Construct(eventType.TypeSymbol);
                Assert.Equal(expectedFieldType, @event.AssociatedField.Type.TypeSymbol);
            }
            else
            {
                Assert.Null(@event.AssociatedField);
            }
        }

        private static void VerifyNormalEventShape(EventSymbol @event, CSharpCompilation compilation)
        {
            Assert.False(@event.IsWindowsRuntimeEvent);

            var eventType = @event.Type.TypeSymbol;
            var voidType = compilation.GetSpecialType(SpecialType.System_Void);
            Assert.NotNull(voidType);

            var addMethod = @event.AddMethod;
            Assert.Equal(voidType, addMethod.ReturnType.TypeSymbol);
            Assert.True(addMethod.ReturnsVoid);
            Assert.Equal(1, addMethod.ParameterCount);
            Assert.Equal(eventType, addMethod.ParameterTypes.Single().TypeSymbol);

            var removeMethod = @event.RemoveMethod;
            Assert.Equal(voidType, removeMethod.ReturnType.TypeSymbol);
            Assert.True(removeMethod.ReturnsVoid);
            Assert.Equal(1, removeMethod.ParameterCount);
            Assert.Equal(eventType, removeMethod.ParameterTypes.Single().TypeSymbol);

            if (@event.HasAssociatedField)
            {
                Assert.Equal(eventType, @event.AssociatedField.Type.TypeSymbol);
            }
            else
            {
                Assert.Null(@event.AssociatedField);
            }
        }
    }
}
