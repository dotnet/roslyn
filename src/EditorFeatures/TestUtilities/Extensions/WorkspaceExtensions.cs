// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public static class WorkspaceExtensions
    {
        public static void ApplyOptions(this Workspace workspace, IDictionary<OptionKey, object> options)
        {
            if (options != null)
            {
                var optionSet = workspace.Options;
                foreach (var option in options)
                {
                    optionSet = optionSet.WithChangedOption(option.Key, option.Value);
                }

                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(optionSet));
            }
        }
    }
}
