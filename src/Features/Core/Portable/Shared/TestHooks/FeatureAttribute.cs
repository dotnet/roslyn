// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal partial class FeatureAttribute : Attribute
    {
        private readonly string _featureName;

        public FeatureAttribute(string featureName)
        {
            _featureName = featureName;
        }

        public string FeatureName
        {
            get { return _featureName; }
        }
    }
}
