// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Event accessor that has been synthesized for a field-like event declared in source,
    /// or for an event re-abstraction in an interface.
    /// </summary>
    /// <remarks>
    /// Associated with <see cref="SourceFieldLikeEventSymbol"/> and <see cref="SourceCustomEventSymbol"/>.
    /// </remarks>
    internal sealed class SynthesizedEventAccessorSymbol : SourceEventAccessorSymbol
    {
        // Since we don't have a syntax reference, we'll have to use another object for locking.
        private readonly object _methodChecksLockObject = new object();

        internal SynthesizedEventAccessorSymbol(SourceEventSymbol @event, bool isAdder, bool isExpressionBodied, EventSymbol explicitlyImplementedEventOpt = null, string aliasQualifierOpt = null)
            : base(@event, null, @event.Location, explicitlyImplementedEventOpt, aliasQualifierOpt, isAdder, isIterator: false, isNullableAnalysisEnabled: false, isExpressionBodied: isExpressionBodied)
        {
            Debug.Assert(IsAbstract || IsExtern || IsFieldLikeEventAccessor());
        }

        private bool IsFieldLikeEventAccessor()
        {
            return AssociatedEvent.HasAssociatedField;
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal override bool GenerateDebugInfo
        {
            get { return false; }
        }

        protected override SourceMemberMethodSymbol BoundAttributesSource
        {
            get
            {
                Debug.Assert(PartialImplementationPart is null);

                if (PartialDefinitionPart is { } definitionPart)
                {
                    return (SourceMemberMethodSymbol)definitionPart;
                }

                return this.MethodKind == MethodKind.EventAdd
                    ? (SourceMemberMethodSymbol)this.AssociatedEvent.RemoveMethod
                    : null;
            }
        }

        protected override IAttributeTargetSymbol AttributeOwner
        {
            get
            {
                // attributes for this accessor are specified on the associated event:
                return AssociatedEvent;
            }
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            // If we are asking this question on a partial implementation symbol,
            // it must be from a context which prefers to order implementation attributes before definition attributes.
            // For example, the 'value' parameter of an add or remove accessor.
            if (PartialDefinitionPart is { } definitionPart)
            {
                return OneOrMany.Create(
                    this.AssociatedEvent.AttributeDeclarationSyntaxList,
                    ((SourceEventAccessorSymbol)definitionPart).AssociatedEvent.AttributeDeclarationSyntaxList);
            }

            if (PartialImplementationPart is { } implementationPart)
            {
                return OneOrMany.Create(
                    this.AssociatedEvent.AttributeDeclarationSyntaxList,
                    ((SourceEventAccessorSymbol)implementationPart).AssociatedEvent.AttributeDeclarationSyntaxList);
            }

            return OneOrMany.Create(this.AssociatedEvent.AttributeDeclarationSyntaxList);
        }

        protected override object MethodChecksLockObject
        {
            get { return _methodChecksLockObject; }
        }

        internal override MethodImplAttributes ImplementationAttributes
        {
            get
            {
                MethodImplAttributes result = base.ImplementationAttributes;

                if (!IsAbstract && !AssociatedEvent.IsWindowsRuntimeEvent && !ContainingType.IsStructType() &&
                    (object)DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Threading_Interlocked__CompareExchange_T) == null)
                {
                    // Under these conditions, this method needs to be synchronized.
                    result |= MethodImplAttributes.Synchronized;
                }

                return result;
            }
        }

        internal override ExecutableCodeBinder TryGetBodyBinder(BinderFactory binderFactoryOpt = null, bool ignoreAccessibility = false)
        {
            return TryGetBodyBinderFromSyntax(binderFactoryOpt, ignoreAccessibility);
        }

        internal override bool SynthesizesLoweredBoundBody
        {
            get
            {
                Debug.Assert(TryGetBodyBinder() is null);

                if (IsFieldLikeEventAccessor())
                {
                    return true;
                }

                return base.SynthesizesLoweredBoundBody;
            }
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            if (IsFieldLikeEventAccessor())
            {
                SourceEventSymbol fieldLikeEvent = AssociatedEvent;
                if (fieldLikeEvent.Type.IsDelegateType())
                {
                    BoundBlock body = CSharp.MethodBodySynthesizer.ConstructFieldLikeEventAccessorBody(fieldLikeEvent, isAddMethod: MethodKind == MethodKind.EventAdd, compilationState.Compilation, diagnostics);

                    if (body != null)
                    {
                        compilationState.AddSynthesizedMethod(this, body);
                    }
                }

                return;
            }

            base.GenerateMethodBody(compilationState, diagnostics);
        }
    }
}
