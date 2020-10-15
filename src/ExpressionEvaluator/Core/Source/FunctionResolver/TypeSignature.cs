// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal enum TypeSignatureKind
    {
        GenericType,
        QualifiedType,
        ArrayType,
        PointerType,
    }

    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal abstract class TypeSignature
    {
        internal abstract TypeSignatureKind Kind { get; }

        internal abstract string GetDebuggerDisplay();
    }

    internal sealed class GenericTypeSignature : TypeSignature
    {
        internal GenericTypeSignature(QualifiedTypeSignature qualifiedName, ImmutableArray<TypeSignature> typeArguments)
        {
            Debug.Assert(qualifiedName != null);
            Debug.Assert(!typeArguments.IsDefault);
            QualifiedName = qualifiedName;
            TypeArguments = typeArguments;
        }

        internal override TypeSignatureKind Kind => TypeSignatureKind.GenericType;
        internal QualifiedTypeSignature QualifiedName { get; }
        internal ImmutableArray<TypeSignature> TypeArguments { get; }

        internal override string GetDebuggerDisplay()
        {
            var builder = new StringBuilder();
            builder.Append(QualifiedName.GetDebuggerDisplay());
            builder.Append('<');
            bool any = false;
            foreach (var typeArg in TypeArguments)
            {
                if (any)
                {
                    builder.Append(", ");
                }
                builder.Append(typeArg.GetDebuggerDisplay());
                any = true;
            }
            builder.Append('>');
            return builder.ToString();
        }
    }

    internal sealed class QualifiedTypeSignature : TypeSignature
    {
        internal QualifiedTypeSignature(TypeSignature qualifier, string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Qualifier = qualifier;
            Name = name;
        }

        internal override TypeSignatureKind Kind => TypeSignatureKind.QualifiedType;
        internal TypeSignature Qualifier { get; }
        internal string Name { get; }

        internal override string GetDebuggerDisplay()
        {
            return (Qualifier == null) ? Name : $"{Qualifier.GetDebuggerDisplay()}.{Name}";
        }
    }

    internal sealed class ArrayTypeSignature : TypeSignature
    {
        internal ArrayTypeSignature(TypeSignature elementType, int rank)
        {
            Debug.Assert(elementType != null);
            ElementType = elementType;
            Rank = rank;
        }

        internal override TypeSignatureKind Kind => TypeSignatureKind.ArrayType;
        internal TypeSignature ElementType { get; }
        internal int Rank { get; }

        internal override string GetDebuggerDisplay()
        {
            var builder = new StringBuilder();
            builder.Append(ElementType.GetDebuggerDisplay());
            builder.Append('[');
            builder.Append(',', Rank - 1);
            builder.Append(']');
            return builder.ToString();
        }
    }

    internal sealed class PointerTypeSignature : TypeSignature
    {
        internal PointerTypeSignature(TypeSignature pointedAtType)
        {
            Debug.Assert(pointedAtType != null);
            PointedAtType = pointedAtType;
        }

        internal override TypeSignatureKind Kind => TypeSignatureKind.PointerType;
        internal TypeSignature PointedAtType { get; }

        internal override string GetDebuggerDisplay()
        {
            return $"{PointedAtType.GetDebuggerDisplay()}*";
        }
    }
}
