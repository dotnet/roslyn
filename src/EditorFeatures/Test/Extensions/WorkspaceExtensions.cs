// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                var optionService = workspace.Services.GetService<IOptionService>();
                var optionSet = optionService.GetOptions();
                foreach (var option in options)
                {
                    optionSet = optionSet.WithChangedOption(option.Key, option.Value);
                }

                optionService.SetOptions(optionSet);
            }
        }
    }
}
