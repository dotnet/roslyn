// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class CovariantReturnTests : CSharpTestBase
    {
        [Fact]
        public void CovariantReturns_01()
        {
            var source = @"
class Base
{
    public virtual object M() => null;
}
class Derived : Base
{
    public override string M() => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(8, 28)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_02()
        {
            var source = @"
class Base
{
    public virtual T M<T, U>() where T : class where U : class, T => null;
}
class Derived : Base
{
    public override U M<T, U>() => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override U M<T, U>() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(8, 23)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_03()
        {
            var source = @"
class Base<T> where T : class
{
    public virtual T M() => null;
}
class Derived<T, U> : Base<T> where T : class where U : class, T
{
    public override U M() => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override U M() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(8, 23)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_04()
        {
            var source = @"
class N { }
class Base
{
    public virtual N M() => null;
}
class Derived<T> : Base where T : N
{
    public override T M() => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (9,23): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override T M() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(9, 23)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_05()
        {
            var source = @"
class Base
{
    public virtual object M => null;
}
class Derived : Base
{
    public override string M => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS1715: 'Derived.M': type must be 'object' to match overridden member 'Base.M'
                //     public override string M => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M").WithArguments("Derived.M", "Base.M", "object").WithLocation(8, 28)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_06()
        {
            var source = @"
class Base<T> where T : class
{
    public virtual T M => null;
}
class Derived<T, U> : Base<T> where T : class where U : class, T
{
    public override U M => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,23): error CS1715: 'Derived<T, U>.M': type must be 'T' to match overridden member 'Base<T>.M'
                //     public override U M => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M").WithArguments("Derived<T, U>.M", "Base<T>.M", "T").WithLocation(8, 23)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_07()
        {
            var source = @"
class N { }
class Base
{
    public virtual N M => null;
}
class Derived<T> : Base where T : N
{
    public override T M => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (9,23): error CS1715: 'Derived<T>.M': type must be 'N' to match overridden member 'Base.M'
                //     public override T M => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "M").WithArguments("Derived<T>.M", "Base.M", "N").WithLocation(9, 23)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_08()
        {
            var source = @"
class Base
{
    public virtual object this[int i] => null;
}
class Derived : Base
{
    public override string this[int i] => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS1715: 'Derived.this[int]': type must be 'object' to match overridden member 'Base.this[int]'
                //     public override string this[int i] => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "this").WithArguments("Derived.this[int]", "Base.this[int]", "object").WithLocation(8, 28)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_09()
        {
            var source = @"
class Base<T> where T : class
{
    public virtual T this[int i] => null;
}
class Derived<T, U> : Base<T> where T : class where U : class, T
{
    public override U this[int i] => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,23): error CS1715: 'Derived<T, U>.this[int]': type must be 'T' to match overridden member 'Base<T>.this[int]'
                //     public override U this[int i] => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "this").WithArguments("Derived<T, U>.this[int]", "Base<T>.this[int]", "T").WithLocation(8, 23)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_10()
        {
            var source = @"
class N { }
class Base
{
    public virtual N this[int i] => null;
}
class Derived<T> : Base where T : N
{
    public override T this[int i] => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (9,23): error CS1715: 'Derived<T>.this[int]': type must be 'N' to match overridden member 'Base.this[int]'
                //     public override T this[int i] => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "this").WithArguments("Derived<T>.this[int]", "Base.this[int]", "N").WithLocation(9, 23)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_Events()
        {
            var source = @"
using System;
class Base
{
    public virtual event Func<object> E;
    private void SuppressUnusedWarning() => E?.Invoke();
}
class Derived : Base
{
    public override event Func<string> E;
    private void SuppressUnusedWarning() => E?.Invoke();
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (10,40): error CS1715: 'Derived.E': type must be 'Func<object>' to match overridden member 'Base.E'
                //     public override event Func<string> E;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "E").WithArguments("Derived.E", "Base.E", "System.Func<object>").WithLocation(10, 40)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                // (10,40): error CS1715: 'Derived.E': type must be 'Func<object>' to match overridden member 'Base.E'
                //     public override event Func<string> E;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "E").WithArguments("Derived.E", "Base.E", "System.Func<object>").WithLocation(10, 40)
                );
        }

        // PROTOTYPE: Future tests to be added:
        // - What is expected for public Derived : Base { public new string M => null;
        // - What is expected for public Derived : Base { public string M => null;
        // - What is expected for public Derived2: Derived { public new object/string M => null;
        // - Please add a test where Base has a property, Derived hides it with a different return type, and Derived2 tries to override with either return type.
        //   - These are also applicable to virtual methods.
        // - I assume that an abstract method can be implemented with a covariant return. Please add a test.
        // - Please add a test with nested variance involved (returning CIn<object> vs.CIn<string>, or COut<object> vs.COut<string>). Also consider nullability variance (COut<object?>vs.COut<string!>` and some permutations).
        // - Test with an override that doesn't have an implicit reference conversion from base. For instance, numeric types, types convertible via user-defined operators, etc.
        // - Test with wrong variance in override (Base.Method() returns string but override Derived.Method() returns object)
        // - Test other implicit reference conversion scenarios, such as Interface Base.Method() and TypeThatImplementsInterface Derived.Method(), to lock-in the proper check
        // - Test some DIM scenarios (no changed behavior)
        // - Test that UD conversions don't count (not an implicit reference conversion)
        // - Test three levels of inheritance - overriding one, both, neither, correctly, incorrectly. https://github.com/dotnet/roslyn/pull/43576#discussion_r414074476
    }
}
