// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess;

internal static class ErrorListExtensions
{
    extension(ITableEntry tableEntry)
    {
        public __VSERRORCATEGORY GetCategory()
        {
            return tableEntry.GetValueOrDefault(StandardTableKeyNames.ErrorSeverity, (__VSERRORCATEGORY)(-1));
        }

        public ItemOrigin? GetItemOrigin()
        {
            return tableEntry.GetValueOrNull<ItemOrigin>(StandardTableKeyNames.ItemOrigin);
        }

        public string? GetPath()
        {
            return tableEntry.GetValueOrDefault<string?>(StandardTableKeyNames.Path, null);
        }

        public string? GetDocumentName()
        {
            return tableEntry.GetValueOrDefault<string?>(StandardTableKeyNames.DocumentName, null);
        }

        public string? GetDisplayPath()
        {
            return tableEntry.GetValueOrDefault<string?>(StandardTableKeyNames.DisplayPath, null);
        }

        public int? GetLine()
        {
            return tableEntry.GetValueOrNull<int>(StandardTableKeyNames.Line);
        }

        public int? GetColumn()
        {
            return tableEntry.GetValueOrNull<int>(StandardTableKeyNames.Column);
        }

        public string? GetErrorCode()
        {
            return tableEntry.GetValueOrDefault<string?>(StandardTableKeyNames.ErrorCode, null);
        }

        public ErrorSource? GetErrorSource()
        {
            return tableEntry.GetValueOrNull<ErrorSource>(StandardTableKeyNames.ErrorSource);
        }

        public string? GetText()
        {
            return tableEntry.GetValueOrDefault<string?>(StandardTableKeyNames.Text, null);
        }

        private T GetValueOrDefault<T>(string keyName, T defaultValue)
        {
            if (!tableEntry.TryGetValue(keyName, out T value))
            {
                value = defaultValue;
            }

            return value;
        }

        private T? GetValueOrNull<T>(string keyName)
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
