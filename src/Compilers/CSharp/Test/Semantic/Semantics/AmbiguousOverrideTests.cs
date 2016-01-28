// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AmbiguousOverrideTests : CompilingTestBase
    {
        [Fact]
        public void TestAmbiguousNoErrors()
        {
            string source = @"
using System;
public class Base<TLong, TInt>
{
    public virtual void Method(TLong l, int i) {  Console.Write(1); }
    public virtual void Method(long l, TInt i) { Console.Write(2);}
}

public class Derived<TInt> : Base<long, TInt>
{
    public override void Method(long l, TInt i) { Console.Write(3); } // overrides 2
}

public class Derived2 : Derived<int>
{
    public override void Method(long l, int i) { Console.Write(4); } // overrides 2
}

class EntryPoint 
{
    static void Main() 
    {
        Base<long, int> b = new Base<long, int>();
        CallOne<long>(b); // 1
        CallTwo<int>(b); // 2
        b = new Derived<int>();
        CallOne<long>(b); // 1
        CallTwo<int>(b); // 3
        b = new Derived2();
        CallOne<long>(b); // 1
        CallTwo<int>(b);  // 4
    } 

    static void CallOne<TL>(Base<TL, int> b)
    {
        b.Method(default(TL), default(int));
}

    static void CallTwo<TI>(Base<long, TI> b)
    {
        b.Method(default(long), default(TI));
        }
}

";
            CompileAndVerify(source, expectedOutput: "121314");
        }

        [WorkItem(544936, "DevDiv")]
        [Fact]
        public void TestAmbiguousInvocationError()
        {
            var source = @"
public class Base<TLong, TInt>
{
    public virtual void Method(TLong l, int i) { } // 1
    public virtual void Method(long l, TInt i) { } // 2
}

public class Derived<TInt> : Base<long, TInt>
{
    public override void Method(long l, TInt i) { } // overrides 2
}

public class Derived2 : Derived<int>
{
    public override void Method(long l, int i) { } // overrides 2
}

class EntryPoint 
{
    static void Main() 
    {
        new Derived2().Method(1L, 2); //CS0121
    } 
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (22,9): error CS0121: The call is ambiguous between the following methods or properties: 'Base<TLong, TInt>.Method(long, TInt)' and 'Base<TLong, TInt>.Method(TLong, int)'
                //         new Derived2().Method(1L, 2); //CS0121
                Diagnostic(ErrorCode.ERR_AmbigCall, "Method").WithArguments("Base<TLong, TInt>.Method(long, TInt)", "Base<TLong, TInt>.Method(TLong, int)"));
        }

        [Fact]
        public void TestAmbiguousOverrideError()
        {
            var text1 = @"
public class Base<TLong, TInt>
{
    public virtual void Method(TLong l, int i) { }
    public virtual void Method(long l, TInt i) { }
}
";
            var text2 = @"
public class Derived<TInt> : Base<long, TInt>
{
    public override void Method(long l, TInt i) { }
    public override void Method(long l, int i) { }
}
";
            var text3 = @"
public class Derived2 : Derived<int>
{
    public override void Method(long l, int i) { }  //CS0462 and CS1957
}
";

            var comp1 = CreateCompilationWithMscorlib(text1);
            var comp1ref = new CSharpCompilationReference(comp1);
            var ref1 = new List<MetadataReference>() { comp1ref };

            var comp2 = CreateCompilationWithMscorlib(text2, references: ref1, assemblyName: "Test2");
            var comp2ref = new CSharpCompilationReference(comp2);

            var ref2 = new List<MetadataReference>() { comp1ref, comp2ref };
            var comp = CreateCompilationWithMscorlib(text3, ref2, assemblyName: "Test3");
            var diagnostics = comp.GetDiagnostics();

            comp.VerifyDiagnostics(
                // (4,26): error CS0462: The inherited members 'Derived<TInt>.Method(long, TInt)' and 'Derived<TInt>.Method(long, int)' have the same signature in type 'Derived2', so they cannot be overridden
                //     public override void Method(long l, int i) { }  //CS0462 and CS1957
                Diagnostic(ErrorCode.ERR_AmbigOverride, "Method").WithArguments("Derived<TInt>.Method(long, TInt)", "Derived<TInt>.Method(long, int)", "Derived2").WithLocation(4, 26),
                // (4,26): warning CS1957: Member 'Derived2.Method(long, int)' overrides 'Derived<int>.Method(long, int)'. There are multiple override candidates at run-time. It is implementation dependent which method will be called.
                //     public override void Method(long l, TInt i) { }
                Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "Method").WithArguments("Derived<int>.Method(long, int)", "Derived2.Method(long, int)").WithLocation(4, 26));
        }

        [Fact]
        public void TestAmbiguousOverridesFromSameClass()
        {
            // Tests:
            // Through type argument substitution make two base abstract members with same signature (parameters / return types) –
            // override member in derived class – invoke member in derived class using base.VirtualMember
            // Test similar case where conflicting members are split across multiple base types

            var source = @"
abstract class Base<T, U>
{
    public virtual void Method(T x) { }
    public virtual int Method(int y) { return y; }
    public virtual void Method(U z) { }
    public virtual void Method(T x, U y) { }
    public abstract void Method(U y, T x);
}
class Derived : Base<int, int>
{
    public override void Method(int a)
    {
    }
    public override void Method(int a, int b)
    {
    }
    void Test()
    {
        base.Method(1);
        base.Method(1, 1);
    }
}
abstract class Derived2 : Base<int, long>
{
    public override void Method(int a, long b)
    {
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,26): error CS0462: The inherited members 'Base<T, U>.Method(T)' and 'Base<T, U>.Method(int)' have the same signature in type 'Derived', so they cannot be overridden
                //     public override void Method(int a)
                Diagnostic(ErrorCode.ERR_AmbigOverride, "Method").WithArguments("Base<T, U>.Method(T)", "Base<T, U>.Method(int)", "Derived"),
                // (4,25): warning CS1957: Member 'Derived.Method(int)' overrides 'Base<int, int>.Method(int)'. There are multiple override candidates at run-time. It is implementation dependent which method will be called.
                //     public virtual void Method(T x) { }
                Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "Method").WithArguments("Base<int, int>.Method(int)", "Derived.Method(int)"),
                // (15,26): error CS0462: The inherited members 'Base<T, U>.Method(T, U)' and 'Base<T, U>.Method(U, T)' have the same signature in type 'Derived', so they cannot be overridden
                //     public override void Method(int a, int b)
                Diagnostic(ErrorCode.ERR_AmbigOverride, "Method").WithArguments("Base<T, U>.Method(T, U)", "Base<T, U>.Method(U, T)", "Derived"),
                // (7,25): warning CS1957: Member 'Derived.Method(int, int)' overrides 'Base<int, int>.Method(int, int)'. There are multiple override candidates at run-time. It is implementation dependent which method will be called.
                //     public virtual void Method(T x, U y) { }
                Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "Method").WithArguments("Base<int, int>.Method(int, int)", "Derived.Method(int, int)"),
                // (10,7): error CS0534: 'Derived' does not implement inherited abstract member 'Base<int, int>.Method(int, int)'
                // class Derived : Base<int, int>
                Diagnostic(ErrorCode.ERR_UnimplementedAbstractMethod, "Derived").WithArguments("Derived", "Base<int, int>.Method(int, int)"),
                // (21,9): error CS0121: The call is ambiguous between the following methods or properties: 'Base<T, U>.Method(T, U)' and 'Base<T, U>.Method(U, T)'
                //         base.Method(1, 1);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Method").WithArguments("Base<T, U>.Method(T, U)", "Base<T, U>.Method(U, T)"));
        }

        [Fact]
        public void TestAmbiguousOverridesRefOut()
        {
            // Tests:
            // Test that we continue to report errors / warnings even when ambiguous base methods that we are trying to
            // override only differ by ref / out - test that only a warning (for runtime ambiguity) is reported 
            // in the case where ambiguous signatures differ by just ref / out

            var source = @"
using System.Collections.Generic;
abstract class Base<T, U>
{
    public virtual void Method(ref List<T> x, out List<U> y) { y = null; }
    public virtual void Method(out List<U> y, ref List<T> x) { y = null; }
    public virtual void Method(ref List<U> x) { }  
}
class Base2<A, B> : Base<A, B>
{
    public virtual void Method(out List<A> x) { x = null; }
}
class Derived : Base2<int, int>
{
    public override void Method(ref List<int> a, out List<int> b) { b = null; } // Reports warning about runtime ambiguity
    public override void Method(ref List<int> a) { } // No warning when ambiguous signatures are spread across multiple base types
}";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (5,25): warning CS1957: Member 'Derived.Method(ref System.Collections.Generic.List<int>, out System.Collections.Generic.List<int>)' overrides 'Base<int, int>.Method(ref System.Collections.Generic.List<int>, out System.Collections.Generic.List<int>)'. There are multiple override candidates at run-time. It is implementation dependent which method will be called.
                Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "Method").WithArguments("Base<int, int>.Method(ref System.Collections.Generic.List<int>, out System.Collections.Generic.List<int>)", "Derived.Method(ref System.Collections.Generic.List<int>, out System.Collections.Generic.List<int>)"));
        }

        [Fact]
        public void TestAmbiguousOverridesParams()
        {
            // Tests:
            // Test that we continue to report errors / warnings even when ambiguous base methods that we are trying to
            // override only differ by params

            var source = @"
using System.Collections.Generic;
abstract class Base<T, U>
{
    public virtual void Method(List<T> x, params List<U>[] y) { }
    public virtual void Method(List<U> y, List<T>[] x) { }
}
class Derived : Base<int, int>
{
    public override void Method(List<int> x, List<int>[] y) { }
}";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_AmbigOverride, "Method").WithArguments("Base<T, U>.Method(System.Collections.Generic.List<T>, params System.Collections.Generic.List<U>[])", "Base<T, U>.Method(System.Collections.Generic.List<U>, System.Collections.Generic.List<T>[])", "Derived"),
                Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "Method").WithArguments("Base<int, int>.Method(System.Collections.Generic.List<int>, params System.Collections.Generic.List<int>[])", "Derived.Method(System.Collections.Generic.List<int>, params System.Collections.Generic.List<int>[])"));
        }


        [Fact]
        public void TestAmbiguousOverridesOptionalParameters()
        {
            // Tests:
            // Test that we continue to report errors / warnings even when ambiguous base methods that we are trying to
            // override only differ by optional parameters

            var source = @"
using System.Collections.Generic;
abstract class Base<T, U>
{
    public virtual void Method(List<T> x, List<U>[] y=null) { }
    public virtual void Method(List<U> y, List<T>[] x) { }
}
class Derived : Base<int, int>
{
    public override void Method(List<int> x, List<int>[] y=null) { }
}";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_AmbigOverride, "Method").WithArguments("Base<T, U>.Method(System.Collections.Generic.List<T>, System.Collections.Generic.List<U>[])", "Base<T, U>.Method(System.Collections.Generic.List<U>, System.Collections.Generic.List<T>[])", "Derived"),
                Diagnostic(ErrorCode.WRN_MultipleRuntimeOverrideMatches, "Method").WithArguments("Base<int, int>.Method(System.Collections.Generic.List<int>, System.Collections.Generic.List<int>[])", "Derived.Method(System.Collections.Generic.List<int>, System.Collections.Generic.List<int>[])"));
        }

        [Fact]
        public void TestAmbiguousMethodsWithCustomModifiers()
        {
            var text = @"using Metadata;
public class Test
{
    void Test1()
    {
        LeastModoptsWinAmbiguous obj = new LeastModoptsWin();
        obj.M(obj.GetByte(), 121); // CS0121
    }
}
";
            var asm = TestReferences.SymbolsTests.CustomModifiers.ModoptTests;

            CreateCompilationWithMscorlib(text, new[] { asm }).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("Metadata.LeastModoptsWinAmbiguous.M(byte, byte)", "Metadata.LeastModoptsWinAmbiguous.M(byte, byte)")
            );
        }

        /// <summary>
        /// Dev10 gives errors if all properties contain modopt, but no error for methods (pick the one with least modopt)
        /// </summary>
        [Fact]
        public void TestAmbiguousPropertiesWithCustomModifiers()
        {
            var text = @"using Metadata;
public class Test
{
    void Test1()
    {
        ModoptPropAmbiguous obj = new ModoptPropAmbiguous();
        System.Console.Write(obj.P); // CS0229
    }
}
";
            var asm = TestReferences.SymbolsTests.CustomModifiers.ModoptTests;

            CreateCompilationWithMscorlib(text, new[] { asm }).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_AmbigMember, "P").WithArguments("Metadata.ModoptPropAmbiguous.P", "Metadata.ModoptPropAmbiguous.P")
            );
        }

        [Fact]
        public void TestImplicitImplementInterfaceMethodsWithCustomModifiers()
        {
            var text = @"using Metadata;
public class CFoo : IFooAmbiguous<string, long> // CS0535
{
    public long M(string t) { return 127; } 
}

class CBar : IFoo // CS0535 * 2
{
    public sbyte M1<T, V>(T t, V v) { return 123; }
}
";
            var asm = TestReferences.SymbolsTests.CustomModifiers.ModoptTests;

            CreateCompilationWithMscorlib(text, new[] { asm }).VerifyDiagnostics(
    // (7,14): error CS0535: 'CBar' does not implement interface member 'IFoo.M<T>(T)'
    // class CBar : IFoo // CS0535 * 2
    Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IFoo").WithArguments("CBar", "Metadata.IFoo.M<T>(T)").WithLocation(7, 14),
    // (7,14): error CS0535: 'CBar' does not implement interface member 'IFoo.M<T>(T)'
    // class CBar : IFoo // CS0535 * 2
    Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IFoo").WithArguments("CBar", "Metadata.IFoo.M<T>(T)").WithLocation(7, 14),
    // (2,21): error CS0570: 'IFooAmbiguous<T, R>.M(?)' is not supported by the language
    // public class CFoo : IFooAmbiguous<string, long> // CS0535
    Diagnostic(ErrorCode.ERR_BindToBogus, "IFooAmbiguous<string, long>").WithArguments("Metadata.IFooAmbiguous<T, R>.M(?)").WithLocation(2, 21)
            );
        }

        [WorkItem(540518, "DevDiv")]
        [Fact]
        public void TestExplicitImplementInterfaceMethodsWithCustomModifiers()
        {
            var text = @"using Metadata;
public class CFoo : IFooAmbiguous<string, long> // CS0535 *2
{
    long IFooAmbiguous<string, long>.M(string t) { return -128; } // W CS0437
}
";
            var asm = TestReferences.SymbolsTests.CustomModifiers.ModoptTests;

            CreateCompilationWithMscorlib(text, new[] { asm }).VerifyDiagnostics(
    // (4,38): warning CS0473: Explicit interface implementation 'CFoo.IFooAmbiguous<string, long>.M(string)' matches more than one interface member. Which interface member is actually chosen is implementation-dependent. Consider using a non-explicit implementation instead.
    //     long IFooAmbiguous<string, long>.M(string t) { return -128; } // W CS0437
    Diagnostic(ErrorCode.WRN_ExplicitImplCollision, "M").WithArguments("CFoo.Metadata.IFooAmbiguous<string, long>.M(string)").WithLocation(4, 38),
    // (2,21): error CS0535: 'CFoo' does not implement interface member 'IFooAmbiguous<string, long>.M(string)'
    // public class CFoo : IFooAmbiguous<string, long> // CS0535 *2
    Diagnostic(ErrorCode.ERR_UnimplementedInterfaceMember, "IFooAmbiguous<string, long>").WithArguments("CFoo", "Metadata.IFooAmbiguous<string, long>.M(string)").WithLocation(2, 21),
    // (2,21): error CS0570: 'IFooAmbiguous<T, R>.M(?)' is not supported by the language
    // public class CFoo : IFooAmbiguous<string, long> // CS0535 *2
    Diagnostic(ErrorCode.ERR_BindToBogus, "IFooAmbiguous<string, long>").WithArguments("Metadata.IFooAmbiguous<T, R>.M(?)").WithLocation(2, 21)
                );
        }

        [Fact]
        public void TestDeriveFromClassWithOnlyModreqCustomModifier()
        {
            var text = @"using Metadata;
class Test
{
    // Modreg has one method 'M' with modreg on it 
    class D : Modreq
    {
    }

    static void Main()
    {
        new D().M(11); // Dev10: error CS0570: 'M' is not supported by the language
    }
}
";
            var asm = MetadataReference.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.ModoptTests.AsImmutableOrNull());
            CreateCompilationWithMscorlib(text, new[] { asm }).VerifyDiagnostics(
    // (11,9): error CS0570: 'Metadata.Modreq.M(?)' is not supported by the language
    //         new D().M(11); // Dev10: error CS0570: 'M' is not supported by the language
    Diagnostic(ErrorCode.ERR_BindToBogus, "M").WithArguments("Metadata.Modreq.M(?)")
                );
        }

        [Fact]
        public void TestOverrideMethodWithModreqCustomModifier()
        {
            var text = @"using System;
using Metadata;
class Test
{
    // Modreq has one virtual method 'M' with modreq on it 
    class D : Modreq
    {
        public override void M(uint x) { Console.Write(x + 1); } // CS0115
    }

    static void Main()
    {
        new D().M(22);
    }
}
";
            var asm = TestReferences.SymbolsTests.CustomModifiers.ModoptTests;

            CreateCompilationWithMscorlib(text, new[] { asm }).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M").WithArguments("Test.D.M(uint)"));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void TestOverrideMethod_FewestCustomModifiers_BothCorrect()
        {
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  Foo(int32 modopt(int64) x) cil managed
  {
    ret
  }
  .method public hidebysig newslot virtual 
          instance void  Foo(int32 modopt(int64) modopt(int32) x) cil managed
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
} // end of class Base
";

            var csharp = @"
public class Derived : Base
{
    public override void Foo(int x) { }
}
";
            var compilation = CreateCompilationWithCustomILSource(csharp, il);

            // No diagnostics - just choose the overload with fewer custom modifiers
            compilation.VerifyDiagnostics();

            Func<int, Func<MethodSymbol, bool>> hasCustomModifierCount = c => m => m.CustomModifierCount() == c;

            var baseClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var baseMethod1 = baseClass.GetMembers("Foo").Cast<MethodSymbol>().Where(hasCustomModifierCount(1)).Single();
            var baseMethod2 = baseClass.GetMembers("Foo").Cast<MethodSymbol>().Where(hasCustomModifierCount(2)).Single();

            var derivedClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var derivedMethod = derivedClass.GetMember<MethodSymbol>("Foo");

            Assert.Equal(baseMethod1, derivedMethod.OverriddenMethod);
            Assert.NotEqual(baseMethod2, derivedMethod.OverriddenMethod);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void TestOverrideMethod_FewestCustomModifiers_OneCorrect()
        {
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  // Dev10 chooses to override this method, since it has fewer custom modifiers -
  // even thought the return type is less correct.
  .method public hidebysig newslot virtual 
          instance int64 modopt(int64)  Foo(int32 x) cil managed
  {
    ret
  }
  .method public hidebysig newslot virtual 
          instance char modopt(int64) modopt(int32)  Foo(int32 x) cil managed
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
} // end of class Base
";

            var csharp = @"
public class Derived : Base
{
    public override char Foo(int x) { return 'a'; }
}
";
            var compilation = CreateCompilationWithCustomILSource(csharp, il);

            compilation.VerifyDiagnostics(
                // (4,26): error CS0508: 'Derived.Foo(int)': return type must be 'long' to match overridden member 'Base.Foo(int)'
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "Foo").WithArguments("Derived.Foo(int)", "Base.Foo(int)", "long"));

            Func<int, Func<MethodSymbol, bool>> hasCustomModifierCount = c => m => m.CustomModifierCount() == c;

            var baseClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var baseMethod1 = baseClass.GetMembers("Foo").Cast<MethodSymbol>().Where(hasCustomModifierCount(1)).Single();
            var baseMethod2 = baseClass.GetMembers("Foo").Cast<MethodSymbol>().Where(hasCustomModifierCount(2)).Single();

            var derivedClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var derivedMethod = derivedClass.GetMember<MethodSymbol>("Foo");

            Assert.Equal(baseMethod1, derivedMethod.OverriddenMethod);
            Assert.NotEqual(baseMethod2, derivedMethod.OverriddenMethod);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void TestOverrideProperty_FewestCustomModifiers_BothCorrect()
        {
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual 
          instance int32 modopt(int64)  get_P() cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig newslot specialname virtual 
          instance int32 modopt(int64) modopt(int32)  get_P() cil managed
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

  .property instance int32 modopt(int64) P()
  {
    .get instance int32 modopt(int64) Base::get_P()
  }

  .property instance int32 modopt(int64) modopt(int32) P()
  {
    .get instance int32 modopt(int64) modopt(int32) Base::get_P()
  }
} // end of class Base
";

            var csharp = @"
