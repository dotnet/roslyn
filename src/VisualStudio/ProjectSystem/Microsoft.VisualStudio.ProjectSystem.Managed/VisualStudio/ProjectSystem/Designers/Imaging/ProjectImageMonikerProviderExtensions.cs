// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Imaging
{
    internal static class ProjectImageMonikerProviderExtensions
    {
        public static bool TryGetProjectImageMoniker(this IEnumerable<IProjectImageMonikerProvider> providers, string key, out ProjectImageMoniker result)
        {
            Requires.NotNull(providers, nameof(providers));             

            foreach (IProjectImageMonikerProvider provider in providers)
            {
                if (provider.TryGetProjectImageMoniker(key, out result))
                {
                    return true;
                }
            }

            result = default(ProjectImageMoniker);
            return false;
        }
    }
}
