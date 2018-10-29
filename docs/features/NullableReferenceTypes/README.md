## Testing `features/NullableReferenceTypes` in Visual Studio 2017
_Install is available for Visual Studio versions 15.3 and 15.4 only. Installing on 15.5 or later is **not** supported currently._

### Installing Roslyn extension
1. Start Visual Studio 2017
2. Go to Tools: Options: Extensions and Updates

![tools-options](https://user-images.githubusercontent.com/10732005/30494467-10c33ff8-99fd-11e7-915c-7f8d7b038fdc.png)

3. Click Add and enter:

    Name: Roslyn Nullable

    URL:  https://dotnet.myget.org/F/roslyn-nonnull/vsix/

4. Use the checkbox at the top to enable or disable automatically updating the extension when Visual Studio is started
5. Click Apply and OK
6. Go to Tools: Extensions and Updates

![tools-extensions-and-updates](https://user-images.githubusercontent.com/10732005/30494782-53f7a5a6-99fe-11e7-9c1f-3b11f4a321d8.png)

7. Choose Online: Roslyn Nullable: Roslyn Insiders for VS next
8. Click Download and Close
9. Close Visual Studio
10. Click Modify on the VSIX Installer that starts automatically

![vsix-installer](https://user-images.githubusercontent.com/10732005/30494890-bb75d504-99fe-11e7-8b3d-61545aa8e5da.png)

11. After the install completes, click Close
12. Start Visual Studio

The extension should be installed.

### Uninstalling Roslyn extension
1. Start Visual Studio 2017
2. Go to Tools: Extensions and Updates
3. Search for Roslyn

![uninstall-extension](https://user-images.githubusercontent.com/10732005/30495242-24e4271a-9a00-11e7-988b-ed71af8ba719.png)

4. Click Uninstall on Roslyn Insiders for VS next
5. Click Close
6. Close Visual Studio

The extension should be uninstalled.
