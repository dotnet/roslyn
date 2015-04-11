// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AnalyzerFileReferenceTests : TestBase
    {
        public static AnalyzerFileReference CreateAnalyzerFileReference(string fullPath)
        {
            return new AnalyzerFileReference(fullPath, InMemoryAssemblyProvider.GetAssembly);
        }

        [Fact]
        public void AssemblyLoading()
        {
            StringBuilder sb = new StringBuilder();
            var directory = Temp.CreateDirectory();

            EventHandler<InMemoryAssemblyProvider.AssemblyLoadEventArgs> handler = (e, args) =>
            {
                var relativePath = args.Path.Substring(directory.Path.Length);
                sb.AppendFormat("Assembly {0} loaded from {1}", args.LoadedAssembly.FullName, relativePath);
                sb.AppendLine();
            };

            InMemoryAssemblyProvider.AssemblyLoad += handler;

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Alpha);
            var betaDll = directory.CreateFile("Beta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Beta);
            var gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Gamma);
            var deltaDll = directory.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Delta);

            AnalyzerFileReference alphaReference = CreateAnalyzerFileReference(alphaDll.Path);
            Assembly alpha = alphaReference.GetAssembly();
            File.Delete(alphaDll.Path);

            var a = alpha.CreateInstance("Alpha.A");
            a.GetType().GetMethod("Write").Invoke(a, new object[] { sb, "Test A" });

            File.Delete(gammaDll.Path);
            File.Delete(deltaDll.Path);

            AnalyzerFileReference betaReference = CreateAnalyzerFileReference(betaDll.Path);
            Assembly beta = betaReference.GetAssembly();
            var b = beta.CreateInstance("Beta.B");
            b.GetType().GetMethod("Write").Invoke(b, new object[] { sb, "Test B" });

            var expected = @"Assembly Alpha, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null loaded from \Alpha.dll
Assembly Gamma, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null loaded from \Gamma.dll
Assembly Delta, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null loaded from \Delta.dll
Delta: Gamma: Alpha: Test A
Assembly Beta, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null loaded from \Beta.dll
Delta: Gamma: Beta: Test B
";

            var actual = sb.ToString();

            Assert.Equal(expected, actual);

            var alphaDllRequestor = InMemoryAssemblyProvider.TryGetRequestingAssembly(alphaDll.Path);
            var betaDllRequestor = InMemoryAssemblyProvider.TryGetRequestingAssembly(betaDll.Path);
            var gammaDllRequestor = InMemoryAssemblyProvider.TryGetRequestingAssembly(gammaDll.Path);
            var deltaDllRequestor = InMemoryAssemblyProvider.TryGetRequestingAssembly(deltaDll.Path);

            Assert.Null(alphaDllRequestor);
            Assert.Null(betaDllRequestor);
            Assert.Equal(expected: alphaDll.Path, actual: gammaDllRequestor, comparer: StringComparer.OrdinalIgnoreCase);
            Assert.Equal(expected: gammaDll.Path, actual: deltaDllRequestor, comparer: StringComparer.OrdinalIgnoreCase);

            InMemoryAssemblyProvider.AssemblyLoad -= handler;
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
            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Alpha);
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
        [WorkItem(1029928, "DevDiv")]
        public void BadAnalyzerReference_DisplayName()
        {
            var directory = Temp.CreateDirectory();
            var textFile = directory.CreateFile("Foo.txt").WriteAllText("I am the very model of a modern major general.");
            AnalyzerFileReference reference = CreateAnalyzerFileReference(textFile.Path);

            Assert.Equal(expected: "Foo.txt", actual: reference.Display);
        }

        [Fact]
        [WorkItem(1032909)]
        public void TestFailedLoadDoesntCauseNoAnalyzersWarning()
        {
            var directory = Temp.CreateDirectory();
            var analyzerDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AnalyzerTests.AnalyzerTests.FaultyAnalyzer);
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

        [Fact]
        public void AssemblyPathHelper_NeutralCultureAssembly()
        {
            string directoryPath = @"C:\Alpha\Beta";
            AssemblyIdentity assemblyIdentity = new AssemblyIdentity("Gamma");

            string expected = @"C:\Alpha\Beta\Gamma.dll";
            string actual = InMemoryAssemblyProvider.GetCandidatePath(directoryPath, assemblyIdentity);

            Assert.Equal(expected, actual, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void AssemblyPathHelper_AssemblyWithCulture()
        {
            string directoryPath = @"C:\Alpha\Beta";
            AssemblyIdentity assemblyIdentity = new AssemblyIdentity(name: "Gamma", cultureName: "fr-FR");

            string expected = @"C:\Alpha\Beta\fr-FR\Gamma.dll";
            string actual = InMemoryAssemblyProvider.GetCandidatePath(directoryPath, assemblyIdentity);

            Assert.Equal(expected, actual, StringComparer.OrdinalIgnoreCase);
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
