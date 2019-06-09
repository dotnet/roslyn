// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.AddMissingImports;
using Microsoft.CodeAnalysis.CSharp.AddImport;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.AddMissingImports
{
    [ExportLanguageService(typeof(IAddMissingImportsFeatureService), LanguageNames.CSharp), Shared]
    internal class CSharpAddMissingImportsFeatureService : AbstractAddMissingImportsFeatureService
    {
        [ImportingConstructor]
        public CSharpAddMissingImportsFeatureService()
        {
        }

        protected sealed override ImmutableArray<string> FixableDiagnosticIds => AddImportDiagnosticIds.FixableDiagnosticIds;
    }
}
