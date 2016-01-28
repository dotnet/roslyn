// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides the C# implementation of <see cref="IItemTypeGuidProvider"/> and <see cref="IAddItemTemplatesGuidProvider"/>.
    /// </summary>
    //[Export(typeof(IItemTypeGuidProvider))]
    //[Export(typeof(IAddItemTemplatesGuidProvider))]
    [AppliesTo(ProjectCapabilities.CSharp)]
    internal class CSharpProjectGuidProvider : IItemTypeGuidProvider, IAddItemTemplatesGuidProvider
    {
        private static readonly Guid s_csharpProjectType = new Guid("{FAE04EC0-301F-11d3-BF4B-00C04F79EFBC}");

        [ImportingConstructor]
        public CSharpProjectGuidProvider(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));
        }

        public Guid ProjectTypeGuid
        {
            get { return s_csharpProjectType; }
        }

        public Guid AddItemTemplatesGuid
        {
            get { return s_csharpProjectType; }
        }
    }
}
