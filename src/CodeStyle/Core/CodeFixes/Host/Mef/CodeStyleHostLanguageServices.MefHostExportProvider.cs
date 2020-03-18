﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    internal sealed partial class CodeStyleHostLanguageServices : HostLanguageServices
    {
        private static readonly ConditionalWeakTable<HostLanguageServices, CodeStyleHostLanguageServices> s_mappedLanguageServices =
            new ConditionalWeakTable<HostLanguageServices, CodeStyleHostLanguageServices>();
        private static readonly ConditionalWeakTable<string, MefHostExportProvider> s_exportProvidersByLanguageCache =
            new ConditionalWeakTable<string, MefHostExportProvider>();

        private readonly HostLanguageServices _hostLanguageServices;
        private readonly HostLanguageServices _codeStyleLanguageServices;

        private CodeStyleHostLanguageServices(HostLanguageServices hostLanguageServices)
        {
            _hostLanguageServices = hostLanguageServices;

            var exportProvider = s_exportProvidersByLanguageCache.GetValue(hostLanguageServices.Language, MefHostExportProvider.Create);
            _codeStyleLanguageServices = new MefWorkspaceServices(exportProvider, hostLanguageServices.WorkspaceServices.Workspace)
                .GetLanguageServices(hostLanguageServices.Language);
        }

        public static CodeStyleHostLanguageServices? GetMappedCodeStyleLanguageServices(HostLanguageServices? hostLanguageServices)
            => hostLanguageServices != null ? s_mappedLanguageServices.GetValue(hostLanguageServices, Create) : null;

        public static CodeStyleHostLanguageServices GetRequiredMappedCodeStyleLanguageServices(HostLanguageServices hostLanguageServices)
            => s_mappedLanguageServices.GetValue(hostLanguageServices, Create);

        private static CodeStyleHostLanguageServices Create(HostLanguageServices hostLanguageServices)
            => new CodeStyleHostLanguageServices(hostLanguageServices);

        public override HostWorkspaceServices WorkspaceServices => _hostLanguageServices.WorkspaceServices;

        public override string Language => _hostLanguageServices.Language;

        [return: MaybeNull]
        public override TLanguageService GetService<TLanguageService>()
        {
            return _codeStyleLanguageServices.GetService<TLanguageService>() ?? _hostLanguageServices.GetService<TLanguageService>();
        }
    }
}
