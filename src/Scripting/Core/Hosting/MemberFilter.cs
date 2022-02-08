// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal class MemberFilter
    {
        public virtual bool Include(StackFrame frame) => Include(frame.GetMethod());
        public virtual bool Include(MemberInfo member) => true;
    }
}
