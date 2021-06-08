// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    public abstract partial class Compilation
    {
        internal readonly struct DeterministicKeyBuilder
        {
            internal StringBuilder Builder { get; }

            public DeterministicKeyBuilder(StringBuilder builder)
            {
                Builder = builder;
            }

            internal void AppendEnum<T>(string name, T value) where T : struct, Enum
            {
                Builder.Append(name);
                Builder.Append('=');
                Builder.Append(value.ToString());
                Builder.AppendLine();
            }

            internal void AppendString(string name, string? value)
            {
                // Skip null values for brevity. The lack of the value is just as significant in the 
                // key and overall makes it more readable
                if (value is object)
                {
                    Builder.Append(name);
                    Builder.Append('=');
                    Builder.Append(value);
                    Builder.AppendLine();
                }
            }

            internal void AppendByteArray(string name, ImmutableArray<byte> value)
            {
                if (!value.IsDefault)
                {
                    Builder.Append(name);
                    Builder.Append('=');
                    foreach (var b in value)
                    {
                        Builder.Append(b.ToString("x"));
                    }
                    Builder.AppendLine();
                }
            }
        }
    }
}
