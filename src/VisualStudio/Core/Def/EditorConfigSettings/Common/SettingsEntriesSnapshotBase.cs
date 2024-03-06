// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

internal abstract class SettingsEntriesSnapshotBase<T> : WpfTableEntriesSnapshotBase
{
    private readonly ImmutableArray<T> _data;
    private readonly int _currentVersionNumber;

    public SettingsEntriesSnapshotBase(ImmutableArray<T> data, int currentVersionNumber)
    {
        _data = data;
        _currentVersionNumber = currentVersionNumber;
    }

    public override int VersionNumber => _currentVersionNumber;
    public override int Count => _data.Length;

    public override bool TryGetValue(int index, string keyName, out object? content)
    {
        T? result;
        try
        {
            if (index < 0 || index > _data.Length)
            {
                content = null;
                return false;
            }

            result = _data[index];

            if (result == null)
            {
                content = null;
                return false;
            }
        }
        catch (Exception)
        {
            content = null;
            return false;
        }

        return TryGetValue(result, keyName, out content);
    }

    protected abstract bool TryGetValue(T result, string keyName, out object? content);
}
