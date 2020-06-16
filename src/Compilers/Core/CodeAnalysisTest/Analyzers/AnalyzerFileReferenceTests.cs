// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AnalyzerFileReferenceTests : TestBase
    {
        private static readonly AnalyzerAssemblyLoader s_analyzerLoader = new DefaultAnalyzerAssemblyLoader();

        public static AnalyzerFileReference CreateAnalyzerFileReference(string fullPath)
        {
            return new AnalyzerFileReference(fullPath, s_analyzerLoader);
        }

        [Fact]
        public void AnalyzerFileReference_Errors()
        {
            Assert.Throws<ArgumentNullException>("fullPath", () => new AnalyzerFileReference(null!, s_analyzerLoader));
            Assert.Throws<ArgumentNullException>("assemblyLoader", () => new AnalyzerFileReference(TempRoot.Root, null!));

            // path must be absolute
            Assert.Throws<ArgumentException>("fullPath", () => new AnalyzerFileReference("a.dll", s_analyzerLoader));
        }

        [Fact]
        public void DisplayAndId_BadPath()
        {
            var loader = new TestAnalyzerAssemblyLoader(loadFromPath: _ => throw new Exception());
            var refBadPath = new AnalyzerFileReference(PathUtilities.CombinePathsUnchecked(TempRoot.Root, "\0<>|*.xyz"), loader);
            Assert.Equal("\0<>|*", refBadPath.Display);
            Assert.Equal("\0<>|*", refBadPath.Id);
        }

        [Fact]
        public void Equality()
        {
            var path1 = Path.Combine(TempRoot.Root, "dir");
            var path2 = Path.Combine(TempRoot.Root, "dir", "..", "dir");

            // Equals/GetHashCode should not load the analyzer
            var loader1 = new TestAnalyzerAssemblyLoader(loadFromPath: _ => throw new InvalidOperationException());
            var loader2 = new TestAnalyzerAssemblyLoader(loadFromPath: _ => throw new InvalidOperationException());

            var refA = new AnalyzerFileReference(path1, loader1);
            var refB = new AnalyzerFileReference(path1, loader1);

            Assert.False(refA.Equals(null));
            Assert.True(refA.Equals(refA));
            Assert.True(refA.Equals(refB));
            Assert.Equal(refA.GetHashCode(), refB.GetHashCode());

            // paths are compared for exact equality, it's up to the host to normalize them:
            Assert.False(refA.Equals(new AnalyzerFileReference(path2, loader1)));

            // different loader:
            Assert.False(refA.Equals(new AnalyzerFileReference(path1, loader2)));

            // legacy overload:
            Assert.True(refA.Equals((AnalyzerReference)refA));
            Assert.False(refA.Equals((AnalyzerReference?)null));
            Assert.True(refA!.Equals((AnalyzerReference)refB));
            Assert.True(refA.Equals(new TestAnalyzerReference(path1)));
            Assert.False(refA.Equals(new TestAnalyzerReference(path2)));
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
            AnalyzerFileReference reference = CreateAnalyzerFileReference(Path.Combine(TempRoot.Root, "random.dll"));

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
        [WorkItem(1029928, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1029928")]
        public void BadAnalyzerReference_DisplayName()
        {
            var directory = Temp.CreateDirectory();
            var textFile = directory.CreateFile("Goo.txt").WriteAllText("I am the very model of a modern major general.");
            AnalyzerFileReference reference = CreateAnalyzerFileReference(textFile.Path);

            Assert.Equal(expected: "Goo", actual: reference.Display);
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

            AssemblyIdentity.TryParseDisplayName("Alpha, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", out var expectedIdentity);

            Assert.Equal(expected: expectedIdentity, actual: reference.Id);
        }

        [Fact]
        [WorkItem(2781, "https://github.com/dotnet/roslyn/issues/2781")]
        [WorkItem(2782, "https://github.com/dotnet/roslyn/issues/2782")]
        public void BadAnalyzerReference_Id()
        {
            var directory = Temp.CreateDirectory();
            var textFile = directory.CreateFile("Goo.txt").WriteAllText("I am the very model of a modern major general.");
            AnalyzerFileReference reference = CreateAnalyzerFileReference(textFile.Path);

            Assert.Equal(expected: "Goo", actual: reference.Id);
        }

        [Fact]
        [WorkItem(1032909, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032909")]
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

        [Fact]
        public void TestLoadGenerators()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(Assembly.GetExecutingAssembly().Location);
            var generators = reference.GetGenerators();
            Assert.Equal(5, generators.Count());

            var typeNames = generators.Select(g => g.GetType().FullName);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestGenerator", typeNames);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+SomeType+NestedGenerator", typeNames);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.TestGenerator", typeNames);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.BaseGenerator", typeNames);
            Assert.Contains("Microsoft.CodeAnalysis.UnitTests.SubClassedGenerator", typeNames);
            Assert.DoesNotContain("Microsoft.CodeAnalysis.UnitTests.TestGeneratorNoAttrib", typeNames);
            Assert.DoesNotContain("Microsoft.CodeAnalysis.UnitTests.Test.NotAGenerator", typeNames);
            Assert.DoesNotContain("Microsoft.CodeAnalysis.UnitTests.NotAGenerator", typeNames);
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

        [Generator]
        public class TestGenerator : ISourceGenerator
        {
            public void Execute(SourceGeneratorContext context) => throw new NotImplementedException();
            public void Initialize(InitializationContext context) => throw new NotImplementedException();
        }

        public class SomeType
        {
            [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
            public class NestedAnalyzer : DiagnosticAnalyzer
            {
                public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
                public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
            }

            [Generator]
            public class NestedGenerator : ISourceGenerator
            {
                public void Execute(SourceGeneratorContext context) => throw new NotImplementedException();
                public void Initialize(InitializationContext context) => throw new NotImplementedException();
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

        public class Generator : Attribute
        {
        }

        [Test.Generator]
        public class NotAGenerator { }
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

    [Generator]
    public class TestGenerator : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context) => throw new NotImplementedException();
        public void Initialize(InitializationContext context) => throw new NotImplementedException();
    }

    public class TestGeneratorNoAttrib : ISourceGenerator
    {
        public void Execute(SourceGeneratorContext context) => throw new NotImplementedException();
        public void Initialize(InitializationContext context) => throw new NotImplementedException();
    }

    [Generator]
    public class BaseGenerator : ISourceGenerator
    {
        public virtual void Execute(SourceGeneratorContext context) => throw new NotImplementedException();
        public void Initialize(InitializationContext context) => throw new NotImplementedException();
    }

    [Generator]
    public class SubClassedGenerator : BaseGenerator
    {
        public override void Execute(SourceGeneratorContext context) => throw new NotImplementedException();
    }

    [Generator]
    public class NotAGenerator { }
}
