// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class CovariantReturnTests : CSharpTestBase
    {
        [Fact]
        public void CovariantReturns_01()
        {
            var source = @"
public class Base
{
    public virtual object M() => null;
}
public class Derived : Base
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
public class Base
{
    public virtual T M<T, U>() where T : class where U : class, T => null;
}
public class Derived : Base
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
public class Base<T> where T : class
{
    public virtual T M() => null;
}
public class Derived<T, U> : Base<T> where T : class where U : class, T
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
public class N { }
public class Base
{
    public virtual N M() => null;
}
public class Derived<T> : Base where T : N
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
public class Base
{
    public virtual object M => null;
}
public class Derived : Base
{
    public override string M => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(8, 28)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_06()
        {
            var source = @"
public class Base<T> where T : class
{
    public virtual T M => null;
}
public class Derived<T, U> : Base<T> where T : class where U : class, T
{
    public override U M => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override U M => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(8, 23)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_07()
        {
            var source = @"
public class N { }
public class Base
{
    public virtual N M => null;
}
public class Derived<T> : Base where T : N
{
    public override T M => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (9,23): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override T M => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(9, 23)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_08()
        {
            var source = @"
public class Base
{
    public virtual object this[int i] => null;
}
public class Derived : Base
{
    public override string this[int i] => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string this[int i] => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "this").WithArguments("covariant returns").WithLocation(8, 28)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_09()
        {
            var source = @"
public class Base<T> where T : class
{
    public virtual T this[int i] => null;
}
public class Derived<T, U> : Base<T> where T : class where U : class, T
{
    public override U this[int i] => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,23): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override U this[int i] => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "this").WithArguments("covariant returns").WithLocation(8, 23)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_10()
        {
            var source = @"
public class N { }
public class Base
{
    public virtual N this[int i] => null;
}
public class Derived<T> : Base where T : N
{
    public override T this[int i] => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (9,23): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override T this[int i] => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "this").WithArguments("covariant returns").WithLocation(9, 23)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_Events()
        {
            var source = @"
using System;
public class Base
{
    public virtual event Func<object> E;
    private void SuppressUnusedWarning() => E?.Invoke();
}
public class Derived : Base
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

        [Fact]
        public void CovariantReturns_WritableProperties()
        {
            var source = @"
using System;
public class Base
{
    public virtual Func<object> P { get; set; }
}
public class Derived : Base
{
    public override Func<string> P { get; set; }
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (9,38): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override Func<string> P { get; set; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "get").WithArguments("covariant returns").WithLocation(9, 38),
                // (9,43): error CS0115: 'Derived.P.set': no suitable method found to override
                //     public override Func<string> P { get; set; }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "set").WithArguments("Derived.P.set").WithLocation(9, 43)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                // (9,43): error CS0115: 'Derived.P.set': no suitable method found to override
                //     public override Func<string> P { get; set; }
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "set").WithArguments("Derived.P.set").WithLocation(9, 43)
                );
        }

        [Fact]
        public void CovariantReturns_MetadataVsSource_01()
        {
            var s0 = @"
public class Base
{
    public virtual object M() => null;
}
";
            var baseMetadata = CreateCompilation(s0).EmitToImageReference();
            var source = @"
public class Derived : Base
{
    public override string M() => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns, references: new[] { baseMetadata }).VerifyDiagnostics(
                // (4,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M() => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(4, 28)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns, references: new[] { baseMetadata }).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_MetadataVsSource_02()
        {
            var s0 = @"
public class Base
{
    public virtual object M => null;
}
";
            var baseMetadata = CreateCompilation(s0).EmitToImageReference();
            var source = @"
public class Derived : Base
{
    public override string M => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns, references: new[] { baseMetadata }).VerifyDiagnostics(
                // (4,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string M => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "M").WithArguments("covariant returns").WithLocation(4, 28)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns, references: new[] { baseMetadata }).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_MetadataVsSource_03()
        {
            var s0 = @"
public class Base
{
    public virtual object this[int i] => null;
}
";
            var baseMetadata = CreateCompilation(s0).EmitToImageReference();
            var source = @"
public class Derived : Base
{
    public override string this[int i] => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns, references: new[] { baseMetadata }).VerifyDiagnostics(
                // (4,28): error CS8652: The feature 'covariant returns' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public override string this[int i] => null;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "this").WithArguments("covariant returns").WithLocation(4, 28)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns, references: new[] { baseMetadata }).VerifyDiagnostics(
                );
        }

        [Fact]
        public void CovariantReturns_11()
        {
            var source = @"
public abstract class Base
{
    public abstract object M();
}
public class Derived : Base
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
        public void CovariantReturns_WrongReturnType()
        {
            var source = @"
public class Base
{
    public virtual string M() => null;
}
public class Derived : Base
{
    public override object M() => null;
}
";
            CreateCompilation(source, parseOptions: TestOptions.WithoutCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS0508: 'Derived.M()': return type must be 'string' to match overridden member 'Base.M()'
                //     public override object M() => null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M").WithArguments("Derived.M()", "Base.M()", "string").WithLocation(8, 28)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                // (8,28): error CS0508: 'Derived.M()': return type must be 'string' to match overridden member 'Base.M()'
                //     public override object M() => null;
                Diagnostic(ErrorCode.ERR_CantChangeReturnTypeOnOverride, "M").WithArguments("Derived.M()", "Base.M()", "string").WithLocation(8, 28)
                );
        }


        // PROTOTYPE: Future tests to be added:
        // - What is expected for public Derived : Base { public new string M => null;
        // - What is expected for public Derived : Base { public string M => null;
        // - What is expected for public Derived2: Derived { public new object/string M => null;
        // - Please add a test where Base has a property, Derived hides it with a different return type, and Derived2 tries to override with either return type.
        //   - These are also applicable to virtual methods.
        // - Please add a test with nested variance involved (returning CIn<object> vs.CIn<string>, or COut<object> vs.COut<string>). Also consider nullability variance (COut<object?>vs.COut<string!>` and some permutations).
        // - Test with an override that doesn't have an implicit reference conversion from base. For instance, numeric types, types convertible via user-defined operators, etc.
        // - Test other implicit reference conversion scenarios, such as Interface Base.Method() and TypeThatImplementsInterface Derived.Method(), to lock-in the proper check
        // - Test some DIM scenarios (no changed behavior)
        // - Test that UD conversions don't count (not an implicit reference conversion)
        // - Test three levels of inheritance - overriding one, both, neither, correctly, incorrectly. https://github.com/dotnet/roslyn/pull/43576#discussion_r414074476
    }
}
