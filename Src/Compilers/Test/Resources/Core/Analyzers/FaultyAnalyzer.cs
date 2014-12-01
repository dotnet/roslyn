// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Build FaultyAnalyzer.dll with: csc.exe /t:library FaultyAnalyzer.cs /r:Microsoft.CodeAnalysis.dll /r:System.Runtime.dll

using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer]
public abstract class TestAnalyzer : DiagnosticAnalyzer
{
}
