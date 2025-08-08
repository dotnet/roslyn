// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Definitions;

internal interface IXamlGoToDefinitionService : ILanguageService
{
    Task<ImmutableArray<XamlDefinition>> GetDefinitionsAsync(TextDocument document, int position, CancellationToken cancellationToken);
}
