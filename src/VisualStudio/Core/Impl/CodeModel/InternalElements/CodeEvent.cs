// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements;

[ComVisible(true)]
[ComDefaultInterface(typeof(EnvDTE80.CodeEvent))]
public sealed partial class CodeEvent : AbstractCodeMember, EnvDTE80.CodeEvent
{
    internal static EnvDTE80.CodeEvent Create(
        CodeModelState state,
        FileCodeModel fileCodeModel,
        SyntaxNodeKey nodeKey,
        int? nodeKind)
    {
        var element = new CodeEvent(state, fileCodeModel, nodeKey, nodeKind);
        var result = (EnvDTE80.CodeEvent)ComAggregate.CreateAggregatedObject(element);

        fileCodeModel.OnCodeElementCreated(nodeKey, (EnvDTE.CodeElement)result);

        return result;
    }

    internal static EnvDTE80.CodeEvent CreateUnknown(
        CodeModelState state,
        FileCodeModel fileCodeModel,
        int nodeKind,
        string name)
    {
        var element = new CodeEvent(state, fileCodeModel, nodeKind, name);
        return (EnvDTE80.CodeEvent)ComAggregate.CreateAggregatedObject(element);
    }

    private CodeEvent(
        CodeModelState state,
        FileCodeModel fileCodeModel,
        SyntaxNodeKey nodeKey,
        int? nodeKind)
        : base(state, fileCodeModel, nodeKey, nodeKind)
    {
    }

    private CodeEvent(
        CodeModelState state,
        FileCodeModel fileCodeModel,
        int nodeKind,
        string name)
        : base(state, fileCodeModel, nodeKind, name)
    {
    }

    private IEventSymbol EventSymbol
    {
        get { return (IEventSymbol)LookupSymbol(); }
    }

    public override EnvDTE.vsCMElement Kind
    {
        get { return EnvDTE.vsCMElement.vsCMElementEvent; }
    }

    public override EnvDTE.CodeElements Children
    {
        get { return this.Attributes; }
    }

    public EnvDTE.CodeFunction Adder
    {
        get
        {
            if (IsPropertyStyleEvent)
            {
                return CodeAccessorFunction.Create(this.State, this, MethodKind.EventAdd);
            }

            return null;
        }

        set
        {
            // Stroke of luck: both C# and VB legacy code model implementations throw E_NOTIMPL
            throw Exceptions.ThrowENotImpl();
        }
    }

    public bool IsPropertyStyleEvent
    {
        get
        {
            var node = this.CodeModelService.GetNodeWithModifiers(LookupNode());
            return this.CodeModelService.GetIsPropertyStyleEvent(node);
        }
    }

    public EnvDTE.CodeFunction Remover
    {
        get
        {
            if (IsPropertyStyleEvent)
            {
                return CodeAccessorFunction.Create(this.State, this, MethodKind.EventRemove);
            }

            return null;
        }

        set
        {
            // Stroke of luck: both C# and VB legacy code model implementations throw E_NOTIMPL
            throw Exceptions.ThrowENotImpl();
        }
    }

    public EnvDTE.CodeFunction Thrower
    {
        get
        {
            if (!CodeModelService.SupportsEventThrower)
            {
                throw Exceptions.ThrowEFail();
            }

            if (IsPropertyStyleEvent)
            {
                return CodeAccessorFunction.Create(this.State, this, MethodKind.EventRaise);
            }

            return null;
        }

        set
        {
            // TODO: C# throws E_FAIL but VB throws E_NOTIMPL.
            throw new NotImplementedException();
        }
    }

    public EnvDTE.CodeTypeRef Type
    {
        get
        {
            return CodeTypeRef.Create(this.State, this, GetProjectId(), EventSymbol.Type);
        }

        set
        {
            // The type is sometimes part of the node key, so we should be sure to reacquire
            // it after updating it. Note that we pass trackKinds: false because it's possible
            // that UpdateType might change the kind of a node (e.g. change a VB Sub to a Function).

            UpdateNodeAndReacquireNodeKey(FileCodeModel.UpdateType, value, trackKinds: false);
        }
    }
}
