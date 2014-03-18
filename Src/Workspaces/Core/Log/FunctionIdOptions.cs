// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal static class FunctionIdOptions
    {
        private const string FeatureName = "Performance/FunctionId";

        private static readonly ConcurrentDictionary<FunctionId, Option<bool>> options =
            new ConcurrentDictionary<FunctionId, Option<bool>>();

        private static readonly Func<FunctionId, Option<bool>> optionGetter =
            id => new Option<bool>(FeatureName, Enum.GetName(typeof(FunctionId), id), defaultValue: GetDefaultValue(id));

        public static Option<bool> GetOption(FunctionId id)
        {
            return options.GetOrAdd(id, optionGetter);
        }

        private static bool GetDefaultValue(FunctionId id)
        {
            return id != FunctionId.FindReference_ProcessDocumentAsync;
        }
    }
}