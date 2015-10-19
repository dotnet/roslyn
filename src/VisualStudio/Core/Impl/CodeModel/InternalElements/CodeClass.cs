// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.InternalElements
{
    [ComVisible(true)]
    [ComDefaultInterface(typeof(EnvDTE80.CodeClass2))]
    public sealed class CodeClass : AbstractCodeType, EnvDTE.CodeClass, EnvDTE80.CodeClass2, ICodeClassBase
    {
        private static readonly SymbolDisplayFormat s_BaseNameFormat =
            new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        internal static EnvDTE.CodeClass Create(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
        {
            var element = new CodeClass(state, fileCodeModel, nodeKey, nodeKind);
            var result = (EnvDTE.CodeClass)ComAggregate.CreateAggregatedObject(element);

            fileCodeModel.OnCodeElementCreated(nodeKey, (EnvDTE.CodeElement)result);

            return result;
        }

        internal static EnvDTE.CodeClass CreateUnknown(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
        {
            var element = new CodeClass(state, fileCodeModel, nodeKind, name);

            return (EnvDTE.CodeClass)ComAggregate.CreateAggregatedObject(element);
        }

        private CodeClass(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            SyntaxNodeKey nodeKey,
            int? nodeKind)
            : base(state, fileCodeModel, nodeKey, nodeKind)
        {
        }

        private CodeClass(
            CodeModelState state,
            FileCodeModel fileCodeModel,
            int nodeKind,
            string name)
            : base(state, fileCodeModel, nodeKind, name)
        {
        }

        public override EnvDTE.vsCMElement Kind
        {
            get
            {
                return this.CodeModelService.GetElementKind(LookupNode());
            }
        }

        public bool IsAbstract
        {
            get
            {
                return (InheritanceKind & EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract) != 0;
            }

            set
            {
                var inheritanceKind = InheritanceKind;

                var newInheritanceKind = inheritanceKind;
                if (value)
                {
                    newInheritanceKind |= EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract;
                    newInheritanceKind &= ~EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindSealed;
                }
                else
                {
                    newInheritanceKind &= ~EnvDTE80.vsCMInheritanceKind.vsCMInheritanceKindAbstract;
                }

                if (inheritanceKind != newInheritanceKind)
                {
                    InheritanceKind = newInheritanceKind;
                }
            }
        }

        public EnvDTE80.vsCMClassKind ClassKind
        {
            get
            {
                return CodeModelService.GetClassKind(LookupNode(), (INamedTypeSymbol)LookupSymbol());
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateClassKind, value);
            }
        }

        public EnvDTE80.vsCMInheritanceKind InheritanceKind
        {
            get
            {
                return CodeModelService.GetInheritanceKind(LookupNode(), (INamedTypeSymbol)LookupSymbol());
            }

            set
            {
                UpdateNode(FileCodeModel.UpdateInheritanceKind, value);
            }
        }

        public EnvDTE.CodeElements PartialClasses
        {
            get { return PartialTypeCollection.Create(State, FileCodeModel, this); }
        }

        public EnvDTE.CodeElements Parts
        {
            get { return PartialTypeCollection.Create(State, FileCodeModel, this); }
        }

        public EnvDTE.CodeClass AddClass(string name, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddClass(LookupNode(), name, position, bases, implementedInterfaces, access);
            });
        }

        public EnvDTE.CodeDelegate AddDelegate(string name, object type, object position, EnvDTE.vsCMAccess access)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddDelegate(LookupNode(), name, type, position, access);
            });
        }

        public EnvDTE.CodeEnum AddEnum(string name, object position, object bases, EnvDTE.vsCMAccess access)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddEnum(LookupNode(), name, position, bases, access);
            });
        }

        public EnvDTE80.CodeEvent AddEvent(string name, string fullDelegateName, bool createPropertyStyleEvent, object location, EnvDTE.vsCMAccess access)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddEvent(LookupNode(), name, fullDelegateName, createPropertyStyleEvent, location, access);
            });
        }

        public EnvDTE.CodeFunction AddFunction(string name, EnvDTE.vsCMFunction kind, object type, object position, EnvDTE.vsCMAccess access, object location)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddFunction(LookupNode(), name, kind, type, position, access);
            });
        }

        public EnvDTE.CodeProperty AddProperty(string getterName, string putterName, object type, object position, EnvDTE.vsCMAccess access, object location)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddProperty(LookupNode(), getterName, putterName, type, position, access);
            });
        }

        public EnvDTE.CodeStruct AddStruct(string name, object position, object bases, object implementedInterfaces, EnvDTE.vsCMAccess access)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                return FileCodeModel.AddStruct(LookupNode(), name, position, bases, implementedInterfaces, access);
            });
        }

        public EnvDTE.CodeVariable AddVariable(string name, object type, object position, EnvDTE.vsCMAccess access, object location)
        {
            return FileCodeModel.EnsureEditor(() =>
            {
                // NOTE: C# ignores this location parameter.

                return FileCodeModel.AddVariable(LookupNode(), name, type, position, access);
            });
        }

        public int GetBaseName(out string pBaseName)
        {
            var typeSymbol = LookupTypeSymbol();
            if (typeSymbol?.BaseType == null)
            {
                pBaseName = null;
                return VSConstants.E_FAIL;
            }

            pBaseName = typeSymbol.BaseType.ToDisplayString(s_BaseNameFormat);
            return VSConstants.S_OK;
        }
    }
}
