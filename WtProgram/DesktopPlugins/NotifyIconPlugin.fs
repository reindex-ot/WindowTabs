namespace Bemo
open System
open System.Windows.Forms
open System.Reflection
open System.Resources
open Newtonsoft.Json.Linq
open System.Diagnostics
open System.IO
open System.Threading

type NotifyIconPlugin() as this =
    let Cell = CellScope()
    
    let closeSettingsDialog() =
        // Try to close any existing settings dialog using mutex check
        let mutable tempMutex : Mutex option = None
        let mutexCreated = ref false
        try
            tempMutex <- Some(new Mutex(true, "WindowTabsSettingsDialog", mutexCreated))
            if not !mutexCreated then
                // Dialog exists in another process, we can't close it directly
                // but we'll clear our local reference
                ()
            // Always release the mutex immediately
            match tempMutex with
            | Some m -> 
                try m.ReleaseMutex(); m.Dispose() with _ -> ()
            | None -> ()
        with _ -> ()
        // Close local form reference if exists
        match DesktopManagerFormState.currentForm with
        | Some form -> 
            try form.Close() with _ -> ()
        | None -> ()
    
    let resources = new ResourceManager("Properties.Resources", Assembly.GetExecutingAssembly());

    member this.icon = Cell.cacheProp this <| fun() ->
        let notifyIcon = new NotifyIcon()
        notifyIcon.Visible <- true
        notifyIcon.Text <- "WindowTabs (version " + Services.program.version + ")"
        notifyIcon.Icon <- Services.openIcon("Bemo.ico")
        let contextMenu = new ContextMenu()

        // Apply dark mode setting when menu is about to be shown
        contextMenu.Popup.Add <| fun _ ->
            let darkModeEnabled =
                try
                    let json = Services.settings.root
                    match json.getBool("enableMenuDarkMode") with
                    | Some(value) -> value
                    | None -> false
                with | _ -> false
            DarkMode.setDarkModeForMenus(darkModeEnabled)

        notifyIcon.ContextMenu <- contextMenu
        notifyIcon.DoubleClick.Add <| fun _ -> Services.managerView.show()
        notifyIcon

    member this.contextMenuItems = this.icon.ContextMenu.MenuItems

    member this.addItem(text, handler) =
        this.contextMenuItems.Add(text, EventHandler(fun obj (e:EventArgs) -> handler())) |> ignore

    member this.onNewVersion() =
        this.icon.ShowBalloonTip(
            1000,
            "A new version is available.",
            "Please visit windowtabs.com to download the latest version.",
            ToolTipIcon.Info
        )

    member this.restartApplication() =
        let exePath = Assembly.GetExecutingAssembly().Location
        // Start new process with a delay using cmd
        let startInfo = ProcessStartInfo()
        startInfo.FileName <- "cmd.exe"
        startInfo.Arguments <- sprintf "/c timeout /t 3 /nobreak >nul && start \"\" \"%s\"" exePath
        startInfo.WindowStyle <- ProcessWindowStyle.Hidden
        startInfo.CreateNoWindow <- true
        try
            Process.Start(startInfo) |> ignore
            Services.program.shutdown()
        with
        | ex -> MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error) |> ignore

    member this.createLanguageMenu() =
        let languageMenu = new MenuItem(resources.GetString("Language"))
        let currentLanguage = 
            try
                let settingsJson = Services.settings.root
                let value = settingsJson.["language"]
                if value = null then "en" else value.ToString()
            with
            | _ -> "en"
        
        let englishItem = new MenuItem("English")
        englishItem.Checked <- (currentLanguage = "en")
        englishItem.Click.Add <| fun _ ->
            try
                let json = Services.settings.root
                json.["language"] <- JToken.FromObject("en")
                Services.settings.root <- json
                closeSettingsDialog()
                let result = MessageBox.Show("Language will be changed to English.\nThe application will restart now.", "Language Change", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)
                if result = DialogResult.OK then
                    this.restartApplication()
            with
            | ex -> MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error) |> ignore
            
        let japaneseItem = new MenuItem("Japanese")
        japaneseItem.Checked <- (currentLanguage = "ja")
        japaneseItem.Click.Add <| fun _ ->
            try
                let json = Services.settings.root
                json.["language"] <- JToken.FromObject("ja")
                Services.settings.root <- json
                closeSettingsDialog()
                let result = MessageBox.Show("Language will be changed to Japanese.\nThe application will restart now.", "Language Change", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)
                if result = DialogResult.OK then
                    this.restartApplication()
            with
            | ex -> MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error) |> ignore
            
        languageMenu.MenuItems.Add(englishItem) |> ignore
        languageMenu.MenuItems.Add(japaneseItem) |> ignore
        languageMenu

    interface IPlugin with
        member this.init() =
            this.addItem(resources.GetString("Settings"), fun() -> Services.managerView.show())
            this.contextMenuItems.Add(this.createLanguageMenu()) |> ignore
            //this.addItem(resources.GetString("Feedback"), Forms.openFeedback) // 404 Not Found.
            this.contextMenuItems.Add("-").ignore
            this.addItem(resources.GetString("RestartWindowTabs"), fun() -> this.restartApplication())
            this.addItem(resources.GetString("CloseWindowTabs"), fun() -> Services.program.shutdown())
            Services.program.newVersion.Add this.onNewVersion

    interface IDisposable with
        member this.Dispose() = this.icon.Dispose()
