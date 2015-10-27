// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.CSharp.Interactive;
using Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Traits = Roslyn.Test.Utilities.Traits;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    [Trait(Traits.Feature, Traits.Features.InteractiveHost)]
    public sealed class InteractiveHostTests : AbstractInteractiveHostTests
    {
        #region Utils

        private SynchronizedStringWriter _synchronizedOutput;
        private SynchronizedStringWriter _synchronizedErrorOutput;
        private int[] _outputReadPosition = new int[] { 0, 0 };

        private readonly InteractiveHost Host;

        private static readonly string s_fxDir = FileUtilities.NormalizeDirectoryPath(RuntimeEnvironment.GetRuntimeDirectory());
        private static readonly string s_homeDir = FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        public InteractiveHostTests()
        {
            Host = new InteractiveHost(typeof(CSharpReplServiceProvider), GetInteractiveHostPath(), ".", millisecondsTimeout: -1);

            RedirectOutput();

            Host.ResetAsync(new InteractiveHostOptions(initializationFile: null, culture: CultureInfo.InvariantCulture)).Wait();

            var remoteService = Host.TryGetService();
            Assert.NotNull(remoteService);

            remoteService.SetTestObjectFormattingOptions();

            Host.SetPathsAsync(new[] { s_fxDir }, new[] { s_homeDir }, s_homeDir).Wait();

            // assert and remove logo:
            var output = SplitLines(ReadOutputToEnd());
            var errorOutput = ReadErrorOutputToEnd();

            Assert.Equal("", errorOutput);
            Assert.Equal(2, output.Length);
            Assert.Equal("Microsoft (R) Roslyn C# Compiler version " + FileVersionInfo.GetVersionInfo(Host.GetType().Assembly.Location).FileVersion, output[0]);
            // "Type "#help" for more information."
            Assert.Equal(FeaturesResources.TypeHelpForMoreInformation, output[1]);

            // remove logo:
            ClearOutput();
        }

        public override void Dispose()
        {
            try
            {
                Process process = Host.TryGetProcess();

                DisposeInteractiveHostProcess(Host);

                // the process should be terminated 
                if (process != null && !process.HasExited)
                {
                    process.WaitForExit();
                }
            }
            finally
            {
                // Dispose temp files only after the InteractiveHost exits, 
                // so that assemblies are unloaded.
                base.Dispose();
            }
        }

        private void RedirectOutput()
        {
            _synchronizedOutput = new SynchronizedStringWriter();
            _synchronizedErrorOutput = new SynchronizedStringWriter();
            ClearOutput();
            Host.Output = _synchronizedOutput;
            Host.ErrorOutput = _synchronizedErrorOutput;
        }

        private bool LoadReference(string reference)
        {
            return Execute($"#r \"{reference}\"");
        }

        private bool Execute(string code)
        {
            var task = Host.ExecuteAsync(code);
            task.Wait();
            return task.Result.Success;
        }

        private bool IsShadowCopy(string path)
        {
            return Host.TryGetService().IsShadowCopy(path);
        }

        public string ReadErrorOutputToEnd()
        {
            return ReadOutputToEnd(isError: true);
        }

        public void ClearOutput()
        {
            _outputReadPosition = new int[] { 0, 0 };
            _synchronizedOutput.Clear();
            _synchronizedErrorOutput.Clear();
        }

        public void RestartHost(string rspFile = null)
        {
            ClearOutput();

            var initTask = Host.ResetAsync(new InteractiveHostOptions(initializationFile: rspFile, culture: CultureInfo.InvariantCulture));
            initTask.Wait();
        }

        public string ReadOutputToEnd(bool isError = false)
        {
            var writer = isError ? _synchronizedErrorOutput : _synchronizedOutput;
            var markPrefix = '\uFFFF';
            var mark = markPrefix + Guid.NewGuid().ToString();

            // writes mark to the STDOUT/STDERR pipe in the remote process:
            Host.TryGetService().RemoteConsoleWrite(Encoding.UTF8.GetBytes(mark), isError);

            while (true)
            {
                var data = writer.Prefix(mark, ref _outputReadPosition[isError ? 0 : 1]);
                if (data != null)
                {
                    return data;
                }

                Thread.Sleep(10);
            }
        }

        private class CompiledFile
        {
            public string Path;
            public ImmutableArray<byte> Image;
        }

        private static CompiledFile CompileLibrary(TempDirectory dir, string fileName, string assemblyName, string source, params MetadataReference[] references)
        {
            var file = dir.CreateFile(fileName);
            var compilation = CreateCompilation(
                new[] { source },
                assemblyName: assemblyName,
                references: references.Concat(new[] { MetadataReference.CreateFromAssemblyInternal(typeof(object).Assembly) }),
                options: fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? TestOptions.ReleaseExe : TestOptions.ReleaseDll);

            var image = compilation.EmitToArray();
            file.WriteAllBytes(image);

            return new CompiledFile { Path = file.Path, Image = image };
        }

        #endregion

        [Fact]
        public void OutputRedirection()
        {
            Execute(@"
System.Console.WriteLine(""hello-\u4567!""); 
System.Console.Error.WriteLine(""error-\u7890!""); 
1+1
            ");

            var output = ReadOutputToEnd();
            var error = ReadErrorOutputToEnd();

            Assert.Equal("hello-\u4567!\r\n2\r\n", output);
            Assert.Equal("error-\u7890!\r\n", error);
        }

        [Fact]
        public void OutputRedirection2()
        {
            Execute(@"System.Console.WriteLine(1);");
            Execute(@"System.Console.Error.WriteLine(2);");

            var output = ReadOutputToEnd();
            var error = ReadErrorOutputToEnd();

            Assert.Equal("1\r\n", output);
            Assert.Equal("2\r\n", error);

            RedirectOutput();

            Execute(@"System.Console.WriteLine(3);");
            Execute(@"System.Console.Error.WriteLine(4);");

            output = ReadOutputToEnd();
            error = ReadErrorOutputToEnd();

            Assert.Equal("3\r\n", output);
            Assert.Equal("4\r\n", error);
        }

        [Fact]
        public void StackOverflow()
        {
            // Windows Server 2008 (OS v6.0), Vista (OS v6.0) and XP (OS v5.1) ignores SetErrorMode and shows crash dialog, which would hang the test:
            if (Environment.OSVersion.Version < new Version(6, 1, 0, 0))
            {
                return;
            }

            Execute(@"
int foo(int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9) 
{ 
    return foo(0,1,2,3,4,5,6,7,8,9) + foo(0,1,2,3,4,5,6,7,8,9); 
} 
foo(0,1,2,3,4,5,6,7,8,9)
            ");

            Assert.Equal("", ReadOutputToEnd());
            // Hosting process exited with exit code -1073741571.
            Assert.Equal("Process is terminated due to StackOverflowException.\n" + string.Format(FeaturesResources.HostingProcessExitedWithExitCode, -1073741571), ReadErrorOutputToEnd().Trim());

            Execute(@"1+1");

            Assert.Equal("2\r\n", ReadOutputToEnd().ToString());
        }

        private const string MethodWithInfiniteLoop = @"
void foo() 
{ 
    int i = 0;
    while (true) 
    { 
        if (i < 10) 
        {
            i = i + 1;
        }
        else if (i == 10)
        {
            System.Console.Error.WriteLine(""in the loop"");
            i = i + 1;
        }
    } 
}
";

        [Fact]
        public void AsyncExecute_InfiniteLoop()
        {
            var mayTerminate = new ManualResetEvent(false);
            Host.ErrorOutputReceived += (_, __) => mayTerminate.Set();

            var executeTask = Host.ExecuteAsync(MethodWithInfiniteLoop + "\r\nfoo()");
            Assert.True(mayTerminate.WaitOne());

            RestartHost();

            executeTask.Wait();

            Assert.True(Execute(@"1+1"));
            Assert.Equal("2\r\n", ReadOutputToEnd());
        }

        [Fact(Skip = "529027")]
        public void AsyncExecute_HangingForegroundThreads()
        {
            var mayTerminate = new ManualResetEvent(false);
            Host.OutputReceived += (_, __) =>
            {
                mayTerminate.Set();
            };

            var executeTask = Host.ExecuteAsync(@"
using System.Threading;

int i1 = 0;
Thread t1 = new Thread(() => { while(true) { i1++; } });
t1.Name = ""TestThread-1"";
t1.IsBackground = false;
t1.Start();

int i2 = 0;
Thread t2 = new Thread(() => { while(true) { i2++; } });
t2.Name = ""TestThread-2"";
t2.IsBackground = true;
t2.Start();

Thread t3 = new Thread(() => Thread.Sleep(Timeout.Infinite));
t3.Name = ""TestThread-3"";
t3.Start();

while (i1 < 2 || i2 < 2 || t3.ThreadState != System.Threading.ThreadState.WaitSleepJoin) { }

System.Console.WriteLine(""terminate!"");

while(true) {}
");
            Assert.Equal("", ReadErrorOutputToEnd());

            Assert.True(mayTerminate.WaitOne());

            var service = Host.TryGetService();
            Assert.NotNull(service);

            var process = Host.TryGetProcess();
            Assert.NotNull(process);

            service.EmulateClientExit();

            // the process should terminate with exit code 0:
            process.WaitForExit();
            Assert.Equal(0, process.ExitCode);
        }

        [Fact]
        public void AsyncExecuteFile_InfiniteLoop()
        {
            var file = Temp.CreateFile().WriteAllText(MethodWithInfiniteLoop + "\r\nfoo();").Path;

            var mayTerminate = new ManualResetEvent(false);
            Host.ErrorOutputReceived += (_, __) => mayTerminate.Set();

            var executeTask = Host.ExecuteFileAsync(file);
            mayTerminate.WaitOne();

            RestartHost();

            executeTask.Wait();

            Assert.True(Execute(@"1+1"));
            Assert.Equal("2\r\n", ReadOutputToEnd());
        }

        [Fact]
        public void AsyncExecuteFile_SourceKind()
        {
            var file = Temp.CreateFile().WriteAllText("1 1").Path;
            var task = Host.ExecuteFileAsync(file);
            task.Wait();
            Assert.False(task.Result.Success);

            var errorOut = ReadErrorOutputToEnd().Trim();
            Assert.True(errorOut.StartsWith(file + "(1,3):", StringComparison.Ordinal), "Error output should start with file name, line and column");
            Assert.True(errorOut.Contains("CS1002"), "Error output should include error CS1002");
        }

        [Fact]
        public void AsyncExecuteFile_NonExistingFile()
        {
            var task = Host.ExecuteFileAsync("non existing file");
            task.Wait();
            Assert.False(task.Result.Success);

            var errorOut = ReadErrorOutputToEnd().Trim();
            Assert.Contains(FeaturesResources.SpecifiedFileNotFound, errorOut, StringComparison.Ordinal);
            Assert.Contains(FeaturesResources.SearchedInDirectory, errorOut, StringComparison.Ordinal);
        }

        [Fact]
        public void AsyncExecuteFile()
        {
            var file = Temp.CreateFile().WriteAllText(@"
using static System.Console;

public class C 
{ 
   public int field = 4; 
   public int Foo(int i) { return i; } 
}

public int Foo(int i) { return i; }

WriteLine(5);
").Path;
            var task = Host.ExecuteFileAsync(file);
            task.Wait();

            Assert.True(task.Result.Success);
            Assert.Equal("5", ReadOutputToEnd().Trim());

            Execute("Foo(2)");
            Assert.Equal("2", ReadOutputToEnd().Trim());

            Execute("new C().Foo(3)");
            Assert.Equal("3", ReadOutputToEnd().Trim());

            Execute("new C().field");
            Assert.Equal("4", ReadOutputToEnd().Trim());
        }

        [Fact]
        public void AsyncExecuteFile_InvalidFileContent()
        {
            var executeTask = Host.ExecuteFileAsync(typeof(Process).Assembly.Location);

            executeTask.Wait();

            var errorOut = ReadErrorOutputToEnd().Trim();
            Assert.True(errorOut.StartsWith(typeof(Process).Assembly.Location + "(1,3):", StringComparison.Ordinal), "Error output should start with file name, line and column");
            Assert.True(errorOut.Contains("CS1056"), "Error output should include error CS1056");
            Assert.True(errorOut.Contains("CS1002"), "Error output should include error CS1002");
        }

        [Fact]
        public void AsyncExecuteFile_ScriptFileWithBuildErrors()
        {
            var file = Temp.CreateFile().WriteAllText("#load blah.csx" + "\r\n" + "class C {}");

            Host.ExecuteFileAsync(file.Path).Wait();

            var errorOut = ReadErrorOutputToEnd().Trim();
            Assert.True(errorOut.StartsWith(file.Path + "(1,7):", StringComparison.Ordinal), "Error output should start with file name, line and column");
            Assert.True(errorOut.Contains("CS7010"), "Error output should include error CS7010");
        }

        /// <summary>
        /// Check that the assembly resolve event doesn't cause any harm. It shouldn't actually be
        /// even invoked since we resolve the assembly via Fusion.
        /// </summary>
        [Fact(Skip = "987032")]
        public void UserDefinedAssemblyResolve_InfiniteLoop()
        {
            var mayTerminate = new ManualResetEvent(false);
            Host.ErrorOutputReceived += (_, __) => mayTerminate.Set();

            Host.TryGetService().HookMaliciousAssemblyResolve();
            var executeTask = Host.AddReferenceAsync("nonexistingassembly" + Guid.NewGuid());

            Assert.True(mayTerminate.WaitOne());
            executeTask.Wait();

            Assert.True(Execute(@"1+1"));

            var output = ReadOutputToEnd();
            Assert.Equal("2\r\n", output);
        }

        [Fact]
        public void AddReference_Path()
        {
            Assert.False(Execute("new System.Data.DataSet()"));
            Assert.True(LoadReference(Assembly.Load(new AssemblyName("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")).Location));
            Assert.True(Execute("new System.Data.DataSet()"));
        }

        [Fact]
        public void AddReference_PartialName()
        {
            Assert.False(Execute("new System.Data.DataSet()"));
            Assert.True(LoadReference("System.Data"));
            Assert.True(Execute("new System.Data.DataSet()"));
        }

        [Fact]
        public void AddReference_PartialName_LatestVersion()
        {
            // there might be two versions of System.Data - v2 and v4, we should get the latter:
            Assert.True(LoadReference("System.Data"));
            Assert.True(LoadReference("System"));
            Assert.True(LoadReference("System.Xml"));
            Execute(@"new System.Data.DataSet().GetType().Assembly.GetName().Version");
            var output = ReadOutputToEnd();
            Assert.Equal("[4.0.0.0]\r\n", output);
        }

        [Fact]
        public void AddReference_FullName()
        {
            Assert.False(Execute("new System.Data.DataSet()"));
            Assert.True(LoadReference("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
            Assert.True(Execute("new System.Data.DataSet()"));
        }
                
        [ConditionalFact(typeof(Framework35Installed), Skip="https://github.com/dotnet/roslyn/issues/5167")]
        public void AddReference_VersionUnification1()
        {
            // V3.5 unifies with the current Framework version:
            var result = LoadReference("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            Assert.Equal("", ReadErrorOutputToEnd().Trim());
            Assert.Equal("", ReadOutputToEnd().Trim());
            Assert.True(result);

            result = LoadReference("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            Assert.Equal("", ReadErrorOutputToEnd().Trim());
            Assert.Equal("", ReadOutputToEnd().Trim());
            Assert.True(result);

            result = LoadReference("System.Core");
            Assert.Equal("", ReadErrorOutputToEnd().Trim());
            Assert.Equal("", ReadOutputToEnd().Trim());
            Assert.True(result);
        }

        [Fact]
        public void AddReference_AssemblyAlreadyLoaded()
        {
            var result = LoadReference("System.Core");
            Assert.Equal("", ReadErrorOutputToEnd().Trim());
            Assert.Equal("", ReadOutputToEnd().Trim());
            Assert.True(result);

            result = LoadReference("System.Core.dll");
            Assert.Equal("", ReadErrorOutputToEnd().Trim());
            Assert.Equal("", ReadOutputToEnd().Trim());
            Assert.True(result);
        }

        // Caused by submission not inheriting references.
        [Fact(Skip = "101161")]
        public void AddReference_ShadowCopy()
        {
            var dir = Temp.CreateDirectory();

            // create C.dll
            var c = CompileLibrary(dir, "c.dll", "c", @"public class C { }");

            // load C.dll: 
            Assert.True(LoadReference(c.Path));
            Assert.True(Execute("new C()"));
            Assert.Equal("C { }", ReadOutputToEnd().Trim());

            // rewrite C.dll:
            File.WriteAllBytes(c.Path, new byte[] { 1, 2, 3 });

            // we can still run code:
            var result = Execute("new C()");
            Assert.Equal("", ReadErrorOutputToEnd().Trim());
            Assert.Equal("C { }", ReadOutputToEnd().Trim());
            Assert.True(result);
        }
#if TODO
        /// <summary>
        /// Tests that a dependency is correctly resolved and loaded at runtime.
        /// A depends on B, which depends on C. When CallB is jitted B is loaded. When CallC is jitted C is loaded.
        /// </summary>
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/860")]
        public void AddReference_Dependencies()
        {
            var dir = Temp.CreateDirectory();

            var c = CompileLibrary(dir, "c.dll", "c", @"public class C { }");
            var b = CompileLibrary(dir, "b.dll", "b", @"public class B { public static int CallC() { new C(); return 1; } }", MetadataReference.CreateFromImage(c.Image));
            var a = CompileLibrary(dir, "a.dll", "a", @"public class A { public static int CallB() { B.CallC(); return 1; } }", MetadataReference.CreateFromImage(b.Image));

            AssemblyLoadResult result;

            result = LoadReference(a.Path);
            Assert.Equal(a.Path, result.OriginalPath);
            Assert.True(IsShadowCopy(result.Path));
            Assert.True(result.IsSuccessful);

            Assert.True(Execute("A.CallB()"));

            // c.dll is loaded as a dependency, so #r should be successful:
            result = LoadReference(c.Path);
            Assert.Equal(c.Path, result.OriginalPath);
            Assert.True(IsShadowCopy(result.Path));
            Assert.True(result.IsSuccessful);

            // c.dll was already loaded explicitly via #r so we should fail now:
            result = LoadReference(c.Path);
            Assert.False(result.IsSuccessful);
            Assert.Equal(c.Path, result.OriginalPath);
            Assert.True(IsShadowCopy(result.Path));

            Assert.Equal("", ReadErrorOutputToEnd().Trim());
            Assert.Equal("1", ReadOutputToEnd().Trim());
        }
#endif
        /// <summary>
        /// When two files of the same version are in the same directory, prefer .dll over .exe.
        /// </summary>
        [Fact]
        public void AddReference_Dependencies_DllExe()
        {
            var dir = Temp.CreateDirectory();

            var dll = CompileLibrary(dir, "c.dll", "C", @"public class C { public static int Main() { return 1; } }");
            var exe = CompileLibrary(dir, "c.exe", "C", @"public class C { public static int Main() { return 2; } }");

            var main = CompileLibrary(dir, "main.exe", "Main", @"public class Program { public static int Main() { return C.Main(); } }",
                MetadataReference.CreateFromImage(dll.Image));

            Assert.True(LoadReference(main.Path));
            Assert.True(Execute("Program.Main()"));

            Assert.Equal("", ReadErrorOutputToEnd().Trim());
            Assert.Equal("1", ReadOutputToEnd().Trim());
        }

        [Fact]
        public void AddReference_Dependencies_Versions()
        {
            var dir1 = Temp.CreateDirectory();
            var dir2 = Temp.CreateDirectory();
            var dir3 = Temp.CreateDirectory();

            // [assembly:AssemblyVersion("1.0.0.0")] public class C { public static int Main() { return 1; } }");
            var file1 = dir1.CreateFile("c.dll").WriteAllBytes(TestResources.General.C1);

            // [assembly:AssemblyVersion("2.0.0.0")] public class C { public static int Main() { return 2; } }");
            var file2 = dir2.CreateFile("c.dll").WriteAllBytes(TestResources.General.C2);

            Assert.True(LoadReference(file1.Path));
            Assert.True(LoadReference(file2.Path));

            var main = CompileLibrary(dir3, "main.exe", "Main", @"public class Program { public static int Main() { return C.Main(); } }",
                MetadataReference.CreateFromImage(TestResources.General.C2.AsImmutableOrNull()));

            Assert.True(LoadReference(main.Path));
            Assert.True(Execute("Program.Main()"));

            Assert.Equal("", ReadErrorOutputToEnd().Trim());
            Assert.Equal("2", ReadOutputToEnd().Trim());
        }

        [Fact]
        public void AddReference_AlreadyLoadedDependencies()
        {
            var dir = Temp.CreateDirectory();

            var lib1 = CompileLibrary(dir, "lib1.dll", "lib1", @"public interface I { int M(); }");
            var lib2 = CompileLibrary(dir, "lib2.dll", "lib2", @"public class C : I { public int M() { return 1; } }",
                MetadataReference.CreateFromFile(lib1.Path));

            Execute("#r \"" + lib1.Path + "\"");
            Execute("#r \"" + lib2.Path + "\"");
            Execute("new C().M()");

            Assert.Equal("", ReadErrorOutputToEnd().Trim());
            Assert.Equal("1", ReadOutputToEnd().Trim());
        }

        [Fact(Skip = "101161")]
        public void AddReference_LoadUpdatedReference()
        {
            var dir = Temp.CreateDirectory();

            var source1 = "public class C { public int X = 1; }";
            var c1 = CreateCompilationWithMscorlib(source1, assemblyName: "C");
            var file = dir.CreateFile("c.dll").WriteAllBytes(c1.EmitToArray());

            // use:
            Execute($@"
#r ""{file.Path}""
C foo() => new C();
new C().X
");

            // update:
            var source2 = "public class D { public int Y = 2; }";
            var c2 = CreateCompilationWithMscorlib(source2, assemblyName: "C");
            file.WriteAllBytes(c2.EmitToArray());

            // add the reference again:
            Execute($@"
#r ""{file.Path}""

new D().Y
");
            // TODO: We should report an error that assembly named 'a' was already loaded with different content.
            // In future we can let it load and improve error reporting around type conversions.

            Assert.Equal("", ReadErrorOutputToEnd().Trim());
            Assert.Equal(
@"1
2", ReadOutputToEnd().Trim());
        }

        [Fact(Skip = "129388")]
        public void AddReference_MultipleReferencesWithSameWeakIdentity()
        {
            var dir = Temp.CreateDirectory();

            var dir1 = dir.CreateDirectory("1");
            var dir2 = dir.CreateDirectory("2");

            var source1 = "public class C1 { }";
            var c1 = CreateCompilationWithMscorlib(source1, assemblyName: "C");
            var file1 = dir1.CreateFile("c.dll").WriteAllBytes(c1.EmitToArray());

            var source2 = "public class C2 { }";
            var c2 = CreateCompilationWithMscorlib(source2, assemblyName: "C");
            var file2 = dir2.CreateFile("c.dll").WriteAllBytes(c2.EmitToArray());

            Execute($@"
#r ""{file1.Path}""
#r ""{file2.Path}""
");
            Execute("new C1()");
            Execute("new C2()");

            // TODO: We should report an error that assembly named 'c' was already loaded with different content.
            // In future we can let it load and let the compiler report the error CS1704: "An assembly with the same simple name 'C' has already been imported".

            Assert.Equal(
@"(2,1): error CS1704: An assembly with the same simple name 'C' has already been imported. Try removing one of the references (e.g. '" + file1.Path + @"') or sign them to enable side-by-side.
(1,5): error CS0246: The type or namespace name 'C1' could not be found (are you missing a using directive or an assembly reference?)
(1,5): error CS0246: The type or namespace name 'C2' could not be found (are you missing a using directive or an assembly reference?)", ReadErrorOutputToEnd().Trim());

            Assert.Equal("", ReadOutputToEnd().Trim());
        }

        [Fact(Skip = "129388")]
        public void AddReference_MultipleReferencesWeakVersioning()
        {
            var dir = Temp.CreateDirectory();

            var dir1 = dir.CreateDirectory("1");
            var dir2 = dir.CreateDirectory("2");

            var source1 = @"[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")] public class C1 { }";
            var c1 = CreateCompilationWithMscorlib(source1, assemblyName: "C");
            var file1 = dir1.CreateFile("c.dll").WriteAllBytes(c1.EmitToArray());

            var source2 = @"[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")] public class C2 { }";
            var c2 = CreateCompilationWithMscorlib(source2, assemblyName: "C");
            var file2 = dir2.CreateFile("c.dll").WriteAllBytes(c2.EmitToArray());

            Execute($@"
#r ""{file1.Path}""
#r ""{file2.Path}""
");
            Execute("new C1()");
            Execute("new C2()");

            // TODO: We should report an error that assembly named 'c' was already loaded with different content.
            // In future we can let it load and improve error reporting around type conversions.

            Assert.Equal("TODO: error", ReadErrorOutputToEnd().Trim());
            Assert.Equal("", ReadOutputToEnd().Trim());
        }

        //// TODO (987032):
        ////        [Fact]
        ////        public void AsyncInitializeContextWithDotNETLibraries()
        ////        {
        ////            var rspFile = Temp.CreateFile();
        ////            var rspDisplay = Path.GetFileName(rspFile.Path);
        ////            var initScript = Temp.CreateFile();

        ////            rspFile.WriteAllText(@"
        /////r:System.Core
        ////""" + initScript.Path + @"""
        ////");

        ////            initScript.WriteAllText(@"
        ////using static System.Console;
        ////using System.Linq.Expressions;
        ////WriteLine(Expression.Constant(123));
        ////");

        ////            // override default "is restarting" behavior (the REPL is already initialized):
        ////            var task = Host.InitializeContextAsync(rspFile.Path, isRestarting: false, killProcess: true);
        ////            task.Wait();

        ////            var output = SplitLines(ReadOutputToEnd());
        ////            var errorOutput = ReadErrorOutputToEnd();

        ////            Assert.Equal(4, output.Length);
        ////            Assert.Equal("Microsoft (R) Roslyn C# Compiler version " + FileVersionInfo.GetVersionInfo(typeof(Compilation).Assembly.Location).FileVersion, output[0]);
        ////            Assert.Equal("Loading context from '" + rspDisplay + "'.", output[1]);
        ////            Assert.Equal("Type \"#help\" for more information.", output[2]);
        ////            Assert.Equal("123", output[3]);

        ////            Assert.Equal("", errorOutput);

        ////            Host.InitializeContextAsync(rspFile.Path).Wait();

        ////            output = SplitLines(ReadOutputToEnd());
        ////            errorOutput = ReadErrorOutputToEnd();

        ////            Assert.True(2 == output.Length, "Output is: '" + string.Join("<NewLine>", output) + "'. Expecting 2 lines.");
        ////            Assert.Equal("Loading context from '" + rspDisplay + "'.", output[0]);
        ////            Assert.Equal("123", output[1]);

        ////            Assert.Equal("", errorOutput);
        ////        }

        ////        [Fact]
        ////        public void AsyncInitializeContextWithBothUserDefinedAndDotNETLibraries()
        ////        {
        ////            var dir = Temp.CreateDirectory();
        ////            var rspFile = Temp.CreateFile();
        ////            var initScript = Temp.CreateFile();

        ////            var dll = CompileLibrary(dir, "c.dll", "C", @"public class C { public static int Main() { return 1; } }");

        ////            rspFile.WriteAllText(@"
        /////r:System.Numerics
        /////r:" + dll.Path + @"
        ////""" + initScript.Path + @"""
        ////");

        ////            initScript.WriteAllText(@"
        ////using static System.Console;
        ////using System.Numerics;
        ////WriteLine(new Complex(12, 6).Real + C.Main());
        ////");

        ////            // override default "is restarting" behavior (the REPL is already initialized):
        ////            var task = Host.InitializeContextAsync(rspFile.Path, isRestarting: false, killProcess: true);
        ////            task.Wait();

        ////            var errorOutput = ReadErrorOutputToEnd();
        ////            Assert.Equal("", errorOutput);

        ////            var output = SplitLines(ReadOutputToEnd());
        ////            Assert.Equal(4, output.Length);
        ////            Assert.Equal("Microsoft (R) Roslyn C# Compiler version " + FileVersionInfo.GetVersionInfo(Host.GetType().Assembly.Location).FileVersion, output[0]);
        ////            Assert.Equal("Loading context from '" + Path.GetFileName(rspFile.Path) + "'.", output[1]);
        ////            Assert.Equal("Type \"#help\" for more information.", output[2]);
        ////            Assert.Equal("13", output[3]);
        ////        }

        [Fact]
        public void ReferencePaths()
        {
            var directory = Temp.CreateDirectory();
            var assemblyName = GetUniqueName();
            CompileLibrary(directory, assemblyName + ".dll", assemblyName, @"public class C { }");
            var rspFile = Temp.CreateFile();
            rspFile.WriteAllText("/lib:" + directory.Path);

            Host.ResetAsync(new InteractiveHostOptions(initializationFile: rspFile.Path, culture: CultureInfo.InvariantCulture)).Wait();

            Execute(
$@"#r ""{assemblyName}.dll""
typeof(C).Assembly.GetName()");

            Assert.Equal("", ReadErrorOutputToEnd());

            var output = SplitLines(ReadOutputToEnd());
            Assert.Equal(2, output.Length);
            Assert.Equal("Loading context from '" + Path.GetFileName(rspFile.Path) + "'.", output[0]);
            Assert.Equal($"[{assemblyName}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]", output[1]);
        }

        [Fact]
        public void DefaultUsings()
        {
            var rspFile = Temp.CreateFile();
            rspFile.WriteAllText(@"
/r:System
/r:System.Core
/r:Microsoft.CSharp
/u:System
/u:System.IO
/u:System.Collections.Generic
/u:System.Diagnostics
/u:System.Dynamic
/u:System.Linq
/u:System.Linq.Expressions
/u:System.Text
/u:System.Threading.Tasks
");
            Host.ResetAsync(new InteractiveHostOptions(initializationFile: rspFile.Path, culture: CultureInfo.InvariantCulture)).Wait();

            Execute(@"
dynamic d = new ExpandoObject();
Process p = new Process();
Expression<Func<int>> e = () => 1;
var squares = from x in new[] { 1, 2, 3 } select x * x;
var sb = new StringBuilder();
var list = new List<int>();
var stream = new MemoryStream();
await Task.Delay(10);

Console.Write(""OK"")
");

            Assert.Equal("", ReadErrorOutputToEnd());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"Loading context from '{Path.GetFileName(rspFile.Path)}'.
OK
", ReadOutputToEnd());
        }

        [Fact]
        public void ScriptAndArguments()
        {
            var scriptFile = Temp.CreateFile(extension: ".csx").WriteAllText("foreach (var arg in Args) Print(arg);");

            var rspFile = Temp.CreateFile();
            rspFile.WriteAllText($@"
{scriptFile}
a
b
c
");
            Host.ResetAsync(new InteractiveHostOptions(initializationFile: rspFile.Path, culture: CultureInfo.InvariantCulture)).Wait();

            Assert.Equal("", ReadErrorOutputToEnd());

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"Loading context from '{Path.GetFileName(rspFile.Path)}'.
""a""
""b""
""c""
", ReadOutputToEnd());
        }


        [Fact]
        public void ReferenceDirectives()
        {
            Execute(@"
#r ""System.Numerics""
#r """ + typeof(System.Linq.Expressions.Expression).Assembly.Location + @"""

using static System.Console;
using System.Linq.Expressions;
using System.Numerics;
WriteLine(Expression.Constant(1));
WriteLine(new Complex(2, 6).Real);
");

            var output = ReadOutputToEnd();
            Assert.Equal("1\r\n2\r\n", output);
        }

        [Fact]
        public void Script_NoHostNamespaces()
        {
            Execute("nameof(Microsoft.CodeAnalysis)");

            AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
(1,8): error CS0234: The type or namespace name 'CodeAnalysis' does not exist in the namespace 'Microsoft' (are you missing an assembly reference?)",
                ReadErrorOutputToEnd());

            Assert.Equal("", ReadOutputToEnd());
        }

        [Fact]
        public void ExecutesOnStaThread()
        {
            Execute(@"
#r ""System""
#r ""System.Xaml""
#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""

new System.Windows.Window();
System.Console.WriteLine(""OK"");
");
            var error = ReadErrorOutputToEnd();
            Assert.Equal("", error);

            var output = ReadOutputToEnd();
            Assert.Equal("OK\r\n", output);
        }

        /// <summary>
        /// Execution of expressions should be
        /// sequential, even await expressions.
        /// </summary>
        [Fact]
        public void ExecuteSequentially()
        {
            Execute(@"using System;
using System.Threading.Tasks;");
            Execute(@"await Task.Delay(1000).ContinueWith(t => 1)");
            Execute(@"await Task.Delay(500).ContinueWith(t => 2)");
            Execute(@"3");
            var output = ReadOutputToEnd();
            Assert.Equal("1\r\n2\r\n3\r\n", output);
        }

        [Fact]
        public void MultiModuleAssembly()
        {
            var dir = Temp.CreateDirectory();
            var dll = dir.CreateFile("MultiModule.dll").WriteAllBytes(TestResources.SymbolsTests.MultiModule.MultiModuleDll);
            dir.CreateFile("mod2.netmodule").WriteAllBytes(TestResources.SymbolsTests.MultiModule.mod2);
            dir.CreateFile("mod3.netmodule").WriteAllBytes(TestResources.SymbolsTests.MultiModule.mod3);

            Execute(@"
#r """ + dll.Path + @"""

new object[] { new Class1(), new Class2(), new Class3() }
");

            var error = ReadErrorOutputToEnd();
            Assert.Equal("", error);

            var output = ReadOutputToEnd();
            Assert.Equal("object[3] { Class1 { }, Class2 { }, Class3 { } }\r\n", output);
        }

        [Fact]
        public void SearchPaths1()
        {
            var dll = Temp.CreateFile(extension: ".dll").WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSInterfaces01);
            var srcDir = Temp.CreateDirectory();
            var dllDir = Path.GetDirectoryName(dll.Path);
            srcDir.CreateFile("foo.csx").WriteAllText("ReferencePaths.Add(@\"" + dllDir + "\");");

            Func<string, string> normalizeSeparatorsAndFrameworkFolders = (s) => s.Replace("\\", "\\\\").Replace("Framework64", "Framework");

            // print default:
            Host.ExecuteAsync(@"ReferencePaths").Wait();
            var output = ReadOutputToEnd();
            Assert.Equal("SearchPaths { \"" + normalizeSeparatorsAndFrameworkFolders(string.Join("\", \"", new[] { s_fxDir })) + "\" }\r\n", output);

            Host.ExecuteAsync(@"SourcePaths").Wait();
            output = ReadOutputToEnd();
            Assert.Equal("SearchPaths { \"" + normalizeSeparatorsAndFrameworkFolders(string.Join("\", \"", new[] { s_homeDir })) + "\" }\r\n", output);

            // add and test if added:
            Host.ExecuteAsync("SourcePaths.Add(@\"" + srcDir + "\");").Wait();

            Host.ExecuteAsync(@"SourcePaths").Wait();

            output = ReadOutputToEnd();
            Assert.Equal("SearchPaths { \"" + normalizeSeparatorsAndFrameworkFolders(string.Join("\", \"", new[] { s_homeDir, srcDir.Path })) + "\" }\r\n", output);

            // execute file (uses modified search paths), the file adds a reference path
            Host.ExecuteFileAsync("foo.csx").Wait();

            Host.ExecuteAsync(@"ReferencePaths").Wait();

            output = ReadOutputToEnd();
            Assert.Equal("SearchPaths { \"" + normalizeSeparatorsAndFrameworkFolders(string.Join("\", \"", new[] { s_fxDir, dllDir })) + "\" }\r\n", output);

            Host.AddReferenceAsync(Path.GetFileName(dll.Path)).Wait();

            Host.ExecuteAsync(@"typeof(Metadata.ICSProp)").Wait();

            var error = ReadErrorOutputToEnd();
            Assert.Equal("", error);

            output = ReadOutputToEnd();
            Assert.Equal("[Metadata.ICSProp]\r\n", output);
        }

        #region Submission result printing - null/void/value.

        [Fact]
        public void SubmissionResult_PrintingNull()
        {
            Execute(@"
string s; 
s
");

            var output = ReadOutputToEnd();

            Assert.Equal("null\r\n", output);
        }

        [Fact]
        public void SubmissionResult_PrintingVoid()
        {
            Execute(@"System.Console.WriteLine(2)");

            var output = ReadOutputToEnd();
            Assert.Equal("2\r\n", output);

            Execute(@"
void foo() { } 
foo()
");

            output = ReadOutputToEnd();
            Assert.Equal("", output);
        }

        #endregion

        private static ImmutableArray<string> SplitLines(string text)
        {
            return ImmutableArray.Create(text.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
