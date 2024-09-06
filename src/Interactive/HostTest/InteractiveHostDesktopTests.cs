// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias InteractiveHost;

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestMetadata;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    using InteractiveHost::Microsoft.CodeAnalysis.Interactive;

    [Trait(Traits.Feature, Traits.Features.InteractiveHost)]
    public sealed class InteractiveHostDesktopTests : AbstractInteractiveHostTests
    {
        internal override InteractiveHostPlatform DefaultPlatform => InteractiveHostPlatform.Desktop64;
        internal override bool UseDefaultInitializationFile => false;

        [Fact]
        public async Task OutputRedirection()
        {
            await Execute(@"
System.Console.WriteLine(""hello-\u4567!""); 
System.Console.Error.WriteLine(""error-\u7890!""); 
1+1");

            var output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            Assert.Equal("hello-\u4567!\r\n2\r\n", output);
            Assert.Equal("error-\u7890!\r\n", error);
        }

        [Fact]
        public async Task OutputRedirection2()
        {
            await Execute(@"System.Console.WriteLine(1);");
            await Execute(@"System.Console.Error.WriteLine(2);");

            var output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            Assert.Equal("1\r\n", output);
            Assert.Equal("2\r\n", error);

            RedirectOutput();

            await Execute(@"System.Console.WriteLine(3);");
            await Execute(@"System.Console.Error.WriteLine(4);");

            output = await ReadOutputToEnd();
            error = await ReadErrorOutputToEnd();
            Assert.Equal("3\r\n", output);
            Assert.Equal("4\r\n", error);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/46414")]
        public async Task StackOverflow()
        {
            var process = Host.TryGetProcess();

            await Execute(@"
int goo(int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9) 
{ 
    return goo(0,1,2,3,4,5,6,7,8,9) + goo(0,1,2,3,4,5,6,7,8,9); 
} 
goo(0,1,2,3,4,5,6,7,8,9)
            ");

            var output = await ReadOutputToEnd();
            Assert.Equal("", output);

            // Hosting process exited with exit code ###.
            var errorOutput = (await ReadErrorOutputToEnd()).Trim();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                "Process is terminated due to StackOverflowException.\n" + string.Format(InteractiveHostResources.Hosting_process_exited_with_exit_code_0, process!.ExitCode), errorOutput);

            await Execute(@"1+1");
            output = await ReadOutputToEnd();
            Assert.Equal("2\r\n", output.ToString());
        }

        private const string MethodWithInfiniteLoop = @"
void goo() 
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
        public async Task AsyncExecute_InfiniteLoop()
        {
            var mayTerminate = new ManualResetEvent(false);
            Host.ErrorOutputReceived += (_, __) => mayTerminate.Set();

            await Host.ExecuteAsync(MethodWithInfiniteLoop + "\r\nfoo()");
            Assert.True(mayTerminate.WaitOne());
            await RestartHost();

            await Host.ExecuteAsync(MethodWithInfiniteLoop + "\r\nfoo()");

            var execution = await Execute(@"1+1");
            var output = await ReadOutputToEnd();
            Assert.True(execution);
            Assert.Equal("2\r\n", output);
        }

        [Fact(Skip = "529027")]
        public async Task AsyncExecute_HangingForegroundThreads()
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
            var error = await ReadErrorOutputToEnd();
            Assert.Equal("", error);

            Assert.True(mayTerminate.WaitOne());

            // TODO: var service = _host.TryGetService();
            // Assert.NotNull(service);

            var process = Host.TryGetProcess();
            Assert.NotNull(process);

            // service!.EmulateClientExit();

            // the process should terminate with exit code 0:
            process!.WaitForExit();
            Assert.Equal(0, process.ExitCode);
        }

        [Fact]
        public async Task AsyncExecuteFile_InfiniteLoop()
        {
            var file = Temp.CreateFile().WriteAllText(MethodWithInfiniteLoop + "\r\nfoo();").Path;

            var mayTerminate = new ManualResetEvent(false);
            Host.ErrorOutputReceived += (_, __) => mayTerminate.Set();

            await Host.ExecuteFileAsync(file);
            mayTerminate.WaitOne();

            await RestartHost();

            var execution = await Execute(@"1+1");
            var output = await ReadOutputToEnd();
            Assert.True(execution);
            Assert.Equal("2\r\n", output);
        }

        [Fact]
        public async Task AsyncExecuteFile_SourceKind()
        {
            var file = Temp.CreateFile().WriteAllText("1 1").Path;
            var task = await Host.ExecuteFileAsync(file);
            Assert.False(task.Success);

            var errorOut = (await ReadErrorOutputToEnd()).Trim();
            Assert.True(errorOut.StartsWith(file + "(1,3):", StringComparison.Ordinal), "Error output should start with file name, line and column");
            Assert.True(errorOut.Contains("CS1002"), "Error output should include error CS1002");
        }

        [Fact]
        public async Task AsyncExecuteFile_NonExistingFile()
        {
            var result = await Host.ExecuteFileAsync("non existing file");
            Assert.False(result.Success);

            var errorOut = (await ReadErrorOutputToEnd()).Trim();
            Assert.Contains(InteractiveHostResources.Specified_file_not_found, errorOut, StringComparison.Ordinal);
            Assert.Contains(InteractiveHostResources.Searched_in_directory_colon, errorOut, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AsyncExecuteFile()
        {
            var file = Temp.CreateFile().WriteAllText(@"
using static System.Console;

public class C 
{ 
   public int field = 4; 
   public int Goo(int i) { return i; } 
}

public int Goo(int i) { return i; }

WriteLine(5);
").Path;
            var task = await Host.ExecuteFileAsync(file);

            var output = await ReadOutputToEnd();
            Assert.True(task.Success);
            Assert.Equal("5", output.Trim());

            await Execute("Goo(2)");
            output = await ReadOutputToEnd();
            Assert.Equal("2", output.Trim());

            await Execute("new C().Goo(3)");
            output = await ReadOutputToEnd();
            Assert.Equal("3", output.Trim());

            await Execute("new C().field");
            output = await ReadOutputToEnd();
            Assert.Equal("4", output.Trim());
        }

        [Fact]
        public async Task AsyncExecuteFile_InvalidFileContent()
        {
            await Host.ExecuteFileAsync(typeof(Process).Assembly.Location);

            var errorOut = (await ReadErrorOutputToEnd()).Trim();
            Assert.True(errorOut.StartsWith(typeof(Process).Assembly.Location + "(1,3):", StringComparison.Ordinal), "Error output should start with file name, line and column");
            Assert.True(errorOut.Contains("CS1056"), "Error output should include error CS1056");
            Assert.True(errorOut.Contains("CS1002"), "Error output should include error CS1002");
        }

        [Fact]
        public async Task AsyncExecuteFile_ScriptFileWithBuildErrors()
        {
            var file = Temp.CreateFile().WriteAllText("#load blah.csx" + "\r\n" + "class C {}");

            await Host.ExecuteFileAsync(file.Path);

            var errorOut = (await ReadErrorOutputToEnd()).Trim();
            Assert.True(errorOut.StartsWith(file.Path + "(1,7):", StringComparison.Ordinal), "Error output should start with file name, line and column");
            Assert.True(errorOut.Contains("CS7010"), "Error output should include error CS7010");
        }

        /// <summary>
        /// Check that the assembly resolve event doesn't cause any harm. It shouldn't actually be
        /// even invoked since we resolve the assembly via Fusion.
        /// </summary>
        [Fact(Skip = "987032")]
        public async Task UserDefinedAssemblyResolve_InfiniteLoop()
        {
            var mayTerminate = new ManualResetEvent(false);
            Host.ErrorOutputReceived += (_, __) => mayTerminate.Set();

            // TODO: _host.TryGetService()!.HookMaliciousAssemblyResolve();
            Assert.True(mayTerminate.WaitOne());
            await Host.AddReferenceAsync("nonexistingassembly" + Guid.NewGuid());

            Assert.True(await Execute(@"1+1"));

            var output = await ReadOutputToEnd();
            Assert.Equal("2\r\n", output);
        }

        [Fact]
        public async Task AddReference_Path()
        {
            var fxDir = await GetHostRuntimeDirectoryAsync();
            Assert.False(await Execute("new System.Data.DataSet()"));
            Assert.True(await LoadReference(Path.Combine(fxDir, "System.Data.dll")));
            Assert.True(await Execute("new System.Data.DataSet()"));
        }

        [Fact]
        public async Task AddReference_PartialName()
        {
            Assert.False(await Execute("new System.Data.DataSet()"));
            Assert.True(await LoadReference("System.Data"));
            Assert.True(await Execute("new System.Data.DataSet()"));
        }

        [Fact]
        public async Task AddReference_PartialName_LatestVersion()
        {
            // there might be two versions of System.Data - v2 and v4, we should get the latter:
            Assert.True(await LoadReference("System.Data"));
            Assert.True(await LoadReference("System"));
            Assert.True(await LoadReference("System.Xml"));
            await Execute(@"new System.Data.DataSet().GetType().Assembly.GetName().Version");
            var output = await ReadOutputToEnd();
            Assert.Equal("[4.0.0.0]\r\n", output);
        }

        [Fact]
        public async Task AddReference_FullName()
        {
            Assert.False(await Execute("new System.Data.DataSet()"));
            Assert.True(await LoadReference("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
            Assert.True(await Execute("new System.Data.DataSet()"));
        }

        [ConditionalFact(typeof(Framework35Installed), AlwaysSkip = "https://github.com/dotnet/roslyn/issues/5167")]
        public async Task AddReference_VersionUnification1()
        {
            // V3.5 unifies with the current Framework version:
            var result = await LoadReference("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            var output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            Assert.Equal("", error.Trim());
            Assert.Equal("", output.Trim());
            Assert.True(result);

            result = await LoadReference("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            output = await ReadOutputToEnd();
            error = await ReadErrorOutputToEnd();
            Assert.Equal("", error.Trim());
            Assert.Equal("", output.Trim());
            Assert.True(result);

            result = await LoadReference("System.Core");
            output = await ReadOutputToEnd();
            error = await ReadErrorOutputToEnd();
            Assert.Equal("", error.Trim());
            Assert.Equal("", output.Trim());
            Assert.True(result);
        }

        // Caused by submission not inheriting references.
        [Fact(Skip = "101161")]
        public async Task AddReference_ShadowCopy()
        {
            var dir = Temp.CreateDirectory();

            // create C.dll
            var c = CompileLibrary(dir, "c.dll", "c", @"public class C { }");

            // load C.dll: 
            var output = await ReadOutputToEnd();
            Assert.True(await LoadReference(c.Path));
            Assert.True(await Execute("new C()"));
            Assert.Equal("C { }", output.Trim());

            // rewrite C.dll:            
            File.WriteAllBytes(c.Path, [1, 2, 3]);

            // we can still run code:
            var result = await Execute("new C()");
            output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            Assert.Equal("", error.Trim());
            Assert.Equal("C { }", output.Trim());
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
        public async Task AddReference_Dependencies_DllExe()
        {
            var dir = Temp.CreateDirectory();

            var dll = CompileLibrary(dir, "c.dll", "C", @"public class C { public static int Main() { return 1; } }");
            var exe = CompileLibrary(dir, "c.exe", "C", @"public class C { public static int Main() { return 2; } }");

            var main = CompileLibrary(dir, "main.exe", "Main", @"public class Program { public static int Main() { return C.Main(); } }",
                MetadataReference.CreateFromImage(dll.Image));

            Assert.True(await LoadReference(main.Path));
            Assert.True(await Execute("Program.Main()"));

            var output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            Assert.Equal("", error.Trim());
            Assert.Equal("1", output.Trim());
        }

        [Fact]
        public async Task AddReference_Dependencies_Versions()
        {
            var dir1 = Temp.CreateDirectory();
            var dir2 = Temp.CreateDirectory();
            var dir3 = Temp.CreateDirectory();

            // [assembly:AssemblyVersion("1.0.0.0")] public class C { public static int Main() { return 1; } }");
            var file1 = dir1.CreateFile("c.dll").WriteAllBytes(TestResources.General.C1);

            // [assembly:AssemblyVersion("2.0.0.0")] public class C { public static int Main() { return 2; } }");
            var file2 = dir2.CreateFile("c.dll").WriteAllBytes(TestResources.General.C2);

            Assert.True(await LoadReference(file1.Path));
            Assert.True(await LoadReference(file2.Path));

            var main = CompileLibrary(dir3, "main.exe", "Main", @"public class Program { public static int Main() { return C.Main(); } }",
                MetadataReference.CreateFromImage(TestResources.General.C2.AsImmutableOrNull()));

            Assert.True(await LoadReference(main.Path));
            Assert.True(await Execute("Program.Main()"));

            var output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            Assert.Equal("", error.Trim());
            Assert.Equal("2", output.Trim());
        }

        [Fact]
        public async Task AddReference_AlreadyLoadedDependencies()
        {
            var dir = Temp.CreateDirectory();

            var lib1 = CompileLibrary(dir, "lib1.dll", "lib1", @"public interface I { int M(); }");
            var lib2 = CompileLibrary(dir, "lib2.dll", "lib2", @"public class C : I { public int M() { return 1; } }",
                MetadataReference.CreateFromFile(lib1.Path));

            await Execute("#r \"" + lib1.Path + "\"");
            await Execute("#r \"" + lib2.Path + "\"");
            await Execute("new C().M()");

            var output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", error);
            AssertEx.AssertEqualToleratingWhitespaceDifferences("1", output);
        }

        [Fact(Skip = "101161")]
        public async Task AddReference_LoadUpdatedReference()
        {
            var dir = Temp.CreateDirectory();

            var source1 = "public class C { public int X = 1; }";
            var c1 = CreateCompilation(source1, assemblyName: "C");
            var file = dir.CreateFile("c.dll").WriteAllBytes(c1.EmitToArray());

            // use:
            await Execute($@"
#r ""{file.Path}""
C goo() => new C();
new C().X
");

            // update:
            var source2 = "public class D { public int Y = 2; }";
            var c2 = CreateCompilation(source2, assemblyName: "C");
            file.WriteAllBytes(c2.EmitToArray());

            // add the reference again:
            await Execute($@"
#r ""{file.Path}""

new D().Y
");
            // TODO: We should report an error that assembly named 'a' was already loaded with different content.
            // In future we can let it load and improve error reporting around type conversions.

            var output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            Assert.Equal("", error.Trim());
            Assert.Equal(@"1
2", output.Trim());
        }

        [Fact(Skip = "129388")]
        public async Task AddReference_MultipleReferencesWithSameWeakIdentity()
        {
            var dir = Temp.CreateDirectory();

            var dir1 = dir.CreateDirectory("1");
            var dir2 = dir.CreateDirectory("2");

            var source1 = "public class C1 { }";
            var c1 = CreateCompilation(source1, assemblyName: "C");
            var file1 = dir1.CreateFile("c.dll").WriteAllBytes(c1.EmitToArray());

            var source2 = "public class C2 { }";
            var c2 = CreateCompilation(source2, assemblyName: "C");
            var file2 = dir2.CreateFile("c.dll").WriteAllBytes(c2.EmitToArray());

            await Execute($@"
#r ""{file1.Path}""
#r ""{file2.Path}""
");
            await Execute("new C1()");
            await Execute("new C2()");

            // TODO: We should report an error that assembly named 'c' was already loaded with different content.
            // In future we can let it load and let the compiler report the error CS1704: "An assembly with the same simple name 'C' has already been imported".

            var output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            Assert.Equal(@"(2,1): error CS1704: An assembly with the same simple name 'C' has already been imported. Try removing one of the references (e.g. '" + file1.Path + @"') or sign them to enable side-by-side.
(1,5): error CS0246: The type or namespace name 'C1' could not be found (are you missing a using directive or an assembly reference?)
(1,5): error CS0246: The type or namespace name 'C2' could not be found (are you missing a using directive or an assembly reference?)", error.Trim());

            Assert.Equal("", output.Trim());
        }

        [Fact(Skip = "129388")]
        public async Task AddReference_MultipleReferencesWeakVersioning()
        {
            var dir = Temp.CreateDirectory();

            var dir1 = dir.CreateDirectory("1");
            var dir2 = dir.CreateDirectory("2");

            var source1 = @"[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")] public class C1 { }";
            var c1 = CreateCompilation(source1, assemblyName: "C");
            var file1 = dir1.CreateFile("c.dll").WriteAllBytes(c1.EmitToArray());

            var source2 = @"[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")] public class C2 { }";
            var c2 = CreateCompilation(source2, assemblyName: "C");
            var file2 = dir2.CreateFile("c.dll").WriteAllBytes(c2.EmitToArray());

            await Execute($@"
#r ""{file1.Path}""
#r ""{file2.Path}""
");
            await Execute("new C1()");
            await Execute("new C2()");

            // TODO: We should report an error that assembly named 'c' was already loaded with different content.
            // In future we can let it load and improve error reporting around type conversions.

            var output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            Assert.Equal("TODO: error", error.Trim());
            Assert.Equal("", output.Trim());
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
        public async Task ReferencePathsRsp()
        {
            var directory1 = Temp.CreateDirectory();
            CompileLibrary(directory1, "Assembly0.dll", "Assembly0", @"public class C0 { }");
            CompileLibrary(directory1, "Assembly1.dll", "Assembly1", @"public class C1 { }");

            var initDirectory = Temp.CreateDirectory();
            var initFile = initDirectory.CreateFile("init.csx");

            initFile.WriteAllText(@"
#r ""Assembly0.dll""
System.Console.WriteLine(typeof(C0).Assembly.GetName());
System.Console.WriteLine(typeof(C2).Assembly.GetName());
Print(ReferencePaths);
");
            var rspDirectory = Temp.CreateDirectory();
            CompileLibrary(rspDirectory, "Assembly2.dll", "Assembly2", @"public class C2 { }");
            CompileLibrary(rspDirectory, "Assembly3.dll", "Assembly3", @"public class C3 { }");

            var rspFile = rspDirectory.CreateFile("init.rsp");
            rspFile.WriteAllText($"/lib:{directory1.Path} /r:Assembly2.dll {initFile.Path}");

            await Host.ResetAsync(new InteractiveHostOptions(Host.OptionsOpt!.HostPath, rspFile.Path, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture, Host.OptionsOpt!.Platform));
            var fxDir = await GetHostRuntimeDirectoryAsync();

            await Execute(@"
#r ""Assembly1.dll""
System.Console.WriteLine(typeof(C1).Assembly.GetName());
Print(ReferencePaths);
");

            var error = await ReadErrorOutputToEnd();
            var output = await ReadOutputToEnd();

            var expectedSearchPaths = PrintSearchPaths(fxDir, directory1.Path);

            AssertEx.AssertEqualToleratingWhitespaceDifferences("", error);
            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
{string.Format(InteractiveHostResources.Loading_context_from_0, Path.GetFileName(rspFile.Path))}
Assembly0, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
Assembly2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
{expectedSearchPaths}
Assembly1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
{expectedSearchPaths}
    ", output);
        }

        [Fact(Skip = "Metalama Compiler doesn't support C# Interactive and this test fails because of localization of the exception message.")]
        public async Task ReferencePathsRsp_Error()
        {
            var initDirectory = Temp.CreateDirectory();
            var initFile = initDirectory.CreateFile("init.csx");
            initFile.WriteAllText(@"#r ""Assembly.dll""");

            var rspDirectory = Temp.CreateDirectory();
            CompileLibrary(rspDirectory, "Assembly.dll", "Assembly", "public class C { }");

            var rspFile = rspDirectory.CreateFile("init.rsp");
            rspFile.WriteAllText($"{initFile.Path}");

            await Host.ResetAsync(new InteractiveHostOptions(Host.OptionsOpt!.HostPath, rspFile.Path, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture, Host.OptionsOpt!.Platform));

            var error = await ReadErrorOutputToEnd();
            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                @$"{initFile.Path}(1,1): error CS0006: {string.Format(CSharpResources.ERR_NoMetadataFile, "Assembly.dll")}", error);
        }

        [Fact]
        public async Task DefaultUsings()
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
            await Host.ResetAsync(new InteractiveHostOptions(Host.OptionsOpt!.HostPath, rspFile.Path, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture, Host.OptionsOpt!.Platform));

            await Execute(@"
dynamic d = new ExpandoObject();
");
            await Execute(@"
Process p = new Process();
");
            await Execute(@"
Expression<Func<int>> e = () => 1;
");
            await Execute(@"
var squares = from x in new[] { 1, 2, 3 } select x * x;
");
            await Execute(@"
var sb = new StringBuilder();
");
            await Execute(@"
var list = new List<int>();
");
            await Execute(@"
var stream = new MemoryStream();
await Task.Delay(10);
p = new Process();

Console.Write(""OK"")
");

            var error = await ReadErrorOutputToEnd();
            var output = await ReadOutputToEnd();

            AssertEx.AssertEqualToleratingWhitespaceDifferences("", error);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{string.Format(InteractiveHostResources.Loading_context_from_0, Path.GetFileName(rspFile.Path))} 
OK
", output);
        }

        [Fact(Skip = "Metalama Compiler doesn't support C# Interactive and this test fails because of localization of the exception message.")]
        public async Task InitialScript_Error()
        {
            var initFile = Temp.CreateFile(extension: ".csx").WriteAllText("1 1");

            var rspFile = Temp.CreateFile();

            rspFile.WriteAllText($@"
/r:System
/u:System.Diagnostics
{initFile.Path}
");

            await Host.ResetAsync(new InteractiveHostOptions(Host.OptionsOpt!.HostPath, rspFile.Path, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture, Host.OptionsOpt!.Platform));

            await Execute("new Process()");

            var error = await ReadErrorOutputToEnd();
            var output = await ReadOutputToEnd();
            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"{initFile.Path}(1,3): error CS1002: {CSharpResources.ERR_SemicolonExpected}
", error);

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
{string.Format(InteractiveHostResources.Loading_context_from_0, Path.GetFileName(rspFile.Path))}
[System.Diagnostics.Process]
", output);
        }

        [Fact]
        public async Task ScriptAndArguments()
        {
            var scriptFile = Temp.CreateFile(extension: ".csx").WriteAllText("foreach (var arg in Args) Print(arg);");

            var rspFile = Temp.CreateFile();
            rspFile.WriteAllText($@"
{scriptFile}
a
b
c
");
            await Host.ResetAsync(new InteractiveHostOptions(Host.OptionsOpt!.HostPath, rspFile.Path, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture, Host.OptionsOpt!.Platform));

            var error = await ReadErrorOutputToEnd();
            Assert.Equal("", error);
            AssertEx.AssertEqualToleratingWhitespaceDifferences(
$@"{string.Format(InteractiveHostResources.Loading_context_from_0, Path.GetFileName(rspFile.Path))}
""a""
""b""
""c""
", await ReadOutputToEnd());
        }

        [Fact(Skip = "Metalama Compiler doesn't support C# Interactive and this test fails because of localization of the exception message.")]
        public async Task Script_NoHostNamespaces()
        {
            await Execute("nameof(Microsoft.Missing)");
            var error = await ReadErrorOutputToEnd();
            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"(1,8): error CS0234: {string.Format(CSharpResources.ERR_DottedTypeNameNotFoundInNS, "Missing", "Microsoft")}",
    error);

            var output = await ReadOutputToEnd();
            Assert.Equal("", output);
        }

        [Fact]
        public async Task ExecutesOnStaThread()
        {
            await Execute(@"
#r ""System""
#r ""System.Xaml""
#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""

new System.Windows.Window();
System.Console.WriteLine(""OK"");
");
            var error = await ReadErrorOutputToEnd();
            var output = await ReadOutputToEnd();
            Assert.Equal("", error);
            Assert.Equal("OK\r\n", output);
        }

        /// <summary>
        /// Execution of expressions should be
        /// sequential, even await expressions.
        /// </summary>
        [Fact]
        public async Task ExecuteSequentially()
        {
            await Execute(@"using System;
using System.Threading.Tasks;");
            await Execute(@"await Task.Delay(1000).ContinueWith(t => 1)");
            await Execute(@"await Task.Delay(500).ContinueWith(t => 2)");
            await Execute(@"3");
            var output = await ReadOutputToEnd();
            Assert.Equal("1\r\n2\r\n3\r\n", output);
        }

        [Fact]
        public async Task MultiModuleAssembly()
        {
            var dir = Temp.CreateDirectory();
            var dll = dir.CreateFile("MultiModule.dll").WriteAllBytes(TestResources.SymbolsTests.MultiModule.MultiModuleDll);
            dir.CreateFile("mod2.netmodule").WriteAllBytes(TestResources.SymbolsTests.MultiModule.mod2);
            dir.CreateFile("mod3.netmodule").WriteAllBytes(TestResources.SymbolsTests.MultiModule.mod3);

            await Execute(@"
#r """ + dll.Path + @"""

new object[] { new Class1(), new Class2(), new Class3() }
");

            var error = await ReadErrorOutputToEnd();
            var output = await ReadOutputToEnd();
            Assert.Equal("", error);
            Assert.Equal("object[3] { Class1 { }, Class2 { }, Class3 { } }\r\n", output);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6457")]
        public async Task MissingReferencesReuse()
        {
            var source = @"
public class C
{
    public System.Diagnostics.Process P;
}
";

            var lib = CSharpCompilation.Create(
"Lib",
new[] { SyntaxFactory.ParseSyntaxTree(source) },
new[] { Net451.mscorlib, Net451.System },
new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var libFile = Temp.CreateFile("lib").WriteAllBytes(lib.EmitToArray());

            await Execute($@"#r ""{libFile.Path}""");
            await Execute("C c;");
            await Execute("c = new C()");

            var error = await ReadErrorOutputToEnd();
            var output = await ReadOutputToEnd();
            Assert.Equal("", error);
            AssertEx.AssertEqualToleratingWhitespaceDifferences("C { P=null }", output);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7280")]
        public async Task AsyncContinueOnDifferentThread()
        {
            await Execute(@"
using System;
using System.Threading;
using System.Threading.Tasks;

Console.Write(Task.Run(() => { Thread.CurrentThread.Join(100); return 42; }).ContinueWith(t => t.Result).Result)");

            var output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            Assert.Equal("42", output);
            Assert.Empty(error);
        }

        [Fact]
        public async Task Exception()
        {
            await Execute(@"throw new System.Exception();");

            var output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            Assert.Equal("", output);
            Assert.DoesNotContain("Unexpected", error, StringComparison.OrdinalIgnoreCase);
            Assert.True(error.StartsWith($"{new Exception().GetType()}: {new Exception().Message}"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10883")]
        public async Task PreservingDeclarationsOnException()
        {
            await Execute(@"int i = 100;");
            await Execute(@"int j = 20; throw new System.Exception(""Bang!""); int k = 3;");
            await Execute(@"i + j + k");

            var output = await ReadOutputToEnd();
            var error = await ReadErrorOutputToEnd();
            AssertEx.AssertEqualToleratingWhitespaceDifferences("120", output);
            AssertEx.AssertEqualToleratingWhitespaceDifferences("System.Exception: Bang!", error);
        }

        [Fact]
        public async Task Bitness()
        {
            await Host.ExecuteAsync(@"System.IntPtr.Size");
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", await ReadErrorOutputToEnd());
            AssertEx.AssertEqualToleratingWhitespaceDifferences("8\r\n", await ReadOutputToEnd());

            await Host.ResetAsync(InteractiveHostOptions.CreateFromDirectory(TestUtils.HostRootPath, initializationFileName: null, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture, InteractiveHostPlatform.Desktop32));
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", await ReadErrorOutputToEnd());
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", await ReadOutputToEnd());

            await Host.ExecuteAsync(@"System.IntPtr.Size");
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", await ReadErrorOutputToEnd());
            AssertEx.AssertEqualToleratingWhitespaceDifferences("4\r\n", await ReadOutputToEnd());

            var result = await Host.ResetAsync(InteractiveHostOptions.CreateFromDirectory(TestUtils.HostRootPath, initializationFileName: null, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture, InteractiveHostPlatform.Core));
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", await ReadErrorOutputToEnd());
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", await ReadOutputToEnd());

            await Host.ExecuteAsync(@"System.IntPtr.Size");
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", await ReadErrorOutputToEnd());
            AssertEx.AssertEqualToleratingWhitespaceDifferences("8\r\n", await ReadOutputToEnd());
        }

        [Fact]
        public async Task Culture()
        {
            var rspFile = Temp.CreateFile();

            var culture = new CultureInfo("cs-CZ");
            var uiCulture = CultureInfo.CurrentUICulture;

            await Host.ResetAsync(new InteractiveHostOptions(Host.OptionsOpt!.HostPath, rspFile.Path, culture, uiCulture, Host.OptionsOpt!.Platform));

            await Host.ExecuteAsync(@"(1000.23).ToString(""C"")");

            var error = await ReadErrorOutputToEnd();
            Assert.Equal("", error);

            var output = await ReadOutputToEnd();

            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
{string.Format(InteractiveHostResources.Loading_context_from_0, Path.GetFileName(rspFile.Path))}
""1 000,23 Kč""
", output);
        }

        #region Submission result printing - null/void/value.

        [Fact]
        public async Task SubmissionResult_PrintingNull()
        {
            await Execute(@"
string s; 
s
");

            var output = await ReadOutputToEnd();

            Assert.Equal("null\r\n", output);
        }

        [Fact]
        public async Task SubmissionResult_PrintingVoid()
        {
            await Execute(@"System.Console.WriteLine(2)");

            var output = await ReadOutputToEnd();
            Assert.Equal("2\r\n", output);

            await Execute(@"
void goo() { } 
goo()
");

            output = await ReadOutputToEnd();
            Assert.Equal("", output);
        }

        // TODO (https://github.com/dotnet/roslyn/issues/7976): delete this
        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/7976")]
        public void Workaround7976()
        {
            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        #endregion
    }
}
