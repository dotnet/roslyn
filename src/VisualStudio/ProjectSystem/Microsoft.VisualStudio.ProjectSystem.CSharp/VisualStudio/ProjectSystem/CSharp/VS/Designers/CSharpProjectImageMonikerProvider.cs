// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Designers.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.CSharp.VS.Designers
{
    /// <summary>
    ///     Provides C# project images.
    /// </summary>
    [Export(typeof(IProjectImageMonikerProvider))]
    [AppliesTo(ProjectCapability.CSharp)]
    internal class CSharpProjectImageMonikerProvider : IProjectImageMonikerProvider
    {
        [ImportingConstructor]
        public CSharpProjectImageMonikerProvider()
        {
        }

        public bool TryGetProjectImageMoniker(string key, out ProjectImageMoniker result)
        {
            switch (key)
            {
                case ProjectImageMonikerKey.ProjectRoot:
                    result = KnownMonikers.CSProjectNode.ToProjectSystemType();
                    return true;

                case ProjectImageMonikerKey.AppDesignerFolder:
                    result = KnownMonikers.Property.ToProjectSystemType();
                    return true;

                default:
                    result = default(ProjectImageMoniker);
                    return false;
            }
        }
    }
}
