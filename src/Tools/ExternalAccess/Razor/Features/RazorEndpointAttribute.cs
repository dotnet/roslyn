// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;

[MetadataAttribute]
internal class RazorEndpointAttribute : LanguageServerEndpointAttribute
{
    public RazorEndpointAttribute(string method) : base(method, LanguageServerConstants.DefaultLanguageName)
    {
    }

    public RazorEndpointAttribute(string method, string language) : base(method, language)
    {
    }
}
