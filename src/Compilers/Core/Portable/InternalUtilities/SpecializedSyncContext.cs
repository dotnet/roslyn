#pragma warning disable IDE0073 // We are preserving the original copyright header for this file

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This was copied from https://github.com/microsoft/vs-threading/blob/4332894cbeaad95797e24004cf3adc5abc5b9be7/src/Microsoft.VisualStudio.Threading/SpecializedSyncContext.cs with some changes to
// match naming and conventions in Roslyn.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Roslyn.Utilities;

/// <summary>
/// A structure that applies and reverts changes to the <see cref="SynchronizationContext"/>.
/// </summary>
internal readonly struct SpecializedSyncContext : IDisposable
{
    /// <summary>
    /// A flag indicating whether the non-default constructor was invoked.
    /// </summary>
    private readonly bool _initialized;

    /// <summary>
    /// The SynchronizationContext to restore when <see cref="Dispose"/> is invoked.
    /// </summary>
    private readonly SynchronizationContext? _prior;

    /// <summary>
    /// The SynchronizationContext applied when this struct was constructed.
    /// </summary>
    private readonly SynchronizationContext? _appliedContext;

    /// <summary>
    /// A value indicating whether to check that the applied SyncContext is still the current one when the original is restored.
    /// </summary>
    private readonly bool _checkForChangesOnRevert;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpecializedSyncContext"/> struct.
    /// </summary>
    private SpecializedSyncContext(SynchronizationContext? syncContext, bool checkForChangesOnRevert)
    {
        this._initialized = true;
        this._prior = SynchronizationContext.Current;
        this._appliedContext = syncContext;
        this._checkForChangesOnRevert = checkForChangesOnRevert;
        SynchronizationContext.SetSynchronizationContext(syncContext);
    }

    /// <summary>
    /// Applies the specified <see cref="SynchronizationContext"/> to the caller's context.
    /// </summary>
    /// <param name="syncContext">The synchronization context to apply.</param>
    /// <param name="checkForChangesOnRevert">A value indicating whether to check that the applied SyncContext is still the current one when the original is restored.</param>
    public static SpecializedSyncContext Apply(SynchronizationContext? syncContext, bool checkForChangesOnRevert = true)
    {
        return new SpecializedSyncContext(syncContext, checkForChangesOnRevert);
    }

    /// <summary>
    /// Reverts the SynchronizationContext to its previous instance.
    /// </summary>
    public void Dispose()
    {
        if (this._initialized)
        {
            if (this._checkForChangesOnRevert && SynchronizationContext.Current != this._appliedContext)
                FatalError.ReportNonFatalError(new Exception("The SynchronizationContext was changed since it was applied."));

            SynchronizationContext.SetSynchronizationContext(this._prior);
        }
    }
}
