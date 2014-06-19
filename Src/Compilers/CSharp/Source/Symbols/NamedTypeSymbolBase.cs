using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace Roslyn.Compilers.CSharp
{
    internal abstract partial class NamedTypeSymbolBase : NamedTypeSymbol
    {
        private readonly int arity;
        private readonly string name;
        private readonly NamespaceOrTypeSymbol containingSymbol;

        internal NamedTypeSymbolBase(NamespaceOrTypeSymbol containingSymbol, string name, int arity, bool isSerializable = false)
        {
            this.containingSymbol = containingSymbol;
            this.name = name;
            this.arity = arity;
        }

        public override int Arity
        {
            get
            {
                return arity;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return containingSymbol;
            }
        }

        public override string Name
        {
            get
            {
                return name;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IEnumerable<SymbolAttribute> GetAttributes()
        {
            throw new NotImplementedException();
        }

        public override bool IsStatic
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal override bool MightContainExtensionMethods
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private ReadOnlyArray<TypeParameterSymbol> typeParameters;
        internal abstract ReadOnlyArray<TypeParameterSymbol> MakeTypeParameters(DiagnosticBag diagnostics);
        public override ReadOnlyArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                if (typeParameters.IsNull)
                {
                    var diagnostics = DiagnosticBag.GetInstance();
                    if (ReadOnlyInterlocked.CompareExchange(ref typeParameters, MakeTypeParameters(diagnostics), ReadOnlyArray<TypeParameterSymbol>.Null).IsNull)
                    {
                        AddSemanticDiagnostics(diagnostics);

                        NotePartComplete(CompletionPart.TypeParameters);
                    }

                    diagnostics.Free();
                }

                return typeParameters;
            }
        }
        protected void SetTypeParameters(ReadOnlyArray<TypeParameterSymbol> value)
        {
            Debug.Assert(typeParameters.IsNull);
            ReadOnlyInterlocked.CompareExchange(ref typeParameters, value, ReadOnlyArray<TypeParameterSymbol>.Null);
        }

        private Dictionary<string, ReadOnlyArray<NamedTypeSymbol>> typeMembers;
        internal abstract Dictionary<string, ReadOnlyArray<NamedTypeSymbol>> MakeTypeMembers(DiagnosticBag diagnostics, CancellationToken cancellationToken = default(CancellationToken));
        internal Dictionary<string, ReadOnlyArray<NamedTypeSymbol>> GetTypeMembersDictionary(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (typeMembers == null)
            {
                var diagnostics = DiagnosticBag.GetInstance();
                if (Interlocked.CompareExchange(ref typeMembers, MakeTypeMembers(diagnostics, cancellationToken), null) == null)
                {
                    AddSemanticDiagnostics(diagnostics);

                    NotePartComplete(CompletionPart.TypeMembers);
                }

                diagnostics.Free();
            }

            return typeMembers;
        }

        private IEnumerable<NamedTypeSymbol> allTypeMembers;
        public override IEnumerable<NamedTypeSymbol> GetTypeMembers(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (allTypeMembers == null)
            {
                var builder = ArrayBuilder<NamedTypeSymbol>.GetInstance();
                this.GetTypeMembersDictionary(cancellationToken).AddAllValues(builder);
                allTypeMembers = builder.ToReadOnlyAndFree().AsEnumerable();
            }

            return allTypeMembers;
        }

        public override ReadOnlyArray<NamedTypeSymbol> GetTypeMembers(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            ReadOnlyArray<NamedTypeSymbol> members;
            if (GetTypeMembersDictionary(cancellationToken).TryGetValue(name, out members))
            {
                return members;
            }

            return ReadOnlyArray<NamedTypeSymbol>.Empty;
        }

        public override IEnumerable<NamedTypeSymbol> GetTypeMembers(string name, int arity, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetTypeMembers(name, cancellationToken).Where(t => t.Arity == arity);
        }

        /// <summary>
        /// All member symbols grouped by name.
        /// </summary>
        internal Dictionary<string, ReadOnlyArray<Symbol>> MembersByName
        {
            get
            {
                return GetMembersByName(CancellationToken.None);
            }
        }

        private Dictionary<string, ReadOnlyArray<Symbol>> GetMembersByName(CancellationToken cancellationToken)
        {
            return GetMembersAndInitializers(cancellationToken).Members;
        }

        /// <summary>
        /// Static initializers grouped by the declaring class syntax node.
        /// </summary>
        internal ReadOnlyArray<ReadOnlyArray<FieldInitializer>> StaticInitializers
        {
            get
            {
                return GetMembersAndInitializers(CancellationToken.None).StaticInitializers;
            }
        }

        /// <summary>
        /// Instance initializers and global statements grouped by the declaring syntax node.
        /// </summary>
        internal ReadOnlyArray<ReadOnlyArray<FieldInitializer>> InstanceInitializers
        {
            get
            {
                return GetMembersAndInitializers(CancellationToken.None).InstanceInitializers;
            }
        }

        private MembersAndInitializers GetMembersAndInitializers(CancellationToken cancellationToken)
        {
            if (lazyMembersAndInitializers == null)
            {
                var diagnostics = DiagnosticBag.GetInstance();
                try
                {
                    if (Interlocked.CompareExchange(ref lazyMembersAndInitializers, MakeMembers(diagnostics, cancellationToken), null) == null)
                    {
                        AfterMembersChecks(diagnostics);
                        AddSemanticDiagnostics(diagnostics);

                        NotePartComplete(CompletionPart.Members);
                    }
                }
                finally
                {
                    diagnostics.Free();
                }
            }

            return lazyMembersAndInitializers;
        }

        private MembersAndInitializers lazyMembersAndInitializers;

        internal abstract MembersAndInitializers MakeNonTypeMembers(DiagnosticBag diagnostics, CancellationToken cancellationToken);

        // any checks that must be performed after "members" are set should be done here.
        protected virtual void AfterMembersChecks(DiagnosticBag diagnostics)
        {
        }

        private ReadOnlyArray<Symbol> allMembers;
        public override ReadOnlyArray<Symbol> GetMembers(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (allMembers == ReadOnlyArray<Symbol>.Null)
            {
                var builder = ArrayBuilder<Symbol>.GetInstance();
                try
                {
                    GetMembersByName(cancellationToken).AddAllValues(builder);
                    allMembers = builder.ToReadOnly();
                }
                finally
                {
                    builder.Free();
                }
            }

            return allMembers;
        }

        public override ReadOnlyArray<Symbol> GetMembers(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            ReadOnlyArray<Symbol> members;
            if (GetMembersByName(cancellationToken).TryGetValue(name, out members))
            {
                return members;
            }

            return ReadOnlyArray<Symbol>.Empty;
        }

        internal MembersAndInitializers MakeMembers(DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            // Build dictionary with non-type members
            var membersAndInitializers = MakeNonTypeMembers(diagnostics, cancellationToken);
            var members = membersAndInitializers.Members;

            // Merge types into the member's dictionary
            foreach (var nestedType in GetTypeMembersDictionary(cancellationToken).Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var key = nestedType[0].Name;
                ReadOnlyArray<Symbol> membersForKey;
                if (!members.TryGetValue(key, out membersForKey))
                {
                    members.Add(key, ReadOnlyArray<Symbol>.CreateFrom(nestedType));
                }
                else
                {
                    members[key] = membersForKey.Concat(ReadOnlyArray<Symbol>.CreateFrom(nestedType));
                }

            }
            return membersAndInitializers;
        }

        internal sealed class MembersAndInitializers
        {
            internal Dictionary<string, ReadOnlyArray<Symbol>> Members { get; set; }
            internal ReadOnlyArray<ReadOnlyArray<FieldInitializer>> StaticInitializers { get; set; }
            internal ReadOnlyArray<ReadOnlyArray<FieldInitializer>> InstanceInitializers { get; set; }
        }
    }
}