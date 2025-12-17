// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;

#if Unified_ExternalAccess
namespace Microsoft.VisualStudio.ExternalAccess.Apex;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Apex;
#endif

internal interface IApexAsynchronousOperationListenerProviderAccessor
{
    Task WaitAllAsync(string[] featureNames = null, Action eventProcessingAction = null, TimeSpan? timeout = null);
}
