namespace Bemo
open System
open System.Windows.Forms
open System.Reflection
open System.Resources
open Newtonsoft.Json.Linq
open System.Diagnostics
open System.IO

type NotifyIconPlugin() as this =
    let Cell = CellScope()
    
    let resources = new ResourceManager("Properties.Resources", Assembly.GetExecutingAssembly());

    member this.icon = Cell.cacheProp this <| fun() ->
        let notifyIcon = new NotifyIcon()
        notifyIcon.Visible <- true
        notifyIcon.Text <- "WindowTabs (version " + Services.program.version + ")"
        notifyIcon.Icon <- Services.openIcon("Bemo.ico")
        notifyIcon.ContextMenu <- new ContextMenu()
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
                let result = MessageBox.Show("Language will be changed to English.\nThe application will restart now.", "Language Change", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)
                if result = DialogResult.OK then
                    let timer = new Timer()
                    timer.Interval <- 100
                    timer.Tick.Add(fun _ ->
                        timer.Stop()
                        let exePath = Assembly.GetExecutingAssembly().Location
                        try
                            Process.Start(exePath) |> ignore
                        with
                        | _ -> ()
                        Environment.Exit(0)
                    )
                    timer.Start()
            with
            | ex -> MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error) |> ignore
            
        let japaneseItem = new MenuItem("日本語")
        japaneseItem.Checked <- (currentLanguage = "ja")
        japaneseItem.Click.Add <| fun _ ->
            try
                let json = Services.settings.root
                json.["language"] <- JToken.FromObject("ja")
                Services.settings.root <- json
                let result = MessageBox.Show("言語が日本語に変更されます。\nアプリケーションを再起動します。", "言語変更", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)
                if result = DialogResult.OK then
                    let timer = new Timer()
                    timer.Interval <- 100
                    timer.Tick.Add(fun _ ->
                        timer.Stop()
                        let exePath = Assembly.GetExecutingAssembly().Location
                        try
                            Process.Start(exePath) |> ignore
                        with
                        | _ -> ()
                        Environment.Exit(0)
                    )
                    timer.Start()
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
            this.addItem(resources.GetString("CloseWindowTabs"), fun() -> Services.program.shutdown())
            Services.program.newVersion.Add this.onNewVersion

    interface IDisposable with
        member this.Dispose() = this.icon.Dispose()