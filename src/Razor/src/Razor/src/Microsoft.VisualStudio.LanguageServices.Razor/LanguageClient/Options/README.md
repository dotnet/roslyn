## Adding A New Option To Storage

As of writing this, options are in a limbo between the "legacy" and "unified" settings storage. The unified settings are contained in "razor.registration.json", see [internal documentation](https://devdiv.visualstudio.com/DevDiv/_wiki/wikis/DevDiv.wiki/38111/Guide-for-setting-owners)
for information on the schema for unified settings. When adding a new setting to advanced options, the storage has to account for both locations. The way this is done is that all unified settings have a migration
in place to update the old setting location. That way, our OptionsStorage only needs to look at the legacy location to retrieve values. Writing of values in unified settings is handled automatically, and we subscribe
to those changes being made. Follow the following steps when adding a new setting

1. Add the setting to SettingsNames.cs it should include the LegacyName and UnifiedName
2. Add a field to OptionsStorage.cs using the LegacyName
3. Add to the razor.registration.json file to include in the unified settings experience


## Testing

You can test both the legacy and unified setting if you're an internal user. Make sure that the unified settings feature flag is enabled and restart. Then when you open options
you should get the unified experience. Test and make sure your setting works as expected for this experience. You can also click the "Advanced" section in unified settings and it will
open the legacy page to test with.