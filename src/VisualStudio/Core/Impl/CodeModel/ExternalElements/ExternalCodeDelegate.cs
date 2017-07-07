// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE.CodeDelegate))]
    public sealed class ExternalCodeDelegate : AbstractExternalCodeType, EnvDTE80.CodeDelegate2, EnvDTE.CodeDelegate, EnvDTE.CodeType, EnvDTE80.CodeElement2, EnvDTE.CodeElement
    {
        internal static EnvDTE.CodeDelegate Create(CodeModelState state, ProjectId projectId, ITypeSymbol typeSymbol)
        {
            var element = new ExternalCodeDelegate(state, projectId, typeSymbol);
            return (EnvDTE.CodeDelegate)ComAggregate.CreateAggregatedObject(element);
        }

        private ExternalCodeDelegate(CodeModelState state, ProjectId projectId, ITypeSymbol symbol)
            : base(state, projectId, symbol)
        {
        }

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementDelegate; }
        }

        public EnvDTE.CodeClass BaseClass
        {
            get { throw Exceptions.ThrowEFail(); }
        }

        public EnvDTE.CodeElements Parameters
        {
            get { throw new NotImplementedException(); }
        }

        public EnvDTE.CodeTypeRef Type
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool IsGeneric
        {
            get { throw new NotImplementedException(); }
        }
    }
}
