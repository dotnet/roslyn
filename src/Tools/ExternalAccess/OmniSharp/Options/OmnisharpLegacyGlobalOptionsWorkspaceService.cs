// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Options;
using System.Threading.Tasks;
using System.Threading;

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
        public OmnisharpLegacyGlobalOptionsWorkspaceService()
        {
            _provider = new OmniSharpCleanCodeGenerationOptionsProvider();
        }

        public bool RazorUseTabs
            => LineFormattingOptions.Default.UseTabs;

        public int RazorTabSize
            => LineFormattingOptions.Default.TabSize;

        public CleanCodeGenerationOptionsProvider CleanCodeGenerationOptionsProvider
            => _provider;

        /// TODO: remove. https://github.com/dotnet/roslyn/issues/57283
        public bool InlineHintsOptionsDisplayAllOverride
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
            public override ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            {
                return new ValueTask<CleanCodeGenerationOptions>(CleanCodeGenerationOptions.GetDefault(languageServices));
            }
        }
    }
}
