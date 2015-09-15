// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting.CSharp;
using Microsoft.CodeAnalysis.Scripting.Test;
using Roslyn.Test.Utilities;
using Xunit;

#pragma warning disable RS0003 // Do not directly await a Task

namespace Microsoft.CodeAnalysis.Scripting.CSharpTest
{
    public class InteractiveSessionTests : TestBase
    {
        [Fact]
        public async void CompilationChain_GlobalImportsRebinding()
        {
            var options = ScriptOptions.Default.AddNamespaces("System.Diagnostics");

            var s0 = await CSharpScript.RunAsync("", options);

            ScriptingTestHelpers.AssertCompilationError(s0, @"Process.GetCurrentProcess()",
                // (2,1): error CS0103: The name 'Process' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Process").WithArguments("Process"));

            var s1 = s0.ContinueWithAsync($"#r \"{typeof(Process).Assembly.Location}\"");
            var s2 = s1.ContinueWith("Process.GetCurrentProcess()");

            Assert.NotNull(s2.Result);
        }

        [Fact]
        public async void CompilationChain_UsingRebinding_AddReference()
        {
            var s0 = await CSharpScript.RunAsync("using System.Diagnostics;");

            var newOptions = s0.Script.Options.AddReferences(typeof(Process).Assembly);

            var s1 = s0.ContinueWithAsync(@"Process.GetCurrentProcess()", newOptions);

            Assert.NotNull(s1.Result);
        }

        [Fact]
        public async void CompilationChain_UsingRebinding_Directive()
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
            var options = ScriptOptions.Default.WithDefaultMetadataResolution(RuntimeEnvironment.GetRuntimeDirectory());

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
        public async void SearchPaths_BaseDirectory()
        {
            var options = ScriptOptions.Default.
                WithCustomMetadataResolution(new TestMetadataReferenceResolver(
                    pathResolver: new VirtualizedRelativePathResolver(existingFullPaths: new[] { @"C:\dir\x.dll" }, baseDirectory: @"C:\foo\bar"),
                    files: new Dictionary<string, PortableExecutableReference> { { @"C:\dir\x.dll", (PortableExecutableReference)SystemCoreRef } }));

            var script = CSharpScript.Create(@"
#r ""x.dll""
using System.Linq;

var x = from a in new[] { 1, 2 ,3 } select a + 1;
", options.WithPath(@"C:\dir\a.csx").WithIsInteractive(false));

            var state = await script.RunAsync().ContinueWith<IEnumerable<int>>("x", options.WithPath(null).WithIsInteractive(true));

            AssertEx.Equal(new[] { 2, 3, 4 }, state.ReturnValue);
        }

        [Fact]
        public async void References1()
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
new System.Windows.Forms.Form();
", options3);

            Assert.NotNull(s3.ReturnValue);
        }

        [Fact]
        public void References2()
        {
            var options = ScriptOptions.Default.
                WithDefaultMetadataResolution(RuntimeEnvironment.GetRuntimeDirectory()).
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

        [Fact]
        public void MissingDependency()
        {
            var source = @"
#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""

using System.Windows;
System.Collections.IEnumerable w = new Window();
";

            ScriptingTestHelpers.AssertCompilationError(() => CSharpScript.EvaluateAsync(source),
                // (7,36): error CS0012: The type 'System.ComponentModel.ISupportInitialize' is defined in an assembly that is not referenced. You must add a reference to assembly 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'.
                // System.Collections.IEnumerable w = new Window();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "new Window()").WithArguments("System.ComponentModel.ISupportInitialize", "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                // (7,36): error CS0012: The type 'System.Windows.Markup.IQueryAmbient' is defined in an assembly that is not referenced. You must add a reference to assembly 'System.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'.
                // System.Collections.IEnumerable w = new Window();
                Diagnostic(ErrorCode.ERR_NoTypeDef, "new Window()").WithArguments("System.Windows.Markup.IQueryAmbient", "System.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                // (7,36): error CS0266: Cannot implicitly convert type 'System.Windows.Window' to 'System.Collections.IEnumerable'. An explicit conversion exists (are you missing a cast?)
                // System.Collections.IEnumerable w = new Window();
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "new Window()").WithArguments("System.Windows.Window", "System.Collections.IEnumerable"));
        }

        [WorkItem(529637)]
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
                script.Build();

                Assert.Throws<TypeLoadException>(() => script.EvaluateAsync().GetAwaiter().GetResult());
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= handler;
            }
        }

        public class C { public int x = 1; }

        [Fact]
        public async void HostObjectBinding_DuplicateReferences()
        {
            var options = ScriptOptions.Default.
                AddReferences(typeof(C).Assembly, typeof(C).Assembly);

            var s0 = await CSharpScript.RunAsync<int>("x", options, new C());

            // includes corlib, host type assembly by default:
            AssertEx.Equal(new[] 
            {
                typeof(object).GetTypeInfo().Assembly.Location,
                typeof(C).Assembly.Location,
                typeof(C).Assembly.Location,
                typeof(C).Assembly.Location,
            }, s0.Script.GetCompilation().ExternalReferences.SelectAsArray(m => m.Display));

            Assert.Equal(1, s0.ReturnValue);

            var s1 = await s0.ContinueWithAsync($@"
#r ""{typeof(C).Assembly.Location}""
#r ""{typeof(C).Assembly.Location}""
x            
");
            Assert.Equal(1, s1.ReturnValue);
        }
    }
}
