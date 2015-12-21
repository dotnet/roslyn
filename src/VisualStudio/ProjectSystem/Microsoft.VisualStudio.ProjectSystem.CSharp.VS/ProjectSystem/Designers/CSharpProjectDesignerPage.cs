// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    /// <summary>
    ///     Provides common well-known C# project property pages.
    /// </summary>
    internal static class CSharpProjectDesignerPage
    {
        public static readonly ProjectDesignerPageMetadata Application = new ProjectDesignerPageMetadata(new Guid("{5E9A8AC2-4F34-4521-858F-4C248BA31532}"), pageOrder:0, hasConfigurationCondition:false);
    }
}
