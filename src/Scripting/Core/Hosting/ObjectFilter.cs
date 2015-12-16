// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    public class ObjectFilter
    {
        public virtual IEnumerable<StackFrame> Filter(IEnumerable<StackFrame> frames) => frames;
        public virtual IEnumerable<MemberInfo> Filter(IEnumerable<MemberInfo> members) => members;
    }
}