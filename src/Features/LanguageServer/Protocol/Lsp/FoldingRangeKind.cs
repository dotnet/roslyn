// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Straight copy until new Microsoft.VisualStudio.LanguageServer.Protocol nuget is published with this type in
namespace Microsoft.VisualStudio.LanguageServer.Protocol
{
    internal static class FoldingRangeKind
    {
        public const string Comment = nameof(Comment);
        public const string Imports = nameof(Imports);
        public const string Region = nameof(Region);
    }
}
