// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    /// <summary>
    ///     Provides members for for opening the Project Designer and querying whether it is supported.
    /// </summary>
    internal interface IProjectDesignerService
    {
        /// <summary>
        ///     Gets a value indicating whether the current project supports the Project Designer.
        /// </summary>
        /// <value>
        ///     <see langword="true"/> if the current project supports the Project Designer; otherwise, <see langword="false"/>.
        /// </value>
        bool SupportsProjectDesigner
        {
            get;
        }

        /// <summary>
        ///     Shows the current project's Project Designer window.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     <see cref="SupportsProjectDesigner"/> is <see langword="false"/>.
        /// </exception>
        Task ShowProjectDesignerAsync();
    }
}
