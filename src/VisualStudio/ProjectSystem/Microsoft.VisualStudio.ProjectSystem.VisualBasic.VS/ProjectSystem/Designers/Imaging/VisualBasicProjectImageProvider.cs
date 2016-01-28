// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Imaging
{
    /// <summary>
    ///     Provides Visual Basic project images.
    /// </summary>
    [Export(typeof(IProjectImageProvider))]
    [AppliesTo(ProjectCapability.VisualBasic)]
    internal class VisualBasicProjectImageProvider : IProjectImageProvider
    {
        [ImportingConstructor]
        public VisualBasicProjectImageProvider()
        {
        }

        public ProjectImageMoniker GetProjectImage(string key)
        {
            Requires.NotNullOrEmpty(key, nameof(key));

            switch (key)
            {
                case ProjectImageKey.ProjectRoot:
                    return KnownMonikers.VBProjectNode.ToProjectSystemType();

                case ProjectImageKey.AppDesignerFolder:
                    return KnownMonikers.Property.ToProjectSystemType();

                default:
                    return null;
            }
        }
    }
}
