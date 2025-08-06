namespace Bemo
open System
open System.Drawing
open System.IO
open System.Windows.Forms
open Bemo.Win32.Forms

module DesktopManagerFormState =
    let mutable currentForm : Form option = None

type DesktopManagerForm() =
    let title = sprintf "WindowTabs Settings (version %s)"  (Services.program.version)
    let tabs = List2([
        ProgramView() :> ISettingsView
        AppearanceView() :> ISettingsView
        HotKeyView() :> ISettingsView
        WorkspaceView() :> ISettingsView
        DiagnosticsView() :> ISettingsView
        ])
    let tabControl : TabControl = {
        new TabControl() with
            override this.OnKeyDown(e:KeyEventArgs) =
                if (e.KeyData = (Keys.Control ||| Keys.PageDown) ||
                    e.KeyData = (Keys.Control  ||| Keys.PageUp)) then
                    ()
                else
                    base.OnKeyDown(e)
        }

    let form = 
        let form = Form()
        tabs.iter <| fun view ->
            let page = TabPage(view.title)
            let control = view.control
            control.Dock <- DockStyle.Fill
            page.Controls.Add(control)
            page.Dock <- DockStyle.Fill
            tabControl.TabPages.Add(page)
        tabControl.Dock <- DockStyle.Fill
        form.Controls.Add(tabControl)
        form.FormBorderStyle <- FormBorderStyle.SizableToolWindow
        form.StartPosition <- FormStartPosition.CenterScreen
        form.Size <- Size(800, 600)
        form.Text <- title
        form.Icon <- Services.openIcon("Bemo.ico")
        form.TopMost <- true
        form.FormClosed.Add(fun _ -> DesktopManagerFormState.currentForm <- None)
        form

    member this.show() =
        DesktopManagerFormState.currentForm <- Some(form)
        form.Show()
        form.Activate()

    member this.showView(view) =
        let tabIndex = tabs.findIndex(fun tab -> tab.key = view)
        tabControl.SelectedIndex <- tabIndex
        DesktopManagerFormState.currentForm <- Some(form)
        form.Show()
        form.Activate()
        
    member this.close() =
        form.Close()
        DesktopManagerFormState.currentForm <- None
        
