// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal class ConflictTag : TextMarkerTag
    {
        public const string TagId = "RoslynConflictTag";

        public static readonly ConflictTag Instance = new ConflictTag();

        private ConflictTag()
            : base(TagId)
        {
        }
    }
}
