// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.NamingStyles;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.FullyQualify
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ApplyNamingStyle), Shared]
    internal class CSharpNamingStyleCodeFixProvider : AbstractNamingStyleCodeFixProvider
    {
    }
}
