// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

[MetadataAttribute]
internal class RazorMethodAttribute : LanguageServerEndpointAttribute
{
    public RazorMethodAttribute(string method) : base(method, LanguageServerConstants.DefaultLanguageName)
    {
    }

    public RazorMethodAttribute(string method, string language) : base(method, language)
    {
    }
}
