// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.Analyzers.NamespaceFileSync.NamespaceFileSyncDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.CodeFixes.NamespaceSync.CSharpNamespaceSyncCodeFixProvider>;
using System.IO;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.NamespaceFileSync
{
    public class NamespaceFileSyncTests
    {
        private static readonly string Directory = Path.Combine("Test", "Directory");
        private static readonly string EditorConfig = @$"
is_global=true
build_property.RootNamespace = Test
build_property.ProjectDir = {Directory}
";

        [Fact]
        public async Task DiagnosticReported()
        {
            var testState = new VerifyCS.Test
            {
                EditorConfig = EditorConfig
            };

            var fileName = Path.Combine(Directory, "C.cs");
            testState.TestState.Sources.Add((fileName, @"namespace OtherNamespace { class C { } }"));

            var diagnostic = VerifyCS.Diagnostic(IDEDiagnosticIds.NamespaceSyncAnalyzerDiagnosticId)
                .WithSpan(fileName, 1, 11, 1, 25)
                .WithSeverity(DiagnosticSeverity.Warning);

            testState.ExpectedDiagnostics.Add(diagnostic);
            await testState.RunAsync();
        }
    }
}
