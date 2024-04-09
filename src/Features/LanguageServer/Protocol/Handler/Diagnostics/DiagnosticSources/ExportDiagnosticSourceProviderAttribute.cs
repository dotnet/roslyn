// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;

/// <summary>
/// Use this attribute to declare a <see cref="IDiagnosticSource"/> implementation for inclusion in a MEF-based workspace.
/// </summary>
/// <remarks>
/// Declares a <see cref="IDiagnosticSource"/> implementation for inclusion in a MEF-based workspace.
/// </remarks>
[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class)]
internal class ExportDiagnosticSourceProviderAttribute() : ExportAttribute(typeof(IDiagnosticSourceProvider))
{
}
