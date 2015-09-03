// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Designers.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.CSharp.VS.Designers.Imaging
{
    /// <summary>
    ///     Provides C# project images.
    /// </summary>
    [Export(typeof(IProjectImageProvider))]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpProjectImageMonikerProvider : IProjectImageProvider
    {
        [ImportingConstructor]
        public CSharpProjectImageMonikerProvider()
        {
        }

        public bool TryGetProjectImage(string key, out ProjectImageMoniker result)
        {
            switch (key)
            {
                case ProjectImageKey.ProjectRoot:
                    result = KnownMonikers.CSProjectNode.ToProjectSystemType();
                    return true;

                case ProjectImageKey.AppDesignerFolder:
                    result = KnownMonikers.Property.ToProjectSystemType();
                    return true;

                default:
                    result = default(ProjectImageMoniker);
                    return false;
            }
        }
    }
}
