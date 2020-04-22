// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class TextEditEqualityComparer : IEqualityComparer<TextEdit>
    {
        public bool Equals(TextEdit x, TextEdit y)
        {
            return EqualityComparer<Range>.Default.Equals(x.Range, y.Range) &&
                   x.NewText == y.NewText;
        }

        public int GetHashCode(TextEdit obj)
        {
            var hashCode = -1114201889;
            hashCode = (hashCode * -1521134295) + EqualityComparer<Range>.Default.GetHashCode(obj.Range);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(obj.NewText);
            return hashCode;
        }
    }
}
