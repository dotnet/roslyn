// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    internal class WarningTag : TextMarkerTag
    {
        public const string TagId = "RoslynWarningTag";

        public static readonly WarningTag Instance = new WarningTag();

        private WarningTag()
            : base(TagId)
        {
        }
    }
}
