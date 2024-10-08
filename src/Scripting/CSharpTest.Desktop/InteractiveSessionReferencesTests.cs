// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

extern alias PortableTestUtils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Test;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using AssertEx = PortableTestUtils::Roslyn.Test.Utilities.AssertEx;
using TestBase = PortableTestUtils::Roslyn.Test.Utilities.TestBase;
using WorkItemAttribute = PortableTestUtils::Roslyn.Test.Utilities.WorkItemAttribute;
using static Microsoft.CodeAnalysis.Scripting.TestCompilationFactory;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Test
{
    public class InteractiveSessionReferencesTests : TestBase
    {
        private static readonly CSharpCompilationOptions s_signedDll =
           new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, cryptoPublicKey: TestResources.TestKeys.PublicKey_ce65828c82a341f2);

        private static readonly CSharpCompilationOptions s_signedDll2 =
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, cryptoPublicKey: TestResources.TestKeys.PublicKey_ce65828c82a341f2);

        [Fact]
        public async Task CompilationChain_GlobalImportsRebinding()
        {
            var options = ScriptOptions.Default.AddImports("System.Diagnostics");

            var s0 = await CSharpScript.RunAsync("", options);

            ScriptingTestHelpers.AssertCompilationError(s0, @"Process.GetCurrentProcess()",
                // (2,1): error CS0103: The name 'Process' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Process").WithArguments("Process"));

            var s1 = s0.ContinueWithAsync($"#r \"{typeof(Process).Assembly.Location}\"");
            var s2 = s1.ContinueWith("Process.GetCurrentProcess()");

            Assert.NotNull(s2.Result);
        }

        [Fact]
        public async Task CompilationChain_UsingRebinding_AddReference()
        {
            var s0 = await CSharpScript.RunAsync("using System.Diagnostics;");

            var newOptions = s0.Script.Options.AddReferences(typeof(Process).Assembly);

            var s1 = s0.ContinueWithAsync(@"Process.GetCurrentProcess()", newOptions);

            Assert.NotNull(s1.Result);
        }

        [Fact]
        public async Task CompilationChain_UsingRebinding_Directive()
        {
            var s0 = await CSharpScript.RunAsync("using System.Diagnostics;");

            var s1 = s0.ContinueWithAsync($@"
#r ""{typeof(Process).Assembly.Location}""
Process.GetCurrentProcess()");

            Assert.NotNull(s1.Result);
        }

        //
        // General rule for symbol lookup: 
        //
        // Declaration A in submission S hides declaration B in submission T iff
        // S precedes T, and A and B can't coexist in the same scope.

        [Fact]
        public void CompilationChain_SubmissionSlots()
        {
            var s =
                CSharpScript.RunAsync("using System;").
                ContinueWith("using static System.Environment;").
                ContinueWith("int x; x = 1;").
                ContinueWith("using static System.Math;").
                ContinueWith("int goo(int a) { return a + 1; } ");

#if false
            Assert.True(session.executionState.submissions.Length >= 2, "Expected two submissions");
            session.executionState.submissions.Aggregate(0, (i, sub) => { Assert.Equal(i < 2, sub != null); return i + 1; });
#endif
            ScriptingTestHelpers.AssertCompilationError(s, "Version",
                // (1,1): error CS0229: Ambiguity between 'System.Version' and 'System.Environment.Version'
                Diagnostic(ErrorCode.ERR_AmbigMember, "Version").WithArguments("System.Version", "System.Environment.Version"));

            s = s.ContinueWith("new System.Collections.Generic.List<Version>()");
            Assert.IsType<List<Version>>(s.Result.ReturnValue);

            s = s.ContinueWith("Environment.Version");
            Assert.Equal(Environment.Version, s.Result.ReturnValue);

            s = s.ContinueWith("goo(x)");
            Assert.Equal(2, s.Result.ReturnValue);

            s = s.ContinueWith("Sin(0)");
            Assert.Equal(0.0, s.Result.ReturnValue);
        }

        [Fact]
        public void SearchPaths1()
        {
            var options = ScriptOptions.Default.WithMetadataResolver(ScriptMetadataResolver.Default.WithSearchPaths(RuntimeEnvironment.GetRuntimeDirectory()));

            var result = CSharpScript.EvaluateAsync($@"
#r ""System.Data.dll""
#r ""System""
#r ""{typeof(System.Xml.Serialization.IXmlSerializable).GetTypeInfo().Assembly.Location}""
new System.Data.DataSet()
", options).Result;

            Assert.True(result is System.Data.DataSet, "Expected DataSet");
        }

        /// <summary>
        /// Default search paths can be removed.
        /// </summary>
        [Fact]
        public void SearchPaths_RemoveDefault()
        {
            // remove default paths:
            var options = ScriptOptions.Default;

            var source = @"
#r ""System.Data.dll""
new System.Data.DataSet()
";

            ScriptingTestHelpers.AssertCompilationError(() => CSharpScript.EvaluateAsync(source, options),
                // (2,1): error CS0006: Metadata file 'System.Data.dll' could not be found
                // #r "System.Data.dll"
                Diagnostic(ErrorCode.ERR_NoMetadataFile, @"#r ""System.Data.dll""").WithArguments("System.Data.dll"),
                // (3,12): error CS0234: The type or namespace name 'Data' does not exist in the namespace 'System' (are you missing an assembly reference?)
                // new System.Data.DataSet()
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Data").WithArguments("Data", "System"));
        }

        /// <summary>
        /// Look at base directory (or directory containing #r) before search paths.
        /// </summary>
        [Fact]
        public async Task SearchPaths_BaseDirectory()
        {
            var options = ScriptOptions.Default.
                WithMetadataResolver(new TestMetadataReferenceResolver(
                    pathResolver: new VirtualizedRelativePathResolver(existingFullPaths: new[] { @"C:\dir\x.dll" }, baseDirectory: @"C:\goo\bar"),
                    files: new Dictionary<string, PortableExecutableReference> { { @"C:\dir\x.dll", (PortableExecutableReference)SystemCoreRef } }));

            var script = CSharpScript.Create(@"
#r ""x.dll""
using System.Linq;

var x = from a in new[] { 1, 2 ,3 } select a + 1;
", options.WithFilePath(@"C:\dir\a.csx"));

            var state = await script.RunAsync().ContinueWith<IEnumerable<int>>("x", options.WithFilePath(null));

            AssertEx.Equal(new[] { 2, 3, 4 }, state.ReturnValue);
        }

        [Fact]
        public async Task References1()
        {
            var options0 = ScriptOptions.Default.AddReferences(
                typeof(Process).Assembly,
                typeof(System.Linq.Expressions.Expression).Assembly);

            var s0 = await CSharpScript.RunAsync<Process>($@"
#r ""{typeof(System.Data.DataSet).Assembly.Location}""
#r ""System""
#r ""{typeof(System.Xml.Serialization.IXmlSerializable).Assembly.Location}""
new System.Data.DataSet();
System.Linq.Expressions.Expression.Constant(123);
System.Diagnostics.Process.GetCurrentProcess()
", options0);

            Assert.NotNull(s0.ReturnValue);

            var options1 = options0.AddReferences(typeof(System.Xml.XmlDocument).Assembly);

            var s1 = await s0.ContinueWithAsync<System.Xml.XmlDocument>(@"
new System.Xml.XmlDocument()
", options1);

            Assert.NotNull(s1.ReturnValue);

            var options2 = options1.AddReferences("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            var s2 = await s1.ContinueWithAsync(@"
System.Drawing.Color.Coral
", options2);

            Assert.NotNull(s2.ReturnValue);

            var options3 = options2.AddReferences(typeof(System.Windows.Forms.Form).Assembly.Location);

            var s3 = await s2.ContinueWithAsync<System.Windows.Forms.Form>(@"
new System.Windows.Forms.Form()
", options3);

            Assert.NotNull(s3.ReturnValue);
        }

        [Fact]
        public void References2()
        {
            var options = ScriptOptions.Default.
                WithMetadataResolver(ScriptMetadataResolver.Default.WithSearchPaths(RuntimeEnvironment.GetRuntimeDirectory())).
                AddReferences("System.Core", "System.dll").
                AddReferences(typeof(System.Data.DataSet).Assembly);

            var process = CSharpScript.EvaluateAsync<Process>($@"
#r ""{typeof(System.Xml.Serialization.IXmlSerializable).Assembly.Location}""
new System.Data.DataSet();
System.Linq.Expressions.Expression.Constant(123);
System.Diagnostics.Process.GetCurrentProcess()
", options).Result;

            Assert.NotNull(process);
        }

        private static readonly Lazy<bool> s_isSystemV2AndV4Available = new Lazy<bool>(() =>
        {
            string path;
            return GlobalAssemblyCache.Instance.ResolvePartialName("System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", out path) != null &&
                   GlobalAssemblyCache.Instance.ResolvePartialName("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", out path) != null;
        });

        [Fact]
        public void References_Versioning_FxUnification1()
        {
            if (!s_isSystemV2AndV4Available.Value)
                return;

            var script = CSharpScript.Create($@"
#r ""System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089""
#r ""System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089""

System.Diagnostics.Process.GetCurrentProcess()
");
            script.GetCompilation().VerifyAssemblyVersionsAndAliases(
                "System, Version=2.0.0.0: <superseded>",
                "System, Version=4.0.0.0",
                "mscorlib, Version=4.0.0.0",
                "System.Configuration, Version=4.0.0.0: <implicit>,global",
                "System.Xml, Version=4.0.0.0: <implicit>,global",
                "System.Data.SqlXml, Version=4.0.0.0: <implicit>,global",
                "System.Security, Version=4.0.0.0: <implicit>,global",
                "System.Core, Version=4.0.0.0: <implicit>,global",
                "System.Numerics, Version=4.0.0.0: <implicit>,global",
                "System.Configuration, Version=2.0.0.0: <superseded>",
                "System.Xml, Version=2.0.0.0: <superseded>",
                "System.Data.SqlXml, Version=2.0.0.0: <superseded>",
                "System.Security, Version=2.0.0.0: <superseded>");

            Assert.NotNull(script.RunAsync().Result.ReturnValue);
        }

        [Fact]
        public void References_Versioning_FxUnification2()
        {
            if (!s_isSystemV2AndV4Available.Value)
                return;

            var script0 = CSharpScript.Create($@"
#r ""System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089""
");
            var script1 = script0.ContinueWith($@"
#r ""System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089""
");
            var script2 = script1.ContinueWith(@"
System.Diagnostics.Process.GetCurrentProcess()
");
            script0.GetCompilation().VerifyAssemblyVersionsAndAliases(
                "System, Version=2.0.0.0",
                "mscorlib, Version=4.0.0.0",
                "System.Configuration, Version=2.0.0.0: <implicit>,global",
                "System.Xml, Version=2.0.0.0: <implicit>,global",
                "System.Data.SqlXml, Version=2.0.0.0: <implicit>,global",
                "System.Security, Version=2.0.0.0: <implicit>,global");

            // TODO (https://github.com/dotnet/roslyn/issues/6456): 
            // This is not correct. "global" alias should be recursively applied on all 
            // dependencies of System, V4. The problem is in ResolveReferencedAssembly which considers
            // System, V2 equivalent to System, V4 and immediately returns, instead of checking if a better match exists.
            // This is not a problem in csc since it can't have both System, V2 and System, V4 among definitions.
            script1.GetCompilation().VerifyAssemblyVersionsAndAliases(
                "System, Version=4.0.0.0",
                "System, Version=2.0.0.0: <superseded>",
                "mscorlib, Version=4.0.0.0",
                "System.Configuration, Version=2.0.0.0: <superseded>",
                "System.Xml, Version=2.0.0.0: <superseded>",
                "System.Data.SqlXml, Version=2.0.0.0: <superseded>",
                "System.Security, Version=2.0.0.0: <superseded>",
                "System.Configuration, Version=4.0.0.0: <implicit>",
                "System.Xml, Version=4.0.0.0: <implicit>",
                "System.Data.SqlXml, Version=4.0.0.0: <implicit>",
                "System.Security, Version=4.0.0.0: <implicit>",
                "System.Core, Version=4.0.0.0: <implicit>",
                "System.Numerics, Version=4.0.0.0: <implicit>");

            // TODO (https://github.com/dotnet/roslyn/issues/6456): 
            // "global" alias should be recursively applied on all 
            script2.GetCompilation().VerifyAssemblyVersionsAndAliases(
                "System, Version=4.0.0.0",
                "System, Version=2.0.0.0: <superseded>",
                "mscorlib, Version=4.0.0.0",
                "System.Configuration, Version=2.0.0.0: <superseded>",
                "System.Xml, Version=2.0.0.0: <superseded>",
                "System.Data.SqlXml, Version=2.0.0.0: <superseded>",
                "System.Security, Version=2.0.0.0: <superseded>",
                "System.Configuration, Version=4.0.0.0: <implicit>",
                "System.Xml, Version=4.0.0.0: <implicit>",
                "System.Data.SqlXml, Version=4.0.0.0: <implicit>",
                "System.Security, Version=4.0.0.0: <implicit>",
                "System.Core, Version=4.0.0.0: <implicit>",
                "System.Numerics, Version=4.0.0.0: <implicit>");

            Assert.NotNull(script2.EvaluateAsync().Result);
        }

        [Fact]
        public void References_Versioning_StrongNames1()
        {
            var c1 = Temp.CreateFile(extension: ".dll").WriteAllBytes(TestResources.General.C1);
            var c2 = Temp.CreateFile(extension: ".dll").WriteAllBytes(TestResources.General.C2);

            var result = CSharpScript.EvaluateAsync($@"
#r ""{c1.Path}""
#r ""{c2.Path}""

new C()
").Result;

            Assert.NotNull(result);
        }

        [Fact]
        public void References_Versioning_StrongNames2()
        {
            var c1 = Temp.CreateFile(extension: ".dll").WriteAllBytes(TestResources.General.C1);
            var c2 = Temp.CreateFile(extension: ".dll").WriteAllBytes(TestResources.General.C2);

            var result = CSharpScript.Create($@"
#r ""{c1.Path}""
").ContinueWith($@"
#r ""{c2.Path}""
").ContinueWith(@"
new C()
").EvaluateAsync().Result;

            Assert.NotNull(result);
        }

        [Fact]
        public void References_Versioning_WeakNames1()
        {
            var c1 = Temp.CreateFile(extension: ".dll").WriteAllBytes(
                CreateCSharpCompilation(@"[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")] public class C {}", new[] { NetFramework.mscorlib }, assemblyName: "C").EmitToArray());

            var c2 = Temp.CreateFile(extension: ".dll").WriteAllBytes(
                CreateCSharpCompilation(@"[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")] public class C {}", new[] { NetFramework.mscorlib }, assemblyName: "C").EmitToArray());

            var result = CSharpScript.EvaluateAsync($@"
#r ""{c1.Path}""
#r ""{c2.Path}""

new C()
").Result;

            Assert.NotNull(result);
        }

        [Fact]
        public void References_Versioning_WeakNames2()
        {
            var c1 = Temp.CreateFile(extension: ".dll").WriteAllBytes(
                CreateCSharpCompilation(@"[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")] public class C {}", new[] { NetFramework.mscorlib }, assemblyName: "C").EmitToArray());

            var c2 = Temp.CreateFile(extension: ".dll").WriteAllBytes(
                CreateCSharpCompilation(@"[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")] public class C {}", new[] { NetFramework.mscorlib }, assemblyName: "C").EmitToArray());

            var result = CSharpScript.Create($@"
#r ""{c1.Path}""
").ContinueWith($@"
#r ""{c2.Path}""
").ContinueWith(@"
new C()
").EvaluateAsync().Result;

            Assert.NotNull(result);
        }

        [Fact]
        public void References_Versioning_WeakNames3()
        {
            var c1 = Temp.CreateFile(extension: ".dll").WriteAllBytes(
                CreateCSharpCompilation(@"[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")] public class C {}", new[] { NetFramework.mscorlib }, assemblyName: "C").EmitToArray());

            var c2 = Temp.CreateFile(extension: ".dll").WriteAllBytes(
                CreateCSharpCompilation(@"[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")] public class C {}", new[] { NetFramework.mscorlib }, assemblyName: "C").EmitToArray());

            var script0 = CSharpScript.Create($@"
#r ""{c1.Path}""
var c1 = new C();
");
            script0.GetCompilation().VerifyAssemblyVersionsAndAliases(
            "C, Version=1.0.0.0",
            "mscorlib, Version=4.0.0.0");

            var script1 = script0.ContinueWith($@"
#r ""{c2.Path}""
var c2 = new C();
");
            script1.GetCompilation().VerifyAssemblyVersionsAndAliases(
                "C, Version=2.0.0.0",
                "C, Version=1.0.0.0: <superseded>",
                "mscorlib, Version=4.0.0.0");

            var script2 = script1.ContinueWith(@"
c1 = c2;
");
            script2.GetCompilation().VerifyAssemblyVersionsAndAliases(
                "C, Version=2.0.0.0",
                "C, Version=1.0.0.0: <superseded>",
                "mscorlib, Version=4.0.0.0");

            DiagnosticExtensions.VerifyEmitDiagnostics(script2.GetCompilation(),
                // (2,6): error CS0029: Cannot implicitly convert type 'C [{c2.Path}]' to 'C [{c1.Path}]'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "c2").WithArguments($"C [{c2.Path}]", $"C [{c1.Path}]"));
        }

        [Fact]
        public void AssemblyResolution()
        {
            var s0 = CSharpScript.RunAsync("var x = new { a = 3 }; x");
            var s1 = s0.ContinueWith<Type>("System.Type.GetType(x.GetType().AssemblyQualifiedName, true)");
            Assert.Equal(s0.Result.ReturnValue.GetType(), s1.Result.ReturnValue);
        }

        [Fact]
        public void ReferenceToInvalidType()
        {
            var badTypeBytes = TestResources.MetadataTests.Invalid.ClassLayout;
            var badTypeRef = MetadataReference.CreateFromImage(badTypeBytes.AsImmutableOrNull());

            // TODO: enable this with our AssemblyLoader:
            Assembly handler(object _, ResolveEventArgs args)
            {
                if (args.Name.StartsWith("b,", StringComparison.Ordinal))
                {
                    return Assembly.Load(badTypeBytes);
                }

                return null;
            }

            AppDomain.CurrentDomain.AssemblyResolve += handler;
            try
            {
                var options = ScriptOptions.Default.AddReferences(badTypeRef);

                // we shouldn't throw while compiling:
                var script = CSharpScript.Create("new S1()", options);
                script.Compile();

                Assert.Throws<TypeLoadException>(() => script.EvaluateAsync().GetAwaiter().GetResult());
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= handler;
            }
        }

        public class C { public int x = 1; }

        [Fact]
        public async Task HostObjectBinding_DuplicateReferences()
        {
            var options = ScriptOptions.Default.
                AddReferences(typeof(C).Assembly, typeof(C).Assembly);

            var s0 = await CSharpScript.RunAsync<int>("x", options, new C());
            var c0 = s0.Script.GetCompilation();

            // includes corlib, host type assembly by default:
            AssertEx.Equal(new[]
            {
                typeof(object).GetTypeInfo().Assembly.Location,
                typeof(C).Assembly.Location,
                typeof(C).Assembly.Location,
                typeof(C).Assembly.Location,
            }, c0.ExternalReferences.SelectAsArray(m => m.Display));

            Assert.Equal(1, s0.ReturnValue);

            var s1 = await s0.ContinueWithAsync($@"
#r ""{typeof(C).Assembly.Location}""
#r ""{typeof(C).Assembly.Location}""
x            
");
            Assert.Equal(1, s1.ReturnValue);
        }

        [Fact]
        public async Task MissingRefrencesAutoResolution()
        {
            var portableLib = CSharpCompilation.Create(
                "PortableLib",
                new[] { SyntaxFactory.ParseSyntaxTree("public class C {}") },
                new[] { SystemRuntimePP7Ref },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var portableLibRef = portableLib.ToMetadataReference();

            var loader = new InteractiveAssemblyLoader();
            loader.RegisterDependency(Assembly.Load(portableLib.EmitToArray().ToArray()));

            var s0 = await CSharpScript.Create("new C()", options: ScriptOptions.Default.AddReferences(portableLibRef), assemblyLoader: loader).RunAsync();
            var c0 = s0.Script.GetCompilation();

            // includes corlib, host type assembly by default:
            AssertEx.Equal(new[]
            {
                typeof(object).GetTypeInfo().Assembly.Location,
                "PortableLib"
            }, c0.ExternalReferences.SelectAsArray(m => m.Display));

            // System.Runtime, 4.0.0.0 depends on all the assemblies below:
            AssertEx.Equal(new[]
            {
                "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "PortableLib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
                "System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.ComponentModel.Composition, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Data.SqlXml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Security, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                "System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
            }, c0.GetBoundReferenceManager().GetReferencedAssemblies().Select(a => a.Value.Identity.GetDisplayName()));
        }

        // https://github.com/dotnet/roslyn/issues/2246
        [Fact]
        public void HostObjectInInMemoryAssembly()
        {
            var lib = CreateCSharpCompilation("public class C { public int X = 1, Y = 2; }", new[] { NetFramework.mscorlib }, "HostLib");
            var libImage = lib.EmitToArray();
            var libRef = MetadataImageReference.CreateFromImage(libImage);

            var libAssembly = Assembly.Load(libImage.ToArray());
            var globalsType = libAssembly.GetType("C");
            var globals = Activator.CreateInstance(globalsType);

            using (var loader = new InteractiveAssemblyLoader())
            {
                loader.RegisterDependency(libAssembly);

                var script = CSharpScript.Create<int>(
                    "X+Y",
                    ScriptOptions.Default.WithReferences(libRef),
                    globalsType: globalsType,
                    assemblyLoader: loader);

                int result = script.RunAsync(globals).Result.ReturnValue;
                Assert.Equal(3, result);
            }
        }

        [Fact]
        public async Task SharedLibCopy_Identical_Weak()
        {
            string libBaseName = "LibBase_" + Guid.NewGuid();
            string lib1Name = "Lib1_" + Guid.NewGuid();
            string lib2Name = "Lib2_" + Guid.NewGuid();

            var libBase = CreateCSharpCompilation(@"
public class LibBase
{
    public readonly int X = 1;
}
", new[] { NetFramework.mscorlib }, libBaseName);

            var lib1 = CreateCSharpCompilation(@"
public class Lib1
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase.ToMetadataReference() }, lib1Name);

            var lib2 = CreateCSharpCompilation(@"
public class Lib2
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase.ToMetadataReference() }, lib2Name);

            var libBaseImage = libBase.EmitToArray();
            var lib1Image = lib1.EmitToArray();
            var lib2Image = lib2.EmitToArray();

            var root = Temp.CreateDirectory();
            var dir1 = root.CreateDirectory("1");
            var file1 = dir1.CreateFile(lib1Name + ".dll").WriteAllBytes(lib1Image);
            var fileBase1 = dir1.CreateFile(libBaseName + ".dll").WriteAllBytes(libBaseImage);

            var dir2 = root.CreateDirectory("2");
            var file2 = dir2.CreateFile(lib2Name + ".dll").WriteAllBytes(lib2Image);
            var fileBase2 = dir2.CreateFile(libBaseName + ".dll").WriteAllBytes(libBaseImage);

            var s0 = await CSharpScript.RunAsync($@"#r ""{file1.Path}""");
            var s1 = await s0.ContinueWithAsync($@"var l1 = new Lib1();");
            var s2 = await s1.ContinueWithAsync($@"#r ""{file2.Path}""");
            var s3 = await s2.ContinueWithAsync($@"var l2 = new Lib2();");
            var s4 = await s3.ContinueWithAsync($@"l2.libBase.X");

            var c4 = s4.Script.GetCompilation();
            c4.VerifyAssemblyAliases(
                lib2Name,
                lib1Name,
                "mscorlib",
                libBaseName + ": <implicit>,global");

            var libBaseRefAndSymbol = c4.GetBoundReferenceManager().GetReferencedAssemblies().ToArray()[3];
            Assert.Equal(fileBase1.Path, ((PortableExecutableReference)libBaseRefAndSymbol.Key).FilePath);
        }

        [Fact]
        public async Task SharedLibCopy_Identical_Strong()
        {
            string libBaseName = "LibBase_" + Guid.NewGuid();
            string lib1Name = "Lib1_" + Guid.NewGuid();
            string lib2Name = "Lib2_" + Guid.NewGuid();

            var libBase = CreateCSharpCompilation(@"
public class LibBase
{
    public readonly int X = 1;
}
", new[] { NetFramework.mscorlib }, libBaseName, s_signedDll);

            var lib1 = CreateCSharpCompilation(@"
public class Lib1
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase.ToMetadataReference() }, lib1Name);

            var lib2 = CreateCSharpCompilation(@"
public class Lib2
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase.ToMetadataReference() }, lib2Name);

            var libBaseImage = libBase.EmitToArray();
            var lib1Image = lib1.EmitToArray();
            var lib2Image = lib2.EmitToArray();

            var root = Temp.CreateDirectory();
            var dir1 = root.CreateDirectory("1");
            var file1 = dir1.CreateFile(lib1Name + ".dll").WriteAllBytes(lib1Image);
            var fileBase1 = dir1.CreateFile(libBaseName + ".dll").WriteAllBytes(libBaseImage);

            var dir2 = root.CreateDirectory("2");
            var file2 = dir2.CreateFile(lib2Name + ".dll").WriteAllBytes(lib2Image);
            var fileBase2 = dir2.CreateFile(libBaseName + ".dll").WriteAllBytes(libBaseImage);

            var s0 = await CSharpScript.RunAsync($@"#r ""{file1.Path}""");
            var s1 = await s0.ContinueWithAsync($@"var l1 = new Lib1();");
            var s2 = await s1.ContinueWithAsync($@"#r ""{file2.Path}""");
            var s3 = await s2.ContinueWithAsync($@"var l2 = new Lib2();");
            var s4 = await s3.ContinueWithAsync($@"l2.libBase.X");

            var c4 = s4.Script.GetCompilation();
            c4.VerifyAssemblyAliases(
                lib2Name,
                lib1Name,
                "mscorlib",
                libBaseName + ": <implicit>,global");

            var libBaseRefAndSymbol = c4.GetBoundReferenceManager().GetReferencedAssemblies().ToArray()[3];
            Assert.Equal(fileBase1.Path, ((PortableExecutableReference)libBaseRefAndSymbol.Key).FilePath);
        }

        [Fact]
        public async Task SharedLibCopy_SameVersion_Weak_DifferentContent()
        {
            string libBaseName = "LibBase_" + Guid.NewGuid();
            string lib1Name = "Lib1_" + Guid.NewGuid();
            string lib2Name = "Lib2_" + Guid.NewGuid();

            var libBase1 = CreateCSharpCompilation(@"
public class LibBase
{
    public readonly int X = 1;
}
", new[] { NetFramework.mscorlib }, libBaseName);

            var libBase2 = CreateCSharpCompilation(@"
public class LibBase
{
    public readonly int X = 2;
}
", new[] { NetFramework.mscorlib }, libBaseName);

            var lib1 = CreateCSharpCompilation(@"
public class Lib1
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase1.ToMetadataReference() }, lib1Name);

            var lib2 = CreateCSharpCompilation(@"
public class Lib2
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase1.ToMetadataReference() }, lib2Name);

            var libBase1Image = libBase1.EmitToArray();
            var libBase2Image = libBase2.EmitToArray();
            var lib1Image = lib1.EmitToArray();
            var lib2Image = lib2.EmitToArray();

            var root = Temp.CreateDirectory();
            var dir1 = root.CreateDirectory("1");
            var file1 = dir1.CreateFile(lib1Name + ".dll").WriteAllBytes(lib1Image);
            var fileBase1 = dir1.CreateFile(libBaseName + ".dll").WriteAllBytes(libBase1Image);

            var dir2 = root.CreateDirectory("2");
            var file2 = dir2.CreateFile(lib2Name + ".dll").WriteAllBytes(lib2Image);
            var fileBase2 = dir2.CreateFile(libBaseName + ".dll").WriteAllBytes(libBase2Image);

            var s0 = await CSharpScript.RunAsync($@"#r ""{file1.Path}""");
            var s1 = await s0.ContinueWithAsync($@"var l1 = new Lib1();");
            var s2 = await s1.ContinueWithAsync($@"#r ""{file2.Path}""");

            bool exceptionSeen = false;
            try
            {
                await s2.ContinueWithAsync($@"var l2 = new Lib2();");
            }
            catch (FileLoadException fileLoadEx) when (fileLoadEx.InnerException is InteractiveAssemblyLoaderException)
            {
                exceptionSeen = true;
            }

            Assert.True(exceptionSeen);
        }

        [Fact]
        public async Task SharedLibCopy_SameVersion_Strong_DifferentContent()
        {
            string libBaseName = "LibBase_" + Guid.NewGuid();
            string lib1Name = "Lib1_" + Guid.NewGuid();
            string lib2Name = "Lib2_" + Guid.NewGuid();

            var libBase1 = CreateCSharpCompilation(@"
[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]
public class LibBase
{
    public readonly int X = 1;
}
", new[] { NetFramework.mscorlib }, libBaseName, s_signedDll);

            var libBase2 = CreateCSharpCompilation(@"
[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]
public class LibBase
{
    public readonly int X = 2;
}
", new[] { NetFramework.mscorlib }, libBaseName, s_signedDll);

            var lib1 = CreateCSharpCompilation(@"
public class Lib1
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase1.ToMetadataReference() }, lib1Name);

            var lib2 = CreateCSharpCompilation(@"
public class Lib2
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase1.ToMetadataReference() }, lib2Name);

            var libBase1Image = libBase1.EmitToArray();
            var libBase2Image = libBase2.EmitToArray();
            var lib1Image = lib1.EmitToArray();
            var lib2Image = lib2.EmitToArray();

            var root = Temp.CreateDirectory();
            var dir1 = root.CreateDirectory("1");
            var file1 = dir1.CreateFile(lib1Name + ".dll").WriteAllBytes(lib1Image);
            var fileBase1 = dir1.CreateFile(libBaseName + ".dll").WriteAllBytes(libBase1Image);

            var dir2 = root.CreateDirectory("2");
            var file2 = dir2.CreateFile(lib2Name + ".dll").WriteAllBytes(lib2Image);
            var fileBase2 = dir2.CreateFile(libBaseName + ".dll").WriteAllBytes(libBase2Image);

            var s0 = await CSharpScript.RunAsync($@"#r ""{file1.Path}""");
            var s1 = await s0.ContinueWithAsync($@"new Lib1().libBase.X");
            var s2 = await s1.ContinueWithAsync($@"#r ""{file2.Path}""");

            bool exceptionSeen = false;
            try
            {
                await s2.ContinueWithAsync($@"new Lib2().libBase.X");
            }
            catch (FileLoadException fileLoadEx) when (fileLoadEx.InnerException is InteractiveAssemblyLoaderException)
            {
                exceptionSeen = true;
            }

            Assert.True(exceptionSeen);
        }

        [Fact]
        public async Task SharedLibCopy_SameVersion_StrongWeak_DifferentContent()
        {
            string libBaseName = "LibBase_" + Guid.NewGuid();
            string lib1Name = "Lib1_" + Guid.NewGuid();
            string lib2Name = "Lib2_" + Guid.NewGuid();

            var libBase1 = CreateCSharpCompilation(@"
[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]
public class LibBase
{
    public readonly int X = 1;
}
", new[] { NetFramework.mscorlib }, libBaseName, s_signedDll);

            var libBase2 = CreateCSharpCompilation(@"
[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]
public class LibBase
{
    public readonly int X = 2;
}
", new[] { NetFramework.mscorlib }, libBaseName);

            var lib1 = CreateCSharpCompilation(@"
public class Lib1
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase1.ToMetadataReference() }, lib1Name);

            var lib2 = CreateCSharpCompilation(@"
public class Lib2
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase1.ToMetadataReference() }, lib2Name);

            var libBase1Image = libBase1.EmitToArray();
            var libBase2Image = libBase2.EmitToArray();
            var lib1Image = lib1.EmitToArray();
            var lib2Image = lib2.EmitToArray();

            var root = Temp.CreateDirectory();
            var dir1 = root.CreateDirectory("1");
            var file1 = dir1.CreateFile(lib1Name + ".dll").WriteAllBytes(lib1Image);
            var fileBase1 = dir1.CreateFile(libBaseName + ".dll").WriteAllBytes(libBase1Image);

            var dir2 = root.CreateDirectory("2");
            var file2 = dir2.CreateFile(lib2Name + ".dll").WriteAllBytes(lib2Image);
            var fileBase2 = dir2.CreateFile(libBaseName + ".dll").WriteAllBytes(libBase2Image);

            var s0 = await CSharpScript.RunAsync($@"#r ""{file1.Path}""");
            var s1 = await s0.ContinueWithAsync($@"new Lib1().libBase.X");
            var s2 = await s1.ContinueWithAsync($@"#r ""{file2.Path}""");

            bool exceptionSeen = false;
            try
            {
                await s2.ContinueWithAsync($@"new Lib2().libBase.X");
            }
            catch (FileLoadException fileLoadEx) when (fileLoadEx.InnerException is InteractiveAssemblyLoaderException)
            {
                exceptionSeen = true;
            }

            Assert.True(exceptionSeen);
        }

        [Fact]
        public async Task SharedLibCopy_SameVersion_StrongDifferentPKT_DifferentContent()
        {
            string libBaseName = "LibBase_" + Guid.NewGuid();
            string lib1Name = "Lib1_" + Guid.NewGuid();
            string lib2Name = "Lib2_" + Guid.NewGuid();

            var libBase1 = CreateCSharpCompilation(@"
[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]
public class LibBase
{
    public readonly int X = 1;
}
", new[] { NetFramework.mscorlib }, libBaseName, s_signedDll);

            var libBase2 = CreateCSharpCompilation(@"
[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]
public class LibBase
{
    public readonly int X = 2;
}
", new[] { NetFramework.mscorlib }, libBaseName, s_signedDll2);

            var lib1 = CreateCSharpCompilation(@"
public class Lib1
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase1.ToMetadataReference() }, lib1Name);

            var lib2 = CreateCSharpCompilation(@"
public class Lib2
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase1.ToMetadataReference() }, lib2Name);

            var libBase1Image = libBase1.EmitToArray();
            var libBase2Image = libBase2.EmitToArray();
            var lib1Image = lib1.EmitToArray();
            var lib2Image = lib2.EmitToArray();

            var root = Temp.CreateDirectory();
            var dir1 = root.CreateDirectory("1");
            var file1 = dir1.CreateFile(lib1Name + ".dll").WriteAllBytes(lib1Image);
            var fileBase1 = dir1.CreateFile(libBaseName + ".dll").WriteAllBytes(libBase1Image);

            var dir2 = root.CreateDirectory("2");
            var file2 = dir2.CreateFile(lib2Name + ".dll").WriteAllBytes(lib2Image);
            var fileBase2 = dir2.CreateFile(libBaseName + ".dll").WriteAllBytes(libBase2Image);

            var s0 = await CSharpScript.RunAsync($@"#r ""{file1.Path}""");
            var s1 = await s0.ContinueWithAsync($@"new Lib1().libBase.X");
            var s2 = await s1.ContinueWithAsync($@"#r ""{file2.Path}""");

            bool exceptionSeen = false;
            try
            {
                await s2.ContinueWithAsync($@"new Lib2().libBase.X");
            }
            catch (FileLoadException fileLoadEx) when (fileLoadEx.InnerException is InteractiveAssemblyLoaderException)
            {
                exceptionSeen = true;
            }

            Assert.True(exceptionSeen);
        }

        [Fact]
        public async Task SharedLibCopy_DifferentVersion_Weak()
        {
            string libBaseName = "LibBase_" + Guid.NewGuid();
            string lib1Name = "Lib1_" + Guid.NewGuid();
            string lib2Name = "Lib2_" + Guid.NewGuid();

            var libBase1 = CreateCSharpCompilation(@"
[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]
public class LibBase
{
    public readonly int X = 1;
}
", new[] { NetFramework.mscorlib }, libBaseName);

            var libBase2 = CreateCSharpCompilation(@"
[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")]
public class LibBase
{
    public readonly int X = 2;
}
", new[] { NetFramework.mscorlib }, libBaseName);

            var lib1 = CreateCSharpCompilation(@"
public class Lib1
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase1.ToMetadataReference() }, lib1Name);

            var lib2 = CreateCSharpCompilation(@"
public class Lib2
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase2.ToMetadataReference() }, lib2Name);

            var libBase1Image = libBase1.EmitToArray();
            var libBase2Image = libBase2.EmitToArray();
            var lib1Image = lib1.EmitToArray();
            var lib2Image = lib2.EmitToArray();

            var root = Temp.CreateDirectory();
            var dir1 = root.CreateDirectory("1");
            var file1 = dir1.CreateFile(lib1Name + ".dll").WriteAllBytes(lib1Image);
            var fileBase1 = dir1.CreateFile(libBaseName + ".dll").WriteAllBytes(libBase1Image);

            var dir2 = root.CreateDirectory("2");
            var file2 = dir2.CreateFile(lib2Name + ".dll").WriteAllBytes(lib2Image);
            var fileBase2 = dir2.CreateFile(libBaseName + ".dll").WriteAllBytes(libBase2Image);

            var s0 = await CSharpScript.RunAsync($@"#r ""{file1.Path}""");
            var s1 = await s0.ContinueWithAsync($@"var l1 = new Lib1().libBase.X;");
            var s2 = await s1.ContinueWithAsync($@"#r ""{file2.Path}""");

            bool exceptionSeen = false;
            try
            {
                await s2.ContinueWithAsync($@"var l2 = new Lib2().libBase.X;");
            }
            catch (FileLoadException fileLoadEx) when (fileLoadEx.InnerException is InteractiveAssemblyLoaderException)
            {
                exceptionSeen = true;
            }

            Assert.True(exceptionSeen);
        }

        [Fact]
        public async Task SharedLibCopy_DifferentVersion_Strong()
        {
            string libBaseName = "LibBase_" + Guid.NewGuid();
            string lib1Name = "Lib1_" + Guid.NewGuid();
            string lib2Name = "Lib2_" + Guid.NewGuid();

            var libBase1 = CreateCSharpCompilation(@"
[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]
public class LibBase
{
    public readonly int X = 1;
}
", new[] { NetFramework.mscorlib }, libBaseName, s_signedDll);

            var libBase2 = CreateCSharpCompilation(@"
[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")]
public class LibBase
{
    public readonly int X = 2;
}
", new[] { NetFramework.mscorlib }, libBaseName, s_signedDll);

            var lib1 = CreateCSharpCompilation(@"
public class Lib1
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase1.ToMetadataReference() }, lib1Name);

            var lib2 = CreateCSharpCompilation(@"
public class Lib2
{
    public LibBase libBase = new LibBase();
}
", new MetadataReference[] { NetFramework.mscorlib, libBase2.ToMetadataReference() }, lib2Name);

            var libBase1Image = libBase1.EmitToArray();
            var libBase2Image = libBase2.EmitToArray();
            var lib1Image = lib1.EmitToArray();
            var lib2Image = lib2.EmitToArray();

            var root = Temp.CreateDirectory();
            var dir1 = root.CreateDirectory("1");
            var file1 = dir1.CreateFile(lib1Name + ".dll").WriteAllBytes(lib1Image);
            var fileBase1 = dir1.CreateFile(libBaseName + ".dll").WriteAllBytes(libBase1Image);

            var dir2 = root.CreateDirectory("2");
            var file2 = dir2.CreateFile(lib2Name + ".dll").WriteAllBytes(lib2Image);
            var fileBase2 = dir2.CreateFile(libBaseName + ".dll").WriteAllBytes(libBase2Image);

            var s0 = await CSharpScript.RunAsync($@"#r ""{file1.Path}""");
            var s1 = await s0.ContinueWithAsync($@"new Lib1().libBase.X");
            Assert.Equal(1, s1.ReturnValue);
            var s2 = await s1.ContinueWithAsync($@"#r ""{file2.Path}""");
            var s3 = await s2.ContinueWithAsync($@"new Lib2().libBase.X");
            Assert.Equal(2, s3.ReturnValue);
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
                new[] { NetFramework.mscorlib, NetFramework.System },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var libFile = Temp.CreateFile("lib").WriteAllBytes(lib.EmitToArray());

            var s0 = await CSharpScript.RunAsync("C c;", ScriptOptions.Default.WithReferences(libFile.Path));
            await s0.ContinueWithAsync("c = new C()");
        }
    }
}
