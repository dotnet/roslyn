// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using System;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// Supporession column definition filter
    /// </summary>
    /// <remarks>
    /// TODO: Move this column down to the shell as it is shared by multiple issue sources (Roslyn and FxCop).
    /// </remarks>
    [Export(typeof(EntryFilterDefinition))]
    [Name(SuppressionStateColumnDefinition.ColumnName)]
    internal class SuppressionStateColumnFilterDefinition : EntryFilterDefinition
    {
        public override bool HasAttribute(string key) => string.Equals(NonActionable, key, StringComparison.OrdinalIgnoreCase);
    }
}

