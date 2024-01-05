﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

#if !CODE_STYLE
using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Text;
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

            /// <summary>
            /// The set of code action <see cref="CodeAction.Title"/>s offered the user in this exact order.
            /// Set this to ensure that a very specific set of actions is offered.
            /// </summary>
            public string[]? ExactActionSetOffered { get; set; }

            protected override async Task RunImplAsync(CancellationToken cancellationToken)
            {
                _sharedState.Apply();
                await base.RunImplAsync(cancellationToken);
            }

            protected override ImmutableArray<CodeAction> FilterCodeActions(ImmutableArray<CodeAction> actions)
            {
                var result = base.FilterCodeActions(actions);

                if (ExactActionSetOffered != null)
                {
                    Verify.SequenceEqual(ExactActionSetOffered, result.SelectAsArray(a => a.Title));
                }

                return result;
            }

            protected override ParseOptions CreateParseOptions()
            {
                var parseOptions = (VisualBasicParseOptions)base.CreateParseOptions();
                return parseOptions.WithLanguageVersion(LanguageVersion);
            }

#if !CODE_STYLE

            protected override AnalyzerOptions GetAnalyzerOptions(Project project)
                => new WorkspaceAnalyzerOptions(base.GetAnalyzerOptions(project), _sharedState.GetIdeAnalyzerOptions(project));

            protected override CodeRefactoringContext CreateCodeRefactoringContext(Document document, TextSpan span, Action<CodeAction> registerRefactoring, CancellationToken cancellationToken)
                => new CodeRefactoringContext(document, span, (action, textSpan) => registerRefactoring(action), _sharedState.CodeActionOptions, cancellationToken);

            /// <summary>
            /// The <see cref="TestHost"/> we want this test to run in.  Defaults to <see cref="TestHost.InProcess"/> if unspecified.
            /// </summary>
            public TestHost TestHost { get; set; } = TestHost.InProcess;

            private static readonly TestComposition s_editorFeaturesOOPComposition = EditorTestCompositions.EditorFeatures.WithTestHostParts(TestHost.OutOfProcess);

            protected override Workspace CreateWorkspaceImpl()
            {
                if (TestHost == TestHost.InProcess)
                    return base.CreateWorkspaceImpl();

                var hostServices = s_editorFeaturesOOPComposition.GetHostServices();
                var workspace = new AdhocWorkspace(hostServices);
                return workspace;
            }

#endif
        }
    }
}
