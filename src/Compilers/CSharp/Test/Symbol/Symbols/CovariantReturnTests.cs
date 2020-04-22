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
            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
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
            CreateCompilation(source).VerifyDiagnostics(
                // (9,23): error CS1715: 'Derived<T>.this[int]': type must be 'N' to match overridden member 'Base.this[int]'
                //     public override T this[int i] => null;
                Diagnostic(ErrorCode.ERR_CantChangeTypeOnOverride, "this").WithArguments("Derived<T>.this[int]", "Base.this[int]", "N").WithLocation(9, 23)
                );
            CreateCompilation(source, parseOptions: TestOptions.WithCovariantReturns).VerifyDiagnostics(
                );
        }
    }
}
