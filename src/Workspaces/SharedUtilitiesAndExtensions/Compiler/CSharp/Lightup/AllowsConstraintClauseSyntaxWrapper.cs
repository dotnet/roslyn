// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Syntax;

internal readonly struct AllowsConstraintClauseSyntaxWrapper
{
    public static implicit operator TypeParameterConstraintSyntax(AllowsConstraintClauseSyntaxWrapper wrapper)
        => throw new NotImplementedException();
}
