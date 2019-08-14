// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

// The ShadowCopyAnalyzerAssemblyLoader type is only defined for the below platforms
#if NET472 || NETCOREAPP2_1 || NETCOREAPP3_0

namespace Microsoft.CodeAnalysis.UnitTests
{
    public sealed class ShadowCopyAnalyzerAssemblyLoaderTests : TestBase
    {
        [Fact, WorkItem(32226, "https://github.com/dotnet/roslyn/issues/32226")]
        public void LoadWithDependency()
        {
            var directory = Temp.CreateDirectory();
            var immutable = directory.CopyFile(typeof(ImmutableArray).Assembly.Location);
            var microsoftCodeAnalysis = directory.CopyFile(typeof(DiagnosticAnalyzer).Assembly.Location);

            var analyzerDependencyFile = CreateAnalyzerDependency();
            var analyzerMainFile = CreateMainAnalyzerWithDependency(analyzerDependencyFile);
            var loader = new ShadowCopyAnalyzerAssemblyLoader(Path.Combine(directory.Path, "AnalyzerAssemblyLoader"));

            var analyzerMainReference = new AnalyzerFileReference(analyzerMainFile.Path, loader);
            analyzerMainReference.AnalyzerLoadFailed += (_, e) => AssertEx.Fail(e.Exception.Message);
            var analyzerDependencyReference = new AnalyzerFileReference(analyzerDependencyFile.Path, loader);
            analyzerDependencyReference.AnalyzerLoadFailed += (_, e) => AssertEx.Fail(e.Exception.Message);

            var analyzers = analyzerMainReference.GetAnalyzersForAllLanguages();
            Assert.Equal(1, analyzers.Length);
            Assert.Equal("TestAnalyzer", analyzers[0].ToString());

            Assert.Equal(0, analyzerDependencyReference.GetAnalyzersForAllLanguages().Length);

            Assert.NotNull(analyzerDependencyReference.GetAssembly());

            TempFile CreateAnalyzerDependency()
            {
                var analyzerDependencySource = @"
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public abstract class AbstractTestAnalyzer : DiagnosticAnalyzer
{
    protected static string SomeString = nameof(SomeString);
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { throw new NotImplementedException(); } }
    public override void Initialize(AnalysisContext context) { throw new NotImplementedException(); }
}";

                var analyzerDependencyCompilation = CSharp.CSharpCompilation.Create(
                   "AnalyzerDependency",
                   new SyntaxTree[] { CSharp.SyntaxFactory.ParseSyntaxTree(analyzerDependencySource) },
                   new MetadataReference[]
                   {
                    TestReferences.NetStandard20.NetStandard,
                    TestReferences.NetStandard20.SystemRuntimeRef,
                    MetadataReference.CreateFromFile(immutable.Path),
                    MetadataReference.CreateFromFile(microsoftCodeAnalysis.Path)
                   },
                   new CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                return directory.CreateDirectory("AnalyzerDependency").CreateFile("AnalyzerDependency.dll").WriteAllBytes(analyzerDependencyCompilation.EmitToArray());
            }

            TempFile CreateMainAnalyzerWithDependency(TempFile analyzerDependency)
            {
                var analyzerMainSource = @"
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestAnalyzer : AbstractTestAnalyzer
{
    private static string SomeString2 = AbstractTestAnalyzer.SomeString;
}";
                var analyzerMainCompilation = CSharp.CSharpCompilation.Create(
                   "AnalyzerMain",
                   new SyntaxTree[] { CSharp.SyntaxFactory.ParseSyntaxTree(analyzerMainSource) },
                   new MetadataReference[]
                   {
                    TestReferences.NetStandard20.NetStandard,
                    TestReferences.NetStandard20.SystemRuntimeRef,
                    MetadataReference.CreateFromFile(immutable.Path),
                    MetadataReference.CreateFromFile(microsoftCodeAnalysis.Path),
                    MetadataReference.CreateFromFile(analyzerDependency.Path)
                   },
                   new CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                return directory.CreateDirectory("AnalyzerMain").CreateFile("AnalyzerMain.dll").WriteAllBytes(analyzerMainCompilation.EmitToArray());
            }
        }
    }
}

#else
#error unsupported configuration
#endif
