// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal interface ISyntaxInputNode
    {
        void UpdateDriverStateTable(DriverStateTable.Builder dst);
    }

    internal class SyntaxFilterNode<T> : ISyntaxInputNode
    {
        internal InputNode<T> InputNode { get; }

        internal SyntaxContextReceiverCreator GetReceiverCreator() => null!;

        public void UpdateDriverStateTable(DriverStateTable.Builder dst)
        {

        }
    }
}
