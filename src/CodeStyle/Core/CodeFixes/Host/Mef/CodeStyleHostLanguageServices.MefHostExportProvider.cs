// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    internal sealed partial class CodeStyleHostLanguageServices : HostLanguageServices
    {
        private static readonly ConditionalWeakTable<LanguageServices, LanguageServices> s_extendedServices = new();
        private static readonly ConditionalWeakTable<string, MefHostExportProvider> s_exportProvidersByLanguageCache = new();

        private readonly LanguageServices _languageServices;
        private readonly LanguageServices _codeStyleLanguageServices;
        public override HostWorkspaceServices WorkspaceServices { get; }

        [SuppressMessage("ApiDesign", "RS0030:Do not used banned APIs", Justification = "This is the replacement API")]
        private CodeStyleHostLanguageServices(LanguageServices languageServices)
        {
            _languageServices = languageServices;

            var exportProvider = s_exportProvidersByLanguageCache.GetValue(languageServices.Language, MefHostExportProvider.Create);
            WorkspaceServices = new MefWorkspaceServices(exportProvider);
            _codeStyleLanguageServices = WorkspaceServices.GetLanguageServices(languageServices.Language).LanguageServices;
        }

        public static LanguageServices GetExtendedLanguageServices(LanguageServices languageServices)
            => s_extendedServices.GetValue(languageServices, Create);

        private static LanguageServices Create(LanguageServices languageServices)
            => new CodeStyleHostLanguageServices(languageServices).LanguageServices;

        public override string Language => _languageServices.Language;

        public override TLanguageService? GetService<TLanguageService>()
            where TLanguageService : default
            => _codeStyleLanguageServices.GetService<TLanguageService>() ?? _languageServices.GetService<TLanguageService>();
    }
}
