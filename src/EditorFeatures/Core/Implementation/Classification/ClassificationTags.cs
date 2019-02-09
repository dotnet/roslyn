// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Classification
{
    internal static class ClassificationTags
    {
        [Obsolete("Use the ToClassificationTypeName extension method from TaggedTextExtensions.")]
        public static string GetClassificationTypeName(string textTag)
        {
            return textTag.ToClassificationTypeName();
        }
    }
}
