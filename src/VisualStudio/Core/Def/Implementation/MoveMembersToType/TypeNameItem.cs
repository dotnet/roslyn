// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembersToType
{
    internal class TypeNameItem
    {
        public string TypeName { get; }
        public string DeclarationFile { get; }
        public bool IsFromHistory { get; }
        public bool IsNew { get; }

        public TypeNameItem(bool isFromHistory, string declarationFile, string @typeName)
        {
            IsFromHistory = isFromHistory;
            IsNew = false;
            TypeName = @typeName;
            DeclarationFile = declarationFile;
        }

        public TypeNameItem(string @typeName)
        {
            IsFromHistory = false;
            IsNew = true;
            TypeName = @typeName;
            DeclarationFile = "[WIP] Auto-Generated File";
        }

        public override string ToString() => TypeName;

        public static int CompareTo(TypeNameItem x, TypeNameItem y)
        {
            // sort so that history is first, then type name, then file name
            if (x.IsFromHistory ^ y.IsFromHistory)
            {
                // one is from history and the other isn't
                return x.IsFromHistory ? -1 : 1;
            }
            // compare by each namespace/finally type
            var xnames = x.TypeName.Split('.');
            var ynames = y.TypeName.Split('.');

            for (var i = 0; i < Math.Min(xnames.Length, ynames.Length); i++)
            {
                var comp = xnames[i].CompareTo(ynames[i]);
                if (comp != 0)
                {
                    return comp;
                }
            }

            return x.DeclarationFile.CompareTo(y.DeclarationFile);
        }
    }
}
