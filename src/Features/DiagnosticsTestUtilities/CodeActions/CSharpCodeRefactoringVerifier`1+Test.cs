// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

#if !CODE_STYLE
using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;

public static partial class CSharpCodeRefactoringVerifier<TCodeRefactoring>
    where TCodeRefactoring : CodeRefactoringProvider, new()
{
    public class Test : CSharpCodeRefactoringTest<TCodeRefactoring, DefaultVerifier>
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
            this.FixedState.InheritanceMode = StateInheritanceMode.AutoInherit;
        }

        /// <summary>
        /// Gets or sets the language version to use for the test. The default value is
        /// <see cref="LanguageVersion.CSharp8"/>.
        /// </summary>
        public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp8;

        /// <inheritdoc cref="SharedVerifierState.Options"/>
        internal OptionsCollection Options => _sharedState.Options;

        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)]
        public new string TestCode { set => base.TestCode = value; }

        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)]
        public new string FixedCode { set => base.FixedCode = value; }

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
            var parseOptions = (CSharpParseOptions)base.CreateParseOptions();
            return parseOptions.WithLanguageVersion(LanguageVersion);
        }

        protected override CompilationOptions CreateCompilationOptions()
        {
            var compilationOptions = (CSharpCompilationOptions)base.CreateCompilationOptions();
            return compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
        }

#if !CODE_STYLE
        /// <summary>
        /// The <see cref="TestHost"/> we want this test to run in.  Defaults to <see cref="TestHost.OutOfProcess"/> if unspecified.
        /// </summary>
        public TestHost TestHost { get; set; } = TestHost.OutOfProcess;

        private static readonly TestComposition s_editorFeaturesOOPComposition = FeaturesTestCompositions.Features.WithTestHostParts(TestHost.OutOfProcess);

        protected override Task<Workspace> CreateWorkspaceImplAsync()
        {
            if (TestHost == TestHost.InProcess)
                return base.CreateWorkspaceImplAsync();

            var hostServices = s_editorFeaturesOOPComposition.GetHostServices();
            var workspace = new AdhocWorkspace(hostServices);
            return Task.FromResult<Workspace>(workspace);
        }
#endif
    }
}
