// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            Debug.Assert(!node.Type.IsAnonymousType); // Missing EnsureParamCollectionAttributeExists call?

            if (node.Argument.HasDynamicType())
            {
                var loweredArgument = VisitExpression(node.Argument);

                // Creates a delegate whose instance is the delegate that is returned by the call-site and the method is Invoke.
                var loweredReceiver = _dynamicFactory.MakeDynamicConversion(loweredArgument, isExplicit: false, isArrayIndex: false, isChecked: false, resultType: node.Type).ToExpression();

                return new BoundDelegateCreationExpression(node.Syntax, loweredReceiver, methodOpt: null, isExtensionMethod: false, node.WasTargetTyped, type: node.Type);
            }

            if (node.Argument.Kind == BoundKind.MethodGroup)
            {
                var mg = (BoundMethodGroup)node.Argument;
                var method = node.MethodOpt;
                Debug.Assert(method is { });
                var oldSyntax = _factory.Syntax;
                _factory.Syntax = (mg.ReceiverOpt ?? mg).Syntax;
                bool isExtensionMethod = node.IsExtensionMethod;
                var receiver = (!method.RequiresInstanceReceiver && !isExtensionMethod && !method.IsAbstract && !method.IsVirtual) ? _factory.Type(method.ContainingType) : VisitExpression(mg.ReceiverOpt);
                _factory.Syntax = oldSyntax;

                AdjustDelegateTargetMethodIfNecessary(ref method, ref isExtensionMethod);

                Debug.Assert(receiver is not null);
                return node.Update(receiver, method, isExtensionMethod, node.WasTargetTyped, node.Type);
            }

            return base.VisitDelegateCreationExpression(node)!;
        }

        private static void AdjustDelegateTargetMethodIfNecessary(ref MethodSymbol method, ref bool isExtensionMethod)
        {
            // PROTOTYPE(roles): We could do this transformation in a separate pass after LocalRewriter and for instance extension methods
            //                   that work could be bundled with InstanceExtensionMethodBodyRewriter, addressing all the concerns about
            //                   BoundThisReference result type inconsistencies (see a comment for InstanceExtensionMethodBodyRewriter.VisitThisReference).
            if (method.OriginalDefinition.ContainingSymbol is NamedTypeSymbol declaringTypeDefinition &&
                declaringTypeDefinition.TryGetCorrespondingStaticMetadataExtensionMember(method.OriginalDefinition) is MethodSymbol metadataMethod)
            {
                method = metadataMethod.AsMember(method.ContainingType).ConstructIfGeneric(method.TypeArgumentsWithAnnotations);

                // PROTOTYPE(roles): We probably also need to convert the receiver to the parameter's type here

                isExtensionMethod = true;
            }
        }
    }
}
