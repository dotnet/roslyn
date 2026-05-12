// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class ClassifiedSpanSerializer
{
    internal static string Serialize(RazorSyntaxTree syntaxTree, bool validateSpanEditHandlers)
    {
        using (var writer = new StringWriter())
        {
            var visitor = new ClassifiedSpanWriter(writer, syntaxTree, validateSpanEditHandlers);
            visitor.Visit();

            return writer.ToString();
        }
    }
}
