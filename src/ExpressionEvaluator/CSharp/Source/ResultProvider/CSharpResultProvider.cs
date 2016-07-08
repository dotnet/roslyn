// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    [DkmReportNonFatalWatsonException(ExcludeExceptionType = typeof(NotImplementedException)), DkmContinueCorruptingException]
    internal sealed class CSharpResultProvider : ResultProvider
    {
        public CSharpResultProvider()
            : this(new CSharpFormatter())
        {
        }

        private CSharpResultProvider(CSharpFormatter formatter)
            : this(formatter, formatter)
        {
        }

        internal CSharpResultProvider(IDkmClrFormatter2 formatter2, IDkmClrFullNameProvider fullNameProvider)
            : base(formatter2, fullNameProvider)
        {
        }

        internal override string StaticMembersString
        {
            get { return Resources.StaticMembers; }
        }

        internal override bool IsPrimitiveType(Type type)
        {
            return type.IsPredefinedType();
        }
    }
}
