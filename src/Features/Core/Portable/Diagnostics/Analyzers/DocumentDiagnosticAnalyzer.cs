using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;

// for backward compat with TypeScript (https://github.com/dotnet/roslyn/issues/43313)
[assembly: TypeForwardedTo(typeof(DocumentDiagnosticAnalyzer))]