public class Derived : Base
{
    public override int P { get { return 0; } }
}
";
            var compilation = CreateCompilationWithCustomILSource(csharp, il);

            // No diagnostics - just choose the overload with fewer custom modifiers
            compilation.VerifyDiagnostics();

            Func<int, Func<PropertySymbol, bool>> hasCustomModifierCount = c => p => p.CustomModifierCount() == c;

            var baseClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var baseProperty1 = baseClass.GetMembers("P").Cast<PropertySymbol>().Where(hasCustomModifierCount(1)).Single();
            var baseProperty2 = baseClass.GetMembers("P").Cast<PropertySymbol>().Where(hasCustomModifierCount(2)).Single();

            var derivedClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var derivedProperty = derivedClass.GetMember<PropertySymbol>("P");

            Assert.Equal(baseProperty1, derivedProperty.OverriddenProperty);
            Assert.NotEqual(baseProperty2, derivedProperty.OverriddenProperty);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void TestOverrideProperty_FewestCustomModifiers_OneCorrect()
        {
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot specialname virtual 
          instance char modopt(int64)  get_P() cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig newslot specialname virtual 
          instance int32 modopt(int64) modopt(int32)  get_P() cil managed
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

  // Dev10 overrides this property - even though the type is worse -
  // because it has fewer custom modifiers.
  .property instance char modopt(int64) P()
  {
    .get instance char modopt(int64) Base::get_P()
  }

  .property instance int32 modopt(int64) modopt(int32) P()
  {
    .get instance int32 modopt(int64) modopt(int32) Base::get_P()
  }
} // end of class Base
";

            var csharp = @"
