// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class PublicNullableAnnotationExtensions
    {
        public static NullableAnnotation ToInternalAnnotation(this CodeAnalysis.NullableAnnotation annotation)
        {
            Debug.Assert((CodeAnalysis.NullableAnnotation)(NullableAnnotation.Unknown + 1) == CodeAnalysis.NullableAnnotation.Unknown);
            return annotation == CodeAnalysis.NullableAnnotation.Default ? NullableAnnotation.Unknown : (NullableAnnotation)annotation - 1;
        }
    }
}
