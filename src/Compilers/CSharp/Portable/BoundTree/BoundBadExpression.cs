// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundBadExpression
    {
        private partial void Validate()
        {
            Debug.Assert(!this.ChildBoundNodes.Any(c => Binder.IsTypeOrValueExpression(c) || Binder.IsMethodGroupWithTypeOrValueReceiver(c)));
        }
    }
}