public class Derived : Base
{
    public override int P { get { return 0; } }
}
";
            var compilation = CreateCompilationWithCustomILSource(csharp, il);

            compilation.VerifyDiagnostics(
                // (4,25): error CS1715: 'Derived.P': type must be 'char' to match overridden member 'Base.P'
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "P").WithArguments("Derived.P", "Base.P", "char"));

            Func<int, Func<PropertySymbol, bool>> hasCustomModifierCount = c => p => p.CustomModifierCount() == c;

            var baseClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var baseProperty1 = baseClass.GetMembers("P").Cast<PropertySymbol>().Where(hasCustomModifierCount(1)).Single();
            var baseProperty2 = baseClass.GetMembers("P").Cast<PropertySymbol>().Where(hasCustomModifierCount(2)).Single();

            var derivedClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var derivedProperty = derivedClass.GetMember<PropertySymbol>("P");

            Assert.Equal(baseProperty1, derivedProperty.OverriddenProperty);
            Assert.NotEqual(baseProperty2, derivedProperty.OverriddenProperty);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void TestOverrideIndexer_FewestCustomModifiers_BothCorrect()
        {
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig newslot specialname virtual 
          instance int32  get_Item(int32 modopt(int64) x) cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual 
          instance int32  get_Item(int32 modopt(int64) modopt(int32) x) cil managed
  {
    ret
  }

  .property instance int32 Item(int32 modopt(int64))
  {
    .get instance int32 Base::get_Item(int32 modopt(int64))
  }

  .property instance int32 Item(int32 modopt(int64) modopt(int32))
  {
    .get instance int32 Base::get_Item(int32 modopt(int64) modopt(int32))
  }
} // end of class Base
";

            var csharp = @"
