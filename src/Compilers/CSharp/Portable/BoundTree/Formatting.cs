// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
                var argumentDisplays = new object[arguments.Length];

                builder.Append('(');
                builder.Append("{0}");
                argumentDisplays[0] = arguments[0].Display;

                for (int i = 1; i < arguments.Length; i++)
                {
                    builder.Append(", {" + i + "}");
                    argumentDisplays[i] = arguments[i].Display;
                }

                builder.Append(')');

                var format = pooledBuilder.ToStringAndFree();
                return FormattableStringFactory.Create(format, argumentDisplays);
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

    internal partial class BoundDiscardExpression
    {
        public override object Display
        {
            get { return (object)this.Type ?? "_"; }
        }
    }

    internal partial class DeconstructionVariablePendingInference
    {
        public override object Display
        {
            get { throw ExceptionUtilities.Unreachable; }
        }
    }

    internal partial class BoundDefaultLiteral
    {
        public override object Display
        {
            get { return (object)this.Type ?? "default"; }
        }
    }

    internal partial class BoundStackAllocArrayCreation
    {
        public override object Display
            => (Type is null) ? FormattableStringFactory.Create("stackalloc {0}[{1}]", ElementType, Count.WasCompilerGenerated ? null : Count.Syntax.ToString()) : base.Display;
    }

    internal partial class BoundUnconvertedSwitchExpression
    {
        public override object Display
            => (Type is null) ? MessageID.IDS_FeatureSwitchExpression.Localize() : base.Display;
    }

    internal partial class BoundPassByCopy
    {
        public override object Display => Expression.Display;
    }

    internal partial class UnboundObjectCreationExpression
    {
        public override object Display => "new()";
    }
}
