// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.AsyncMain, CompilerFeature.Async)]
    public class CodeGenAsyncMainTests : EmitMetadataTestBase
    {
        [Fact]
        public void MultipleMainsOneOfWhichHasBadTaskType_CSharp71_WithMainType()
        {
            var source = @"
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
    static void Main(string[] args) { }
}";
            var sourceCompilation = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe.WithMainTypeName("Program"), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // (11,12): warning CS0436: The type 'Task<T>' in '' conflicts with the imported type 'Task<TResult>' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task<int>").WithArguments("", "System.Threading.Tasks.Task<T>", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task<TResult>").WithLocation(11, 12));
        }

        [Fact]
        public void MultipleMainsOneOfWhichHasBadTaskType_CSharp7()
        {
            var source = @"
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
    static void Main(string[] args) { }
}";
            var sourceCompilation = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            sourceCompilation.VerifyEmitDiagnostics(
                // (11,12): warning CS0436: The type 'Task<T>' in '' conflicts with the imported type 'Task<TResult>' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task<int>").WithArguments("", "System.Threading.Tasks.Task<T>", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task<TResult>").WithLocation(11, 12),
                // (11,22): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(11, 22));
        }

        [Fact]
        public void MultipleMainsOneOfWhichHasBadTaskType_CSharp7_WithExplicitMain()
        {
            var source = @"
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
    static void Main(string[] args) { }
}";
            var sourceCompilation = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe.WithMainTypeName("Program"), parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            sourceCompilation.VerifyEmitDiagnostics(
                // (11,12): warning CS0436: The type 'Task<T>' in '' conflicts with the imported type 'Task<TResult>' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task<int>").WithArguments("", "System.Threading.Tasks.Task<T>", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task<TResult>").WithLocation(11, 12));
        }

        [Fact]
        public void MultipleMainsOneOfWhichHasBadTaskType_CSharp71()
        {
            var source = @"
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
    static void Main(string[] args) { }
}";
            var sourceCompilation = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // (11,12): warning CS0436: The type 'Task<T>' in '' conflicts with the imported type 'Task<TResult>' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task<int>").WithArguments("", "System.Threading.Tasks.Task<T>", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task<TResult>").WithLocation(11, 12),
                // (11,22): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(11, 22));
        }

        [Fact]
        public void GetResultReturnsSomethingElse_CSharp7()
        {
            var source = @"
using System.Threading.Tasks;
using System;

namespace System.Runtime.CompilerServices {
    public interface INotifyCompletion {
        void OnCompleted(Action action);
    }
}

namespace System.Threading.Tasks {
    public class Awaiter: System.Runtime.CompilerServices.INotifyCompletion {
        public double GetResult() { return 0.0; }
        public bool IsCompleted  => true;
        public void OnCompleted(Action action) {}
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
            var sourceCompilation = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            sourceCompilation.VerifyEmitDiagnostics(
                // (25,12): warning CS0436: The type 'Task<T>' in '' conflicts with the imported type 'Task<TResult>' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task<int>").WithArguments("", "System.Threading.Tasks.Task<T>", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task<TResult>").WithLocation(25, 12),
                // (25,22): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(25, 22),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void GetResultReturnsSomethingElse_CSharp71()
        {
            var source = @"
using System.Threading.Tasks;
using System;

namespace System.Runtime.CompilerServices {
    public interface INotifyCompletion {
        void OnCompleted(Action action);
    }
}

namespace System.Threading.Tasks {
    public class Awaiter: System.Runtime.CompilerServices.INotifyCompletion {
        public double GetResult() { return 0.0; }
        public bool IsCompleted  => true;
        public void OnCompleted(Action action) {}
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
            var sourceCompilation = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // (25,12): warning CS0436: The type 'Task<T>' in '' conflicts with the imported type 'Task<TResult>' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task<int>").WithArguments("", "System.Threading.Tasks.Task<T>", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task<TResult>").WithLocation(25, 12),
                // (25,22): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(25, 22),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void TaskOfTGetAwaiterReturnsVoid_CSharp7()
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

            var sourceCompilation = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            sourceCompilation.VerifyDiagnostics(
                // (12,12): warning CS0436: The type 'Task<T>' in '' conflicts with the imported type 'Task<TResult>' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task<int>").WithArguments("", "System.Threading.Tasks.Task<T>", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task<TResult>").WithLocation(12, 12),
                // (12,12): error CS1986: 'await' requires that the type Task<int> have a suitable GetAwaiter method
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "Task<int>").WithArguments("System.Threading.Tasks.Task<int>").WithLocation(12, 12),
                // (12,22): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(12, 22),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(2, 1));
        }

        [Fact]
        public void TaskOfTGetAwaiterReturnsVoid_CSharp71()
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

            var sourceCompilation = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyDiagnostics(
                // (12,12): warning CS0436: The type 'Task<T>' in '' conflicts with the imported type 'Task<TResult>' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task<int>").WithArguments("", "System.Threading.Tasks.Task<T>", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task<TResult>").WithLocation(12, 12),
                // (12,12): error CS1986: 'await' requires that the type Task<int> have a suitable GetAwaiter method
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "Task<int>").WithArguments("System.Threading.Tasks.Task<int>").WithLocation(12, 12),
                // (12,22): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(12, 22),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(2, 1));
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
            var sourceCompilation = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // (12,12): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task").WithLocation(12, 12),
                // (12,12): error CS1986: 'await' requires that the type Task have a suitable GetAwaiter method
                //     static Task Main() {
                Diagnostic(ErrorCode.ERR_BadAwaitArg, "Task").WithArguments("System.Threading.Tasks.Task").WithLocation(12, 12),
                // (12,17): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(12, 17),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
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
            var sourceCompilation = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // (10,12): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task").WithLocation(10, 12),
                // (10,12): error CS1061: 'Task' does not contain a definition for 'GetAwaiter' and no extension method 'GetAwaiter' accepting a first argument of type 'Task' could be found (are you missing a using directive or an assembly reference?)
                //     static Task Main() {
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Task").WithArguments("System.Threading.Tasks.Task", "GetAwaiter").WithLocation(10, 12),
                // (10,17): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(10, 17),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void EmitTaskOfIntReturningMainWithoutInt()
        {
            var corAssembly = @"
namespace System {
    public class Object {}
    public class Void {}
    public abstract class ValueType{}
    public struct Int32{}
}";
            var corCompilation = CreateEmptyCompilation(corAssembly, options: TestOptions.DebugDll);
            corCompilation.VerifyDiagnostics();

            var taskAssembly = @"
namespace System.Threading.Tasks {
    public class Task<T>{}
}";
            var taskCompilation = CreateCompilationWithMscorlib461(taskAssembly, options: TestOptions.DebugDll);
            taskCompilation.VerifyDiagnostics();

            var source = @"
using System;
using System.Threading.Tasks;

static class Program {
    static Task<int> Main() {
        return null;
    }
}";
            var sourceCompilation = CreateEmptyCompilation(source, new[] { corCompilation.ToMetadataReference(), taskCompilation.ToMetadataReference() }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (6,17): error CS0518: Predefined type 'System.Int32' is not defined or imported
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Task<int>").WithArguments("System.Threading.Tasks.Task<int>", "GetAwaiter").WithLocation(6, 12),
                // (6,22): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task<int> Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(6, 22),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void EmitTaskReturningMainWithoutVoid()
        {
            var corAssembly = @"
namespace System {
    public class Object {}
}";
            var corCompilation = CreateEmptyCompilation(corAssembly, options: TestOptions.DebugDll);
            corCompilation.VerifyDiagnostics();

            var taskAssembly = @"
namespace System.Threading.Tasks {
    public class Task{}
}";
            var taskCompilation = CreateCompilationWithMscorlib461(taskAssembly, options: TestOptions.DebugDll);
            taskCompilation.VerifyDiagnostics();

            var source = @"
using System;
using System.Threading.Tasks;

static class Program {
    static Task Main() {
        return null;
    }
}";
            var sourceCompilation = CreateEmptyCompilation(source, new[] { corCompilation.ToMetadataReference(), taskCompilation.ToMetadataReference() }, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            sourceCompilation.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (6,12): error CS1061: 'Task' does not contain a definition for 'GetAwaiter' and no extension method 'GetAwaiter' accepting a first argument of type 'Task' could be found (are you missing a using directive or an assembly reference?)
                //     static Task Main() {
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Task").WithArguments("System.Threading.Tasks.Task", "GetAwaiter").WithLocation(6, 12),
                // (6,17): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(6, 17),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
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
            var c = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 0);
        }

        [Fact]
        public void AsyncMainTestCodegenWithErrors()
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
            var c = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
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
            var c = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 10);
        }

        [Fact]
        public void AsyncEmitMainOfIntTest_ParamsStringArgs()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program {
    static async Task<int> Main(params string[] args) {
        Console.Write(""hello "");
        await Task.Factory.StartNew(() => 5);
        Console.Write(""async main"");
        return 10;
    }
}";
            var c = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
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
            var c = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
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
            var c = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
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
            var c = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
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
            var c = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = CompileAndVerify(c, expectedOutput: "hello async main", expectedReturnCode: 0, args: new string[] { "async main" });
        }

        [Fact]
        public void MainCanBeAsyncWithArgs()
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task A.Main(System.String[] args)", entry.ToTestDisplayString());

            CompileAndVerify(compilation, expectedReturnCode: 0);
        }

        [Fact]
        public void MainCanReturnTaskWithArgs_NoAsync()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static Task Main(string[] args)
    {
        return Task.Factory.StartNew(() => { });
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task A.Main(System.String[] args)", entry.ToTestDisplayString());

            CompileAndVerify(compilation, expectedReturnCode: 0);
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
                // (6,18): error CS8107: Feature 'async main' is not available in C# 7.0. Please use language version 7.1 or greater.
                //     async static Task Main(string[] args)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Task").WithArguments("async main", "7.1").WithLocation(6, 18),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1)
                );
        }

        [Fact]
        public void MainCantBeAsyncWithArgs_CSharp7_NoAwait()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static Task Main(string[] args)
    {
        return Task.Factory.StartNew(() => { });
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
                // (6,18): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                //     async static Task Main(string[] args)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Task").WithArguments("async main", "7.1").WithLocation(6, 12),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void MainCanReturnTask()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static Task Main()
    {
        await Task.Factory.StartNew(() => { });
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task A.Main()", entry.ToTestDisplayString());

            var image = compilation.EmitToArray();
            using var reader = new PEReader(image);
            var metadataReader = reader.GetMetadataReader();
            var main = metadataReader.MethodDefinitions.Where(mh => getMethodName(mh) == "<Main>").Single();
            Assert.Equal(new[] { "MemberReference:Void System.Diagnostics.DebuggerStepThroughAttribute..ctor()" }, getMethodAttributes(main));

            string getMethodName(MethodDefinitionHandle handle)
                => metadataReader.GetString(metadataReader.GetMethodDefinition(handle).Name);

            IEnumerable<string> getMethodAttributes(MethodDefinitionHandle handle)
                => metadataReader.GetMethodDefinition(handle).GetCustomAttributes()
                    .Select(a => metadataReader.Dump(metadataReader.GetCustomAttribute(a).Constructor));

            if (ExecutionConditionUtil.IsWindows)
            {
                _ = ConditionalSkipReason.NativePdbRequiresDesktop;

                // Verify asyncInfo.catchHandler
                compilation.VerifyPdb("A+<Main>d__0.MoveNext",
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""A"" methodName=""&lt;Main&gt;"" />
  <methods>
    <method containingType=""A+&lt;Main&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""A+&lt;&gt;c"" methodName=""&lt;Main&gt;b__0_0"" />
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""33"" offset=""11"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xe"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0xf"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""48"" document=""1"" />
        <entry offset=""0x3e"" hidden=""true"" document=""1"" />
        <entry offset=""0x91"" hidden=""true"" document=""1"" />
        <entry offset=""0xa9"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0xb1"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <catchHandler offset=""0x91"" />
        <kickoffMethod declaringType=""A"" methodName=""Main"" />
        <await yield=""0x50"" resume=""0x6b"" declaringType=""A+&lt;Main&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>", options: PdbValidationOptions.SkipConversionValidation);
            }
            // The PDB conversion from portable to windows drops the entryPoint node
            // Tracked by https://github.com/dotnet/roslyn/issues/34561
        }

        [Fact]
        public void MainCanReturnTask_NoAsync()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static Task Main()
    {
        return Task.Factory.StartNew(() => { });
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task A.Main()", entry.ToTestDisplayString());
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
                // (6,23): error CS8413: Async Main methods must return Task or Task<int>
                //     async static void Main()
                Diagnostic(ErrorCode.ERR_NonTaskMainCantBeAsync, "Main").WithLocation(6, 23),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics(
                // (6,22): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async static int Main()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "Main").WithLocation(6, 22),
                // (6,22): error CS4009: A void or int returning entry point cannot be async
                //     async static int Main()
                Diagnostic(ErrorCode.ERR_NonTaskMainCantBeAsync, "Main").WithLocation(6, 22),
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
                // (6,22): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async static int Main()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "Main").WithLocation(6, 22),
                // (6,22): error CS8413: Async Main methods must return Task or Task<int>
                //     async static int Main()
                Diagnostic(ErrorCode.ERR_NonTaskMainCantBeAsync, "Main").WithLocation(6, 22),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void MainCanReturnTaskAndGenericOnInt_WithArgs()
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32> A.Main(System.String[] args)", entry.ToTestDisplayString());
        }

        [Fact]
        public void MainCanReturnTaskAndGenericOnInt_WithArgs_NoAsync()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static Task<int> Main(string[] args)
    {
        return Task.Factory.StartNew(() => 5);
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32> A.Main(System.String[] args)", entry.ToTestDisplayString());
        }

        [Fact]
        public void MainCantBeAsyncAndGenericOnInt_WithArgs_Csharp7()
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
                // (6,18): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                //     async static Task<int> Main(string[] args)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Task<int>").WithArguments("async main", "7.1").WithLocation(6, 18),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void MainCantBeAsyncAndGenericOnInt_WithArgs_Csharp7_NoAsync()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static Task<int> Main(string[] args)
    {
        return Task.Factory.StartNew(() => 5);
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
                // (6,18): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                //     async static Task<int> Main(string[] args)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Task<int>").WithArguments("async main", "7.1").WithLocation(6, 12),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void MainCanReturnTaskAndGenericOnInt()
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32> A.Main()", entry.ToTestDisplayString());
        }

        [Fact]
        public void MainCanReturnTaskAndGenericOnInt_NoAsync()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static Task<int> Main()
    {
        return Task.Factory.StartNew(() => 5);
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics();
            var entry = compilation.GetEntryPoint(CancellationToken.None);
            Assert.NotNull(entry);
            Assert.Equal("System.Threading.Tasks.Task<System.Int32> A.Main()", entry.ToTestDisplayString());
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
                // (6,18): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                //     async static Task<int> Main()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Task<int>").WithArguments("async main", "7.1").WithLocation(6, 18),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void MainCantBeAsyncAndGenericOnInt_CSharp7_NoAsync()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static Task<int> Main()
    {
        return Task.Factory.StartNew(() => 5);
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7));
            compilation.VerifyDiagnostics(
                // (6,18): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                //     async static Task<int> Main()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Task<int>").WithArguments("async main", "7.1").WithLocation(6, 12),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
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
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            compilation.VerifyDiagnostics(
                // (6,30): warning CS0028: 'A.Main()' has the wrong signature to be an entry point
                //     async static Task<float> Main()
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main()").WithLocation(6, 30),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
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
            CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1)).VerifyDiagnostics(
                // (6,23): warning CS0402: 'A.Main<T>()': an entry point cannot be generic or in a generic type
                //     async static void Main<T>()
                Diagnostic(ErrorCode.WRN_MainCantBeGeneric, "Main").WithArguments("A.Main<T>()").WithLocation(6, 23),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
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
            CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (6,23): warning CS0028: 'A.Main(bool)' has the wrong signature to be an entry point
                //     async static void Main(bool truth)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main(bool)").WithLocation(6, 23),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
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
            CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (6,23): warning CS0028: 'A.Main<T>(bool)' has the wrong signature to be an entry point
                //     async static void Main<T>(bool truth)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main<T>(bool)").WithLocation(6, 23),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void TaskMainAndNonTaskMain()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static void Main()
    {
        System.Console.WriteLine(""Non Task Main"");
    }
    async static Task Main(string[] args)
    {
        await Task.Factory.StartNew(() => { });
        System.Console.WriteLine(""Task Main"");
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7)).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "Non Task Main", expectedReturnCode: 0);
        }

        [Fact]
        public void TaskMainAndNonTaskMain_CSharp71()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static void Main()
    {
        System.Console.WriteLine(""Non Task Main"");
    }
    async static Task Main(string[] args)
    {
        await Task.Factory.StartNew(() => { });
        System.Console.WriteLine(""Task Main"");
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1)).VerifyDiagnostics(
                // (10,23): warning CS8892: Method 'A.Main(string[])' will not be used as an entry point because a synchronous entry point 'A.Main()' was found.
                //     async static Task Main(string[] args)
                Diagnostic(ErrorCode.WRN_SyncAndAsyncEntryPoints, "Main").WithArguments("A.Main(string[])", "A.Main()").WithLocation(10, 23)
                );
            CompileAndVerify(compilation, expectedOutput: "Non Task Main", expectedReturnCode: 0);
        }

        [Fact]
        public void AsyncVoidMain_CSharp7()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static void Main(string[] args)
    {
        await Task.Factory.StartNew(() => { });
        System.Console.WriteLine(""Async Void Main"");
    }
    async static int Main()
    {
        await Task.Factory.StartNew(() => { });
        System.Console.WriteLine(""Async Void Main"");
        return 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (11,22): error CS1983: The return type of an async method must be void, Task or Task<T>
                //     async static int Main()
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "Main").WithLocation(11, 22),
                // (11,22): error CS4009: A void or int returning entry point cannot be async
                //     async static int Main()
                Diagnostic(ErrorCode.ERR_NonTaskMainCantBeAsync, "Main").WithLocation(11, 22),
                // (6,23): error CS4009: A void or int returning entry point cannot be async
                //     async static void Main(string[] args)
                Diagnostic(ErrorCode.ERR_NonTaskMainCantBeAsync, "Main").WithLocation(6, 23),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void AsyncVoidMain_CSharp71()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static async void Main(string[] args)
    {
        await Task.Factory.StartNew(() => { });
        System.Console.WriteLine(""Async Void Main"");
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1)).VerifyDiagnostics(
                // (6,23): error CS4009: A void or int returning entry point cannot be async
                //     static async void Main(string[] args)
                Diagnostic(ErrorCode.ERR_NonTaskMainCantBeAsync, "Main").WithLocation(6, 23),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void TaskMainAndNonTaskMain_WithExplicitMain()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static void Main()
    {
        System.Console.WriteLine(""Non Task Main"");
    }
    async static Task Main(string[] args)
    {
        await Task.Factory.StartNew(() => { });
        System.Console.WriteLine(""Task Main"");
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithMainTypeName("A")).VerifyDiagnostics(
                // (10,23): warning CS8892: Method 'A.Main(string[])' will not be used as an entry point because a synchronous entry point 'A.Main()' was found.
                //     async static Task Main(string[] args)
                Diagnostic(ErrorCode.WRN_SyncAndAsyncEntryPoints, "Main").WithArguments("A.Main(string[])", "A.Main()").WithLocation(10, 23));
            CompileAndVerify(compilation, expectedOutput: "Non Task Main", expectedReturnCode: 0);
        }

        [Fact]
        public void TaskIntAndTaskFloat_CSharp7()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    async static Task<int> Main()
    {
        System.Console.WriteLine(""Task<int>"");
        return 0;
    }

    async static Task<float> Main(string[] args)
    {
        System.Console.WriteLine(""Task<float>"");
        return 0.0F;
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseDebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7)).VerifyDiagnostics(
                // (6,18): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                //     async static Task<int> Main()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Task<int>").WithArguments("async main", "7.1").WithLocation(6, 18),
                // (12,30): warning CS0028: 'A.Main(string[])' has the wrong signature to be an entry point
                //     async static Task<float> Main(string[] args)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main(string[])").WithLocation(12, 30),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void TaskIntAndTaskFloat_CSharp7_NoAsync()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static Task<int> Main()
    {
        System.Console.WriteLine(""Task<int>"");
        return Task.FromResult(0);
    }

    static Task<float> Main(string[] args)
    {
        System.Console.WriteLine(""Task<float>"");
        return Task.FromResult(0.0F);
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseDebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7)).VerifyDiagnostics(
                // (6,12): error CS8107: Feature 'async main' is not available in C# 7. Please use language version 7.1 or greater.
                //     static Task<int> Main()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "Task<int>").WithArguments("async main", "7.1").WithLocation(6, 12),
                // (12,24): warning CS0028: 'A.Main(string[])' has the wrong signature to be an entry point
                //     static Task<float> Main(string[] args)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main(string[])").WithLocation(12, 24),
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1));
        }

        [Fact]
        public void TaskOfFloatMainAndNonTaskMain()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static void Main()
    {
        System.Console.WriteLine(""Non Task Main"");
    }

    async static Task<float> Main(string[] args)
    {
        await Task.Factory.StartNew(() => { });
        System.Console.WriteLine(""Task Main"");
        return 0.0f;
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (10,30): warning CS0028: 'A.Main(string[])' has the wrong signature to be an entry point
                //     async static Task<float> Main(string[] args)
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("A.Main(string[])").WithLocation(11, 30));
            CompileAndVerify(compilation, expectedOutput: "Non Task Main", expectedReturnCode: 0);
        }

        [Fact]
        public void TaskOfFloatMainAndNonTaskMain_WithExplicitMain()
        {
            var source = @"
using System.Threading.Tasks;

class A
{
    static void Main()
    {
        System.Console.WriteLine(""Non Task Main"");
    }

    async static Task<float> Main(string[] args)
    {
        await Task.Factory.StartNew(() => { });
        System.Console.WriteLine(""Task Main"");
        return 0.0f;
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.ReleaseExe.WithMainTypeName("A")).VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "Non Task Main", expectedReturnCode: 0);
        }

        [Fact]
        public void ImplementGetAwaiterGetResultViaExtensionMethods()
        {
            var source = @"
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices {
    public class ExtensionAttribute {}
}

namespace System.Runtime.CompilerServices {
    public interface INotifyCompletion {
        void OnCompleted(Action action);
    }
}

namespace System.Threading.Tasks {
    public class Awaiter: System.Runtime.CompilerServices.INotifyCompletion {
        public bool IsCompleted  => true;
        public void OnCompleted(Action action) {}
        public void GetResult() {
            System.Console.Write(""GetResult called"");
        }
    }
    public class Task {}
}

public static class MyExtensions {
    public static Awaiter GetAwaiter(this Task task) {
        System.Console.Write(""GetAwaiter called | "");
        return new Awaiter();
    }
}

static class Program {
    static Task Main() {
        return new Task();
    }
}";
            var sourceCompilation = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = sourceCompilation.VerifyEmitDiagnostics(
                // (26,43): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     public static Awaiter GetAwaiter(this Task task) {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task").WithLocation(26, 43),
                // (33,12): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task").WithLocation(33, 12),
                // (34,20): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //         return new Task();
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task").WithLocation(34, 20));
            CompileAndVerify(sourceCompilation, expectedOutput: "GetAwaiter called | GetResult called");
        }

        [Fact]
        public void ImplementGetAwaiterGetResultViaExtensionMethods_Obsolete()
        {
            var source = @"
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices {
    public class ExtensionAttribute {}
}

namespace System.Runtime.CompilerServices {
    public interface INotifyCompletion {
        void OnCompleted(Action action);
    }
}

namespace System.Threading.Tasks {
    public class Awaiter: System.Runtime.CompilerServices.INotifyCompletion {
        public bool IsCompleted  => true;
        public void OnCompleted(Action action) {}
        public void GetResult() {
            System.Console.Write(""GetResult called"");
        }
    }
    public class Task {}
}

public static class MyExtensions {
    [System.Obsolete(""test"")]
    public static Awaiter GetAwaiter(this Task task) {
        System.Console.Write(""GetAwaiter called | "");
        return new Awaiter();
    }
}

static class Program {
    static Task Main() {
        return new Task();
    }
}";
            var sourceCompilation = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = sourceCompilation.VerifyEmitDiagnostics(
                // (34,12): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task").WithLocation(34, 12),
                // (27,43): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     public static Awaiter GetAwaiter(this Task task) {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task").WithLocation(27, 43),
                // (35,20): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //         return new Task();
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task").WithLocation(35, 20),
                // (34,12): warning CS0618: 'MyExtensions.GetAwaiter(Task)' is obsolete: 'test'
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "Task").WithArguments("MyExtensions.GetAwaiter(System.Threading.Tasks.Task)", "test").WithLocation(34, 12));

            CompileAndVerify(sourceCompilation, expectedOutput: "GetAwaiter called | GetResult called");
        }

        [Fact]
        public void ImplementGetAwaiterGetResultViaExtensionMethods_ObsoleteFailing()
        {
            var source = @"
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices {
    public class ExtensionAttribute {}
}

namespace System.Runtime.CompilerServices {
    public interface INotifyCompletion {
        void OnCompleted(Action action);
    }
}

namespace System.Threading.Tasks {
    public class Awaiter: System.Runtime.CompilerServices.INotifyCompletion {
        public bool IsCompleted  => true;
        public void OnCompleted(Action action) {}
        public void GetResult() {}
    }
    public class Task {}
}

public static class MyExtensions {
    [System.Obsolete(""test"", true)]
    public static Awaiter GetAwaiter(this Task task) {
        return null;
    }
}

static class Program {
    static Task Main() {
        return new Task();
    }
}";
            var sourceCompilation = CreateCompilationWithMscorlib40(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));
            var verifier = sourceCompilation.VerifyEmitDiagnostics(
                // (25,43): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     public static Awaiter GetAwaiter(this Task task) {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task").WithLocation(25, 43),
                // (31,12): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //     static Task Main() {
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task").WithLocation(31, 12),
                // (32,20): warning CS0436: The type 'Task' in '' conflicts with the imported type 'Task' in 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //         return new Task();
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Task").WithArguments("", "System.Threading.Tasks.Task", "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Threading.Tasks.Task").WithLocation(32, 20),
                // (31,12): warning CS0618: 'MyExtensions.GetAwaiter(Task)' is obsolete: 'test'
                //     static Task Main() {
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "Task").WithArguments("MyExtensions.GetAwaiter(System.Threading.Tasks.Task)", "test").WithLocation(31, 12));
        }

        [Fact]
        [WorkItem(33542, "https://github.com/dotnet/roslyn/issues/33542")]
        public void AwaitInFinallyInNestedTry_01()
        {
            string source =
@"using System.Threading.Tasks;
class Program
{
    static async Task Main()
    {
        try
        {
            try
            {
                return;
            }
            finally
            {
                await Task.CompletedTask;
            }
        }
        catch
        {
        }
        finally
        {
            await Task.CompletedTask;
        }
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe.WithOptimizationLevel(OptimizationLevel.Release));
            var verifier = CompileAndVerify(comp, expectedOutput: "");
            verifier.VerifyIL("Program.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
@"{
  // Code size      426 (0x1aa)
  .maxstack  3
  .locals init (int V_0,
                object V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                int V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_001f
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_0125
    IL_0011:  ldarg.0
    IL_0012:  ldnull
    IL_0013:  stfld      ""object Program.<Main>d__0.<>7__wrap1""
    IL_0018:  ldarg.0
    IL_0019:  ldc.i4.0
    IL_001a:  stfld      ""int Program.<Main>d__0.<>7__wrap2""
    IL_001f:  nop
    .try
    {
      IL_0020:  ldloc.0
      IL_0021:  pop
      IL_0022:  nop
      .try
      {
        IL_0023:  ldloc.0
        IL_0024:  brfalse.s  IL_007e
        IL_0026:  ldarg.0
        IL_0027:  ldnull
        IL_0028:  stfld      ""object Program.<Main>d__0.<>7__wrap3""
        IL_002d:  ldarg.0
        IL_002e:  ldc.i4.0
        IL_002f:  stfld      ""int Program.<Main>d__0.<>7__wrap4""
        .try
        {
          IL_0034:  ldarg.0
          IL_0035:  ldc.i4.1
          IL_0036:  stfld      ""int Program.<Main>d__0.<>7__wrap4""
          IL_003b:  leave.s    IL_0047
        }
        catch object
        {
          IL_003d:  stloc.1
          IL_003e:  ldarg.0
          IL_003f:  ldloc.1
          IL_0040:  stfld      ""object Program.<Main>d__0.<>7__wrap3""
          IL_0045:  leave.s    IL_0047
        }
        IL_0047:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get""
        IL_004c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
        IL_0051:  stloc.2
        IL_0052:  ldloca.s   V_2
        IL_0054:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
        IL_0059:  brtrue.s   IL_009a
        IL_005b:  ldarg.0
        IL_005c:  ldc.i4.0
        IL_005d:  dup
        IL_005e:  stloc.0
        IL_005f:  stfld      ""int Program.<Main>d__0.<>1__state""
        IL_0064:  ldarg.0
        IL_0065:  ldloc.2
        IL_0066:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter Program.<Main>d__0.<>u__1""
        IL_006b:  ldarg.0
        IL_006c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
        IL_0071:  ldloca.s   V_2
        IL_0073:  ldarg.0
        IL_0074:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref Program.<Main>d__0)""
        IL_0079:  leave      IL_01a9
        IL_007e:  ldarg.0
        IL_007f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter Program.<Main>d__0.<>u__1""
        IL_0084:  stloc.2
        IL_0085:  ldarg.0
        IL_0086:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter Program.<Main>d__0.<>u__1""
        IL_008b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
        IL_0091:  ldarg.0
        IL_0092:  ldc.i4.m1
        IL_0093:  dup
        IL_0094:  stloc.0
        IL_0095:  stfld      ""int Program.<Main>d__0.<>1__state""
        IL_009a:  ldloca.s   V_2
        IL_009c:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
        IL_00a1:  ldarg.0
        IL_00a2:  ldfld      ""object Program.<Main>d__0.<>7__wrap3""
        IL_00a7:  stloc.1
        IL_00a8:  ldloc.1
        IL_00a9:  brfalse.s  IL_00c0
        IL_00ab:  ldloc.1
        IL_00ac:  isinst     ""System.Exception""
        IL_00b1:  dup
        IL_00b2:  brtrue.s   IL_00b6
        IL_00b4:  ldloc.1
        IL_00b5:  throw
        IL_00b6:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
        IL_00bb:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
        IL_00c0:  ldarg.0
        IL_00c1:  ldfld      ""int Program.<Main>d__0.<>7__wrap4""
        IL_00c6:  stloc.3
        IL_00c7:  ldloc.3
        IL_00c8:  ldc.i4.1
        IL_00c9:  bne.un.s   IL_00cd
        IL_00cb:  leave.s    IL_00db
        IL_00cd:  ldarg.0
        IL_00ce:  ldnull
        IL_00cf:  stfld      ""object Program.<Main>d__0.<>7__wrap3""
        IL_00d4:  leave.s    IL_00d9
      }
      catch object
      {
        IL_00d6:  pop
        IL_00d7:  leave.s    IL_00d9
      }
      IL_00d9:  leave.s    IL_00ee
      IL_00db:  ldarg.0
      IL_00dc:  ldc.i4.1
      IL_00dd:  stfld      ""int Program.<Main>d__0.<>7__wrap2""
      IL_00e2:  leave.s    IL_00ee
    }
    catch object
    {
      IL_00e4:  stloc.1
      IL_00e5:  ldarg.0
      IL_00e6:  ldloc.1
      IL_00e7:  stfld      ""object Program.<Main>d__0.<>7__wrap1""
      IL_00ec:  leave.s    IL_00ee
    }
    IL_00ee:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.CompletedTask.get""
    IL_00f3:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
    IL_00f8:  stloc.2
    IL_00f9:  ldloca.s   V_2
    IL_00fb:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
    IL_0100:  brtrue.s   IL_0141
    IL_0102:  ldarg.0
    IL_0103:  ldc.i4.1
    IL_0104:  dup
    IL_0105:  stloc.0
    IL_0106:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_010b:  ldarg.0
    IL_010c:  ldloc.2
    IL_010d:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter Program.<Main>d__0.<>u__1""
    IL_0112:  ldarg.0
    IL_0113:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_0118:  ldloca.s   V_2
    IL_011a:  ldarg.0
    IL_011b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref Program.<Main>d__0)""
    IL_0120:  leave      IL_01a9
    IL_0125:  ldarg.0
    IL_0126:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter Program.<Main>d__0.<>u__1""
    IL_012b:  stloc.2
    IL_012c:  ldarg.0
    IL_012d:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter Program.<Main>d__0.<>u__1""
    IL_0132:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
    IL_0138:  ldarg.0
    IL_0139:  ldc.i4.m1
    IL_013a:  dup
    IL_013b:  stloc.0
    IL_013c:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0141:  ldloca.s   V_2
    IL_0143:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
    IL_0148:  ldarg.0
    IL_0149:  ldfld      ""object Program.<Main>d__0.<>7__wrap1""
    IL_014e:  stloc.1
    IL_014f:  ldloc.1
    IL_0150:  brfalse.s  IL_0167
    IL_0152:  ldloc.1
    IL_0153:  isinst     ""System.Exception""
    IL_0158:  dup
    IL_0159:  brtrue.s   IL_015d
    IL_015b:  ldloc.1
    IL_015c:  throw
    IL_015d:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
    IL_0162:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
    IL_0167:  ldarg.0
    IL_0168:  ldfld      ""int Program.<Main>d__0.<>7__wrap2""
    IL_016d:  stloc.3
    IL_016e:  ldloc.3
    IL_016f:  ldc.i4.1
    IL_0170:  bne.un.s   IL_0174
    IL_0172:  leave.s    IL_0196
    IL_0174:  ldarg.0
    IL_0175:  ldnull
    IL_0176:  stfld      ""object Program.<Main>d__0.<>7__wrap1""
    IL_017b:  leave.s    IL_0196
  }
  catch System.Exception
  {
    IL_017d:  stloc.s    V_4
    IL_017f:  ldarg.0
    IL_0180:  ldc.i4.s   -2
    IL_0182:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_0187:  ldarg.0
    IL_0188:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_018d:  ldloc.s    V_4
    IL_018f:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_0194:  leave.s    IL_01a9
  }
  IL_0196:  ldarg.0
  IL_0197:  ldc.i4.s   -2
  IL_0199:  stfld      ""int Program.<Main>d__0.<>1__state""
  IL_019e:  ldarg.0
  IL_019f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
  IL_01a4:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_01a9:  ret
}");
        }

        [Fact]
        [WorkItem(34720, "https://github.com/dotnet/roslyn/issues/34720")]
        public void AwaitInFinallyInNestedTry_02()
        {
            string source =
@"using System.IO;
using System.Threading.Tasks;
class Program
{
    static async Task Main()
    {
        for (int i = 0; i < 5; i++)
        {
            using (new MemoryStream())
            {
                try
                {
                    continue;
                }
                finally
                {
                    await Task.Delay(1);
                }
            }
        }
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe.WithOptimizationLevel(OptimizationLevel.Release));
            var verifier = CompileAndVerify(comp, expectedOutput: "");
            verifier.VerifyIL("Program.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
@"{
  // Code size      320 (0x140)
  .maxstack  3
  .locals init (int V_0,
                object V_1,
                System.Runtime.CompilerServices.TaskAwaiter V_2,
                int V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Main>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0021
    IL_000a:  ldarg.0
    IL_000b:  ldc.i4.0
    IL_000c:  stfld      ""int Program.<Main>d__0.<i>5__2""
    IL_0011:  br         IL_0105
    IL_0016:  ldarg.0
    IL_0017:  newobj     ""System.IO.MemoryStream..ctor()""
    IL_001c:  stfld      ""System.IO.MemoryStream Program.<Main>d__0.<>7__wrap2""
    IL_0021:  nop
    .try
    {
      IL_0022:  ldloc.0
      IL_0023:  brfalse.s  IL_007e
      IL_0025:  ldarg.0
      IL_0026:  ldnull
      IL_0027:  stfld      ""object Program.<Main>d__0.<>7__wrap3""
      IL_002c:  ldarg.0
      IL_002d:  ldc.i4.0
      IL_002e:  stfld      ""int Program.<Main>d__0.<>7__wrap4""
      .try
      {
        IL_0033:  ldarg.0
        IL_0034:  ldc.i4.1
        IL_0035:  stfld      ""int Program.<Main>d__0.<>7__wrap4""
        IL_003a:  leave.s    IL_0046
      }
      catch object
      {
        IL_003c:  stloc.1
        IL_003d:  ldarg.0
        IL_003e:  ldloc.1
        IL_003f:  stfld      ""object Program.<Main>d__0.<>7__wrap3""
        IL_0044:  leave.s    IL_0046
      }
      IL_0046:  ldc.i4.1
      IL_0047:  call       ""System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(int)""
      IL_004c:  callvirt   ""System.Runtime.CompilerServices.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()""
      IL_0051:  stloc.2
      IL_0052:  ldloca.s   V_2
      IL_0054:  call       ""bool System.Runtime.CompilerServices.TaskAwaiter.IsCompleted.get""
      IL_0059:  brtrue.s   IL_009a
      IL_005b:  ldarg.0
      IL_005c:  ldc.i4.0
      IL_005d:  dup
      IL_005e:  stloc.0
      IL_005f:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_0064:  ldarg.0
      IL_0065:  ldloc.2
      IL_0066:  stfld      ""System.Runtime.CompilerServices.TaskAwaiter Program.<Main>d__0.<>u__1""
      IL_006b:  ldarg.0
      IL_006c:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
      IL_0071:  ldloca.s   V_2
      IL_0073:  ldarg.0
      IL_0074:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter, Program.<Main>d__0>(ref System.Runtime.CompilerServices.TaskAwaiter, ref Program.<Main>d__0)""
      IL_0079:  leave      IL_013f
      IL_007e:  ldarg.0
      IL_007f:  ldfld      ""System.Runtime.CompilerServices.TaskAwaiter Program.<Main>d__0.<>u__1""
      IL_0084:  stloc.2
      IL_0085:  ldarg.0
      IL_0086:  ldflda     ""System.Runtime.CompilerServices.TaskAwaiter Program.<Main>d__0.<>u__1""
      IL_008b:  initobj    ""System.Runtime.CompilerServices.TaskAwaiter""
      IL_0091:  ldarg.0
      IL_0092:  ldc.i4.m1
      IL_0093:  dup
      IL_0094:  stloc.0
      IL_0095:  stfld      ""int Program.<Main>d__0.<>1__state""
      IL_009a:  ldloca.s   V_2
      IL_009c:  call       ""void System.Runtime.CompilerServices.TaskAwaiter.GetResult()""
      IL_00a1:  ldarg.0
      IL_00a2:  ldfld      ""object Program.<Main>d__0.<>7__wrap3""
      IL_00a7:  stloc.1
      IL_00a8:  ldloc.1
      IL_00a9:  brfalse.s  IL_00c0
      IL_00ab:  ldloc.1
      IL_00ac:  isinst     ""System.Exception""
      IL_00b1:  dup
      IL_00b2:  brtrue.s   IL_00b6
      IL_00b4:  ldloc.1
      IL_00b5:  throw
      IL_00b6:  call       ""System.Runtime.ExceptionServices.ExceptionDispatchInfo System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(System.Exception)""
      IL_00bb:  callvirt   ""void System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw()""
      IL_00c0:  ldarg.0
      IL_00c1:  ldfld      ""int Program.<Main>d__0.<>7__wrap4""
      IL_00c6:  stloc.3
      IL_00c7:  ldloc.3
      IL_00c8:  ldc.i4.1
      IL_00c9:  bne.un.s   IL_00cd
      IL_00cb:  leave.s    IL_00f5
      IL_00cd:  ldarg.0
      IL_00ce:  ldnull
      IL_00cf:  stfld      ""object Program.<Main>d__0.<>7__wrap3""
      IL_00d4:  leave.s    IL_00ee
    }
    finally
    {
      IL_00d6:  ldloc.0
      IL_00d7:  ldc.i4.0
      IL_00d8:  bge.s      IL_00ed
      IL_00da:  ldarg.0
      IL_00db:  ldfld      ""System.IO.MemoryStream Program.<Main>d__0.<>7__wrap2""
      IL_00e0:  brfalse.s  IL_00ed
      IL_00e2:  ldarg.0
      IL_00e3:  ldfld      ""System.IO.MemoryStream Program.<Main>d__0.<>7__wrap2""
      IL_00e8:  callvirt   ""void System.IDisposable.Dispose()""
      IL_00ed:  endfinally
    }
    IL_00ee:  ldarg.0
    IL_00ef:  ldnull
    IL_00f0:  stfld      ""System.IO.MemoryStream Program.<Main>d__0.<>7__wrap2""
    IL_00f5:  ldarg.0
    IL_00f6:  ldfld      ""int Program.<Main>d__0.<i>5__2""
    IL_00fb:  stloc.3
    IL_00fc:  ldarg.0
    IL_00fd:  ldloc.3
    IL_00fe:  ldc.i4.1
    IL_00ff:  add
    IL_0100:  stfld      ""int Program.<Main>d__0.<i>5__2""
    IL_0105:  ldarg.0
    IL_0106:  ldfld      ""int Program.<Main>d__0.<i>5__2""
    IL_010b:  ldc.i4.5
    IL_010c:  blt        IL_0016
    IL_0111:  leave.s    IL_012c
  }
  catch System.Exception
  {
    IL_0113:  stloc.s    V_4
    IL_0115:  ldarg.0
    IL_0116:  ldc.i4.s   -2
    IL_0118:  stfld      ""int Program.<Main>d__0.<>1__state""
    IL_011d:  ldarg.0
    IL_011e:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
    IL_0123:  ldloc.s    V_4
    IL_0125:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_012a:  leave.s    IL_013f
  }
  IL_012c:  ldarg.0
  IL_012d:  ldc.i4.s   -2
  IL_012f:  stfld      ""int Program.<Main>d__0.<>1__state""
  IL_0134:  ldarg.0
  IL_0135:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Main>d__0.<>t__builder""
  IL_013a:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_013f:  ret
}");
        }

        [Fact]
        public void ValueTask()
        {
            var source =
@"using System.Threading.Tasks;
class Program
{
    static async ValueTask Main()
    {
        await Task.Delay(0);
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1),
                // (4,28): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static async ValueTask Main()
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(4, 28));
        }

        [Fact]
        public void ValueTaskOfInt()
        {
            var source =
@"using System.Threading.Tasks;
class Program
{
    static async ValueTask<int> Main()
    {
        await Task.Delay(0);
        return 0;
    }
}";
            var comp = CreateCompilationWithTasksExtensions(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1),
                // (4,33): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static async ValueTask<int> Main()
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(4, 33));
        }

        [Fact]
        public void TasklikeType()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