public class Derived : Base
{
    public override int this[int x] { get { return 0; } }
}
";
            var compilation = CreateCompilationWithCustomILSource(csharp, il);

            // No diagnostics - just choose the overload with fewer custom modifiers
            compilation.VerifyDiagnostics();

            Func<int, Func<PropertySymbol, bool>> hasCustomModifierCount = c => p => p.CustomModifierCount() == c;

            var baseClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var baseProperty1 = baseClass.Indexers.Where(hasCustomModifierCount(1)).Single();
            var baseProperty2 = baseClass.Indexers.Where(hasCustomModifierCount(2)).Single();

            Assert.True(baseProperty1.IsIndexer);
            Assert.True(baseProperty2.IsIndexer);

            var derivedClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var derivedProperty = derivedClass.Indexers.Single();

            Assert.True(derivedProperty.IsIndexer);

            Assert.Equal(baseProperty1, derivedProperty.OverriddenProperty);
            Assert.NotEqual(baseProperty2, derivedProperty.OverriddenProperty);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void TestOverrideIndexer_FewestCustomModifiers_OneCorrect()
        {
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string)
           = {string('Item')}

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig newslot specialname virtual 
          instance char  get_Item(int32 modopt(int64) x) cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual 
          instance int32  get_Item(int32 modopt(int64) modopt(int32) x) cil managed
  {
    ret
  }

  // Dev10 overrides this indexer - even though the type is worse -
  // because it has fewer custom modifiers.
  .property instance char Item(int32 modopt(int64))
  {
    .get instance char Base::get_Item(int32 modopt(int64))
  }

  .property instance int32 Item(int32 modopt(int64) modopt(int32))
  {
    .get instance int32 Base::get_Item(int32 modopt(int64) modopt(int32))
  }
} // end of class Base
";

            var csharp = @"
