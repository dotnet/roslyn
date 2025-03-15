// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    /// <summary>
    /// RS0006: Do not mix attributes from different versions of MEF
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    [method: ImportingConstructor]
    [method: Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public class CSharpDoNotMixAttributesFromDifferentVersionsOfMEFFixer() : DoNotMixAttributesFromDifferentVersionsOfMEFFixer
    {
    }
}
