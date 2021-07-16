// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;

// for backward compat with TypeScript (https://github.com/dotnet/roslyn/issues/43313)
[assembly: TypeForwardedTo(typeof(DocumentDiagnosticAnalyzer))]
