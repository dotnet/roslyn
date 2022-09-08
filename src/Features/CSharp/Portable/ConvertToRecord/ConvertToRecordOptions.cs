// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRecord
{
    internal sealed class ConvertToRecordOptions
    {
        public static readonly Option2<bool> Disable = new("ConvertToRecord", "Disable",
            defaultValue: true, new FeatureFlagStorageLocation("Roslyn.ConvertToRecordDisable"));
    }
}
