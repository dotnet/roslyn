// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.Services;

/// <summary>
/// Provider to allow MEF access to <see cref="ExtensionAssemblyManager"/>
/// Must be done this way as the manager is required to create MEF as well.
/// </summary>
[Export, Shared]
internal class ExtensionAssemblyManagerMefProvider
{
    private ExtensionAssemblyManager? _extensionAssemblyManager;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ExtensionAssemblyManagerMefProvider()
    {
    }

    [Export]
    public ExtensionAssemblyManager ExtensionAssemblyManager => _extensionAssemblyManager ?? throw new InvalidOperationException($"{nameof(ExtensionAssemblyManager)} is not initialized");

    public void SetMefExtensionAssemblyManager(ExtensionAssemblyManager extensionAssemblyManager)
    {
        _extensionAssemblyManager = extensionAssemblyManager;
    }
}
