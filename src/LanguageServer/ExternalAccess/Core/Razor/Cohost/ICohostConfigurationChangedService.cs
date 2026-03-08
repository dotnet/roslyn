// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Razor.Cohost;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
#endif

internal interface ICohostConfigurationChangedService
{
    Task OnConfigurationChangedAsync(RazorCohostRequestContext requestContext, CancellationToken cancellationToken);
}
