// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Testing;
using Xunit;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;

public static partial class VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public sealed class Test : VisualBasicCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
    {
        private readonly SharedVerifierState _sharedState;

        static Test()
        {
            // If we have outdated defaults from the host unit test application targeting an older .NET Framework, use more
            // reasonable TLS protocol version for outgoing connections.
#pragma warning disable CA5364 // Do Not Use Deprecated Security Protocols
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable SYSLIB0014 // 'ServicePointManager' is obsolete
            if (ServicePointManager.SecurityProtocol == (SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls))
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CA5364 // Do Not Use Deprecated Security Protocols
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
#pragma warning restore SYSLIB0014 // 'ServicePointManager' is obsolete
        }

        public Test()
        {
            _sharedState = new SharedVerifierState(this, DefaultFileExt);

            MarkupOptions = Testing.MarkupOptions.UseFirstDescriptor;
        }

        public new string TestCode { set => base.TestCode = NormalizeToCRLF(value); }

        public new string FixedCode { set => base.FixedCode = NormalizeToCRLF(value); }

        public new string BatchFixedCode { set => base.BatchFixedCode = NormalizeToCRLF(value); }

        /// <summary>
        /// Normalizes line endings to CRLF (\r\n) to match the end_of_line=crlf editorconfig setting
        /// in <see cref="CodeFixVerifierHelper.ConvertOptionsToAnalyzerConfig"/>. This ensures consistent
        /// test behavior across Windows and Linux where raw string line endings differ.
        /// </summary>
        private static string NormalizeToCRLF(string value)
            => value.Replace("\r\n", "\n").Replace("\n", "\r\n");

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

        public Func<ImmutableArray<Diagnostic>, Diagnostic?>? DiagnosticSelector { get; set; }

        protected override async Task RunImplAsync(CancellationToken cancellationToken = default)
        {
            if (DiagnosticSelector is object)
            {
                Assert.True(CodeFixTestBehaviors.HasFlag(Testing.CodeFixTestBehaviors.FixOne), $"'{nameof(DiagnosticSelector)}' can only be used with '{nameof(Testing.CodeFixTestBehaviors)}.{nameof(Testing.CodeFixTestBehaviors.FixOne)}'");
            }

            _sharedState.Apply();

            // Skip normalization if the test explicitly sets FormattingOptions2.NewLine (e.g. to "\n")
            // since it intentionally tests specific line ending behavior.
            if (!Options.Any(kvp => ReferenceEquals(kvp.Key.Option, FormattingOptions2.NewLine)))
            {
                NormalizeSourceFileEndingsToCRLF(TestState);
                NormalizeSourceFileEndingsToCRLF(FixedState);
                NormalizeSourceFileEndingsToCRLF(BatchFixedState);
                EnsureEditorConfigInAdditionalProjects(TestState);
            }

            await base.RunImplAsync(cancellationToken);
        }

        /// <summary>
        /// Normalizes all source file line endings in the given state to CRLF so that tests
        /// produce consistent results across Windows and Linux. On Linux, raw string literals
        /// use LF but SyntaxFactory/NormalizeWhitespace generate CRLF, causing mismatches.
        /// </summary>
        private static void NormalizeSourceFileEndingsToCRLF(Testing.SolutionState state)
        {
            NormalizeSourceCollection(state.Sources);
            foreach (var project in state.AdditionalProjects.Values)
                NormalizeSourceCollection(project.Sources);
        }

        private static void NormalizeSourceCollection(Testing.SourceFileCollection sources)
        {
            for (var i = 0; i < sources.Count; i++)
            {
                var (filename, sourceText) = sources[i];
                var text = sourceText.ToString();
                var normalized = NormalizeToCRLF(text);
                if (text != normalized)
                    sources[i] = (filename, SourceText.From(normalized, sourceText.Encoding, sourceText.ChecksumAlgorithm));
            }
        }

        /// <summary>
        /// Ensures additional projects have the end_of_line=crlf editorconfig so that code fixes
        /// that modify documents in additional projects produce consistent line endings on Linux.
        /// </summary>
        private static void EnsureEditorConfigInAdditionalProjects(Testing.SolutionState state)
        {
            foreach (var project in state.AdditionalProjects.Values)
            {
                var hasEditorConfig = false;
                for (var i = 0; i < project.AnalyzerConfigFiles.Count; i++)
                {
                    if (project.AnalyzerConfigFiles[i].filename?.Contains(".editorconfig") == true)
                    {
                        hasEditorConfig = true;
                        break;
                    }
                }

                if (!hasEditorConfig)
                {
                    project.AnalyzerConfigFiles.Add(("/.editorconfig",
                        SourceText.From("root = true\r\n\r\n[*]\r\nend_of_line = crlf\r\n", Encoding.UTF8)));
                }
            }
        }

        protected override ParseOptions CreateParseOptions()
        {
            var parseOptions = (VisualBasicParseOptions)base.CreateParseOptions();
            return parseOptions.WithLanguageVersion(LanguageVersion);
        }

        protected override Diagnostic? TrySelectDiagnosticToFix(ImmutableArray<Diagnostic> fixableDiagnostics)
        {
            return DiagnosticSelector?.Invoke(fixableDiagnostics)
                ?? base.TrySelectDiagnosticToFix(fixableDiagnostics);
        }
    }
}
