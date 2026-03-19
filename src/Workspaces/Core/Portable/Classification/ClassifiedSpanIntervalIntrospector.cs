// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification;

internal static partial class ClassifierHelper
{
    private readonly struct ClassifiedSpanIntervalIntrospector : IIntervalIntrospector<ClassifiedSpan>
    {
        public static readonly ClassifiedSpanIntervalIntrospector Instance = new();

        public TextSpan GetSpan(ClassifiedSpan value)
            => value.TextSpan;
    }
}
