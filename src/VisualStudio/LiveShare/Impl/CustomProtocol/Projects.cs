// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol
{
    /// <summary>
    /// TODO - This custom liveshare model should live elsewhere.
    /// </summary>
    internal class Project
    {
        /// <summary>
        /// Name of the project.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The project language.
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Paths of the files in the project.
        /// </summary>
        public Uri[] SourceFiles { get; set; }
    }
}
