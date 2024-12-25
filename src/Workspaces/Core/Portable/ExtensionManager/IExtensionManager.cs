// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Extensions;

internal interface IExtensionManager : IWorkspaceService
{
    bool IsDisabled(object provider);

    /// <summary>
    /// Returns <see langword="true"/> to make it easy to use in an exception filter.  Note: will be called with any
    /// exception, so this should not do anything in the case of <see cref="OperationCanceledException"/>.
    /// </summary>
    bool HandleException(object provider, Exception exception);
}
