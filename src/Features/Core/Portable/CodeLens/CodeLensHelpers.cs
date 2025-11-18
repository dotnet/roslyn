// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CodeLens;

internal static class CodeLensHelpers
{
    public static DocumentId? GetSourceGeneratorDocumentId(IDictionary<object, object> descriptorProperties)
    {
        // Undocumented contract here:'
        // https://devdiv.visualstudio.com/DevDiv/_git/VS?path=/src/CodeSense/Framework/Roslyn/Roslyn/Editor/CodeElementTag.cs&version=GBmain&_a=contents&line=84&lineStyle=plain&lineEnd=85&lineStartColumn=1&lineEndColumn=96
        if (TryGetGuid("RoslynDocumentIdGuid", out var documentIdGuid) &&
            TryGetGuid("RoslynProjectIdGuid", out var projectIdGuid))
        {
            var isSourceGenerated = false;

            if (descriptorProperties.TryGetValue("RoslynDocumentIsSourceGenerated", out var isSourceGeneratedObj))
            {
                if (isSourceGeneratedObj is bool isSourceGeneratedBoolean)
                    isSourceGenerated = isSourceGeneratedBoolean;
            }

            var projectId = ProjectId.CreateFromSerialized(projectIdGuid);
            return DocumentId.CreateFromSerialized(projectId, documentIdGuid, isSourceGenerated, debugName: null);
        }

        return null;

        bool TryGetGuid(string key, out Guid guid)
        {
            guid = Guid.Empty;
            return descriptorProperties.TryGetValue(key, out var guidStringUntyped) &&
                guidStringUntyped is string guidString &&
                Guid.TryParse(guidString, out guid);
        }
    }
}
