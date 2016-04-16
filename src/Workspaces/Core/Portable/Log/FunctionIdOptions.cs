// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal static class FunctionIdOptions
    {
        private const string FeatureName = "Performance/FunctionId";

        private static readonly ConcurrentDictionary<FunctionId, Option<bool>> s_options =
            new ConcurrentDictionary<FunctionId, Option<bool>>();

        private static readonly Func<FunctionId, Option<bool>> s_optionGetter =
            id => new Option<bool>(FeatureName, Enum.GetName(typeof(FunctionId), id), defaultValue: GetDefaultValue(id));

        public static Option<bool> GetOption(FunctionId id)
        {
            return s_options.GetOrAdd(id, s_optionGetter);
        }

        private static bool GetDefaultValue(FunctionId id)
        {
            switch (id)
            {
                // change not to enable any etw events by default.
                // we used to couple this to other logger such as code marker but now it is only specific to etw.
                // each events should be enabled specifically when needed.
                default:
                    return false;
            }
        }
    }
}
