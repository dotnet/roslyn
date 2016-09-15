// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal static class FunctionIdOptions
    {
        private static readonly ConcurrentDictionary<FunctionId, Option<bool>> s_options =
            new ConcurrentDictionary<FunctionId, Option<bool>>();

        private static readonly Func<FunctionId, Option<bool>> s_optionCreator = CreateOption;

        private static Option<bool> CreateOption(FunctionId id)
        {
            var name = Enum.GetName(typeof(FunctionId), id);

            return new Option<bool>(nameof(FunctionIdOptions), name, defaultValue: false,
                storageLocations: new LocalUserProfileStorageLocation(@"Roslyn\Internal\Performance\FunctionId\" + name));
        }

        public static Option<bool> GetOption(FunctionId id)
        {
            return s_options.GetOrAdd(id, s_optionCreator);
        }
    }
}
