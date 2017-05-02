// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttributes;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    internal class DesignerAttributeData
    {
        public readonly VersionStamp SemanticVersion;
        public readonly ImmutableDictionary<string, DesignerAttributeResult> PathToResult;

        public DesignerAttributeData(VersionStamp semanticVersion, ImmutableDictionary<string, DesignerAttributeResult> pathToResult)
        {
            SemanticVersion = semanticVersion;
            PathToResult = pathToResult;
        }
    }
}