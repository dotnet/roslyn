// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Utilities;

#if !CODE_STYLE
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
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

                MarkupOptions = Testing.MarkupOptions.UseFirstDescriptor;
            }

            /// <summary>
            /// Gets or sets the language version to use for the test. The default value is
            /// <see cref="LanguageVersion.CSharp8"/>.
            /// </summary>
            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp8;

            /// <inheritdoc cref="SharedVerifierState.Options"/>
            internal OptionsCollection Options => _sharedState.Options;

#if !CODE_STYLE
            internal CodeActionOptionsProvider CodeActionOptions
            {
                get => _sharedState.CodeActionOptions;
                set => _sharedState.CodeActionOptions = value;
            }
#endif
            /// <inheritdoc cref="SharedVerifierState.EditorConfig"/>
            public string? EditorConfig
            {
                get => _sharedState.EditorConfig;
                set => _sharedState.EditorConfig = value;
            }

            public Func<ImmutableArray<Diagnostic>, Diagnostic?>? DiagnosticSelector { get; set; }

            public Action<ImmutableArray<CodeAction>>? CodeActionsVerifier { get; set; }

            protected override async Task RunImplAsync(CancellationToken cancellationToken = default)
            {
                if (DiagnosticSelector is object)
                {
                    Assert.True(CodeFixTestBehaviors.HasFlag(Testing.CodeFixTestBehaviors.FixOne), $"'{nameof(DiagnosticSelector)}' can only be used with '{nameof(Testing.CodeFixTestBehaviors)}.{nameof(Testing.CodeFixTestBehaviors.FixOne)}'");
                }

                _sharedState.Apply();
                await base.RunImplAsync(cancellationToken);
            }

            protected override ParseOptions CreateParseOptions()
            {
                var parseOptions = (CSharpParseOptions)base.CreateParseOptions();
                return parseOptions.WithLanguageVersion(LanguageVersion);
            }

            protected override CompilationOptions CreateCompilationOptions()
            {
                var compilationOptions = (CSharpCompilationOptions)base.CreateCompilationOptions();
                return compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
            }

#if !CODE_STYLE
            protected override AnalyzerOptions GetAnalyzerOptions(Project project)
                => new WorkspaceAnalyzerOptions(base.GetAnalyzerOptions(project), _sharedState.GetIdeAnalyzerOptions(project));

            protected override CodeFixContext CreateCodeFixContext(Document document, TextSpan span, ImmutableArray<Diagnostic> diagnostics, Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix, CancellationToken cancellationToken)
                => new(document, span, diagnostics, registerCodeFix, _sharedState.CodeActionOptions, cancellationToken);

            protected override FixAllContext CreateFixAllContext(
                Document? document,
                TextSpan? diagnosticSpan,
                Project project,
                CodeFixProvider codeFixProvider,
                FixAllScope scope,
                string? codeActionEquivalenceKey,
                IEnumerable<string> diagnosticIds,
                DiagnosticSeverity minimumSeverity,
                FixAllContext.DiagnosticProvider fixAllDiagnosticProvider,
                CancellationToken cancellationToken)
                => new(new FixAllState(
                    fixAllProvider: NoOpFixAllProvider.Instance,
                    diagnosticSpan,
                    document,
                    project,
                    codeFixProvider,
                    scope,
                    codeActionEquivalenceKey,
                    diagnosticIds,
                    fixAllDiagnosticProvider,
                    _sharedState.CodeActionOptions),
                  new ProgressTracker(), cancellationToken);
#endif

            protected override Diagnostic? TrySelectDiagnosticToFix(ImmutableArray<Diagnostic> fixableDiagnostics)
            {
                return DiagnosticSelector?.Invoke(fixableDiagnostics)
                    ?? base.TrySelectDiagnosticToFix(fixableDiagnostics);
            }

            protected override ImmutableArray<CodeAction> FilterCodeActions(ImmutableArray<CodeAction> actions)
            {
                CodeActionsVerifier?.Invoke(actions);
                return base.FilterCodeActions(actions);
            }
        }
    }
}
