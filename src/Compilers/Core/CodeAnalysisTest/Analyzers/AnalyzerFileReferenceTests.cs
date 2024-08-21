// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [CollectionDefinition(Name)]
    public class AssemblyLoadTestFixtureCollection : ICollectionFixture<AssemblyLoadTestFixture>
    {
        public const string Name = nameof(AssemblyLoadTestFixtureCollection);
        private AssemblyLoadTestFixtureCollection() { }
    }

    [Collection(AssemblyLoadTestFixtureCollection.Name)]
    public class AnalyzerFileReferenceTests : TestBase
    {
        private static readonly AnalyzerAssemblyLoader s_analyzerLoader = new DefaultAnalyzerAssemblyLoader();
        private readonly AssemblyLoadTestFixture _testFixture;
        public AnalyzerFileReferenceTests(AssemblyLoadTestFixture testFixture)
        {
            _testFixture = testFixture;
        }

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
            var loader = new ThrowingLoader();
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
            var loader1 = new ThrowingLoader();
            var loader2 = new ThrowingLoader();

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
            AnalyzerFileReference reference = CreateAnalyzerFileReference(_testFixture.Alpha);

            List<AnalyzerLoadFailureEventArgs> errors = new List<AnalyzerLoadFailureEventArgs>();
            EventHandler<AnalyzerLoadFailureEventArgs> errorHandler = (o, e) => errors.Add(e);
            reference.AnalyzerLoadFailed += errorHandler;
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            reference.AddAnalyzers(builder, LanguageNames.CSharp);
            var analyzers = builder.ToImmutable();
            reference.AnalyzerLoadFailed -= errorHandler;

            Assert.Equal(0, errors.Count);
        }

        [Fact]
        [WorkItem(1029928, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1029928")]
        public void BadAnalyzerReference_DisplayName()
        {
            var directory = Temp.CreateDirectory();
            var textFile = directory.CreateFile("Goo.txt").WriteAllText("I am the very model of a modern major general.").Path;
            AnalyzerFileReference reference = CreateAnalyzerFileReference(textFile);

            Assert.Equal(expected: "Goo", actual: reference.Display);
        }

        [Fact]
        public void ValidAnalyzerReference_DisplayName()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(_testFixture.Alpha);

            Assert.Equal(expected: "Alpha", actual: reference.Display);
        }

        [Fact]
        [WorkItem(2781, "https://github.com/dotnet/roslyn/issues/2781")]
        [WorkItem(2782, "https://github.com/dotnet/roslyn/issues/2782")]
        public void ValidAnalyzerReference_Id()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(_testFixture.Alpha);

            AssemblyIdentity.TryParseDisplayName("Alpha, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", out var expectedIdentity);

            Assert.Equal(expected: expectedIdentity, actual: reference.Id);
        }

        [Fact]
        [WorkItem(2781, "https://github.com/dotnet/roslyn/issues/2781")]
        [WorkItem(2782, "https://github.com/dotnet/roslyn/issues/2782")]
        public void BadAnalyzerReference_Id()
        {
            var directory = Temp.CreateDirectory();
            var textFile = directory.CreateFile("Goo.txt").WriteAllText("I am the very model of a modern major general.").Path;
            AnalyzerFileReference reference = CreateAnalyzerFileReference(textFile);

            Assert.Equal(expected: "Goo", actual: reference.Id);
        }

        [Fact]
        [WorkItem(1032909, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032909")]
        public void TestFailedLoadDoesntCauseNoAnalyzersWarning()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(_testFixture.FaultyAnalyzer);

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
        [WorkItem(1032909, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032909")]
        public void TestReferencingFakeCompiler()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(_testFixture.AnalyzerWithFakeCompilerDependency);

            List<AnalyzerLoadFailureEventArgs> errors = new List<AnalyzerLoadFailureEventArgs>();
            EventHandler<AnalyzerLoadFailureEventArgs> errorHandler = (o, e) => errors.Add(e);
            reference.AnalyzerLoadFailed += errorHandler;
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            reference.AddAnalyzers(builder, LanguageNames.CSharp);
            var analyzers = builder.ToImmutable();
            reference.AnalyzerLoadFailed -= errorHandler;

            Assert.Equal(1, errors.Count);
            var error = errors[0];

            // failure is in the analyzer itself, i.e. abstract members on DiagnosticAnalyzer are not implemented.
            Assert.Equal(AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer, error.ErrorCode);
            Assert.Equal("Analyzer", error.TypeName);
        }

        [Fact]
        [WorkItem(1032909, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032909")]
        public void TestReferencingLaterFakeCompiler()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(_testFixture.AnalyzerWithLaterFakeCompilerDependency);

            List<AnalyzerLoadFailureEventArgs> errors = new List<AnalyzerLoadFailureEventArgs>();
            EventHandler<AnalyzerLoadFailureEventArgs> errorHandler = (o, e) => errors.Add(e);
            reference.AnalyzerLoadFailed += errorHandler;
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            reference.AddAnalyzers(builder, LanguageNames.CSharp);
            var analyzers = builder.ToImmutable();
            reference.AnalyzerLoadFailed -= errorHandler;

            Assert.Equal(1, errors.Count);
            var error = errors[0];
            Assert.Equal(AnalyzerLoadFailureEventArgs.FailureErrorCode.ReferencesNewerCompiler, error.ErrorCode);
            Assert.Null(error.TypeName);
        }

        private class AnalyzerLoaderMockCSharpCompiler : CSharpCompiler
        {
            public AnalyzerLoaderMockCSharpCompiler(CSharpCommandLineParser parser, string? responseFile, string[] args, BuildPaths buildPaths, string? additionalReferenceDirectories, IAnalyzerAssemblyLoader assemblyLoader, GeneratorDriverCache? driverCache = null, ICommonCompilerFileSystem? fileSystem = null)
                : base(parser, responseFile, args, buildPaths, additionalReferenceDirectories, assemblyLoader, driverCache, fileSystem)
            {
            }
        }

        [ConditionalFact(typeof(IsEnglishLocal))]
        public void AssemblyLoading_ReferencesLaterFakeCompiler_EndToEnd_CSharp()
        {
            var directory = Temp.CreateDirectory();

            TempFile corlib = directory.CreateFile("mscorlib.dll");
            corlib.WriteAllBytes(TestResources.NetFX.Minimal.mincorlib);

            TempFile source = directory.CreateFile("in.cs");
            source.WriteAllText("int x = 0;");

            var compiler = new AnalyzerLoaderMockCSharpCompiler(
                CSharpCommandLineParser.Default,
                responseFile: null,
                args: new[] { "/nologo", $@"/analyzer:""{_testFixture.AnalyzerWithLaterFakeCompilerDependency}""", "/nostdlib", $@"/r:""{corlib}""", "/out:something.dll", source.Path },
                new BuildPaths(clientDir: directory.Path, workingDir: directory.Path, sdkDir: null, tempDir: null),
                additionalReferenceDirectories: null,
                new DefaultAnalyzerAssemblyLoader());

            var writer = new StringWriter();
            var result = compiler.Run(writer);
            Assert.Equal(0, result);
            AssertEx.Equal($"""
                warning CS9057: The analyzer assembly '{_testFixture.AnalyzerWithLaterFakeCompilerDependency}' references version '100.0.0.0' of the compiler, which is newer than the currently running version '{typeof(DefaultAnalyzerAssemblyLoader).Assembly.GetName().Version}'.
                in.cs(1,5): warning CS0219: The variable 'x' is assigned but its value is never used

                """, writer.ToString());
        }

        [ConditionalFact(typeof(IsEnglishLocal), AlwaysSkip = "https://github.com/dotnet/roslyn/issues/63856")]
        public void DuplicateAnalyzerReference()
        {
            var directory = Temp.CreateDirectory();

            TempFile corlib = directory.CreateFile("mscorlib.dll");
            corlib.WriteAllBytes(TestResources.NetFX.Minimal.mincorlib);

            TempFile source = directory.CreateFile("in.cs");
            source.WriteAllText("int x = 0;");

            var compiler = new AnalyzerLoaderMockCSharpCompiler(
                CSharpCommandLineParser.Default,
                responseFile: null,
                args: new[] { "/nologo", $@"/analyzer:""{_testFixture.AnalyzerWithFakeCompilerDependency}""", $@"/analyzer:""{_testFixture.AnalyzerWithFakeCompilerDependency}""", "/nostdlib", $@"/r:""{corlib}""", "/out:something.dll", source.Path },
                new BuildPaths(clientDir: directory.Path, workingDir: directory.Path, sdkDir: null, tempDir: null),
                additionalReferenceDirectories: null,
                new DefaultAnalyzerAssemblyLoader());

            var writer = new StringWriter();
            var result = compiler.Run(writer);
            Assert.Equal(0, result);
            AssertEx.Equal($"""
                warning CS9067: Analyzer reference '{_testFixture.AnalyzerWithFakeCompilerDependency}' specified multiple times
                warning CS8032: An instance of analyzer Analyzer cannot be created from {_testFixture.AnalyzerWithFakeCompilerDependency} : Method 'get_SupportedDiagnostics' in type 'Analyzer' from assembly 'AnalyzerWithFakeCompilerDependency, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' does not have an implementation..
                in.cs(1,5): warning CS0219: The variable 'x' is assigned but its value is never used

                """, writer.ToString());
        }

        [ConditionalFact(typeof(CoreClrOnly), Reason = "Can't load a framework targeting generator, which these are in desktop")]
        public void TestLoadGenerators()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(Assembly.GetExecutingAssembly().Location);
            var generators = reference.GetGeneratorsForAllLanguages();
            var typeNames = generators.Select(g => g.GetGeneratorType().FullName);

            AssertEx.SetEqual(new[]
            {
                "Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestGenerator",
                "Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+SomeType+NestedGenerator",
                "Microsoft.CodeAnalysis.UnitTests.TestGenerator",
                "Microsoft.CodeAnalysis.UnitTests.BaseGenerator",
                "Microsoft.CodeAnalysis.UnitTests.SubClassedGenerator",
                "Microsoft.CodeAnalysis.UnitTests.ExplicitCSharpOnlyGenerator",
                "Microsoft.CodeAnalysis.UnitTests.VisualBasicOnlyGenerator",
                "Microsoft.CodeAnalysis.UnitTests.CSharpAndVisualBasicGenerator",
                "Microsoft.CodeAnalysis.UnitTests.VisualBasicAndCSharpGenerator",
                "Microsoft.CodeAnalysis.UnitTests.FSharpGenerator",
                "Microsoft.CodeAnalysis.UnitTests.TestSourceAndIncrementalGenerator",
                "Microsoft.CodeAnalysis.UnitTests.TestIncrementalGenerator"
            }, typeNames);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestLoadGeneratorsWithoutArgumentOnlyLoadsCSharp()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(Assembly.GetExecutingAssembly().Location);
            var generators = reference.GetGenerators(LanguageNames.CSharp);

#pragma warning disable CS0618 // Type or member is obsolete
            var generators2 = reference.GetGenerators();
#pragma warning restore CS0618 // Type or member is obsolete

            Assert.Equal(generators, generators2);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestLoadCSharpGenerators()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(Assembly.GetExecutingAssembly().Location);
            var generators = reference.GetGenerators(LanguageNames.CSharp);

            var typeNames = generators.Select(g => g.GetGeneratorType().FullName);
            AssertEx.SetEqual(new[]
            {
                "Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestGenerator",
                "Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+SomeType+NestedGenerator",
                "Microsoft.CodeAnalysis.UnitTests.TestGenerator",
                "Microsoft.CodeAnalysis.UnitTests.BaseGenerator",
                "Microsoft.CodeAnalysis.UnitTests.SubClassedGenerator",
                "Microsoft.CodeAnalysis.UnitTests.ExplicitCSharpOnlyGenerator",
                "Microsoft.CodeAnalysis.UnitTests.CSharpAndVisualBasicGenerator",
                "Microsoft.CodeAnalysis.UnitTests.VisualBasicAndCSharpGenerator",
                "Microsoft.CodeAnalysis.UnitTests.TestSourceAndIncrementalGenerator",
                "Microsoft.CodeAnalysis.UnitTests.TestIncrementalGenerator"
            }, typeNames);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void TestLoadVisualBasicGenerators()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(Assembly.GetExecutingAssembly().Location);
            var generators = reference.GetGenerators(LanguageNames.VisualBasic);

            var typeNames = generators.Select(g => g.GetGeneratorType().FullName);
            AssertEx.SetEqual(new[]
            {
                "Microsoft.CodeAnalysis.UnitTests.VisualBasicOnlyGenerator",
                "Microsoft.CodeAnalysis.UnitTests.CSharpAndVisualBasicGenerator",
                "Microsoft.CodeAnalysis.UnitTests.VisualBasicAndCSharpGenerator",
                "Microsoft.CodeAnalysis.UnitTests.TestSourceAndIncrementalGenerator",
                "Microsoft.CodeAnalysis.UnitTests.TestIncrementalGenerator"
            }, typeNames);
        }

        // can't load a coreclr targeting generator on net framework / mono
        [ConditionalFact(typeof(CoreClrOnly), AlwaysSkip = "https://github.com/dotnet/roslyn/issues/60762")]
        public void TestGeneratorsCantTargetNetFramework()
        {
            var directory = Temp.CreateDirectory();

            // core
            var errors = buildAndLoadGeneratorAndReturnAnyErrors(".NETCoreApp,Version=v5.0");
            Assert.Empty(errors);

            // netstandard
            errors = buildAndLoadGeneratorAndReturnAnyErrors(".NETStandard,Version=v2.0");
            Assert.Empty(errors);

            // no target
            errors = buildAndLoadGeneratorAndReturnAnyErrors(targetFramework: null);
            Assert.Empty(errors);

            // framework
            errors = buildAndLoadGeneratorAndReturnAnyErrors(".NETFramework,Version=v4.7.2");
            Assert.Equal(2, errors.Count);
            Assert.Equal(AnalyzerLoadFailureEventArgs.FailureErrorCode.ReferencesFramework, errors.First().ErrorCode);

            List<AnalyzerLoadFailureEventArgs> buildAndLoadGeneratorAndReturnAnyErrors(string? targetFramework)
            {
                string targetFrameworkAttributeText = targetFramework is object
                                                        ? $"[assembly: System.Runtime.Versioning.TargetFramework(\"{targetFramework}\")]"
                                                        : string.Empty;

                string generatorSource = $@"
using Microsoft.CodeAnalysis;

{targetFrameworkAttributeText}

[Generator]
public class Generator : ISourceGenerator
{{
            public void Execute(GeneratorExecutionContext context) {{ }}
            public void Initialize(GeneratorInitializationContext context) {{ }}
 }}";

                var directory = Temp.CreateDirectory();
                var generatorPath = Path.Combine(directory.Path, $"generator_{targetFramework}.dll");

                var compilation = CSharpCompilation.Create($"generator_{targetFramework}",
                                                           new[] { CSharpTestSource.Parse(generatorSource) },
                                                           TargetFrameworkUtil.GetReferences(TargetFramework.Standard, new[] { MetadataReference.CreateFromAssemblyInternal(typeof(ISourceGenerator).Assembly) }),
                                                           new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                compilation.VerifyDiagnostics();
                var result = compilation.Emit(generatorPath);
                Assert.True(result.Success);

                AnalyzerFileReference reference = CreateAnalyzerFileReference(generatorPath);
                List<AnalyzerLoadFailureEventArgs> errors = new List<AnalyzerLoadFailureEventArgs>();
                void errorHandler(object? o, AnalyzerLoadFailureEventArgs e) => errors.Add(e);
                reference.AnalyzerLoadFailed += errorHandler;
                var builder = ImmutableArray.CreateBuilder<ISourceGenerator>();
                reference.AddGenerators(builder, LanguageNames.CSharp);
                reference.AnalyzerLoadFailed -= errorHandler;

                if (errors.Count > 0)
                {
                    Assert.Empty(builder);
                }
                else
                {
                    Assert.Single(builder);
                }
                return errors;
            }
        }

        [Fact]
        [WorkItem(52035, "https://github.com/dotnet/roslyn/issues/52035")]
        public void TestLoadedAnalyzerOrderIsDeterministic()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(Assembly.GetExecutingAssembly().Location);

            var csharpAnalyzers = reference.GetAnalyzers(LanguageNames.CSharp).Select(a => a.GetType().FullName).ToArray();
            Assert.Equal(4, csharpAnalyzers.Length);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+SomeType+NestedAnalyzer", csharpAnalyzers[0]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzer", csharpAnalyzers[1]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzerCS", csharpAnalyzers[2]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.TestAnalyzerCSVB", csharpAnalyzers[3]);

            var vbAnalyzers = reference.GetAnalyzers(LanguageNames.VisualBasic).Select(a => a.GetType().FullName).ToArray();
            Assert.Equal(4, vbAnalyzers.Length);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+SomeType+NestedAnalyzer", vbAnalyzers[0]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzer", vbAnalyzers[1]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzerVB", vbAnalyzers[2]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.TestAnalyzerCSVB", vbAnalyzers[3]);

            // analyzers return C#, then VB, including duplicates
            var allAnalyzers = reference.GetAnalyzersForAllLanguages().Select(a => a.GetType().FullName).ToArray();
            Assert.Equal(8, allAnalyzers.Length);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+SomeType+NestedAnalyzer", allAnalyzers[0]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzer", allAnalyzers[1]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzerCS", allAnalyzers[2]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.TestAnalyzerCSVB", allAnalyzers[3]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+SomeType+NestedAnalyzer", allAnalyzers[4]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzer", allAnalyzers[5]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestAnalyzerVB", allAnalyzers[6]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.TestAnalyzerCSVB", allAnalyzers[7]);
        }

        [ConditionalFact(typeof(CoreClrOnly), Reason = "Can't load a framework targeting generator, which these are in desktop")]
        [WorkItem(52035, "https://github.com/dotnet/roslyn/issues/52035")]
        public void TestLoadedGeneratorOrderIsDeterministic()
        {
            AnalyzerFileReference reference = CreateAnalyzerFileReference(Assembly.GetExecutingAssembly().Location);

            var csharpGenerators = reference.GetGenerators(LanguageNames.CSharp).Select(g => g.GetGeneratorType().FullName).ToArray();
            Assert.Equal(10, csharpGenerators.Length);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+SomeType+NestedGenerator", csharpGenerators[0]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestGenerator", csharpGenerators[1]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.BaseGenerator", csharpGenerators[2]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.CSharpAndVisualBasicGenerator", csharpGenerators[3]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.ExplicitCSharpOnlyGenerator", csharpGenerators[4]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.SubClassedGenerator", csharpGenerators[5]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.TestGenerator", csharpGenerators[6]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.TestIncrementalGenerator", csharpGenerators[7]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.TestSourceAndIncrementalGenerator", csharpGenerators[8]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.VisualBasicAndCSharpGenerator", csharpGenerators[9]);

            var vbGenerators = reference.GetGenerators(LanguageNames.VisualBasic).Select(g => g.GetGeneratorType().FullName).ToArray();
            Assert.Equal(5, vbGenerators.Length);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.CSharpAndVisualBasicGenerator", vbGenerators[0]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.TestIncrementalGenerator", vbGenerators[1]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.TestSourceAndIncrementalGenerator", vbGenerators[2]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.VisualBasicAndCSharpGenerator", vbGenerators[3]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.VisualBasicOnlyGenerator", vbGenerators[4]);

            // generators load in language order (C#, F#, VB), and *do not* include duplicates
            var allGenerators = reference.GetGeneratorsForAllLanguages().Select(g => g.GetGeneratorType().FullName).ToArray();
            Assert.Equal(12, allGenerators.Length);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+SomeType+NestedGenerator", allGenerators[0]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.AnalyzerFileReferenceTests+TestGenerator", allGenerators[1]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.BaseGenerator", allGenerators[2]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.CSharpAndVisualBasicGenerator", allGenerators[3]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.ExplicitCSharpOnlyGenerator", allGenerators[4]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.SubClassedGenerator", allGenerators[5]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.TestGenerator", allGenerators[6]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.TestIncrementalGenerator", allGenerators[7]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.TestSourceAndIncrementalGenerator", allGenerators[8]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.VisualBasicAndCSharpGenerator", allGenerators[9]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.FSharpGenerator", allGenerators[10]);
            Assert.Equal("Microsoft.CodeAnalysis.UnitTests.VisualBasicOnlyGenerator", allGenerators[11]);
        }

        // NOTE: the order in which these are emitted can change the test 'TestLoadedAnalyzerOrderIsDeterministic'
        //       and other determinism tests in this file.
        //       Ensure you do not re-arrange them alphabetically, as that will invalidate the tests, without 
        //       explicitly failing them

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
            public void Execute(GeneratorExecutionContext context) => throw new NotImplementedException();
            public void Initialize(GeneratorInitializationContext context) => throw new NotImplementedException();
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
                public void Execute(GeneratorExecutionContext context) => throw new NotImplementedException();
                public void Initialize(GeneratorInitializationContext context) => throw new NotImplementedException();
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
        public void Execute(GeneratorExecutionContext context) => throw new NotImplementedException();
        public void Initialize(GeneratorInitializationContext context) => throw new NotImplementedException();
    }

    public class TestGeneratorNoAttrib : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context) => throw new NotImplementedException();
        public void Initialize(GeneratorInitializationContext context) => throw new NotImplementedException();
    }

    [Generator]
    public class BaseGenerator : ISourceGenerator
    {
        public virtual void Execute(GeneratorExecutionContext context) => throw new NotImplementedException();
        public void Initialize(GeneratorInitializationContext context) => throw new NotImplementedException();
    }

    [Generator]
    public class SubClassedGenerator : BaseGenerator
    {
        public override void Execute(GeneratorExecutionContext context) => throw new NotImplementedException();
    }

    [Generator]
    public class NotAGenerator { }

    [Generator(LanguageNames.CSharp)]
    public class ExplicitCSharpOnlyGenerator : TestGenerator { }

    [Generator(LanguageNames.VisualBasic)]
    public class VisualBasicOnlyGenerator : TestGenerator { }

    [Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class CSharpAndVisualBasicGenerator : TestGenerator { }

    [Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class VisualBasicAndCSharpGenerator : TestGenerator { }

    [Generator(LanguageNames.FSharp)]
    public class FSharpGenerator : TestGenerator { }

    [Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class TestIncrementalGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context) => throw new NotImplementedException();
    }

    public class TestIncrementalGeneratorWithNoAttrib : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context) => throw new NotImplementedException();
    }

    [Generator(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class TestSourceAndIncrementalGenerator : IIncrementalGenerator, ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context) => throw new NotImplementedException();

        public void Initialize(IncrementalGeneratorInitializationContext context) => throw new NotImplementedException();

        public void Initialize(GeneratorInitializationContext context) => throw new NotImplementedException();
    }

    file sealed class ThrowingLoader : IAnalyzerAssemblyLoaderInternal
    {
        public void AddDependencyLocation(string fullPath) { }
        public bool IsHostAssembly(Assembly assembly) => false;
        public Assembly LoadFromPath(string fullPath) => throw new Exception();
        public string? GetOriginalDependencyLocation(AssemblyName assembly) => throw new Exception();
        public void UnloadAll() { }
    }
}
