// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public static class WorkspaceExtensions
    {
        public static void ApplyOptions(this Workspace workspace, IReadOnlyCollection<KeyValuePair<OptionKey, object>>? options)
            => workspace.ApplyOptions(options?.Select(kvp => (kvp.Key, kvp.Value)));

        internal static void ApplyOptions(this Workspace workspace, IReadOnlyCollection<KeyValuePair<OptionKey2, object>>? options)
            => workspace.ApplyOptions(options?.Select(kvp => ((OptionKey)kvp.Key, kvp.Value)));

        private static void ApplyOptions(this Workspace workspace, IEnumerable<(OptionKey key, object value)>? options)
        {
            if (options != null)
            {
                var optionSet = workspace.Options;
                foreach (var option in options)
                {
                    optionSet = optionSet.WithChangedOption(option.key, option.value);
                }

                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(optionSet));
            }
        }
    }
}
