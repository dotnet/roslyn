// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings
{
    [ExportLanguageService(typeof(ICodeRefactoringService), LanguageNames.CSharp)]
    internal class CSharpCodeRefactoringService : AbstractCodeRefactoringService
    {
        private readonly IEnumerable<Lazy<CodeRefactoringProvider, OrderableLanguageMetadata>> lazyCodeRefactoringProviders;

        private IEnumerable<CodeRefactoringProvider> defaultCodeRefactoringProviders;

        [ImportingConstructor]
        public CSharpCodeRefactoringService(
            [ImportMany] IEnumerable<Lazy<CodeRefactoringProvider, OrderableLanguageMetadata>> codeRefactoringProviders)
        {
            this.lazyCodeRefactoringProviders = ExtensionOrderer.Order(codeRefactoringProviders.Where(p => p.Metadata.Language == LanguageNames.CSharp)).ToImmutableList();
        }

        public override IEnumerable<CodeRefactoringProvider> GetDefaultCodeRefactoringProviders()
        {
            if (this.defaultCodeRefactoringProviders == null)
            {
                System.Threading.Interlocked.CompareExchange(ref this.defaultCodeRefactoringProviders, this.lazyCodeRefactoringProviders.Select(lz => lz.Value).ToImmutableList(), null);
            }

            return defaultCodeRefactoringProviders;
        }
    }
}
