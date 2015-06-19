// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Runtime.Analyzers
{
    /// <summary>
    /// CA1019: Define accessors for attribute arguments
    /// 
    /// Cause:
    /// In its constructor, an attribute defines arguments that do not have corresponding properties.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpDefineAccessorsForAttributeArgumentsAnalyzer : DefineAccessorsForAttributeArgumentsAnalyzer
    {
        protected override bool IsAssignableTo(ITypeSymbol fromSymbol, ITypeSymbol toSymbol, Compilation compilation)
        {
            return fromSymbol != null &&
                toSymbol != null &&
                ((CSharpCompilation)compilation).ClassifyConversion(fromSymbol, toSymbol).IsImplicit;
        }
    }
}
