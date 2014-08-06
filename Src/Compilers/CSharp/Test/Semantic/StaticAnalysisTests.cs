// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class StaticAnalysisTests : CompilingTestBase
    {
        [Fact]
        public void TestMissingDispose()
        {
            var source =
@"using System;

namespace Washington
{
    public class MyDisposable : IDisposable
    {
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool managedResourcesToo)
        {
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            MyDisposable x = new MyDisposable();
            return;
        }
    }
}";
            CreateCompilationWithMscorlib(source, options: StaticAnalysisCompilationOptions).VerifyDiagnostics(
                // (22,30): error CS10000: Call System.IDisposable.Dispose() on allocated instance of Washington.MyDisposable before all references to it are out of scope.
                //             MyDisposable x = new MyDisposable();
                Diagnostic(ErrorCode.WRN_CA2000_DisposeObjectsBeforeLosingScope1, "new MyDisposable()").WithArguments("Washington.MyDisposable").WithLocation(22, 30)
                );
        }

        private CSharpCompilationOptions StaticAnalysisCompilationOptions
        {
            get
            {
                var compOptions = TestOptions.ReleaseDll.WithFeatures((new[] { "checkdispose" }).AsImmutable());
                if (Debugger.IsAttached)
                {
                    // Using single-threaded build if debugger attached, to simplify debugging.
                    compOptions = compOptions.WithConcurrentBuild(false);
                }
                return compOptions;
            }
        }

        [Fact]
        public void TestProperlyDisposed01()
        {
            var source =
@"using System;

namespace Washington
{
    public class MyDisposable : IDisposable
    {
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool managedResourcesToo)
        {
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            MyDisposable x = new MyDisposable();
            x.Dispose();
            return;
        }
    }
}";
            CreateCompilationWithMscorlib(source, options: StaticAnalysisCompilationOptions).VerifyDiagnostics();
        }

        [Fact]
        public void TestNullCheck()
        {
            var source =
@"using System;

namespace Washington
{
    public class MyDisposable : IDisposable
    {
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool managedResourcesToo)
        {
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            MyDisposable x;
            x = new MyDisposable();
            if (x != null) x.Dispose();
            return;
        }
    }
}";
            CreateCompilationWithMscorlib(source, options: StaticAnalysisCompilationOptions).VerifyDiagnostics();
        }

        [Fact]
        public void TestExceptionFromMethod()
        {
            var source =
@"using System;

namespace Washington
{
    public class MyDisposable : IDisposable
    {
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool managedResourcesToo)
        {
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            MyDisposable x;
            x = new MyDisposable();
            """".GetHashCode(); // can throw an exception
            x.Dispose();
            return;
        }
    }
}";
            CreateCompilationWithMscorlib(source, options: StaticAnalysisCompilationOptions).VerifyDiagnostics(
                // (23,17): error CS10001: Allocated instance of Washington.MyDisposable is not disposed along all exception paths.  Call System.IDisposable.Dispose() before all references to it are out of scope.
                //             x = new MyDisposable();
                Diagnostic(ErrorCode.WRN_CA2000_DisposeObjectsBeforeLosingScope2, "new MyDisposable()").WithArguments("Washington.MyDisposable")
                );
        }

        [Fact]
        public void TestDuplicateDispose()
        {
            var source =
@"using System;

namespace Washington
{
    public class MyDisposable : IDisposable
    {
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool managedResourcesToo)
        {
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            MyDisposable x;
            x = new MyDisposable();
            x.Dispose();
            x.Dispose();
            return;
        }
    }
}";
            CreateCompilationWithMscorlib(source, options: StaticAnalysisCompilationOptions).VerifyDiagnostics(
                // (25,13): error CS10002: Object 'new MyDisposable()' can be disposed more than once.
                //             x.Dispose();
                Diagnostic(ErrorCode.WRN_CA2202_DoNotDisposeObjectsMultipleTimes, "x.Dispose()").WithArguments("new MyDisposable()")
                );
        }

        [Fact]
        public void SaveToField()
        {
            var source =
@"using System;

namespace Washington
{
    public class MyDisposable : IDisposable
    {
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool managedResourcesToo)
        {
        }
    }

    public class Program
    {
        static Program whatever = null;
        MyDisposable y;
        public static void Main(string[] args)
        {
            MyDisposable x = new MyDisposable();
            whatever.y = x;
            return;
        }
    }
}";
            CreateCompilationWithMscorlib(source, options: StaticAnalysisCompilationOptions).VerifyDiagnostics(
                );
        }

        [Fact]
        public void SaveToProperty()
        {
            var source =
@"using System;

namespace Washington
{
    public class MyDisposable : IDisposable
    {
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool managedResourcesToo)
        {
        }
    }

    public class Program
    {
        static Program whatever = null;
        MyDisposable y { get; set; }
        public static void Main(string[] args)
        {
            MyDisposable x = new MyDisposable();
            whatever.y = x;
            return;
        }
    }
}";
            CreateCompilationWithMscorlib(source, options: StaticAnalysisCompilationOptions).VerifyDiagnostics(
                );
        }

        [Fact]
        public void SaveToProperty2()
        {
            var source =
@"using System;

namespace Washington
{
    public class MyDisposable : IDisposable
    {
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool managedResourcesToo)
        {
        }
    }

    public class Program
    {
        static Program G() { return null; }
        MyDisposable y { get; set; }
        public static void Main(string[] args)
        {
            MyDisposable x = new MyDisposable();
            G().y = x;
            return;
        }
    }
}";
            CreateCompilationWithMscorlib(source, options: StaticAnalysisCompilationOptions).VerifyDiagnostics(
                // (23,17): error CS10001: Allocated instance of Washington.MyDisposable is not disposed along all exception paths.  Call System.IDisposable.Dispose() before all references to it are out of scope.
                //             x = new MyDisposable();
                Diagnostic(ErrorCode.WRN_CA2000_DisposeObjectsBeforeLosingScope2, "new MyDisposable()").WithArguments("Washington.MyDisposable")
                );
        }

    }
}
