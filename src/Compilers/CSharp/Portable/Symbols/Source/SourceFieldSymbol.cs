// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class SourceFieldSymbol : FieldSymbolWithAttributesAndModifiers
    {
        protected readonly SourceMemberContainerTypeSymbol containingType;

        protected SourceFieldSymbol(SourceMemberContainerTypeSymbol containingType)
        {
            Debug.Assert((object)containingType != null);

            this.containingType = containingType;
        }

        public abstract override string Name { get; }

        protected override IAttributeTargetSymbol AttributeOwner
        {
            get { return this; }
        }

        internal sealed override bool RequiresCompletion
        {
            get { return true; }
        }

        internal bool IsNew
        {
            get
            {
                return (Modifiers & DeclarationModifiers.New) != 0;
            }
        }

        protected void CheckAccessibility(BindingDiagnosticBag diagnostics)
        {
            ModifierUtils.CheckAccessibility(Modifiers, this, isExplicitInterfaceImplementation: false, diagnostics, ErrorLocation);
        }

        protected void ReportModifiersDiagnostics(BindingDiagnosticBag diagnostics)
        {
            if (ContainingType.IsSealed && this.DeclaredAccessibility.HasProtected())
            {
                diagnostics.Add(AccessCheck.GetProtectedMemberInSealedTypeError(containingType), ErrorLocation, this);
            }
            else if (IsVolatile && IsReadOnly)
            {
                diagnostics.Add(ErrorCode.ERR_VolatileAndReadonly, ErrorLocation, this);
            }
            else if (containingType.IsStatic && !IsStatic)
            {
                diagnostics.Add(ErrorCode.ERR_InstanceMemberInStaticClass, ErrorLocation, this);
            }
            else if (!IsStatic && !IsReadOnly && containingType.IsReadOnly)
            {
                diagnostics.Add(ErrorCode.ERR_FieldsInRoStruct, ErrorLocation);
            }

            // TODO: Consider checking presence of core type System.Runtime.CompilerServices.IsVolatile
            // if there is a volatile modifier. Perhaps an appropriate error should be reported if the
            // type isn't available.
        }

        protected ImmutableArray<CustomModifier> RequiredCustomModifiers
        {
            get
            {
                if (!IsVolatile)
                {
                    return ImmutableArray<CustomModifier>.Empty;
                }
                else
                {
                    return ImmutableArray.Create<CustomModifier>(
                            CSharpCustomModifier.CreateRequired(this.ContainingAssembly.GetSpecialType(SpecialType.System_Runtime_CompilerServices_IsVolatile)));
                }
            }
        }

        // Currently, source symbols cannot declare RefCustomModifiers. If that changes, and this
        // property is updated, test retargeting. (Update RefFieldTests.RetargetingField for instance.)
        public sealed override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public sealed override Symbol ContainingSymbol
        {
            get
            {
                return containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return this.containingType;
            }
        }

        protected sealed override void DecodeWellKnownAttributeImpl(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);
            Debug.Assert(arguments.Diagnostics is BindingDiagnosticBag);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None);

            if (attribute.IsTargetAttribute(this, AttributeDescription.FixedBufferAttribute))
            {
                // error CS1716: Do not use 'System.Runtime.CompilerServices.FixedBuffer' attribute. Use the 'fixed' field modifier instead.
                ((BindingDiagnosticBag)arguments.Diagnostics).Add(ErrorCode.ERR_DoNotUseFixedBufferAttr, arguments.AttributeSyntaxOpt.Name.Location);
            }
            else
            {
                base.DecodeWellKnownAttributeImpl(ref arguments);
            }
        }

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ErrorLocation;

            if (RefKind == RefKind.RefReadOnly)
            {
                compilation.EnsureIsReadOnlyAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            if (compilation.ShouldEmitNativeIntegerAttributes(Type))
            {
                compilation.EnsureNativeIntegerAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            if (compilation.ShouldEmitNullableAttributes(this) &&
                TypeWithAnnotations.NeedsNullableAttribute())
            {
                compilation.EnsureNullableAttributeExists(diagnostics, location, modifyCompilation: true);
            }
        }

        internal sealed override bool HasRuntimeSpecialName
        {
            get
            {
                return this.Name == WellKnownMemberNames.EnumBackingFieldName;
            }
        }

        internal override bool IsRequired => (Modifiers & DeclarationModifiers.Required) != 0;
    }

    internal abstract class SourceFieldSymbolWithSyntaxReference : SourceFieldSymbol
    {
        private readonly string _name;
        private readonly TextSpan _locationSpan;
        private readonly SyntaxReference _syntaxReference;

        private string _lazyDocComment;
        private string _lazyExpandedDocComment;
        private ConstantValue _lazyConstantEarlyDecodingValue = Microsoft.CodeAnalysis.ConstantValue.Unset;
        private ConstantValue _lazyConstantValue = Microsoft.CodeAnalysis.ConstantValue.Unset;

        protected SourceFieldSymbolWithSyntaxReference(SourceMemberContainerTypeSymbol containingType, string name, SyntaxReference syntax, TextSpan locationSpan)
            : base(containingType)
        {
            Debug.Assert(name != null);
            Debug.Assert(syntax != null);

            _name = name;
            _syntaxReference = syntax;
            _locationSpan = locationSpan;
        }

        public SyntaxTree SyntaxTree
        {
            get
            {
                return _syntaxReference.SyntaxTree;
            }
        }

        public CSharpSyntaxNode SyntaxNode
        {
            get
            {
                return (CSharpSyntaxNode)_syntaxReference.GetSyntax();
            }
        }

        public sealed override string Name
        {
            get
            {
                return _name;
            }
        }

        internal override LexicalSortKey GetLexicalSortKey()
            => new LexicalSortKey(_syntaxReference, this.DeclaringCompilation);

        public override Location TryGetFirstLocation()
            => _syntaxReference.SyntaxTree.GetLocation(_locationSpan);

        public sealed override ImmutableArray<Location> Locations
            => ImmutableArray.Create(GetFirstLocation());

        internal sealed override Location ErrorLocation
            => GetFirstLocation();

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            => ImmutableArray.Create(_syntaxReference);

        public override bool IsDefinedInSourceTree(SyntaxTree tree, TextSpan? definedWithinSpan, CancellationToken cancellationToken = default)
            => IsDefinedInSourceTree(_syntaxReference, tree, definedWithinSpan);

        public sealed override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            ref var lazyDocComment = ref expandIncludes ? ref _lazyExpandedDocComment : ref _lazyDocComment;
            return SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this, expandIncludes, ref lazyDocComment);
        }

        internal sealed override ConstantValue GetConstantValue(ConstantFieldsInProgress inProgress, bool earlyDecodingWellKnownAttributes)
        {
            var value = this.GetLazyConstantValue(earlyDecodingWellKnownAttributes);
            if (value != Microsoft.CodeAnalysis.ConstantValue.Unset)
            {
                return value;
            }

            if (!inProgress.IsEmpty)
            {
                // Add this field as a dependency of the original field, and
                // return Unset. The outer GetConstantValue caller will call
                // this method again after evaluating any dependencies.
                inProgress.AddDependency(this);
                return Microsoft.CodeAnalysis.ConstantValue.Unset;
            }

            // Order dependencies.
            var order = ArrayBuilder<ConstantEvaluationHelpers.FieldInfo>.GetInstance();
            this.OrderAllDependencies(order, earlyDecodingWellKnownAttributes);

            // Evaluate fields in order.
            foreach (var info in order)
            {
                // Bind the field value regardless of whether the field represents
                // the start of a cycle. In the cycle case, there will be unevaluated
                // dependencies and the result will be ConstantValue.Bad plus cycle error.
                var field = info.Field;
                field.BindConstantValueIfNecessary(earlyDecodingWellKnownAttributes, startsCycle: info.StartsCycle);
            }

            order.Free();

            // Return the value of this field.
            return this.GetLazyConstantValue(earlyDecodingWellKnownAttributes);
        }

        /// <summary>
        /// Return the constant value dependencies. Compute the dependencies
        /// if necessary by evaluating the constant value but only persist the
        /// constant value if there were no dependencies. (If there are dependencies,
        /// the constant value will be re-evaluated after evaluating dependencies.)
        /// </summary>
        internal ImmutableHashSet<SourceFieldSymbolWithSyntaxReference> GetConstantValueDependencies(bool earlyDecodingWellKnownAttributes)
        {
            var value = this.GetLazyConstantValue(earlyDecodingWellKnownAttributes);
            if (value != Microsoft.CodeAnalysis.ConstantValue.Unset)
            {
                // Constant value already determined. No need to
                // compute dependencies since the constant values
                // of all dependencies should be evaluated as well.
                return ImmutableHashSet<SourceFieldSymbolWithSyntaxReference>.Empty;
            }

            ImmutableHashSet<SourceFieldSymbolWithSyntaxReference> dependencies;
            var builder = PooledHashSet<SourceFieldSymbolWithSyntaxReference>.GetInstance();
            var diagnostics = BindingDiagnosticBag.GetInstance();
            value = MakeConstantValue(builder, earlyDecodingWellKnownAttributes, diagnostics);

            // Only persist if there are no dependencies and the calculation
            // completed successfully. (We could probably persist in other
            // scenarios but it's probably not worth the added complexity.)
            if ((builder.Count == 0) &&
                (value != null) &&
                !value.IsBad &&
                (value != Microsoft.CodeAnalysis.ConstantValue.Unset) &&
                !diagnostics.HasAnyResolvedErrors())
            {
                this.SetLazyConstantValue(
                    value,
                    earlyDecodingWellKnownAttributes,
                    diagnostics,
                    startsCycle: false);
                dependencies = ImmutableHashSet<SourceFieldSymbolWithSyntaxReference>.Empty;
            }
            else
            {
                dependencies = ImmutableHashSet<SourceFieldSymbolWithSyntaxReference>.Empty.Union(builder);
            }

            diagnostics.Free();
            builder.Free();
            return dependencies;
        }

        private void BindConstantValueIfNecessary(bool earlyDecodingWellKnownAttributes, bool startsCycle)
        {
            if (this.GetLazyConstantValue(earlyDecodingWellKnownAttributes) != Microsoft.CodeAnalysis.ConstantValue.Unset)
            {
                return;
            }

            var builder = PooledHashSet<SourceFieldSymbolWithSyntaxReference>.GetInstance();
            var diagnostics = BindingDiagnosticBag.GetInstance();
            if (startsCycle)
            {
                diagnostics.Add(ErrorCode.ERR_CircConstValue, GetFirstLocation(), this);
            }

            var value = MakeConstantValue(builder, earlyDecodingWellKnownAttributes, diagnostics);
            this.SetLazyConstantValue(
                value,
                earlyDecodingWellKnownAttributes,
                diagnostics,
                startsCycle);
            diagnostics.Free();
            builder.Free();
        }

        private ConstantValue GetLazyConstantValue(bool earlyDecodingWellKnownAttributes)
        {
            return earlyDecodingWellKnownAttributes ? _lazyConstantEarlyDecodingValue : _lazyConstantValue;
        }

        private void SetLazyConstantValue(
            ConstantValue value,
            bool earlyDecodingWellKnownAttributes,
            BindingDiagnosticBag diagnostics,
            bool startsCycle)
        {
            Debug.Assert(value != Microsoft.CodeAnalysis.ConstantValue.Unset);
            Debug.Assert((GetLazyConstantValue(earlyDecodingWellKnownAttributes) == Microsoft.CodeAnalysis.ConstantValue.Unset) ||
                (GetLazyConstantValue(earlyDecodingWellKnownAttributes) == value));

            if (earlyDecodingWellKnownAttributes)
            {
                Interlocked.CompareExchange(ref _lazyConstantEarlyDecodingValue, value, Microsoft.CodeAnalysis.ConstantValue.Unset);
            }
            else
            {
                if (Interlocked.CompareExchange(ref _lazyConstantValue, value, Microsoft.CodeAnalysis.ConstantValue.Unset) == Microsoft.CodeAnalysis.ConstantValue.Unset)
                {
#if REPORT_ALL
                    Console.WriteLine("Thread {0}, Field {1}, StartsCycle {2}", Thread.CurrentThread.ManagedThreadId, this, startsCycle);
#endif
                    this.AddDeclarationDiagnostics(diagnostics);
                    // CompletionPart.ConstantValue is the last part for a field
                    DeclaringCompilation.SymbolDeclaredEvent(this);
                    var wasSetThisThread = this.state.NotePartComplete(CompletionPart.ConstantValue);
                    Debug.Assert(wasSetThisThread);
                }
            }
        }

        protected abstract ConstantValue MakeConstantValue(HashSet<SourceFieldSymbolWithSyntaxReference> dependencies, bool earlyDecodingWellKnownAttributes, BindingDiagnosticBag diagnostics);
    }
}
