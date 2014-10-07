// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal static class FeatureIdOptions
    {
        private const string FeatureName = "Performance/FeatureId";

        private static readonly ConcurrentDictionary<FeatureId, Option<bool>> options
            = new ConcurrentDictionary<FeatureId, Option<bool>>();

        private static readonly Func<FeatureId, Option<bool>> optionGetter =
            id => new Option<bool>(FeatureName, Enum.GetName(typeof(FeatureId), id), defaultValue: GetDefaultValue(id));

        public static Option<bool> GetOption(FeatureId featureId)
        {
            return options.GetOrAdd(featureId, optionGetter);
        }

        private static bool GetDefaultValue(FeatureId id)
        {
            return id != FeatureId.Cache &&
                   id != FeatureId.WorkCoordinator &&
                   id != FeatureId.Simplifier &&
                   id != FeatureId.Recoverable &&
                   id != FeatureId.Diagnostics;
        }
    }
}