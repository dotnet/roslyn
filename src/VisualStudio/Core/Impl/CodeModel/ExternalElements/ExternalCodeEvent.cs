// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements;

[ComVisible(true)]
[ComDefaultInterface(typeof(EnvDTE80.CodeEvent))]
public sealed class ExternalCodeEvent : AbstractExternalCodeMember, EnvDTE80.CodeEvent
{
    internal static EnvDTE80.CodeEvent Create(CodeModelState state, ProjectId projectId, IEventSymbol symbol)
    {
        var element = new ExternalCodeEvent(state, projectId, symbol);
        return (EnvDTE80.CodeEvent)ComAggregate.CreateAggregatedObject(element);
    }

    private ExternalCodeEvent(CodeModelState state, ProjectId projectId, IEventSymbol symbol)
        : base(state, projectId, symbol)
    {
    }

    private IEventSymbol EventSymbol
    {
        get { return (IEventSymbol)LookupSymbol(); }
    }

    protected override EnvDTE.CodeElements GetParameters()
        => throw new NotImplementedException();

    public override EnvDTE.vsCMElement Kind
    {
        get { return EnvDTE.vsCMElement.vsCMElementEvent; }
    }

    public EnvDTE.CodeFunction Adder
    {
        get
        {
            var symbol = EventSymbol;
            if (symbol.AddMethod == null)
            {
                throw Exceptions.ThrowEFail();
            }

            return ExternalCodeAccessorFunction.Create(this.State, this.ProjectId, symbol.AddMethod, this);
        }

        set
        {
            throw Exceptions.ThrowEFail();
        }
    }

    // TODO: Verify VB implementation
    public bool IsPropertyStyleEvent
    {
        get { return true; }
    }

    // TODO: Verify VB implementation
    public EnvDTE80.vsCMOverrideKind OverrideKind
    {
        get
        {
            throw Exceptions.ThrowENotImpl();
        }

        set
        {
            throw Exceptions.ThrowEFail();
        }
    }

    public EnvDTE.CodeFunction Remover
    {
        get
        {
            var symbol = EventSymbol;
            if (symbol.RemoveMethod == null)
            {
                throw Exceptions.ThrowEFail();
            }

            return ExternalCodeAccessorFunction.Create(this.State, this.ProjectId, symbol.RemoveMethod, this);
        }

        set
        {
            throw Exceptions.ThrowEFail();
        }
    }

    public EnvDTE.CodeFunction Thrower
    {
        get
        {
            // TODO: Verify this with VB implementation
            var symbol = EventSymbol;
            if (symbol.RaiseMethod == null)
            {
                throw Exceptions.ThrowEFail();
            }

            return ExternalCodeAccessorFunction.Create(this.State, this.ProjectId, symbol.RaiseMethod, this);
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
            return CodeTypeRef.Create(this.State, this, this.ProjectId, EventSymbol.Type);
        }

        set
        {
            throw Exceptions.ThrowEFail();
        }
    }
}
