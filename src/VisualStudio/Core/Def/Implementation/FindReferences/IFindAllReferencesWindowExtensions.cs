﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    internal static class IFindAllReferencesWindowExtensions
    {
        public static ColumnState2 GetDefinitionColumn(this IFindAllReferencesWindow window)
        {
            return window.TableControl.ColumnStates.FirstOrDefault(
                s => s.Name == StandardTableColumnDefinitions2.Definition) as ColumnState2;
        }
    }
}
