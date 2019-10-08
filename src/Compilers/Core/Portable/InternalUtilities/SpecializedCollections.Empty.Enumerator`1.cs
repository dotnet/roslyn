// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal partial class SpecializedCollections
    {
        private partial class Empty
        {
            internal class Enumerator<T> : Enumerator, IEnumerator<T>
            {
                public static new readonly IEnumerator<T> Instance = new Enumerator<T>();

                protected Enumerator()
                {
                }

                public new T Current => throw new InvalidOperationException();

                public void Dispose()
                {
                }
            }
        }
    }
}
