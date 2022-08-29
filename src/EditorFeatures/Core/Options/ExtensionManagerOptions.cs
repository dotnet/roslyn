// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal sealed class ExtensionManagerOptions
    {
        public static readonly Option2<bool> DisableCrashingExtensions = new(
            nameof(ExtensionManagerOptions), nameof(DisableCrashingExtensions), defaultValue: true);
    }
}
