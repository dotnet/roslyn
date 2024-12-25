// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.Interactive;
using LanguageServiceGuids = Microsoft.VisualStudio.LanguageServices.Guids;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive;

[Guid(LanguageServiceGuids.CSharpReplPackageIdString)]
internal sealed partial class CSharpVsInteractiveWindowPackage : VsInteractiveWindowPackage<CSharpVsInteractiveWindowProvider>
{
    private const string IdString = "CA8CC5C7-0231-406A-95CD-AA5ED6AC0190";
    internal static readonly Guid Id = new(IdString);

    protected override Guid ToolWindowId => Id;
    protected override Guid LanguageServiceGuid => LanguageServiceGuids.CSharpLanguageServiceId;
}
