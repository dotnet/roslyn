// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.CodeCleanup;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Options
{
    /// <summary>
    /// Enables legacy APIs to access global options from workspace.
    /// </summary>
    [ExportWorkspaceService(typeof(ILegacyGlobalOptionsWorkspaceService)), Shared]
    internal sealed class OmnisharpLegacyGlobalOptionsWorkspaceService : ILegacyGlobalOptionsWorkspaceService
    {
        private readonly CleanCodeGenerationOptionsProvider _provider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OmnisharpLegacyGlobalOptionsWorkspaceService(IOmniSharpLineFormattingOptionsProvider lineFormattingOptionsProvider)
        {
            _provider = new OmniSharpCleanCodeGenerationOptionsProvider(lineFormattingOptionsProvider);
        }

        public bool RazorUseTabs
            => LineFormattingOptions.Default.UseTabs;

        public int RazorTabSize
            => LineFormattingOptions.Default.TabSize;

        public CleanCodeGenerationOptionsProvider CleanCodeGenerationOptionsProvider
            => _provider;

        /// TODO: remove. https://github.com/dotnet/roslyn/issues/57283
#pragma warning disable CA1822 // Mark members as static
        public bool InlineHintsOptionsDisplayAllOverride
#pragma warning restore CA1822
        {
            get => false;
            set { }
        }

        public bool GenerateOverrides
        {
            get => true;
            set { }
        }

        public bool GetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language)
            => false;

        public void SetGenerateEqualsAndGetHashCodeFromMembersGenerateOperators(string language, bool value)
        {
        }

        public bool GetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language)
            => false;

        public void SetGenerateEqualsAndGetHashCodeFromMembersImplementIEquatable(string language, bool value)
        {
        }

        public bool GetGenerateConstructorFromMembersOptionsAddNullChecks(string language)
            => false;

        public void SetGenerateConstructorFromMembersOptionsAddNullChecks(string language, bool value)
        {
        }

        internal sealed class OmniSharpCleanCodeGenerationOptionsProvider : AbstractCleanCodeGenerationOptionsProvider
        {
            private readonly IOmniSharpLineFormattingOptionsProvider _lineFormattingOptionsProvider;

            public OmniSharpCleanCodeGenerationOptionsProvider(IOmniSharpLineFormattingOptionsProvider lineFormattingOptionsProvider)
            {
                _lineFormattingOptionsProvider = lineFormattingOptionsProvider;
            }

            public override ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            {
                var lineFormattingOptions = _lineFormattingOptionsProvider.GetLineFormattingOptions();
                var codeGenerationOptions = CleanCodeGenerationOptions.GetDefault(languageServices) with
                {
                    CleanupOptions = CodeCleanupOptions.GetDefault(languageServices) with
                    {
                        FormattingOptions = SyntaxFormattingOptions.GetDefault(languageServices) with
                        {
                            LineFormatting = new()
                            {
                                IndentationSize = lineFormattingOptions.IndentationSize,
                                TabSize = lineFormattingOptions.TabSize,
                                UseTabs = lineFormattingOptions.UseTabs,
                                NewLine = lineFormattingOptions.NewLine,
                            }
                        }
                    }
                };
                return new ValueTask<CleanCodeGenerationOptions>(codeGenerationOptions);
            }
        }
    }
}
