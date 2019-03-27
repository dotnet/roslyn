// Copyright (c) Microsoft. All rights reserved.

// Straight copy until new Microsoft.VisualStudio.LanguageServer.Protocol nuget is published with this type in
namespace Microsoft.VisualStudio.LanguageServer.Protocol
{
    public static class FoldingRangeKind
    {
        public static readonly string Comment = nameof(Comment);
        public static readonly string Imports = nameof(Imports);
        public static readonly string Region = nameof(Region);
    }
}
