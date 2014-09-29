// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AnalyzerFileReferenceTests : TestBase
    {
        [Fact]
        public void AssemblyLoading()
        {
            StringBuilder sb = new StringBuilder();
            var directory = Temp.CreateDirectory();

            EventHandler<AnalyzerAssemblyLoadEventArgs> handler = (e, args) =>
            {
                var relativePath = args.Path.Substring(directory.Path.Length);
                sb.AppendFormat("Assembly {0} loaded from {1}", args.LoadedAssembly.FullName, relativePath);
                sb.AppendLine();
            };

            AnalyzerFileReference.AssemblyLoad += handler;

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Alpha);
            var betaDll = directory.CreateFile("Beta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Beta);
            var gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Gamma);
            var deltaDll = directory.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.AssemblyLoadTests.Delta);

            AnalyzerFileReference alphaReference = new AnalyzerFileReference(alphaDll.Path);
            Assembly alpha = alphaReference.GetAssembly();
            File.Delete(alphaDll.Path);

            var a = alpha.CreateInstance("Alpha.A");
            a.GetType().GetMethod("Write").Invoke(a, new object[] { sb, "Test A" });

            File.Delete(gammaDll.Path);
            File.Delete(deltaDll.Path);

            AnalyzerFileReference betaReference = new AnalyzerFileReference(betaDll.Path);
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

            var alphaDllRequestor = AnalyzerFileReference.TryGetRequestingAssemblyPath(alphaDll.Path);
            var betaDllRequestor = AnalyzerFileReference.TryGetRequestingAssemblyPath(betaDll.Path);
            var gammaDllRequestor = AnalyzerFileReference.TryGetRequestingAssemblyPath(gammaDll.Path);
            var deltaDllRequestor = AnalyzerFileReference.TryGetRequestingAssemblyPath(deltaDll.Path);

            Assert.Null(alphaDllRequestor);
            Assert.Null(betaDllRequestor);
            Assert.Equal(expected: alphaDll.Path, actual: gammaDllRequestor, comparer: StringComparer.OrdinalIgnoreCase);
            Assert.Equal(expected: gammaDll.Path, actual: deltaDllRequestor, comparer: StringComparer.OrdinalIgnoreCase);

            AnalyzerFileReference.AssemblyLoad -= handler;
        }

        [Fact]
        public void TestMetadataParse()
        {
            AnalyzerFileReference reference = new AnalyzerFileReference(Assembly.GetExecutingAssembly().Location);
            var analyzerTypeNameMap = reference.GetAnalyzerTypeNameMap();
            Assert.Equal(3, analyzerTypeNameMap.Keys.Count());
            Assert.Equal(6, analyzerTypeNameMap[string.Empty].Count);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzer", analyzerTypeNameMap[string.Empty]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.TestAnalyzerCSAll", analyzerTypeNameMap[string.Empty]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.TestAnalyzerAllCS", analyzerTypeNameMap[string.Empty]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+SomeType+NestedAnalyzer", analyzerTypeNameMap[string.Empty]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AbstractAnalyzer", analyzerTypeNameMap[string.Empty]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.OpenGenericAnalyzer`1", analyzerTypeNameMap[string.Empty]);
            Assert.DoesNotContain("Microsoft.CodeAnalysis.UnitTests.Test.NotAnAnalyzer", analyzerTypeNameMap[string.Empty]);

            Assert.Equal(2, analyzerTypeNameMap[LanguageNames.CSharp].Count);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzerCS", analyzerTypeNameMap[LanguageNames.CSharp]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.TestAnalyzerCSVB", analyzerTypeNameMap[LanguageNames.CSharp]);

            Assert.Equal(2, analyzerTypeNameMap[LanguageNames.VisualBasic].Count);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzerVB", analyzerTypeNameMap[LanguageNames.VisualBasic]);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.TestAnalyzerCSVB", analyzerTypeNameMap[LanguageNames.VisualBasic]);
        }

        [Fact]
        public void TestGetAnalyzers()
        {
            AnalyzerFileReference reference = new AnalyzerFileReference(Assembly.GetExecutingAssembly().Location);
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            reference.AddAnalyzers(builder, null);
            var analyzers = builder.ToImmutable();
            Assert.Equal(7, analyzers.Length);
            var analyzerNames = analyzers.Select(a => a.GetType().Name);
            Assert.Contains("TestAnalyzer", analyzerNames);
            Assert.Contains("TestAnalyzerCS", analyzerNames);
            Assert.Contains("TestAnalyzerVB", analyzerNames);
            Assert.Contains("TestAnalyzerCSVB", analyzerNames);
            Assert.Contains("TestAnalyzerCSAll", analyzerNames);
            Assert.Contains("TestAnalyzerAllCS", analyzerNames);
            Assert.Contains("NestedAnalyzer", analyzerNames);
        }

        [Fact]
        public void TestGetAnalyzersPerLanguage()
        {
            AnalyzerFileReference reference = new AnalyzerFileReference(Assembly.GetExecutingAssembly().Location);
            var analyzers = reference.GetAnalyzers(LanguageNames.CSharp);
            Assert.Equal(6, analyzers.Length);
            var analyzerNames = analyzers.Select(a => a.GetType().Name);
            Assert.Contains("TestAnalyzer", analyzerNames);
            Assert.Contains("TestAnalyzerCS", analyzerNames);
            Assert.Contains("TestAnalyzerCSVB", analyzerNames);
            Assert.Contains("TestAnalyzerCSAll", analyzerNames);
            Assert.Contains("TestAnalyzerAllCS", analyzerNames);
            Assert.Contains("NestedAnalyzer", analyzerNames);

            analyzers = reference.GetAnalyzers(LanguageNames.VisualBasic);
            analyzerNames = analyzers.Select(a => a.GetType().Name);
            Assert.Equal(6, analyzers.Length);
            Assert.Contains("TestAnalyzerVB", analyzerNames);
            Assert.Contains("TestAnalyzerCSVB", analyzerNames);
            Assert.Contains("TestAnalyzer", analyzerNames);
            Assert.Contains("TestAnalyzerCSAll", analyzerNames);
            Assert.Contains("TestAnalyzerAllCS", analyzerNames);
            Assert.Contains("NestedAnalyzer", analyzerNames);
        }

        [Fact]
        public void TestLoadErrors1()
        {
            AnalyzerFileReference reference = new AnalyzerFileReference(Assembly.GetExecutingAssembly().Location);

            List<AnalyzerLoadFailureEventArgs> errors = new List<AnalyzerLoadFailureEventArgs>();
            EventHandler<AnalyzerLoadFailureEventArgs> errorHandler = (o, e) => errors.Add(e);
            reference.AnalyzerLoadFailed += errorHandler;
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            reference.AddAnalyzers(builder, LanguageNames.CSharp);
            var analyzers = builder.ToImmutable();
            reference.AnalyzerLoadFailed -= errorHandler;

            Assert.Equal(2, errors.Count);
            var failedTypes = errors.Where(e=> e.ErrorCode == AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer).Select(e => e.TypeName);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AbstractAnalyzer", failedTypes);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.OpenGenericAnalyzer`1", failedTypes);
        }

        [Fact]
        public void TestLoadErrors2()
        {
            AnalyzerFileReference reference = new AnalyzerFileReference("C:\\randomlocation\\random.dll");

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
            AnalyzerFileReference reference = new AnalyzerFileReference(alphaDll.Path);

            List<AnalyzerLoadFailureEventArgs> errors = new List<AnalyzerLoadFailureEventArgs>();
            EventHandler<AnalyzerLoadFailureEventArgs> errorHandler = (o, e) => errors.Add(e);
            reference.AnalyzerLoadFailed += errorHandler;
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            reference.AddAnalyzers(builder, LanguageNames.CSharp);
            var analyzers = builder.ToImmutable();
            reference.AnalyzerLoadFailed -= errorHandler;

            File.Delete(alphaDll.Path);

            Assert.Equal(1, errors.Count);
            Assert.Equal(AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers, errors.First().ErrorCode);
        }

        [Fact]
        [WorkItem(1029928, "DevDiv")]
        public void BadAnalyzerReference_DisplayName()
        {
            var directory = Temp.CreateDirectory();
            var textFile = directory.CreateFile("Foo.txt").WriteAllText("I am the very model of a modern major general.");
            AnalyzerFileReference reference = new AnalyzerFileReference(textFile.Path);

            Assert.Equal(expected: "Foo.txt", actual: reference.Display);
        }

        [DiagnosticAnalyzer]
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

        [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
        public class TestAnalyzerVB : DiagnosticAnalyzer
        {
            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
            public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
        }

        public class SomeType
        {
            [DiagnosticAnalyzer]
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

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    public class TestAnalyzerCSVB : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
        public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [DiagnosticAnalyzer]
    public class TestAnalyzerCSAll : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
        public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
    }

    [DiagnosticAnalyzer]
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TestAnalyzerAllCS : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
        public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
    }

    public class TestAnalyzerNone
    { }

    [DiagnosticAnalyzer]
    public abstract class AbstractAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
        public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
    }

    [DiagnosticAnalyzer]
    public class OpenGenericAnalyzer<T> : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
        public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
    }
}
