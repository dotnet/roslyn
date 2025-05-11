// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel.Extenders;

[ComVisible(true)]
[ComDefaultInterface(typeof(ICSCodeTypeLocation))]
public class CodeTypeLocationExtender : ICSCodeTypeLocation
{
    internal static ICSCodeTypeLocation Create(string externalLocation)
    {
        var result = new CodeTypeLocationExtender(externalLocation);
        return (ICSCodeTypeLocation)ComAggregate.CreateAggregatedObject(result);
    }

    private CodeTypeLocationExtender(string externalLocation)
        => ExternalLocation = externalLocation;

    public string ExternalLocation { get; }
}
