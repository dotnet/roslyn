// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Metadata;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class ModuleImpl : Module
    {
        internal readonly System.Reflection.Module Module;

        internal ModuleImpl(System.Reflection.Module module)
        {
            Debug.Assert(module != null);
            this.Module = module;
        }

        public override Type[] FindTypes(TypeFilter filter, object filterCriteria)
        {
            throw new NotImplementedException();
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            throw new NotImplementedException();
        }

        public override Type GetType(string className, bool throwOnError, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetTypes()
        {
            throw new NotImplementedException();
        }

        public override Guid ModuleVersionId
        {
            get { return this.Module.ModuleVersionId; }
        }
    }
}
