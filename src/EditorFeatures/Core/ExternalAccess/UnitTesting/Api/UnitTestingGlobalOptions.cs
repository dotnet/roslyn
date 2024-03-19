// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

[Export(typeof(UnitTestingGlobalOptions)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class UnitTestingGlobalOptions(IGlobalOptionService globalOptions)
{
    private readonly IGlobalOptionService _globalOptions = globalOptions;

    public bool IsServiceHubProcessCoreClr
        => _globalOptions.GetOption(RemoteHostOptionsStorage.OOPCoreClr);
}
