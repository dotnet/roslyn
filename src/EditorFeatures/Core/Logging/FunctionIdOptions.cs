// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Internal.Log;

internal static class FunctionIdOptions
{
    private static readonly ConcurrentDictionary<FunctionId, Option2<bool>> s_options = [];

    private static readonly Func<FunctionId, Option2<bool>> s_optionCreator = CreateOption;

    private static Option2<bool> CreateOption(FunctionId id)
    {
        var name = id.ToString();

        // This local storage location can be set via vsregedit. Which is available on any VS Command Prompt.
        //
        // To enable logging:
        //
        //     vsregedit set local [hive name] HKCU Roslyn\Internal\Performance\FunctionId [function name] dword 1
        //
        // To disable logging
        //
        //     vsregedit delete local [hive name] HKCU Roslyn\Internal\Performance\FunctionId [function name]
        //
        // If you want to set it for the default hive, use "" as the hive name (i.e. an empty argument)
        return new("FunctionIdOptions_" + name, defaultValue: false);
    }

    private static IEnumerable<FunctionId> GetFunctionIds()
        => Enum.GetValues<FunctionId>();

    public static IEnumerable<IOption2> GetOptions()
        => GetFunctionIds().Select(GetOption);

    public static Option2<bool> GetOption(FunctionId id)
        => s_options.GetOrAdd(id, s_optionCreator);

    public static Func<FunctionId, bool> CreateFunctionIsEnabledPredicate(IGlobalOptionService globalOptions)
    {
        var functionIdOptions = GetFunctionIds().ToDictionary(id => id, id => globalOptions.GetOption(GetOption(id)));
        return functionId => functionIdOptions[functionId];
    }
}
