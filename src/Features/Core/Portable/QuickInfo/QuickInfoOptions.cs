// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    [DataContract]
    internal readonly record struct QuickInfoOptions(
        [property: DataMember(Order = 0)] bool ShowRemarksInQuickInfo = true,
        [property: DataMember(Order = 1)] bool IncludeNavigationHintsInQuickInfo = true)
    {
        public QuickInfoOptions()
            : this(ShowRemarksInQuickInfo: true)
        {
        }

        public static readonly QuickInfoOptions Default = new();
    }
}