public class Derived : Base
{
    public override int this[int x] { get { return 0; } }
}
";
            var compilation = CreateCompilationWithCustomILSource(csharp, il);

            compilation.VerifyDiagnostics(
                // (4,25): error CS1715: 'Derived.this[int]': type must be 'char' to match overridden member 'Base.this[int]'
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "this").WithArguments("Derived.this[int]", "Base.this[int]", "char"));

            Func<int, Func<PropertySymbol, bool>> hasCustomModifierCount = c => p => p.CustomModifierCount() == c;

            var baseClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var baseProperty1 = baseClass.Indexers.Where(hasCustomModifierCount(1)).Single();
            var baseProperty2 = baseClass.Indexers.Where(hasCustomModifierCount(2)).Single();

            Assert.True(baseProperty1.IsIndexer);
            Assert.True(baseProperty2.IsIndexer);

            var derivedClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var derivedProperty = derivedClass.Indexers.Single();

            Assert.True(derivedProperty.IsIndexer);

            Assert.Equal(baseProperty1, derivedProperty.OverriddenProperty);
            Assert.NotEqual(baseProperty2, derivedProperty.OverriddenProperty);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void TestOverrideEvent_FewestCustomModifiers_BothCorrect()
        {
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
  
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
  
  .method public hidebysig newslot specialname virtual 
          instance void  add_E(class [mscorlib]System.Action`1<int32 modopt(int64) modopt(int32) []> 'value') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual 
          instance void  remove_E(class [mscorlib]System.Action`1<int32 modopt(int64) modopt(int32) []> 'value') cil managed
  {
    ret
  }

  .event class [mscorlib]System.Action`1<int32 modopt(int64) []> E
  {
    .addon instance void Base::add_E(class [mscorlib]System.Action`1<int32 modopt(int64) []>)
    .removeon instance void Base::remove_E(class [mscorlib]System.Action`1<int32 modopt(int64) []>)
  } // end of event Base::E

  .event class [mscorlib]System.Action`1<int32 modopt(int64) modopt(int32) []> E
  {
    .addon instance void Base::add_E(class [mscorlib]System.Action`1<int32 modopt(int64) modopt(int32) []>)
    .removeon instance void Base::remove_E(class [mscorlib]System.Action`1<int32 modopt(int64) modopt(int32) []>)
  } // end of event Base::E
} // end of class Base
";

            var csharp = @"
