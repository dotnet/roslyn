// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer;

[Export(typeof(LspWorkspaceRegistrationService)), Shared]
internal sealed class VisualStudioLspWorkspaceRegistrationService : LspWorkspaceRegistrationService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioLspWorkspaceRegistrationService()
    {
    }
}
