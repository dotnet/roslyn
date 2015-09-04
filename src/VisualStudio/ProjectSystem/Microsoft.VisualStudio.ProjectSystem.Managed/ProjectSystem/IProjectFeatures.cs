// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides methods for retrieving information about the current project's features.
    /// </summary>
    internal interface IProjectFeatures
    {
        /// <summary>
        ///     Returns a value indicating if the current project supports the Project Designer.
        /// </summary>
        bool SupportsProjectDesigner
        {
            get;
        }
    }
}
