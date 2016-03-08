// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class SymbolSpecificationViewModel : AbstractNotifyPropertyChanged
    {
        public Guid ID { get; set; }
        public List<SymbolKindViewModel> SymbolKindList { get; set; }
        public List<AccessibilityViewModel> AccessibilityList { get; set; }
        public List<ModifierViewModel> ModifierList { get; set; }
        public List<CustomTagViewModel> CustomTagList { get; set; }

        private string _symbolSpecName;
        public string SymbolSpecName
        {
            get { return _symbolSpecName; }
            set { SetProperty(ref _symbolSpecName, value); }
        }

        private readonly INotificationService _notificationService;

        public SymbolSpecificationViewModel(string languageName, ImmutableArray<string> categories, INotificationService notificationService) : this(languageName, categories, new SymbolSpecification(), notificationService) { }

        public SymbolSpecificationViewModel(string languageName, ImmutableArray<string> categories, SymbolSpecification specification, INotificationService notificationService)
        {
            _notificationService = notificationService;
            SymbolSpecName = specification.Name;
            ID = specification.ID;

            CustomTagList = new List<CustomTagViewModel>();
            foreach (var category in categories)
            {
                CustomTagList.Add(new CustomTagViewModel(category, specification));
            }

            // The list of supported SymbolKinds is limited due to https://github.com/dotnet/roslyn/issues/8753.
            if (languageName == LanguageNames.CSharp)
            {
                SymbolKindList = new List<SymbolKindViewModel>
                {
                    new SymbolKindViewModel(TypeKind.Class, "class", specification),
                    new SymbolKindViewModel(TypeKind.Struct, "struct", specification),
                    new SymbolKindViewModel(TypeKind.Interface, "interface", specification),
                    new SymbolKindViewModel(TypeKind.Enum, "enum", specification),
                    new SymbolKindViewModel(SymbolKind.Property, "property", specification),
                    new SymbolKindViewModel(SymbolKind.Method, "method", specification),
                    new SymbolKindViewModel(SymbolKind.Field, "field", specification),
                    new SymbolKindViewModel(SymbolKind.Event, "event", specification),
                    new SymbolKindViewModel(SymbolKind.Namespace, "namespace", specification),
                    new SymbolKindViewModel(TypeKind.Delegate, "delegate", specification),
                    new SymbolKindViewModel(TypeKind.TypeParameter, "type parameter", specification),
                };

                AccessibilityList = new List<AccessibilityViewModel>
                {
                    new AccessibilityViewModel(Accessibility.Public, "public", specification),
                    new AccessibilityViewModel(Accessibility.Internal, "internal", specification),
                    new AccessibilityViewModel(Accessibility.Private, "private", specification),
                    new AccessibilityViewModel(Accessibility.Protected, "protected", specification),
                    new AccessibilityViewModel(Accessibility.ProtectedOrInternal, "protected internal", specification),
                };

                ModifierList = new List<ModifierViewModel>
                {
                    new ModifierViewModel(DeclarationModifiers.Abstract, "abstract", specification),
                    new ModifierViewModel(DeclarationModifiers.Async, "async", specification),
                    new ModifierViewModel(DeclarationModifiers.Const, "const", specification),
                    new ModifierViewModel(DeclarationModifiers.ReadOnly, "readonly", specification),
                    new ModifierViewModel(DeclarationModifiers.Static, "static", specification)
                };
            }
            else if (languageName == LanguageNames.VisualBasic)
            {
                SymbolKindList = new List<SymbolKindViewModel>
                {
                    new SymbolKindViewModel(TypeKind.Class, "Class", specification),
                    new SymbolKindViewModel(TypeKind.Struct, "Structure", specification),
                    new SymbolKindViewModel(TypeKind.Interface, "Interface", specification),
                    new SymbolKindViewModel(TypeKind.Enum, "Enum", specification),
                    new SymbolKindViewModel(TypeKind.Module, "Module", specification),
                    new SymbolKindViewModel(SymbolKind.Property, "Property", specification),
                    new SymbolKindViewModel(SymbolKind.Method, "Method", specification),
                    new SymbolKindViewModel(SymbolKind.Field, "Field", specification),
                    new SymbolKindViewModel(SymbolKind.Event, "Event", specification),
                    new SymbolKindViewModel(SymbolKind.Namespace, "Namespace", specification),
                    new SymbolKindViewModel(TypeKind.Delegate, "Delegate", specification),
                    new SymbolKindViewModel(TypeKind.TypeParameter, "Type Parameter", specification),
                };

                AccessibilityList = new List<AccessibilityViewModel>
                {
                    new AccessibilityViewModel(Accessibility.Public, "Public", specification),
                    new AccessibilityViewModel(Accessibility.Friend, "Friend", specification),
                    new AccessibilityViewModel(Accessibility.Private, "Private", specification),
                    new AccessibilityViewModel(Accessibility.Protected , "Protected", specification),
                    new AccessibilityViewModel(Accessibility.ProtectedOrInternal, "Protected Friend", specification),
                };

                ModifierList = new List<ModifierViewModel>
                {
                    new ModifierViewModel(DeclarationModifiers.Abstract, "MustInherit", specification),
                    new ModifierViewModel(DeclarationModifiers.Async, "Async", specification),
                    new ModifierViewModel(DeclarationModifiers.Const, "Const", specification),
                    new ModifierViewModel(DeclarationModifiers.ReadOnly, "ReadOnly", specification),
                    new ModifierViewModel(DeclarationModifiers.Static, "Shared", specification)
                };
            }
            else
            {
                throw new ArgumentException(string.Format("Unexpected language name: {0}", languageName), nameof(languageName));
            }
        }

        internal SymbolSpecification GetSymbolSpecification()
        {
            return new SymbolSpecification(
                ID,
                SymbolSpecName,
                SymbolKindList.Where(s => s.IsChecked).Select(s => s.CreateSymbolKindOrTypeKind()).ToList(),
                AccessibilityList.Where(a => a.IsChecked).Select(a => new SymbolSpecification.AccessibilityKind(a._accessibility)).ToList(),
                ModifierList.Where(m => m.IsChecked).Select(m => new SymbolSpecification.ModifierKind(m._modifier)).ToList(),
                CustomTagList.Where(t => t.IsChecked).Select(t => t.Name).ToList());
        }

        internal interface ISymbolSpecificationViewModelPart
        {
            bool IsChecked { get; set; }
        }

        public class SymbolKindViewModel : AbstractNotifyPropertyChanged, ISymbolSpecificationViewModelPart
        {
            public string Name { get; set; }
            public bool IsChecked
            {
                get { return _isChecked; }
                set { SetProperty(ref _isChecked, value); }
            }

            private readonly SymbolKind? _symbolKind;
            private readonly TypeKind? _typeKind;

            private bool _isChecked;

            public SymbolKindViewModel(SymbolKind symbolKind, string name, SymbolSpecification specification)
            {
                this._symbolKind = symbolKind;
                Name = name;
                IsChecked = specification.ApplicableSymbolKindList.Any(k => k.SymbolKind == symbolKind);
            }

            public SymbolKindViewModel(TypeKind typeKind, string name, SymbolSpecification specification)
            {
                this._typeKind = typeKind;
                Name = name;
                IsChecked = specification.ApplicableSymbolKindList.Any(k => k.TypeKind == typeKind);
            }

            internal SymbolSpecification.SymbolKindOrTypeKind CreateSymbolKindOrTypeKind()
            {
                if (_symbolKind.HasValue)
                {
                    return new SymbolSpecification.SymbolKindOrTypeKind(_symbolKind.Value);
                }
                else
                {
                    return new SymbolSpecification.SymbolKindOrTypeKind(_typeKind.Value);
                }
            }
        }

        public class AccessibilityViewModel: AbstractNotifyPropertyChanged, ISymbolSpecificationViewModelPart
        {
            internal readonly Accessibility _accessibility;

            public string Name { get; set; }

            private bool _isChecked;
            public bool IsChecked
            {
                get { return _isChecked; }
                set { SetProperty(ref _isChecked, value); }
            }

            public AccessibilityViewModel(Accessibility accessibility, string name, SymbolSpecification specification)
            {
                _accessibility = accessibility;
                Name = name;

                IsChecked = specification.ApplicableAccessibilityList.Any(a => a.Accessibility == accessibility);
            }
        }

        public class ModifierViewModel: AbstractNotifyPropertyChanged, ISymbolSpecificationViewModelPart
        {
            public string Name { get; set; }

            private bool _isChecked;
            public bool IsChecked
            {
                get { return _isChecked; }
                set { SetProperty(ref _isChecked, value); }
            }

            internal readonly DeclarationModifiers _modifier;

            public ModifierViewModel(DeclarationModifiers modifier, string name, SymbolSpecification specification)
            {
                this._modifier = modifier;
                Name = name;
                IsChecked = specification.RequiredModifierList.Any(m => m.Modifier == modifier);
            }
        }

        public class CustomTagViewModel : AbstractNotifyPropertyChanged, ISymbolSpecificationViewModelPart
        {
            public string Name { get; set; }

            private bool _isChecked;
            public bool IsChecked
            {
                get { return _isChecked; }
                set { SetProperty(ref _isChecked, value); }
            }

            public CustomTagViewModel(string name, SymbolSpecification specification)
            {
                Name = name;
                IsChecked = specification.RequiredCustomTagList.Contains(name);
            }
        }

        internal bool TrySubmit()
        {
            if (string.IsNullOrWhiteSpace(SymbolSpecName))
            {
                _notificationService.SendNotification(ServicesVSResources.EnterATitleForThisSymbolSpecification);
                return false;
            }

            return true;
        }
    }
}
