// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        internal readonly IVSTypeScriptDiagnosticAnalyzerImplementation? Implementation;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptDiagnosticAnalyzerLanguageService(
            [Import(AllowDefault = true)] IVSTypeScriptDiagnosticAnalyzerImplementation? implementation = null)
        {
            Implementation = implementation;
        }
    }
}
