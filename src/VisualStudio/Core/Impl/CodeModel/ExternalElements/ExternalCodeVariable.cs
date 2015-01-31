// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE.CodeVariable))]
    public sealed class ExternalCodeVariable : AbstractExternalCodeMember, EnvDTE.CodeVariable, EnvDTE80.CodeVariable2
    {
        internal static EnvDTE.CodeVariable Create(CodeModelState state, ProjectId projectId, IFieldSymbol symbol)
        {
            var element = new ExternalCodeVariable(state, projectId, symbol);
            return (EnvDTE.CodeVariable)ComAggregate.CreateAggregatedObject(element);
        }

        private ExternalCodeVariable(CodeModelState state, ProjectId projectId, IFieldSymbol symbol)
            : base(state, projectId, symbol)
        {
        }

        private IFieldSymbol FieldSymbol
        {
            get { return (IFieldSymbol)LookupSymbol(); }
        }

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementVariable; }
        }

        public object InitExpression
        {
            get
            {
                throw Exceptions.ThrowEFail();
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public bool IsConstant
        {
            get
            {
                // TODO: Verify VB implementation
                var symbol = FieldSymbol;
                return symbol.IsConst
                    || symbol.IsReadOnly;
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
                return CodeTypeRef.Create(this.State, this, this.ProjectId, FieldSymbol.Type);
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public EnvDTE80.vsCMConstKind ConstKind
        {
            get
            {
                // TODO: Verify VB Implementation
                var symbol = FieldSymbol;
                if (symbol.IsConst)
                {
                    return EnvDTE80.vsCMConstKind.vsCMConstKindConst;
                }
                else if (symbol.IsReadOnly)
                {
                    return EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly;
                }
                else
                {
                    return EnvDTE80.vsCMConstKind.vsCMConstKindNone;
                }
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public bool IsGeneric
        {
            get
            {
                // TODO: C# checks whether the field Type is generic. What does VB do?
                var namedType = FieldSymbol.Type as INamedTypeSymbol;
                return namedType != null
                    ? namedType.IsGenericType
                    : false;
            }
        }
    }
}
