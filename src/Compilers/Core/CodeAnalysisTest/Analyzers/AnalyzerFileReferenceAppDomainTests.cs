// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET472 // AppDomains

using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.Desktop;
using Xunit;
using Basic.Reference.Assemblies;
using System.Numerics;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class RemoteAnalyzerFileReferenceTest : MarshalByRefObject
    {
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public Exception LoadAnalyzer(string shadowPath, string analyzerPath)
        {
            var loader = DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(shadowPath);
            Exception analyzerLoadException = null;
            var analyzerRef = new AnalyzerFileReference(analyzerPath, loader);
            analyzerRef.AnalyzerLoadFailed += (s, e) => analyzerLoadException = e.Exception;
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            analyzerRef.AddAnalyzers(builder, LanguageNames.CSharp);
            return analyzerLoadException;
        }
    }

    public class AnalyzerFileReferenceAppDomainTests : TestBase
    {
        [Fact]
        public void TestAnalyzerLoading_AppDomain()
        {
            var dir = Temp.CreateDirectory();
            dir.CopyFile(typeof(AppDomainUtils).Assembly.Location);
            dir.CopyFile(typeof(RemoteAnalyzerFileReferenceTest).Assembly.Location);
            dir.CopyFile(typeof(Vector).Assembly.Location);
            var analyzerFile = DesktopTestHelpers.CreateCSharpAnalyzerAssemblyWithTestAnalyzer(dir, "MyAnalyzer");
            var loadDomain = AppDomainUtils.Create("AnalyzerTestDomain", basePath: dir.Path);
            try
            {
                // Test analyzer load success.
                var remoteTest = (RemoteAnalyzerFileReferenceTest)loadDomain.CreateInstanceAndUnwrap(typeof(RemoteAnalyzerFileReferenceTest).Assembly.FullName, typeof(RemoteAnalyzerFileReferenceTest).FullName);
                var exception = remoteTest.LoadAnalyzer(dir.CreateDirectory("shadow").Path, analyzerFile.Path);
                Assert.Null(exception);
            }
            finally
            {
                AppDomain.Unload(loadDomain);
            }
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/mono/mono/issues/10960")]
        public void TestAnalyzerLoading_Error()
        {
            var analyzerSource = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Runtime.InteropServices;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
[StructLayout(LayoutKind.Sequential, Size = 10000000)]
public class TestAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
    public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
}";

            var dir = Temp.CreateDirectory();

            dir.CopyFile(typeof(System.Reflection.Metadata.MetadataReader).Assembly.Location);
            dir.CopyFile(typeof(AppDomainUtils).Assembly.Location);
            dir.CopyFile(typeof(Memory<>).Assembly.Location);
            dir.CopyFile(typeof(System.Runtime.CompilerServices.Unsafe).Assembly.Location);
            var immutable = dir.CopyFile(typeof(ImmutableArray).Assembly.Location);
            var analyzer = dir.CopyFile(typeof(DiagnosticAnalyzer).Assembly.Location);
            dir.CopyFile(typeof(RemoteAnalyzerFileReferenceTest).Assembly.Location);
            dir.CopyFile(typeof(Vector).Assembly.Location);

            var analyzerCompilation = CSharp.CSharpCompilation.Create(
                "MyAnalyzer",
                new SyntaxTree[] { CSharp.SyntaxFactory.ParseSyntaxTree(analyzerSource) },
                new MetadataReference[]
                {
                    NetStandard20.References.mscorlib,
                    NetStandard20.References.netstandard,
                    NetStandard20.References.SystemRuntime,
                    MetadataReference.CreateFromFile(immutable.Path),
                    MetadataReference.CreateFromFile(analyzer.Path)
                },
                new CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, warningLevel: CodeAnalysis.Diagnostic.MaxWarningLevel));

            var analyzerFile = dir.CreateFile("MyAnalyzer.dll").WriteAllBytes(analyzerCompilation.EmitToArray());

            var loadDomain = AppDomainUtils.Create("AnalyzerTestDomain", basePath: dir.Path);
            try
            {
                // Test analyzer load failure.
                var remoteTest = (RemoteAnalyzerFileReferenceTest)loadDomain.CreateInstanceAndUnwrap(typeof(RemoteAnalyzerFileReferenceTest).Assembly.FullName, typeof(RemoteAnalyzerFileReferenceTest).FullName);
                var exception = remoteTest.LoadAnalyzer(dir.CreateDirectory("shadow").Path, analyzerFile.Path);
                Assert.NotNull(exception as TypeLoadException);
            }
            finally
            {
                AppDomain.Unload(loadDomain);
            }
        }

        private static Assembly OnResolve(object sender, ResolveEventArgs e)
        {
            Console.WriteLine($"Resolve in {AppDomain.CurrentDomain.Id} for {e.Name}");
            return null;
        }
    }
}
#endif
