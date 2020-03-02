// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.IO;
using System;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.Controls
{
    internal class MoveToNewTypeControlViewModel : AbstractNotifyPropertyChanged, ISelectDestinationViewModel
    {
        private readonly string _fileExtension;
        private readonly INamedTypeSymbol _originalSymbol;
        private readonly IEnumerable<string> _conflictingTypeNames;

        public MoveToNewTypeControlViewModel(bool isInterface, INamedTypeSymbol targetType, string fileExtension)
        {
            _isInterface = isInterface;
            _fileExtension = fileExtension;
            _originalSymbol = targetType;

            var targetTypeIsInterface = targetType.TypeKind == TypeKind.Interface;
            CanMoveToClass = !targetTypeIsInterface;

            _conflictingTypeNames = GetTypes(targetType.ContainingNamespace).Select(t => t.Name);
            DefaultInterfaceName = NameGenerator.GenerateUniqueInterfaceName(targetType.Name, targetTypeIsInterface, name => !_conflictingTypeNames.Contains(name));
            DefaultClassName = NameGenerator.GenerateBaseTypeName(targetType.Name, name => !_conflictingTypeNames.Contains(name));

            // use public property to trigger dependent property changes as well
            TypeName = targetTypeIsInterface ? DefaultClassName : DefaultInterfaceName;

            UpdateIsValid();
        }

        private static IEnumerable<INamedTypeSymbol> GetTypes(INamespaceOrTypeSymbol symbol)
        {
            var stack = new Stack<INamespaceOrTypeSymbol>();
            stack.Push(symbol);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current is INamespaceSymbol currentNs)
                {
                    stack.Push(currentNs.GetMembers());
                }
                else
                {
                    var namedType = (INamedTypeSymbol)current;
                    stack.Push(namedType.GetTypeMembers());
                    yield return namedType;
                }
            }
        }

        private void UpdateIsValid()
        {
            IsValid = CalculateIsValid();

            bool CalculateIsValid()
            {
                var trimmedTypeName = TypeName.Trim();
                var trimmedFileName = FileName.Trim();

                if (_conflictingTypeNames.Contains(trimmedTypeName))
                {
                    return false;
                }

                if (trimmedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    return false;
                }

                if (!Path.GetExtension(trimmedFileName).Equals(_fileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            }
        }

        public bool CanMoveToClass { get; }
        public string DefaultInterfaceName { get; }
        public string DefaultClassName { get; }

        private bool _isValid;
        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value);
        }

        private SymbolDestination _newSymbolDestination;
        public SymbolDestination NewSymbolDestination
        {
            get => _newSymbolDestination;
            set => SetProperty(ref _newSymbolDestination, value);
        }

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set
            {
                if (SetProperty(ref _fileName, value))
                {
                    UpdateIsValid();
                }
            }
        }

        private string _typeName;
        public string TypeName
        {
            get => _typeName;
            set
            {
                if (SetProperty(ref _typeName, value))
                {
                    var fileName = string.Format("{0}{1}", _typeName.Trim(), _fileExtension);
                    SetProperty(ref _fileName, fileName, nameof(FileName));

                    SelectedDestination = CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
                        attributes: default,
                        accessibility: _originalSymbol.DeclaredAccessibility,
                        modifiers: default,
                        typeKind: IsInterface ? TypeKind.Interface : TypeKind.Class,
                        name: _fileExtension,
                        baseType: IsInterface ? null : _originalSymbol.BaseType);

                    UpdateIsValid();
                }
            }
        }

        private bool _isInterface;
        public bool IsInterface
        {
            get => _isInterface;
            set => SetProperty(ref _isInterface, value);
        }

        private INamedTypeSymbol _selectedDestination;
        public INamedTypeSymbol SelectedDestination
        {
            get => _selectedDestination;
            set => SetProperty(ref _selectedDestination, value);
        }

        public UserControl CreateUserControl()
            => new MoveToNewTypeControl(this);
    }
}
