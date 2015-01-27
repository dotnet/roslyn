// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    [ExcludeFromCodeCoverage]
    internal partial class FeatureAttribute : Attribute
    {
        private readonly string featureName;

        public FeatureAttribute(string featureName)
        {
            this.featureName = featureName;
        }

        public string FeatureName
        {
            get { return featureName; }
        }
    }
}
