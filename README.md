# Calendar Dock

System tray calendar (Google + Outlook) with Join buttons, 5-minute toast reminders and a Windows 11 widget.

## Setup
1. Google: follow [Google credentials setup](#google-credentials-setup) below.
2. Microsoft: register an Entra public client app (redirect `http://localhost`, delegated `Calendars.Read`), put ClientId in `appsettings.local.json`.
3. Build/deploy as MSIX (required for widgets and notifications): see [Build and run](#build-and-run).

## Google credentials setup

Credentials are stored in `src/CalendarDock.App/appsettings.local.json` (gitignored). Any non-empty value there overrides `appsettings.json`.

### Step 1: Create a project
1. Open https://console.cloud.google.com and sign in with the Google account whose calendar you want to use.
2. Accept the terms-of-service prompt if one appears.
3. Click the project picker at the top left, next to the Google Cloud logo, then **New Project**.
4. Set **Project name** to `Calendar Dock` and leave Organization and Location at their defaults.
5. Click **Create**, then select the new project from the notification bell.
6. Check that the top bar shows the project name.

### Step 2: Enable the Calendar API
1. Open the left menu and go to **APIs & Services > Library**.
2. Search for `Google Calendar API` and open it.
3. Click **Enable**.

### Step 3: Configure the consent screen
The area is now called **Google Auth Platform**. Older layouts call it **OAuth consent screen**; the fields are the same.

1. Go to **APIs & Services > OAuth consent screen**.
2. If you see **Get started**, click it. If the overview page shows Branding, Audience, Clients and Data Access in the sidebar, it is already configured.
3. **App Information:** set **App name** to `Calendar Dock` and pick your **User support email**, then **Next**.
4. **Audience:** choose **External**, then **Next**.
5. **Contact information:** enter your email, then **Next**.
6. Accept the agreement, then **Continue** and **Create**.
7. Under **Branding**, check that App name and User support email are filled in. Add a developer contact email if missing and **Save**.

### Step 4: Add yourself as a test user
1. In the sidebar, open **Audience**.
2. Check that **Publishing status** is **Testing**.
3. Under **Test users**, click **+ Add users**, enter the Google address you will sign in with, and **Save**.

Without this you get `Error 403: access_denied` at sign-in.

### Step 5: Add the scope
1. In the sidebar, open **Data Access**.
2. Click **Add or remove scopes**.
3. Filter for `calendar.events.readonly`. If it is missing, make sure the Calendar API is enabled (Step 2).
4. Tick the row ending in `.../auth/calendar.events.readonly` ("View events on all your calendars").
5. Click **Update**, then **Save**.

### Step 6: Create the OAuth client
1. In the sidebar, open **Clients** (older layouts: **Credentials > Create credentials > OAuth client ID**).
2. Click **+ Create client**.
3. Set **Application type** to **Desktop app**. Other types require a redirect URI this app does not use.
4. Set **Name** to `Calendar Dock Desktop` and click **Create**.
5. Copy the **Client ID** (ends in `.apps.googleusercontent.com`) and **Client secret** (starts with `GOCSPX-`), or click **Download JSON** as a backup.

### Step 7: Put the values in the app
Edit `src/CalendarDock.App/appsettings.local.json`:

```json
{
  "Google": { "ClientId": "<your id>.apps.googleusercontent.com", "ClientSecret": "GOCSPX-..." },
  "Microsoft": { "ClientId": "" }
}
```

Never commit this file or paste the secret anywhere public.

### First sign-in
- Google shows "Google hasn't verified this app". This is expected in Testing mode. Click **Advanced > Go to Calendar Dock (unsafe) > Continue**.
- In Testing mode refresh tokens expire after 7 days, so you must sign in again weekly. Test users are capped at 100.
- To remove the weekly re-login, use **Audience > Publish app**. The scope is read-only but classed as sensitive, so Google may require verification for other users. That is fine if you are the only user.

### Common errors
| Error | Cause |
|---|---|
| `access_denied` / 403 | You are not added as a test user (Step 4). |
| `redirect_uri_mismatch` | The client type is not **Desktop app** (Step 6). |
| `accessNotConfigured` | The Calendar API is not enabled (Step 2). |

## Build and run

### Prerequisites
- Windows **Developer Mode** on (Settings > System > For developers), required to install the unsigned MSIX.
- Visual Studio with **Desktop development with C++** and **.NET desktop development** workloads. Without the C++ tools, MSIX packaging fails with `GetLatestMSVCVersion ... DirectoryNotFoundException`.

### Build and register (command line)
```
dotnet build src/CalendarDock.App -p:Platform=x64
Add-AppxPackage -Register src\CalendarDock.App\bin\x64\Debug\net10.0-windows10.0.19041.0\AppxManifest.xml
```

The app is registered from the build output folder, so rebuilding changes the installed app. Quit it first (tray icon > Exit) or the build can fail on locked files.

### Run
Search "Calendar Dock" in the Start menu, or:
```
Start-Process "shell:AppsFolder\CalendarDock_4ckkhnxf9bmvc!App"
```
The app lives in the system tray (check the `^` overflow). Left-click the icon for the meetings popup; right-click for Open/Exit.

### Troubleshooting
- **Raw `.exe` crashes with `REGDB_E_CLASSNOTREG`:** the app is packaged and must be registered as above, not run directly.
- **Visual Studio F5 says it does not know the `MsixPackage` profile:** install the WinUI application development workload (Windows App SDK C# extension), or use the command-line steps above.
- **Build fails with `PRI210 0x800704c8` on `resources.pri`:** run `dotnet build-server shutdown`, delete `resources.pri` in the `bin` output folder, and rebuild.
- **App exits at startup:** see `%TEMP%\calendardock-crash.log`.

## Layout
- `CalendarDock.Core` models, join-link extraction, merge/dedupe, reminders
- `CalendarDock.Providers` Google Calendar API + Microsoft Graph (MSAL)
- `CalendarDock.App` WinUI 3 tray app, popup, toasts, widget provider
