// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.PrivateAnalyzers;

internal static class PrivateDiagnosticIds
{
    /// <summary>
    /// Target-typed new is not allowed by code style policy in the compiler layer.
    /// </summary>
    public const string AvoidImplicitObjectCreation = "RP0001";

    /// <summary>
    /// Prefer <c>argument: value</c> syntax to <c>value /*argument*/</c>.
    /// </summary>
    public const string UseNamedArguments = "RP0002";
}
