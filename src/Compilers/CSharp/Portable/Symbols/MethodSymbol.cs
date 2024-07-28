// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a method or method-like symbol (including constructor,
    /// destructor, operator, or property/event accessor).
    /// </summary>
    internal abstract partial class MethodSymbol : Symbol, IMethodSymbolInternal
    {
        internal const MethodSymbol None = null;

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // Changes to the public interface of this class should remain synchronized with the VB version.
        // Do not make any changes to the public interface without making the corresponding change
        // to the VB version.
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        protected MethodSymbol()
        {
        }

        /// <summary>
        /// The original definition of this symbol. If this symbol is constructed from another
        /// symbol by type substitution then OriginalDefinition gets the original symbol as it was defined in
        /// source or metadata.
        /// </summary>
        public new virtual MethodSymbol OriginalDefinition
        {
            get
            {
                return this;
            }
        }

        protected sealed override Symbol OriginalSymbolDefinition
        {
            get
            {
                return this.OriginalDefinition;
            }
        }

        /// <summary>
        /// Gets what kind of method this is. There are several different kinds of things in the
        /// C# language that are represented as methods. This property allow distinguishing those things
        /// without having to decode the name of the method.
        /// </summary>
        public abstract MethodKind MethodKind
        {
            get;
        }

        /// <summary>
        /// Returns the arity of this method, or the number of type parameters it takes.
        /// A non-generic method has zero arity.
        /// </summary>
        public abstract int Arity { get; }

        /// <summary>
        /// Returns whether this method is generic; i.e., does it have any type parameters?
        /// </summary>
        public virtual bool IsGenericMethod
        {
            get
            {
                return this.Arity != 0;
            }
        }

        /// <summary>
        /// Returns true if this symbol requires an instance reference as the implicit receiver. This is false if the symbol is static, or a <see cref="LocalFunctionSymbol"/>
        /// </summary>
        public virtual bool RequiresInstanceReceiver => !IsStatic;

        /// <summary>
        /// True if the method itself is excluded from code coverage instrumentation.
        /// True for source methods marked with <see cref="AttributeDescription.ExcludeFromCodeCoverageAttribute"/>.
        /// </summary>
        internal virtual bool IsDirectlyExcludedFromCodeCoverage { get => false; }

        /// <summary>
        /// If a method is annotated with `[MemberNotNull(...)]` attributes, returns the list of members
        /// listed in those attributes.
        /// Otherwise, an empty array.
        /// </summary>
        internal virtual ImmutableArray<string> NotNullMembers => ImmutableArray<string>.Empty;

        internal virtual ImmutableArray<string> NotNullWhenTrueMembers => ImmutableArray<string>.Empty;

        internal virtual ImmutableArray<string> NotNullWhenFalseMembers => ImmutableArray<string>.Empty;

#nullable enable
        /// <summary>
        /// Returns the <see cref="UnmanagedCallersOnlyAttributeData"/> data for this method, if there is any. If forceComplete
        /// is false and the data has not yet been loaded or only early attribute binding has occurred, then either
        /// <see cref="UnmanagedCallersOnlyAttributeData.Uninitialized"/> or
        /// <see cref="UnmanagedCallersOnlyAttributeData.AttributePresentDataNotBound"/> will be returned, respectively.
        /// If passing true for forceComplete, ensure that cycles will not occur by not calling in the process of binding
        /// an attribute argument.
        /// </summary>
        internal abstract UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete);
#nullable disable

        /// <summary>
        /// Returns true if this method is an extension method.
        /// </summary>
        public abstract bool IsExtensionMethod { get; }

        /// <summary>
        /// True if this symbol has a special name (metadata flag SpecialName is set).
        /// </summary>
        internal abstract bool HasSpecialName { get; }

        /// <summary>
        /// Misc implementation metadata flags (ImplFlags in metadata).
        /// </summary>
        internal abstract MethodImplAttributes ImplementationAttributes { get; }

        /// <summary>
        /// True if the type has declarative security information (HasSecurity flags).
        /// </summary>
        internal abstract bool HasDeclarativeSecurity { get; }

        internal abstract bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument);

#nullable enable
        /// <summary>
        /// Platform invoke information, or null if the method isn't a P/Invoke.
        /// </summary>
        public abstract DllImportData? GetDllImportData();
