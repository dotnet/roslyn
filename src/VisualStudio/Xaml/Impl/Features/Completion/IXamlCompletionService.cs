// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Completion;

internal interface IXamlCompletionService : ILanguageService
{
    Task<XamlCompletionResult> GetCompletionsAsync(XamlCompletionContext completionContext, CancellationToken cancellationToken);
    Task<ISymbol> GetSymbolAsync(XamlCompletionContext completionContext, string label, CancellationToken cancellationToken);
}
