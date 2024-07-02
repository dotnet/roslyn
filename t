[1mdiff --git a/src/EditorFeatures/Core.Wpf/InlineRename/UI/SmartRename/SmartRenameViewModel.cs b/src/EditorFeatures/Core.Wpf/InlineRename/UI/SmartRename/SmartRenameViewModel.cs[m
[1mindex 919b8cc1dc7..93292d50c83 100644[m
[1m--- a/src/EditorFeatures/Core.Wpf/InlineRename/UI/SmartRename/SmartRenameViewModel.cs[m
[1m+++ b/src/EditorFeatures/Core.Wpf/InlineRename/UI/SmartRename/SmartRenameViewModel.cs[m
[36m@@ -73 +73 @@[m [minternal sealed partial class SmartRenameViewModel : INotifyPropertyChanged, IDi[m
[31m-    public bool IsUsingContext { get; }[m
[32m+[m[32m    public bool IsUsingSemanticContext { get; }[m
[36m@@ -130 +130 @@[m [mpublic SmartRenameViewModel([m
[31m-        this.IsUsingContext = _globalOptionService.GetOption(InlineRenameUIOptionsStorage.GetSuggestionsContext);[m
[32m+[m[32m        this.IsUsingSemanticContext = _globalOptionService.GetOption(InlineRenameUIOptionsStorage.GetSuggestionsContext);[m
[36m@@ -162,3 +162,2 @@[m [mprivate async Task GetSuggestionsTaskAsync(bool isAutomaticOnInitialization, Can[m
[31m-            // ConfigureAwait(true) to stay on the UI thread;[m
[31m-            // WPF view is bound to _smartRenameSession properties and so they must be updated on the UI thread.[m
[31m-            await Task.Delay(_smartRenameSession.AutomaticFetchDelay, cancellationToken).ConfigureAwait(true);[m
[32m+[m[32m            await Task.Delay(_smartRenameSession.AutomaticFetchDelay, cancellationToken)[m
[32m+[m[32m                .ConfigureAwait(false);[m
[36m@@ -172 +171 @@[m [mprivate async Task GetSuggestionsTaskAsync(bool isAutomaticOnInitialization, Can[m
[31m-        if (IsUsingContext)[m
[32m+[m[32m        if (IsUsingSemanticContext)[m
[36m@@ -180 +179 @@[m [mprivate async Task GetSuggestionsTaskAsync(bool isAutomaticOnInitialization, Can[m
[31m-                    .ConfigureAwait(true);[m
[32m+[m[32m                    .ConfigureAwait(false);[m
[36m@@ -182 +181 @@[m [mprivate async Task GetSuggestionsTaskAsync(bool isAutomaticOnInitialization, Can[m
[31m-                    .ConfigureAwait(true);[m
[32m+[m[32m                    .ConfigureAwait(false);[m
[36m@@ -189 +188 @@[m [mprivate async Task GetSuggestionsTaskAsync(bool isAutomaticOnInitialization, Can[m
[31m-                .ConfigureAwait(true);[m
[32m+[m[32m                .ConfigureAwait(false);[m
[36m@@ -194 +193 @@[m [mprivate async Task GetSuggestionsTaskAsync(bool isAutomaticOnInitialization, Can[m
[31m-                .ConfigureAwait(true);[m
[32m+[m[32m                .ConfigureAwait(false);[m
[36m@@ -200,3 +199 @@[m [mprivate void SessionPropertyChanged(object sender, PropertyChangedEventArgs e)[m
[31m-        _threadingContext.ThrowIfNotOnUIThread();[m
[31m-        // _smartRenameSession.SuggestedNames is a normal list. We need to convert it to ObservableCollection to bind to UI Element.[m
[31m-        if (e.PropertyName == nameof(_smartRenameSession.SuggestedNames))[m
[32m+[m[32m        _ = _threadingContext.JoinableTaskFactory.RunAsync(async () =>[m
[36m@@ -204 +201 @@[m [mprivate void SessionPropertyChanged(object sender, PropertyChangedEventArgs e)[m
[31m-            var textInputBackup = BaseViewModel.IdentifierText;[m
[32m+[m[32m            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();[m
[36m@@ -206,3 +203,2 @@[m [mprivate void SessionPropertyChanged(object sender, PropertyChangedEventArgs e)[m
[31m-            SuggestedNames.Clear();[m
[31m-            // Set limit of 3 results[m
[31m-            foreach (var name in _smartRenameSession.SuggestedNames.Take(3))[m
[32m+[m[32m            // _smartRenameSession.SuggestedNames is a normal list. We need to convert it to ObservableCollection to bind to UI Element.[m
[32m+[m[32m            if (e.PropertyName == nameof(_smartRenameSession.SuggestedNames))[m
[36m@@ -210 +206,14 @@[m [mprivate void SessionPropertyChanged(object sender, PropertyChangedEventArgs e)[m
[31m-                SuggestedNames.Add(name);[m
[32m+[m[32m                var textInputBackup = BaseViewModel.IdentifierText;[m
[32m+[m
[32m+[m[32m                SuggestedNames.Clear();[m
[32m+[m[32m                // Set limit of 3 results[m
[32m+[m[32m                foreach (var name in _smartRenameSession.SuggestedNames.Take(3))[m
[32m+[m[32m                {[m
[32m+[m[32m                    SuggestedNames.Add(name);[m
[32m+[m[32m                }[m
[32m+[m
[32m+[m[32m                // Changing the list may have changed the text in the text box. We need to restore it.[m
[32m+[m[32m                BaseViewModel.IdentifierText = textInputBackup;[m
[32m+[m
[32m+[m[32m                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSuggestionsPanelExpanded)));[m
[32m+[m[32m                return;[m
[36m@@ -213,9 +222,3 @@[m [mprivate void SessionPropertyChanged(object sender, PropertyChangedEventArgs e)[m
[31m-            // Changing the list may have changed the text in the text box. We need to restore it.[m
[31m-            BaseViewModel.IdentifierText = textInputBackup;[m
[31m-[m
[31m-            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSuggestionsPanelExpanded)));[m
[31m-            return;[m
[31m-        }[m
[31m-[m
[31m-        // For the rest of the property, like HasSuggestions, IsAvailable and etc. Just forward it has changed to subscriber[m
[31m-        PropertyChanged?.Invoke(this, e);[m
[32m+[m[32m            // For the rest of the property, like HasSuggestions, IsAvailable and etc. Just forward it has changed to subscriber[m
[32m+[m[32m            PropertyChanged?.Invoke(this, e);[m
[32m+[m[32m        });[m
