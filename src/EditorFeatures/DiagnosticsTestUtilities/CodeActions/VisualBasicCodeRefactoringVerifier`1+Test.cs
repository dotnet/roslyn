// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

#if !CODE_STYLE
using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public static partial class VisualBasicCodeRefactoringVerifier<TCodeRefactoring>
        where TCodeRefactoring : CodeRefactoringProvider, new()
    {
        public class Test : VisualBasicCodeRefactoringTest<TCodeRefactoring, XUnitVerifier>
        {
            private readonly SharedVerifierState _sharedState;

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
                _sharedState = new SharedVerifierState(this, DefaultFileExt);

                SolutionTransforms.Add((solution, projectId) =>
                {
                    var parseOptions = (VisualBasicParseOptions)solution.GetProject(projectId)!.ParseOptions!;
                    solution = solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion));

                    return solution;
                });
            }

            /// <summary>
            /// Gets or sets the language version to use for the test. The default value is
            /// <see cref="LanguageVersion.VisualBasic16"/>.
            /// </summary>
            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.VisualBasic16;

            /// <inheritdoc cref="SharedVerifierState.Options"/>
            internal OptionsCollection Options => _sharedState.Options;

            /// <inheritdoc cref="SharedVerifierState.EditorConfig"/>
            public string? EditorConfig
            {
                get => _sharedState.EditorConfig;
                set => _sharedState.EditorConfig = value;
            }

            protected override async Task RunImplAsync(CancellationToken cancellationToken)
            {
                _sharedState.Apply();
                await base.RunImplAsync(cancellationToken);
            }

#if !CODE_STYLE
            protected override AnalyzerOptions GetAnalyzerOptions(Project project)
                => new WorkspaceAnalyzerOptions(base.GetAnalyzerOptions(project), project.Solution, _sharedState.GetIdeAnalyzerOptions(project));
#endif
        }
    }
}
