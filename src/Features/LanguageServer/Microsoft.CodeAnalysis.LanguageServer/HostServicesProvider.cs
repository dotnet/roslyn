// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// A simple type to provide a single copy of <see cref="Microsoft.CodeAnalysis.Host.HostServices"/> for the MEF composition.
/// </summary>
[Export(typeof(HostServicesProvider)), Shared]
internal class HostServicesProvider
{
    public HostServices HostServices { get; }

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public HostServicesProvider(ExportProvider exportProvider)
    {
        HostServices = VisualStudioMefHostServices.Create(exportProvider);
    }
}
