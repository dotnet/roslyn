// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

[Export(typeof(UnitTestingGlobalOptions)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class UnitTestingGlobalOptions()
{
#pragma warning disable CA1822 // Mark members as static
    public bool IsServiceHubProcessCoreClr => true;
#pragma warning restore CA1822 // Mark members as static
}
