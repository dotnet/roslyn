// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommonLanguageServerProtocol.Framework;

public class ExampleRequestContext
{
    public ILspServices LspServices;
    public ILspLogger Logger;

    public ExampleRequestContext(ILspServices lspServices, ILspLogger logger)
    {
        LspServices = lspServices;
        Logger = logger;
    }
}