public class Derived : Base
{
    public override event System.Action<int[]> E;

    void UseEvent() { E(null); }
}
";
            var compilation = CreateCompilationWithCustomILSource(csharp, il);

            // No diagnostics - just choose the overload with fewer custom modifiers
            compilation.VerifyDiagnostics();

            Func<int, Func<EventSymbol, bool>> hasCustomModifierCount = c => e => e.Type.CustomModifierCount() == c;

            var baseClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var baseEvent1 = baseClass.GetMembers("E").Cast<EventSymbol>().Where(hasCustomModifierCount(1)).Single();
            var baseEvent2 = baseClass.GetMembers("E").Cast<EventSymbol>().Where(hasCustomModifierCount(2)).Single();

            var derivedClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var derivedEvent = derivedClass.GetMember<EventSymbol>("E");

            Assert.Equal(baseEvent1, derivedEvent.OverriddenEvent);
            Assert.NotEqual(baseEvent2, derivedEvent.OverriddenEvent);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void TestOverrideEvent_FewestCustomModifiers_OneCorrect()
        {
            var il = @"
.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
  
  .method public hidebysig newslot specialname virtual 
          instance void  add_E(class [mscorlib]System.Action`1<char modopt(int64) []> 'value') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual 
          instance void  remove_E(class [mscorlib]System.Action`1<char modopt(int64) []> 'value') cil managed
  {
    ret
  }
  
  .method public hidebysig newslot specialname virtual 
          instance void  add_E(class [mscorlib]System.Action`1<int32 modopt(int64) modopt(int32) []> 'value') cil managed
  {
    ret
  }

  .method public hidebysig newslot specialname virtual 
          instance void  remove_E(class [mscorlib]System.Action`1<int32 modopt(int64) modopt(int32) []> 'value') cil managed
  {
    ret
  }

  // Dev10 overrides this event - even though the type is worse -
  // because it has fewer custom modifiers.
  .event class [mscorlib]System.Action`1<char modopt(int64) []> E
  {
    .addon instance void Base::add_E(class [mscorlib]System.Action`1<char modopt(int64) []>)
    .removeon instance void Base::remove_E(class [mscorlib]System.Action`1<char modopt(int64) []>)
  } // end of event Base::E

  .event class [mscorlib]System.Action`1<int32 modopt(int64) modopt(int32) []> E
  {
    .addon instance void Base::add_E(class [mscorlib]System.Action`1<int32 modopt(int64) modopt(int32) []>)
    .removeon instance void Base::remove_E(class [mscorlib]System.Action`1<int32 modopt(int64) modopt(int32) []>)
  } // end of event Base::E
} // end of class Base
";

            var csharp = @"
