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
        internal static EnvDTE.CodeVariable Create(CodeModelState state, ProjectId projectId, ISymbol symbol)
        {
            var element = new ExternalCodeVariable(state, projectId, symbol);
            return (EnvDTE.CodeVariable)ComAggregate.CreateAggregatedObject(element);
        }

        private ExternalCodeVariable(CodeModelState state, ProjectId projectId, ISymbol symbol)
            : base(state, projectId, symbol)
        {
        }

        private ITypeSymbol GetSymbolType()
        {
            var symbol = LookupSymbol();
            if (symbol != null)
            {
                switch (symbol.Kind)
                {
                    case SymbolKind.Field:
                        return ((IFieldSymbol)symbol).Type;
                    case SymbolKind.Property:
                        // Note: VB WithEvents fields are represented as properties
                        return ((IPropertySymbol)symbol).Type;
                }
            }

            return null;
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
                if (!(LookupSymbol() is IFieldSymbol fieldSymbol))
                {
                    return false;
                }

                return fieldSymbol.IsConst
                    || fieldSymbol.IsReadOnly;
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
                var type = GetSymbolType();
                if (type == null)
                {
                    throw Exceptions.ThrowEFail();
                }

                return CodeTypeRef.Create(this.State, this, this.ProjectId, type);
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
                if (!(LookupSymbol() is IFieldSymbol fieldSymbol))
                {
                    return EnvDTE80.vsCMConstKind.vsCMConstKindNone;
                }

                if (fieldSymbol.IsConst)
                {
                    return EnvDTE80.vsCMConstKind.vsCMConstKindConst;
                }
                else if (fieldSymbol.IsReadOnly)
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
                return GetSymbolType() is INamedTypeSymbol namedType
                    ? namedType.IsGenericType
                    : false;
            }
        }
    }
}
