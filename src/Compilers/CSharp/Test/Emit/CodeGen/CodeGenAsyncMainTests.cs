// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenAsyncMainTests : EmitMetadataTestBase
    {
        [Fact]
        public void GetResultReturnsSomethingElse()
        {
            var source = @"
using System.Threading.Tasks;

namespace System.Threading.Tasks {
    public class Awaiter {
        public double GetResult() { return 0.0; }
    }
    public class Task<T> {
        public Awaiter GetAwaiter() {
            return new Awaiter();
        }
    }
}

static class Program {
    static Task<int> Main() {
        return null;
    }
}";
            var sourceCompilation = CreateCompilationWithMscorlib(source,  options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            // Prototype: this should be a diagnostic.
            Assert.Throws<System.Exception>(() => { sourceCompilation.VerifyEmitDiagnostics(); });
        }

        [Fact]
        public void TaskOfTGetAwaiterReturnsVoid()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace System.Threading.Tasks {
    public class Task<T> {
        public void GetAwaiter() {}
    }
}

static class Program {
    static Task<int> Main() {
        return null;
    }
}";

            // PROTOTYPE(async-main):  make sure that these errors are better.
            var sourceCompilation = CreateCompilationWithMscorlib(source,  options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // (12,12): warning CS0436: The type 'Task<T>' in '' conflicts with the imported type 'Task<TResult>' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     Task<int>
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task<int>").WithArguments("", "System.Threading.Tasks.Task<T>", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task<TResult>").WithLocation(12, 12),
                // (12,5): error CS0656: Missing compiler required member 'void.GetResult'
                //     Task<int>
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"Task<int>").WithArguments("void", "GetResult").WithLocation(12, 12)
);
        }
        [Fact]
        public void TaskGetAwaiterReturnsVoid()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace System.Threading.Tasks {
    public class Task {
        public void GetAwaiter() {}
    }
}

static class Program {
    static Task Main() {
        return null;
    }
}";
            // PROTOTYPE(async-main):  make sure that these errors are better.
            var sourceCompilation = CreateCompilationWithMscorlib(source,  options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // (12,12): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task").WithLocation(12, 12),
                // (12,5): error CS0656: Missing compiler required member 'void.GetResult'
                //     static Task Main() {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"Task").WithArguments("void", "GetResult").WithLocation(12, 12));
        }

        [Fact]
        public void MissingMethodsOnTask()
        {
            var source = @"
using System;
using System.Threading.Tasks;

namespace System.Threading.Tasks {
    public class Task {}
}

static class Program {
    static Task Main() {
        return null;
    }
}";
            var sourceCompilation = CreateCompilationWithMscorlib(source,  options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // (10,12): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task"),
                // (10,12): error CS0656: Missing compiler required member 'Task.GetAwaiter'
                //     static Task Main() {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "Task").WithArguments("System.Threading.Tasks.Task", "GetAwaiter").WithLocation(10, 12));
        }

        [Fact]
        public void EmitTaskOfIntReturningMainWithoutInt()
        {
            var corAssembly = @"
namespace System {
    public class Object {}
}";
            var corCompilation = CreateCompilation(corAssembly, options: TestOptions.DebugDll);
            corCompilation.VerifyDiagnostics();

            var taskAssembly = @"
namespace System.Threading.Tasks {
    public class Task<T>{}
}";
            var taskCompilation = CreateCompilationWithMscorlib45(taskAssembly, options: TestOptions.DebugDll);
            taskCompilation.VerifyDiagnostics();

            var source = @"
using System;
using System.Threading.Tasks;

static class Program {
    static Task<int> Main() {
        return null;
    }
}";
            var sourceCompilation = CreateCompilation(source, new[] { corCompilation.ToMetadataReference(), taskCompilation.ToMetadataReference() },  options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (6,17): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "int").WithArguments("System.Int32"),
                // (6,12): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Task<int>").WithArguments("System.Int32").WithLocation(6, 12),
                // (6,12): error CS0656: Missing compiler required member 'Task<int>.GetAwaiter'
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "Task<int>").WithArguments("System.Threading.Tasks.Task<int>", "GetAwaiter").WithLocation(6, 12));
        }

        [Fact]
        public void EmitTaskReturningMainWithoutVoid()
        {
            var corAssembly = @"
namespace System {
    public class Object {}
}";
            var corCompilation = CreateCompilation(corAssembly, options: TestOptions.DebugDll);
            corCompilation.VerifyDiagnostics();

            var taskAssembly = @"
namespace System.Threading.Tasks {
    public class Task{}
}";
            var taskCompilation = CreateCompilationWithMscorlib45(taskAssembly, options: TestOptions.DebugDll);
            taskCompilation.VerifyDiagnostics();

            var source = @"
using System;
using System.Threading.Tasks;

static class Program {
    static Task Main() {
        return null;
    }
}";
            var sourceCompilation = CreateCompilation(source, new[] { corCompilation.ToMetadataReference(), taskCompilation.ToMetadataReference() },  options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion),
                // (6,12): error CS0518: Predefined type 'System.Void' is not defined or imported
                //     static Task Main() {
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Task").WithArguments("System.Void").WithLocation(6, 12),
                // (6,12): error CS0656: Missing compiler required member 'Task.GetAwaiter'
                //     static Task Main() {
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "Task").WithArguments("System.Threading.Tasks.Task", "GetAwaiter").WithLocation(6, 12));
        }

        [Fact]
        public void AsyncEmitMainOfIntTest()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task<int> Main() {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(""async main"");
        return 10;
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 10);
        }

        [Fact]
        public void AsyncEmitMainTest()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task Main() {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(""async main"");
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 0);
        }

        [Fact]
        public void AsyncEmitMainTestCodegenWithErrors()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task<int> Main() {
        Console.WriteLine(""hello"");
        await Task.Factory.StartNew(() => 5);
        Console.WriteLine(""async main"");
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            c.VerifyEmitDiagnostics(
                // (6,28): error CS0161: 'Program.Main()': not all code paths return a value
                //     static async Task<int> Main() {
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Main").WithArguments("Program.Main()").WithLocation(6, 28));
        }


        [Fact]
        public void AsyncEmitMainOfIntTest_StringArgs()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task<int> Main(string[] args) {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(""async main"");
        return 10;
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 10);
        }

        [Fact]
        public void AsyncEmitMainTest_StringArgs()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task Main(string[] args) {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(""async main"");
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 0);
        }

        [Fact]
        public void AsyncEmitMainTestCodegenWithErrors_StringArgs()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task<int> Main(string[] args) {
        Console.WriteLine(""hello"");
        await Task.Factory.StartNew(() => 5);
        Console.WriteLine(""async main"");
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            c.VerifyEmitDiagnostics(
                // (6,28): error CS0161: 'Program.Main()': not all code paths return a value
                //     static async Task<int> Main() {
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Main").WithArguments("Program.Main(string[])").WithLocation(6, 28));
        }

        [Fact]
        public void AsyncEmitMainOfIntTest_StringArgs_WithArgs()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task<int> Main(string[] args) {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(args[0]);
        return 10;
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 10, args: new string[] { "async main" });
        }

        [Fact]
        public void AsyncEmitMainTest_StringArgs_WithArgs()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task Main(string[] args) {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(args[0]);
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 0, args: new string[] { "async main" });
        }
    }
}
