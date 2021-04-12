// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Build FaultyAnalyzer.dll with: csc.exe /t:library FaultyAnalyzer.cs /r:Microsoft.CodeAnalysis.dll /r:System.Runtime.dll

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public abstract class TestAnalyzer : DiagnosticAnalyzer
{
}
