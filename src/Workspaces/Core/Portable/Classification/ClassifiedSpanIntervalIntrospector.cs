// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.Classification
{
    internal static partial class ClassifierHelper
    {
        private readonly struct ClassifiedSpanIntervalIntrospector : IIntervalIntrospector<ClassifiedSpan>
        {
            public static readonly ClassifiedSpanIntervalIntrospector Instance = new ClassifiedSpanIntervalIntrospector();

            public int GetLength(ClassifiedSpan value)
                => value.TextSpan.Length;

            public int GetStart(ClassifiedSpan value)
                => value.TextSpan.Start;
        }
    }
}
