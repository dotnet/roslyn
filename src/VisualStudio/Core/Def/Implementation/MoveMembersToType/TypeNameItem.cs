// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembersToType
{
    internal class TypeNameItem
    {
        public string TypeName { get; }
        public string DeclarationFile { get; }
        public bool IsFromHistory { get; }

        public TypeNameItem(bool isFromHistory, string declarationFile, string @typeName)
        {
            IsFromHistory = isFromHistory;
            TypeName = @typeName;
            DeclarationFile = declarationFile;
        }

        public override string ToString() => TypeName;
    }
}
