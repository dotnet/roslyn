// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

/// <summary>
/// This derivation of <see cref="ObservableCollection{T}"/> also supports raising an initialized event through
/// <see cref="ISupportInitializeNotification"/>. This is used to show the spinning icon in the solution explorer
/// the first time you expand it.
/// </summary>
internal sealed class BulkObservableCollectionWithInit<T> : BulkObservableCollection<T>, ISupportInitializeNotification
{
    private bool _isInitialized;

    public bool IsInitialized
    {
        get => _isInitialized;
        set
        {
            if (_isInitialized != value)
            {
                _isInitialized = value;
                Initialized?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? Initialized;

    void ISupportInitialize.BeginInit()
    {
    }

    void ISupportInitialize.EndInit()
    {
    }
}
