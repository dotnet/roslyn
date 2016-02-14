// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Semantics
{
    internal sealed class VariableDeclaration : IVariableDeclaration
    {
        public VariableDeclaration(ILocalSymbol variable, IOperation initialValue, SyntaxNode syntax)
        {
            Variable = variable;
            InitialValue = initialValue;
            Syntax = syntax;
        }

        public ILocalSymbol Variable { get; }

        public IOperation InitialValue { get; }

        public bool IsInvalid => Variable == null || (InitialValue != null && InitialValue.IsInvalid);

        public OperationKind Kind => OperationKind.VariableDeclaration;

        public SyntaxNode Syntax { get; }

        public ITypeSymbol Type => null;

        public Optional<object> ConstantValue => default(Optional<object>);

        public void Accept(OperationVisitor visitor)
        {
            visitor.VisitVariableDeclaration(this);
        }

        public TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitVariableDeclaration(this, argument);
        }
    }
}