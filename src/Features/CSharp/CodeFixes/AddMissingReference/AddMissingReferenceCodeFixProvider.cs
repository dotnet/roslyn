// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.AddMissingReference;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddMissingReference
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddMissingReference), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.SimplifyNames)]
    internal class AddMissingReferenceCodeFixProvider : AbstractAddMissingReferenceCodeFixProvider<IdentifierNameSyntax>
    {
        private const string CS0012 = "CS0012"; // The type 'A' is defined in an assembly that is not referenced. You must add a reference to assembly 'ProjectA, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS0012); }
        }
    }
}