#nullable disable

        /// <summary>
        /// Declaration security information associated with this type, or null if there is none.
        /// </summary>
        internal abstract IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation();

        /// <summary>
        /// Marshalling information for return value (FieldMarshal in metadata).
        /// </summary>
        internal abstract MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation { get; }

        /// <summary>
        /// True if the method calls another method containing security code (metadata flag RequiresSecurityObject is set).
        /// </summary>
        /// <remarks>
        /// A method can me marked as RequiresSecurityObject by applying the DynamicSecurityMethodAttribute in source.
        /// DynamicSecurityMethodAttribute is a pseudo custom attribute defined as an internal class in System.Security namespace.
        /// This attribute is set on certain security methods defined within mscorlib.
        /// </remarks>
        internal abstract bool RequiresSecurityObject { get; }

        // Note: This is no public "IsNew". This is intentional, because new has no syntactic meaning.

        /// <summary>
        /// Returns true if this method hides base methods by name. This cannot be specified directly
        /// in the C# language, but can be true for methods defined in other languages imported from
        /// metadata. The equivalent of the "hidebyname" flag in metadata.
        /// </summary>
        public abstract bool HidesBaseMethodsByName { get; }

        /// <summary>
        /// Returns whether this method is using CLI VARARG calling convention. This is used for C-style variable
        /// argument lists. This is used extremely rarely in C# code and is represented using the undocumented "__arglist" keyword.
        ///
        /// Note that methods with "params" on the last parameter are indicated with the "IsParams" property on ParameterSymbol, and
        /// are not represented with this property.
        /// </summary>
        public abstract bool IsVararg { get; }

        /// <summary>
        /// Returns whether this built-in operator checks for integer overflow.
        /// </summary>
        public virtual bool IsCheckedBuiltin
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if this method has no return type; i.e., returns "void".
        /// </summary>
        public abstract bool ReturnsVoid { get; }

        /// <summary>
        /// Source: Returns whether this method is async; i.e., does it have the async modifier?
        /// Metadata: Returns false; methods from metadata cannot be async.
        /// </summary>
        public abstract bool IsAsync { get; }

        /// <summary>
        /// Indicates whether or not the method returns by reference
        /// </summary>
        public bool ReturnsByRef
        {
            get
            {
                return this.RefKind == RefKind.Ref;
            }
        }

        /// <summary>
        /// Indicates whether or not the method returns by ref readonly
        /// </summary>
        public bool ReturnsByRefReadonly
        {
            get
            {
                Debug.Assert(this.RefKind != RefKind.Out);
                return this.RefKind == RefKind.RefReadOnly;
            }
        }

        /// <summary>
        /// Gets the ref kind of the method's return value
        /// </summary>
        public abstract RefKind RefKind { get; }

        /// <summary>
        /// Gets the return type of the method along with its annotations
        /// </summary>
        public abstract TypeWithAnnotations ReturnTypeWithAnnotations { get; }

        /// <summary>
        /// Gets the return type of the method
        /// </summary>
        public TypeSymbol ReturnType => ReturnTypeWithAnnotations.Type;

        public abstract FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations { get; }

        public abstract ImmutableHashSet<string> ReturnNotNullIfParameterNotNull { get; }

        /// <summary>
        /// Flow analysis annotations on the method itself (ie. DoesNotReturn)
        /// </summary>
        public abstract FlowAnalysisAnnotations FlowAnalysisAnnotations { get; }

        /// <summary>
        /// Returns the type arguments that have been substituted for the type parameters.
        /// If nothing has been substituted for a given type parameter,
        /// then the type parameter itself is consider the type argument.
        /// </summary>
        public abstract ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations { get; }

        /// <summary>
        /// Get the type parameters on this method. If the method has not generic,
        /// returns an empty list.
        /// </summary>
        public abstract ImmutableArray<TypeParameterSymbol> TypeParameters { get; }

        internal ImmutableArray<TypeWithAnnotations> GetTypeParametersAsTypeArguments()
        {
            return TypeMap.TypeParametersAsTypeSymbolsWithAnnotations(TypeParameters);
        }

        /// <summary>
        /// Call <see cref="TryGetThisParameter"/> and throw if it returns false.
        /// </summary>
        internal ParameterSymbol ThisParameter
        {
            get
            {
                ParameterSymbol thisParameter;
                if (!TryGetThisParameter(out thisParameter))
                {
                    throw ExceptionUtilities.Unreachable();
                }
                return thisParameter;
            }
        }

        /// <returns>
        /// True if this <see cref="MethodSymbol"/> type supports retrieving the this parameter
        /// and false otherwise.  Note that a return value of true does not guarantee a non-null
        /// <paramref name="thisParameter"/> (e.g. fails for static methods).
        /// </returns>
        internal virtual bool TryGetThisParameter(out ParameterSymbol thisParameter)
        {
            thisParameter = null;
            return false;
        }

        /// <summary>
        /// Optimization: in many cases, the parameter count (fast) is sufficient and we
        /// don't need the actual parameter symbols (slow).
        /// </summary>
        /// <remarks>
        /// The default implementation is always correct, but may be unnecessarily slow.
        /// </remarks>
        internal virtual int ParameterCount
        {
            get
            {
                return this.Parameters.Length;
            }
        }

        /// <summary>
        /// Gets the parameters of this method. If this method has no parameters, returns
        /// an empty list.
        /// </summary>
        public abstract ImmutableArray<ParameterSymbol> Parameters { get; }

        /// <summary>
        /// Returns the method symbol that this method was constructed from. The resulting
        /// method symbol
        /// has the same containing type (if any), but has type arguments that are the same
        /// as the type parameters (although its containing type might not).
        /// </summary>
        public virtual MethodSymbol ConstructedFrom
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Source: Was the member name qualified with a type name?
        /// Metadata: Is the member an explicit implementation?
        /// </summary>
        /// <remarks>
        /// Will not always agree with ExplicitInterfaceImplementations.Any()
        /// (e.g. if binding of the type part of the name fails).
        /// </remarks>
        internal virtual bool IsExplicitInterfaceImplementation
        {
            get { return ExplicitInterfaceImplementations.Any(); }
        }

        /// <summary>
        /// Indicates whether the method is declared readonly, i.e.
        /// whether the 'this' receiver parameter is 'ref readonly'.
        /// See also <see cref="IsEffectivelyReadOnly"/>
        /// </summary>
        internal abstract bool IsDeclaredReadOnly { get; }

        /// <summary>
        /// Indicates whether the accessor is marked with the 'init' modifier.
        /// </summary>
        internal abstract bool IsInitOnly { get; }

        /// <summary>
        /// Indicates whether the method is effectively readonly,
        /// by either the method or the containing type being marked readonly.
        /// </summary>
        internal virtual bool IsEffectivelyReadOnly => (IsDeclaredReadOnly || ContainingType?.IsReadOnly == true) && IsValidReadOnlyTarget;

        protected bool IsValidReadOnlyTarget => !IsStatic && ContainingType.IsStructType() && MethodKind != MethodKind.Constructor && !IsInitOnly;

        /// <summary>
        /// Returns interface methods explicitly implemented by this method.
        /// </summary>
        /// <remarks>
        /// Methods imported from metadata can explicitly implement more than one method,
        /// that is why return type is ImmutableArray.
        /// </remarks>
        public abstract ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations { get; }

        /// <summary>
        /// Custom modifiers associated with the ref modifier, or an empty array if there are none.
        /// </summary>
        public abstract ImmutableArray<CustomModifier> RefCustomModifiers { get; }

        /// <summary>
        /// Gets the attributes on method's return type.
        /// Returns an empty array if there are no attributes.
        /// </summary>
        public virtual ImmutableArray<CSharpAttributeData> GetReturnTypeAttributes()
        {
            Debug.Assert(!(this is IAttributeTargetSymbol)); //such types must override

            // Return an empty array by default.
            // Sub-classes that can have return type attributes must
            // override this method
            return ImmutableArray<CSharpAttributeData>.Empty;
        }

        /// <summary>
        /// If this method has MethodKind of MethodKind.PropertyGet or MethodKind.PropertySet,
        /// returns the property that this method is the getter or setter for.
        /// If this method has MethodKind of MethodKind.EventAdd or MethodKind.EventRemove,
        /// returns the event that this method is the adder or remover for.
        /// Note, the set of possible associated symbols might be expanded in the future to
        /// reflect changes in the languages.
        /// </summary>
        public abstract Symbol AssociatedSymbol { get; }

        /// <summary>
        /// Returns the original virtual or abstract method which a given method symbol overrides,
        /// ignoring any other overriding methods in base classes.
        /// </summary>
        /// <param name="accessingTypeOpt">The search must respect accessibility from this type.</param>
        internal MethodSymbol GetLeastOverriddenMethod(NamedTypeSymbol accessingTypeOpt)
        {
            return GetLeastOverriddenMethodCore(accessingTypeOpt, requireSameReturnType: false);
        }

        /// <summary>
        /// Returns the original virtual or abstract method which a given method symbol overrides,
        /// ignoring any other overriding methods in base classes.
        /// </summary>
        /// <param name="accessingTypeOpt">The search must respect accessibility from this type.</param>
        /// <param name="requireSameReturnType">The returned method must have the same return type.</param>
        private MethodSymbol GetLeastOverriddenMethodCore(NamedTypeSymbol accessingTypeOpt, bool requireSameReturnType)
        {
            accessingTypeOpt = accessingTypeOpt?.OriginalDefinition;
            MethodSymbol m = this;
            while (m.IsOverride && !m.HidesBaseMethodsByName)
            {
                // We might not be able to access the overridden method. For example,
                //
                //   .assembly A
                //   {
                //      InternalsVisibleTo("B")
                //      public class A { internal virtual void M() { } }
                //   }
                //
                //   .assembly B
                //   {
                //      InternalsVisibleTo("C")
                //      public class B : A { internal override void M() { } }
                //   }
                //
                //   .assembly C
                //   {
                //      public class C : B { ... new B().M ... }       // A.M is not accessible from here
                //   }
                //
                // See InternalsVisibleToAndStrongNameTests: IvtVirtualCall1, IvtVirtualCall2, IvtVirtual_ParamsAndDynamic.
                MethodSymbol overridden = m.OverriddenMethod;
                var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                if ((object)overridden == null ||
                    (accessingTypeOpt is { } && !AccessCheck.IsSymbolAccessible(overridden, accessingTypeOpt, ref discardedUseSiteInfo)) ||
                    (requireSameReturnType && !this.ReturnType.Equals(overridden.ReturnType, TypeCompareKind.AllIgnoreOptions)))
                {
                    break;
                }

                m = overridden;
            }

            return m;
        }

        /// <summary>
        /// Returns the original virtual or abstract method which a given method symbol overrides,
        /// ignoring any other overriding methods in base classes.
        /// Also, if the given method symbol is generic then the resulting virtual or abstract method is constructed with the
        /// same type arguments as the given method.
        /// </summary>
        /// <param name="requireSameReturnType">The returned method must have the same return type.</param>
        internal MethodSymbol GetConstructedLeastOverriddenMethod(NamedTypeSymbol accessingTypeOpt, bool requireSameReturnType)
        {
            var m = this.ConstructedFrom.GetLeastOverriddenMethodCore(accessingTypeOpt, requireSameReturnType);
            return m.IsGenericMethod ? m.Construct(this.TypeArgumentsWithAnnotations) : m;
        }

        /// <summary>
        /// If this method overrides another method (because it both had the override modifier
        /// and there correctly was a method to override), returns the overridden method.
        /// Note that if an overriding method D.M overrides C.M, which in turn overrides
        /// virtual method A.M, the "overridden method" of D.M is C.M, not the original virtual
        /// method A.M. Note also that constructed generic methods are not considered to
        /// override anything.
        /// </summary>
        public MethodSymbol OverriddenMethod
        {
            get
            {
                if (this.IsOverride && ReferenceEquals(this.ConstructedFrom, this))
                {
                    if (IsDefinition)
                    {
                        return (MethodSymbol)OverriddenOrHiddenMembers.GetOverriddenMember();
                    }

                    return (MethodSymbol)OverriddenOrHiddenMembersResult.GetOverriddenMember(this, OriginalDefinition.OverriddenMethod);
                }

                return null;
            }
        }

        /// <summary>
        /// Returns true if calls to this method are omitted in this syntax tree. Calls are omitted
        /// when the called method is a partial method with no implementation part, or when the
        /// called method is a conditional method whose condition is not true in the source file
        /// corresponding to the given syntax tree.
        /// </summary>
        internal virtual bool CallsAreOmitted(SyntaxTree syntaxTree)
        {
            return syntaxTree != null && this.CallsAreConditionallyOmitted(syntaxTree);
        }

        /// <summary>
        /// Calls are conditionally omitted if both the following requirements are true:
        ///  (a) IsConditional == true, i.e. it has at least one applied/inherited conditional attribute AND
        ///  (b) None of conditional symbols corresponding to these conditional attributes are defined in the given syntaxTree.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        private bool CallsAreConditionallyOmitted(SyntaxTree syntaxTree)
        {
            if (this.IsConditional)
            {
                ImmutableArray<string> conditionalSymbols = this.GetAppliedConditionalSymbols();
                Debug.Assert(conditionalSymbols != null);
                if (syntaxTree.IsAnyPreprocessorSymbolDefined(conditionalSymbols))
                {
                    return false;
                }

                if (this.IsOverride)
                {
                    var overriddenMethod = this.OverriddenMethod;
                    if ((object)overriddenMethod != null && overriddenMethod.IsConditional)
                    {
                        return overriddenMethod.CallsAreConditionallyOmitted(syntaxTree);
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a sequence of preprocessor symbols specified in <see cref="ConditionalAttribute"/> applied on this symbol, or null if there are none.
        /// </summary>
        internal abstract ImmutableArray<string> GetAppliedConditionalSymbols();

        /// <summary>
        /// Returns a flag indicating whether this symbol has at least one applied/inherited conditional attribute.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        public bool IsConditional
        {
            get
            {
                if (this.GetAppliedConditionalSymbols().Any())
                {
                    return true;
                }

                // Conditional attributes are inherited by overriding methods.
                if (this.IsOverride)
                {
                    var overriddenMethod = this.OverriddenMethod;
                    if ((object)overriddenMethod != null)
                    {
                        return overriddenMethod.IsConditional;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Returns true if this is a constructor attributed with HasSetsRequiredMembers
        /// </summary>
        internal bool HasSetsRequiredMembers => MethodKind == MethodKind.Constructor && HasSetsRequiredMembersImpl;

        protected abstract bool HasSetsRequiredMembersImpl { get; }

        internal abstract bool HasUnscopedRefAttribute { get; }

        internal abstract bool UseUpdatedEscapeRules { get; }

        /// <summary>
        /// Some method kinds do not participate in overriding/hiding (e.g. constructors).
        /// </summary>
        internal static bool CanOverrideOrHide(MethodKind kind)
        {
            switch (kind)
            {
                case MethodKind.AnonymousFunction:
                case MethodKind.Constructor:
                case MethodKind.Destructor:
                case MethodKind.ExplicitInterfaceImplementation:
                case MethodKind.StaticConstructor:
                case MethodKind.ReducedExtension:
                    return false;
                case MethodKind.Conversion:
                case MethodKind.DelegateInvoke:
                case MethodKind.EventAdd:
                case MethodKind.EventRemove:
                case MethodKind.LocalFunction:
                case MethodKind.UserDefinedOperator:
                case MethodKind.Ordinary:
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                    return true;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        internal virtual OverriddenOrHiddenMembersResult OverriddenOrHiddenMembers
        {
            get
            {
                // To save space, the default implementation does not cache its result.  We expect there to
                // be a very large number of MethodSymbols and we expect that a large percentage of them will
                // obviously not override anything (e.g. static methods, constructors, destructors, etc).
                return this.MakeOverriddenOrHiddenMembers();
            }
        }

        /// <summary>
        /// Returns value 'Method' of the <see cref="SymbolKind"/>
        /// </summary>
        public sealed override SymbolKind Kind
        {
            get
            {
                return SymbolKind.Method;
            }
        }

        /// <summary>
        /// Returns true if this symbol represents a constructor of a script class.
        /// </summary>
        internal bool IsScriptConstructor
        {
            get
            {
                return MethodKind == MethodKind.Constructor && ContainingType.IsScriptClass;
            }
        }

        internal virtual bool IsScriptInitializer
        {
            get { return false; }
        }

        /// <summary>
        /// Returns if the method is implicit constructor (normal and static)
        /// </summary>
        internal bool IsImplicitConstructor
        {
            get
            {
                return ((MethodKind == MethodKind.Constructor || MethodKind == MethodKind.StaticConstructor) && IsImplicitlyDeclared);
            }
        }

        /// <summary>
        /// Returns if the method is implicit instance constructor
        /// </summary>
        internal bool IsImplicitInstanceConstructor
        {
            get
            {
                return MethodKind == MethodKind.Constructor && IsImplicitlyDeclared;
            }
        }

        /// <summary>
        /// Returns true if this symbol represents a constructor of an interactive submission class.
        /// </summary>
        internal bool IsSubmissionConstructor
        {
            get
            {
                return IsScriptConstructor && ContainingAssembly.IsInteractive;
            }
        }

        internal bool IsSubmissionInitializer
        {
            get
            {
                return IsScriptInitializer && ContainingAssembly.IsInteractive;
            }
        }

        /// <summary>
        /// Determines whether this method is a candidate for a default assembly entry point
        /// (i.e. it is a static method called "Main").
        /// </summary>
        internal bool IsEntryPointCandidate
        {
            get
            {
                if (this.IsPartialDefinition() &&
                    this.PartialImplementationPart is null)
                {
                    return false;
                }

                return IsStatic && !IsAbstract && !IsVirtual && Name == WellKnownMemberNames.EntryPointMethodName;
            }
        }

        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitMethod(this, argument);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitMethod(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitMethod(this);
        }

        public MethodSymbol ReduceExtensionMethod(TypeSymbol receiverType, CSharpCompilation compilation)
        {
            return ReduceExtensionMethod(receiverType, compilation, wasFullyInferred: out _);
        }

        /// <summary>
        /// If this is an extension method that can be applied to a receiver of the given type,
        /// returns a reduced extension method symbol thus formed. Otherwise, returns null.
        /// </summary>
        /// <param name="compilation">The compilation in which constraints should be checked.
        /// Should not be null, but if it is null we treat constraints as we would in the latest
        /// language version.</param>
        public MethodSymbol ReduceExtensionMethod(TypeSymbol receiverType, CSharpCompilation compilation, out bool wasFullyInferred)
        {
            if ((object)receiverType == null)
            {
                throw new ArgumentNullException(nameof(receiverType));
            }

            if (!this.IsExtensionMethod || this.MethodKind == MethodKind.ReducedExtension || receiverType.IsVoidType())
            {
                wasFullyInferred = false;
                return null;
            }

            return ReducedExtensionMethodSymbol.Create(this, receiverType, compilation, out wasFullyInferred);
        }

        /// <summary>
        /// If this is an extension method, returns a reduced extension method
        /// symbol representing the method. Otherwise, returns null.
        /// </summary>
        public MethodSymbol ReduceExtensionMethod()
        {
            return (this.IsExtensionMethod && this.MethodKind != MethodKind.ReducedExtension) ? ReducedExtensionMethodSymbol.Create(this) : null;
        }

        /// <summary>
        /// If this method is a reduced extension method, returns the extension method that
        /// should be used at call site during ILGen. Otherwise, returns null.
        /// </summary>
        internal virtual MethodSymbol CallsiteReducedFromMethod
        {
            get { return null; }
        }

        /// <summary>
        /// If this is a partial method declaration without a body, and the method also
        /// has a part that implements it with a body, returns that implementing
        /// definition.  Otherwise null.
        /// </summary>
        public virtual MethodSymbol PartialImplementationPart
        {
            get { return null; }
        }

        /// <summary>
        /// If this is a partial method with a body, returns the corresponding
        /// definition part (without a body).  Otherwise null.
        /// </summary>
        public virtual MethodSymbol PartialDefinitionPart
        {
            get { return null; }
        }

        /// <summary>
        /// If this method is a reduced extension method, gets the extension method definition that
        /// this method was reduced from. Otherwise, returns null.
        /// </summary>
        public virtual MethodSymbol ReducedFrom
        {
            get { return null; }
        }

        /// <summary>
        /// If this method can be applied to an object, returns the type of object it is applied to.
        /// </summary>
        public virtual TypeSymbol ReceiverType
        {
            get
            {
                return this.ContainingType;
            }
        }

        /// <summary>
        /// If this method is a reduced extension method, returns a type inferred during reduction process for the type parameter.
        /// </summary>
        /// <param name="reducedFromTypeParameter">Type parameter of the corresponding <see cref="ReducedFrom"/> method.</param>
        /// <returns>Inferred type or Nothing if nothing was inferred.</returns>
        /// <exception cref="System.InvalidOperationException">If this is not a reduced extension method.</exception>
        /// <exception cref="System.ArgumentNullException">If <paramref name="reducedFromTypeParameter"/> is null.</exception>
        /// <exception cref="System.ArgumentException">If <paramref name="reducedFromTypeParameter"/> doesn't belong to the corresponding <see cref="ReducedFrom"/> method.</exception>
        public virtual TypeSymbol GetTypeInferredDuringReduction(TypeParameterSymbol reducedFromTypeParameter)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Apply type substitution to a generic method to create a method symbol with the given type parameters supplied.
        /// </summary>
        /// <param name="typeArguments"></param>
        /// <returns></returns>
        public MethodSymbol Construct(params TypeSymbol[] typeArguments)
        {
            return this.Construct(ImmutableArray.Create(typeArguments));
        }

        // https://github.com/dotnet/roslyn/issues/30071: Replace with Construct(ImmutableArray<TypeWithAnnotations>).
        /// <summary>
        /// Apply type substitution to a generic method to create a method symbol with the given type parameters supplied.
        /// </summary>
        /// <param name="typeArguments"></param>
        /// <returns></returns>
        public MethodSymbol Construct(ImmutableArray<TypeSymbol> typeArguments)
        {
            return Construct(typeArguments.SelectAsArray(a => TypeWithAnnotations.Create(a)));
        }

        internal MethodSymbol Construct(ImmutableArray<TypeWithAnnotations> typeArguments)
        {
            if (!ReferenceEquals(this, ConstructedFrom) || this.Arity == 0)
            {
                throw new InvalidOperationException();
            }

            if (typeArguments.IsDefault)
            {
                throw new ArgumentNullException(nameof(typeArguments));
            }

            if (typeArguments.Any(NamedTypeSymbol.TypeWithAnnotationsIsNullFunction))
            {
                throw new ArgumentException(CSharpResources.TypeArgumentCannotBeNull, nameof(typeArguments));
            }

            if (typeArguments.Length != this.Arity)
            {
                throw new ArgumentException(CSharpResources.WrongNumberOfTypeArguments, nameof(typeArguments));
            }

            if (ConstructedNamedTypeSymbol.TypeParametersMatchTypeArguments(this.TypeParameters, typeArguments))
            {
                return this;
            }

            return new ConstructedMethodSymbol(this, typeArguments);
        }

        internal MethodSymbol AsMember(NamedTypeSymbol newOwner)
        {
            Debug.Assert(this.IsDefinition);
            Debug.Assert(ReferenceEquals(newOwner.OriginalDefinition, this.ContainingSymbol.OriginalDefinition));
            return newOwner.IsDefinition ? this : new SubstitutedMethodSymbol(newOwner, this);
        }

        /// <summary>
        /// As a performance optimization, cache parameter types and refkinds - overload resolution uses them a lot.
        /// </summary>
        private ParameterSignature _lazyParameterSignature;
        internal ImmutableArray<TypeWithAnnotations> ParameterTypesWithAnnotations
        {
            get
            {
                ParameterSignature.PopulateParameterSignature(this.Parameters, ref _lazyParameterSignature);
                return _lazyParameterSignature.parameterTypesWithAnnotations;
            }
        }
        internal TypeSymbol GetParameterType(int index) => ParameterTypesWithAnnotations[index].Type;

        /// <summary>
        /// Null if no parameter is ref/out. Otherwise the RefKind for each parameter.
        /// </summary>
        internal ImmutableArray<RefKind> ParameterRefKinds
        {
            get
            {
                ParameterSignature.PopulateParameterSignature(this.Parameters, ref _lazyParameterSignature);
                return _lazyParameterSignature.parameterRefKinds;
            }
        }

        internal abstract Microsoft.Cci.CallingConvention CallingConvention
        {
            get;
        }

        internal virtual ImmutableArray<NamedTypeSymbol> UnmanagedCallingConventionTypes => ImmutableArray<NamedTypeSymbol>.Empty;

        /// <summary>
        /// Returns the map from type parameters to type arguments.
        /// If this is not a generic method instantiation, returns null.
        /// The map targets the original definition of the method.
        /// </summary>
        internal virtual TypeMap TypeSubstitution
        {
            get { return null; }
        }

        #region Use-Site Diagnostics

        internal override UseSiteInfo<AssemblySymbol> GetUseSiteInfo()
        {
            if (this.IsDefinition)
            {
                return new UseSiteInfo<AssemblySymbol>(PrimaryDependency);
            }

            // There is no reason to specially check type arguments because
            // constructed members are never imported.
            return this.OriginalDefinition.GetUseSiteInfo();
        }

        internal bool CalculateUseSiteDiagnostic(ref UseSiteInfo<AssemblySymbol> result)
        {
            Debug.Assert(this.IsDefinition);

            // Check return type, custom modifiers, parameters
            if (DeriveUseSiteInfoFromType(ref result, this.ReturnTypeWithAnnotations,
                                                IsInitOnly ?
                                                    AllowedRequiredModifierType.System_Runtime_CompilerServices_IsExternalInit :
                                                    AllowedRequiredModifierType.None) ||
                DeriveUseSiteInfoFromCustomModifiers(ref result, this.RefCustomModifiers, AllowedRequiredModifierType.System_Runtime_InteropServices_InAttribute) ||
                DeriveUseSiteInfoFromParameters(ref result, this.Parameters))
            {
                return true;
            }

            // If the member is in an assembly with unified references,
            // we check if its definition depends on a type from a unified reference.
            if (this.ContainingModule?.HasUnifiedReferences == true)
            {
                HashSet<TypeSymbol> unificationCheckedTypes = null;
                DiagnosticInfo diagnosticInfo = result.DiagnosticInfo;

                if (this.ReturnTypeWithAnnotations.GetUnificationUseSiteDiagnosticRecursive(ref diagnosticInfo, this, ref unificationCheckedTypes) ||
                    GetUnificationUseSiteDiagnosticRecursive(ref diagnosticInfo, this.RefCustomModifiers, this, ref unificationCheckedTypes) ||
                    GetUnificationUseSiteDiagnosticRecursive(ref diagnosticInfo, this.Parameters, this, ref unificationCheckedTypes) ||
                    GetUnificationUseSiteDiagnosticRecursive(ref diagnosticInfo, this.TypeParameters, this, ref unificationCheckedTypes))
                {
                    result = result.AdjustDiagnosticInfo(diagnosticInfo);
                    return true;
                }

                result = result.AdjustDiagnosticInfo(diagnosticInfo);
            }

            return false;
        }

#nullable enable
        internal static (bool IsCallConvs, ImmutableHashSet<INamedTypeSymbolInternal>? CallConvs) TryDecodeUnmanagedCallersOnlyCallConvsField(
            string key,
            TypedConstant value,
            bool isField,
            Location? location,
            BindingDiagnosticBag? diagnostics)
        {
            ImmutableHashSet<INamedTypeSymbolInternal>? callingConventionTypes = null;

            if (!UnmanagedCallersOnlyAttributeData.IsCallConvsTypedConstant(key, isField, in value))
            {
                return (false, callingConventionTypes);
            }

            if (value.Values.IsDefaultOrEmpty)
            {
                callingConventionTypes = ImmutableHashSet<INamedTypeSymbolInternal>.Empty;
                return (true, callingConventionTypes);
            }

            var builder = PooledHashSet<INamedTypeSymbolInternal>.GetInstance();
            foreach (var callConvTypedConstant in value.Values)
            {
                Debug.Assert(callConvTypedConstant.Kind == TypedConstantKind.Type);
                if (!(callConvTypedConstant.ValueInternal is NamedTypeSymbol callConvType)
                    || !FunctionPointerTypeSymbol.IsCallingConventionModifier(callConvType))
                {
                    // `{0}` is not a valid calling convention type for 'UnmanagedCallersOnly'.
                    diagnostics?.Add(ErrorCode.ERR_InvalidUnmanagedCallersOnlyCallConv, location!, callConvTypedConstant.ValueInternal ?? "null");
                }
                else
                {
                    _ = builder.Add(callConvType);
                }

            }
            callingConventionTypes = builder.ToImmutableHashSet();
            builder.Free();

            return (true, callingConventionTypes);
        }

        /// <summary>
        /// Determines if this method is a valid target for UnmanagedCallersOnly, reporting an error in the given diagnostic
        /// bag if it is not null. <paramref name="node"/> and <paramref name="diagnostics"/> should both be null, or 
        /// neither should be null. If an error would be reported (whether or not diagnostics is null), true is returned.
        /// </summary>
        internal bool CheckAndReportValidUnmanagedCallersOnlyTarget(SyntaxNode? node, BindingDiagnosticBag? diagnostics)
        {
            Debug.Assert((node == null) == (diagnostics == null));

            if (!IsStatic || IsAbstract || IsVirtual || MethodKind is not (MethodKind.Ordinary or MethodKind.LocalFunction))
            {
                // `UnmanagedCallersOnly` can only be applied to ordinary static methods or local functions.
                diagnostics?.Add(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, node!.Location);
                return true;
            }

            if (isGenericMethod(this) || ContainingType.IsGenericType)
            {
                diagnostics?.Add(ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric, node!.Location);
                return true;
            }

            return false;

            static bool isGenericMethod([DisallowNull] MethodSymbol? method)
            {
                do
                {
                    if (method.IsGenericMethod)
                    {
                        return true;
                    }

                    method = method.ContainingSymbol as MethodSymbol;
                } while (method is not null);

                return false;
            }
        }
#nullable disable

        /// <summary>
        /// Returns true if the error code is highest priority while calculating use site error for this symbol.
        /// </summary>
        protected sealed override bool IsHighestPriorityUseSiteErrorCode(int code) => code is (int)ErrorCode.ERR_UnsupportedCompilerFeature or (int)ErrorCode.ERR_BindToBogus;

        public sealed override bool HasUnsupportedMetadata
        {
            get
            {
                DiagnosticInfo info = GetUseSiteInfo().DiagnosticInfo;
                return (object)info != null && info.Code is (int)ErrorCode.ERR_BindToBogus or (int)ErrorCode.ERR_UnsupportedCompilerFeature;
            }
        }

        #endregion

        internal virtual bool IsIterator
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// If the method was written as an iterator method (i.e. with yield statements in its body) returns the
        /// element type of the iterator.  Otherwise returns default(TypeWithAnnotations).
        /// </summary>
        internal virtual TypeWithAnnotations IteratorElementTypeWithAnnotations
        {
            get { return default; }
        }

        /// <summary>
        /// Generates bound block representing method's body for methods in lowered form and adds it to
        /// a collection of method bodies of the current module. This method is supposed to only be
        /// called for method symbols which return SynthesizesLoweredBoundBody == true.
        /// </summary>
        internal virtual void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            throw ExceptionUtilities.Unreachable();
        }

        /// <summary>
        /// Returns true for synthesized symbols which generate synthesized body in lowered form
        /// </summary>
        internal virtual bool SynthesizesLoweredBoundBody
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Return true iff the method contains user code.
        /// </summary>
        internal abstract bool GenerateDebugInfo { get; }

        /// <summary>
        /// Calculates a syntax offset for a local (user-defined or long-lived synthesized) declared at <paramref name="localPosition"/>.
        /// Must be implemented by all methods that may contain user code.
        /// </summary>
        /// <remarks>
        /// Syntax offset is a unique identifier for the local within the emitted method body.
        /// It's based on position of the local declarator. In single-part method bodies it's simply the distance
        /// from the start of the method body syntax span. If a method body has multiple parts (such as a constructor
        /// comprising of code for member initializers and constructor initializer calls) the offset is calculated
        /// as if all source these parts were concatenated together and prepended to the constructor body.
        /// The resulting syntax offset is then negative for locals defined outside of the constructor body.
        /// </remarks>
        internal abstract int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree);

        internal virtual CodeAnalysis.NullableAnnotation ReceiverNullableAnnotation =>
            RequiresInstanceReceiver ? CodeAnalysis.NullableAnnotation.NotAnnotated : CodeAnalysis.NullableAnnotation.None;

        /// <summary>
        /// Build and add synthesized return type attributes for this method symbol.
        /// </summary>
        internal virtual void AddSynthesizedReturnTypeAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            if (this.ReturnsByRefReadonly)
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsReadOnlyAttribute(this));
            }

            var compilation = this.DeclaringCompilation;
            var type = this.ReturnTypeWithAnnotations;

            if (type.Type.ContainsDynamic() && compilation.HasDynamicEmitAttributes(BindingDiagnosticBag.Discarded, Location.None))
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(type.Type, type.CustomModifiers.Length + this.RefCustomModifiers.Length, this.RefKind));
            }

            if (compilation.ShouldEmitNativeIntegerAttributes(type.Type))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNativeIntegerAttribute(this, type.Type));
            }

            if (type.Type.ContainsTupleNames() && compilation.HasTupleNamesAttributes(BindingDiagnosticBag.Discarded, Location.None))
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeTupleNamesAttribute(type.Type));
            }

            if (compilation.ShouldEmitNullableAttributes(this))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNullableAttributeIfNecessary(this, GetNullableContextValue(), type));
            }
        }

        /// <summary>
        /// Returns true if locals are to be initialized
        /// </summary>
        public abstract bool AreLocalsZeroed { get; }

        internal abstract bool IsNullableAnalysisEnabled();

        /// <summary>
        /// Gets the resolution priority of this method, 0 if not set.
        /// </summary>
        /// <remarks>
        /// Do not call this method from early attribute binding, cycles will occur.
        /// </remarks>
        internal int OverloadResolutionPriority => CanHaveOverloadResolutionPriority ? (TryGetOverloadResolutionPriority() ?? 0) : 0;

        internal abstract int? TryGetOverloadResolutionPriority();

        internal bool CanHaveOverloadResolutionPriority =>
            MethodKind is MethodKind.Ordinary
                       or MethodKind.Constructor
                       or MethodKind.UserDefinedOperator
                       or MethodKind.ReducedExtension
            && !IsOverride;

        #region IMethodSymbolInternal

        bool IMethodSymbolInternal.HasDeclarativeSecurity => HasDeclarativeSecurity;
        bool IMethodSymbolInternal.IsAccessCheckedOnOverride => IsAccessCheckedOnOverride;
        bool IMethodSymbolInternal.IsExternal => IsExternal;
        bool IMethodSymbolInternal.IsHiddenBySignature => !HidesBaseMethodsByName;
        bool IMethodSymbolInternal.IsMetadataNewSlot => IsMetadataNewSlot();
        bool IMethodSymbolInternal.IsPlatformInvoke => GetDllImportData() != null;
        bool IMethodSymbolInternal.HasRuntimeSpecialName => HasRuntimeSpecialName;
        bool IMethodSymbolInternal.IsMetadataFinal => IsSealed;
        bool IMethodSymbolInternal.HasSpecialName => HasSpecialName;
        bool IMethodSymbolInternal.RequiresSecurityObject => RequiresSecurityObject;
        MethodImplAttributes IMethodSymbolInternal.ImplementationAttributes => ImplementationAttributes;
        bool IMethodSymbolInternal.IsIterator => IsIterator;
        ISymbolInternal IMethodSymbolInternal.AssociatedSymbol => AssociatedSymbol;
        IMethodSymbolInternal IMethodSymbolInternal.PartialImplementationPart => PartialImplementationPart;
        IMethodSymbolInternal IMethodSymbolInternal.PartialDefinitionPart => PartialDefinitionPart;

        /// <summary>
        /// Gets the handle for the signature of this method as it appears in metadata. 
        /// Nil handle for symbols not loaded from metadata, or if the metadata is invalid.
        /// </summary>
        public virtual BlobHandle MetadataSignatureHandle => default;

        int IMethodSymbolInternal.ParameterCount => ParameterCount;

        ImmutableArray<IParameterSymbolInternal> IMethodSymbolInternal.Parameters => Parameters.Cast<ParameterSymbol, IParameterSymbolInternal>();

        int IMethodSymbolInternal.CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => CalculateLocalSyntaxOffset(localPosition, localTree);

        IMethodSymbolInternal IMethodSymbolInternal.Construct(params ITypeSymbolInternal[] typeArguments)
        {
            return Construct((TypeSymbol[])typeArguments);
        }

        #endregion

        protected sealed override ISymbol CreateISymbol()
        {
            return new PublicModel.MethodSymbol(this);
        }

        public override bool Equals(Symbol other, TypeCompareKind compareKind)
        {
            if (other is SubstitutedMethodSymbol sms)
            {
                return sms.Equals(this, compareKind);
            }

            if (other is NativeIntegerMethodSymbol nms)
            {
                return nms.Equals(this, compareKind);
            }

            return base.Equals(other, compareKind);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

#nullable enable
        protected static void AddRequiredMembersMarkerAttributes(ref ArrayBuilder<SynthesizedAttributeData> attributes, MethodSymbol methodToAttribute)
        {
            if (methodToAttribute.ShouldCheckRequiredMembers() && methodToAttribute.ContainingType.HasAnyRequiredMembers)
            {
                var obsoleteData = methodToAttribute.ObsoleteAttributeData;
                Debug.Assert(obsoleteData != ObsoleteAttributeData.Uninitialized, "getting synthesized attributes before attributes are decoded");

                CSharpCompilation declaringCompilation = methodToAttribute.DeclaringCompilation;
                if (obsoleteData == null)
                {
                    AddSynthesizedAttribute(ref attributes, declaringCompilation.TrySynthesizeAttribute(WellKnownMember.System_ObsoleteAttribute__ctor,
                        ImmutableArray.Create(
                            new TypedConstant(declaringCompilation.GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, PEModule.RequiredMembersMarker), // message
                            new TypedConstant(declaringCompilation.GetSpecialType(SpecialType.System_Boolean), TypedConstantKind.Primitive, true)) // error
                        ));
                }

                AddSynthesizedAttribute(ref attributes, declaringCompilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute__ctor,
                    ImmutableArray.Create(new TypedConstant(declaringCompilation.GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, nameof(CompilerFeatureRequiredFeatures.RequiredMembers)))
                    ));
            }
        }
    }
}
