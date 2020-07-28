// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

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
                    TestMetadata.NetStandard20.mscorlib,
                    TestMetadata.NetStandard20.netstandard,
                    TestMetadata.NetStandard20.SystemRuntime,
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
                        TestMetadata.NetStandard20.mscorlib,
                        TestMetadata.NetStandard20.netstandard,
                        TestMetadata.NetStandard20.SystemRuntime,
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
