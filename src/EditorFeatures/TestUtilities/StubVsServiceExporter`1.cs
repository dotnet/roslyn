// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.UnitTests;

[Export(typeof(IVsService<>))]
[PartCreationPolicy(CreationPolicy.NonShared)]
[PartNotDiscoverable]
internal class StubVsServiceExporter<T> : StubVsServiceExporter<T, T>
    where T : class
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public StubVsServiceExporter(
        [Import(typeof(SAsyncServiceProvider))] IAsyncServiceProvider2 asyncServiceProvider,
        JoinableTaskContext joinableTaskContext)
        : base(asyncServiceProvider, joinableTaskContext)
    {
    }
}
