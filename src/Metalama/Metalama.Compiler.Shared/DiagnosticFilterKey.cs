// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#pragma warning disable 8618
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Metalama.Compiler;

internal sealed record DiagnosticFilterKey(string FilePath, string SuppressedDiagnosticId)
{
    public bool Equals(DiagnosticFilterKey? other)
    {
        return other != null &&
               string.Equals(FilePath, other.FilePath, StringComparison.Ordinal) &&
               string.Equals(SuppressedDiagnosticId, other.SuppressedDiagnosticId, StringComparison.Ordinal);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (17 + FilePath.GetHashCode()) * 31 +
                    SuppressedDiagnosticId.GetHashCode();
        }
    }
}
