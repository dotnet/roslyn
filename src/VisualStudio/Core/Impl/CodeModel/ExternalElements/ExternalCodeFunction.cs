// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements;

[ComVisible(true)]
[ComDefaultInterface(typeof(EnvDTE.CodeFunction))]
public sealed class ExternalCodeFunction : AbstractExternalCodeMember, ICodeElementContainer<ExternalCodeParameter>, EnvDTE.CodeFunction, EnvDTE80.CodeFunction2
{
    internal static EnvDTE.CodeFunction Create(CodeModelState state, ProjectId projectId, IMethodSymbol symbol)
    {
        var element = new ExternalCodeFunction(state, projectId, symbol);
        return (EnvDTE.CodeFunction)ComAggregate.CreateAggregatedObject(element);
    }

    private ExternalCodeFunction(CodeModelState state, ProjectId projectId, IMethodSymbol symbol)
        : base(state, projectId, symbol)
    {
    }

    private IMethodSymbol MethodSymbol
    {
        get { return (IMethodSymbol)LookupSymbol(); }
    }

    EnvDTE.CodeElements ICodeElementContainer<ExternalCodeParameter>.GetCollection()
        => this.Parameters;

    public override EnvDTE.vsCMElement Kind
    {
        get { return EnvDTE.vsCMElement.vsCMElementFunction; }
    }

    public EnvDTE.vsCMFunction FunctionKind
    {
        get
        {
            // TODO: Verify VB implementation
            switch (MethodSymbol.MethodKind)
            {
                case MethodKind.Constructor:
                    return EnvDTE.vsCMFunction.vsCMFunctionConstructor;
                case MethodKind.Destructor:
                    return EnvDTE.vsCMFunction.vsCMFunctionDestructor;
                case MethodKind.UserDefinedOperator:
                case MethodKind.Conversion:
                    return EnvDTE.vsCMFunction.vsCMFunctionOperator;
                case MethodKind.Ordinary:
                    return EnvDTE.vsCMFunction.vsCMFunctionFunction;
                default:
                    throw Exceptions.ThrowEFail();
            }
        }
    }

    public bool IsOverloaded
    {
        get
        {
            var symbol = (IMethodSymbol)LookupSymbol();

            // Only methods and constructors can be overloaded
            if (symbol.MethodKind is not MethodKind.Ordinary and
                not MethodKind.Constructor)
            {
                return false;
            }

            var methodsOfName = symbol.ContainingType.GetMembers(symbol.Name)
                                                     .Where(m => m.Kind == SymbolKind.Method);

            return methodsOfName.Count() > 1;
        }
    }

    public EnvDTE.CodeElements Overloads
    {
        get
        {
            return ExternalOverloadsCollection.Create(this.State, this, this.ProjectId);
        }
    }

    public EnvDTE.CodeTypeRef Type
    {
        get
        {
            // TODO: What if ReturnType is null?
            return CodeTypeRef.Create(this.State, this, this.ProjectId, MethodSymbol.ReturnType);
        }

        set
        {
            throw Exceptions.ThrowEFail();
        }
    }

    public bool IsGeneric
    {
        get { return MethodSymbol.IsGenericMethod; }
    }

    public EnvDTE80.vsCMOverrideKind OverrideKind
    {
        get
        {
            // TODO: Verify VB implementation

            var symbol = MethodSymbol;
            var result = EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone;

            if (symbol.IsAbstract)
            {
                result = EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract;
            }

            if (symbol.IsVirtual)
            {
                result = EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual;
            }

            if (symbol.IsOverride)
            {
                result = EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride;
            }

            if (symbol.HidesBaseMethodsByName)
            {
                result = EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew;
            }

            if (symbol.IsSealed)
            {
                result = EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed;
            }

            return result;
        }

        set
        {
            throw Exceptions.ThrowEFail();
        }
    }
}
