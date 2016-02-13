// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    internal class PropertyDictionary : Dictionary<string, object>
    {
        public T GetOrDefault<T>(string name, T @default)
        {
            object value;
            if (this.TryGetValue(name, out value))
            {
                return (T)value;
            }
            return @default;
        }

        public new object this[string name]
        {
            get
            {
                object value;
                return this.TryGetValue(name, out value)
                    ? value : null;
            }
            set { base[name] = value; }
        }
    }
}
