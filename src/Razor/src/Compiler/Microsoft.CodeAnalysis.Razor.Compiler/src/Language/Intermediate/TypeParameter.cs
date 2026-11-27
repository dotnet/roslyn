// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class TypeParameter
{
    public IntermediateToken Name { get; }
    public IntermediateToken? Constraints { get; }

    public TypeParameter(string name)
        : this(name, nameSource: null, constraints: null, constraintsSource: null)
    {
    }

    public TypeParameter(string name, string constraints)
        : this(name, nameSource: null, constraints, constraintsSource: null)
    {
    }

    public TypeParameter(string name, SourceSpan? nameSource, string? constraints, SourceSpan? constraintsSource)
    {
        Name = IntermediateNodeFactory.CSharpToken(name, nameSource);

        if (constraints is not null)
        {
            Constraints = IntermediateNodeFactory.CSharpToken(constraints, constraintsSource);
        }
    }
}
