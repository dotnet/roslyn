// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Features
{
    internal class EditorConfigCompletionItem
    {
        public string Label { get; set; }
        public string InsertText { get; set; }
        public EditorConfigCompletionKind Kind { get; set; }
        public string Documentation { get; set; }
        public string[] CommitCharacters { get; set; }
        public string[] Values { get; set; }
    }
}
