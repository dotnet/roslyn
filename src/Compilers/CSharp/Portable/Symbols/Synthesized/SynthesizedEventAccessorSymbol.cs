// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Reflection;
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

        internal SynthesizedEventAccessorSymbol(SourceEventSymbol @event, bool isAdder, EventSymbol explicitlyImplementedEventOpt = null, string aliasQualifierOpt = null)
            : base(@event, null, @event.Location, explicitlyImplementedEventOpt, aliasQualifierOpt, isAdder, isIterator: false, isNullableAnalysisEnabled: false)
        {
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
            return OneOrMany.Create(this.AssociatedEvent.AttributeDeclarationSyntaxList);
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
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
    }
}
