// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    [Export]
    internal class ClassificationTypeMap : AbstractClassificationTypeMap
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ClassificationTypeMap(
            IClassificationTypeRegistryService registryService) : base(registryService, ClassificationLayer.Default)
        {
        }
    }
}
