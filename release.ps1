dotnet publish Flow.Launcher.Plugin.Zoxide -c Release -r win-x64 --no-self-contained
Compress-Archive -Path "Flow.Launcher.Plugin.Zoxide/bin/Release/win-x64/publish/*" -DestinationPath "Flow.Launcher.Plugin.Zoxide/bin/Zoxide.zip" -Force
