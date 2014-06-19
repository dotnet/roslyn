using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Common.Semantics;
using Microsoft.CodeAnalysis.Common.Symbols;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Emit.NoPia
{
    internal abstract class EmbeddedMember
    {
        internal abstract EmbeddedTypesManager TypeManager { get; }
    }

    internal abstract class EmbeddedMember<T> : EmbeddedMember, Cci.IReference where T : Symbol
    {
        protected readonly T UnderlyingSymbol;
        private ReadOnlyArray<AttributeData> lazyAttributes;

        public EmbeddedMember(T underlyingSymbol)
        {
            Debug.Assert(underlyingSymbol.IsDefinition);
            this.UnderlyingSymbol = underlyingSymbol;
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(Context context)
        {
            if (lazyAttributes.IsNull)
            {
                var builder = ArrayBuilder<AttributeData>.GetInstance();
                var syntaxNodeOpt = (SyntaxNode)context.SyntaxNodeOpt;
                var diagnostics = context.Diagnostics;


                // Copy some of the attributes.

                // Note, when porting attributes, we are not using constructors from original symbol.
                // The constructors might be missing (for example, in metadata case) and doing lookup
                // will ensure that we report appropriate errors.

                foreach (var attrData in UnderlyingSymbol.GetCustomAttributesToEmit())
                {
                    if (attrData.IsTargetAttribute(UnderlyingSymbol, AttributeDescription.DispIdAttribute))
                    {
                        if (attrData.CommonConstructorArguments.Count == 1)
                        {
                            var ctor = TypeManager.GetWellKnownMethod(WellKnownMember.System_Runtime_InteropServices_DispIdAttribute__ctor, syntaxNodeOpt, diagnostics);

                            if ((object)ctor != null)
                            {
                                builder.Add(new SynthesizedAttributeData(ctor,
                                                                         attrData.CommonConstructorArguments,
                                                                         ReadOnlyArray<KeyValuePair<String, CommonTypedConstant>>.Empty));
                            }
                        }
                    }
                    else
                    {
                        PortAttributeIfNeedTo(attrData, builder, syntaxNodeOpt, diagnostics);
                    }
                }

                ReadOnlyInterlocked.CompareExchangeIfNull(ref lazyAttributes, builder.ToReadOnlyAndFree());
            }

            return lazyAttributes.AsEnumerable();
        }

        protected virtual void PortAttributeIfNeedTo(AttributeData attrData, ArrayBuilder<AttributeData> builder, SyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
        {
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            throw new NotImplementedException();
        }

        Cci.IDefinition Cci.IReference.AsDefinition(Context context)
        {
            throw new NotImplementedException();
        }
    }
}