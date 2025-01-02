// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;

internal interface ISettingUpdater<TSetting, TValue>
{
    void QueueUpdate(TSetting setting, TValue value);
    Task<SourceText?> GetChangedEditorConfigAsync(SourceText sourceText, CancellationToken token);
    Task<bool> HasAnyChangesAsync();
}
