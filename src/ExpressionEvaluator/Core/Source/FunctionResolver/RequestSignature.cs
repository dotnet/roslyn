// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
