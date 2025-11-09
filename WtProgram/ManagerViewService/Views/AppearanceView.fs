namespace Bemo
open System
open System.Drawing
open System.IO
open System.Windows.Forms
open Bemo.Win32
open Bemo.Win32.Forms
open Microsoft.FSharp.Reflection

type AppearanceProperty = {
    displayText : string
    key: string
    propertyType : AppearancePropertyType
    }

and AppearancePropertyType =
    | HotKeyProperty
    | IntProperty
    | ColorProperty

type AppearanceView() as this =
    let settingsPropertyBool name (defaultValue:bool) =
        {
            new IProperty<bool> with
                member x.value
                    with get() =
                        try
                            let json = Services.settings.root
                            match json.getBool(name) with
                            | Some(value) -> value
                            | None -> defaultValue
                        with | _ -> defaultValue
                    and set(value) =
                        let json = Services.settings.root
                        json.setBool(name, value)
                        Services.settings.root <- json
        }

    let colorConfig key displayText =
        { displayText=displayText; key=key; propertyType=ColorProperty }

    let intConfig key displayText =
        { displayText=displayText; key=key; propertyType=IntProperty }

    let hkConfig key displayText =
        { displayText=displayText; key=key; propertyType=HotKeyProperty }
        
    let mutable suppressEvents = false

    let checkBox (prop:IProperty<bool>) =
        let checkbox = BoolEditor() :> IPropEditor
        checkbox.value <- box(prop.value)
        checkbox.changed.Add <| fun() -> prop.value <- unbox<bool>(checkbox.value)
        checkbox.control

    let settingsCheckboxBool key defaultValue = checkBox(settingsPropertyBool key defaultValue)

    let properties = List2([
        intConfig "tabHeight" "Height"
        intConfig "tabMaxWidth" "Max Width"
        intConfig "tabOverlap" "Overlap"
        intConfig "tabIndentNormal" "Indent Normal"
        intConfig "tabIndentFlipped" "Indent Flipped"
        colorConfig "tabTextColor" "Text Color"
        colorConfig "tabNormalBgColor" "Background Normal"
        colorConfig "tabHighlightBgColor" "Background Highlight"
        colorConfig "tabActiveBgColor" "Background Active"
        colorConfig "tabFlashBgColor" "Background Flash"
        colorConfig "tabBorderColor" "Border"
        ])

    let panel =
        let panel = TableLayoutPanel()
        panel.AutoScroll <- true
        panel.Dock <- DockStyle.Fill
        panel.GrowStyle <- TableLayoutPanelGrowStyle.FixedSize
        panel.Padding <- Padding(10)
        panel.RowCount <- properties.length + 2  // +1 for checkbox, +1 for buttons
        List2([0..properties.length + 1]).iter <| fun row ->
            panel.RowStyles.Add(RowStyle(SizeType.Absolute, 35.0f)).ignore
        panel.ColumnCount <- 2
        panel.ColumnStyles.Add(ColumnStyle(SizeType.Absolute, 250.0f)).ignore
        panel.ColumnStyles.Add(ColumnStyle(SizeType.Percent, 100.0f)).ignore
        panel

   
    let editors = properties.enumerate.fold (Map2()) <| fun editors (i,prop) ->
        let label =
            let label = Label()
            label.AutoSize <- true
            label.Text <- Localization.getString(prop.displayText)
            label.TextAlign <- ContentAlignment.MiddleLeft
            label
        let editor = 
            match prop.propertyType with
            | ColorProperty -> ColorEditor() :> IPropEditor
            | IntProperty -> IntEditor() :> IPropEditor
            | HotKeyProperty -> HotKeyEditor() :> IPropEditor

        editor.control.Dock <- DockStyle.Fill
        editor.control.Margin <- Padding(0,5,0,5)
        label.Margin <- Padding(0,5,0,5)
        panel.Controls.Add(label)
        panel.Controls.Add(editor.control)
        panel.SetRow(label, i)
        panel.SetColumn(label, 0)
        panel.SetRow(editor.control, i)
        panel.SetColumn(editor.control, 1)
        editors.add prop.key editor

    let setEditorValues appearance =
        properties.iter <| fun prop ->
            let editor = editors.find prop.key
            try
                editor.value <- Serialize.readField appearance prop.key
            with | _ ->()

    let appearance = Services.program.tabAppearanceInfo

    let font = Font(Localization.getString("Font"), 9f)

    let buttonPanel =
        let container = new FlowLayoutPanel()
        container.FlowDirection <- FlowDirection.LeftToRight
        container.AutoSize <- true
        container.WrapContents <- false
        container.Anchor <- AnchorStyles.Right

        let resetBtn = Button()
        resetBtn.Text <- Localization.getString("Reset")
        resetBtn.Font <- font
        resetBtn.Click.Add <| fun _ ->
            suppressEvents <- true
            let defaultAppearance = Services.program.defaultTabAppearanceInfo
            // Reset only size settings, keep current colors
            let currentAppearance = Services.program.tabAppearanceInfo
            let mergedAppearance = {
                tabHeight = defaultAppearance.tabHeight
                tabMaxWidth = defaultAppearance.tabMaxWidth
                tabOverlap = defaultAppearance.tabOverlap
                tabHeightOffset = defaultAppearance.tabHeightOffset
                tabIndentFlipped = defaultAppearance.tabIndentFlipped
                tabIndentNormal = defaultAppearance.tabIndentNormal
                tabTextColor = currentAppearance.tabTextColor
                tabNormalBgColor = currentAppearance.tabNormalBgColor
                tabHighlightBgColor = currentAppearance.tabHighlightBgColor
                tabActiveBgColor = currentAppearance.tabActiveBgColor
                tabFlashBgColor = currentAppearance.tabFlashBgColor
                tabBorderColor = currentAppearance.tabBorderColor
            }
            Services.settings.setValue("tabAppearance", box(mergedAppearance))
            setEditorValues mergedAppearance
            suppressEvents <- false
        
        let lightBtn = Button()
        lightBtn.Text <- Localization.getString("LightColor")
        lightBtn.Font <- font
        lightBtn.Click.Add <| fun _ ->
            suppressEvents <- true
            let currentAppearance = Services.program.tabAppearanceInfo
            let defaultAppearance = Services.program.defaultTabAppearanceInfo
            // Apply only color settings from default, keep size settings
            let mergedAppearance = {
                tabHeight = currentAppearance.tabHeight
                tabMaxWidth = currentAppearance.tabMaxWidth
                tabOverlap = currentAppearance.tabOverlap
                tabHeightOffset = currentAppearance.tabHeightOffset
                tabIndentFlipped = currentAppearance.tabIndentFlipped
                tabIndentNormal = currentAppearance.tabIndentNormal
                tabTextColor = defaultAppearance.tabTextColor
                tabNormalBgColor = defaultAppearance.tabNormalBgColor
                tabHighlightBgColor = defaultAppearance.tabHighlightBgColor
                tabActiveBgColor = defaultAppearance.tabActiveBgColor
                tabFlashBgColor = defaultAppearance.tabFlashBgColor
                tabBorderColor = defaultAppearance.tabBorderColor
            }
            Services.settings.setValue("tabAppearance", box(mergedAppearance))
            setEditorValues mergedAppearance
            suppressEvents <- false
        
        let darkBtn = Button()
        darkBtn.Text <- Localization.getString("DarkColor")
        darkBtn.Font <- font
        darkBtn.Click.Add <| fun _ ->
            suppressEvents <- true
            let currentAppearance = Services.program.tabAppearanceInfo
            let darkAppearance = Services.program.darkModeTabAppearanceInfo
            // Merge color settings with current size settings
            let mergedAppearance = {
                tabHeight = if darkAppearance.tabHeight = -1 then currentAppearance.tabHeight else darkAppearance.tabHeight
                tabMaxWidth = if darkAppearance.tabMaxWidth = -1 then currentAppearance.tabMaxWidth else darkAppearance.tabMaxWidth
                tabOverlap = if darkAppearance.tabOverlap = -1 then currentAppearance.tabOverlap else darkAppearance.tabOverlap
                tabHeightOffset = if darkAppearance.tabHeightOffset = -1 then currentAppearance.tabHeightOffset else darkAppearance.tabHeightOffset
                tabIndentFlipped = if darkAppearance.tabIndentFlipped = -1 then currentAppearance.tabIndentFlipped else darkAppearance.tabIndentFlipped
                tabIndentNormal = if darkAppearance.tabIndentNormal = -1 then currentAppearance.tabIndentNormal else darkAppearance.tabIndentNormal
                tabTextColor = darkAppearance.tabTextColor
                tabNormalBgColor = darkAppearance.tabNormalBgColor
                tabHighlightBgColor = darkAppearance.tabHighlightBgColor
                tabActiveBgColor = darkAppearance.tabActiveBgColor
                tabFlashBgColor = darkAppearance.tabFlashBgColor
                tabBorderColor = darkAppearance.tabBorderColor
            }
            // Apply settings first
            Services.settings.setValue("tabAppearance", box(mergedAppearance))
            // Then update UI
            setEditorValues mergedAppearance
            suppressEvents <- false

        let darkBlueBtn = Button()
        darkBlueBtn.Text <- Localization.getString("DarkBlueColor")
        darkBlueBtn.Font <- font
        darkBlueBtn.Click.Add <| fun _ ->
            suppressEvents <- true
            let currentAppearance = Services.program.tabAppearanceInfo
            let blueAppearance = Services.program.darkModeBlueTabAppearanceInfo
            // Merge color settings with current size settings
            let mergedAppearance = {
                tabHeight = if blueAppearance.tabHeight = -1 then currentAppearance.tabHeight else blueAppearance.tabHeight
                tabMaxWidth = if blueAppearance.tabMaxWidth = -1 then currentAppearance.tabMaxWidth else blueAppearance.tabMaxWidth
                tabOverlap = if blueAppearance.tabOverlap = -1 then currentAppearance.tabOverlap else blueAppearance.tabOverlap
                tabHeightOffset = if blueAppearance.tabHeightOffset = -1 then currentAppearance.tabHeightOffset else blueAppearance.tabHeightOffset
                tabIndentFlipped = if blueAppearance.tabIndentFlipped = -1 then currentAppearance.tabIndentFlipped else blueAppearance.tabIndentFlipped
                tabIndentNormal = if blueAppearance.tabIndentNormal = -1 then currentAppearance.tabIndentNormal else blueAppearance.tabIndentNormal
                tabTextColor = blueAppearance.tabTextColor
                tabNormalBgColor = blueAppearance.tabNormalBgColor
                tabHighlightBgColor = blueAppearance.tabHighlightBgColor
                tabActiveBgColor = blueAppearance.tabActiveBgColor
                tabFlashBgColor = blueAppearance.tabFlashBgColor
                tabBorderColor = blueAppearance.tabBorderColor
            }
            // Apply settings first
            Services.settings.setValue("tabAppearance", box(mergedAppearance))
            // Then update UI
            setEditorValues mergedAppearance
            suppressEvents <- false
        
        container.Controls.Add(darkBtn)
        container.Controls.Add(darkBlueBtn)
        container.Controls.Add(lightBtn)
        container.Controls.Add(resetBtn)
        container

    do
        // Add dark mode checkbox after border color (last property)
        let darkModeLabel = Label()
        darkModeLabel.AutoSize <- true
        darkModeLabel.Text <- Localization.getString("MenuDarkMode")
        darkModeLabel.TextAlign <- ContentAlignment.MiddleLeft
        darkModeLabel.Margin <- Padding(0,5,0,5)

        let darkModeCheckbox = settingsCheckboxBool "enableMenuDarkMode" false
        darkModeCheckbox.Margin <- Padding(0,5,0,5)

        let checkboxRow = properties.length
        panel.Controls.Add(darkModeLabel)
        panel.Controls.Add(darkModeCheckbox)
        panel.SetRow(darkModeLabel, checkboxRow)
        panel.SetColumn(darkModeLabel, 0)
        panel.SetRow(darkModeCheckbox, checkboxRow)
        panel.SetColumn(darkModeCheckbox, 1)

        // Add button panel
        panel.Controls.Add(buttonPanel)
        let btnRow = properties.length + 1
        panel.SetRow(buttonPanel, btnRow)
        panel.SetColumn(buttonPanel, 1)
        setEditorValues appearance
        editors.items.map(snd).iter <| fun editor ->
            editor.changed.Add <| fun() ->
                if not suppressEvents then
                    this.applyAppearance()
        
    member this.applyAppearance() =
        // Get all current values from UI editors
        let getValue key = (editors.find key).value
        
        // Get the current appearance for fields not in UI
        let currentAppearance = Services.program.tabAppearanceInfo
        
        // Create new appearance with correct field order
        let newAppearance = {
            tabHeight = unbox(getValue "tabHeight")
            tabMaxWidth = unbox(getValue "tabMaxWidth")
            tabOverlap = unbox(getValue "tabOverlap")
            tabHeightOffset = currentAppearance.tabHeightOffset  // Keep internal value
            tabIndentFlipped = unbox(getValue "tabIndentFlipped")
            tabIndentNormal = unbox(getValue "tabIndentNormal")
            tabTextColor = unbox(getValue "tabTextColor")
            tabNormalBgColor = unbox(getValue "tabNormalBgColor")
            tabHighlightBgColor = unbox(getValue "tabHighlightBgColor")
            tabActiveBgColor = unbox(getValue "tabActiveBgColor")
            tabFlashBgColor = unbox(getValue "tabFlashBgColor")
            tabBorderColor = unbox(getValue "tabBorderColor")
        }
        
        Services.settings.setValue("tabAppearance", box(newAppearance))
        
    interface ISettingsView with
        member x.key = SettingsViewType.AppearanceSettings
        member x.title = Localization.getString("Appearance")
        member x.control = panel :> Control

