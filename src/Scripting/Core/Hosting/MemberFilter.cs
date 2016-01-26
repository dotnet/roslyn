// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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