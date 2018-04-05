// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    internal class RenameTrackingTag : TextMarkerTag
    {
        internal const string TagId = "RenameTrackingTag";

        public static readonly RenameTrackingTag Instance = new RenameTrackingTag();

        private RenameTrackingTag()
            : base(TagId)
        {
        }
    }
}