public class Derived : Base
{
    public override event System.Action<int[]> E;

    void UseEvent() { E(null); }
}
";
            var compilation = CreateCompilationWithCustomILSource(csharp, il);

            compilation.VerifyDiagnostics(
                // (4,48): error CS1715: 'Derived.E': type must be 'System.Action<char[]>' to match overridden member 'Base.E'
                //     public override event System.Action<int[]> E;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "E").WithArguments("Derived.E", "Base.E", "System.Action<char[]>"));

            Func<int, Func<EventSymbol, bool>> hasCustomModifierCount = c => e => e.Type.CustomModifierCount() == c;

            var baseClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Base");
            var baseEvent1 = baseClass.GetMembers("E").Cast<EventSymbol>().Where(hasCustomModifierCount(1)).Single();
            var baseEvent2 = baseClass.GetMembers("E").Cast<EventSymbol>().Where(hasCustomModifierCount(2)).Single();

            var derivedClass = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("Derived");
            var derivedEvent = derivedClass.GetMember<EventSymbol>("E");

            Assert.Equal(baseEvent1, derivedEvent.OverriddenEvent);
            Assert.NotEqual(baseEvent2, derivedEvent.OverriddenEvent);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ModOptTestWithErrors()
        {
            // NOTE: removed Microsoft.VisualC attributes
            var il = @"
.class public sequential ansi sealed beforefieldinit ModA
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
//  .custom instance void [Microsoft.VisualC]Microsoft.VisualC.MiscellaneousBitsAttribute::.ctor(int32) = ( 01 00 40 00 00 00 00 00 )                         // ..@.....
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.NativeCppClassAttribute::.ctor() = ( 01 00 00 00 ) 
//  .custom instance void [Microsoft.VisualC]Microsoft.VisualC.DebugInfoInPDBAttribute::.ctor() = ( 01 00 00 00 ) 
} // end of class ModA

.class public sequential ansi sealed beforefieldinit ModB
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
//  .custom instance void [Microsoft.VisualC]Microsoft.VisualC.MiscellaneousBitsAttribute::.ctor(int32) = ( 01 00 40 00 00 00 00 00 )                         // ..@.....
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.NativeCppClassAttribute::.ctor() = ( 01 00 00 00 ) 
//  .custom instance void [Microsoft.VisualC]Microsoft.VisualC.DebugInfoInPDBAttribute::.ctor() = ( 01 00 00 00 ) 
} // end of class ModB

.class public auto ansi beforefieldinit CG`1<([mscorlib]System.Object) T>
       extends [mscorlib]System.Object
{
// Removing this method makes the C# invocation of F ambiguous, because when
// we attempt to figure out which instance of F to overload, we pick the one
// with the least amount of modopts. But since theres more than one (the two
// below have exactly one modopt), this should result in an error.
//
//  .method public hidebysig newslot virtual 
//          instance void  F(!T c) cil managed
//  {
//    // Code size       11 (0xb)
//    .maxstack  1
//    IL_0000:  ldstr      ""CG::F(T)""
//    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
//    IL_000a:  ret
//  } // end of method CG`1::F

  .method public hidebysig newslot virtual 
          instance void  F(!T modopt(ModA) c) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldstr      ""CG::F(T [A])""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method CG`1::F

  .method public hidebysig newslot virtual 
          instance void  F(!T modopt(ModB) c) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldstr      ""CG::F(T [B])""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method CG`1::F

  .method public hidebysig newslot virtual 
          instance void  F(!T modopt(ModA) modopt(ModB) c) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldstr      ""CG::F(T [A][B])""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method CG`1::F

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CG`1::.ctor

} // end of class CG`1

.class public auto ansi beforefieldinit DG`1<([mscorlib]System.Object) T>
       extends class CG`1<!T>
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class CG`1<!T>::.ctor()
    IL_0006:  ret
  } // end of method DG`1::.ctor

} // end of class DG`1
";

            var csharp = @"
using System;

class EG<T> : DG<T>
{
    public override void F(T c)
    {
        Console.Write(""C# EG.F(T): "");
        base.F(c);
    }
}

class EGI : DG<int>
{
    public override void F(int c)
    {
        Console.Write(""C# GEI.F(int): "");
        base.F(c);
    }
}

class M
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""***** Start mod opt tests ****"");

        {
            Console.WriteLine(""  *** Generic Non-ref"");

            EG<string> e = new EG<string>();
            string c = ""Hello"";

            e.F(c);
        }

        {
            Console.WriteLine(""  *** Generic-base Non-ref"");

            EGI e = new EGI();
            int c = 5;

            e.F(c);
        }

