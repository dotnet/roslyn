// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Interactive
{
    public sealed class InteractiveHostObject
    {
        public SearchPaths ReferencePaths { get; }
        public SearchPaths SourcePaths { get; }

        internal InteractiveHostObject()
        {
            ReferencePaths = new SearchPaths();
            SourcePaths = new SearchPaths();
        }
    }
}
