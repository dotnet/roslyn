// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Reflection.Mock;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.CSharp.Formatter
{
    internal enum ExpansionKind
    {
        RuntimeType,
        BaseType,
        Member,
        ArrayElement,
        PointerDereference,
        RawView,
        StaticMembers,
    }

    internal abstract class ExpansionRow
    {
        public abstract ExpansionKind Kind { get; }
    }

    internal sealed class RuntimeTypeRow : ExpansionRow
    {
        public static readonly RuntimeTypeRow Instance = new RuntimeTypeRow();

        private RuntimeTypeRow()
        {
        }

        public override ExpansionKind Kind
        {
            get { return ExpansionKind.RuntimeType; }
        }
    }

    internal sealed class BaseTypeRow : ExpansionRow
    {
        public readonly Type BaseType;

        public BaseTypeRow(Type baseType)
        {
            Debug.Assert(baseType != null);
            this.BaseType = baseType;
        }

        public override ExpansionKind Kind
        {
            get { return ExpansionKind.BaseType; }
        }
    }

    internal sealed class MemberRow : ExpansionRow
    {
        public readonly MemberInfo Member;

        public MemberRow(MemberInfo member)
        {
            Debug.Assert(member != null);
            this.Member = member;
        }

        public override ExpansionKind Kind
        {
            get { return ExpansionKind.Member; }
        }
    }

    internal sealed class ArrayElementRow : ExpansionRow
    {
        public readonly int[] Indices;

        public ArrayElementRow(int[] indices)
        {
            Debug.Assert(indices != null);
            this.Indices = indices;
        }

        public override ExpansionKind Kind
        {
            get { return ExpansionKind.ArrayElement; }
        }
    }

    internal sealed class PointerDereferenceRow : ExpansionRow
    {
        public readonly string Name;
        public readonly Type ElementType;

        public PointerDereferenceRow(string name, Type elementType)
        {
            Debug.Assert(elementType != null);
            this.Name = name;
            this.ElementType = elementType;
        }

        public override ExpansionKind Kind
        {
            get { return ExpansionKind.PointerDereference; }
        }
    }

    internal sealed class RawViewRow : ExpansionRow
    {
        public readonly string Name;
        public readonly Type DeclaredType;
        public readonly DkmClrValue Value;

        public RawViewRow(string name, Type declaredType, DkmClrValue value)
        {
            Debug.Assert(declaredType != null);
            this.Name = name;
            this.DeclaredType = declaredType;
            this.Value = value;
        }

        public override ExpansionKind Kind
        {
            get { return ExpansionKind.RawView; }
        }
    }

    internal sealed class StaticMembersRow : ExpansionRow
    {
        public readonly CollectionExpansion Expansion;

        public StaticMembersRow(CollectionExpansion expansion)
        {
            Debug.Assert(expansion != null);
            this.Expansion = expansion;
        }

        public override ExpansionKind Kind
        {
            get { return ExpansionKind.StaticMembers; }
        }
    }
}
