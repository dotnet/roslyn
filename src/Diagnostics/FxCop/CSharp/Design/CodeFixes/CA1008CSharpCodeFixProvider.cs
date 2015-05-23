// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.AnalyzerPowerPack.Design;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.AnalyzerPowerPack.CSharp.Design
{
    /// <summary>
    /// CA1008: Enums should have zero value
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "CA1008"), Shared]
    public class CA1008CSharpCodeFixProvider : CA1008CodeFixProviderBase
    {
    }
}
