// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Services;

/// <summary>
/// Provider to allow MEF access to <see cref="ExtensionAssemblyManager"/>
/// Must be done this way as the manager is required to create MEF as well.
/// </summary>
[Export, Shared]
internal sealed class ExtensionAssemblyManagerMefProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ExtensionAssemblyManagerMefProvider()
    {
    }

    [Export]
    public ExtensionAssemblyManager ExtensionAssemblyManager { get => field ?? throw new InvalidOperationException($"{nameof(ExtensionAssemblyManager)} is not initialized"); private set; }

    public void SetMefExtensionAssemblyManager(ExtensionAssemblyManager extensionAssemblyManager)
    {
        ExtensionAssemblyManager = extensionAssemblyManager;
    }
}
