// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Lightup
{
    internal static class SemanticModelExtensions
    {
        private static readonly Func<SemanticModel, int, NullableContext> s_getNullableContext
            = LightupHelpers.CreateAccessorWithArgument<SemanticModel, int, NullableContext>(typeof(SemanticModel), "semanticModel", typeof(int), "position", nameof(GetNullableContext));

        public static NullableContext GetNullableContext(this SemanticModel semanticModel, int position)
            => s_getNullableContext(semanticModel, position);
    }
}
