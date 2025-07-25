// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.VisualStudio.Shell.FindAllReferences;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages;

internal static class IFindAllReferencesWindowExtensions
{
    extension(IFindAllReferencesWindow window)
    {
        public ColumnState2 GetDefinitionColumn()
        {
            return (ColumnState2)window.TableControl.ColumnStates.First(
                s => s.Name == StandardTableColumnDefinitions2.Definition);
        }
    }
}
