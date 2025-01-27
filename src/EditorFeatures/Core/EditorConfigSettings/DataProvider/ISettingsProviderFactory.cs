// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;

internal interface ISettingsProviderFactory<TData>
{
    ISettingsProvider<TData> GetForFile(string filePath);
}
