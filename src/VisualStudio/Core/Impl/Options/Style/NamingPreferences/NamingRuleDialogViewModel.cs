// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    internal partial class NamingRuleDialogViewModel : AbstractNotifyPropertyChanged
    {
        private readonly INotificationService _notificationService;

        public NamingRuleDialogViewModel(
            string title,
            SymbolSpecificationViewModel symbolSpecification,
            IList<SymbolSpecificationViewModel> symbolSpecificationList,
            NamingStyleViewModel namingStyle,
            IList<NamingStyleViewModel> namingStyleList,
            NamingRuleTreeItemViewModel parent,
            IList<NamingRuleTreeItemViewModel> allowableParentList,
            EnforcementLevel enforcementLevel,
            INotificationService notificationService)
        {
            this._notificationService = notificationService;

            this._title = title;
            
            this._symbolSpecificationList = new CollectionView(symbolSpecificationList);
            this._selectedSymbolSpecificationIndex = symbolSpecificationList.IndexOf(symbolSpecification);

            this._namingStyleList = new CollectionView(namingStyleList);
            this._namingStyleIndex = namingStyleList.IndexOf(namingStyle);

            allowableParentList.Insert(0, new NamingRuleTreeItemViewModel("-- None --"));
            this._parentRuleList = new CollectionView(allowableParentList);
            this._parentRuleIndex = parent != null ? allowableParentList.IndexOf(parent) : 0;
            if (_parentRuleIndex < 0)
            {
                _parentRuleIndex = 0;
            }

            _enforcementLevelsList = new List<EnforcementLevel>
                {
                    new EnforcementLevel(DiagnosticSeverity.Hidden),
                    new EnforcementLevel(DiagnosticSeverity.Info),
                    new EnforcementLevel(DiagnosticSeverity.Warning),
                    new EnforcementLevel(DiagnosticSeverity.Error),
                };

            _enforcementLevelIndex = _enforcementLevelsList.IndexOf(_enforcementLevelsList.Single(e => e.Value == enforcementLevel.Value));
        }

        private string _title;
        public string Title
        {
            get { return this._title; }
            set
            {
                this.SetProperty(ref this._title, value);
            }
        }

        private CollectionView _symbolSpecificationList;
        public CollectionView SymbolSpecificationList
        {
            get { return _symbolSpecificationList; }
        }

        private int _selectedSymbolSpecificationIndex;
        public int SelectedSymbolSpecificationIndex
        {
            get
            {
                return _selectedSymbolSpecificationIndex;
            }
            set
            {
                SetProperty(ref _selectedSymbolSpecificationIndex, value);
            }
        }

        private CollectionView _namingStyleList;
        public CollectionView NamingStyleList
        {
            get { return _namingStyleList; }
        }

        private int _namingStyleIndex;
        public int NamingStyleIndex
        {
            get
            {
                return _namingStyleIndex;
            }
            set
            {
                SetProperty(ref _namingStyleIndex, value);
            }
        }

        private NamingRuleTreeItemViewModel _parentRule;

        private CollectionView _parentRuleList;
        public CollectionView ParentRuleList
        {
            get { return _parentRuleList; }
        }

        private int _parentRuleIndex;
        public int ParentRuleIndex
        {
            get
            {
                return _parentRuleIndex;
            }
            set
            {
                _parentRuleIndex = value;
            }
        }

        public NamingRuleTreeItemViewModel Parent
        {
            get
            {
                return this._parentRule;
            }
            private set
            {
                this.SetProperty(ref this._parentRule, value);
            }
        }

        private IList<EnforcementLevel> _enforcementLevelsList;
        public IList<EnforcementLevel> EnforcementLevelsList
        {
            get
            {
                return _enforcementLevelsList;
            }
        }

        private int _enforcementLevelIndex;
        public int EnforcementLevelIndex
        {
            get
            {
                return _enforcementLevelIndex;
            }
            set
            {
                _enforcementLevelIndex = value;
            }
        }

        internal bool TrySubmit()
        {
            if (_selectedSymbolSpecificationIndex < 0 || _namingStyleIndex < 0)
            {
                SendFailureNotification(ServicesVSResources.ChooseASymbolSpecificationAndNamingStyle);
                return false;
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                SendFailureNotification(ServicesVSResources.EnterATitleForThisNamingRule);
                return false;
            }
            
            return true;
        }

        private void SendFailureNotification(string message)
        {
            _notificationService.SendNotification(message, severity: NotificationSeverity.Information);
        }
    }
}
