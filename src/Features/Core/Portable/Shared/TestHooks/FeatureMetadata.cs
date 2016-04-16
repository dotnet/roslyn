// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    /// <summary>
    /// This interface declares a contract for MEF to import metadata in a strongly-typed fashion.
    /// The name of the interface is unimportant, only the property names and types are important.
    /// This interface matches the Roslyn.Services.Threading.FeatureAttribute's public properties.
    /// Whatever you specify in a feature like this [Feature("Outlining")] will become available in
    /// this interface's properties (FeatureName will be set to "Outlining")
    /// </summary>
    /// <remarks>A good link is: http://mef.codeplex.com/wikipage?title=Exports%20and%20Metadata
    /// </remarks>
    internal class FeatureMetadata
    {
        public string FeatureName { get; }

        public FeatureMetadata(IDictionary<string, object> data)
        {
            this.FeatureName = (string)data.GetValueOrDefault("FeatureName");
        }
    }
}
