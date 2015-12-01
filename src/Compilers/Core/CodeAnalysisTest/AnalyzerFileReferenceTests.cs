// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class FromFileLoader : IAnalyzerAssemblyLoader
    {
        public static FromFileLoader Instance = new FromFileLoader();

        public void AddDependencyLocation(string fullPath)
        {
        }

        public Assembly LoadFromPath(string fullPath)
        {
            return Assembly.LoadFrom(fullPath);
        }
    }

    public class RemoteAssert : MarshalByRefObject
    {
        public static RemoteAssert Instance = new RemoteAssert();

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void True(bool value, string message)
        {
            Assert.True(value, message);
        }
    }

    public class RemoteAnalyzerFileReferenceTest : MarshalByRefObject
    {
        private RemoteAssert _assert;

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void TestTypeLoadException(string analyzerPath)
        {
            var analyzerRef = new AnalyzerFileReference(analyzerPath, FromFileLoader.Instance);
            analyzerRef.AnalyzerLoadFailed += (s, e) => _assert.True(e.Exception is TypeLoadException, "Expected TypeLoadException");
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            analyzerRef.AddAnalyzers(builder, LanguageNames.CSharp);
        }

        public void TestSuccess(string analyzerPath)
        {
            var analyzerRef = new AnalyzerFileReference(analyzerPath, FromFileLoader.Instance);
            analyzerRef.AnalyzerLoadFailed += (s, e) => _assert.True(false, "Unexpected exception: " + e.Message);
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            analyzerRef.AddAnalyzers(builder, LanguageNames.CSharp);
        }

        internal void SetAssert(RemoteAssert assert)
        {
            _assert = assert;
        }
    }

    public class AnalyzerFileReferenceTests : TestBase
    {
        private static readonly SimpleAnalyzerAssemblyLoader s_analyzerLoader = new SimpleAnalyzerAssemblyLoader();

        public static AnalyzerFileReference CreateAnalyzerFileReference(string fullPath)
        {
            return new AnalyzerFileReference(fullPath, s_analyzerLoader);
        }

        [Fact]
        public void TestMetadataParse()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(Assembly.GetExecutingAssembly().Location);
            var analyzerTypeNameMap = reference.GetAnalyzerTypeNameMap();
            Assert.Equal(2, analyzerTypeNameMap.Keys.Count());

            Assert.Equal(6, analyzerTypeNameMap[LanguageNames.CSharp].Count);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzerCS", analyzerTypeNameMap[LanguageNames.CSharp]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.TestAnalyzerCSVB", analyzerTypeNameMap[LanguageNames.CSharp]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzer", analyzerTypeNameMap[LanguageNames.CSharp]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+SomeType+NestedAnalyzer", analyzerTypeNameMap[LanguageNames.CSharp]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AbstractAnalyzer", analyzerTypeNameMap[LanguageNames.CSharp]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.OpenGenericAnalyzer`1", analyzerTypeNameMap[LanguageNames.CSharp]);
            Assert.DoesNotContain("Microsoft.CodeAnalysis.UnitTests.Test.NotAnAnalyzer", analyzerTypeNameMap[LanguageNames.CSharp]);

            Assert.Equal(6, analyzerTypeNameMap[LanguageNames.VisualBasic].Count);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzerVB", analyzerTypeNameMap[LanguageNames.VisualBasic]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.TestAnalyzerCSVB", analyzerTypeNameMap[LanguageNames.VisualBasic]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzer", analyzerTypeNameMap[LanguageNames.VisualBasic]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+SomeType+NestedAnalyzer", analyzerTypeNameMap[LanguageNames.VisualBasic]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AbstractAnalyzer", analyzerTypeNameMap[LanguageNames.VisualBasic]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.OpenGenericAnalyzer`1", analyzerTypeNameMap[LanguageNames.VisualBasic]);
            Assert.DoesNotContain("Microsoft.CodeAnalysis.UnitTests.Test.NotAnAnalyzer", analyzerTypeNameMap[LanguageNames.VisualBasic]);
        }

        [Fact]
        public void TestGetAnalyzersPerLanguage()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(Assembly.GetExecutingAssembly().Location);
            var analyzers = reference.GetAnalyzers(LanguageNames.CSharp);
            Assert.Equal(4, analyzers.Length);
            var analyzerNames = analyzers.Select(a => a.GetType().Name);
            Assert.Contains("TestAnalyzer", analyzerNames);
            Assert.Contains("TestAnalyzerCS", analyzerNames);
            Assert.Contains("TestAnalyzerCSVB", analyzerNames);
            Assert.Contains("NestedAnalyzer", analyzerNames);

            analyzers = reference.GetAnalyzers(LanguageNames.VisualBasic);
            analyzerNames = analyzers.Select(a => a.GetType().Name);
            Assert.Equal(4, analyzers.Length);
            Assert.Contains("TestAnalyzerVB", analyzerNames);
            Assert.Contains("TestAnalyzerCSVB", analyzerNames);
            Assert.Contains("TestAnalyzer", analyzerNames);
            Assert.Contains("NestedAnalyzer", analyzerNames);
        }

        [Fact]
        public void TestLoadErrors1()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(Assembly.GetExecutingAssembly().Location);

            List<AnalyzerLoadFailureEventArgs> errors = new List<AnalyzerLoadFailureEventArgs>();
            EventHandler<AnalyzerLoadFailureEventArgs> errorHandler = (o, e) => errors.Add(e);
            reference.AnalyzerLoadFailed += errorHandler;
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            reference.AddAnalyzers(builder, LanguageNames.CSharp);
            var analyzers = builder.ToImmutable();
            reference.AnalyzerLoadFailed -= errorHandler;

            Assert.Equal(2, errors.Count);
            var failedTypes = errors.Where(e => e.ErrorCode == AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer).Select(e => e.TypeName);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AbstractAnalyzer", failedTypes);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.OpenGenericAnalyzer`1", failedTypes);
        }

        [Fact]
        public void TestLoadErrors2()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference("C:\\randomlocation\\random.dll");

            List<AnalyzerLoadFailureEventArgs> errors = new List<AnalyzerLoadFailureEventArgs>();
            EventHandler<AnalyzerLoadFailureEventArgs> errorHandler = (o, e) => errors.Add(e);
            reference.AnalyzerLoadFailed += errorHandler;
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            reference.AddAnalyzers(builder, LanguageNames.CSharp);
            var analyzers = builder.ToImmutable();
            reference.AnalyzerLoadFailed -= errorHandler;

            Assert.Equal(1, errors.Count);
            Assert.Equal(AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToLoadAnalyzer, errors.First().ErrorCode);
        }

        [Fact]
        public void TestLoadErrors3()
        {
            var directory = Temp.CreateDirectory();
            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            AnalyzerFileReference reference = CreateAnalyzerFileReference(alphaDll.Path);

            List<AnalyzerLoadFailureEventArgs> errors = new List<AnalyzerLoadFailureEventArgs>();
            EventHandler<AnalyzerLoadFailureEventArgs> errorHandler = (o, e) => errors.Add(e);
            reference.AnalyzerLoadFailed += errorHandler;
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            reference.AddAnalyzers(builder, LanguageNames.CSharp);
            var analyzers = builder.ToImmutable();
            reference.AnalyzerLoadFailed -= errorHandler;

            File.Delete(alphaDll.Path);

            Assert.Equal(0, errors.Count);
        }

        [Fact]
        public void TestAnalyzerLoading()
        {
            var dir = Temp.CreateDirectory();
            var test = dir.CopyFile(typeof(FromFileLoader).Assembly.Location);
            var analyzerFile = TestHelpers.CreateCSharpAnalyzerAssemblyWithTestAnalyzer(dir, "MyAnalyzer");
            var loadDomain = AppDomainUtils.Create("AnalyzerTestDomain", basePath: dir.Path);
            try
            {
                var remoteTest = (RemoteAnalyzerFileReferenceTest)loadDomain.CreateInstanceAndUnwrap(typeof(RemoteAnalyzerFileReferenceTest).Assembly.FullName, typeof(RemoteAnalyzerFileReferenceTest).FullName);
                remoteTest.SetAssert(RemoteAssert.Instance);
                remoteTest.TestSuccess(analyzerFile.Path);
            }
            finally
            {
                AppDomain.Unload(loadDomain);
            }
        }

        [ConditionalFact(typeof(x86))]
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

            var metadata = dir.CopyFile(typeof(System.Reflection.Metadata.MetadataReader).Assembly.Location);
            var immutable = dir.CopyFile(typeof(ImmutableArray).Assembly.Location);
            var analyzer = dir.CopyFile(typeof(DiagnosticAnalyzer).Assembly.Location);
            var test = dir.CopyFile(typeof(FromFileLoader).Assembly.Location);

            var analyzerCompilation = CSharp.CSharpCompilation.Create(
                "MyAnalyzer",
                new SyntaxTree[] { CSharp.SyntaxFactory.ParseSyntaxTree(analyzerSource) },
                new MetadataReference[]
                {
                    SystemRuntimePP7Ref,
                    MetadataReference.CreateFromFile(immutable.Path),
                    MetadataReference.CreateFromFile(analyzer.Path)
                },
                new CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var analyzerFile = dir.CreateFile("MyAnalyzer.dll").WriteAllBytes(analyzerCompilation.EmitToArray());

            var loadDomain = AppDomainUtils.Create("AnalyzerTestDomain", basePath: dir.Path);
            try
            {
                var remoteTest = (RemoteAnalyzerFileReferenceTest)loadDomain.CreateInstanceAndUnwrap(typeof(RemoteAnalyzerFileReferenceTest).Assembly.FullName, typeof(RemoteAnalyzerFileReferenceTest).FullName);
                remoteTest.SetAssert(RemoteAssert.Instance);
                remoteTest.TestTypeLoadException(analyzerFile.Path);
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

        [Fact]
        [WorkItem(1029928, "DevDiv")]
        public void BadAnalyzerReference_DisplayName()
        {
            var directory = Temp.CreateDirectory();
            var textFile = directory.CreateFile("Foo.txt").WriteAllText("I am the very model of a modern major general.");
            AnalyzerFileReference reference = CreateAnalyzerFileReference(textFile.Path);

            Assert.Equal(expected: "Foo", actual: reference.Display);
        }

        [Fact]
        public void ValidAnalyzerReference_DisplayName()
        {
            var directory = Temp.CreateDirectory();
            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            AnalyzerFileReference reference = CreateAnalyzerFileReference(alphaDll.Path);

            Assert.Equal(expected: "Alpha", actual: reference.Display);
        }

        [Fact]
        [WorkItem(2781, "https://github.com/dotnet/roslyn/issues/2781")]
        [WorkItem(2782, "https://github.com/dotnet/roslyn/issues/2782")]
        public void ValidAnalyzerReference_Id()
        {
            var directory = Temp.CreateDirectory();
            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            AnalyzerFileReference reference = CreateAnalyzerFileReference(alphaDll.Path);

            AssemblyIdentity expectedIdentity = null;
            AssemblyIdentity.TryParseDisplayName("Alpha, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", out expectedIdentity);

            Assert.Equal(expected: expectedIdentity, actual: reference.Id);
        }

        [Fact]
        [WorkItem(2781, "https://github.com/dotnet/roslyn/issues/2781")]
        [WorkItem(2782, "https://github.com/dotnet/roslyn/issues/2782")]
        public void BadAnalyzerReference_Id()
        {
            var directory = Temp.CreateDirectory();
            var textFile = directory.CreateFile("Foo.txt").WriteAllText("I am the very model of a modern major general.");
            AnalyzerFileReference reference = CreateAnalyzerFileReference(textFile.Path);

            Assert.Equal(expected: "Foo", actual: reference.Id);
        }

        [Fact]
        [WorkItem(1032909)]
        public void TestFailedLoadDoesntCauseNoAnalyzersWarning()
        {
            var directory = Temp.CreateDirectory();
            var analyzerDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AnalyzerTests.FaultyAnalyzer);
            AnalyzerFileReference reference = CreateAnalyzerFileReference(analyzerDll.Path);

            List<AnalyzerLoadFailureEventArgs> errors = new List<AnalyzerLoadFailureEventArgs>();
            EventHandler<AnalyzerLoadFailureEventArgs> errorHandler = (o, e) => errors.Add(e);
            reference.AnalyzerLoadFailed += errorHandler;
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            reference.AddAnalyzers(builder, LanguageNames.CSharp);
            var analyzers = builder.ToImmutable();
            reference.AnalyzerLoadFailed -= errorHandler;

            Assert.Equal(1, errors.Count);
            Assert.Equal(AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer, errors.First().ErrorCode);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp, new string[] { LanguageNames.VisualBasic })]
        public class TestAnalyzer : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
            public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public class TestAnalyzerCS : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
            public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
        }

        [DiagnosticAnalyzer(LanguageNames.VisualBasic, new string[] { })]
        public class TestAnalyzerVB : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
            public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
        }

        public class SomeType
        {
            [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
            public class NestedAnalyzer : DiagnosticAnalyzer
            {
                public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
                public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
            }
        }
    }

    namespace Test
    {
        public class DiagnosticAnalyzer : Attribute
        {
        }

        [Test.DiagnosticAnalyzer]
        public class NotAnAnalyzer { }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class TestAnalyzerCSVB : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
        public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
    }

    public class TestAnalyzerNone
    { }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public abstract class AbstractAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
        public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class OpenGenericAnalyzer<T> : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
        public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
    }
}
