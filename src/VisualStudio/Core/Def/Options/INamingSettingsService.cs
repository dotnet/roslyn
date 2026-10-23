// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal interface INamingSettingsService
{
    void Start();
    Task<NamingSettingsProvider> GetProviderAsync(string language, CancellationToken cancellationToken);
    Task<NamingSettingsCoordinator> GetCoordinatorAsync(string language, CancellationToken cancellationToken);
    Task ResetAsync(string language, CancellationToken cancellationToken);
}
