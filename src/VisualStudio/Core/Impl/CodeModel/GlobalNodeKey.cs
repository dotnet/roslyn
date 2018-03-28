// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal struct GlobalNodeKey
    {
        public readonly SyntaxNodeKey NodeKey;
        public readonly SyntaxPath Path;

        public GlobalNodeKey(SyntaxNodeKey nodeKey, SyntaxPath path)
        {
            this.NodeKey = nodeKey;
            this.Path = path;
        }
    }
}
