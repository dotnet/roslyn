// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public sealed class CSharpIdentifiersShouldBeSpelledCorrectlyFixer() : IdentifiersShouldBeSpelledCorrectlyFixer
    {
    }
}
