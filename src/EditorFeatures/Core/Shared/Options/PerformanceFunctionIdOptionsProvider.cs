﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.Shared.Options
{
    [ExportOptionProvider, Shared]
    internal class PerformanceFunctionIdOptionsProvider : IOptionProvider
    {
        public ImmutableArray<IOption> Options
        {
            get
            {
                var result = ArrayBuilder<IOption>.GetInstance();
                foreach (var id in (FunctionId[])Enum.GetValues(typeof(FunctionId)))
                {
                    result.Add(FunctionIdOptions.GetOption(id));
                }

                return result.ToImmutableAndFree();
            }
        }
    }
}
