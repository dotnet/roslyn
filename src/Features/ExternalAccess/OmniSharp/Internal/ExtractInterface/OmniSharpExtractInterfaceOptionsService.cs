// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ExtractInterface;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Internal.ExtractInterface
{
    [Shared]
    [ExportWorkspaceService(typeof(IExtractInterfaceOptionsService))]
    internal class OmniSharpExtractInterfaceOptionsService : IExtractInterfaceOptionsService
    {
        private readonly IOmniSharpExtractInterfaceOptionsService _omniSharpExtractInterfaceOptionsService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OmniSharpExtractInterfaceOptionsService(IOmniSharpExtractInterfaceOptionsService omniSharpExtractInterfaceOptionsService)
        {
            _omniSharpExtractInterfaceOptionsService = omniSharpExtractInterfaceOptionsService;
        }

        public async Task<ExtractInterfaceOptionsResult> GetExtractInterfaceOptionsAsync(
            ISyntaxFactsService syntaxFactsService,
            INotificationService notificationService,
            List<ISymbol> extractableMembers,
            string defaultInterfaceName,
            List<string> conflictingTypeNames,
            string defaultNamespace,
            string generatedNameTypeParameterSuffix,
            string languageName,
            CancellationToken cancellationToken)
        {
            var result = await _omniSharpExtractInterfaceOptionsService.GetExtractInterfaceOptionsAsync(extractableMembers, defaultInterfaceName).ConfigureAwait(false);
            return new(
                result.IsCancelled,
                result.IncludedMembers,
                result.InterfaceName,
                result.FileName,
                (ExtractInterfaceOptionsResult.ExtractLocation)result.Location);
        }
    }
}
