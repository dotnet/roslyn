// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Templating
{
    /// <summary>
    /// Implementation of the item type provider for CSharp project system.
    /// </summary>
    // [Export(typeof(IItemTypeGuidProvider))]
    [AppliesTo(ProjectCapabilities.CSharp)]
    internal class CSharpItemTypeGuidProvider : IItemTypeGuidProvider
    {
        private static readonly Guid CSharpProjectType = new Guid("{FAE04EC0-301F-11d3-BF4B-00C04F79EFBC}");

        [ImportingConstructor]
        public CSharpItemTypeGuidProvider()
        {
        }

        [Import]
        private UnconfiguredProject UnconfiguredProject
        {
            get;
            set;
        }

        /// <summary>
        /// Returns the item type Guid.
        /// </summary>
        public Guid ProjectTypeGuid
        {
            get { return CSharpProjectType; }
        }
    }
}
