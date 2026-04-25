// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using Microsoft.AspNetCore.Razor;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Shared]
[PartNotDiscoverable]
[Export(typeof(SVsServiceProvider))]
internal class TestVsServiceProvider : SVsServiceProvider
{
    public object GetService(Type serviceType)
    {
        return Assumed.Unreachable<object>();
    }
}
