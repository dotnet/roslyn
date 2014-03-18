// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Cci = Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal class MethodImplementation : Cci.IMethodImplementation
    {
        private readonly Cci.IMethodDefinition implementing;
        private readonly Cci.IMethodReference implemented;

        public MethodImplementation(Cci.IMethodDefinition implementing, Cci.IMethodReference implemented)
        {
            this.implementing = implementing;
            this.implemented = implemented;
        }

        #region IMethodImplementation Members

        public Cci.ITypeDefinition ContainingType
        {
            get { return implementing.ContainingTypeDefinition; }
        }

        public void Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public Cci.IMethodReference ImplementingMethod
        {
            get { return implementing; }
        }

        public Cci.IMethodReference ImplementedMethod
        {
            get { return implemented; }
        }

        #endregion
    }
}