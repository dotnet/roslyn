// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Settings;

namespace Microsoft.VisualStudio.Razor.Settings;

internal interface IAdvancedSettingsStorage
{
    ClientAdvancedSettings GetAdvancedSettings();

    Task OnChangedAsync(Action<ClientAdvancedSettings> changed);
}
