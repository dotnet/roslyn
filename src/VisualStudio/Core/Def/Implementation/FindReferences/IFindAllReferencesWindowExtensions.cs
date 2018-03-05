// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
