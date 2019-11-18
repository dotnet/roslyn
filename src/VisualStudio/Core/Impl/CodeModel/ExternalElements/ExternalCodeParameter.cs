// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE.CodeParameter))]
    public sealed class ExternalCodeParameter : AbstractExternalCodeElement, EnvDTE.CodeParameter, EnvDTE80.CodeParameter2
    {
        internal static EnvDTE.CodeParameter Create(CodeModelState state, ProjectId projectId, IParameterSymbol symbol, AbstractExternalCodeMember parent)
        {
            var element = new ExternalCodeParameter(state, projectId, symbol, parent);
            return (EnvDTE.CodeParameter)ComAggregate.CreateAggregatedObject(element);
        }

        private readonly ParentHandle<AbstractExternalCodeElement> _parentHandle;

        private ExternalCodeParameter(CodeModelState state, ProjectId projectId, IParameterSymbol symbol, AbstractExternalCodeElement parent)
            : base(state, projectId, symbol)
        {
            _parentHandle = new ParentHandle<AbstractExternalCodeElement>(parent);
        }

        private IParameterSymbol ParameterSymbol
        {
            get { return (IParameterSymbol)LookupSymbol(); }
        }

        protected override EnvDTE.CodeElements GetCollection()
        {
            return GetCollection<ExternalCodeParameter>(this.Parent);
        }

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementParameter; }
        }

        protected override string GetDocComment()
        {
            return string.Empty;
        }

        protected override object GetParent()
        {
            return _parentHandle.Value;
        }

        public new EnvDTE.CodeElement Parent
        {
            get { return (EnvDTE.CodeElement)GetParent(); }
        }

        public EnvDTE.CodeTypeRef Type
        {
            get
            {
                return CodeTypeRef.Create(this.State, this, this.ProjectId, ParameterSymbol.Type);
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public string DefaultValue
        {
            get
            {
                return ParameterSymbol is
                {
                    HasExplicitDefaultValue: true,
                    ExplicitDefaultValue: { }
                } ? ParameterSymbol.ExplicitDefaultValue.ToString()
                    : null;
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public EnvDTE80.vsCMParameterKind ParameterKind
        {
            get
            {
                var result = EnvDTE80.vsCMParameterKind.vsCMParameterKindNone;

                if (ParameterSymbol.RefKind == RefKind.Ref)
                {
                    result = EnvDTE80.vsCMParameterKind.vsCMParameterKindRef;
                }
                else if (ParameterSymbol.RefKind == RefKind.Out)
                {
                    result = EnvDTE80.vsCMParameterKind.vsCMParameterKindOut;
                }
                else if (ParameterSymbol.IsParams)
                {
                    result = EnvDTE80.vsCMParameterKind.vsCMParameterKindParamArray;
                }

                if (ParameterSymbol.IsOptional)
                {
                    result |= EnvDTE80.vsCMParameterKind.vsCMParameterKindOptional;
                }

                return result;
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }
    }
}
