// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Source
{
    public class EventTests : CSharpTestBase
    {
        #region Positive Cases
        [WorkItem(537323, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537323")]
        [Fact]
        public void EventInStructFollowedByClassDecl()
        {
            var text =
@"using System;
struct Test1
{
    event MyEvent ITest.Clicked;
}

class main1
{
    public static void Main()
    {
    }
}
";
            var comp = CreateCompilation(text);

            var actualSymbols = comp.Assembly.GlobalNamespace.GetMembers();
            var actual = string.Join(", ", actualSymbols.Where(s => !s.IsImplicitlyDeclared).Select(symbol => symbol.Name).OrderBy(name => name));
            Assert.Equal("main1, Test1", actual);
        }

        [WorkItem(537401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537401")]
        [Fact]
        public void EventEscapedIdentifier()
        {
            var text = @"
delegate void @out();
class C1
{
    event @out @in;
}
";
            var comp = CreateCompilation(Parse(text));
            NamedTypeSymbol c1 = (NamedTypeSymbol)comp.SourceModule.GlobalNamespace.GetMembers("C1").Single();
            //EventSymbol ein = c1.GetMembers("in").Single();
            //Assert.Equal("in", ein.Name);
            //Assert.Equal("C1.@in", ein.ToString());
            //NamedTypeSymbol dout = ein.Type;
            //Assert.Equal("out", dout.Name);
            //Assert.Equal("@out", dout.ToString());
        }

        [Fact]
        public void InstanceFieldLikeEventDeclaration()
        {
            var text = @"
class C
{
    public virtual event System.Action E;
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var @class = global.GetMember<NamedTypeSymbol>("C");

            var @event = @class.GetMember<EventSymbol>("E");

            Assert.Equal(SymbolKind.Event, @event.Kind);
            Assert.Equal(Accessibility.Public, @event.DeclaredAccessibility);
            Assert.True(@event.IsVirtual);
            Assert.False(@event.IsStatic);

            var addMethod = @event.AddMethod;
            Assert.Equal(MethodKind.EventAdd, addMethod.MethodKind);
            Assert.Equal("void C.E.add", addMethod.ToTestDisplayString());
            addMethod.CheckAccessorShape(@event);

            var removeMethod = @event.RemoveMethod;
            Assert.Equal(MethodKind.EventRemove, removeMethod.MethodKind);
            Assert.Equal("void C.E.remove", removeMethod.ToTestDisplayString());
            removeMethod.CheckAccessorShape(@event);

            Assert.True(@event.HasAssociatedField);

            var associatedField = @event.AssociatedField;
            Assert.Equal(SymbolKind.Field, associatedField.Kind);
            Assert.Equal(Accessibility.Private, associatedField.DeclaredAccessibility);
            Assert.False(associatedField.IsStatic);
            Assert.Equal(@event.Type, associatedField.Type);
        }

        [Fact]
        public void StaticFieldLikeEventDeclaration()
        {
            var text = @"
class C
{
    internal static event System.Action E;
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var @class = global.GetMember<NamedTypeSymbol>("C");

            var @event = @class.GetMember<EventSymbol>("E");

            Assert.Equal(SymbolKind.Event, @event.Kind);
            Assert.Equal(Accessibility.Internal, @event.DeclaredAccessibility);
            Assert.True(@event.IsStatic);

            var addMethod = @event.AddMethod;
            Assert.Equal(MethodKind.EventAdd, addMethod.MethodKind);
            Assert.Equal("void C.E.add", addMethod.ToTestDisplayString());
            addMethod.CheckAccessorShape(@event);

            var removeMethod = @event.RemoveMethod;
            Assert.Equal(MethodKind.EventRemove, removeMethod.MethodKind);
            Assert.Equal("void C.E.remove", removeMethod.ToTestDisplayString());
            removeMethod.CheckAccessorShape(@event);

            Assert.True(@event.HasAssociatedField);

            var associatedField = @event.AssociatedField;
            Assert.Equal(SymbolKind.Field, associatedField.Kind);
            Assert.Equal(Accessibility.Private, associatedField.DeclaredAccessibility);
            Assert.True(associatedField.IsStatic);
            Assert.Equal(@event.Type, associatedField.Type);
        }

        [Fact]
        public void InstanceCustomEventDeclaration()
        {
            var text = @"
class C
{
    protected event System.Action E { add { } remove { } }
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var @class = global.GetMember<NamedTypeSymbol>("C");

            var @event = @class.GetMember<EventSymbol>("E");

            Assert.Equal(SymbolKind.Event, @event.Kind);
            Assert.Equal(Accessibility.Protected, @event.DeclaredAccessibility);
            Assert.False(@event.IsVirtual);
            Assert.False(@event.IsStatic);

            var addMethod = @event.AddMethod;
            Assert.Equal(MethodKind.EventAdd, addMethod.MethodKind);
            Assert.Equal("void C.E.add", addMethod.ToTestDisplayString());
            addMethod.CheckAccessorShape(@event);

            var removeMethod = @event.RemoveMethod;
            Assert.Equal(MethodKind.EventRemove, removeMethod.MethodKind);
            Assert.Equal("void C.E.remove", removeMethod.ToTestDisplayString());
            removeMethod.CheckAccessorShape(@event);

            Assert.False(@event.HasAssociatedField);

            Assert.Null(@event.AssociatedField);
        }

        [Fact]
        public void StaticCustomEventDeclaration()
        {
            var text = @"
class C
{
    private static event System.Action E { add { } remove { } }
}
";
            var comp = CreateCompilation(text);
            var global = comp.GlobalNamespace;
            var @class = global.GetMember<NamedTypeSymbol>("C");

            var @event = @class.GetMember<EventSymbol>("E");

            Assert.Equal(SymbolKind.Event, @event.Kind);
            Assert.Equal(Accessibility.Private, @event.DeclaredAccessibility);
            Assert.False(@event.IsVirtual);
            Assert.True(@event.IsStatic);

            var addMethod = @event.AddMethod;
            Assert.Equal(MethodKind.EventAdd, addMethod.MethodKind);
            Assert.Equal("void C.E.add", addMethod.ToTestDisplayString());
            addMethod.CheckAccessorShape(@event);

            var removeMethod = @event.RemoveMethod;
            Assert.Equal(MethodKind.EventRemove, removeMethod.MethodKind);
            Assert.Equal("void C.E.remove", removeMethod.ToTestDisplayString());
            removeMethod.CheckAccessorShape(@event);

            Assert.False(@event.HasAssociatedField);

            Assert.Null(@event.AssociatedField);
        }

        [Fact]
        public void UseAccessorParameter()
        {
            var text = @"
class C
{
    protected event System.Action E 
    { 
        add 
        { 
            value();
        } 
        remove 
        { 
            value();
        } 
    }
}
";
            CreateCompilation(text).VerifyDiagnostics();
        }

        [ClrOnlyFact]
        public void EventInvocation()
        {
            var text =
@"using System;
public class Program
{
    static void Main()
    {
        EventHandler<EventArgs> handler1 = (s, ev) => System.Console.Write(""H1"");
        EventHandler<EventArgs> handler2 = (s, ev) => System.Console.Write(""H2"");
        var e = new E();
        e./*anchorE_3*/E1 += handler1;
        e.E1 += handler2;
       
        System.Console.Write(""T1"");
        e.TriggerE1();

        e.E1 -= handler1;
        System.Console.Write(""T2"");
        e.TriggerE1();
        
        e.E1 -= handler2;
        System.Console.Write(""T3"");
        e.TriggerE1();

        e.E2 += handler1;
        e.E2 += handler2;
       
        System.Console.Write(""T4"");
        e.TriggerE2();

        e.E2 -= handler1;
        System.Console.Write(""T5"");
        e.TriggerE2();
        
        e.E2 -= handler2;
        System.Console.Write(""T6"");
        e.TriggerE2();
    }
}
public class E
{
    public event EventHandler<EventArgs> E1;
    public EventHandler<EventArgs> _E2;
    public event EventHandler<EventArgs> E2
    {
        add { _E2 += value; }
        remove { _E2 -= value; }
    }

    public void Connect()
    {
        /*anchorE_1*/E1 += (s, e) => {};
    }

    public void TriggerE1() 
    {
        if(/*anchorE_2*/E1 != null) E1(null, null); 
    }
    public void TriggerE2() { if(_E2 != null) _E2(null, null); }
}
";

            var compVerifier = CompileAndVerify(text, expectedOutput: "T1H1H2T2H2T3T4H1H2T5H2T6");
            compVerifier.VerifyDiagnostics(DiagnosticDescription.None);
            var semanticModel = compVerifier.Compilation.GetSemanticModel(compVerifier.Compilation.SyntaxTrees.Single());

            var eventSymbol1 = semanticModel.LookupSymbols(text.IndexOf("/*anchorE_1*/", StringComparison.Ordinal), name: "E1").SingleOrDefault() as IEventSymbol;
            Assert.NotNull(eventSymbol1);

            var eventSymbol2 = semanticModel.LookupSymbols(text.IndexOf("/*anchorE_2*/", StringComparison.Ordinal), name: "E1").SingleOrDefault() as IEventSymbol;
            Assert.NotNull(eventSymbol2);
        }

        [WorkItem(542748, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542748")]
        [Fact()]
        public void FieldLikeEventAccessorIsSynthesized()
        {
            var text = @"
delegate void D();
class C
{
    internal event D FieldLikeEvent;
}
";
            var comp = CreateCompilation(text);
            NamedTypeSymbol type01 = comp.SourceModule.GlobalNamespace.GetTypeMembers("C").Single();
            var fevent = type01.GetMembers("FieldLikeEvent").Single() as EventSymbol;
            Assert.NotNull(fevent.AddMethod);
            Assert.True(fevent.AddMethod.IsImplicitlyDeclared, "FieldLikeEvent AddAccessor");
            Assert.NotNull(fevent.AddMethod);
            Assert.True(fevent.RemoveMethod.IsImplicitlyDeclared, "FieldLikeEvent RemoveAccessor");
        }

        [Fact, WorkItem(7845, "https://github.com/dotnet/roslyn/issues/7845")]
        public void EventWithDynamicAttribute_DynamicAttributeIsSynthesized()
        {
            var source = @"
class A
{
        public event System.Action<dynamic> E1;
        public event System.Action<dynamic> E2 { add {} remove {} }
        public System.Action<dynamic> P { get; set; }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var peModule = (PEModuleSymbol)module;
                var type = peModule.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
                var e1 = type.GetMember<EventSymbol>("E1");
                var e2 = type.GetMember<EventSymbol>("E2");
                var p = type.GetMember<PropertySymbol>("P");

                Assert.Equal("System.Action<dynamic>", e1.Type.ToTestDisplayString());
                Assert.Equal("System.Action<dynamic>", e2.Type.ToTestDisplayString());
                Assert.Equal("System.Action<dynamic>", p.Type.ToTestDisplayString());

                Assert.Equal(1, e1.AddMethod.ParameterTypesWithAnnotations.Length);
                Assert.Equal("System.Action<dynamic>", e1.AddMethod.ParameterTypesWithAnnotations[0].ToTestDisplayString());

                Assert.Equal(1, e1.RemoveMethod.ParameterTypesWithAnnotations.Length);
                Assert.Equal("System.Action<dynamic>", e1.RemoveMethod.ParameterTypesWithAnnotations[0].ToTestDisplayString());

                Assert.Equal(1, e2.AddMethod.ParameterTypesWithAnnotations.Length);
                Assert.Equal("System.Action<dynamic>", e2.AddMethod.ParameterTypesWithAnnotations[0].ToTestDisplayString());

                Assert.Equal(1, e2.RemoveMethod.ParameterTypesWithAnnotations.Length);
                Assert.Equal("System.Action<dynamic>", e2.RemoveMethod.ParameterTypesWithAnnotations[0].ToTestDisplayString());

                Assert.Equal(1, e1.GetAttributes(AttributeDescription.DynamicAttribute).Count());
                Assert.Equal(1, e2.GetAttributes(AttributeDescription.DynamicAttribute).Count());
                Assert.Equal(1, p.GetAttributes(AttributeDescription.DynamicAttribute).Count());
            };
            var comp = CreateEmptyCompilation(source, new[] { MscorlibRef, SystemCoreRef });
            CompileAndVerify(comp, symbolValidator: validator);
        }

        [Fact, WorkItem(7845, "https://github.com/dotnet/roslyn/issues/7845")]
        public void EventFieldWithDynamicAttribute_CannotCompileWithoutSystemCore()
        {
            var source = @"
public class A
{
    public event System.Action<dynamic> E1;
}";
            var libComp = CreateEmptyCompilation(source, new[] { MscorlibRef }).VerifyDiagnostics(
                // (4,32): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public event System.Action<dynamic> E1;
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(4, 32),
                // (4,41): warning CS0067: The event 'A.E1' is never used
                //     public event System.Action<dynamic> E1;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E1").WithArguments("A.E1").WithLocation(4, 41));
        }

        [Fact, WorkItem(7845, "https://github.com/dotnet/roslyn/issues/7845")]
        public void EventWithDynamicAttribute_CannotCompileWithoutSystemCore()
        {
            var source = @"
public class A
{
    public event System.Action<dynamic> E1 { add {} remove {} }
}";
            var libComp = CreateEmptyCompilation(source, references: new[] { MscorlibRef }).VerifyDiagnostics(
                // (4,32): error CS1980: Cannot define a class or member that utilizes 'dynamic' because the compiler required type 'System.Runtime.CompilerServices.DynamicAttribute' cannot be found. Are you missing a reference?
                //     public event System.Action<dynamic> E1 { add {} remove {} }
                Diagnostic(ErrorCode.ERR_DynamicAttributeMissing, "dynamic").WithArguments("System.Runtime.CompilerServices.DynamicAttribute").WithLocation(4, 32));
        }

        [Fact, WorkItem(7845, "https://github.com/dotnet/roslyn/issues/7845")]
        public void EventFieldWithDynamicAttribute_IsLoadedAsDynamic()
        {
            var libText = @"
public class C 
{
    public event System.Action<dynamic> E1;
    public void RaiseEvent() => E1(this); 
    public void Print() => System.Console.WriteLine(""Print method ran.""); 
}";
            var libComp = CreateCompilation(source: libText).VerifyDiagnostics();
            var libAssemblyRef = libComp.EmitToImageReference();

            var source = @"
class LambdaConsumer
{
    static void Main()
    {
        var c = new C();
        c.E1 += f => f.Print();
        c.RaiseEvent();
    }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = sourceModule.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var lambdaSyntax = tree.GetRoot().DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().First();
                Assert.Equal("f => f.Print()", lambdaSyntax.ToFullString());

                var lambdaTypeInfo = model.GetTypeInfo(lambdaSyntax);
                Assert.Equal("System.Action<dynamic>", lambdaTypeInfo.ConvertedType.ToTestDisplayString());

                var parameterSyntax = lambdaSyntax.DescendantNodes().OfType<ParameterSyntax>().First();
                var parameterSymbol = model.GetDeclaredSymbol(parameterSyntax);
                Assert.Equal("dynamic", parameterSymbol.Type.ToTestDisplayString());
            };

            CompileAndVerify(source: source, references: new[] { TargetFrameworkUtil.StandardCSharpReference, libAssemblyRef },
                                                    expectedOutput: "Print method ran.", sourceSymbolValidator: validator);
        }

        [Fact, WorkItem(7845, "https://github.com/dotnet/roslyn/issues/7845")]
        public void EventWithDynamicAttribute_IsLoadedAsDynamic()
        {
            var libText = @"
public class C {
    private System.Action<dynamic> _e1;
    public event System.Action<dynamic> E1 { add { _e1 = value; } remove {} }
    public void RaiseEvent() => _e1(this); 
    public void Print() => System.Console.WriteLine(""Print method ran.""); 
}";
            var libComp = CreateCompilation(source: libText);
            libComp.VerifyDiagnostics();
            var libAssemblyRef = libComp.EmitToImageReference();

            var source = @"
class D
{
    static void Main()
    {
        var c = new C();
        c.E1 += f => f.Print();
        c.RaiseEvent();
    }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var sourceModule = (SourceModuleSymbol)module;
                var compilation = module.DeclaringCompilation;
                var tree = compilation.SyntaxTrees.First();
                var model = compilation.GetSemanticModel(tree);

                var lambdaSyntax = tree.GetRoot().DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().First();
                Assert.Equal("f => f.Print()", lambdaSyntax.ToFullString());

                var lambdaTypeInfo = model.GetTypeInfo(lambdaSyntax);
                Assert.Equal("System.Action<dynamic>", lambdaTypeInfo.ConvertedType.ToTestDisplayString());

                var parameterSyntax = lambdaSyntax.DescendantNodes().OfType<ParameterSyntax>().First();
                var parameterSymbol = model.GetDeclaredSymbol(parameterSyntax);
                Assert.Equal("dynamic", parameterSymbol.Type.ToTestDisplayString());
            };

            var compilationVerifier = CompileAndVerify(source: source, references: new[] { TargetFrameworkUtil.StandardCSharpReference, libAssemblyRef },
                                                    expectedOutput: "Print method ran.");
        }

        [Fact, WorkItem(7845, "https://github.com/dotnet/roslyn/issues/7845")]
        public void EventFieldWithDynamicAttribute_IsLoadedAsDynamicViaCompilationRef()
        {
            var libText = @"
public class C 
{
    public event System.Action<dynamic> E1;
    public void RaiseEvent() => E1(this); 
    public void Print() => System.Console.WriteLine(""Print method ran.""); 
}";
            var libComp = CreateCompilation(libText);
            var libCompRef = new CSharpCompilationReference(libComp);
            var source = @"
class D
{
    static void Main()
    {
        var c = new C();
        c.E1 += f => f.Print();
        c.RaiseEvent();
    }
}";
            var expectedOutput = "Print method ran.";
            var compilationVerifier = CompileAndVerify(source: source,
                references: new[] { libCompRef },
                targetFramework: TargetFramework.StandardAndCSharp,
                expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(7845, "https://github.com/dotnet/roslyn/issues/7845")]
        public void EventFieldWithDynamicAttribute_CanBeOverridden()
        {
            var libText = @"
public class C 
{
    public virtual event System.Action<dynamic> E1 { add { System.Console.WriteLine(""Not called""); } remove {} }
    public virtual event System.Action<dynamic> E2 { add { System.Console.WriteLine(""Not called""); } remove {} }
    public virtual event System.Action<object> E3 { add { System.Console.WriteLine(""Not called""); } remove {} }
}";
            var libComp = CreateCompilation(source: libText);
            var libAssemblyRef = libComp.EmitToImageReference();
            var source = @"
class B : C
{
    public override event System.Action<dynamic> E1;
    public override event System.Action<object> E2;
    public override event System.Action<dynamic> E3;
    public void RaiseE1(object s) => E1(s);
    public void RaiseE2(string s) => E2(s);
    public void RaiseE3(string s) => E3(s);
}
class A
{
    static void Main()
    {
        var b1 = new B();
        b1.E1 += f => System.Console.WriteLine(f.Insert(0, ""Success1: ""));
        b1.E2 += f => System.Console.WriteLine(""Success2"");
        b1.E3 += f => System.Console.WriteLine(f.Insert(0, ""Success3: ""));
        b1.RaiseE1(""Alice"");
        b1.RaiseE2(""Bob"");
        b1.RaiseE3(""Charlie"");

        var b2 = new B();
        b2.E1 += Print;
        b2.E2 += Print;
        b2.E3 += Print;
        b2.RaiseE1(""Alice"");
        b2.RaiseE2(""Bob"");
        b2.RaiseE3(""Charlie"");
    }

    static void Print(object o) => System.Console.WriteLine(""Printed: "" + (System.String)o);
}";
            var expectedOutput =
@"Success1: Alice
Success2
Success3: Charlie
Printed: Alice
Printed: Bob
Printed: Charlie
";
            var compilationVerifier = CompileAndVerify(
                source: source,
                targetFramework: TargetFramework.StandardAndCSharp,
                references: new[] { libAssemblyRef },
                expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(7845, "https://github.com/dotnet/roslyn/issues/7845")]
        public void EventWithDynamicAttribute_OverriddenTypeDoesntLeak()
        {
            var libText = @"
public class CL1
{
    public virtual event System.Action<dynamic> E1 { add {} remove {} }
    public virtual event System.Action<dynamic> E2 { add {} remove {} }
}";
            var libComp = CreateCompilation(source: libText);
            var libAssemblyRef = libComp.EmitToImageReference();

            var source = @"
public class CL2 : CL1
{
    public override event System.Action<object> E1;
    public override event System.Action<object> E2 { add {} remove {} }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var peModule = (PEModuleSymbol)module;
                var type = peModule.GlobalNamespace.GetMember<NamedTypeSymbol>("CL2");
                var e1 = type.GetMember<EventSymbol>("E1");
                var e2 = type.GetMember<EventSymbol>("E2");

                Assert.Equal("System.Action<System.Object>", e1.Type.ToTestDisplayString());
                Assert.Equal("System.Action<System.Object>", e2.Type.ToTestDisplayString());
            };

            CompileAndVerify(source: source, references: new[] { libAssemblyRef }, symbolValidator: validator);
        }

        [Fact, WorkItem(7845, "https://github.com/dotnet/roslyn/issues/7845")]
        public void EventFieldWithDynamicAttribute_OverriddenTypeDoesntLeak()
        {
            var libText = @"
public class CL1
{
    public virtual event System.Action<dynamic> E1;
    public virtual event System.Action<dynamic> E2;
}";
            var libComp = CreateCompilation(source: libText);
            var libAssemblyRef = libComp.EmitToImageReference();

            var source = @"
public class CL2 : CL1
{
    public override event System.Action<object> E1;
    public override event System.Action<object> E2 { add {} remove {} }
}";
            Action<ModuleSymbol> validator = module =>
            {
                var peModule = (PEModuleSymbol)module;
                var type = peModule.GlobalNamespace.GetMember<NamedTypeSymbol>("CL2");
                var e1 = type.GetMember<EventSymbol>("E1");
                var e2 = type.GetMember<EventSymbol>("E2");

                Assert.Equal("System.Action<System.Object>", e1.Type.ToTestDisplayString());
                Assert.Equal("System.Action<System.Object>", e2.Type.ToTestDisplayString());
            };

            CompileAndVerify(source: source, references: new[] { libAssemblyRef }, symbolValidator: validator);
        }

        [Fact, WorkItem(7845, "https://github.com/dotnet/roslyn/issues/7845")]
        public void EventWithoutDynamicAttributeFromLegacyCompiler()
        {
            #region IL for class C
            // This is most of the IL generated by the native compiler (4.6) when compiling class C below:
            //  public class C
            //  {
            //      public event System.Action<dynamic> E;
            //      public void Raise() { E("Event raised"); }
            //  }
            // The important thing here is that the .event doesn't have a DynamicAttribute, but the add and remove accessors do.
            // Because of that the event will only be loaded as Action<object>, not Action<dynamic>

            var ilSource = @"
.assembly extern System.Core
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

.class public auto ansi beforefieldinit C
       extends [mscorlib]System.Object
{
  .field private class [mscorlib]System.Action`1<object> E
  .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 00 01 00 00 ) 
  .method public hidebysig specialname instance void 
          add_E(class [mscorlib]System.Action`1<object> 'value') cil managed
  {
    .param [1]
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 00 01 00 00 ) 
    // Code size       48 (0x30)
    .maxstack  3
    .locals init (class [mscorlib]System.Action`1<object> V_0,
             class [mscorlib]System.Action`1<object> V_1,
             class [mscorlib]System.Action`1<object> V_2,
             bool V_3)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [mscorlib]System.Action`1<object> C::E
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  stloc.1
    IL_0009:  ldloc.1
    IL_000a:  ldarg.1
    IL_000b:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Combine(class [mscorlib]System.Delegate,
                                                                                            class [mscorlib]System.Delegate)
    IL_0010:  castclass  class [mscorlib]System.Action`1<object>
    IL_0015:  stloc.2
    IL_0016:  ldarg.0
    IL_0017:  ldflda     class [mscorlib]System.Action`1<object> C::E
    IL_001c:  ldloc.2
    IL_001d:  ldloc.1
    IL_001e:  call       !!0 [mscorlib]System.Threading.Interlocked::CompareExchange<class [mscorlib]System.Action`1<object>>(!!0&,
                                                                                                                              !!0,
                                                                                                                              !!0)
    IL_0023:  stloc.0
    IL_0024:  ldloc.0
    IL_0025:  ldloc.1
    IL_0026:  ceq
    IL_0028:  ldc.i4.0
    IL_0029:  ceq
    IL_002b:  stloc.3
    IL_002c:  ldloc.3
    IL_002d:  brtrue.s   IL_0007

    IL_002f:  ret
  } // end of method C::add_E

  .method public hidebysig specialname instance void 
          remove_E(class [mscorlib]System.Action`1<object> 'value') cil managed
  {
    .param [1]
    .custom instance void [System.Core]System.Runtime.CompilerServices.DynamicAttribute::.ctor(bool[]) = ( 01 00 02 00 00 00 00 01 00 00 ) 
    // Code size       48 (0x30)
    .maxstack  3
    .locals init (class [mscorlib]System.Action`1<object> V_0,
             class [mscorlib]System.Action`1<object> V_1,
             class [mscorlib]System.Action`1<object> V_2,
             bool V_3)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [mscorlib]System.Action`1<object> C::E
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  stloc.1
    IL_0009:  ldloc.1
    IL_000a:  ldarg.1
    IL_000b:  call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::Remove(class [mscorlib]System.Delegate,
                                                                                           class [mscorlib]System.Delegate)
    IL_0010:  castclass  class [mscorlib]System.Action`1<object>
    IL_0015:  stloc.2
    IL_0016:  ldarg.0
    IL_0017:  ldflda     class [mscorlib]System.Action`1<object> C::E
    IL_001c:  ldloc.2
    IL_001d:  ldloc.1
    IL_001e:  call       !!0 [mscorlib]System.Threading.Interlocked::CompareExchange<class [mscorlib]System.Action`1<object>>(!!0&,
                                                                                                                              !!0,
                                                                                                                              !!0)
    IL_0023:  stloc.0
    IL_0024:  ldloc.0
    IL_0025:  ldloc.1
    IL_0026:  ceq
    IL_0028:  ldc.i4.0
    IL_0029:  ceq
    IL_002b:  stloc.3
    IL_002c:  ldloc.3
    IL_002d:  brtrue.s   IL_0007

    IL_002f:  ret
  } // end of method C::remove_E

  .method public hidebysig instance void 
          Raise() cil managed
  {
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class [mscorlib]System.Action`1<object> C::E
    IL_0007:  ldstr      ""Event raised""
    IL_000c: callvirt instance void class [mscorlib]
        System.Action`1<object>::Invoke(!0)
    IL_0011:  nop
    IL_0012:  ret
    } // end of method C::Raise

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
    {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call instance void [mscorlib]
        System.Object::.ctor()
    IL_0006:  ret
    } // end of method C::.ctor

  .event class [mscorlib]
    System.Action`1<object> E
  {
    .addon instance void C::add_E(class [mscorlib]
    System.Action`1<object>)
    .removeon instance void C::remove_E(class [mscorlib]
    System.Action`1<object>)
  } // end of event C::E
} // end of class C
";
            #endregion

            var source = @"
class D
{
    static void Main()
    {
        C x = new C();
        x.E += Print;
        x.Raise();
    }
    static void Print(object o) {
        System.Console.WriteLine(o.ToString());
    }
}
";
            var compVerifier = CompileAndVerify(source, new[] { TargetFrameworkUtil.StandardCSharpReference, CompileIL(ilSource) },
                                                expectedOutput: "Event raised");

            var comp = (CSharpCompilation)compVerifier.Compilation;
            var classSymbol = (PENamedTypeSymbol)comp.GetTypeByMetadataName("C");
            var eventSymbol = (PEEventSymbol)classSymbol.GetMember("E");
            Assert.Equal("System.Action<System.Object>", eventSymbol.Type.ToTestDisplayString());
        }

        [Fact]
        public void StaticEventDoesNotRequireInstanceReceiver()
        {
            var source = @"using System;
class C
{
    public static event EventHandler E;
}";
            var compilation = CreateCompilation(source).VerifyDiagnostics(
                // (4,38): warning CS0067: The event 'C.E' is never used
                //     public static event EventHandler E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 38));
            var eventSymbol = compilation.GetMember<EventSymbol>("C.E");
            Assert.False(eventSymbol.RequiresInstanceReceiver);
        }

        [Fact]
        public void InstanceEventRequiresInstanceReceiver()
        {
            var source = @"using System;
class C
{
    public event EventHandler E;
}";
            var compilation = CreateCompilation(source).VerifyDiagnostics(
                // (4,31): warning CS0067: The event 'C.E' is never used
                //     public event EventHandler E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E").WithLocation(4, 31));
            var eventSymbol = compilation.GetMember<EventSymbol>("C.E");
            Assert.True(eventSymbol.RequiresInstanceReceiver);
        }

        #endregion

        #region Error cases
        [ConditionalFact(typeof(NoUsedAssembliesValidation))] // The test hook is blocked by https://github.com/dotnet/roslyn/issues/39979
        [WorkItem(39979, "https://github.com/dotnet/roslyn/issues/39979")]
        public void VoidEvent()
        {
            var text =
@"interface I
{
    event void E;
}
class C
{
    event void E;
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (3,11): error CS1547: Keyword 'void' cannot be used in this context
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void"),
                // (7,11): error CS1547: Keyword 'void' cannot be used in this context
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void"),

                //CONSIDER: it would be nice to suppress these

                // (7,11): error CS0670: Field cannot have void type
                Diagnostic(ErrorCode.ERR_FieldCantHaveVoidType, "void"),
                // (7,16): warning CS0067: The event 'C.E' is never used
                //     event void E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E"));
        }

        [Fact]
        public void Assignment()
        {
            var text = @"
class DeclaringType
{
    public event System.Action<int> e { add { } remove { } }
    public event System.Action<int> f;

    void Method()
    {
        e = null; //CS0079
        f = null; //fine
    }
}

class OtherType
{
    void Method()
    {
        DeclaringType d = new DeclaringType();
        d.e = null; //CS0079
        d.f = null; //CS0070
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,9): error CS0079: The event 'DeclaringType.e' can only appear on the left hand side of += or -=
                //         e = null; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "e").WithArguments("DeclaringType.e"),
                // (19,11): error CS0079: The event 'DeclaringType.e' can only appear on the left hand side of += or -=
                //         d.e = null; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "e").WithArguments("DeclaringType.e"),
                // (20,11): error CS0070: The event 'DeclaringType.f' can only appear on the left hand side of += or -= (except when used from within the type 'DeclaringType')
                //         d.f = null; //CS0070
                Diagnostic(ErrorCode.ERR_BadEventUsage, "f").WithArguments("DeclaringType.f", "DeclaringType"));
        }

        [Fact]
        public void Overriding()
        {
            var text = @"
using System;

class C
{
    static void Main() { }

    public virtual event Action<int> e
    {
        add { }
        remove { }
    }
    public virtual event Action<int> f;

    void Goo()
    {
        e = null; //CS0079
        f = null; //fine
    }
}

class D : C
{
    public override event Action<int> e;
    public override event Action<int> f
    {
        add { }
        remove { }
    }

    void Goo()
    {
        e = null; //fine
        f = null; //CS0070 (since the least-overridden event is field-like)
    }
}

class E : D
{
    public sealed override event Action<int> e
    {
        add { }
        remove { }
    }
    public sealed override event Action<int> f;

    void Goo()
    {
        e = null; //CS0079
        f = null; //fine
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (17,9): error CS0079: The event 'C.e' can only appear on the left hand side of += or -=
                //         e = null; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "e").WithArguments("C.e"),
                // (34,9): error CS0070: The event 'C.f' can only appear on the left hand side of += or -= (except when used from within the type 'C')
                //         f = null; //CS0070 (since the least-overridden event is field-like)
                Diagnostic(ErrorCode.ERR_BadEventUsage, "f").WithArguments("C.f", "C"),
                // (49,9): error CS0079: The event 'C.e' can only appear on the left hand side of += or -=
                //         e = null; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "e").WithArguments("C.e"),

                // (24,39): warning CS0414: The field 'D.e' is assigned but its value is never used
                //     public override event Action<int> e;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "e").WithArguments("D.e"),
                // (45,46): warning CS0414: The field 'E.f' is assigned but its value is never used
                //     public sealed override event Action<int> f;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "f").WithArguments("E.f"),
                // (13,38): warning CS0414: The field 'C.f' is assigned but its value is never used
                //     public virtual event Action<int> f;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "f").WithArguments("C.f"));
        }

        [Fact]
        public void EventAccessibility()
        {
            var text = @"
class C
{
    private event System.Action e1;
    protected event System.Action e2;
    internal event System.Action e3;
    protected internal event System.Action e4;
    public event System.Action e5;

    private event System.Action f1 { add { } remove { } }
    protected event System.Action f2 { add { } remove { } }
    internal event System.Action f3 { add { } remove { } }
    protected internal event System.Action f4 { add { } remove { } }
    public event System.Action f5 { add { } remove { } }
}

class D
{
    void Goo(C c)
    {
        c.e1 = null; //CS0122
        c.e2 = null; //CS0122
        c.e3 = null; //CS0070
        c.e4 = null; //CS0070
        c.e5 = null; //CS0070

        c.f1 = null; //CS0122 (Dev10 also reports CS0079)
        c.f2 = null; //CS0122 (Dev10 also reports CS0079)
        c.f3 = null; //CS0079
        c.f4 = null; //CS0079
        c.f5 = null; //CS0079
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (21,11): error CS0122: 'C.e1' is inaccessible due to its protection level
                //         c.e1 = null; //CS0122
                Diagnostic(ErrorCode.ERR_BadAccess, "e1").WithArguments("C.e1"),
                // (22,11): error CS0122: 'C.e2' is inaccessible due to its protection level
                //         c.e2 = null; //CS0122
                Diagnostic(ErrorCode.ERR_BadAccess, "e2").WithArguments("C.e2"),
                // (23,11): error CS0070: The event 'C.e3' can only appear on the left hand side of += or -= (except when used from within the type 'C')
                //         c.e3 = null; //CS0070
                Diagnostic(ErrorCode.ERR_BadEventUsage, "e3").WithArguments("C.e3", "C"),
                // (24,11): error CS0070: The event 'C.e4' can only appear on the left hand side of += or -= (except when used from within the type 'C')
                //         c.e4 = null; //CS0070
                Diagnostic(ErrorCode.ERR_BadEventUsage, "e4").WithArguments("C.e4", "C"),
                // (25,11): error CS0070: The event 'C.e5' can only appear on the left hand side of += or -= (except when used from within the type 'C')
                //         c.e5 = null; //CS0070
                Diagnostic(ErrorCode.ERR_BadEventUsage, "e5").WithArguments("C.e5", "C"),
                // (27,11): error CS0122: 'C.f1' is inaccessible due to its protection level
                //         c.f1 = null; //CS0122 (Dev10 also reports CS0079)
                Diagnostic(ErrorCode.ERR_BadAccess, "f1").WithArguments("C.f1"),
                // (28,11): error CS0122: 'C.f2' is inaccessible due to its protection level
                //         c.f2 = null; //CS0122 (Dev10 also reports CS0079)
                Diagnostic(ErrorCode.ERR_BadAccess, "f2").WithArguments("C.f2"),
                // (29,11): error CS0079: The event 'C.f3' can only appear on the left hand side of += or -=
                //         c.f3 = null; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "f3").WithArguments("C.f3"),
                // (30,11): error CS0079: The event 'C.f4' can only appear on the left hand side of += or -=
                //         c.f4 = null; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "f4").WithArguments("C.f4"),
                // (31,11): error CS0079: The event 'C.f5' can only appear on the left hand side of += or -=
                //         c.f5 = null; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "f5").WithArguments("C.f5"),

                // (4,33): warning CS0067: The event 'C.e1' is never used
                //     private event System.Action e1;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "e1").WithArguments("C.e1"),
                // (5,35): warning CS0067: The event 'C.e2' is never used
                //     protected event System.Action e2;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "e2").WithArguments("C.e2"));
        }

        /// <summary>
        /// Even though the raise accessor is part of the event in metadata, it
        /// is just another method in C#.
        /// </summary>
        [Fact]
        public void InterfaceRaiseAccessor()
        {
            var ilSource = @"
.class interface public abstract auto ansi Interface
{

  .method public hidebysig newslot specialname abstract virtual instance void 
          add_e(class [mscorlib]System.Action 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance void 
          remove_e(class [mscorlib]System.Action 'value') cil managed
  {
  }

  .method public hidebysig newslot specialname abstract virtual instance void 
          raise_e(object sender, object e) cil managed
  {
  }

  .event [mscorlib]System.Action e
  {
    .addon instance void Interface::add_e(class [mscorlib]System.Action)
    .removeon instance void Interface::remove_e(class [mscorlib]System.Action)
    .fire instance void Interface::raise_e(object, object)
  } // end of event Interface::e

} // end of class Interface
";

            var csharpSource = @"
class C : Interface
{
    // not implementing event or raise (separate error for each)
}

class D : Interface
{
    public event System.Action e;
    // not implementing raise (error)
}

class E : Interface
{
    public event System.Action e;
    public void raise_e(object sender, object e) { }
}
";

            CreateCompilationWithILAndMscorlib40(csharpSource, ilSource).VerifyDiagnostics(
                // (2,7): error CS0535: 'C' does not implement interface member 'Interface.raise_e(object, object)'
                // class C : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("C", "Interface.raise_e(object, object)"),
                // (2,7): error CS0535: 'C' does not implement interface member 'Interface.e'
                // class C : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("C", "Interface.e"),
                // (7,7): error CS0535: 'D' does not implement interface member 'Interface.raise_e(object, object)'
                // class D : Interface
                Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "Interface").WithArguments("D", "Interface.raise_e(object, object)"),

                // (15,32): warning CS0067: The event 'E.e' is never used
                //     public event System.Action e;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "e").WithArguments("E.e"),
                // (9,32): warning CS0067: The event 'D.e' is never used
                //     public event System.Action e;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "e").WithArguments("D.e"));
        }

        [WorkItem(541704, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541704")]
        [Fact]
        public void OperationsInDeclaringType()
        {
            var text = @"
class C
{
    event System.Action E;
    event System.Action F { add { } remove { } }

    void Method(ref System.Action a)
    {
        E = a;
        E += a;
        a = E;
        Method(ref E);
        E.Invoke();
        bool b1 = E is System.Action;
        E++; //CS0023
        E |= true; //CS0019 (Dev10 also reports CS0029)

        F = a; //CS0079
        F += a;
        a = F; //CS0079
        Method(ref F); //CS0079
        F.Invoke(); //CS0079
        bool b2 = F is System.Action; //CS0079
        F++; //CS0079
        F |= true; //CS0079
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (15,9): error CS0023: Operator '++' cannot be applied to operand of type 'System.Action'
                //         E++; //CS0023
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "E++").WithArguments("++", "System.Action"),
                // (16,9): error CS0019: Operator '|=' cannot be applied to operands of type 'System.Action' and 'bool'
                //         E |= true; //CS0019 (Dev10 also reports CS0029)
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "E |= true").WithArguments("|=", "System.Action", "bool"),
                // (18,9): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         F = a; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"),
                // (20,13): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         a = F; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"),
                // (21,20): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         Method(ref F); //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"),
                // (22,9): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         F.Invoke(); //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"),
                // (23,19): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         bool b2 = F is System.Action; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"),
                // (24,9): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         F++; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"),
                // (25,9): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         F |= true; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"));
        }

        [Fact]
        public void OperationsInNonDeclaringType()
        {
            var text = @"
class C
{
    public event System.Action E;
    public event System.Action F { add { } remove { } }
}

class D
{
    void Method(ref System.Action a, C c)
    {
        c.E = a; //CS0070
        c.E += a;
        a = c.E; //CS0070
        Method(ref c.E, c); //CS0070
        c.E.Invoke(); //CS0070
        bool b1 = c.E is System.Action; //CS0070
        c.E++; //CS0070
        c.E |= true; //CS0070

        c.F = a; //CS0079
        c.F += a;
        a = c.F; //CS0079
        Method(ref c.F, c); //CS0079
        c.F.Invoke(); //CS0079
        bool b2 = c.F is System.Action; //CS0079
        c.F++; //CS0079
        c.F |= true; //CS0079
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (12,11): error CS0070: The event 'C.E' can only appear on the left hand side of += or -= (except when used from within the type 'C')
                //         c.E = a; //CS0070
                Diagnostic(ErrorCode.ERR_BadEventUsage, "E").WithArguments("C.E", "C"),
                // (14,15): error CS0070: The event 'C.E' can only appear on the left hand side of += or -= (except when used from within the type 'C')
                //         a = c.E; //CS0070
                Diagnostic(ErrorCode.ERR_BadEventUsage, "E").WithArguments("C.E", "C"),
                // (15,22): error CS0070: The event 'C.E' can only appear on the left hand side of += or -= (except when used from within the type 'C')
                //         Method(ref c.E, c); //CS0070
                Diagnostic(ErrorCode.ERR_BadEventUsage, "E").WithArguments("C.E", "C"),
                // (16,11): error CS0070: The event 'C.E' can only appear on the left hand side of += or -= (except when used from within the type 'C')
                //         c.E.Invoke(); //CS0070
                Diagnostic(ErrorCode.ERR_BadEventUsage, "E").WithArguments("C.E", "C"),
                // (17,21): error CS0070: The event 'C.E' can only appear on the left hand side of += or -= (except when used from within the type 'C')
                //         bool b1 = c.E is System.Action; //CS0070
                Diagnostic(ErrorCode.ERR_BadEventUsage, "E").WithArguments("C.E", "C"),
                // (18,11): error CS0070: The event 'C.E' can only appear on the left hand side of += or -= (except when used from within the type 'C')
                //         c.E++; //CS0070
                Diagnostic(ErrorCode.ERR_BadEventUsage, "E").WithArguments("C.E", "C"),
                // (19,11): error CS0070: The event 'C.E' can only appear on the left hand side of += or -= (except when used from within the type 'C')
                //         c.E |= true; //CS0070
                Diagnostic(ErrorCode.ERR_BadEventUsage, "E").WithArguments("C.E", "C"),
                // (21,11): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         c.F = a; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"),
                // (23,15): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         a = c.F; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"),
                // (24,22): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         Method(ref c.F, c); //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"),
                // (25,11): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         c.F.Invoke(); //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"),
                // (26,21): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         bool b2 = c.F is System.Action; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"),
                // (27,11): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         c.F++; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"),
                // (28,11): error CS0079: The event 'C.F' can only appear on the left hand side of += or -=
                //         c.F |= true; //CS0079
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "F").WithArguments("C.F"));
        }

        [Fact]
        public void ConversionFails()
        {
            var text = @"
class C
{
    event System.Action E;
    event System.Action F { add { } remove { } }

    void Method()
    {
        E += x => { };
        F += new System.Action<int>(x => {});
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (9,9): error CS1593: Delegate 'System.Action' does not take 1 arguments
                //         E += x => { };
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "E += x => { }").WithArguments("System.Action", "1"),
                // (10,9): error CS0029: Cannot implicitly convert type 'System.Action<int>' to 'System.Action'
                //         F += new System.Action<int>(x => {});
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "F += new System.Action<int>(x => {})").WithArguments("System.Action<int>", "System.Action"),

                // (4,25): warning CS0067: The event 'C.E' is never used
                //     event System.Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("C.E"));
        }

        [Fact]
        public void StructEvent1()
        {
            var text = @"
struct S
{
    event System.Action E;
    event System.Action F { add { } remove { } }

    S(int unused) : this()
    {
    }

    S(int unused1, int unused2)
    {
        // CS0171: E not initialized before C# 11
        // No error for F
    }

    S This { get { return this; } }

    void Method(S s) 
    {
        s.E = null; //fine, since receiver is a variable
        This.E = null; //CS1612: receiver is not a variable
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (11,5): error CS0171: Field 'S.E' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     S(int unused1, int unused2)
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S").WithArguments("S.E", "11.0").WithLocation(11, 5),
                // (22,9): error CS1612: Cannot modify the return value of 'S.This' because it is not a variable
                //         This.E = null; //CS1612: receiver is not a variable
                Diagnostic(ErrorCode.ERR_ReturnNotLValue, "This").WithArguments("S.This").WithLocation(22, 9));

            CreateCompilation(text, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (22,9): error CS1612: Cannot modify the return value of 'S.This' because it is not a variable
                //         This.E = null; //CS1612: receiver is not a variable
                Diagnostic(ErrorCode.ERR_ReturnNotLValue, "This").WithArguments("S.This").WithLocation(22, 9));
        }

        [WorkItem(546356, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546356")]
        [Fact]
        public void StructEvent2()
        {
            var source = @"
using System;

struct S
{
    event Action E;
    int P { get; set; }

    static S Make()
    {
        return new S();
    }

    static void Main()
    {
        Make().E += () => {}; // fine
        Make().P += 1; // CS1612
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (17,9): error CS1612: Cannot modify the return value of 'S.Make()' because it is not a variable
                //         Make().P += 1; // CS1612
                Diagnostic(ErrorCode.ERR_ReturnNotLValue, "Make()").WithArguments("S.Make()"),

                // (6,18): warning CS0067: The event 'S.E' is never used
                //     event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("S.E"));
        }

        [WorkItem(546356, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546356")]
        [Fact]
        public void StructEvent3()
        {
            var source = @"
struct S
{
    event System.Action E;
    event System.Action F { add { } remove { } }

    S Property { get { return this; } }
    S Method() { return this; }

    void Method(S parameter)
    {
        Property.E = null; //CS1612
        Method().E = null; //CS1612
        parameter.E = null;

        Property.E += null;
        Method().E += null;
        parameter.E += null;

        Property.F += null;
        Method().F += null;
        parameter.F += null;
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (12,9): error CS1612: Cannot modify the return value of 'S.Property' because it is not a variable
                //         Property.E = null;
                Diagnostic(ErrorCode.ERR_ReturnNotLValue, "Property").WithArguments("S.Property"),
                // (13,9): error CS1612: Cannot modify the return value of 'S.Method()' because it is not a variable
                //         Method().E = null;
                Diagnostic(ErrorCode.ERR_ReturnNotLValue, "Method()").WithArguments("S.Method()"));
        }

        // CONSIDER: it would be nice to test this scenario with an event from metadata,
        // but ilasm won't accept and event without both add and remove.
        [Fact]
        public void UseMissingAccessor()
        {
            var text = @"
class C
{
    event System.Action E { remove { } } //CS0065

    void Goo()
    {
        E += null; //no separate error
    }
}
";
            var expected = new[] {
                // (4,25): error CS0065: 'C.E': event property must have both add and remove accessors
                //     event System.Action E { remove { } }
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E").WithArguments("C.E")
                };

            CreateCompilation(text).VerifyDiagnostics(expected).VerifyEmitDiagnostics(expected);

            CreateCompilation(text).VerifyEmitDiagnostics(expected);
        }

        [WorkItem(542570, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542570")]
        [Fact]
        public void UseMissingAccessorInInterface()
        {
            var text = @"
delegate void myDelegate(int name = 1);
interface i1
{
    event myDelegate myevent { }
}
";

            CreateCompilation(text).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "myevent").WithArguments("i1.myevent"));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void CS1545ERR_BindToBogusProp2_AccessorSignatureMismatch()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual instance void 
          WithModopt(class [mscorlib]System.Action`1<int32 modopt(int32) []> 'value') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual instance void 
          WithoutModopt(class [mscorlib]System.Action`1<int32[]> 'value') cil managed
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

  // E: Y / A: Y / R: Y
  .event class [mscorlib]System.Action`1<int32 modopt(int32) []> Event7
  {
    .addon instance void Base::WithModopt(class [mscorlib]System.Action`1<int32 modopt(int32) []>)
    .removeon instance void Base::WithModopt(class [mscorlib]System.Action`1<int32 modopt(int32) []>)
  } // end of event Base::Event1

  // E: Y / A: Y / R: N
  .event class [mscorlib]System.Action`1<int32 modopt(int32) []> Event6
  {
    .addon instance void Base::WithModopt(class [mscorlib]System.Action`1<int32 modopt(int32) []>)
    .removeon instance void Base::WithoutModopt(class [mscorlib]System.Action`1<int32[]>)
  } // end of event Base::Event1

  // E: Y / A: N / R: Y
  .event class [mscorlib]System.Action`1<int32 modopt(int32) []> Event5
  {
    .addon instance void Base::WithoutModopt(class [mscorlib]System.Action`1<int32[]>)
    .removeon instance void Base::WithModopt(class [mscorlib]System.Action`1<int32 modopt(int32) []>)
  } // end of event Base::Event1

  // E: Y / A: N / R: N
  .event class [mscorlib]System.Action`1<int32 modopt(int32) []> Event4
  {
    .addon instance void Base::WithoutModopt(class [mscorlib]System.Action`1<int32[]>)
    .removeon instance void Base::WithoutModopt(class [mscorlib]System.Action`1<int32[]>)
  } // end of event Base::Event1

  // E: N / A: Y / R: Y
  .event class [mscorlib]System.Action`1<int32[]> Event3
  {
    .addon instance void Base::WithModopt(class [mscorlib]System.Action`1<int32 modopt(int32) []>)
    .removeon instance void Base::WithModopt(class [mscorlib]System.Action`1<int32 modopt(int32) []>)
  } // end of event Base::Event1

  // E: N / A: Y / R: N
  .event class [mscorlib]System.Action`1<int32[]> Event2
  {
    .addon instance void Base::WithModopt(class [mscorlib]System.Action`1<int32 modopt(int32) []>)
    .removeon instance void Base::WithoutModopt(class [mscorlib]System.Action`1<int32[]>)
  } // end of event Base::Event1

  // E: N / A: N / R: Y
  .event class [mscorlib]System.Action`1<int32[]> Event1
  {
    .addon instance void Base::WithoutModopt(class [mscorlib]System.Action`1<int32[]>)
    .removeon instance void Base::WithModopt(class [mscorlib]System.Action`1<int32 modopt(int32) []>)
  } // end of event Base::Event1

  // E: N / A: N / R: N
  .event class [mscorlib]System.Action`1<int32[]> Event0
  {
    .addon instance void Base::WithoutModopt(class [mscorlib]System.Action`1<int32[]>)
    .removeon instance void Base::WithoutModopt(class [mscorlib]System.Action`1<int32[]>)
  } // end of event Base::Event1
} // end of class Base
";

            var csharpSource = @"
class C
{
    void Method(Base b)
    {
        b.Event0 += null; //fine
        b.Event1 += null;
        b.Event2 += null;
        b.Event3 += null;
        b.Event4 += null;
        b.Event5 += null;
        b.Event6 += null;
        b.Event7 += null; //fine
    }
}
";

            CreateCompilationWithILAndMscorlib40(csharpSource, ilSource).VerifyDiagnostics(
                // (7,11): error CS1545: Property, indexer, or event 'Base.Event1' is not supported by the language; try directly calling accessor methods 'Base.Event0.add' or 'Base.Event7.add'
                //         b.Event1 += null;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Event1").WithArguments("Base.Event1", "Base.Event0.add", "Base.Event7.add"),
                // (8,11): error CS1545: Property, indexer, or event 'Base.Event2' is not supported by the language; try directly calling accessor methods 'Base.Event7.add' or 'Base.Event0.add'
                //         b.Event2 += null;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Event2").WithArguments("Base.Event2", "Base.Event7.add", "Base.Event0.add"),
                // (9,11): error CS1545: Property, indexer, or event 'Base.Event3' is not supported by the language; try directly calling accessor methods 'Base.Event7.add' or 'Base.Event7.add'
                //         b.Event3 += null;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Event3").WithArguments("Base.Event3", "Base.Event7.add", "Base.Event7.add"),
                // (10,11): error CS1545: Property, indexer, or event 'Base.Event4' is not supported by the language; try directly calling accessor methods 'Base.Event0.add' or 'Base.Event0.add'
                //         b.Event4 += null;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Event4").WithArguments("Base.Event4", "Base.Event0.add", "Base.Event0.add"),
                // (11,11): error CS1545: Property, indexer, or event 'Base.Event5' is not supported by the language; try directly calling accessor methods 'Base.Event0.add' or 'Base.Event7.add'
                //         b.Event5 += null;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Event5").WithArguments("Base.Event5", "Base.Event0.add", "Base.Event7.add"),
                // (12,11): error CS1545: Property, indexer, or event 'Base.Event6' is not supported by the language; try directly calling accessor methods 'Base.Event7.add' or 'Base.Event0.add'
                //         b.Event6 += null;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Event6").WithArguments("Base.Event6", "Base.Event7.add", "Base.Event0.add"));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void CallAccessorsDirectly()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual instance void 
          add_Event1(class [mscorlib]System.Action`1<int32[]> 'value') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual instance void 
          remove_Event1(class [mscorlib]System.Action`1<int32[]> 'value') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual instance void 
          add_Event2(class [mscorlib]System.Action`1<int32[]> 'value') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual instance void 
          remove_Event2(class [mscorlib]System.Action`1<int32[]> 'value') cil managed
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

  // Accessors are missing modopt
  .event class [mscorlib]System.Action`1<int32 modopt(int32) []> Event1
  {
    .addon instance void Base::add_Event1(class [mscorlib]System.Action`1<int32[]>)
    .removeon instance void Base::remove_Event1(class [mscorlib]System.Action`1<int32[]>)
  } // end of event Base::Event1

  // Accessors are fine
  .event class [mscorlib]System.Action`1<int32[]> Event2
  {
    .addon instance void Base::add_Event2(class [mscorlib]System.Action`1<int32[]>)
    .removeon instance void Base::remove_Event2(class [mscorlib]System.Action`1<int32[]>)
  } // end of event Base::Event2
} // end of class Base
";

            var csharpSource = @"
class C
{
    void Method(Base b)
    {
        b.add_Event1(null); //fine, since event is bogus
        b.add_Event2(null); //CS0571 - can't call directly - use event accessor
    }
}
";

            CreateCompilationWithILAndMscorlib40(csharpSource, ilSource).VerifyDiagnostics(
                // (7,11): error CS0571: 'Base.Event2.add': cannot explicitly call operator or accessor
                //         b.add_Event2(null);
                Diagnostic(ErrorCode.ERR_CantCallSpecialMethod, "add_Event2").WithArguments("Base.Event2.add"));
        }

        [Fact]
        public void BogusAccessorSignatures()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual instance void 
          remove_Event(class [mscorlib]System.Action`1<int32> 'value') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual instance void 
          add_Event1(class [mscorlib]System.Action 'value') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual instance void 
          add_Event2(class [mscorlib]System.Action`1<int64> 'value') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual instance void 
          add_Event3(class [mscorlib]System.Action`1<int32> 'value', int32 'extra') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual instance int32 
          add_Event4(class [mscorlib]System.Action`1<int32> 'value') cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  // Adder has different type
  .event class [mscorlib]System.Action`1<int32> Event1
  {
    .addon instance void Base::add_Event1(class [mscorlib]System.Action)
    .removeon instance void Base::remove_Event(class [mscorlib]System.Action`1<int32>)
  } // end of event Base::Event1

  // Adder has different type arg
  .event class [mscorlib]System.Action`1<int32> Event2
  {
    .addon instance void Base::add_Event2(class [mscorlib]System.Action`1<int64>)
    .removeon instance void Base::remove_Event(class [mscorlib]System.Action`1<int32>)
  } // end of event Base::Event2

  // Adder has an extra parameter
  .event class [mscorlib]System.Action`1<int32> Event3
  {
    .addon instance void Base::add_Event3(class [mscorlib]System.Action`1<int32>, int32)
    .removeon instance void Base::remove_Event(class [mscorlib]System.Action`1<int32>)
  } // end of event Base::Event3

  // Adder has a return type
  .event class [mscorlib]System.Action`1<int32> Event4
  {
    .addon instance int32 Base::add_Event4(class [mscorlib]System.Action`1<int32>)
    .removeon instance void Base::remove_Event(class [mscorlib]System.Action`1<int32>)
  } // end of event Base::Event4
} // end of class Base
";

            var csharpSource = @"
class C
{
    void Method(Base b)
    {
        b.Event1 += null;
        b.Event2 += null;
        b.Event3 += null;
        b.Event4 += null;
    }
}
";

            CreateCompilationWithILAndMscorlib40(csharpSource, ilSource).VerifyDiagnostics(
                // (6,11): error CS1545: Property, indexer, or event 'Base.Event1' is not supported by the language; try directly calling accessor methods 'Base.add_Event1(System.Action)' or 'Base.remove_Event(System.Action<int>)'
                //         b.Event1 += null;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Event1").WithArguments("Base.Event1", "Base.add_Event1(System.Action)", "Base.remove_Event(System.Action<int>)"),
                // (7,11): error CS1545: Property, indexer, or event 'Base.Event2' is not supported by the language; try directly calling accessor methods 'Base.add_Event2(System.Action<long>)' or 'Base.remove_Event(System.Action<int>)'
                //         b.Event2 += null;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Event2").WithArguments("Base.Event2", "Base.add_Event2(System.Action<long>)", "Base.remove_Event(System.Action<int>)"),
                // (8,11): error CS1545: Property, indexer, or event 'Base.Event3' is not supported by the language; try directly calling accessor methods 'Base.add_Event3(System.Action<int>, int)' or 'Base.remove_Event(System.Action<int>)'
                //         b.Event3 += null;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Event3").WithArguments("Base.Event3", "Base.add_Event3(System.Action<int>, int)", "Base.remove_Event(System.Action<int>)"),
                // (9,11): error CS1545: Property, indexer, or event 'Base.Event4' is not supported by the language; try directly calling accessor methods 'Base.add_Event4(System.Action<int>)' or 'Base.remove_Event(System.Action<int>)'
                //         b.Event4 += null;
                Diagnostic(ErrorCode.ERR_BindToBogusProp2, "Event4").WithArguments("Base.Event4", "Base.add_Event4(System.Action<int>)", "Base.remove_Event(System.Action<int>)"));
        }

        [Fact]
        public void InaccessibleAccessor()
        {
            var ilSource = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual instance void 
          remove_Event(class [mscorlib]System.Action`1<int32> 'value') cil managed
  {
    ret
  }

  .method private hidebysig newslot specialname virtual instance void 
          add_Event1(class [mscorlib]System.Action`1<int32> 'value') cil managed
  {
    ret
  }

  .method family hidebysig newslot specialname virtual instance void 
          add_Event2(class [mscorlib]System.Action`1<int32> 'value') cil managed
  {
    ret
  }

  .method assembly hidebysig newslot specialname virtual instance void 
          add_Event3(class [mscorlib]System.Action`1<int32> 'value') cil managed
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

  .event class [mscorlib]System.Action`1<int32> Event1
  {
    .addon instance void Base::add_Event1(class [mscorlib]System.Action`1<int32>)
    .removeon instance void Base::remove_Event(class [mscorlib]System.Action`1<int32>)
  } // end of event Base::Event1

  .event class [mscorlib]System.Action`1<int32> Event2
  {
    .addon instance void Base::add_Event2(class [mscorlib]System.Action`1<int32>)
    .removeon instance void Base::remove_Event(class [mscorlib]System.Action`1<int32>)
  } // end of event Base::Event2

  .event class [mscorlib]System.Action`1<int32> Event3
  {
    .addon instance void Base::add_Event3(class [mscorlib]System.Action`1<int32>)
    .removeon instance void Base::remove_Event(class [mscorlib]System.Action`1<int32>)
  } // end of event Base::Event3
} // end of class Base
";

            var csharpSource = @"
class C
{
    void Method(Base b)
    {
        b.Event1 += null;
        b.Event2 += null;
        b.Event3 += null;
    }
}
";

            var compilation = CreateCompilationWithILAndMscorlib40(csharpSource, ilSource);

            compilation.VerifyDiagnostics(
                // (6,9): error CS0122: 'Base.Event1.add' is inaccessible due to its protection level
                //         b.Event1 += null;
                Diagnostic(ErrorCode.ERR_BadAccess, "b.Event1 += null").WithArguments("Base.Event1.add"),
                // (7,9): error CS0122: 'Base.Event2.add' is inaccessible due to its protection level
                //         b.Event2 += null;
                Diagnostic(ErrorCode.ERR_BadAccess, "b.Event2 += null").WithArguments("Base.Event2.add"),
                // (8,9): error CS0122: 'Base.Event3.add' is inaccessible due to its protection level
                //         b.Event3 += null;
                Diagnostic(ErrorCode.ERR_BadAccess, "b.Event3 += null").WithArguments("Base.Event3.add"));

            var @class = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var event1 = @class.GetMember<EventSymbol>("Event1");
            var event2 = @class.GetMember<EventSymbol>("Event2");
            var event3 = @class.GetMember<EventSymbol>("Event3");

            Assert.NotNull(event1.AddMethod);
            Assert.Equal(Accessibility.Private, event1.AddMethod.DeclaredAccessibility);
            Assert.NotNull(event1.RemoveMethod);
            Assert.Equal(Accessibility.Public, event1.RemoveMethod.DeclaredAccessibility);

            Assert.NotNull(event2.AddMethod);
            Assert.Equal(Accessibility.Protected, event2.AddMethod.DeclaredAccessibility);
            Assert.NotNull(event2.RemoveMethod);
            Assert.Equal(Accessibility.Public, event2.RemoveMethod.DeclaredAccessibility);

            Assert.NotNull(event3.AddMethod);
            Assert.Equal(Accessibility.Internal, event3.AddMethod.DeclaredAccessibility);
            Assert.NotNull(event3.RemoveMethod);
            Assert.Equal(Accessibility.Public, event3.RemoveMethod.DeclaredAccessibility);
        }

        [WorkItem(538956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538956")]
        [Fact]
        public void EventAccessorDoesNotHideMethod()
        {
            const string cSharpSource = @"
interface IA {
    void add_E(string e);
}

interface IB : IA {
    event System.Action E;
}

class Program {
    static void Main() {
        IB x = null;
        x.add_E(null);
    }
}
";
            CreateCompilation(cSharpSource).VerifyDiagnostics();
        }

        [WorkItem(538956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538956")]
        [Fact]
        public void EventAccessorDoesNotConflictWithMethod()
        {
            const string cSharpSource = @"
interface IA {
    void add_E(string e);
}

interface IB {
    event System.Action E;
}

interface IC : IA, IB { }

class Program {
    static void Main() {
        IC x = null;
        x.add_E(null);
    }
}
";
            CreateCompilation(cSharpSource).VerifyDiagnostics();
        }

        [WorkItem(538992, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538992")]
        [Fact]
        public void CannotAccessEventThroughParenthesizedType()
        {
            const string cSharpSource = @"
class Program
{
    static event System.Action E;

    static void Main()
    {
        (Program).E();
    }
}
";
            CreateCompilation(cSharpSource).VerifyDiagnostics(
                // (8,10): error CS0119: 'Program' is a 'type', which is not valid in the given context
                //         (Program).E();
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Program").WithArguments("Program", "type"));
        }

        [Fact]
        public void CustomEventInvocable()
        {
            const string cSharpSource = @"
class Outer
{
    public static void Q()
    {
    }

    class Goo
    {
        public static event System.Action Q { add { } remove { } }

        class Bar
        {
            void f()
            {
                Q();
            }
        }
    }
}
";
            CreateCompilation(cSharpSource).VerifyDiagnostics(
                // (16,17): error CS0079: The event 'Outer.Goo.Q' can only appear on the left hand side of += or -=
                //                 Q();
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "Q").WithArguments("Outer.Goo.Q"));
        }

        [WorkItem(542461, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542461")]
        [Fact]
        public void EventMustDelegate()
        {
            const string cSharpSource = @"
using System;
namespace MyCollections
{
    using System.Collections;
    public delegate void ChangedEventHandler(object sender, EventArgs e);
    public class ListWithChangedEvent : ArrayList
    {
        public event ListWithChangedEvent Changed;
        protected virtual void OnChanged(EventArgs e)
        {
            if (Changed != null)
                Changed(this, e);
        }
        public override int Add(object value)
        {
            int i = base.Add(value);
            OnChanged(EventArgs.Empty);
            return i;
        }
        public override object this[int index]
        {
            set
            {
                base[index] = value;
                OnChanged(EventArgs.Empty);
            }
        }
    }
}
namespace TestEvents
{
    using MyCollections;

    class EventListener
    {
        private ListWithChangedEvent List;

        public EventListener(ListWithChangedEvent list)
        {
            List = list;
            List.Changed += new ChangedEventHandler(ListChanged);
        }

        private void ListChanged(object sender, EventArgs e)
        {
        }

        public void Detach()
        {
            List.Changed -= new ChangedEventHandler(ListChanged);
            List = null;
        }
    }
}

";
            CreateCompilation(cSharpSource).VerifyDiagnostics(
                // (9,43): error CS0066: 'MyCollections.ListWithChangedEvent.Changed': event must be of a delegate type
                //         public event ListWithChangedEvent Changed;
                Diagnostic(ErrorCode.ERR_EventNotDelegate, "Changed").WithArguments("MyCollections.ListWithChangedEvent.Changed"),

                // Dev10 doesn't report this cascading error, but it seems reasonable since the field isn't a delegate.

                // (13,17): error CS0149: Method name expected
                //                 Changed(this, e);
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "Changed"));
        }

        [WorkItem(543791, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543791")]
        [Fact]
        public void MultipleDeclaratorsOneError()
        {
            var source = @"
class A
{
    event Unknown a, b;
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (4,5): error CS0246: The type or namespace name 'Unknown' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unknown").WithArguments("Unknown"),

                // (4,19): warning CS0067: The event 'A.a' is never used
                //     event Unknown a, b;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "a").WithArguments("A.a"),
                // (4,22): warning CS0067: The event 'A.b' is never used
                //     event Unknown a, b;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "b").WithArguments("A.b"));
        }

        [WorkItem(545682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545682")]
        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void EventHidingMethod()
        {
            var source1 =
@".class public A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public instance void E1() { ret }
  .method public instance void E2() { ret }
}
.class public B extends A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public instance void add_E1(class [mscorlib]System.Action v) { ret }
  .method public instance void remove_E1(class [mscorlib]System.Action v) { ret }
  .event [mscorlib]System.Action E1
  {
    .addon instance void B::add_E1(class [mscorlib]System.Action);
    .removeon instance void B::remove_E1(class [mscorlib]System.Action);
  }
  .method public instance void add_E2(class [mscorlib]System.Action v) { ret }
  .method public instance void remove_E2() { ret }
  .event [mscorlib]System.Action E2
  {
    .addon instance void B::add_E2(class [mscorlib]System.Action);
    .removeon instance void B::remove_E2();
  }
}";
            var reference1 = CompileIL(source1);
            var source2 =
@"class C
{
    static void M(B b)
    {
        b.E1(); // B.E1 valid, should hide A.E1
        b.E2(); // B.E2 invalid, should not hide A.E2
    }
}";
            var compilation2 = CreateCompilation(source2, new[] { reference1 });
            compilation2.VerifyDiagnostics(
                // (5, 11): error CS0079: The event 'B.E1' can only appear on the left hand side of += or -=
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "E1").WithArguments("B.E1").WithLocation(5, 11));
        }

        [WorkItem(547071, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547071")]
        [Fact]
        public void InvalidEventDeclarations()
        {
            CreateCompilation("event this").VerifyDiagnostics(
                // (1,7): error CS1031: Type expected
                // event this
                Diagnostic(ErrorCode.ERR_TypeExpected, "this"),
                // (1,11): error CS1514: { expected
                // event this
                Diagnostic(ErrorCode.ERR_LbraceExpected, ""),
                // (1,11): error CS1513: } expected
                // event this
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
                // (1,7): error CS0065: '<invalid-global-code>.': event property must have both add and remove accessors
                // event this
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "").WithArguments("<invalid-global-code>."));

            CreateCompilation("event System.Action E<T>").VerifyDiagnostics(
                // (1,21): error CS7002: Unexpected use of a generic name
                // event System.Action E<T>
                Diagnostic(ErrorCode.ERR_UnexpectedGenericName, "E"),
                // (1,25): error CS1514: { expected
                // event System.Action E<T>
                Diagnostic(ErrorCode.ERR_LbraceExpected, ""),
                // (1,25): error CS1513: } expected
                // event System.Action E<T>
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
                // (1,21): error CS0065: '<invalid-global-code>.E': event property must have both add and remove accessors
                // event System.Action E<T>
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E").WithArguments("<invalid-global-code>.E"));

            CreateCompilation("event").VerifyDiagnostics(
                // (1,6): error CS1031: Type expected
                // event
                Diagnostic(ErrorCode.ERR_TypeExpected, ""),
                // (1,6): error CS1514: { expected
                // event
                Diagnostic(ErrorCode.ERR_LbraceExpected, ""),
                // (1,6): error CS1513: } expected
                // event
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
                // (1,6): error CS0065: '<invalid-global-code>.': event property must have both add and remove accessors
                // event
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "").WithArguments("<invalid-global-code>."));

            CreateCompilation("event System.Action ").VerifyDiagnostics(
                // (1,21): error CS1001: Identifier expected
                // event System.Action 
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ""),
                // (1,21): error CS1514: { expected
                // event System.Action 
                Diagnostic(ErrorCode.ERR_LbraceExpected, ""),
                // (1,21): error CS1513: } expected
                // event System.Action 
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
                // (1,21): error CS0065: '<invalid-global-code>.': event property must have both add and remove accessors
                // event System.Action 
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "").WithArguments("<invalid-global-code>."));

            CreateCompilation("event System.Action System.IFormattable.").VerifyDiagnostics(
                // (1,40): error CS0071: An explicit interface implementation of an event must use event accessor syntax
                // event System.Action System.IFormattable.
                Diagnostic(ErrorCode.ERR_ExplicitEventFieldImpl, ".").WithLocation(1, 40),
                // (1,41): error CS1001: Identifier expected
                // event System.Action System.IFormattable.
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 41),
                // (1,21): error CS0540: '<invalid-global-code>.': containing type does not implement interface 'IFormattable'
                // event System.Action System.IFormattable.
                Diagnostic(ErrorCode.ERR_ClassDoesntImplementInterface, "System.IFormattable").WithArguments("<invalid-global-code>.", "System.IFormattable").WithLocation(1, 21),
                // (1,41): error CS0539: '<invalid-global-code>.' in explicit interface declaration is not a member of interface
                // event System.Action System.IFormattable.
                Diagnostic(ErrorCode.ERR_InterfaceMemberNotFound, "").WithArguments("<invalid-global-code>.").WithLocation(1, 41));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void OverriddenEventCustomModifiers()
        {
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .field private class [mscorlib]System.Action`1<int32 modopt(int64) []> E
  .method public hidebysig newslot specialname virtual 
          instance void  add_E(class [mscorlib]System.Action`1<int32 modopt(int64) []> 'value') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual 
          instance void  remove_E(class [mscorlib]System.Action`1<int32 modopt(int64) []> 'value') cil managed
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

  .event class [mscorlib]System.Action`1<int32 modopt(int64) []> E
  {
    .addon instance void Base::add_E(class [mscorlib]System.Action`1<int32 modopt(int64) []>)
    .removeon instance void Base::remove_E(class [mscorlib]System.Action`1<int32 modopt(int64) []>)
  } // end of event Base::E
} // end of class Base
";

            var source = @"
using System;

class Derived1 : Base
{
    public override event Action<int[]> E;
}

class Derived2 : Base
{
    public override event Action<int[]> E
    {
        add { }
        remove { }
    }
}
";

            var comp = CreateCompilation(source, new[] { CompileIL(il) });
            comp.VerifyDiagnostics(
                // (6,41): warning CS0067: The event 'Derived1.E' is never used
                //     public override event Action<int[]> E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("Derived1.E"));

            var global = comp.GlobalNamespace;

            var @base = global.GetMember<NamedTypeSymbol>("Base");
            var baseEvent = @base.GetMember<EventSymbol>("E");
            var baseEventType = baseEvent.Type;
            Assert.Equal("System.Action<System.Int32 modopt(System.Int64) []>", baseEventType.ToTestDisplayString()); // Note modopt

            var derived1 = global.GetMember<NamedTypeSymbol>("Derived1");
            var event1 = derived1.GetMember<EventSymbol>("E");
            Assert.Equal(baseEventType, event1.Type);
            Assert.Equal(baseEventType, event1.AssociatedField.Type);
            Assert.Equal(baseEventType, event1.AddMethod.ParameterTypesWithAnnotations.Single().Type);
            Assert.Equal(baseEventType, event1.RemoveMethod.ParameterTypesWithAnnotations.Single().Type);

            var derived2 = global.GetMember<NamedTypeSymbol>("Derived2");
            var event2 = derived2.GetMember<EventSymbol>("E");
            Assert.Equal(baseEventType, event2.Type);
            Assert.Null(event2.AssociatedField);
            Assert.Equal(baseEventType, event2.AddMethod.ParameterTypesWithAnnotations.Single().Type);
            Assert.Equal(baseEventType, event2.RemoveMethod.ParameterTypesWithAnnotations.Single().Type);
        }

        [Fact]
        public void OverriddenAccessorName()
        {
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .field private class [mscorlib]System.Action E
  .method public hidebysig newslot specialname virtual 
          instance void  myAdd(class [mscorlib]System.Action 'value') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual 
          instance void  myRemove(class [mscorlib]System.Action 'value') cil managed
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

  .event [mscorlib]System.Action E
  {
    .addon instance void Base::myAdd(class [mscorlib]System.Action)
    .removeon instance void Base::myRemove(class [mscorlib]System.Action)
  } // end of event Base::E
} // end of class Base
";

            var source = @"
using System;

class Derived1 : Base
{
    public override event Action E;
}

class Derived2 : Base
{
    public override event Action E
    {
        add { }
        remove { }
    }
}
";

            var comp = CreateCompilation(source, new[] { CompileIL(il) });
            comp.VerifyDiagnostics(
                // (6,34): warning CS0067: The event 'Derived1.E' is never used
                //     public override event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("Derived1.E"));

            var derived1 = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived1");
            var event1 = derived1.GetMember<EventSymbol>("E");
            Assert.Equal("myAdd", event1.AddMethod.Name);
            Assert.Equal("myRemove", event1.RemoveMethod.Name);

            var derived2 = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived2");
            var event2 = derived2.GetMember<EventSymbol>("E");
            Assert.Equal("myAdd", event2.AddMethod.Name);
            Assert.Equal("myRemove", event2.RemoveMethod.Name);
        }

        [Fact, WorkItem(570905, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/570905")]
        public void OverriddenAccessorName_BaseMissingAccessor()
        {
            var source = @"
using System;

class Base
{
    public virtual event Action E { } // Missing accessors.
}

class Derived1 : Base
{
    public override event Action E;
}

class Derived2 : Base
{
    public override event Action E
    {
        add { }
        remove { }
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,33): error CS0065: 'Base.E': event property must have both add and remove accessors
                //     public virtual event Action E { } // Missing accessors.
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E").WithArguments("Base.E"),
                // (11,34): warning CS0067: The event 'Derived1.E' is never used
                //     public override event Action E;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("Derived1.E"));

            var derived1 = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived1");
            var event1 = derived1.GetMember<EventSymbol>("E");
            Assert.Equal("add_E", event1.AddMethod.Name);
            Assert.Equal("remove_E", event1.RemoveMethod.Name);

            var derived2 = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived2");
            var event2 = derived2.GetMember<EventSymbol>("E");
            Assert.Equal("add_E", event2.AddMethod.Name);
            Assert.Equal("remove_E", event2.RemoveMethod.Name);
        }

        [WorkItem(850168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850168")]
        [Fact]
        public void AbstractFieldLikeEvent()
        {
            var source = @"
using System;

public abstract class A
{
    public abstract event Action E;
    public abstract event Action F = null; // Invalid initializer.
}
";

            var comp = CreateCompilation(source);

            var typeA = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");
            var eventE = typeA.GetMember<EventSymbol>("E");
            var eventF = typeA.GetMember<EventSymbol>("F");

            Assert.Null(eventE.AssociatedField);
            Assert.NotNull(eventF.AssociatedField); // Since it has an initializer.
        }

        [Fact, WorkItem(406, "https://github.com/dotnet/roslyn/issues/406")]
        public void AbstractBaseEvent()
        {
            var source =
@"using System;

namespace ConsoleApplication3
{
    public abstract class BaseWithAbstractEvent
    {
        public abstract event Action MyEvent;
    }

    public class SuperWithOverriddenEvent : BaseWithAbstractEvent
    {
        public override event Action MyEvent
        {
            add { base.MyEvent += value; } // error
            remove { base.MyEvent -= value; } // error
        }

        public void Goo()
        {
            base.MyEvent += Goo; // error
        }
    }

    class Program
    {
        static void Main()
        {
            SuperWithOverriddenEvent swoe = new SuperWithOverriddenEvent();
            swoe.MyEvent += Main;
        }
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (14,19): error CS0205: Cannot call an abstract base member: 'BaseWithAbstractEvent.MyEvent'
                //             add { base.MyEvent += value; } // error
                Diagnostic(ErrorCode.ERR_AbstractBaseCall, "base.MyEvent += value").WithArguments("ConsoleApplication3.BaseWithAbstractEvent.MyEvent").WithLocation(14, 19),
                // (15,22): error CS0205: Cannot call an abstract base member: 'BaseWithAbstractEvent.MyEvent'
                //             remove { base.MyEvent -= value; } // error
                Diagnostic(ErrorCode.ERR_AbstractBaseCall, "base.MyEvent -= value").WithArguments("ConsoleApplication3.BaseWithAbstractEvent.MyEvent").WithLocation(15, 22),
                // (20,13): error CS0205: Cannot call an abstract base member: 'BaseWithAbstractEvent.MyEvent'
                //             base.MyEvent += Goo; // error
                Diagnostic(ErrorCode.ERR_AbstractBaseCall, "base.MyEvent += Goo").WithArguments("ConsoleApplication3.BaseWithAbstractEvent.MyEvent").WithLocation(20, 13)
                );
        }

        [Fact, WorkItem(40092, "https://github.com/dotnet/roslyn/issues/40092")]
        public void ExternEventInitializer()
        {
            var text = @"
delegate void D();

class Test
{
#pragma warning disable 414 // The field '{0}' is assigned but its value is never used
#pragma warning disable 626 // Method, operator, or accessor '{0}' is marked external and has no attributes on it. Consider adding a DllImport attribute to specify the external implementation.
    public extern event D e = null; // 1
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (8,27): error CS8760: 'Test.e': extern event cannot have initializer
                //     public extern event D e = null; // 1
                Diagnostic(ErrorCode.ERR_ExternEventInitializer, "e").WithArguments("Test.e").WithLocation(8, 27));
        }

        #endregion
    }
}
