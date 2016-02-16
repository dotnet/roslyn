// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a local variable in a method body.
    /// </summary>
    internal abstract class LocalSymbol : Symbol, ILocalSymbolInternal
    {
        protected LocalSymbol()
        {
        }

        internal abstract LocalDeclarationKind DeclarationKind
        {
            get;
        }

        internal abstract SynthesizedLocalKind SynthesizedKind
        {
            get;
        }

        internal abstract LocalSymbol WithSynthesizedLocalKindAndSyntax(SynthesizedLocalKind kind, SyntaxNode syntax);

        internal abstract bool IsImportedFromMetadata
        {
            get;
        }

        internal virtual bool CanScheduleToStack
        {
            get { return !IsConst && !IsPinned; }
        }

        internal abstract SyntaxToken IdentifierToken
        {
            get;
        }

        /// <summary>
        /// Gets the type of this local.
        /// </summary>
        public abstract TypeSymbol Type
        {
            get;
        }

        /// <summary>
        /// WARN WARN WARN: If you access this via the semantic model, things will break (since the initializer may not have been bound).
        /// 
        /// Whether or not this local is pinned (i.e. the type will be emitted with the "pinned" modifier).
        /// </summary>
        /// <remarks>
        /// Superficially, it seems as though this should always be the same as DeclarationKind == LocalDeclarationKind.Fixed.
        /// Unfortunately, when we fix a string expression, it is not the declared local (e.g. char*) but a synthesized temp (string)
        /// that is pinned.
        /// </remarks>
        internal abstract bool IsPinned
        {
            get;
        }

        /// <summary>
        /// Returns false because local variable can't be defined externally.
        /// </summary>
        public sealed override bool IsExtern
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because local variable can't be sealed.
        /// </summary>
        public sealed override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because local variable can't be abstract.
        /// </summary>
        public sealed override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because local variable can't be overridden.
        /// </summary>
        public sealed override bool IsOverride
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because local variable can't be virtual.
        /// </summary>
        public sealed override bool IsVirtual
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns false because local variable can't be declared as static in C#.
        /// </summary>
        public sealed override bool IsStatic
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        /// <summary>
        /// Returns 'NotApplicable' because local variable can't be used outside the member body..
        /// </summary>
        public sealed override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.NotApplicable;
            }
        }

        /// <summary>
        /// Returns value 'Local' of the <see cref="SymbolKind"/>
        /// </summary>
        public sealed override SymbolKind Kind
        {
            get
            {
                return SymbolKind.Local;
            }
        }

        internal sealed override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLocal(this, argument);
        }

        public sealed override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitLocal(this);
        }

        public sealed override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitLocal(this);
        }

        /// <summary>
        /// Returns true if this local variable was declared in a catch clause. 
        /// </summary>
        public bool IsCatch
        {
            get
            {
                return this.DeclarationKind == LocalDeclarationKind.CatchVariable;
            }
        }

        /// <summary>
        /// Returns true if this local variable was declared as "const" (i.e. is a constant declaration).
        /// </summary>
        public bool IsConst
        {
            get
            {
                return this.DeclarationKind == LocalDeclarationKind.Constant;
            }
        }

        /// <summary>
        /// Returns true if the local variable is declared in resource-acquisition of a 'using statement';
        /// otherwise false
        /// </summary>
        /// <example>
        /// <code>
        ///     using (var localVariable = new StreamReader("C:\\Temp\\MyFile.txt")) { ... } 
        /// </code>
        /// </example>
        public bool IsUsing
        {
            get
            {
                return this.DeclarationKind == LocalDeclarationKind.UsingVariable;
            }
        }

        /// <summary>
        /// Returns true if the local variable is declared in fixed-pointer-initializer (in unsafe context)
        /// </summary>
        public bool IsFixed
        {
            get
            {
                return this.DeclarationKind == LocalDeclarationKind.FixedVariable;
            }
        }

        /// <summary>
        /// Returns true if this local variable is declared in for-initializer
        /// </summary>
        public bool IsFor
        {
            get
            {
                return this.DeclarationKind == LocalDeclarationKind.ForInitializerVariable;
            }
        }

        /// <summary>
        /// Returns true if this local variable is declared as iteration variable
        /// </summary>
        public bool IsForEach
        {
            get
            {
                return this.DeclarationKind == LocalDeclarationKind.ForEachIterationVariable;
            }
        }

        /// <summary>
        /// Returns the syntax node that declares the variable.
        /// </summary>
        /// <remarks>
        /// All user-defined and long-lived synthesized variables must return a reference to a node that is 
        /// tracked by the EnC diffing algorithm. For example, for <see cref="LocalDeclarationKind.CatchVariable"/> variable
        /// the declarator is the <see cref="CatchClauseSyntax"/> node.
        /// 
        /// The location of the declarator is used to calculate <see cref="LocalDebugId.SyntaxOffset"/> during emit.
        /// </remarks>
        internal abstract SyntaxNode GetDeclaratorSyntax();

        internal virtual bool IsWritable
        {
            get
            {
                switch (this.DeclarationKind)
                {
                    case LocalDeclarationKind.Constant:
                    case LocalDeclarationKind.FixedVariable:
                    case LocalDeclarationKind.ForEachIterationVariable:
                    case LocalDeclarationKind.UsingVariable:
                    case LocalDeclarationKind.PatternVariable:
                        return false;
                    default:
                        return true;
                }
            }
        }

        /// <summary>
        /// Returns false if the field wasn't declared as "const", or constant value was omitted or erroneous.
        /// True otherwise.
        /// </summary>
        public bool HasConstantValue
        {
            get
            {
                if (!this.IsConst)
                {
                    return false;
                }

                ConstantValue constantValue = this.GetConstantValue(null, null, null);
                return constantValue != null && !constantValue.IsBad; //can be null in error scenarios
            }
        }

        /// <summary>
        /// If IsConst returns true, then returns the constant value of the field or enum member. If IsConst returns
        /// false, then returns null.
        /// </summary>
        public object ConstantValue
        {
            get
            {
                if (!this.IsConst)
                {
                    return null;
                }

                ConstantValue constantValue = this.GetConstantValue(null, null, null);
                return constantValue?.Value; //can be null in error scenarios
            }
        }

        /// <summary>
        /// Returns true if the local symbol was compiler generated.
        /// </summary>
        internal abstract bool IsCompilerGenerated
        {
            get;
        }

        internal abstract ConstantValue GetConstantValue(SyntaxNode node, LocalSymbol inProgress, DiagnosticBag diagnostics = null);

        internal abstract ImmutableArray<Diagnostic> GetConstantValueDiagnostics(BoundExpression boundInitValue);

        internal abstract RefKind RefKind
        {
            get;
        }

        #region ILocalSymbol Members

        ITypeSymbol ILocalSymbol.Type
        {
            get
            {
                return this.Type;
            }
        }

        bool ILocalSymbol.IsFunctionValue
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region ISymbol Members

        public sealed override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitLocal(this);
        }

        public sealed override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitLocal(this);
        }

        #endregion

        #region ILocalSymbolInternal Members

        SynthesizedLocalKind ILocalSymbolInternal.SynthesizedKind
        {
            get
            {
                return this.SynthesizedKind;
            }
        }

        bool ILocalSymbolInternal.IsImportedFromMetadata
        {
            get
            {
                return this.IsImportedFromMetadata;
            }
        }

        SyntaxNode ILocalSymbolInternal.GetDeclaratorSyntax()
        {
            return this.GetDeclaratorSyntax();
        }

        #endregion
    }
}
