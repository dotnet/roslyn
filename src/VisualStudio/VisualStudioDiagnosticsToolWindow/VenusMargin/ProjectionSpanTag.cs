// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text.Tagging;

namespace Roslyn.Hosting.Diagnostics.VenusMargin
{
    internal class ProjectionSpanTag : TextMarkerTag
    {
        public const string TagId = "ProjectionTag";

        public static readonly ProjectionSpanTag Instance = new ProjectionSpanTag();

        public ProjectionSpanTag()
            : base(TagId)
        {
        }
    }
}
