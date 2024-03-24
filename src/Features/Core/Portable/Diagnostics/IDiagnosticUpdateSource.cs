// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Implement this to participate in diagnostic service framework as one of diagnostic update source
/// </summary>
internal interface IDiagnosticUpdateSource
{
}
