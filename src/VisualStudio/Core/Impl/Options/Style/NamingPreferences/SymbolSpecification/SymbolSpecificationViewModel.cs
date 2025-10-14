// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences;

internal sealed class SymbolSpecificationViewModel : AbstractNotifyPropertyChanged, INamingStylesInfoDialogViewModel
{
    public Guid ID { get; set; }
    public List<SymbolKindViewModel> SymbolKindList { get; set; }
    public List<AccessibilityViewModel> AccessibilityList { get; set; }
    public List<ModifierViewModel> ModifierList { get; set; }

    private string _symbolSpecName;

    public bool CanBeDeleted { get; set; }

    private readonly INotificationService _notificationService;

    public SymbolSpecificationViewModel(
        string languageName,
        bool canBeDeleted,
        INotificationService notificationService) : this(languageName, CreateDefaultSymbolSpecification(), canBeDeleted, notificationService) { }

    public SymbolSpecificationViewModel(string languageName, SymbolSpecification specification, bool canBeDeleted, INotificationService notificationService)
    {
        CanBeDeleted = canBeDeleted;
        _notificationService = notificationService;
        ItemName = specification.Name;
        ID = specification.ID;

        // The list of supported SymbolKinds is limited due to https://github.com/dotnet/roslyn/issues/8753.
        if (languageName == LanguageNames.CSharp)
        {
            SymbolKindList =
            [
                new SymbolKindViewModel(SymbolKind.Namespace, ServicesVSResources.NamingSpecification_CSharp_Namespace, specification),
                new SymbolKindViewModel(TypeKind.Class, ServicesVSResources.NamingSpecification_CSharp_Class, specification),
                new SymbolKindViewModel(TypeKind.Struct, ServicesVSResources.NamingSpecification_CSharp_Struct, specification),
                new SymbolKindViewModel(TypeKind.Interface, ServicesVSResources.NamingSpecification_CSharp_Interface, specification),
                new SymbolKindViewModel(TypeKind.Enum, ServicesVSResources.NamingSpecification_CSharp_Enum, specification),
                new SymbolKindViewModel(SymbolKind.Property, ServicesVSResources.NamingSpecification_CSharp_Property, specification),
                new SymbolKindViewModel(MethodKind.Ordinary, ServicesVSResources.NamingSpecification_CSharp_Method, specification),
                new SymbolKindViewModel(MethodKind.LocalFunction, ServicesVSResources.NamingSpecification_CSharp_LocalFunction, specification),
                new SymbolKindViewModel(SymbolKind.Field, ServicesVSResources.NamingSpecification_CSharp_Field, specification),
                new SymbolKindViewModel(SymbolKind.Event, ServicesVSResources.NamingSpecification_CSharp_Event, specification),
                new SymbolKindViewModel(TypeKind.Delegate, ServicesVSResources.NamingSpecification_CSharp_Delegate, specification),
                new SymbolKindViewModel(SymbolKind.Parameter, ServicesVSResources.NamingSpecification_CSharp_Parameter, specification),
                new SymbolKindViewModel(SymbolKind.TypeParameter, ServicesVSResources.NamingSpecification_CSharp_TypeParameter, specification),
                new SymbolKindViewModel(SymbolKind.Local, ServicesVSResources.NamingSpecification_CSharp_Local, specification)
            ];

            // Not localized because they're language keywords
            AccessibilityList =
            [
                new AccessibilityViewModel(Accessibility.Public, "public", specification),
                new AccessibilityViewModel(Accessibility.Internal, "internal", specification),
                new AccessibilityViewModel(Accessibility.Private, "private", specification),
                new AccessibilityViewModel(Accessibility.Protected, "protected", specification),
                new AccessibilityViewModel(Accessibility.ProtectedOrInternal, "protected internal", specification),
                new AccessibilityViewModel(Accessibility.ProtectedAndInternal, "private protected", specification),
                new AccessibilityViewModel(Accessibility.NotApplicable, "local", specification),
            ];

            // Not localized because they're language keywords
            ModifierList =
            [
                new ModifierViewModel(DeclarationModifiers.Abstract, "abstract", specification),
                new ModifierViewModel(DeclarationModifiers.Async, "async", specification),
                new ModifierViewModel(DeclarationModifiers.Const, "const", specification),
                new ModifierViewModel(DeclarationModifiers.ReadOnly, "readonly", specification),
                new ModifierViewModel(DeclarationModifiers.Static, "static", specification)
            ];
        }
        else if (languageName == LanguageNames.VisualBasic)
        {
            SymbolKindList =
            [
                new SymbolKindViewModel(SymbolKind.Namespace, ServicesVSResources.NamingSpecification_VisualBasic_Namespace, specification),
                new SymbolKindViewModel(TypeKind.Class, ServicesVSResources.NamingSpecification_VisualBasic_Class, specification),
                new SymbolKindViewModel(TypeKind.Struct, ServicesVSResources.NamingSpecification_VisualBasic_Structure, specification),
                new SymbolKindViewModel(TypeKind.Interface, ServicesVSResources.NamingSpecification_VisualBasic_Interface, specification),
                new SymbolKindViewModel(TypeKind.Enum, ServicesVSResources.NamingSpecification_VisualBasic_Enum, specification),
                new SymbolKindViewModel(TypeKind.Module, ServicesVSResources.NamingSpecification_VisualBasic_Module, specification),
                new SymbolKindViewModel(SymbolKind.Property, ServicesVSResources.NamingSpecification_VisualBasic_Property, specification),
                new SymbolKindViewModel(MethodKind.Ordinary, ServicesVSResources.NamingSpecification_VisualBasic_Method, specification),
                new SymbolKindViewModel(SymbolKind.Field, ServicesVSResources.NamingSpecification_VisualBasic_Field, specification),
                new SymbolKindViewModel(SymbolKind.Event, ServicesVSResources.NamingSpecification_VisualBasic_Event, specification),
                new SymbolKindViewModel(TypeKind.Delegate, ServicesVSResources.NamingSpecification_VisualBasic_Delegate, specification),
                new SymbolKindViewModel(SymbolKind.Parameter, ServicesVSResources.NamingSpecification_VisualBasic_Parameter, specification),
                new SymbolKindViewModel(SymbolKind.TypeParameter, ServicesVSResources.NamingSpecification_VisualBasic_TypeParameter, specification),
                new SymbolKindViewModel(SymbolKind.Local, ServicesVSResources.NamingSpecification_VisualBasic_Local, specification)
            ];

            // Not localized because they're language keywords
            AccessibilityList =
            [
                new AccessibilityViewModel(Accessibility.Public, "Public", specification),
                new AccessibilityViewModel(Accessibility.Friend, "Friend", specification),
                new AccessibilityViewModel(Accessibility.Private, "Private", specification),
                new AccessibilityViewModel(Accessibility.Protected, "Protected", specification),
                new AccessibilityViewModel(Accessibility.ProtectedOrInternal, "Protected Friend", specification),
                new AccessibilityViewModel(Accessibility.ProtectedAndInternal, "Private Protected", specification),
                new AccessibilityViewModel(Accessibility.NotApplicable, "Local", specification),
            ];

            // Not localized because they're language keywords
            ModifierList =
            [
                new ModifierViewModel(DeclarationModifiers.Abstract, "MustInherit", specification),
                new ModifierViewModel(DeclarationModifiers.Async, "Async", specification),
                new ModifierViewModel(DeclarationModifiers.Const, "Const", specification),
                new ModifierViewModel(DeclarationModifiers.ReadOnly, "ReadOnly", specification),
                new ModifierViewModel(DeclarationModifiers.Static, "Shared", specification)
            ];
        }
        else
        {
            throw new ArgumentException(string.Format("Unexpected language name: {0}", languageName), nameof(languageName));
        }
    }

