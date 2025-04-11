// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;

public sealed class CodeFunctionWithEventHandler : CodeFunction, Interop.IEventHandler
{
    internal static new EnvDTE.CodeFunction Create(
       CodeModelState state,
       FileCodeModel fileCodeModel,
       SyntaxNodeKey nodeKey,
       int? nodeKind)
    {
        var element = new CodeFunctionWithEventHandler(state, fileCodeModel, nodeKey, nodeKind);
        var result = (EnvDTE.CodeFunction)ComAggregate.CreateAggregatedObject(element);

        fileCodeModel.OnCodeElementCreated(nodeKey, (EnvDTE.CodeElement)result);

        return result;
    }

    internal static new EnvDTE.CodeFunction CreateUnknown(
       CodeModelState state,
       FileCodeModel fileCodeModel,
       int nodeKind,
       string name)
    {
        var element = new CodeFunctionWithEventHandler(state, fileCodeModel, nodeKind, name);
        return (EnvDTE.CodeFunction)ComAggregate.CreateAggregatedObject(element);
    }

    private CodeFunctionWithEventHandler(
        CodeModelState state,
        FileCodeModel fileCodeModel,
        SyntaxNodeKey nodeKey,
        int? nodeKind)
        : base(state, fileCodeModel, nodeKey, nodeKind)
    {
    }

    private CodeFunctionWithEventHandler(
        CodeModelState state,
        FileCodeModel fileCodeModel,
        int nodeKind,
        string name)
        : base(state, fileCodeModel, nodeKind, name)
    {
    }

    public int AddHandler(string bstrEventName)
    {
        var node = LookupNode();
        var semanticModel = GetSemanticModel();

        FileCodeModel.PerformEdit(document =>
        {
            return CodeModelService.AddHandlesClause(document, bstrEventName, node, CancellationToken.None);
        });

        return VSConstants.S_OK;
    }

    public int RemoveHandler(string bstrEventName)
    {
        var node = LookupNode();
        var semanticModel = GetSemanticModel();

        FileCodeModel.PerformEdit(document =>
        {
            return CodeModelService.RemoveHandlesClause(document, bstrEventName, node, CancellationToken.None);
        });

        return VSConstants.S_OK;
    }

    public int GetHandledEvents(out IVsEnumBSTR ppUnk)
    {
        var node = LookupNode();
        var semanticModel = GetSemanticModel();
        var handledEvents = CodeModelService.GetHandledEventNames(node, semanticModel);
        ppUnk = new VsEnumBSTR(handledEvents);

        return VSConstants.S_OK;
    }

    public int HandlesEvent(string bstrEventName, out bool result)
    {
        var node = LookupNode();
        var semanticModel = GetSemanticModel();

        result = CodeModelService.HandlesEvent(bstrEventName, node, semanticModel);

        return VSConstants.S_OK;
    }
}
