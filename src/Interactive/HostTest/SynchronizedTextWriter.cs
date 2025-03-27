// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    internal sealed class SynchronizedStringWriter : StringWriter
    {
        public readonly object SyncRoot = new object();

        public override void Write(char value)
        {
            lock (SyncRoot)
            {
                base.Write(value);
            }
        }

        public override void Write(string? value)
        {
            lock (SyncRoot)
            {
                base.Write(value);
            }
        }

        public override void Write(char[] buffer, int index, int count)
        {
            lock (SyncRoot)
            {
                base.Write(buffer, index, count);
            }
        }

        public override string ToString()
        {
            lock (SyncRoot)
            {
                return base.ToString();
            }
        }

        public string? Prefix(string mark, ref int start)
        {
            Debug.Assert(!string.IsNullOrEmpty(mark));

            lock (SyncRoot)
            {
                var builder = GetStringBuilder();

                for (int i = start, n = builder.Length - mark.Length; i <= n; i++)
                {
                    int j = 0;
                    while (j < mark.Length && builder[i + j] == mark[j])
                    {
                        j++;
                    }

                    if (j == mark.Length)
                    {
                        var result = builder.ToString(start, i - start);
                        start = i + j;
                        return result;
                    }
                }

                return null;
            }
        }

        public void Clear()
        {
            lock (SyncRoot)
            {
                GetStringBuilder().Clear();
            }
        }
    }
}
