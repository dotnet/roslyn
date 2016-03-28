// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias PortableTestUtils;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Test;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using TestBase = PortableTestUtils::Roslyn.Test.Utilities.TestBase;
using AssertEx = PortableTestUtils::Roslyn.Test.Utilities.AssertEx;

#pragma warning disable RS0003 // Do not directly await a Task

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Test
{
    using static TestCompilationFactory;
    using DiagnosticExtensions = PortableTestUtils::Microsoft.CodeAnalysis.DiagnosticExtensions;

    public class InteractiveSessionTests : TestBase
    {
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
                ContinueWith("int foo(int a) { return a + 1; } ");

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

            s = s.ContinueWith("foo(x)");
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
                    pathResolver: new VirtualizedRelativePathResolver(existingFullPaths: new[] { @"C:\dir\x.dll" }, baseDirectory: @"C:\foo\bar"),
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

        private static Lazy<bool> s_isSystemV2AndV4Available = new Lazy<bool>(() =>
        {
            string path;
            return GlobalAssemblyCache.Instance.ResolvePartialName("System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", out path) != null &&
                   GlobalAssemblyCache.Instance.ResolvePartialName("System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", out path) != null;
        });

        [Fact]
        public void References_Versioning_FxUnification1()
        {
            if (!s_isSystemV2AndV4Available.Value) return;

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
            if (!s_isSystemV2AndV4Available.Value) return;

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
            var c1 = Temp.CreateFile(extension: ".dll").WriteAllBytes(CreateCSharpCompilationWithMscorlib(@"[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")] public class C {}", assemblyName: "C").EmitToArray());
            var c2 = Temp.CreateFile(extension: ".dll").WriteAllBytes(CreateCSharpCompilationWithMscorlib(@"[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")] public class C {}", assemblyName: "C").EmitToArray());

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
            var c1 = Temp.CreateFile(extension: ".dll").WriteAllBytes(CreateCSharpCompilationWithMscorlib(@"[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")] public class C {}", assemblyName: "C").EmitToArray());
            var c2 = Temp.CreateFile(extension: ".dll").WriteAllBytes(CreateCSharpCompilationWithMscorlib(@"[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")] public class C {}", assemblyName: "C").EmitToArray());

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
            var c1 = Temp.CreateFile(extension: ".dll").WriteAllBytes(CreateCSharpCompilationWithMscorlib(@"[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")] public class C {}", assemblyName: "C").EmitToArray());
            var c2 = Temp.CreateFile(extension: ".dll").WriteAllBytes(CreateCSharpCompilationWithMscorlib(@"[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")] public class C {}", assemblyName: "C").EmitToArray());

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
            ResolveEventHandler handler = (_, args) =>
            {
                if (args.Name.StartsWith("b,", StringComparison.Ordinal))
                {
                    return Assembly.Load(badTypeBytes);
                }

                return null;
            };

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
            var lib = CreateCSharpCompilationWithMscorlib("public class C { public int X = 1, Y = 2; }", "HostLib");
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
    }
}
