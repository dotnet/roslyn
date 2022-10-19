// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.ImplementType
{
    [DataContract]
    internal readonly record struct ImplementTypeOptions(
        [property: DataMember(Order = 0)] ImplementTypeInsertionBehavior InsertionBehavior = ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind,
        [property: DataMember(Order = 1)] ImplementTypePropertyGenerationBehavior PropertyGenerationBehavior = ImplementTypePropertyGenerationBehavior.PreferThrowingProperties)
    {
        public ImplementTypeOptions()
            : this(InsertionBehavior: ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind)
        {
        }

        public static readonly ImplementTypeOptions Default = new();
    }
}
