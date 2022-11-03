// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.VsNavInfo
{
    internal static class Extensions
    {
        public static void Add(this ImmutableArray<NavInfoNode>.Builder builder, string name, _LIB_LISTTYPE type, bool expandDottedNames)
        {
            if (name == null)
            {
                return;
            }

            if (expandDottedNames)
            {
                const char separator = '.';

                var start = 0;
                var separatorPos = name.IndexOf(separator, start);

                while (separatorPos >= 0)
                {
                    builder.Add(name[start..separatorPos], type);
                    start = separatorPos + 1;
                    separatorPos = name.IndexOf(separator, start);
                }

                if (start < name.Length)
                {
                    builder.Add(name[start..], type);
                }
            }
            else
            {
                builder.Add(name, type);
            }
        }

        public static void Add(this ImmutableArray<NavInfoNode>.Builder builder, string name, _LIB_LISTTYPE type)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            builder.Add(new NavInfoNode(name, type));
        }
    }
}
