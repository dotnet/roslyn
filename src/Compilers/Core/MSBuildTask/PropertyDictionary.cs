// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    internal class PropertyDictionary : Dictionary<string, object?>
    {
        public T GetOrDefault<T>(string name, T @default)
        {
            object? value;
            if (this.TryGetValue(name, out value))
            {
                return (T)value!;
            }
            return @default;
        }

        public new object? this[string name]
        {
            get
            {
                object? value;
                return this.TryGetValue(name, out value)
                    ? value : null;
            }
            set { base[name] = value; }
        }
    }
}