namespace System.Runtime.CompilerServices
{
    class AsyncMethodBuilderAttribute : System.Attribute
    {
        public AsyncMethodBuilderAttribute(System.Type t) { }
    }
}
[AsyncMethodBuilder(typeof(MyTaskMethodBuilder))]
struct MyTask
{
    internal Awaiter GetAwaiter() => new Awaiter();
    internal class Awaiter : INotifyCompletion
    {
        public void OnCompleted(Action a) { }
        internal bool IsCompleted => true;
        internal void GetResult() { }
    }
}
struct MyTaskMethodBuilder
{
    private MyTask _task;
    public static MyTaskMethodBuilder Create() => new MyTaskMethodBuilder(new MyTask());
    internal MyTaskMethodBuilder(MyTask task)
    {
        _task = task;
    }
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    {
        stateMachine.MoveNext();
    }
    public void SetException(Exception e) { }
    public void SetResult() { }
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    public MyTask Task => _task;
}
class Program
{
    static async MyTask Main()
    {
        await Task.Delay(0);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                Diagnostic(ErrorCode.ERR_NoEntryPoint).WithLocation(1, 1),
                // (43,25): warning CS0028: 'Program.Main()' has the wrong signature to be an entry point
                //     static async MyTask Main()
                Diagnostic(ErrorCode.WRN_InvalidMainSig, "Main").WithArguments("Program.Main()").WithLocation(43, 25));
        }

        private const string MinimalAsyncCorelibWithAsyncHelpers = """
            namespace System
            {
                public class Object { }
                public class Void { }
                public abstract class ValueType { }
                public struct Int32 { }
                public struct Boolean { }
                public class String { }
                public class Exception { }
                public class Attribute { }
                public abstract class Enum : ValueType { }
                public abstract class Delegate { }
                public abstract class MulticastDelegate : Delegate { }
                public delegate void Action();
                public class Type { }
                public struct IntPtr { }
                public struct RuntimeTypeHandle { }
                public struct RuntimeFieldHandle { }

                namespace Threading.Tasks
                {
                    public class Task
                    {
                        public TaskAwaiter GetAwaiter() => default;
                    }

                    public struct TaskAwaiter : Runtime.CompilerServices.INotifyCompletion
                    {
                        public bool IsCompleted => true;
                        public void GetResult() { }
                        public void OnCompleted(Action continuation) { }
                    }

                    public class Task<T>
                    {
                        public TaskAwaiter<T> GetAwaiter() => default;
                    }

                    public struct TaskAwaiter<T> : Runtime.CompilerServices.INotifyCompletion
                    {
                        public bool IsCompleted => true;
                        public T GetResult() => default;
                        public void OnCompleted(Action continuation) { }
                    }
                }

                namespace Runtime.CompilerServices
                {
                    public interface INotifyCompletion
                    {
                        void OnCompleted(Action continuation);
                    }

                    public interface IAsyncStateMachine
                    {
                        void MoveNext();
                        void SetStateMachine(IAsyncStateMachine stateMachine);
                    }

                    public class AsyncTaskMethodBuilder
                    {
                        public static AsyncTaskMethodBuilder Create() => default;
                        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
                        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
                        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) 
                            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
                        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
                        public void SetResult() { }
                        public void SetException(Exception exception) { }
                        public Threading.Tasks.Task Task => default;
                    }

                    public class AsyncTaskMethodBuilder<T>
                    {
                        public static AsyncTaskMethodBuilder<T> Create() => default;
                        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
                        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
                        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) 
                            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
                        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                            where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
                        public void SetResult(T result) { }
                        public void SetException(Exception exception) { }
                        public Threading.Tasks.Task<T> Task => default;
                    }

                    public static class AsyncHelpers
                    {
                        public static void HandleAsyncEntryPoint(Threading.Tasks.Task task)
                        {
                            task.GetAwaiter().GetResult();
                        }

                        public static int HandleAsyncEntryPoint(Threading.Tasks.Task<int> task)
                        {
                            return task.GetAwaiter().GetResult();
                        }
                    }
                }

                public enum AttributeTargets { All = ~0 }

                [AttributeUsage(AttributeTargets.All)]
                public sealed class AttributeUsageAttribute : Attribute
                {
                    public AttributeUsageAttribute(AttributeTargets validOn) { }
                    public bool AllowMultiple { get; set; }
                    public bool Inherited { get; set; }
                }
            }

            namespace System.Diagnostics
            {
                public class DebuggerStepThroughAttribute : Attribute { }
            }
            """;

        [Fact]
        public void AsyncMainWithHandleAsyncEntryPoint_Task()
        {
            var source = """
                using System.Threading.Tasks;

                class Program
                {
                    static async Task Main()
                    {
                        await new Task();
                    }
                }
                """;

            var corlibRef = CreateEmptyCompilation(MinimalAsyncCorelibWithAsyncHelpers, assemblyName: "mincorlib").EmitToImageReference();
            var comp = CreateEmptyCompilation(source, references: new[] { corlibRef }, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
            verifier.VerifyIL("Program.<Main>()", """
                {
                  // Code size       11 (0xb)
                  .maxstack  1
                  IL_0000:  call       "System.Threading.Tasks.Task Program.Main()"
                  IL_0005:  call       "void System.Runtime.CompilerServices.AsyncHelpers.HandleAsyncEntryPoint(System.Threading.Tasks.Task)"
                  IL_000a:  ret
                }
                """);
        }

        [Fact]
        public void AsyncMainWithHandleAsyncEntryPoint_TaskOfInt()
        {
            var source = """
                using System.Threading.Tasks;

                class Program
                {
                    static async Task<int> Main()
                    {
                        await new Task<int>();
                        return 42;
                    }
                }
                """;

            var corlibRef = CreateEmptyCompilation(MinimalAsyncCorelibWithAsyncHelpers, assemblyName: "mincorlib").EmitToImageReference();
            var comp = CreateEmptyCompilation(source, references: new[] { corlibRef }, options: TestOptions.DebugExe);

            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
            verifier.VerifyIL("Program.<Main>()", """
                {
                  // Code size       11 (0xb)
                  .maxstack  1
                  IL_0000:  call       "System.Threading.Tasks.Task<int> Program.Main()"
                  IL_0005:  call       "int System.Runtime.CompilerServices.AsyncHelpers.HandleAsyncEntryPoint(System.Threading.Tasks.Task<int>)"
                  IL_000a:  ret
                }
                """);
        }

        [Fact]
        public void AsyncMainFallbackToOldPattern_Task()
        {
            var source = """
                using System.Threading.Tasks;

                class Program
                {
                    static async Task Main()
                    {
                        await new Task();
                    }
                }
                """;

            var corlibRef = CreateEmptyCompilation(MinimalAsyncCorelibWithAsyncHelpers, assemblyName: "mincorlib").EmitToImageReference();
            var comp = CreateEmptyCompilation(source, references: new[] { corlibRef }, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__HandleAsyncEntryPoint_Task);

            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
            // Verify it falls back to GetAwaiter().GetResult() pattern
            verifier.VerifyIL("Program.<Main>()", """
                {
                  // Code size       19 (0x13)
                  .maxstack  1
                  .locals init (System.Threading.Tasks.TaskAwaiter V_0)
                  IL_0000:  call       "System.Threading.Tasks.Task Program.Main()"
                  IL_0005:  callvirt   "System.Threading.Tasks.TaskAwaiter System.Threading.Tasks.Task.GetAwaiter()"
                  IL_000a:  stloc.0
                  IL_000b:  ldloca.s   V_0
                  IL_000d:  call       "void System.Threading.Tasks.TaskAwaiter.GetResult()"
                  IL_0012:  ret
                }
                """);
        }

        [Fact]
        public void AsyncMainFallbackToOldPattern_TaskOfInt()
        {
            var source = """
                using System.Threading.Tasks;

                class Program
                {
                    static async Task<int> Main()
                    {
                        await new Task<int>();
                        return 42;
                    }
                }
                """;

            var corlibRef = CreateEmptyCompilation(MinimalAsyncCorelibWithAsyncHelpers, assemblyName: "mincorlib").EmitToImageReference();
            var comp = CreateEmptyCompilation(source, references: new[] { corlibRef }, options: TestOptions.DebugExe);
            comp.MakeMemberMissing(SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__HandleAsyncEntryPoint_Task_Int32);

            var verifier = CompileAndVerify(comp, verify: Verification.Skipped);
            // Verify it falls back to GetAwaiter().GetResult() pattern
            verifier.VerifyIL("Program.<Main>()", """
                {
                  // Code size       19 (0x13)
                  .maxstack  1
                  .locals init (System.Threading.Tasks.TaskAwaiter<int> V_0)
                  IL_0000:  call       "System.Threading.Tasks.Task<int> Program.Main()"
                  IL_0005:  callvirt   "System.Threading.Tasks.TaskAwaiter<int> System.Threading.Tasks.Task<int>.GetAwaiter()"
                  IL_000a:  stloc.0
                  IL_000b:  ldloca.s   V_0
                  IL_000d:  call       "int System.Threading.Tasks.TaskAwaiter<int>.GetResult()"
                  IL_0012:  ret
                }
                """);
        }
    }
}
