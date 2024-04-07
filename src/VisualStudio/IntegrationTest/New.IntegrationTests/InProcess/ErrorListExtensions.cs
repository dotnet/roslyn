// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    internal static class ErrorListExtensions
    {
        public static __VSERRORCATEGORY GetCategory(this ITableEntry tableEntry)
        {
            return tableEntry.GetValueOrDefault(StandardTableKeyNames.ErrorSeverity, (__VSERRORCATEGORY)(-1));
        }

        public static ItemOrigin? GetItemOrigin(this ITableEntry tableEntry)
        {
            return tableEntry.GetValueOrNull<ItemOrigin>(StandardTableKeyNames.ItemOrigin);
        }

        public static string? GetPath(this ITableEntry tableEntry)
        {
            return tableEntry.GetValueOrDefault<string?>(StandardTableKeyNames.Path, null);
        }

        public static string? GetDocumentName(this ITableEntry tableEntry)
        {
            return tableEntry.GetValueOrDefault<string?>(StandardTableKeyNames.DocumentName, null);
        }

        public static string? GetDisplayPath(this ITableEntry tableEntry)
        {
            return tableEntry.GetValueOrDefault<string?>(StandardTableKeyNames.DisplayPath, null);
        }

        public static int? GetLine(this ITableEntry tableEntry)
        {
            return tableEntry.GetValueOrNull<int>(StandardTableKeyNames.Line);
        }

        public static int? GetColumn(this ITableEntry tableEntry)
        {
            return tableEntry.GetValueOrNull<int>(StandardTableKeyNames.Column);
        }

        public static string? GetErrorCode(this ITableEntry tableEntry)
        {
            return tableEntry.GetValueOrDefault<string?>(StandardTableKeyNames.ErrorCode, null);
        }

        public static ErrorSource? GetErrorSource(this ITableEntry tableEntry)
        {
            return tableEntry.GetValueOrNull<ErrorSource>(StandardTableKeyNames.ErrorSource);
        }

        public static string? GetText(this ITableEntry tableEntry)
        {
            return tableEntry.GetValueOrDefault<string?>(StandardTableKeyNames.Text, null);
        }

        private static T GetValueOrDefault<T>(this ITableEntry tableEntry, string keyName, T defaultValue)
        {
            if (!tableEntry.TryGetValue(keyName, out T value))
            {
                value = defaultValue;
            }

            return value;
        }

        private static T? GetValueOrNull<T>(this ITableEntry tableEntry, string keyName)
            where T : struct
        {
            if (!tableEntry.TryGetValue(keyName, out T value))
            {
                return null;
            }

            return value;
        }
    }
}
