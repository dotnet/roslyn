// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class RequestSignature
    {
        internal RequestSignature(Name memberName, ImmutableArray<ParameterSignature> parameters)
        {
            MemberName = memberName;
            Parameters = parameters;
        }

        internal readonly Name MemberName;
        internal readonly ImmutableArray<ParameterSignature> Parameters; // default if not specified
    }
}
