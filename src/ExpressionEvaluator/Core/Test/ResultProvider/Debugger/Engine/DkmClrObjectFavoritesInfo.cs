// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.Debugger.Evaluation
{
    public class DkmClrObjectFavoritesInfo
    {
        public DkmClrObjectFavoritesInfo(IList<string> favorites, string displayString = null, string simpleDisplayString = null)
        {
            Favorites = new ReadOnlyCollection<string>(favorites);
            DisplayString = displayString;
            SimpleDisplayString = simpleDisplayString;
        }

        public string DisplayString { get; }
        public string SimpleDisplayString { get; }
        public ReadOnlyCollection<string> Favorites { get; }
    }
}
