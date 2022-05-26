// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections
{
    public sealed class Enumerator : IEnumerator, ICloneable
    {
        internal static IEnumerator Create(Snapshot snapshot)
        {
            var newEnumerator = new Enumerator(snapshot);
            return (IEnumerator)ComAggregate.CreateAggregatedObject(newEnumerator);
        }

        private readonly Snapshot _snapshot;
        private int _currentItemIndex;

        private Enumerator(Snapshot snapshot)
        {
            _snapshot = snapshot;
            Reset();
        }

        public object Current
        {
            get { return _snapshot[_currentItemIndex]; }
        }

        public bool MoveNext()
        {
            if (_currentItemIndex >= _snapshot.Count - 1)
            {
                return false;
            }

            _currentItemIndex++;
            return true;
        }

        public void Reset()
            => _currentItemIndex = -1;

        public object Clone()
            => Create(_snapshot);
    }
}
