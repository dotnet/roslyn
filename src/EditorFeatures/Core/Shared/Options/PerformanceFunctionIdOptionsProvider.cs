// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    [ExportOptionProvider, Shared]
    internal class PerformanceFunctionIdOptionsProvider : IOptionProvider
    {
        public IEnumerable<IOption> GetOptions()
        {
            foreach (var id in (FunctionId[])Enum.GetValues(typeof(FunctionId)))
            {
                yield return FunctionIdOptions.GetOption(id);
            }
        }
    }
}
