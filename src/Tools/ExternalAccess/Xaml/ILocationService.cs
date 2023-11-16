// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

internal interface ILocationService
{
    Task<LSP.Location[]> GetSymbolDefinitionLocationsAsync(ISymbol symbol, Project project, CancellationToken cancellationToken);
    Task<LSP.Location?> GetLocationAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);

}
