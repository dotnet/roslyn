// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Roslyn.Hosting.Diagnostics.PerfMargin
{
    internal class DataModel
    {
        public ActivityLevel RootNode { get; }

        private readonly ActivityLevel[] _activities;

        public DataModel()
        {
            var functions = from f in typeof(FunctionId).GetFields()
                            where !f.IsSpecialName
                            select f;

            var count = functions.Count();
            _activities = new ActivityLevel[count];

            var features = new Dictionary<string, ActivityLevel>();
            var root = new ActivityLevel("All");

            foreach (var function in functions)
            {
                var value = (int)function.GetRawConstantValue();
                var name = function.Name;
                var featureNames = name.Split('_');
                var featureName = featureNames.Length > 1 ? featureNames[0] : "Uncategorized";

                if (!features.TryGetValue(featureName, out var parent))
                {
                    parent = new ActivityLevel(featureName, root, createChildList: true);
                    features[featureName] = parent;
                }

                _activities[value - 1] = new ActivityLevel(name, parent, createChildList: false);
            }

            root.SortChildren();
            this.RootNode = root;
        }

        public void BlockStart(FunctionId functionId)
        {
            _activities[(int)functionId - 1].Start();
        }

        public void BlockDisposed(FunctionId functionId)
        {
            _activities[(int)functionId - 1].Stop();
        }
    }
}
