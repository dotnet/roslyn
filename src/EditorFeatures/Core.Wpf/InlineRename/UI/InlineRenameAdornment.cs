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
    internal class InlineRenameAdornment : UserControl, IDisposable
    {
        public InlineRenameAdornment()
            : base()
        {
        }

        public virtual void Dispose() { }
    }
}
