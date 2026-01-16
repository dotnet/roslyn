// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Analyzers.HiddenExplicitCast;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.HiddenExplicitCast;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.HiddenExplicitCast;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpHiddenExplicitCastDiagnosticAnalyzer,
    CSharpHiddenExplicitCastCodeFixProvider>;

public sealed class HiddenExplicitCastTests
{
    [Fact]
    public Task TestInheritanceAndUserDefined()
        => new VerifyCS.Test
        {
            TestCode = """
                class Base { }
                class Derived : Base { }

                class Castable
                {
                    public static explicit operator Base(Castable c) => new Base();
                }

                class C
                {
                    static void Main(string[] args)
                    {
                        var v = [|(Derived)new Castable()|]; 
                    }
                }
                """,
            FixedCode = """
                class Base { }
                class Derived : Base { }
                
                class Castable
                {
                    public static explicit operator Base(Castable c) => new Base();
                }
                
                class C
                {
                    static void Main(string[] args)
                    {
                        var v = (Derived)(Base)new Castable(); 
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNullableAndUserDefined()
        => new VerifyCS.Test
        {
            TestCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static void Main(string[] args)
                    {

                        S? s = new S();
                        int i = [|(int)s|];
                    }
                }
                """,
            FixedCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static void Main(string[] args)
                    {

                        S? s = new S();
                        int i = (int)(decimal?)s;
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNumericAndUserDefined()
        => new VerifyCS.Test
        {
            TestCode = """
                struct UnixTime
                {
                    public static explicit operator UnixTime(int i)
                    {
                        return default;
                    }
                }
                
                class C
                {
                    public UnixTime M(long i) => [|(UnixTime)i|];
                }
                """,
            FixedCode = """
                struct UnixTime
                {
                    public static explicit operator UnixTime(int i)
                    {
                        return default;
                    }
                }
                
                class C
                {
                    public UnixTime M(long i) => (UnixTime)(int)i;
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestTopLevel()
        => new VerifyCS.Test
        {
            TestCode = """
                S? s = new S();
                int i = [|(int)s|];

                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }
                """,
            FixedCode = """
                S? s = new S();
                int i = (int)(decimal?)s;

                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
        }.RunAsync();

    [Fact]
    public Task TestArgument()
        => new VerifyCS.Test
        {
            TestCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static void Main(string[] args)
                    {

                        S? s = new S();
                        Goo([|(int)s|]);
                    }

                    static void Goo(int i) { }
                }
                """,
            FixedCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static void Main(string[] args)
                    {

                        S? s = new S();
                        Goo((int)(decimal?)s);
                    }
                
                    static void Goo(int i) { }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestAssignment()
        => new VerifyCS.Test
        {
            TestCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static void Main(string[] args)
                    {

                        S? s = new S();
                        int i;
                        i = [|(int)s|];
                    }
                }
                """,
            FixedCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static void Main(string[] args)
                    {

                        S? s = new S();
                        int i;
                        i = (int)(decimal?)s;
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestReturn()
        => new VerifyCS.Test
        {
            TestCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static int Main(string[] args)
                    {

                        S? s = new S();
                        int i;
                        return [|(int)s|];
                    }
                }
                """,
            FixedCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    static int Main(string[] args)
                    {

                        S? s = new S();
                        int i;
                        return (int)(decimal?)s;
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestFieldInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    int i = [|(int)new S()|];
                }
                """,
            FixedCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    int i = (int)(decimal?)new S();
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestPropertyInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    int X => [|(int)new S()|];
                }
                """,
            FixedCode = """
                public struct S
                {
                    public static explicit operator decimal?(S s) { return 1.0m; }
                }

                class C
                {
                    int X => (int)(decimal?)new S();
                }
                """,
        }.RunAsync();

    #region Explicit numeric conversions with user-defined operators

    [Fact]
    public Task TestUserDefinedToDouble_ThenDoubleToFloat()
        => new VerifyCS.Test
        {
            TestCode = """
                struct Value
                {
                    public static explicit operator double(Value v) => 1.0;
                }

                class C
                {
                    void M()
                    {
                        var v = new Value();
                        float f = [|(float)v|];
                    }
                }
                """,
            FixedCode = """
                struct Value
                {
                    public static explicit operator double(Value v) => 1.0;
                }

                class C
                {
                    void M()
                    {
                        var v = new Value();
                        float f = (float)(double)v;
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestUserDefinedToLong_ThenLongToShort()
        => new VerifyCS.Test
        {
            TestCode = """
                struct BigNumber
                {
                    public static explicit operator long(BigNumber b) => 100L;
                }

                class C
                {
                    short M(BigNumber b) => [|(short)b|];
                }
                """,
            FixedCode = """
                struct BigNumber
                {
                    public static explicit operator long(BigNumber b) => 100L;
                }

                class C
                {
                    short M(BigNumber b) => (short)(long)b;
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestUserDefinedToDecimal_ThenDecimalToInt()
        => new VerifyCS.Test
        {
            TestCode = """
                struct Money
                {
                    public static explicit operator decimal(Money m) => 100.50m;
                }

                class C
                {
                    int M(Money m) => [|(int)m|];
                }
                """,
            FixedCode = """
                struct Money
                {
                    public static explicit operator decimal(Money m) => 100.50m;
                }

                class C
                {
                    int M(Money m) => (int)(decimal)m;
                }
                """,
        }.RunAsync();

    #endregion

    #region Explicit reference conversions with user-defined operators

    [Fact]
    public Task TestUserDefinedToBaseClass_ThenBaseToDerived()
        => new VerifyCS.Test
        {
            TestCode = """
                class Animal { }
                class Dog : Animal { }

                class AnimalFactory
                {
                    public static explicit operator Animal(AnimalFactory f) => new Dog();
                }

                class C
                {
                    Dog M(AnimalFactory f) => [|(Dog)f|];
                }
                """,
            FixedCode = """
                class Animal { }
                class Dog : Animal { }

                class AnimalFactory
                {
                    public static explicit operator Animal(AnimalFactory f) => new Dog();
                }

                class C
                {
                    Dog M(AnimalFactory f) => (Dog)(Animal)f;
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestUserDefinedToGrandparent_ThenGrandparentToGrandchild()
           => new VerifyCS.Test
           {
               TestCode = """
                class GrandParent { }
                class Parent : GrandParent { }
                class Child : Parent { }

                struct Wrapper
                {
                    public static explicit operator GrandParent(Wrapper w) => new Child();
                }

                class C
                {
                    Child M(Wrapper w) => [|(Child)w|];
                }
                """,
               FixedCode = """
                class GrandParent { }
                class Parent : GrandParent { }
                class Child : Parent { }

                struct Wrapper
                {
                    public static explicit operator GrandParent(Wrapper w) => new Child();
                }

                class C
                {
                    Child M(Wrapper w) => (Child)(GrandParent)w;
                }
                """,
           }.RunAsync();

    #endregion

    #region Explicit nullable conversions with user-defined operators

    [Fact]
    public Task TestUserDefinedToNullableInt_ThenNullableIntToInt()
        => new VerifyCS.Test
        {
            TestCode = """
                struct OptionalNumber
                {
                    public static explicit operator int?(OptionalNumber o) => 42;
                }

                class C
                {
                    int M(OptionalNumber o) => [|(int)o|];
                }
                """,
            FixedCode = """
                struct OptionalNumber
                {
                    public static explicit operator int?(OptionalNumber o) => 42;
                }

                class C
                {
                    int M(OptionalNumber o) => (int)(int?)o;
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestUserDefinedToNullableLong_ThenNullableLongToShort()
        => new VerifyCS.Test
        {
            TestCode = """
                struct LargeOptional
                {
                    public static explicit operator long?(LargeOptional l) => 1000L;
                }

                class C
                {
                    short M(LargeOptional l) => [|(short)l|];
                }
                """,
            FixedCode = """
                struct LargeOptional
                {
                    public static explicit operator long?(LargeOptional l) => 1000L;
                }

                class C
                {
                    short M(LargeOptional l) => (short)(long?)l;
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestNullableSourceToUserDefinedFromInt()
        => new VerifyCS.Test
        {
            TestCode = """
                struct Target
                {
                    public static explicit operator Target(int i) => default;
                }

                class C
                {
                    Target M(long? l) => [|(Target)l|];
                }
                """,
            FixedCode = """
                struct Target
                {
                    public static explicit operator Target(int i) => default;
                }

                class C
                {
                    Target M(long? l) => (Target)(int)l;
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestUserDefinedToNullableDouble_ThenToFloat()
        => new VerifyCS.Test
        {
            TestCode = """
                struct PreciseValue
                {
                    public static explicit operator double?(PreciseValue p) => 3.14159;
                }

                class C
                {
                    float M(PreciseValue p) => [|(float)p|];
                }
                """,
            FixedCode = """
                struct PreciseValue
                {
                    public static explicit operator double?(PreciseValue p) => 3.14159;
                }

                class C
                {
                    float M(PreciseValue p) => (float)(double?)p;
                }
                """,
        }.RunAsync();

    #endregion

    #region Explicit unboxing conversions with user-defined operators

    #endregion

    #region Complex expression contexts

    [Fact]
    public Task TestInArrayInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
                struct Value
                {
                    public static explicit operator long(Value v) => 100L;
                }

                class C
                {
                    int[] M(Value v) => new int[] { [|(int)v|] };
                }
                """,
            FixedCode = """
                struct Value
                {
                    public static explicit operator long(Value v) => 100L;
                }

                class C
                {
                    int[] M(Value v) => new int[] { (int)(long)v };
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestInConditionalExpression()
        => new VerifyCS.Test
        {
            TestCode = """
                struct Value
                {
                    public static explicit operator long(Value v) => 100L;
                }

                class C
                {
                    int M(Value v, bool b) => b ? [|(int)v|] : 0;
                }
                """,
            FixedCode = """
                struct Value
                {
                    public static explicit operator long(Value v) => 100L;
                }

                class C
                {
                    int M(Value v, bool b) => b ? (int)(long)v : 0;
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestInInterpolatedString()
        => new VerifyCS.Test
        {
            TestCode = """
                struct Value
                {
                    public static explicit operator double(Value v) => 3.14;
                }

                class C
                {
                    string M(Value v) => $"Result: {[|(int)v|]}";
                }
                """,
            FixedCode = """
                struct Value
                {
                    public static explicit operator double(Value v) => 3.14;
                }

                class C
                {
                    string M(Value v) => $"Result: {(int)(double)v}";
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestInLambdaExpression()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                struct Value
                {
                    public static explicit operator long(Value v) => 100L;
                }

                class C
                {
                    Func<Value, int> M() => v => [|(int)v|];
                }
                """,
            FixedCode = """
                using System;

                struct Value
                {
                    public static explicit operator long(Value v) => 100L;
                }

                class C
                {
                    Func<Value, int> M() => v => (int)(long)v;
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestInObjectInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
                struct Value
                {
                    public static explicit operator long(Value v) => 100L;
                }

                class Target
                {
                    public int Number { get; set; }
                }

                class C
                {
                    Target M(Value v) => new Target { Number = [|(int)v|] };
                }
                """,
            FixedCode = """
                struct Value
                {
                    public static explicit operator long(Value v) => 100L;
                }

                class Target
                {
                    public int Number { get; set; }
                }

                class C
                {
                    Target M(Value v) => new Target { Number = (int)(long)v };
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestInSwitchExpression()
        => new VerifyCS.Test
        {
            TestCode = """
                struct Value
                {
                    public static explicit operator long(Value v) => 100L;
                }

                class C
                {
                    string M(Value v) => [|(int)v|] switch
                    {
                        0 => "zero",
                        _ => "other"
                    };
                }
                """,
            FixedCode = """
                struct Value
                {
                    public static explicit operator long(Value v) => 100L;
                }

                class C
                {
                    string M(Value v) => (int)(long)v switch
                    {
                        0 => "zero",
                        _ => "other"
                    };
                }
                """,
        }.RunAsync();

    #endregion

    #region Generic and collection contexts

    [Fact]
    public Task TestInCollectionInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                struct Value
                {
                    public static explicit operator long(Value v) => 100L;
                }

                class C
                {
                    List<int> M(Value v) => new List<int> { [|(int)v|] };
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                struct Value
                {
                    public static explicit operator long(Value v) => 100L;
                }

                class C
                {
                    List<int> M(Value v) => new List<int> { (int)(long)v };
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestInLinqSelect()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;
                using System.Linq;

                struct Value
                {
                    public static explicit operator long(Value v) => 100L;
                }

                class C
                {
                    IEnumerable<int> M(IEnumerable<Value> values) => values.Select(v => [|(int)v|]);
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                using System.Linq;

                struct Value
                {
                    public static explicit operator long(Value v) => 100L;
                }

                class C
                {
                    IEnumerable<int> M(IEnumerable<Value> values) => values.Select(v => (int)(long)v);
                }
                """,
        }.RunAsync();

    #endregion
}
