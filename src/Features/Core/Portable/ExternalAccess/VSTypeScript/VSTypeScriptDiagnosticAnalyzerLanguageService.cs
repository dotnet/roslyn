// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [Shared]
    [ExportLanguageService(typeof(VSTypeScriptDiagnosticAnalyzerLanguageService), InternalLanguageNames.TypeScript)]
    internal sealed class VSTypeScriptDiagnosticAnalyzerLanguageService : ILanguageService
    {
        internal readonly IVSTypeScriptDiagnosticAnalyzerImplementation Implementation;

        // 'implementation' is a required import, but MEF 2 does not support silent part rejection when a required
        // import is missing so we combine AllowDefault with a null check in the constructor to defer the exception
        // until the part is instantiated.
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptDiagnosticAnalyzerLanguageService(
            [Import(AllowDefault = true)] IVSTypeScriptDiagnosticAnalyzerImplementation? implementation)
        {
            Implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));
        }
    }
}
