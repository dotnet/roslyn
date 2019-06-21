// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Setup
{
    internal sealed class EmptyFeatureController : IFeatureController
    {
        internal static readonly EmptyFeatureController Instance = new EmptyFeatureController();

        private EmptyFeatureController()
        {
        }
    }
}
