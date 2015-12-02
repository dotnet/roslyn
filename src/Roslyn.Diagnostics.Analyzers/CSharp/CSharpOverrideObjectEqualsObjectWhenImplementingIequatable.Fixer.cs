// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
                                 
using System.Composition;
using System.Diagnostics;  
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslyn.Diagnostics.Analyzers
{                                 
    /// <summary>
    /// RS0009: Override Object.Equals(object) when implementing Iequatable
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class CSharpOverrideObjectEqualsObjectWhenImplementingIequatableFixer : OverrideObjectEqualsObjectWhenImplementingIequatableFixer
    { 

    }
}