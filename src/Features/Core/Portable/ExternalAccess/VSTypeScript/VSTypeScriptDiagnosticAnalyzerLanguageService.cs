// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
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

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptDiagnosticAnalyzerLanguageService(IVSTypeScriptDiagnosticAnalyzerImplementation implementation)
        {
            Implementation = implementation;
        }
    }
}
