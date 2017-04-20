using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;


namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class AsyncMainEntryPointTests: CompilingTestBase
    {
        [Fact]
        public void MainCanBeAsyncWithArgs()
        {
            var origSource = @"
using System.Threading.Tasks;

class A
{
    async static Task Main(string[] args)
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            var sources = new string[] { origSource, origSource.Replace("async ", "").Replace("await", "return") };
            foreach (var source in sources)
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
                compilation.VerifyDiagnostics();
                var entry = compilation.GetEntryPoint(CancellationToken.None);
                Assert.NotNull(entry);
                Assert.Equal("System.Threading.Tasks.Task A.Main(System.String[] args)", entry.ToTestDisplayString());
            }
        }

        [Fact]
        public void MainCantBeAsyncWithRefTask()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static ref Task Main(string[] args)
    {
        throw new System.Exception();
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics(
                // (6,21): warning CS0028: 'A.Main(string[])' has the wrong signature to be an entry point
                //     static ref Task Main(string[] args)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main(string[])").WithLocation(6, 21),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void MainCantBeAsyncWithArgs_CSharp7()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static Task Main(string[] args)
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
                // (6,5): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                //     async static Task Main(string[] args)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, @"async static Task Main(string[] args)
    {
        await Task.Factory.StartNew(() => { });
    }").WithArguments("async main", "7.1").WithLocation(6, 5));
        }

        [Fact]
        public void MainCanBeAsync()
        {
            var origSource = @"
using System.Threading.Tasks;

class A
{
    async static Task Main()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            var sources = new string[] { origSource, origSource.Replace("async ", "").Replace("await", "return") };
            foreach (var source in sources)
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
                compilation.VerifyDiagnostics();
                var entry = compilation.GetEntryPoint(CancellationToken.None);
                Assert.NotNull(entry);
                Assert.Equal("System.Threading.Tasks.Task A.Main()", entry.ToTestDisplayString());
            }
        }

        [Fact]
        public void MainCantBeAsyncVoid()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics(
                // (6,23): warning CS0028: 'A.Main()' has the wrong signature to be an entry point
                //     async static void Main()
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main()").WithLocation(6, 23),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.Null(entry);
        }

        [Fact]
        public void MainCantBeAsyncVoid_CSharp7()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
                // (6,5): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                //     async static void Main()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, @"async static void Main()
    {
        await Task.Factory.StartNew(() => { });
    }").WithArguments("async main", "7.1").WithLocation(6, 5));
        }

        [Fact]
        public void MainCantBeAsyncInt()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static int Main()
    {
        return await Task.Factory.StartNew(() => 5);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics(
                // (6,22): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async static int Main()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "Main").WithLocation(6, 22),
                // (6,23): warning CS0028: 'A.Main()' has the wrong signature to be an entry point
                //     async static void Main()
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main()").WithLocation(6, 22),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.Null(entry);
        }

        [Fact]
        public void MainCantBeAsyncInt_CSharp7()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static int Main()
    {
        return await Task.Factory.StartNew(() => 5);
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
                // (6,22): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async static int Main()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "Main"),
                // (6,5): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                //     async static int Main()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, @"async static int Main()
    {
        return await Task.Factory.StartNew(() => 5);
    }").WithArguments("async main", "7.1").WithLocation(6, 5)
);
        }

        [Fact]
        public void MainCanBeAsyncAndGenericOnIntWithArgs()
        {
            var origSource = @"
using System.Threading.Tasks;

class A
{
    async static Task<int> Main(string[] args)
    {
        return await Task.Factory.StartNew(() => 5);
    }
}";
            var sources = new string[] { origSource, origSource.Replace("async ", "").Replace("await", "") };
            foreach (var source in sources)
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
                compilation.VerifyDiagnostics();
                var entry = compilation.GetEntryPoint(CancellationToken.None);
                Assert.NotNull(entry);
                Assert.Equal("System.Threading.Tasks.Task<System.Int32> A.Main(System.String[] args)", entry.ToTestDisplayString());
            }
        }

        [Fact]
        public void MainCantBeAsyncAndGenericOnIntWithArgs_Csharp7()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static Task<int> Main(string[] args)
    {
        return await Task.Factory.StartNew(() => 5);
    }
}";
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
                compilation.VerifyDiagnostics(
// (6,5): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                //     async static Task<int> Main(string[] args)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, @"async static Task<int> Main(string[] args)
    {
        return await Task.Factory.StartNew(() => 5);
    }").WithArguments("async main", "7.1").WithLocation(6, 5)
);
        }

        [Fact]
        public void MainCanBeAsyncAndGenericOnInt()
        {
            var origSource = @"
using System.Threading.Tasks;

class A
{
    async static Task<int> Main()
    {
        return await Task.Factory.StartNew(() => 5);
    }
}";
            var sources = new string[] { origSource, origSource.Replace("async ", "").Replace("await", "") };
            foreach (var source in sources)
            {
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
                compilation.VerifyDiagnostics();
                var entry = compilation.GetEntryPoint(CancellationToken.None);
                Assert.NotNull(entry);
                Assert.Equal("System.Threading.Tasks.Task<System.Int32> A.Main()", entry.ToTestDisplayString());
            }
        }

        [Fact]
        public void MainCantBeAsyncAndGenericOnInt_CSharp7()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static Task<int> Main()
    {
        return await Task.Factory.StartNew(() => 5);
    }
}";
                var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
                compilation.VerifyDiagnostics(
                    // (6,5): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                    //     async static Task<int> Main()
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, @"async static Task<int> Main()
    {
        return await Task.Factory.StartNew(() => 5);
    }").WithArguments("async main", "7.1").WithLocation(6, 5)
                    );
        }

        [Fact]
        public void MainCantBeAsyncAndGenericOverFloats()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static Task<float> Main()
    {
        await Task.Factory.StartNew(() => { });
        return 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics(
                // (6,30): warning CS0028: 'A.Main()' has the wrong signature to be an entry point
                //     async static Task<float> Main()
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main()").WithLocation(6, 30),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1) );
        }

        [Fact]
        public void MainCantBeAsync_AndGeneric()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main<T>()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1)).VerifyDiagnostics(
                // (4,23): warning CS0402: 'A.Main<T>()': an entry point cannot be generic or in a generic type
                //     async static void Main<T>()
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main<T>()"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void MainCantBeAsync_AndBadSig()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main(bool truth)
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (4,23): warning CS0028: 'A.Main(bool)' has the wrong signature to be an entry point
                //     async static void Main(bool truth)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main(bool)"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }

        [Fact]
        public void MainCantBeAsync_AndGeneric_AndBadSig()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main<T>(bool truth)
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (4,23): warning CS0028: 'A.Main<T>(bool)' has the wrong signature to be an entry point
                //     async static void Main<T>(bool truth)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main<T>(bool)"),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint));
        }
    }
}
