// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Experiments;

namespace Microsoft.CodeAnalysis.Editor.Shared.Preview
{
    internal class SymbolSearchPreviewUtility
    {
        internal static bool EditorHandlesSymbolSearch(Workspace workspace)
        {
            if (workspace == null)
            {
                return false;
            }
            var experimentationService = workspace.Services.GetRequiredService<IExperimentationService>();
            return experimentationService.IsExperimentEnabled(WellKnownExperimentNames.EditorHandlesSymbolSearch);
        }
    }
}
