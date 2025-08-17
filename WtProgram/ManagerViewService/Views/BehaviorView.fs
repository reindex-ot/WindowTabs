namespace Bemo
open System
open System.Drawing
open System.IO
open System.Windows.Forms
open Bemo.Win32
open Bemo.Win32.Forms
open System.Resources
open System.Reflection


type HotKeyView() =
    let settingsProperty name =
        {
            new IProperty<'a> with
                member x.value
                    with get() = unbox<'a>(Services.settings.getValue(name))
                    and set(value) = Services.settings.setValue(name, box(value))
        }
        
    let resources = new ResourceManager("Properties.Resources", Assembly.GetExecutingAssembly());

    let switchTabs =
        let hotKeys = List2([
            ("nextTab", "nextTab")
            ("prevTab", "prevTab")
        ])

        let editors = hotKeys.enumerate.fold (Map2()) <| fun editors (i,(key, text)) ->
            let caption = resources.GetString text
            let label = UIHelper.label caption
            let editor = HotKeyEditor() :> IPropEditor
            editor.control.Margin <- Padding(0,5,0,5)
            label.Margin <- Padding(0,5,0,5)
            editors.add key editor

        hotKeys.iter <| fun (key,_) ->
            let editor = editors.find key
            editor.value <- Services.program.getHotKey(key)
            editor.changed.Add <| fun() ->
                Services.program.setHotKey key (unbox<int>(editor.value))
        
        let checkBox (prop:IProperty<bool>) = 
            let checkbox = BoolEditor() :> IPropEditor
            checkbox.value <- box(prop.value)
            checkbox.changed.Add <| fun() -> prop.value <- unbox<bool>(checkbox.value)
            checkbox.control

        let settingsCheckbox key = checkBox(settingsProperty(key))
        
        let defaultTabPositionCombo = 
            let combo = new ComboBox()
            combo.DropDownStyle <- ComboBoxStyle.DropDownList
            combo.Items.AddRange([|
                resources.GetString("AlignLeft")
                resources.GetString("AlignCenter") 
                resources.GetString("AlignRight")
            |])
            let positionToIndex = function
                | "left" -> 0
                | "center" -> 1
                | _ -> 2  // default to right
            let indexToPosition = function
                | 0 -> "left"
                | 1 -> "center"
                | _ -> "right"
            combo.SelectedIndex <- positionToIndex(Services.settings.getValue("defaultTabPosition") :?> string)
            combo.SelectedIndexChanged.Add(fun _ -> 
                Services.settings.setValue("defaultTabPosition", indexToPosition(combo.SelectedIndex))
            )
            combo

        let fields = hotKeys.map <| fun(key,text) ->
            let editor = editors.find key
            text, editor.control

        let fields = fields.prependList(List2([
            ("runAtStartup", settingsCheckbox "runAtStartup")
            ("hideInactiveTabs", settingsCheckbox "hideInactiveTabs")
            ("isTabbingEnabledForAllProcessesByDefault", checkBox(prop<IFilterService, bool>(Services.filter, "isTabbingEnabledForAllProcessesByDefault")))
            ("enableCtrlNumberHotKey", settingsCheckbox "enableCtrlNumberHotKey")
            ("enableHoverActivate", settingsCheckbox "enableHoverActivate")
            ("makeTabsNarrowerByDefault", settingsCheckbox "makeTabsNarrowerByDefault")
            ("defaultTabPosition", defaultTabPositionCombo :> Control)
            ("hideTabsWhenInsideByDefault", settingsCheckbox "hideTabsWhenInsideByDefault")
        ]))

        "Switch Tabs", UIHelper.form fields

    let sections = List2([
        switchTabs
        ])

    let table = 
        // Remove GroupBox border for Switch Tabs section
        let (_,control) = sections.head
        control.Dock <- DockStyle.Fill
        // Add padding to match Appearance tab
        control.Padding <- Padding(10)
        control

    interface ISettingsView with
        member x.key = SettingsViewType.HotKeySettings
        member x.title = resources.GetString("Behavior")
        member x.control = table :> Control

