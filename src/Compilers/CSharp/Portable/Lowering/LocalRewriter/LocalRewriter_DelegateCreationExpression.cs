// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            if (node.Argument.HasDynamicType())
            {
                var loweredArgument = VisitExpression(node.Argument);

                // Creates a delegate whose instance is the delegate that is returned by the call-site and the method is Invoke.
                var loweredReceiver = _dynamicFactory.MakeDynamicConversion(loweredArgument, isExplicit: false, isArrayIndex: false, isChecked: false, resultType: node.Type).ToExpression();

                return new BoundDelegateCreationExpression(node.Syntax, loweredReceiver, methodOpt: null, isExtensionMethod: false, type: node.Type);
            }

            if (node.IsExtensionMethod)
            {
                var method = node.MethodOpt;
                Debug.Assert((object)method != null);
                Debug.Assert(method.IsInExtensionClass || method.MethodKind == MethodKind.ReducedExtension);
                method = method.UnreduceExtensionMethod();
                node = node.Update(node.Argument, method, isExtensionMethod: true, type: node.Type);
            }

            return base.VisitDelegateCreationExpression(node);
        }
    }
}
