// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting.CSharp;
using Microsoft.CodeAnalysis.Scripting.Test;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Scripting.CSharpTest
{
    public class InteractiveSessionTests : TestBase
    {
        private class TestMetadataReferenceProvider : Microsoft.CodeAnalysis.MetadataFileReferenceProvider
        {
            public Func<DocumentationProvider> MakeDocumentationProvider;
            private readonly Dictionary<string, AssemblyMetadata> _cache = new Dictionary<string, AssemblyMetadata>();

            public override PortableExecutableReference GetReference(string fullPath, MetadataReferenceProperties properties = default(MetadataReferenceProperties))
            {
                AssemblyMetadata metadata;
                if (_cache.TryGetValue(fullPath, out metadata))
                {
                    return metadata.GetReference(MakeDocumentationProvider());
                }

                _cache.Add(fullPath, metadata = AssemblyMetadata.CreateFromFile(fullPath));
                return metadata.GetReference(MakeDocumentationProvider());
            }
        }

        [Fact]
        public void CompilationChain_GlobalImportsRebinding()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.ImportNamespace("System.Diagnostics");

            ScriptingTestHelpers.AssertCompilationError(session, @"
Process.GetCurrentProcess()",
                // (2,1): error CS0103: The name 'Process' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Process").WithArguments("Process"));

            session.Execute(@"
#r """ + typeof(Process).Assembly.Location + @"""");

            session.Execute(@"
Process.GetCurrentProcess()");
        }

        [Fact]
        public void CompilationChain_UsingRebinding_AddReference()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"using System.Diagnostics;");

            session.AddReference(typeof(Process).Assembly);

            session.Execute(@"Process.GetCurrentProcess()");
        }

        [Fact]
        public void CompilationChain_UsingRebinding_Directive()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"using System.Diagnostics;");

            session.Execute(@"
#r """ + typeof(Process).Assembly.Location + @"""
Process.GetCurrentProcess()");
        }

        //
        // General rule for symbol lookup: 
        //
        // Declaration A in submission S hides declaration B in submission T iff
        // S precedes T, and A and B can't coexist in the same scope.

        [Fact]
        public void CompilationChain_SubmissionSlots()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.Execute("using System;");
            session.Execute("using static System.Environment;");
            session.Execute("int x; x = 1;");
            session.Execute("using static System.Math;");
            session.Execute("int foo(int a) { return a + 1; } ");

#if false
            Assert.True(session.executionState.submissions.Length >= 2, "Expected two submissions");
            session.executionState.submissions.Aggregate(0, (i, sub) => { Assert.Equal(i < 2, sub != null); return i + 1; });
#endif
            object result;

            // TODO (tomat): Version is a type and property, but we are not looking for a type, so can we disambiguate?
            ScriptingTestHelpers.AssertCompilationError(session, "Version",
                // (1,1): error CS0229: Ambiguity between 'System.Version' and 'System.Environment.Version'
                Diagnostic(ErrorCode.ERR_AmbigMember, "Version").WithArguments("System.Version", "System.Environment.Version")
            );

            result = session.Execute("new System.Collections.Generic.List<Version>()");
            Assert.True(result is List<Version>, "Expected List<Version>");

            result = session.Execute("Environment.Version");
            Assert.Equal(Environment.Version, result);

            result = session.Execute("foo(x)");
            Assert.Equal(2, result);

            result = session.Execute("Sin(0)");
            Assert.Equal(0.0, result);
        }

        // Simulates a sensible override of object.Equals.
        private class TestDocumentationProviderEquals : DocumentationProvider
        {
            protected internal override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default(CancellationToken))
            {
                return "";
            }

            public override bool Equals(object obj)
            {
                return obj != null && this.GetType() == obj.GetType();
            }

            public override int GetHashCode()
            {
                return GetType().GetHashCode();
            }
        }

        // Simulates no override of object.Equals.
        private class TestDocumentationProviderNoEquals : DocumentationProvider
        {
            protected internal override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture, CancellationToken cancellationToken = default(CancellationToken))
            {
                return "";
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj);
            }

            public override int GetHashCode()
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
            }
        }

        [WorkItem(546173)]
        [Fact]
        public void CompilationChain_SystemObject_OrderDependency1()
        {
            CompilationChain_SystemObject_NotEquals();
            CompilationChain_SystemObject_Equals();
        }

        [WorkItem(546173)]
        [Fact]
        public void CompilationChain_SystemObject_OrderDependency2()
        {
            CompilationChain_SystemObject_Equals();
            CompilationChain_SystemObject_NotEquals();
        }

        [WorkItem(545665)]
        [Fact]
        public void CompilationChain_SystemObject_NotEquals()
        {
            // As in VS/ETA, make a new list of references for each submission.

            var options = new CSharpParseOptions(kind: SourceCodeKind.Interactive, documentationMode: DocumentationMode.None);
            var provider = new TestMetadataReferenceProvider() { MakeDocumentationProvider = () => new TestDocumentationProviderNoEquals() };

            var s1 = CSharpCompilation.CreateSubmission("s1.dll",
                                                  syntaxTree: SyntaxFactory.ParseSyntaxTree("struct S { }", options),
                                                  references: MakeReferencesViaCommandLine(provider),
                                                  returnType: typeof(object));
            s1.GetDiagnostics().Verify();

            var s2 = CSharpCompilation.CreateSubmission("s2.dll",
                                                  syntaxTree: SyntaxFactory.ParseSyntaxTree("System.Collections.IEnumerable Iterator() { yield return new S(); }", options),
                                                  previousSubmission: s1,
                                                  references: MakeReferencesViaCommandLine(provider),
                                                  returnType: typeof(object));

            Assert.NotEqual(s1.GetSpecialType(SpecialType.System_Object), s2.GetSpecialType(SpecialType.System_Object));

            s2.GetDiagnostics().Verify(
                // (1,58): error CS0029: Cannot implicitly convert type 'S' to 'object'
                // System.Collections.IEnumerable Iterator() { yield return new S(); }
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new S()").WithArguments("S", "object"));
        }

        [WorkItem(545665)]
        [Fact]
        public void CompilationChain_SystemObject_Equals()
        {
            // As in VS/ETA, make a new list of references for each submission.

            var options = new CSharpParseOptions(kind: SourceCodeKind.Interactive, documentationMode: DocumentationMode.None);
            var provider = new TestMetadataReferenceProvider() { MakeDocumentationProvider = () => new TestDocumentationProviderEquals() };

            var s1 = CSharpCompilation.CreateSubmission("s1.dll",
                syntaxTree: SyntaxFactory.ParseSyntaxTree("struct S { }", options),
                references: MakeReferencesViaCommandLine(provider),
                returnType: typeof(object));
            s1.GetDiagnostics().Verify();

            var s2 = CSharpCompilation.CreateSubmission("s2.dll",
                syntaxTree: SyntaxFactory.ParseSyntaxTree("System.Collections.IEnumerable Iterator() { yield return new S(); }", options),
                previousSubmission: s1,
                references: MakeReferencesViaCommandLine(provider),
                returnType: typeof(object));

            s2.GetDiagnostics().Verify();

            Assert.Equal(s1.GetSpecialType(SpecialType.System_Object), s2.GetSpecialType(SpecialType.System_Object));
        }

        /// <summary>
        /// NOTE: We're going through the command line parser to mimic the approach of visual studio and the ETA.
        /// Crucially, this CommandLineArguments will use the provided TestMetadataReferenceProvider to attach a fresh
        /// DocumentationProvider to each reference.
        /// </summary>
        private static IEnumerable<MetadataReference> MakeReferencesViaCommandLine(TestMetadataReferenceProvider metadataReferenceProvider)
        {
            var commandLineArguments = CSharpCommandLineParser.Interactive.Parse(
                new[] { "/r:" + typeof(Script).Assembly.Location }, //get corlib by default
                Directory.GetDirectoryRoot("."), //NOTE: any absolute path will do - we're not going to use this.
                RuntimeEnvironment.GetRuntimeDirectory());
            var references = commandLineArguments.ResolveMetadataReferences(new AssemblyReferenceResolver(MetadataFileReferenceResolver.Default, metadataReferenceProvider));
            return references;
        }


        [Fact]
        public void SearchPaths1()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.SetReferenceSearchPaths(RuntimeEnvironment.GetRuntimeDirectory());

            object result = session.Execute(@"
#r ""System.Data.dll""
#r ""System""
#r """ + typeof(System.Xml.Serialization.IXmlSerializable).GetTypeInfo().Assembly.Location + @"""
new System.Data.DataSet()
");

            Assert.True(result is System.Data.DataSet, "Expected DataSet");
        }

        /// <summary>
        /// Default search paths can be removed.
        /// </summary>
        [Fact]
        public void SearchPaths_RemoveDefault()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            // remove default paths:
            session.SetReferenceSearchPaths();

            ScriptingTestHelpers.AssertCompilationError(session, @"
#r ""System.Data.dll""
new System.Data.DataSet()
",
                // (2,1): error CS0006: Metadata file 'System.Data.dll' could not be found
                // #r "System.Data.dll"
                Diagnostic(ErrorCode.ERR_NoMetadataFile, @"#r ""System.Data.dll""").WithArguments("System.Data.dll"),
                // (3,12): error CS0234: The type or namespace name 'Data' does not exist in the namespace 'System' (are you missing an assembly reference?)
                // new System.Data.DataSet()
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Data").WithArguments("Data", "System")
            );
        }

        private class MetadataReferenceProvider : Microsoft.CodeAnalysis.MetadataFileReferenceProvider
        {
            private readonly Dictionary<string, PortableExecutableReference> _metadata;

            public MetadataReferenceProvider(Dictionary<string, PortableExecutableReference> metadata)
            {
                _metadata = metadata;
                metadata.Add(typeof(object).Assembly.Location, (PortableExecutableReference)MscorlibRef);
            }

            public override PortableExecutableReference GetReference(string fullPath, MetadataReferenceProperties properties = default(MetadataReferenceProperties))
            {
                return _metadata[fullPath];
            }
        }

        /// <summary>
        /// Look at base directory (or directory containing #r) before search paths.
        /// </summary>
        [Fact]
        public void SearchPaths_BaseDirectory()
        {
            var engine = new CSharpScriptEngine(new MetadataReferenceProvider(new Dictionary<string, PortableExecutableReference>
            {
                { @"C:\dir\x.dll", (PortableExecutableReference)SystemCoreRef }
            }));

            engine.MetadataReferenceResolver = new VirtualizedFileReferenceResolver(
                existingFullPaths: new[]
                {
                    @"C:\dir\x.dll"
                },
                baseDirectory: @"C:\foo\bar"
            );

            var session = engine.CreateSession();

            var source = @"
#r ""x.dll""
using System.Linq;

var x = from a in new[] { 1,2,3 }
        select a + 1;
";

            var submission = session.CompileSubmission<object>(source, @"C:\dir\a.csx", isInteractive: false);
            submission.Execute();
        }

        [Fact]
        public void References1()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.AddReference(typeof(Process).Assembly.FullName);
            session.AddReference(typeof(System.Linq.Expressions.Expression).Assembly);

            var process = (Process)session.Execute(@"
#r """ + typeof(System.Data.DataSet).Assembly.Location + @"""
#r ""System""
#r """ + typeof(System.Xml.Serialization.IXmlSerializable).Assembly.Location + @"""
new System.Data.DataSet();
System.Linq.Expressions.Expression.Constant(123);
System.Diagnostics.Process.GetCurrentProcess()
");

            Assert.NotNull(process);

            session.AddReference(typeof(System.Xml.XmlDocument).Assembly);

            var xmlDoc = (System.Xml.XmlDocument)session.Execute(@"
new System.Xml.XmlDocument()
");

            Assert.NotNull(xmlDoc);

            session.AddReference("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");

            var color = session.Execute(@"
System.Drawing.Color.Coral
");

            Assert.NotNull(color);

            session.AddReference(typeof(System.Windows.Forms.Form).Assembly.Location);

            var form = (System.Windows.Forms.Form)session.Execute(@"
new System.Windows.Forms.Form();
");

            Assert.NotNull(form);
        }

        [Fact]
        public void References2()
        {
            var engine = new CSharpScriptEngine();

            engine.SetReferenceSearchPaths(RuntimeEnvironment.GetRuntimeDirectory());

            engine.AddReference("System.Core");
            engine.AddReference("System.dll");
            engine.AddReference(typeof(System.Data.DataSet).Assembly);

            var session = engine.CreateSession();

            var process = (Process)session.Execute(@"
#r """ + typeof(System.Xml.Serialization.IXmlSerializable).Assembly.Location + @"""
new System.Data.DataSet();
System.Linq.Expressions.Expression.Constant(123);
System.Diagnostics.Process.GetCurrentProcess()
");

            Assert.NotNull(process);
        }

        [Fact]
        public void MissingDependency()
        {
            var engine = new CSharpScriptEngine();

            ScriptingTestHelpers.AssertCompilationError(engine, @"
#r ""WindowsBase""
#r ""PresentationCore""
#r ""PresentationFramework""

using System.Windows;
System.Collections.IEnumerable w = new Window();
",
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
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            var instance = session.Execute("var x = new { a = 3 }; x");
            var type = session.Execute("System.Type.GetType(x.GetType().AssemblyQualifiedName, true)");
            Assert.Equal(instance.GetType(), type);
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
                var engine = new CSharpScriptEngine();
                var session = engine.CreateSession();
                session.AddReference(badTypeRef);

                // we shouldn't throw while compiling:
                var submission = session.CompileSubmission<object>("new S1()");

                // we should throw while executing:
                Assert.Throws<TypeLoadException>(() => submission.Execute());
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= handler;
            }
        }

        public class C { public int x = 1; }

        [Fact]
        public void HostObjectBinding_DuplicateReferences()
        {
            var engine = new CSharpScriptEngine();
            var c = new C();
            var session = engine.CreateSession(c);

            session.AddReference(typeof(C).Assembly);
            session.AddReference(typeof(C).Assembly);

            // includes mscorlib
            Assert.Equal(3, session.References.Length);

            int result = session.Execute<int>("x");
            Assert.Equal(1, result);

            int result2 = session.Execute<int>(@"
#r """ + typeof(C).Assembly.Location + @"""
#r """ + typeof(C).Assembly.Location + @"""
x            
");

            Assert.Equal(1, result);
        }

        [WorkItem(541166)]
        [Fact]
        public void DefineExtensionMethods()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.AddReference(TestReferences.NetFx.v4_0_30319.System_Core);

            // No error for extension method defined in interactive session.
            session.Execute("static void E(this object o) { }");

            ScriptingTestHelpers.AssertCompilationError(session, "void F(this object o) { }",
                // (1,6): error CS1105: Extension method must be static
                // void F(this object o) { }
                Diagnostic(ErrorCode.ERR_BadExtensionMeth, "F").WithLocation(1, 6));

            ScriptingTestHelpers.AssertCompilationError(session, "static void G(this dynamic o) { }",
                // error CS1103: The first parameter of an extension method cannot be of type 'dynamic'
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "dynamic").WithArguments("dynamic"));
        }
    }
}
