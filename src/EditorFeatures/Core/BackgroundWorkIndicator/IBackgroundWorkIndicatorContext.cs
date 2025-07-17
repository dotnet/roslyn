// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;

internal interface IBackgroundWorkIndicatorContext : IUIThreadOperationContext
{
    /// <summary>
    /// Allows clients to temporarily suppress auto cancel behaviors when they want to apply edits or navigate without canceling.
    /// </summary>
    Task<IAsyncDisposable> SuppressAutoCancelAsync();
}
