// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements
{
    public abstract class AbstractExternalCodeMember : AbstractExternalCodeElement
    {
        internal AbstractExternalCodeMember(CodeModelState state, ProjectId projectId, ISymbol symbol)
            : base(state, projectId, symbol)
        {
        }

        protected virtual bool GetCanOverride()
        {
            var symbol = LookupSymbol();
            return symbol.IsVirtual;
        }

        protected virtual bool GetMustImplement()
        {
            var symbol = LookupSymbol();
            return symbol.IsAbstract;
        }

        protected virtual EnvDTE.CodeElements GetParameters()
        {
            return ExternalParameterCollection.Create(this.State, this, this.ProjectId);
        }

        public bool CanOverride
        {
            get
            {
                return GetCanOverride();
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public bool MustImplement
        {
            get
            {
                return GetMustImplement();
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public EnvDTE.CodeElements Parameters
        {
            get { return GetParameters(); }
        }
    }
}
