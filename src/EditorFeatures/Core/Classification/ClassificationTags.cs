// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Classification;

internal static class ClassificationTags
{
    [Obsolete("Use ToClassificationTypeName")]
    public static string GetClassificationTypeName(string textTag)
        => textTag.ToClassificationTypeName();
}
