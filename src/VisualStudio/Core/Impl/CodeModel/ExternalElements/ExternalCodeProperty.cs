// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            => this.Parameters;

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementProperty; }
        }

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
                var symbol = PropertySymbol;
                var result = EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone;

                if (symbol.IsAbstract)
                {
                    result |= EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract;
                }

                if (symbol.IsVirtual)
                {
                    result |= EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual;
                }

                if (symbol.IsOverride)
                {
                    result |= EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride;
                }

                if (symbol.IsSealed)
                {
                    result |= EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed;
                }

                return result;
            }

            set
            {
                throw Exceptions.ThrowEFail();
            }
        }

        public EnvDTE.CodeElement Parent2
        {
            get { return (EnvDTE.CodeElement)base.Parent; }
        }

        public EnvDTE80.vsCMPropertyKind ReadWrite
        {
            get
            {
                var symbol = PropertySymbol;
                if (symbol.GetMethod != null)
                {
                    return symbol.SetMethod != null
                        ? EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite
                        : EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly;
                }
                else if (symbol.SetMethod != null)
                {
                    return EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly;
                }

                throw Exceptions.ThrowEUnexpected();
            }
        }
    }
}
