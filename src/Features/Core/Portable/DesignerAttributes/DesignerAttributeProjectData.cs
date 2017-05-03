// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttributes;

namespace Microsoft.CodeAnalysis.DesignerAttributes
{
    internal class DesignerAttributeProjectData
    {
        public readonly VersionStamp SemanticVersion;
        public readonly ImmutableDictionary<string, DesignerAttributeDocumentData> PathToDocumentData;

        public DesignerAttributeProjectData(
            VersionStamp semanticVersion, ImmutableDictionary<string, DesignerAttributeDocumentData> pathToDocumentData)
        {
            SemanticVersion = semanticVersion;
            PathToDocumentData = pathToDocumentData;
        }
    }
}