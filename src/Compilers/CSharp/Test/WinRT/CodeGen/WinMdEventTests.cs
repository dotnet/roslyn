﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    /// <summary>
    /// Test code generated for Windows Runtime events.
    /// </summary>
    public class WinMdEventTests : CSharpTestBase
    {
        [Fact]
        public void MissingReferences_SynthesizedAccessors()
        {
            var source = @"
class C
{
    event System.Action E;
}
";
            var comp = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseWinMD);
            comp.VerifyDiagnostics(
                // For the backing field and accessors:

                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"),
                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"),
                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1"),
                // Uninteresting:

                // (4,25): warning CS0067: The event 'C.E' is never used
                //     event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E"));
        }

        [Fact]
        public void MissingReferences_EventAssignment()
        {
            var source = @"
class C
{
    event System.Action E;

    void Test()
    {
        E += null;
    }
}
";

            var comp = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseWinMD);
            comp.VerifyEmitDiagnostics(
                // For the backing field and accessors:

                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"),
                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"),
                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1"),
                // (4,25): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1.GetOrCreateEventRegistrationTokenTable'
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1", "GetOrCreateEventRegistrationTokenTable"),
                // (4,25): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1.GetOrCreateEventRegistrationTokenTable'
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1", "GetOrCreateEventRegistrationTokenTable"),

                // Uninteresting:

                // (4,25): warning CS0067: The event 'C.E' is never used
                //     event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E"));
        }

        [Fact]
        public void MissingReferences_EventFieldAssignment()
        {
            var source = @"
class C
{
    event System.Action E;

    void Test()
    {
        E = null;
    }
}
";

            var comp = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseWinMD);
            comp.VerifyEmitDiagnostics(
                // For the backing field and accessors:

                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"),
                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"),
                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1"),
                // (4,25): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1.GetOrCreateEventRegistrationTokenTable'
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1", "GetOrCreateEventRegistrationTokenTable"),
                // (4,25): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1.GetOrCreateEventRegistrationTokenTable'
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1", "GetOrCreateEventRegistrationTokenTable"),

                // Uninteresting:

                // (4,25): warning CS0414: The field 'C.E' is assigned but its value is never used
                //     event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "E").WithArguments("C.E"));
        }

        [Fact]
        public void MissingReferences_EventAccess()
        {
            var source = @"
class C
{
    event System.Action E;

    void Test()
    {
        E();
    }
}
";

            var comp = CreateCompilationWithMscorlib40(source, options: TestOptions.ReleaseWinMD);
            comp.VerifyEmitDiagnostics(
                // For the backing field and accessors:

                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"),
                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken"),
                // (4,25): error CS0518: Predefined type 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1' is not defined or imported
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1"),
                // (4,25): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1.GetOrCreateEventRegistrationTokenTable'
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1", "GetOrCreateEventRegistrationTokenTable"),
                // (4,25): error CS0656: Missing compiler required member 'System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1.GetOrCreateEventRegistrationTokenTable'
                //     event System.Action E;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "E").WithArguments("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1", "GetOrCreateEventRegistrationTokenTable")
            );
        }

        [Fact(), WorkItem(1003193, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1003193")]
        public void InstanceFieldLikeEventAccessors()
        {
            var source = @"
class C
{
    event System.Action E;
}
";
            var verifier = CompileAndVerifyWithWinRt(source, options: TestOptions.ReleaseWinMD);

            verifier.VerifyIL("C.E.add", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> C.E""
  IL_0006:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>)""
  IL_000b:  ldarg.1
  IL_000c:  callvirt   ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.AddEventHandler(System.Action)""
  IL_0011:  ret
}");

            verifier.VerifyIL("C.E.remove", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> C.E""
  IL_0006:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>)""
  IL_000b:  ldarg.1
  IL_000c:  callvirt   ""void System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.RemoveEventHandler(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)""
  IL_0011:  ret
}");
        }

        [Fact(), WorkItem(1003193, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1003193")]
        public void StaticFieldLikeEventAccessors()
        {
            var source = @"
class C
{
    static event System.Action<int> E;
}
";
            var verifier = CompileAndVerifyWithWinRt(source, options: TestOptions.ReleaseWinMD);

            verifier.VerifyIL("C.E.add", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldsflda    ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action<int>> C.E""
  IL_0005:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action<int>> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action<int>>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action<int>>)""
  IL_000a:  ldarg.0
  IL_000b:  callvirt   ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action<int>>.AddEventHandler(System.Action<int>)""
  IL_0010:  ret
}");

            verifier.VerifyIL("C.E.remove", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldsflda    ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action<int>> C.E""
  IL_0005:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action<int>> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action<int>>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action<int>>)""
  IL_000a:  ldarg.0
  IL_000b:  callvirt   ""void System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action<int>>.RemoveEventHandler(System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken)""
  IL_0010:  ret
}");
        }

        [Fact]
        public void EventAssignment()
        {
            var source = @"
class C
{
    public event System.Action Instance;
    public static event System.Action Static;
}

class D
{
    C c;

    void InstanceAdd()
    {
        c.Instance += Action;
    }

    void InstanceRemove()
    {
        c.Instance -= Action;
    }

    static void StaticAdd()
    {
        C.Static += Action;
    }

    static void StaticRemove()
    {
        C.Static -= Action;
    }

    static void Action()
    {
    }
}
";
            var verifier = CompileAndVerifyWithWinRt(source, options: TestOptions.ReleaseWinMD);

            verifier.VerifyIL("D.InstanceAdd", @"
{
  // Code size       64 (0x40)
  .maxstack  4
  .locals init (C V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C D.c""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldftn      ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C.Instance.add""
  IL_000e:  newobj     ""System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0013:  ldloc.0
  IL_0014:  ldftn      ""void C.Instance.remove""
  IL_001a:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_001f:  ldsfld     ""System.Action D.<>O.<0>__Action""
  IL_0024:  dup
  IL_0025:  brtrue.s   IL_003a
  IL_0027:  pop
  IL_0028:  ldnull
  IL_0029:  ldftn      ""void D.Action()""
  IL_002f:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0034:  dup
  IL_0035:  stsfld     ""System.Action D.<>O.<0>__Action""
  IL_003a:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<System.Action>(System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_003f:  ret
}");

            verifier.VerifyIL("D.InstanceRemove", @"
{
  // Code size       50 (0x32)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C D.c""
  IL_0006:  ldftn      ""void C.Instance.remove""
  IL_000c:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0011:  ldsfld     ""System.Action <>x.<Action>w""
  IL_0016:  dup
  IL_0017:  brtrue.s   IL_002c
  IL_0019:  pop
  IL_001a:  ldnull
  IL_001b:  ldftn      ""void D.Action()""
  IL_0021:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0026:  dup
  IL_0027:  stsfld     ""System.Action <>x.<Action>w""
  IL_002c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<System.Action>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_0031:  ret
}");
            verifier.VerifyIL("D.StaticAdd", @"
{
  // Code size       57 (0x39)
  .maxstack  4
  IL_0000:  ldnull
  IL_0001:  ldftn      ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C.Static.add""
  IL_0007:  newobj     ""System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000c:  ldnull
  IL_000d:  ldftn      ""void C.Static.remove""
  IL_0013:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0018:  ldsfld     ""System.Action <>x.<Action>w""
  IL_001d:  dup
  IL_001e:  brtrue.s   IL_0033
  IL_0020:  pop
  IL_0021:  ldnull
  IL_0022:  ldftn      ""void D.Action()""
  IL_0028:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002d:  dup
  IL_002e:  stsfld     ""System.Action <>x.<Action>w""
  IL_0033:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<System.Action>(System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_0038:  ret
}");

            verifier.VerifyIL("D.StaticRemove", @"
{
  // Code size       45 (0x2d)
  .maxstack  3
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void C.Static.remove""
  IL_0007:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000c:  ldsfld     ""System.Action <>x.<Action>w""
  IL_0011:  dup
  IL_0012:  brtrue.s   IL_0027
  IL_0014:  pop
  IL_0015:  ldnull
  IL_0016:  ldftn      ""void D.Action()""
  IL_001c:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0021:  dup
  IL_0022:  stsfld     ""System.Action <>x.<Action>w""
  IL_0027:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<System.Action>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_002c:  ret
}");
        }

        [Fact]
        public void EventFieldAssignment()
        {
            var source = @"
class C
{
    public event System.Action Instance;
    public static event System.Action Static;

    void InstanceAssign()
    {
        Instance = Action;
    }

    static void StaticAssign()
    {
        Static = Action;
    }

    static void Action()
    {
    }
}
";
            var verifier = CompileAndVerifyWithWinRt(source, options: TestOptions.ReleaseWinMD);

            verifier.VerifyIL("C.InstanceAssign", @"
{
  // Code size       74 (0x4a)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""void C.Instance.remove""
  IL_0007:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveAllEventHandlers(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>)""
  IL_0011:  ldarg.0
  IL_0012:  ldftn      ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C.Instance.add""
  IL_0018:  newobj     ""System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_001d:  ldarg.0
  IL_001e:  ldftn      ""void C.Instance.remove""
  IL_0024:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0029:  ldsfld     ""System.Action C.<>O.<0>__Action""
  IL_002e:  dup
  IL_002f:  brtrue.s   IL_0044
  IL_0031:  pop
  IL_0032:  ldnull
  IL_0033:  ldftn      ""void C.Action()""
  IL_0039:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_003e:  dup
  IL_003f:  stsfld     ""System.Action C.<>O.<0>__Action""
  IL_0044:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<System.Action>(System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_0049:  ret
}");

            verifier.VerifyIL("C.StaticAssign", @"
{
  // Code size       74 (0x4a)
  .maxstack  4
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void C.Static.remove""
  IL_0007:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveAllEventHandlers(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>)""
  IL_0011:  ldnull
  IL_0012:  ldftn      ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C.Static.add""
  IL_0018:  newobj     ""System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_001d:  ldnull
  IL_001e:  ldftn      ""void C.Static.remove""
  IL_0024:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0029:  ldsfld     ""System.Action <>x.<Action>w""
  IL_002e:  dup
  IL_002f:  brtrue.s   IL_0044
  IL_0031:  pop
  IL_0032:  ldnull
  IL_0033:  ldftn      ""void C.Action()""
  IL_0039:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_003e:  dup
  IL_003f:  stsfld     ""System.Action <>x.<Action>w""
  IL_0044:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<System.Action>(System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_0049:  ret
}");
        }

        [Fact]
        public void EventAccess()
        {
            var source = @"
class C
{
    public event System.Action Instance;
    public static event System.Action Static;

    void InstanceInvoke()
    {
        Instance();
    }

    static void StaticInvoke()
    {
        Static();
    }

    void InstanceMemberAccess()
    {
        Instance.GetHashCode();
    }

    static void StaticMemberAccess()
    {
        Static.GetHashCode();
    }

    System.Action InstanceReturn()
    {
        return Instance;
    }

    static System.Action StaticReturn()
    {
        return Static;
    }

    static void Action()
    {
    }
}
";
            var verifier = CompileAndVerifyWithWinRt(source, options: TestOptions.ReleaseWinMD);

            verifier.VerifyIL("C.InstanceInvoke", @"
{
  // Code size       22 (0x16)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> C.Instance""
  IL_0006:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>)""
  IL_000b:  callvirt   ""System.Action System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.InvocationList.get""
  IL_0010:  callvirt   ""void System.Action.Invoke()""
  IL_0015:  ret
}");

            verifier.VerifyIL("C.StaticInvoke", @"
{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldsflda    ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> C.Static""
  IL_0005:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>)""
  IL_000a:  callvirt   ""System.Action System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.InvocationList.get""
  IL_000f:  callvirt   ""void System.Action.Invoke()""
  IL_0014:  ret
}");

            verifier.VerifyIL("C.InstanceMemberAccess", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> C.Instance""
  IL_0006:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>)""
  IL_000b:  callvirt   ""System.Action System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.InvocationList.get""
  IL_0010:  callvirt   ""int object.GetHashCode()""
  IL_0015:  pop
  IL_0016:  ret
}");

            verifier.VerifyIL("C.StaticMemberAccess", @"
{
  // Code size       22 (0x16)
  .maxstack  1
  IL_0000:  ldsflda    ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> C.Static""
  IL_0005:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>)""
  IL_000a:  callvirt   ""System.Action System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.InvocationList.get""
  IL_000f:  callvirt   ""int object.GetHashCode()""
  IL_0014:  pop
  IL_0015:  ret
}");

            verifier.VerifyIL("C.InstanceReturn", @"
{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> C.Instance""
  IL_0006:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>)""
  IL_000b:  callvirt   ""System.Action System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.InvocationList.get""
  IL_0010:  ret
}");

            verifier.VerifyIL("C.StaticReturn", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  IL_0000:  ldsflda    ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> C.Static""
  IL_0005:  call       ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action> System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.GetOrCreateEventRegistrationTokenTable(ref System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>)""
  IL_000a:  callvirt   ""System.Action System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable<System.Action>.InvocationList.get""
  IL_000f:  ret
}");
        }

        /// <summary>
        /// Dev11 had bugs in this area (e.g. 281866, 298564), but Roslyn shouldn't be affected.
        /// </summary>
        /// <remarks>
        /// I'm assuming this is why the final dev11 impl uses GetOrCreateEventRegistrationTokenTable.
        /// </remarks>
        [WorkItem(1003193, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1003193")]
        [Fact(Skip = "Issue #321")]
        public void FieldLikeEventSerialization()
        {
            var source1 = @"
namespace EventDeserialization
{
	public delegate void Event();

	public interface Interface
	{
		event Event E;
	}
}
";
            var source2 = @"
using System;
using System.IO;
using System.Runtime.Serialization;

namespace EventDeserialization
{
    class MainPage
    {
        public static void Main()
        {
            Model m1 = new Model();
            m1.E += () => Console.Write(""A"");
            m1.Invoke();

            var bytes = Serialize(m1);

            Model m2 = Deserialize(bytes);
            Console.WriteLine(m1 == m2);
            m2.Invoke();

            m2.E += () => Console.Write(""B"");
            m2.Invoke();
        }

        public static byte[] Serialize(Model model)
        {
            DataContractSerializer ser = new DataContractSerializer(typeof(Model));
            using (var stream = new MemoryStream())
            {
                ser.WriteObject(stream, model);
                return stream.ToArray();
            }
        }

        public static Model Deserialize(byte[] bytes)
        {
            DataContractSerializer ser = new DataContractSerializer(typeof(Model));
            using (var stream = new MemoryStream(bytes))
            {
                return (Model)ser.ReadObject(stream);
            }
        }

    }

    [DataContract]
    public sealed class Model : Interface
    {
        public event Event E;

        public void Invoke()
        {
            if (E == null)
            {
                Console.WriteLine(""null"");
            }
            else
            {
                E();
                Console.WriteLine();
            }
        }
    }
}
";

            var comp1 = CreateEmptyCompilation(source1, WinRtRefs, TestOptions.ReleaseWinMD, TestOptions.Regular, "Lib");

            var serializationRef = TestMetadata.Net451.SystemRuntimeSerialization;

            var comp2 = CreateEmptyCompilation(source2, WinRtRefs.Concat(new MetadataReference[] { new CSharpCompilationReference(comp1), serializationRef, SystemXmlRef }), TestOptions.ReleaseExe);
            CompileAndVerify(comp2, expectedOutput: @"A
False
null
B");
        }

        [WorkItem(1079725, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1079725")]
        [Fact]
        public void EventAssignmentExpression()
        {
            var source =
@"class C
{
    static event System.Action E;
    static void M()
    {
        var e = E;
        var f = E = null;
    }
}";
            var comp = CreateEmptyCompilation(source, WinRtRefs, TestOptions.ReleaseWinMD, TestOptions.Regular7);
            comp.VerifyDiagnostics(
                // (7,13): error CS0815: Cannot assign void to an implicitly-typed variable
                //         var f = E = null;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "f = E = null").WithArguments("void").WithLocation(7, 13));
        }

        [WorkItem(1079725, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1079725")]
        [Fact]
        public void EventAssignmentExpression_SemanticModel()
        {
            var source =
@"class C
{
    static event System.Action E;
    static void M()
    {
        E = null;
    }
}";
            var comp = CreateEmptyCompilation(source, WinRtRefs, TestOptions.ReleaseWinMD);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var syntax = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();
            var type = model.GetTypeInfo(syntax);
            Assert.Equal(SpecialType.System_Void, type.Type.SpecialType);
        }
    }
}
