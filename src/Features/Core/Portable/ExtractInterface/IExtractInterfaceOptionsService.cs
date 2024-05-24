// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;

namespace Microsoft.CodeAnalysis.ExtractInterface;

internal interface IExtractInterfaceOptionsService : IWorkspaceService
{
    Task<ExtractInterfaceOptionsResult> GetExtractInterfaceOptionsAsync(
        ISyntaxFactsService syntaxFactsService,
        INotificationService notificationService,
        List<ISymbol> extractableMembers,
        string defaultInterfaceName,
        List<string> conflictingTypeNames,
        string defaultNamespace,
        string generatedNameTypeParameterSuffix,
        string languageName,
        CleanCodeGenerationOptionsProvider fallbackOptions,
        CancellationToken cancellationToken);
}
