// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    internal partial class ArrayBuilder<T>
    {
        /// <summary>
        /// struct enumerator used in foreach.
        /// </summary>
        internal struct Enumerator : IEnumerator<T>
        {
            private readonly ArrayBuilder<T> builder;
            private int index;

            public Enumerator(ArrayBuilder<T> builder)
            {
                this.builder = builder;
                this.index = -1;
            }

            public T Current
            {
                get
                {
                    return this.builder[this.index];
                }
            }

            public bool MoveNext()
            {
                this.index++;
                return this.index < this.builder.Count;
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }
}