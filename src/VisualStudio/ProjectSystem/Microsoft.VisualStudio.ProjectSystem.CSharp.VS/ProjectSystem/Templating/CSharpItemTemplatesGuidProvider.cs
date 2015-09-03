// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Templating
{
    /// <summary>
    /// Implementation of the item templates Guid provider for CSharp project system.
    /// </summary>
    // [Export(typeof(IAddItemTemplatesGuidProvider))]
    [AppliesTo(ProjectCapabilities.CSharp)]
    internal class CSharpItemTemplatesGuidProvider : IAddItemTemplatesGuidProvider
    {
        private static readonly Guid CSharpProjectType = new Guid("{FAE04EC0-301F-11d3-BF4B-00C04F79EFBC}");

        [ImportingConstructor]
        public CSharpItemTemplatesGuidProvider()
        {
        }

        [Import]
        private UnconfiguredProject UnconfiguredProject
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the item templates Guid.
        /// </summary>
        public Guid AddItemTemplatesGuid
        {
            get { return CSharpProjectType; }
        }
    }
}
