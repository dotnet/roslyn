// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements;

[ComVisible(true)]
[ComDefaultInterface(typeof(EnvDTE.CodeClass))]
public sealed class ExternalCodeClass : AbstractExternalCodeType, EnvDTE80.CodeClass2, EnvDTE.CodeClass, EnvDTE.CodeType, EnvDTE.CodeElement, EnvDTE80.CodeElement2
{
    internal static EnvDTE.CodeClass Create(CodeModelState state, ProjectId projectId, ITypeSymbol typeSymbol)
    {
        var newElement = new ExternalCodeClass(state, projectId, typeSymbol);
        return (EnvDTE.CodeClass)ComAggregate.CreateAggregatedObject(newElement);
    }

    private ExternalCodeClass(CodeModelState state, ProjectId projectId, ITypeSymbol typeSymbol)
        : base(state, projectId, typeSymbol)
    {
    }

    public override EnvDTE.vsCMElement Kind
    {
        get { return EnvDTE.vsCMElement.vsCMElementClass; }
    }

    public EnvDTE80.vsCMClassKind ClassKind
    {
        get
        {
            return EnvDTE80.vsCMClassKind.vsCMClassKindMainClass;
        }

        set
        {
            throw Exceptions.ThrowEFail();
        }
    }

    public EnvDTE80.vsCMDataTypeKind DataTypeKind
    {
        get
        {
            return EnvDTE80.vsCMDataTypeKind.vsCMDataTypeKindMain;
        }

        set
        {
            throw Exceptions.ThrowEFail();
        }
    }

    public override EnvDTE.CodeElements ImplementedInterfaces
    {
        get { return ExternalTypeCollection.Create(this.State, this, this.ProjectId, TypeSymbol.AllInterfaces); }
    }

    public EnvDTE80.vsCMInheritanceKind InheritanceKind
    {
        get
        {
            if (IsAbstract)
            {
                return EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract;
            }
            else if (IsSealed)
            {
                return EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed;
            }
            else
            {
                return EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindNone;
            }
        }

        set
        {
            throw Exceptions.ThrowEFail();
        }
    }

    public new bool IsAbstract
    {
        get
        {
            return base.IsAbstract;
        }

        set
        {
            throw new NotImplementedException();
        }
    }

    public bool IsGeneric
        => TypeSymbol is INamedTypeSymbol { IsGenericType: true };

    public EnvDTE.CodeElements PartialClasses
    {
        get { throw Exceptions.ThrowEFail(); }
    }

    public EnvDTE.CodeElements Parts
    {
        get { throw Exceptions.ThrowEFail(); }
    }

    public EnvDTE.CodeClass AddClass(string name, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access)
        => throw Exceptions.ThrowEFail();

    public EnvDTE.CodeDelegate AddDelegate(string name, object type, object position, EnvDTE.vsCMAccess access)
        => throw Exceptions.ThrowEFail();

    public EnvDTE.CodeEnum AddEnum(string name, object position, object bases, EnvDTE.vsCMAccess access)
        => throw Exceptions.ThrowEFail();

    public EnvDTE.CodeFunction AddFunction(string name, EnvDTE.vsCMFunction kind, object type, object position, EnvDTE.vsCMAccess access, object location)
        => throw Exceptions.ThrowEFail();

    public EnvDTE.CodeInterface AddImplementedInterface(object @base, object position)
        => throw Exceptions.ThrowEFail();

    public EnvDTE.CodeProperty AddProperty(string getterName, string putterName, object type, object position, EnvDTE.vsCMAccess access, object location)
        => throw Exceptions.ThrowEFail();

    public EnvDTE.CodeStruct AddStruct(string name, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access)
        => throw Exceptions.ThrowEFail();

    public EnvDTE.CodeVariable AddVariable(string name, object type, object position, EnvDTE.vsCMAccess access, object location)
        => throw Exceptions.ThrowEFail();

    public EnvDTE80.CodeEvent AddEvent(string name, string fullDelegateName, bool createPropertyStyleEvent, object location, EnvDTE.vsCMAccess access)
        => throw Exceptions.ThrowEFail();

    public void RemoveInterface(object element)
        => throw Exceptions.ThrowEFail();
}
