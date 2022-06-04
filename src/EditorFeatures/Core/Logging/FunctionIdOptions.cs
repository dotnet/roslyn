// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal static class FunctionIdOptions
    {
        private static readonly ConcurrentDictionary<FunctionId, Option2<bool>> s_options =
            new();

        private static readonly Func<FunctionId, Option2<bool>> s_optionCreator = CreateOption;

        private static Option2<bool> CreateOption(FunctionId id)
        {
            var name = Enum.GetName(typeof(FunctionId), id) ?? throw ExceptionUtilities.UnexpectedValue(id);

            return new(nameof(FunctionIdOptions), name, defaultValue: false,
                storageLocation: new LocalUserProfileStorageLocation(@"Roslyn\Internal\Performance\FunctionId\" + name));
        }

        public static Option2<bool> GetOption(FunctionId id)
            => s_options.GetOrAdd(id, s_optionCreator);

        public static Func<FunctionId, bool> CreateFunctionIsEnabledPredicate(IGlobalOptionService globalOptions)
        {
            var functionIds = Enum.GetValues(typeof(FunctionId)).Cast<FunctionId>();
            var functionIdOptions = functionIds.ToDictionary(id => id, id => globalOptions.GetOption(GetOption(id)));
            return functionId => functionIdOptions[functionId];
        }
    }
}
