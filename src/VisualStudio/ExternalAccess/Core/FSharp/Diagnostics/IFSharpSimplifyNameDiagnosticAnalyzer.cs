// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Diagnostics;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics;
#endif

internal interface IFSharpSimplifyNameDiagnosticAnalyzer
{
    Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(DiagnosticDescriptor descriptor, Document document, CancellationToken cancellationToken);
}