        Console.WriteLine(""***** End mod opt tests ****"");
    }
}
";
            CreateCompilationWithCustomILSource(csharp, il).VerifyDiagnostics(
                // CONSIDER: Dev10 reports CS1957, even though the runtime has no trouble distinguishing the potentially
                // overridden methods.

                // (6,26): error CS0462: The inherited members 'CG<T>.F(T)' and 'CG<T>.F(T)' have the same signature in type 'EG<T>', so they cannot be overridden
                //     public override void F(T c)
                Diagnostic(ErrorCode.ERR_AmbigOverride, "F").WithArguments("CG<T>.F(T)", "CG<T>.F(T)", "EG<T>"),
                // (15,26): error CS0462: The inherited members 'CG<T>.F(T)' and 'CG<T>.F(T)' have the same signature in type 'EGI', so they cannot be overridden
                //     public override void F(int c)
                Diagnostic(ErrorCode.ERR_AmbigOverride, "F").WithArguments("CG<T>.F(T)", "CG<T>.F(T)", "EGI"),

                // NOTE: Dev10 doesn't report these cascading errors.

                // (9,9): error CS0121: The call is ambiguous between the following methods or properties: 'CG<T>.F(T)' and 'CG<T>.F(T)'
                //         base.F(c);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("CG<T>.F(T)", "CG<T>.F(T)"),
                // (18,9): error CS0121: The call is ambiguous between the following methods or properties: 'CG<T>.F(T)' and 'CG<T>.F(T)'
                //         base.F(c);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("CG<T>.F(T)", "CG<T>.F(T)"),
                // (34,13): error CS0121: The call is ambiguous between the following methods or properties: 'CG<T>.F(T)' and 'CG<T>.F(T)'
                //             e.F(c);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("CG<T>.F(T)", "CG<T>.F(T)"),
                // (43,13): error CS0121: The call is ambiguous between the following methods or properties: 'CG<T>.F(T)' and 'CG<T>.F(T)'
                //             e.F(c);
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("CG<T>.F(T)", "CG<T>.F(T)"));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ModOptTest()
        {
            // NOTE: removed Microsoft.VisualC attributes
            var il = @"
.class public sequential ansi sealed beforefieldinit ModA
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
//  .custom instance void [Microsoft.VisualC]Microsoft.VisualC.MiscellaneousBitsAttribute::.ctor(int32) = ( 01 00 40 00 00 00 00 00 )                         // ..@.....
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.NativeCppClassAttribute::.ctor() = ( 01 00 00 00 ) 
//  .custom instance void [Microsoft.VisualC]Microsoft.VisualC.DebugInfoInPDBAttribute::.ctor() = ( 01 00 00 00 ) 
} // end of class ModA

.class public sequential ansi sealed beforefieldinit ModB
       extends [mscorlib]System.ValueType
{
  .pack 0
  .size 1
//  .custom instance void [Microsoft.VisualC]Microsoft.VisualC.MiscellaneousBitsAttribute::.ctor(int32) = ( 01 00 40 00 00 00 00 00 )                         // ..@.....
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.NativeCppClassAttribute::.ctor() = ( 01 00 00 00 ) 
//  .custom instance void [Microsoft.VisualC]Microsoft.VisualC.DebugInfoInPDBAttribute::.ctor() = ( 01 00 00 00 ) 
} // end of class ModB

.class public auto ansi beforefieldinit CG`1<([mscorlib]System.Object) T>
       extends [mscorlib]System.Object
{
// Note that this test works fine because we pick the function with the 
// least number of modopts, which we can find as this first function.

  .method public hidebysig newslot virtual 
          instance void  F(!T c) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldstr      ""CG::F(T)""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method CG`1::F

  .method public hidebysig newslot virtual 
          instance void  F(!T modopt(ModA) c) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldstr      ""CG::F(T [A])""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method CG`1::F

  .method public hidebysig newslot virtual 
          instance void  F(!T modopt(ModB) c) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldstr      ""CG::F(T [B])""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method CG`1::F

  .method public hidebysig newslot virtual 
          instance void  F(!T modopt(ModA) modopt(ModB) c) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  1
    IL_0000:  ldstr      ""CG::F(T [A][B])""
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method CG`1::F

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method CG`1::.ctor

} // end of class CG`1

.class public auto ansi beforefieldinit DG`1<([mscorlib]System.Object) T>
       extends class CG`1<!T>
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class CG`1<!T>::.ctor()
    IL_0006:  ret
  } // end of method DG`1::.ctor

} // end of class DG`1
";

            var csharp = @"
using System;

class EG<T> : DG<T>
{
    public override void F(T c)
    {
        Console.Write(""C# EG.F(T): "");
        base.F(c);
    }
}

class EGI : DG<int>
{
    public override void F(int c)
    {
        Console.Write(""C# GEI.F(int): "");
        base.F(c);
    }
}

class M
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""***** Start mod opt tests ****"");

        {
            Console.WriteLine(""  *** Generic Non-ref"");

            EG<string> e = new EG<string>();
            string c = ""Hello"";

            e.F(c);
        }

        {
            Console.WriteLine(""  *** Generic-base Non-ref"");

            EGI e = new EGI();
            int c = 5;

            e.F(c);
        }

        Console.WriteLine(""***** End mod opt tests ****"");
    }
}
";

            var reference = CompileIL(il, appendDefaultHeader: true);

            var verifier = CompileAndVerify(csharp, new[] { reference }, options: TestOptions.ReleaseExe, expectedOutput: @"
***** Start mod opt tests ****
  *** Generic Non-ref
C# EG.F(T): CG::F(T)
  *** Generic-base Non-ref
C# GEI.F(int): CG::F(T)
***** End mod opt tests ****");

            // CONSIDER: Dev10 reports WRN_MultipleRuntimeOverrideMatches twice, which is odd
            // since the runtime can distinguish signatures with different modopts.
            verifier.VerifyDiagnostics();
        }
    }
}
