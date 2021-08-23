// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Text.Analyzers;

namespace Text.CSharp.Analyzers
{
    /// <summary>
    /// CA1704: Identifiers should be spelled correctly
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class CSharpIdentifiersShouldBeSpelledCorrectlyFixer : IdentifiersShouldBeSpelledCorrectlyFixer
    {
    }
}