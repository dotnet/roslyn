// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.AddMissingReference;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.CSharp.AddMissingReference
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddMissingReference), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.SimplifyNames)]
    internal class CSharpAddMissingReferenceCodeFixProvider : AbstractAddMissingReferenceCodeFixProvider
    {
        private const string CS0012 = nameof(CS0012); // The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'ProjectA, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(CS0012);

        [ImportingConstructor]
        public CSharpAddMissingReferenceCodeFixProvider()
        {
        }

        /// <summary>For testing purposes only (so that tests can pass in mock values)</summary> 
        internal CSharpAddMissingReferenceCodeFixProvider(
            IPackageInstallerService installerService,
            ISymbolSearchService symbolSearchService)
            : base(installerService, symbolSearchService)
        {
        }
    }
}
