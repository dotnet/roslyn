// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine;

internal sealed class RenameInvalidIdentifierAnnotation : RenameAnnotation
{
    public static RenameInvalidIdentifierAnnotation Instance = new();

    private RenameInvalidIdentifierAnnotation()
    {
    }
}
