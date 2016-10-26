// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class BoundExpression
    {
        /// <summary>
        /// Returns a serializable object that is used for displaying this expression in a diagnostic message.
        /// </summary>
        public virtual object Display
        {
            get
            {
                Debug.Assert((object)this.Type != null, $"Unexpected null type in {this.GetType().Name}");
                return this.Type;
            }
        }
    }

    internal sealed partial class BoundArgListOperator
    {
        public override object Display
        {
            get { return "__arglist"; }
        }
    }

    internal sealed partial class BoundLiteral
    {
        public override object Display
        {
            get { return ConstantValue.IsNull ? MessageID.IDS_NULL.Localize() : base.Display; }
        }
    }

    internal sealed partial class BoundLambda
    {
        public override object Display
        {
            get { return this.MessageID.Localize(); }
        }
    }

    internal sealed partial class UnboundLambda
    {
        public override object Display
        {
            get { return this.MessageID.Localize(); }
        }
    }

    internal sealed partial class BoundMethodGroup
    {
        public override object Display
        {
            get { return MessageID.IDS_MethodGroup.Localize(); }
        }
    }

    internal sealed partial class BoundThrowExpression
    {
        public override object Display
        {
            get { return MessageID.IDS_ThrowExpression.Localize(); }
        }
    }

    internal partial class BoundTupleExpression
    {
        public override object Display
        {
            get
            {
                var pooledBuilder = PooledStringBuilder.GetInstance();
                var builder = pooledBuilder.Builder;
                var arguments = this.Arguments;


                builder.Append('(');
                builder.Append(arguments[0].Display);

                for(int i = 1; i < arguments.Length; i++)
                {
                    builder.Append(", ");
                    builder.Append(arguments[i].Display);
                }

                builder.Append(')');

                return pooledBuilder.ToStringAndFree();
            }
        }
    }

    internal sealed partial class BoundPropertyGroup
    {
        public override object Display
        {
            get { throw ExceptionUtilities.Unreachable; }
        }
    }

    internal partial class OutVariablePendingInference
    {
        public override object Display
        {
            get { return string.Empty; }
        }
    }

    internal partial class OutDeconstructVarPendingInference
    {
        public override object Display
        {
            get { return string.Empty; }
        }
    }

    internal partial class DeconstructionVariablePendingInference
    {
        public override object Display
        {
            get { throw ExceptionUtilities.Unreachable; }
        }
    }
}
