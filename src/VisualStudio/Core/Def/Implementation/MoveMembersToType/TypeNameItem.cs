// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembersToType
{
    internal class TypeNameItem
    {
        public string TypeName { get; }
        public bool IsFromHistory { get; }

        public TypeNameItem(bool isFromHistory, string @typeName)
        {
            IsFromHistory = isFromHistory;
            TypeName = @typeName;
        }

        public override string ToString() => TypeName;
    }
}
