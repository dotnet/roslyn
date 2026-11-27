// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class BaseTypeWithModel
{
    private const string ModelGenericParameter = "<TModel>";

    public IntermediateToken BaseType { get; }
    public IntermediateToken? GreaterThan { get; }
    public IntermediateToken? ModelType { get; set; }
    public IntermediateToken? LessThan { get; }

    public BaseTypeWithModel(string baseType, SourceSpan? location = null)
    {
        // If the base type ends with the standard "<TModel>" type parameter list, break it into separate tokens.
        if (baseType.EndsWith(ModelGenericParameter, StringComparison.Ordinal))
        {
            BaseType = IntermediateNodeFactory.CSharpToken(baseType[0..^ModelGenericParameter.Length]);
            GreaterThan = IntermediateNodeFactory.CSharpToken("<");
            ModelType = IntermediateNodeFactory.CSharpToken("TModel");
            LessThan = IntermediateNodeFactory.CSharpToken(">");

            if (location is SourceSpan span)
            {
                var greaterThanIndex = baseType.Length - ModelGenericParameter.Length;
                BaseType.Source = span[..greaterThanIndex];
                GreaterThan.Source = span[greaterThanIndex..(greaterThanIndex + 1)];
                ModelType.Source = span[(greaterThanIndex + 1)..^1];
                LessThan.Source = span[^1..];
            }
        }
        else
        {
            BaseType = IntermediateNodeFactory.CSharpToken(baseType, location);
        }
    }
}
