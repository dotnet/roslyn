// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Hosting.Diagnostics.PerfMargin
{
    internal class DataModel
    {
        public ActivityLevel RootNode { get; }

        private readonly ImmutableArray<ActivityLevel?> _activities;

        public DataModel()
        {
            var fields = from field in typeof(FunctionId).GetFields()
                         where !field.IsSpecialName
                         select field;

            using var _ = ArrayBuilder<ActivityLevel?>.GetInstance(out var builder);

            var features = new Dictionary<string, ActivityLevel>();
            var root = new ActivityLevel("All");

            foreach (var field in fields)
            {
                var value = (int)field.GetRawConstantValue();
                var name = field.Name;
                var featureNames = name.Split('_');
                var featureName = featureNames.Length > 1 ? featureNames[0] : "Uncategorized";

                if (!features.TryGetValue(featureName, out var parent))
                {
                    parent = new ActivityLevel(featureName, root, createChildList: true);
                    features[featureName] = parent;
                }

                builder.SetItem(value, new ActivityLevel(name, parent, createChildList: false));
            }

            _activities = builder.ToImmutable();
            root.SortChildren();
            RootNode = root;
        }

        public void BlockStart(FunctionId functionId)
            => _activities[(int)functionId]!.Start();

        public void BlockDisposed(FunctionId functionId)
            => _activities[(int)functionId]!.Stop();
    }
}
