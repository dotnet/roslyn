// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    /// <summary>
    /// A base class for rename controls. Needs to have a default constructor so the 
    /// type can be used in ResourceDictionaries in XAML
    /// </summary>
    internal class InlineRenameAdornment : UserControl, IDisposable
    {
        public virtual void Dispose() { }
    }
}
