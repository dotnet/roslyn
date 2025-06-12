// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements;

[ComVisible(true)]
[ComDefaultInterface(typeof(EnvDTE80.CodeElement2))]
public sealed class ExternalCodeUnknown : AbstractExternalCodeElement, EnvDTE.CodeElement, EnvDTE80.CodeElement2
{
    internal static EnvDTE.CodeElement Create(CodeModelState state, ProjectId projectId, ITypeSymbol typeSymbol)
    {
        var newElement = new ExternalCodeUnknown(state, projectId, typeSymbol);
        return (EnvDTE.CodeElement)ComAggregate.CreateAggregatedObject(newElement);
    }

    private ExternalCodeUnknown(CodeModelState state, ProjectId projectId, ITypeSymbol typeSymbol)
        : base(state, projectId, typeSymbol)
    {
    }

    public override EnvDTE.vsCMElement Kind
    {
        get { return EnvDTE.vsCMElement.vsCMElementOther; }
    }
}
