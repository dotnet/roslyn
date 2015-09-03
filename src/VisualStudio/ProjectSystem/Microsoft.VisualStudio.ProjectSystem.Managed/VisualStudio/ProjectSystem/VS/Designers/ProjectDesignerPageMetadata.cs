// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Designers
{
    /// <summary>
    ///     Concrete implementation of <see cref="IPageMetadata"/>.
    /// </summary>
    internal class ProjectDesignerPageMetadata : IPageMetadata
    {
        public ProjectDesignerPageMetadata(Guid pageGuid, int pageOrder, bool hasConfigurationCondition)
        {
            PageGuid = pageGuid;
            PageOrder = pageOrder;
            HasConfigurationCondition = hasConfigurationCondition;
        }

        public string Name
        {
            get;
        }

        public bool HasConfigurationCondition
        {
            get;
        }        

        public Guid PageGuid
        {
            get;
        }

        public int PageOrder
        {
            get;
        }
    }
}
