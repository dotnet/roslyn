// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Net;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.CodeRefactorings;

#if !CODE_STYLE
using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public static partial class CSharpCodeRefactoringVerifier<TCodeRefactoring>
        where TCodeRefactoring : CodeRefactoringProvider, new()
    {
        public class Test : CSharpCodeRefactoringTest<TCodeRefactoring, XUnitVerifier>
        {
            static Test()
            {
                // If we have outdated defaults from the host unit test application targeting an older .NET Framework, use more
                // reasonable TLS protocol version for outgoing connections.
#pragma warning disable CA5364 // Do Not Use Deprecated Security Protocols
#pragma warning disable CS0618 // Type or member is obsolete
                if (ServicePointManager.SecurityProtocol == (SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls))
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CA5364 // Do Not Use Deprecated Security Protocols
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                }
            }

            public Test()
            {
                SolutionTransforms.Add((solution, projectId) =>
                {
                    var parseOptions = (CSharpParseOptions)solution.GetProject(projectId)!.ParseOptions!;
                    solution = solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion));

                    var compilationOptions = solution.GetProject(projectId)!.CompilationOptions!;
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

                    var (analyzerConfigSource, remainingOptions) = CodeFixVerifierHelper.ConvertOptionsToAnalyzerConfig(DefaultFileExt, EditorConfig, Options);
                    if (analyzerConfigSource is object)
                    {
                        foreach (var id in solution.ProjectIds)
                        {
                            var documentId = DocumentId.CreateNewId(id, ".editorconfig");
                            solution = solution.AddAnalyzerConfigDocument(documentId, ".editorconfig", analyzerConfigSource, filePath: "/.editorconfig");
                        }
                    }

#if !CODE_STYLE
                    var options = solution.Options;
                    foreach (var (key, value) in remainingOptions)
                    {
                        options = options.WithChangedOption(key, value);
                    }

                    solution = solution.WithOptions(options);
#endif

                    return solution;
                });
            }

            /// <summary>
            /// Gets or sets the language version to use for the test. The default value is
            /// <see cref="LanguageVersion.CSharp8"/>.
            /// </summary>
            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp8;

            /// <summary>
            /// Gets a collection of options to apply to <see cref="Solution.Options"/> for testing. Values may be added
            /// using a collection initializer.
            /// </summary>
            internal OptionsCollection Options { get; } = new OptionsCollection(LanguageNames.CSharp);

            public string? EditorConfig { get; set; }

#if !CODE_STYLE
            protected override AnalyzerOptions GetAnalyzerOptions(Project project)
                => new WorkspaceAnalyzerOptions(base.GetAnalyzerOptions(project), project.Solution);
#endif
        }
    }
}
