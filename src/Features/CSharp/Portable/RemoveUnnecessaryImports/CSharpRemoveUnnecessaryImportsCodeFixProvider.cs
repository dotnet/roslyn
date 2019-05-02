// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryImports
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryImports), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddMissingReference)]
    internal class CSharpRemoveUnnecessaryImportsCodeFixProvider : AbstractRemoveUnnecessaryImportsCodeFixProvider
    {
#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public CSharpRemoveUnnecessaryImportsCodeFixProvider()
        {
        }

        protected override string GetTitle()
            => CSharpFeaturesResources.Remove_Unnecessary_Usings;
    }
}
