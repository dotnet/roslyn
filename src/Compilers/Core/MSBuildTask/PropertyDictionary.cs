﻿using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.BuildTask
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
            set { this[name] = value; }
        }
    }
}
