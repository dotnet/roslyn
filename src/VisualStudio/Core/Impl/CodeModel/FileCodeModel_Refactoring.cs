// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    public sealed partial class FileCodeModel
    {
        public void Rename(EnvDTE.CodeElement element)
        {
            throw new NotImplementedException();
        }

        public void RenameNoUI(EnvDTE.CodeElement element, string newName, bool fPreview, bool fSearchComments, bool fOverloads)
        {
            // TODO: Support options

            var codeElement = ComAggregate.TryGetManagedObject<AbstractCodeElement>(element);
            if (codeElement != null)
            {
                codeElement.RenameSymbol(newName);
            }
        }

        public void ReorderParameters(EnvDTE.CodeElement element)
        {
            throw new NotImplementedException();
        }

        public void ReorderParametersNoUI(EnvDTE.CodeElement element, long[] paramIndices, bool fPreview)
        {
            throw new NotImplementedException();
        }

        public void RemoveParameter(EnvDTE.CodeElement element)
        {
            throw new NotImplementedException();
        }

        public void RemoveParameterNoUI(EnvDTE.CodeElement element, object parameter, bool fPreview)
        {
            throw new NotImplementedException();
        }

        public void EncapsulateField(EnvDTE.CodeVariable variable)
        {
            throw new NotImplementedException();
        }

        public EnvDTE.CodeProperty EncapsulateFieldNoUI(EnvDTE.CodeVariable variable, string propertyName, EnvDTE.vsCMAccess accessibility, ReferenceSelectionEnum refSelection, PropertyTypeEnum propertyType, bool fPreview, bool fSearchComments)
        {
            throw new NotImplementedException();
        }

        public void ExtractInterface(EnvDTE.CodeType codeType)
        {
            throw new NotImplementedException();
        }

        public void ImplementInterface(EnvDTE.CodeType implementor, object @interface, bool fExplicit)
        {
            // TODO: Implement!
        }

        public void ImplementAbstractClass(EnvDTE.CodeType implementor, object abstractClass)
        {
            // TODO: Implement!
        }

        public EnvDTE.CodeElement ImplementOverride(EnvDTE.CodeElement member, EnvDTE.CodeType implementor)
        {
            throw new NotImplementedException();
        }
    }
}
