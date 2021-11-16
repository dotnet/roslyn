// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop
{
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    [Guid("376A7817-DB0D-4cfd-956E-5143F71F5CCD")]
    public interface ICSCodeModelRefactoring
    {
        void Rename(EnvDTE.CodeElement element);
        void RenameNoUI(EnvDTE.CodeElement element, string newName, bool fPreview, bool fSearchComments, bool fOverloads);
        void ReorderParameters(EnvDTE.CodeElement element);
        void ReorderParametersNoUI(EnvDTE.CodeElement element, long[] paramIndices, bool fPreview);
        void RemoveParameter(EnvDTE.CodeElement element);
        void RemoveParameterNoUI(EnvDTE.CodeElement element, object parameter, bool fPreview);
        void EncapsulateField(EnvDTE.CodeVariable variable);
        EnvDTE.CodeProperty EncapsulateFieldNoUI(EnvDTE.CodeVariable variable, string propertyName, EnvDTE.vsCMAccess accessibility, ReferenceSelectionEnum refSelection, PropertyTypeEnum propertyType, bool fPreview, bool fSearchComments);

        void ExtractInterface(EnvDTE.CodeType codeType);
        void ImplementInterface(EnvDTE.CodeType implementor, object @interface, bool fExplicit);
        void ImplementAbstractClass(EnvDTE.CodeType implementor, object abstractClass);
        EnvDTE.CodeElement ImplementOverride(EnvDTE.CodeElement member, EnvDTE.CodeType implementor);
    }
}
