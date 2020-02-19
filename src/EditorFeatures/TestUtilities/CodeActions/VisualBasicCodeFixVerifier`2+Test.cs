// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public static partial class VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : VisualBasicCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
        {
            public Test()
            {
                MarkupOptions = Testing.MarkupOptions.UseFirstDescriptor;

                SolutionTransforms.Add((solution, projectId) =>
                {
                    var parseOptions = (VisualBasicParseOptions)solution.GetProject(projectId).ParseOptions;
                    solution = solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion));

                    return CodeFixVerifierHelper.ApplyOptions(solution, Options);
                });
            }

            /// <summary>
            /// Gets or sets the language version to use for the test. The default value is
            /// <see cref="LanguageVersion.VisualBasic16"/>.
            /// </summary>
            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.VisualBasic16;

            /// <summary>
            /// Gets a collection of options to apply to <see cref="Solution.Options"/> for testing. Values may be added
            /// using a collection initializer.
            /// </summary>
            public OptionsCollection Options { get; } = new OptionsCollection(LanguageNames.VisualBasic);

            protected override string DefaultFilePathPrefix
            {
                get
                {
#if CODE_STYLE
                    // For CodeStyle layer tests, we needed rooted file path for documents.
                    // See comments in "CodeFixVerifierHelper.ApplyOptions" for details.
                    return Path.Combine(CodeFixVerifierHelper.DefaultRootFilePath, base.DefaultFilePathPrefix);
#else
                    return base.DefaultFilePathPrefix;
#endif
                }
            }

            protected override AnalyzerOptions GetAnalyzerOptions(Project project)
            {
                var analyzerOptions = base.GetAnalyzerOptions(project);

#if !CODE_STYLE
                analyzerOptions = new WorkspaceAnalyzerOptions(analyzerOptions, project.Solution);
#endif

                return analyzerOptions;
            }
        }
    }
}
