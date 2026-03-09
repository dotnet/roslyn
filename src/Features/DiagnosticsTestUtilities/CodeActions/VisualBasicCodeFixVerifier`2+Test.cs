// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Testing;
using Xunit;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
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

            // Ensure consistent line endings across platforms.
            // NormalizeWhitespace() hardcodes \r\n, so configure the formatter to also use \r\n.
            // Use Set (not Add) so tests can override this default without a duplicate key error.
            _sharedState.Options.Set(FormattingOptions2.NewLine, "\r\n");
        }

        public new string TestCode { set => base.TestCode = value.Replace("\r\n", "\n").Replace("\n", "\r\n"); }

        public new string FixedCode { set => base.FixedCode = value.Replace("\r\n", "\n").Replace("\n", "\r\n"); }

        public new string BatchFixedCode { set => base.BatchFixedCode = value.Replace("\r\n", "\n").Replace("\n", "\r\n"); }

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
            await base.RunImplAsync(cancellationToken);
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
