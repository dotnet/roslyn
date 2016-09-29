// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class EventTests : EmitMetadataTestBase
    {
        #region Metadata and IL

        [Fact]
        public void InstanceCustomEvent()
        {
            var text = @"
class C
{
    public event System.Action E
    {
        add { value(); }
        remove { value(); }
    }
}
";
            var compVerifier = CompileAndVerify(text,
                symbolValidator: module => ValidateEvent(module, isFromSource: false, isStatic: false, isFieldLike: false),
                expectedSignatures: new[]
                {
                    Signature("C", "E", ".event System.Action E"),
                    Signature("C", "add_E", ".method public hidebysig specialname instance System.Void add_E(System.Action value) cil managed"),
                    Signature("C", "remove_E", ".method public hidebysig specialname instance System.Void remove_E(System.Action value) cil managed"),
                });

            var accessorBody = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  callvirt   ""void System.Action.Invoke()""
  IL_0006:  ret
}";
            compVerifier.VerifyIL("C.E.add", accessorBody);
            compVerifier.VerifyIL("C.E.remove", accessorBody);
        }

        [Fact]
        public void StaticCustomEvent()
        {
            var text = @"
class C
{
    public static event System.Action E
    {
        add { value(); }
        remove { value(); }
    }
}
";
            var compVerifier = CompileAndVerify(text,
                symbolValidator: module => ValidateEvent(module, isFromSource: false, isStatic: true, isFieldLike: false),
                expectedSignatures: new[]
                {
                    Signature("C", "E", ".event System.Action E"),
                    Signature("C", "add_E", ".method public hidebysig specialname static System.Void add_E(System.Action value) cil managed"),
                    Signature("C", "remove_E", ".method public hidebysig specialname static System.Void remove_E(System.Action value) cil managed")
                });

            var accessorBody = @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""void System.Action.Invoke()""
  IL_0006:  ret
}";
            compVerifier.VerifyIL("C.E.add", accessorBody);
            compVerifier.VerifyIL("C.E.remove", accessorBody);
        }

        [Fact]
        public void InstanceFieldLikeEvent()
        {
            var text = @"
class C
{
    public event System.Action E;
}
";
            var compVerifier = CompileAndVerify(text,
                symbolValidator: module => ValidateEvent(module, isFromSource: false, isStatic: false, isFieldLike: true),
                expectedSignatures: new[]
                {
                    Signature("C", "E", ".event System.Action E"),
                    Signature("C", "add_E", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] public hidebysig specialname instance System.Void add_E(System.Action value) cil managed"),
                    Signature("C", "remove_E", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] public hidebysig specialname instance System.Void remove_E(System.Action value) cil managed")
                });

            var accessorBodyFormat = @"
{{
  // Code size       41 (0x29)
  .maxstack  3
  .locals init (System.Action V_0,
  System.Action V_1,
  System.Action V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""System.Action C.E""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  stloc.1
  IL_0009:  ldloc.1
  IL_000a:  ldarg.1
  IL_000b:  call       ""System.Delegate System.Delegate.{0}(System.Delegate, System.Delegate)""
  IL_0010:  castclass  ""System.Action""
  IL_0015:  stloc.2
  IL_0016:  ldarg.0
  IL_0017:  ldflda     ""System.Action C.E""
  IL_001c:  ldloc.2
  IL_001d:  ldloc.1
  IL_001e:  call       ""System.Action System.Threading.Interlocked.CompareExchange<System.Action>(ref System.Action, System.Action, System.Action)""
  IL_0023:  stloc.0
  IL_0024:  ldloc.0
  IL_0025:  ldloc.1
  IL_0026:  bne.un.s   IL_0007
  IL_0028:  ret
}}";

            // NOTE: Dev10 used slightly different loop condition code
            //  IL_0023:  stloc.0
            //  IL_0024:  ldloc.0
            //  IL_0025:  ldloc.1
            //  IL_0026:  ceq
            //  IL_0028:  ldc.i4.0
            //  IL_0029:  ceq
            //  IL_002b:  stloc.3
            //  IL_002c:  ldloc.3
            //  IL_002d:  brtrue.s   IL_0007
            //  IL_002f:  ret

            compVerifier.VerifyIL("C.E.add", string.Format(accessorBodyFormat, "Combine"));
            compVerifier.VerifyIL("C.E.remove", string.Format(accessorBodyFormat, "Remove"));
        }

        [Fact]
        public void StaticFieldLikeEvent()
        {
            var text = @"
class C
{
    public static event System.Action E;
}
";
            var compVerifier = CompileAndVerify(text,
                symbolValidator: module => ValidateEvent(module, isFromSource: false, isStatic: true, isFieldLike: true),
                expectedSignatures: new[]
                {
                    Signature("C", "E", ".event System.Action E"),
                    Signature("C", "add_E", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] public hidebysig specialname static System.Void add_E(System.Action value) cil managed"),
                    Signature("C", "remove_E", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] public hidebysig specialname static System.Void remove_E(System.Action value) cil managed")
                });

            var accessorBodyFormat = @"
{{
  // Code size       39 (0x27)
  .maxstack  3
  .locals init (System.Action V_0,
  System.Action V_1,
  System.Action V_2)
  IL_0000:  ldsfld     ""System.Action C.E""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  stloc.1
  IL_0008:  ldloc.1
  IL_0009:  ldarg.0
  IL_000a:  call       ""System.Delegate System.Delegate.{0}(System.Delegate, System.Delegate)""
  IL_000f:  castclass  ""System.Action""
  IL_0014:  stloc.2
  IL_0015:  ldsflda    ""System.Action C.E""
  IL_001a:  ldloc.2
  IL_001b:  ldloc.1
  IL_001c:  call       ""System.Action System.Threading.Interlocked.CompareExchange<System.Action>(ref System.Action, System.Action, System.Action)""
  IL_0021:  stloc.0
  IL_0022:  ldloc.0
  IL_0023:  ldloc.1
  IL_0024:  bne.un.s   IL_0006
  IL_0026:  ret
}}";
            // NOTE: Dev10 used slightly different loop condition code (as in InstanceFieldLikeEvent) 

            compVerifier.VerifyIL("C.E.add", string.Format(accessorBodyFormat, "Combine"));
            compVerifier.VerifyIL("C.E.remove", string.Format(accessorBodyFormat, "Remove"));
        }

        // NOTE: assumes there's an event E in a type C.
        private static void ValidateEvent(ModuleSymbol module, bool isFromSource, bool isStatic, bool isFieldLike)
        {
            var @class = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var @event = @class.GetMember<EventSymbol>("E");

            Assert.Equal(SymbolKind.Event, @event.Kind);
            Assert.Equal(Accessibility.Public, @event.DeclaredAccessibility);
            Assert.Equal(isStatic, @event.IsStatic);
            Assert.False(@event.MustCallMethodsDirectly);

            var addMethod = @event.AddMethod;
            Assert.Equal(MethodKind.EventAdd, addMethod.MethodKind);
            Assert.Equal("void C.E.add", addMethod.ToTestDisplayString());
            Assert.True((addMethod.ImplementationAttributes & System.Reflection.MethodImplAttributes.Synchronized) == 0);
            addMethod.CheckAccessorShape(@event);

            var removeMethod = @event.RemoveMethod;
            Assert.Equal(MethodKind.EventRemove, removeMethod.MethodKind);
            Assert.Equal("void C.E.remove", removeMethod.ToTestDisplayString());
            Assert.True((removeMethod.ImplementationAttributes & System.Reflection.MethodImplAttributes.Synchronized) == 0);
            removeMethod.CheckAccessorShape(@event);

            // Whether or not the event was field-like in source, it will look custom when loaded from metadata.
            if (isFieldLike && isFromSource)
            {
                Assert.True(@event.HasAssociatedField);
                var associatedField = @event.AssociatedField;
                Assert.Equal(SymbolKind.Field, associatedField.Kind);
                Assert.Equal(Accessibility.Private, associatedField.DeclaredAccessibility);
                Assert.Equal(isStatic, associatedField.IsStatic);
                Assert.Equal(@event.Type, associatedField.Type);
            }
            else
            {
                Assert.False(@event.HasAssociatedField);
                Assert.Null(@event.AssociatedField);
            }
        }

        [Fact]
        public void EventOperations()
        {
            var text = @"
class C
{
    public event System.Action E;
    public event System.Action F { add { } remove { } }

    void M(ref System.Action a)
    {
        E = a;
        a = E;
        M(ref E);
        E += a;
        E -= a;

        F += a;
        F -= a;
        F += E;
        F -= E;
    }
}
";
            CompileAndVerify(text).VerifyIL("C.M", @"
{
  // Code size       85 (0x55)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldind.ref
  IL_0003:  stfld      ""System.Action C.E""
  IL_0008:  ldarg.1
  IL_0009:  ldarg.0
  IL_000a:  ldfld      ""System.Action C.E""
  IL_000f:  stind.ref
  IL_0010:  ldarg.0
  IL_0011:  ldarg.0
  IL_0012:  ldflda     ""System.Action C.E""
  IL_0017:  call       ""void C.M(ref System.Action)""
  IL_001c:  ldarg.0
  IL_001d:  ldarg.1
  IL_001e:  ldind.ref
  IL_001f:  call       ""void C.E.add""
  IL_0024:  ldarg.0
  IL_0025:  ldarg.1
  IL_0026:  ldind.ref
  IL_0027:  call       ""void C.E.remove""
  IL_002c:  ldarg.0
  IL_002d:  ldarg.1
  IL_002e:  ldind.ref
  IL_002f:  call       ""void C.F.add""
  IL_0034:  ldarg.0
  IL_0035:  ldarg.1
  IL_0036:  ldind.ref
  IL_0037:  call       ""void C.F.remove""
  IL_003c:  ldarg.0
  IL_003d:  ldarg.0
  IL_003e:  ldfld      ""System.Action C.E""
  IL_0043:  call       ""void C.F.add""
  IL_0048:  ldarg.0
  IL_0049:  ldarg.0
  IL_004a:  ldfld      ""System.Action C.E""
  IL_004f:  call       ""void C.F.remove""
  IL_0054:  ret
}");
        }

        [Fact]
        public void StaticEventOperations()
        {
            var text = @"
class C
{
    public static event System.Action E;
    public static event System.Action F { add { } remove { } }

    void M(ref System.Action a)
    {
        E = a;
        a = E;
        M(ref E);
        E += a;
        E -= a;

        F += a;
        F -= a;
        F += E;
        F -= E;
    }
}
";
            CompileAndVerify(text).VerifyIL("C.M", @"
{
  // Code size       74 (0x4a)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldind.ref
  IL_0002:  stsfld     ""System.Action C.E""
  IL_0007:  ldarg.1
  IL_0008:  ldsfld     ""System.Action C.E""
  IL_000d:  stind.ref
  IL_000e:  ldarg.0
  IL_000f:  ldsflda    ""System.Action C.E""
  IL_0014:  call       ""void C.M(ref System.Action)""
  IL_0019:  ldarg.1
  IL_001a:  ldind.ref
  IL_001b:  call       ""void C.E.add""
  IL_0020:  ldarg.1
  IL_0021:  ldind.ref
  IL_0022:  call       ""void C.E.remove""
  IL_0027:  ldarg.1
  IL_0028:  ldind.ref
  IL_0029:  call       ""void C.F.add""
  IL_002e:  ldarg.1
  IL_002f:  ldind.ref
  IL_0030:  call       ""void C.F.remove""
  IL_0035:  ldsfld     ""System.Action C.E""
  IL_003a:  call       ""void C.F.add""
  IL_003f:  ldsfld     ""System.Action C.E""
  IL_0044:  call       ""void C.F.remove""
  IL_0049:  ret
}");
        }

        [Fact]
        public void EventAccess()
        {
            var text = @"
class C
{
    public event System.Action E;
    public event System.Action F { add { } remove { } }
    public static event System.Action G;
    public static event System.Action H { add { } remove { } }
}

class D
{
    void M(C c, System.Action a)
    {
        c.E += a;
        c.E -= a;
        c.F += a;
        c.F -= a;
        C.G += a;
        C.G -= a;
        C.H += a;
        C.H -= a;
    }
}
";
            CompileAndVerify(text).VerifyIL("D.M", @"
{
  // Code size       53 (0x35)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldarg.2
  IL_0002:  callvirt   ""void C.E.add""
  IL_0007:  ldarg.1
  IL_0008:  ldarg.2
  IL_0009:  callvirt   ""void C.E.remove""
  IL_000e:  ldarg.1
  IL_000f:  ldarg.2
  IL_0010:  callvirt   ""void C.F.add""
  IL_0015:  ldarg.1
  IL_0016:  ldarg.2
  IL_0017:  callvirt   ""void C.F.remove""
  IL_001c:  ldarg.2
  IL_001d:  call       ""void C.G.add""
  IL_0022:  ldarg.2
  IL_0023:  call       ""void C.G.remove""
  IL_0028:  ldarg.2
  IL_0029:  call       ""void C.H.add""
  IL_002e:  ldarg.2
  IL_002f:  call       ""void C.H.remove""
  IL_0034:  ret
}");
        }

        // Regresses IsMetadataVirtual issue (no associated bug).
        [Fact]
        public void InterfaceEvent()
        {
            var text = @"
interface C
{
    event System.Action E;
}
";
            var compVerifier = CompileAndVerify(text,
                symbolValidator: module => ValidateEvent(module, isFromSource: false, isStatic: false, isFieldLike: true),
                expectedSignatures: new[]
                {
                    Signature("C", "E", ".event System.Action E"),
                    Signature("C", "add_E", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] public hidebysig newslot specialname abstract virtual instance System.Void add_E(System.Action value) cil managed"),
                    Signature("C", "remove_E", ".method [System.Runtime.CompilerServices.CompilerGeneratedAttribute()] public hidebysig newslot specialname abstract virtual instance System.Void remove_E(System.Action value) cil managed")
                });
        }

        #endregion

        #region Execution

        [Fact]
        public void CustomEventExecution()
        {
            var text = @"
using System;

class C
{
    Func<string> e;
    event Func<string> E
    {
        add
        {
            Console.Write(""Adding "");
            value();
            Console.WriteLine();
            e += value;
        }
        remove
        {
            Console.Write(""Removing "");
            value();
            Console.WriteLine();
            e -= value;
        }
    }

    void Fire()
    {
        if (e != null)
        {
            Console.Write(""Invoking "");
            e();
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine(""No handlers"");
        }
    }

    static void Main()
    {
        C c = new C();
        Func<string> handler1 = () =>
        {
            Console.Write(""Handler1 "");
            return ""Handler1"";
        };
        Func<string> handler2 = () =>
        {
            Console.Write(""Handler2 "");
            return ""Handler2"";
        };

        c.Fire();
        c.E += handler1;
        c.Fire();
        c.E += handler2;
        c.Fire();
        c.E -= handler1;
        c.Fire();
        c.E -= handler2;
        c.Fire();
    }
}";

            CompileAndVerify(text, expectedOutput: @"
No handlers
Adding Handler1 
Invoking Handler1 
Adding Handler2 
Invoking Handler1 Handler2 
Removing Handler1 
Invoking Handler2 
Removing Handler2 
No handlers
");
        }

        [Fact]
        public void FieldLikeEventExecution()
        {
            var text = @"
using System;

class C
{
    event Func<string> E;

    void Fire()
    {
        if (E != null)
        {
            Console.Write(""Invoking "");
            E();
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine(""No handlers"");
        }
    }

    static void Main()
    {
        C c = new C();
        Func<string> handler1 = () =>
        {
            Console.Write(""Handler1 "");
            return ""Handler1"";
        };
        Func<string> handler2 = () =>
        {
            Console.Write(""Handler2 "");
            return ""Handler2"";
        };

        c.Fire();
        c.E += handler1;
        c.Fire();
        c.E += handler2;
        c.Fire();
        c.E -= handler1;
        c.Fire();
        c.E -= handler2;
        c.Fire();
    }
}";

            CompileAndVerify(text, expectedOutput: @"
No handlers
Invoking Handler1 
Invoking Handler1 Handler2 
Invoking Handler2 
No handlers
");
        }

        [Fact]
        public void VirtualRaiseAccessor()
        {
            var csharpSource = @"
using System;

static class Program
{
    static void Main()
    {
        C c = new C();
        c.Fire(); //Prints ""VirtualEventWithRaise Raise"" since raise_e isn't overridden

        D d = new D();
        d.Fire(); //Prints ""D raise"" since raise_e is overridden (regardless of event)
    }
}

class C : VirtualEventWithRaise
{
    public override event Action e = () => Console.WriteLine(""C Handler"");
}

class D : C
{
    public override void raise_e(object sender, object e)
    {
        Console.WriteLine(""D Raise"");
    }
}
";

            var ilAssemblyReference = TestReferences.SymbolsTests.Events;
            var compilation = CreateCompilationWithMscorlib(csharpSource, new MetadataReference[] { ilAssemblyReference }, TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: @"
VirtualEventWithRaise Raise
D Raise
");
        }

        #endregion Execution

        [Fact]
        public void MissingCompareExchange_01()
        {
            var source1 =
@"namespace System
{
    public class Object { }
    public struct Void { }
    public class ValueType { }
    public struct Boolean { }
    public abstract class Delegate { }
    public abstract class MulticastDelegate : Delegate { }
    public struct IntPtr { private IntPtr m_value; IntPtr Use(IntPtr b) { m_value = b; return m_value; } }
}
";

            var compilation1 = CreateCompilation(source1, assemblyName: GetUniqueName());
            var reference1 = MetadataReference.CreateFromStream(compilation1.EmitToStream());
            var source2 =
@"

public delegate void E1();

class C
{
    public event E1 e;

    public static void Main()
    {
        var v = new C();
        v.e += Main;
    }
}
";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (7,21): warning CS0067: The event 'C.e' is never used
                //     public event E1 e;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "e").WithArguments("C.e")
            );

            compilation2.Emit(new System.IO.MemoryStream()).Diagnostics.Verify(
    // (7,21): error CS0656: Missing compiler required member 'System.Delegate.Combine'
    //     public event E1 e;
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "e").WithArguments("System.Delegate", "Combine").WithLocation(7, 21),
    // (7,21): error CS0656: Missing compiler required member 'System.Delegate.Remove'
    //     public event E1 e;
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "e").WithArguments("System.Delegate", "Remove").WithLocation(7, 21),
    // (7,21): warning CS0067: The event 'C.e' is never used
    //     public event E1 e;
    Diagnostic(ErrorCode.WRN_UnreferencedEvent, "e").WithArguments("C.e").WithLocation(7, 21)
                );
        }

        [Fact, WorkItem(1027568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027568"), WorkItem(528573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528573")]
        public void MissingCompareExchange_02()
        {
            var source =
@"

public delegate void E1();

class C
{
    public event E1 e;

    public static void Main()
    {
        var v = new C();
        System.Console.Write(v.e == null);
        v.e += Main;
        System.Console.Write(v.e == null);
        v.e -= Main;
        System.Console.Write(v.e == null);
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe);

            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Interlocked__CompareExchange_T);

            var verifier = CompileAndVerify(compilation,
                                            expectedOutput: "TrueFalseTrue",
                                            symbolValidator: module =>
                                                                {
                                                                    var @class = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                                                                    var @event = @class.GetMember<EventSymbol>("e");

                                                                    var addMethod = @event.AddMethod;
                                                                    Assert.False((addMethod.ImplementationAttributes & System.Reflection.MethodImplAttributes.Synchronized) == 0);

                                                                    var removeMethod = @event.RemoveMethod;
                                                                    Assert.False((removeMethod.ImplementationAttributes & System.Reflection.MethodImplAttributes.Synchronized) == 0);
                                                                }).VerifyDiagnostics();

            verifier.VerifyIL("C.e.add", @"
{
  // Code size       24 (0x18)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""E1 C.e""
  IL_0007:  ldarg.1
  IL_0008:  call       ""System.Delegate System.Delegate.Combine(System.Delegate, System.Delegate)""
  IL_000d:  castclass  ""E1""
  IL_0012:  stfld      ""E1 C.e""
  IL_0017:  ret
}
");

            verifier.VerifyIL("C.e.remove", @"
{
  // Code size       24 (0x18)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""E1 C.e""
  IL_0007:  ldarg.1
  IL_0008:  call       ""System.Delegate System.Delegate.Remove(System.Delegate, System.Delegate)""
  IL_000d:  castclass  ""E1""
  IL_0012:  stfld      ""E1 C.e""
  IL_0017:  ret
}
");
        }

        [Fact, WorkItem(1027568, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1027568"), WorkItem(528573, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528573")]
        public void MissingCompareExchange_03()
        {
            var source =
@"

public delegate void E1();

struct C
{
    public event E1 e;

    public static void Main()
    {
        var v = new C();
        System.Console.Write(v.e == null);
        v.e += Main;
        System.Console.Write(v.e == null);
        v.e -= Main;
        System.Console.Write(v.e == null);
    }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.DebugExe);

            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Interlocked__CompareExchange_T);

            var verifier = CompileAndVerify(compilation,
                                            expectedOutput: "TrueFalseTrue",
                                            symbolValidator: module =>
                                            {
                                                var @class = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                                                var @event = @class.GetMember<EventSymbol>("e");

                                                var addMethod = @event.AddMethod;
                                                Assert.True((addMethod.ImplementationAttributes & System.Reflection.MethodImplAttributes.Synchronized) == 0);

                                                var removeMethod = @event.RemoveMethod;
                                                Assert.True((removeMethod.ImplementationAttributes & System.Reflection.MethodImplAttributes.Synchronized) == 0);
                                            }).VerifyDiagnostics();

            verifier.VerifyIL("C.e.add", @"
{
  // Code size       24 (0x18)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""E1 C.e""
  IL_0007:  ldarg.1
  IL_0008:  call       ""System.Delegate System.Delegate.Combine(System.Delegate, System.Delegate)""
  IL_000d:  castclass  ""E1""
  IL_0012:  stfld      ""E1 C.e""
  IL_0017:  ret
}
");

            verifier.VerifyIL("C.e.remove", @"
{
  // Code size       24 (0x18)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""E1 C.e""
  IL_0007:  ldarg.1
  IL_0008:  call       ""System.Delegate System.Delegate.Remove(System.Delegate, System.Delegate)""
  IL_000d:  castclass  ""E1""
  IL_0012:  stfld      ""E1 C.e""
  IL_0017:  ret
}
");
        }
    }
}