    public string ItemName
    {
        get { return _symbolSpecName; }
        set { SetProperty(ref _symbolSpecName, value); }
    }

    internal SymbolSpecification GetSymbolSpecification()
    {
        return new SymbolSpecification(
            ID,
            ItemName,
            SymbolKindList.SelectAsArray(s => s.IsChecked, s => s.CreateSymbolOrTypeOrMethodKind()),
            AccessibilityList.SelectAsArray(a => a.IsChecked, a => a._accessibility),
            ModifierList.SelectAsArray(m => m.IsChecked, m => new ModifierKind(m._modifier.Modifiers)));
    }

    internal bool TrySubmit()
    {
        if (string.IsNullOrWhiteSpace(ItemName))
        {
            _notificationService.SendNotification(ServicesVSResources.Enter_a_title_for_this_Naming_Style);
            return false;
        }

        return true;
    }

    // For screen readers
    public override string ToString()
        => _symbolSpecName;

    internal interface ISymbolSpecificationViewModelPart
    {
        bool IsChecked { get; set; }
    }

    public sealed class SymbolKindViewModel : AbstractNotifyPropertyChanged, ISymbolSpecificationViewModelPart
    {
        public string Name { get; set; }
        public bool IsChecked
        {
            get { return _isChecked; }
            set { SetProperty(ref _isChecked, value); }
        }

        private readonly SymbolKind? _symbolKind;
        private readonly TypeKind? _typeKind;
        private readonly MethodKind? _methodKind;

        private bool _isChecked;

        public SymbolKindViewModel(SymbolKind symbolKind, string name, SymbolSpecification specification)
        {
            _symbolKind = symbolKind;
            Name = name;
            IsChecked = specification.ApplicableSymbolKindList.Any(static (k, symbolKind) => k.SymbolKind == symbolKind, symbolKind);
        }

        public SymbolKindViewModel(TypeKind typeKind, string name, SymbolSpecification specification)
        {
            _typeKind = typeKind;
            Name = name;
            IsChecked = specification.ApplicableSymbolKindList.Any(static (k, typeKind) => k.TypeKind == typeKind, typeKind);
        }

        public SymbolKindViewModel(MethodKind methodKind, string name, SymbolSpecification specification)
        {
            _methodKind = methodKind;
            Name = name;
            IsChecked = specification.ApplicableSymbolKindList.Any(static (k, methodKind) => k.MethodKind == methodKind, methodKind);
        }

        internal SymbolKindOrTypeKind CreateSymbolOrTypeOrMethodKind()
        {
            return
                _symbolKind.HasValue ? new SymbolKindOrTypeKind(_symbolKind.Value) :
                _typeKind.HasValue ? new SymbolKindOrTypeKind(_typeKind.Value) :
                _methodKind.HasValue ? new SymbolKindOrTypeKind(_methodKind.Value) :
                throw ExceptionUtilities.Unreachable();
        }
    }

    public sealed class AccessibilityViewModel : AbstractNotifyPropertyChanged, ISymbolSpecificationViewModelPart
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

            IsChecked = specification.ApplicableAccessibilityList.Any(static (a, accessibility) => a == accessibility, accessibility);
        }
    }

    public sealed class ModifierViewModel : AbstractNotifyPropertyChanged, ISymbolSpecificationViewModelPart
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
            _modifier = modifier;
            Name = name;

            IsChecked = specification.RequiredModifierList.Any(static (m, modifier) => m.Modifiers == modifier.Modifiers, modifier);
        }
    }
}
