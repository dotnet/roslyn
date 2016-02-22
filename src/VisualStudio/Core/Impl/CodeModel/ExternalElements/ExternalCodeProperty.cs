// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE.CodeProperty))]
    public sealed class ExternalCodeProperty : AbstractExternalCodeMember, ICodeElementContainer<ExternalCodeParameter>, EnvDTE.CodeProperty, EnvDTE80.CodeProperty2
    {
        internal static EnvDTE.CodeProperty Create(CodeModelState state, ProjectId projectId, IPropertySymbol symbol)
        {
            var element = new ExternalCodeProperty(state, projectId, symbol);
            return (EnvDTE.CodeProperty)ComAggregate.CreateAggregatedObject(element);
        }

        private ExternalCodeProperty(CodeModelState state, ProjectId projectId, IPropertySymbol symbol)
            : base(state, projectId, symbol)
        {
        }

        private IPropertySymbol PropertySymbol
        {
            get { return (IPropertySymbol)LookupSymbol(); }
        }

        EnvDTE.CodeElements ICodeElementContainer<ExternalCodeParameter>.GetCollection()
        {
            return this.Parameters;
        }

        protected override EnvDTE.CodeElements GetParameters()
        {
            // TODO: Need to figure out what to do here. This comes from CodeProperty2, but C# apparently never
            // that interface for external code elements.
            throw new NotImplementedException();
        }

        public override EnvDTE.vsCMElement Kind => EnvDTE.vsCMElement.vsCMElementProperty;

        public EnvDTE.CodeFunction Getter
        {
            get
            {
                var symbol = PropertySymbol;
                if (symbol.GetMethod == null)
                {
                    throw Exceptions.ThrowEFail();
                }

                return ExternalCodeAccessorFunction.Create(this.State, this.ProjectId, symbol.GetMethod, this);
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public new EnvDTE.CodeClass Parent
        {
            get { return (EnvDTE.CodeClass)base.Parent; }
        }

        public EnvDTE.CodeFunction Setter
        {
            get
            {
                var symbol = PropertySymbol;
                if (symbol.SetMethod == null)
                {
                    throw Exceptions.ThrowEFail();
                }

                return ExternalCodeAccessorFunction.Create(this.State, this.ProjectId, symbol.SetMethod, this);
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public EnvDTE.CodeTypeRef Type
        {
            get
            {
                return CodeTypeRef.Create(this.State, this, this.ProjectId, PropertySymbol.Type);
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public bool IsDefault
        {
            get
            {
                return PropertySymbol.IsIndexer;
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public bool IsGeneric
        {
            get { return false; }
        }

        public EnvDTE80.vsCMOverrideKind OverrideKind
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

        public EnvDTE.CodeElement Parent2
        {
            get { throw new NotImplementedException(); }
        }

        public EnvDTE80.vsCMPropertyKind ReadWrite
        {
            get { throw new NotImplementedException(); }
        }
    }
}
