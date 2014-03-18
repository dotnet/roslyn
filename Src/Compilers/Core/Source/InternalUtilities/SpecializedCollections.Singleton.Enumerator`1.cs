// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal static partial class SpecializedCollections
    {
        private static partial class Singleton
        {
            internal class Enumerator<T> : IEnumerator<T>
            {
                private T loneValue;
                private bool moveNextCalled;

                public Enumerator(T value)
                {
                    this.loneValue = value;
                    this.moveNextCalled = false;
                }

                public T Current
                {
                    get
                    {
                        return this.loneValue;
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return this.loneValue;
                    }
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (!this.moveNextCalled)
                    {
                        this.moveNextCalled = true;
                        return true;
                    }

                    return false;
                }

                public void Reset()
                {
                    this.moveNextCalled = false;
                }
            }
        }
    }
}
