// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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

        protected override sealed Symbol OriginalSymbolDefinition
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
        internal abstract System.Reflection.MethodImplAttributes ImplementationAttributes { get; }

        /// <summary>
        /// True if the type has declarative security information (HasSecurity flags).
        /// </summary>
        internal abstract bool HasDeclarativeSecurity { get; }

        /// <summary>
        /// Platform invoke information, or null if the method isn't a P/Invoke.
        /// </summary>
        public abstract DllImportData GetDllImportData();

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
        /// Gets the return type of the method
        /// </summary>
        public abstract TypeSymbolWithAnnotations ReturnType { get; }

        /// <summary>
        /// Returns the type arguments that have been substituted for the type parameters. 
        /// If nothing has been substituted for a given type parameter,
        /// then the type parameter itself is consider the type argument.
        /// </summary>
        public abstract ImmutableArray<TypeSymbolWithAnnotations> TypeArguments { get; }

        /// <summary>
        /// Get the type parameters on this method. If the method has not generic,
        /// returns an empty list.
        /// </summary>
        public abstract ImmutableArray<TypeParameterSymbol> TypeParameters { get; }

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
                    throw ExceptionUtilities.Unreachable;
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
        /// Returns interface methods explicitly implemented by this method.
        /// </summary>
        /// <remarks>
        /// Methods imported from metadata can explicitly implement more than one method, 
        /// that is why return type is ImmutableArray.
        /// </remarks>
        public abstract ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations { get; }

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
            var accessingType = ((object)accessingTypeOpt == null ? this.ContainingType : accessingTypeOpt).OriginalDefinition;

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
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                if ((object)overridden == null || !AccessCheck.IsSymbolAccessible(overridden, accessingType, ref useSiteDiagnostics))
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
        internal MethodSymbol GetConstructedLeastOverriddenMethod(NamedTypeSymbol accessingTypeOpt)
        {
            var m = this.ConstructedFrom.GetLeastOverriddenMethod(accessingTypeOpt);
            return m.IsGenericMethod ? m.Construct(this.TypeArguments) : m;
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
        internal bool IsConditional
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
            get { return IsStatic && Name == WellKnownMemberNames.EntryPointMethodName; }
        }

        /// <summary>
        /// Checks if the method has an entry point compatible signature, i.e.
        /// - the return type is either void or int
        /// - has either no parameter or a single parameter of type string[]
        /// </summary>
        internal bool HasEntryPointSignature()
        {
            if (this.IsVararg)
            {
                return false;
            }

            TypeSymbol returnType = ReturnType.TypeSymbol;
            if (returnType.SpecialType != SpecialType.System_Int32 && returnType.SpecialType != SpecialType.System_Void)
            {
                return false;
            }

            if (Parameters.Length == 0)
            {
                return true;
            }

            if (Parameters.Length > 1)
            {
                return false;
            }

            if (!ParameterRefKinds.IsDefault)
            {
                return false;
            }

            var firstType = Parameters[0].Type.TypeSymbol;
            if (firstType.TypeKind != TypeKind.Array)
            {
                return false;
            }

            var array = (ArrayTypeSymbol)firstType;
            return array.IsSZArray && array.ElementType.SpecialType == SpecialType.System_String;
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

        /// <summary>
        /// If this is an extension method that can be applied to a receiver of the given type,
        /// returns a reduced extension method symbol thus formed. Otherwise, returns null.
        /// </summary>
        public MethodSymbol ReduceExtensionMethod(TypeSymbol receiverType)
        {
            if ((object)receiverType == null)
            {
                throw new ArgumentNullException(nameof(receiverType));
            }

            if (!this.IsExtensionMethod || this.MethodKind == MethodKind.ReducedExtension)
            {
                return null;
            }

            // To give optimal diagnostics, we should really pass the "current" compilation.
            // However, this is never used in batch scenarios, so it doesn't matter
            // (modulo future changes to the API).
            return ReducedExtensionMethodSymbol.Create(this, receiverType, compilation: null);
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
        /// Apply type substitution to a generic method to create an method symbol with the given type parameters supplied.
        /// </summary>
        /// <param name="typeArguments"></param>
        /// <returns></returns>
        public MethodSymbol Construct(params TypeSymbol[] typeArguments)
        {
            return this.Construct(ImmutableArray.Create(typeArguments));
        }

        /// <summary>
        /// Apply type substitution to a generic method to create an method symbol with the given type parameters supplied.
        /// </summary>
        /// <param name="typeArguments"></param>
        /// <returns></returns>
        public MethodSymbol Construct(ImmutableArray<TypeSymbol> typeArguments)
        {
            return Construct(typeArguments.SelectAsArray(a => (object)a == null ? null : TypeSymbolWithAnnotations.Create(a)));
        }

        internal MethodSymbol Construct(ImmutableArray<TypeSymbolWithAnnotations> typeArguments)
        {
            if (!ReferenceEquals(this, ConstructedFrom) || this.Arity == 0)
            {
                throw new InvalidOperationException();
            }

            if (typeArguments.IsDefault)
            {
                throw new ArgumentNullException(nameof(typeArguments));
            }

            if (typeArguments.Any(NamedTypeSymbol.TypeSymbolIsNullFunction))
            {
                throw new ArgumentException(CSharpResources.TypeArgumentCannotBeNull, "typeArguments");
            }

            if (typeArguments.Length != this.Arity)
            {
                throw new ArgumentException(CSharpResources.WrongNumberOfTypeArguments, "typeArguments");
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
            return (newOwner == this.ContainingSymbol) ? this : new SubstitutedMethodSymbol((SubstitutedNamedTypeSymbol)newOwner, this);
        }

        /// <summary>
        /// As a performance optimization, cache parameter types and refkinds - overload resolution uses them a lot.
        /// </summary>
        private ParameterSignature _lazyParameterSignature;
        internal ImmutableArray<TypeSymbol> ParameterTypes
        {
            get
            {
                ParameterSignature.PopulateParameterSignature(this.Parameters, ref _lazyParameterSignature);
                return _lazyParameterSignature.parameterTypes;
            }
        }

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

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (this.IsDefinition)
            {
                return base.GetUseSiteDiagnostic();
            }

            // There is no reason to specially check type arguments because
            // constructed members are never imported.
            return this.OriginalDefinition.GetUseSiteDiagnostic();
        }

        internal bool CalculateUseSiteDiagnostic(ref DiagnosticInfo result)
        {
            Debug.Assert(this.IsDefinition);

            // Check return type, custom modifiers, parameters
            if (DeriveUseSiteDiagnosticFromType(ref result, this.ReturnType) ||
                DeriveUseSiteDiagnosticFromParameters(ref result, this.Parameters))
            {
                return true;
            }

            // If the member is in an assembly with unified references, 
            // we check if its definition depends on a type from a unified reference.
            if (this.ContainingModule.HasUnifiedReferences)
            {
                HashSet<TypeSymbol> unificationCheckedTypes = null;

                if (this.ReturnType.GetUnificationUseSiteDiagnosticRecursive(ref result, this, ref unificationCheckedTypes) ||
                    GetUnificationUseSiteDiagnosticRecursive(ref result, this.Parameters, this, ref unificationCheckedTypes) ||
                    GetUnificationUseSiteDiagnosticRecursive(ref result, this.TypeParameters, this, ref unificationCheckedTypes))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Return error code that has highest priority while calculating use site error for this symbol. 
        /// </summary>
        protected override int HighestPriorityUseSiteError
        {
            get
            {
                return (int)ErrorCode.ERR_BindToBogus;
            }
        }

        public sealed override bool HasUnsupportedMetadata
        {
            get
            {
                DiagnosticInfo info = GetUseSiteDiagnostic();
                return (object)info != null && (info.Code == (int)ErrorCode.ERR_BindToBogus || info.Code == (int)ErrorCode.ERR_ByRefReturnUnsupported);
            }
        }

        #endregion

        internal bool IsIterator
        {
            get
            {
                return (object)IteratorElementType != null;
            }
        }

        /// <summary>
        /// If the method was written as an iterator method (i.e. with yield statements in its body) returns the
        /// element type of the iterator.  Otherwise returns null.
        /// </summary>
        internal virtual TypeSymbol IteratorElementType
        {
            get { return null; }
            set { throw ExceptionUtilities.Unreachable; }
        }

        /// <summary>
        /// Generates bound block representing method's body for methods in lowered form and adds it to
        /// a collection of method bodies of the current module. This method is supposed to only be 
        /// called for method symbols which return SynthesizesLoweredBoundBody == true.
        /// </summary>
        internal virtual void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            throw ExceptionUtilities.Unreachable;
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

        internal override bool NullableOptOut
        {
            get
            {
                Debug.Assert(IsDefinition);

                switch (MethodKind)
                {
                    case MethodKind.PropertyGet:
                    case MethodKind.PropertySet:
                    case MethodKind.EventAdd:
                    case MethodKind.EventRemove:
                        var propertyOrEvent = AssociatedSymbol;
                        if ((object)propertyOrEvent != null)
                        {
                            return propertyOrEvent.NullableOptOut;
                        }
                        break;
                }

                return ContainingType?.NullableOptOut == true;
            }
        }

        #region IMethodSymbol Members

        MethodKind IMethodSymbol.MethodKind
        {
            get
            {
                switch (this.MethodKind)
                {
                    case MethodKind.AnonymousFunction:
                        return MethodKind.AnonymousFunction;
                    case MethodKind.Constructor:
                        return MethodKind.Constructor;
                    case MethodKind.Conversion:
                        return MethodKind.Conversion;
                    case MethodKind.DelegateInvoke:
                        return MethodKind.DelegateInvoke;
                    case MethodKind.Destructor:
                        return MethodKind.Destructor;
                    case MethodKind.EventAdd:
                        return MethodKind.EventAdd;
                    case MethodKind.EventRemove:
                        return MethodKind.EventRemove;
                    case MethodKind.ExplicitInterfaceImplementation:
                        return MethodKind.ExplicitInterfaceImplementation;
                    case MethodKind.UserDefinedOperator:
                        return MethodKind.UserDefinedOperator;
                    case MethodKind.BuiltinOperator:
                        return MethodKind.BuiltinOperator;
                    case MethodKind.Ordinary:
                        return MethodKind.Ordinary;
                    case MethodKind.PropertyGet:
                        return MethodKind.PropertyGet;
                    case MethodKind.PropertySet:
                        return MethodKind.PropertySet;
                    case MethodKind.ReducedExtension:
                        return MethodKind.ReducedExtension;
                    case MethodKind.StaticConstructor:
                        return MethodKind.StaticConstructor;
                    case MethodKind.LocalFunction:
                        return MethodKind.LocalFunction;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(this.MethodKind);
                }
            }
        }

        ITypeSymbol IMethodSymbol.ReturnType
        {
            get
            {
                return this.ReturnType.TypeSymbol;
            }
        }

        ImmutableArray<ITypeSymbol> IMethodSymbol.TypeArguments
        {
            get
            {
                return this.TypeArguments.SelectAsArray(a => (ITypeSymbol)a.TypeSymbol);
            }
        }

        ImmutableArray<ITypeParameterSymbol> IMethodSymbol.TypeParameters
        {
            get
            {
                return StaticCast<ITypeParameterSymbol>.From(this.TypeParameters);
            }
        }

        ImmutableArray<IParameterSymbol> IMethodSymbol.Parameters
        {
            get
            {
                return StaticCast<IParameterSymbol>.From(this.Parameters);
            }
        }

        IMethodSymbol IMethodSymbol.ConstructedFrom
        {
            get
            {
                return this.ConstructedFrom;
            }
        }

        IMethodSymbol IMethodSymbol.OriginalDefinition
        {
            get
            {
                return this.OriginalDefinition;
            }
        }

        IMethodSymbol IMethodSymbol.OverriddenMethod
        {
            get
            {
                return this.OverriddenMethod;
            }
        }

        ITypeSymbol IMethodSymbol.ReceiverType
        {
            get
            {
                return this.ReceiverType;
            }
        }

        IMethodSymbol IMethodSymbol.ReducedFrom
        {
            get
            {
                return this.ReducedFrom;
            }
        }

        ITypeSymbol IMethodSymbol.GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
        {
            return this.GetTypeInferredDuringReduction(reducedFromTypeParameter.EnsureCSharpSymbolOrNull<ITypeParameterSymbol, TypeParameterSymbol>("reducedFromTypeParameter"));
        }

        IMethodSymbol IMethodSymbol.ReduceExtensionMethod(ITypeSymbol receiverType)
        {
            return this.ReduceExtensionMethod(receiverType.EnsureCSharpSymbolOrNull<ITypeSymbol, TypeSymbol>("receiverType"));
        }

        ImmutableArray<IMethodSymbol> IMethodSymbol.ExplicitInterfaceImplementations
        {
            get
            {
                return this.ExplicitInterfaceImplementations.Cast<MethodSymbol, IMethodSymbol>();
            }
        }

        ISymbol IMethodSymbol.AssociatedSymbol
        {
            get
            {
                return this.AssociatedSymbol;
            }
        }

        bool IMethodSymbol.IsGenericMethod
        {
            get
            {
                return this.IsGenericMethod;
            }
        }

        bool IMethodSymbol.IsAsync
        {
            get
            {
                return this.IsAsync;
            }
        }

        bool IMethodSymbol.HidesBaseMethodsByName
        {
            get
            {
                return this.HidesBaseMethodsByName;
            }
        }

        ImmutableArray<CustomModifier> IMethodSymbol.ReturnTypeCustomModifiers
        {
            get
            {
                return this.ReturnType.CustomModifiers;
            }
        }

        ImmutableArray<AttributeData> IMethodSymbol.GetReturnTypeAttributes()
        {
            return this.GetReturnTypeAttributes().Cast<CSharpAttributeData, AttributeData>();
        }

        /// <summary>
        /// Build and add synthesized return type attributes for this method symbol.
        /// </summary>
        internal virtual void AddSynthesizedReturnTypeAttributes(ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
        }

        IMethodSymbol IMethodSymbol.Construct(params ITypeSymbol[] arguments)
        {
            foreach (var arg in arguments)
            {
                arg.EnsureCSharpSymbolOrNull<ITypeSymbol, TypeSymbol>("typeArguments");
            }

            return this.Construct(arguments.Cast<TypeSymbol>().AsImmutable());
        }

        IMethodSymbol IMethodSymbol.PartialImplementationPart
        {
            get
            {
                return PartialImplementationPart;
            }
        }

        IMethodSymbol IMethodSymbol.PartialDefinitionPart
        {
            get
            {
                return PartialDefinitionPart;
            }
        }

        INamedTypeSymbol IMethodSymbol.AssociatedAnonymousDelegate
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region IMethodSymbolInternal

        int IMethodSymbolInternal.CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            return CalculateLocalSyntaxOffset(localPosition, localTree);
        }

        #endregion

        #region ISymbol Members

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitMethod(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitMethod(this);
        }

        #endregion
    }
}
