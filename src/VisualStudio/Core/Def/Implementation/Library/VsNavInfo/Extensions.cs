// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                    builder.Add(name.Substring(start, separatorPos - start), type);
                    start = separatorPos + 1;
                    separatorPos = name.IndexOf(separator, start);
                }

                if (start < name.Length)
                {
                    builder.Add(name.Substring(start), type);
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
