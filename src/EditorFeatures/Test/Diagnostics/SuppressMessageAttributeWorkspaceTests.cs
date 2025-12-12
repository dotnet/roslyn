// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Diagnostics;

[UseExportProvider]
public sealed class SuppressMessageAttributeWorkspaceTests : SuppressMessageAttributeTests
{
    private static readonly TestComposition s_compositionWithMockDiagnosticUpdateSourceRegistrationService = EditorTestCompositions.EditorFeatures;

    private static readonly Lazy<MetadataReference> _unconditionalSuppressMessageRef = new(() =>
    {
        return CSharpCompilation.Create("unconditionalsuppress",
             options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            syntaxTrees: [CSharpSyntaxTree.ParseText("""
        namespace System.Diagnostics.CodeAnalysis
        {
            [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple=true, Inherited=false)]
            public sealed class UnconditionalSuppressMessageAttribute : System.Attribute
            {
                public UnconditionalSuppressMessageAttribute(string category, string checkId)
                {
                    Category = category;
                    CheckId = checkId;
                }
                public string Category { get; }
                public string CheckId { get; }
                public string Scope { get; set; }
                public string Target { get; set; }
                public string MessageId { get; set; }
                public string Justification { get; set; }
            }
        }
        """)],
            references: [TestBase.MscorlibRef]).EmitToImageReference();
    }, LazyThreadSafetyMode.PublicationOnly);

    protected override async Task VerifyAsync(string source, string language, DiagnosticAnalyzer[] analyzers, DiagnosticDescription[] expectedDiagnostics, string rootNamespace = null)
    {
        using var workspace = CreateWorkspaceFromFile(source, language, rootNamespace);

        workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(
        [
            new AnalyzerImageReference([.. analyzers])
        ]).WithProjectMetadataReferences(
            workspace.Projects.Single().Id,
            workspace.Projects.Single().MetadataReferences.Append(_unconditionalSuppressMessageRef.Value)));

        var documentId = workspace.Documents[0].Id;
        var document = workspace.CurrentSolution.GetDocument(documentId);
        var span = (await document.GetSyntaxRootAsync()).FullSpan;

        var actualDiagnostics = await DiagnosticProviderTestUtilities.GetAllDiagnosticsAsync(workspace, document, span);
        actualDiagnostics.Verify(expectedDiagnostics);
    }

    private static TestWorkspace CreateWorkspaceFromFile(string source, string language, string rootNamespace)
    {
        if (language == LanguageNames.CSharp)
        {
            return TestWorkspace.CreateCSharp(source, composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService);
        }
        else
        {
            return TestWorkspace.CreateVisualBasic(
                source,
                compilationOptions: new VisualBasic.VisualBasicCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary, rootNamespace: rootNamespace),
                composition: s_compositionWithMockDiagnosticUpdateSourceRegistrationService);
        }
    }

    protected override bool ConsiderArgumentsForComparingDiagnostics
    {
        get
        {
            // Round tripping diagnostics from DiagnosticData causes the Arguments info stored within compiler DiagnosticWithInfo to be lost, so don't compare Arguments in IDE.
            // NOTE: We will still compare squiggled text for the diagnostics, which is also a sufficient test.
            return false;
        }
    }

    [Fact]
    public async Task AnalyzerExceptionDiagnosticsWithDifferentContext()
    {
        var diagnostic = Diagnostic("AD0001", null);

        // expect 3 different diagnostics with 3 different contexts.
        await VerifyCSharpAsync("""
            public class C
            {
            }
            public class C1
            {
            }
            public class C2
            {
            }
            """,
            [new ThrowExceptionForEachNamedTypeAnalyzer(ExceptionDispatchInfo.Capture(new Exception()))],
            diagnostics: [diagnostic, diagnostic, diagnostic]);
    }

    [Fact]
    public async Task AnalyzerExceptionFromSupportedDiagnosticsCall()
    {
        var diagnostic = Diagnostic("AD0001", null);

        await VerifyCSharpAsync("public class C { }",
            [new ThrowExceptionFromSupportedDiagnostics(new Exception())],
            diagnostics: [diagnostic]);
    }
}
