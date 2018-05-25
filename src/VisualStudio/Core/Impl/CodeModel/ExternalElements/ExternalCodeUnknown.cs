// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE80.CodeElement2))]
    public sealed class ExternalCodeUnknown : AbstractExternalCodeElement, EnvDTE.CodeElement, EnvDTE80.CodeElement2
    {
        internal static EnvDTE.CodeElement Create(CodeModelState state, ProjectId projectId, ITypeSymbol typeSymbol)
        {
            var newElement = new ExternalCodeUnknown(state, projectId, typeSymbol);
            return (EnvDTE.CodeElement)ComAggregate.CreateAggregatedObject(newElement);
        }

        private readonly string _name;

        private ExternalCodeUnknown(CodeModelState state, ProjectId projectId, ITypeSymbol typeSymbol)
            : base(state, projectId, typeSymbol)
        {
            _name = typeSymbol.Name;
        }

        public override EnvDTE.vsCMElement Kind
        {
            get { return EnvDTE.vsCMElement.vsCMElementOther; }
        }
    }
}
