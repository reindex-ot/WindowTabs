namespace Bemo
open System
open System.Drawing
open System.Windows.Forms
open System.Diagnostics
open Bemo.Win32.Forms
open System.Reflection
open System.Resources

// Tab group info for cross-thread access
type TabGroupInfo = {
    hwnd: IntPtr
    tabNames: string list
    tabCount: int
    firstTabIcon: Img option
    tabHwnds: IntPtr list
}

type TabStripDecorator(group:WindowGroup, notifyDetached: IntPtr -> unit) as this =
    // Static registry for all TabStripDecorator instances
    static let mutable decorators = System.Collections.Generic.Dictionary<IntPtr, TabStripDecorator>()
    static let mutable groupInfos = System.Collections.Generic.Dictionary<IntPtr, TabGroupInfo>()

    let os = OS()
    let Cell = CellScope(false, true)
    let resources = new ResourceManager("Properties.Resources", Assembly.GetExecutingAssembly())
    let isDraggingCell = Cell.create(false)
    let dragInfoCell = Cell.create(None)
    let dragPtCell = Cell.create(Pt.empty)
    let dropTarget = Cell.create(None)
    let mouseEvent = Event<_>()
    let _ts = TabStrip(this :> ITabStripMonitor)
    // Variables for double-click detection
    let lastClickTime = ref System.DateTime.MinValue
    let lastClickTab = ref None
    let doubleClickTimeoutMs = 500.0  // Windows default double-click time
    let hiddenByDoubleClick = ref false
    let doubleClickProtectUntil = ref System.DateTime.MinValue
    let firstClickTab = ref None  // Track the tab that was clicked first in potential double-click

    do this.init()

    member this.ts = _ts
    member this.group = group
    member private this.mouse = mouseEvent.Publish

    member private this.updateGroupInfo() =
        try
            // Create a snapshot of current tab information
            let tabs = this.ts.lorder

            // If no tabs remain, remove from groupInfos
            if tabs.count = 0 then
                lock groupInfos (fun () -> groupInfos.Remove(group.hwnd) |> ignore)
            else
                let tabNames =
                    tabs.list |> List.map (fun tab ->
                        let info = this.ts.tabInfo(tab)
                        info.text
                    )
                let tabHwnds =
                    tabs.list |> List.map (fun (Tab(hwnd)) -> hwnd)
                let firstTabIcon =
                    if tabs.count > 0 then
                        let firstTab = tabs.at(0)
                        let info = this.ts.tabInfo(firstTab)
                        try
                            Some(info.iconSmall.ToBitmap().img.resize(Sz(16,16)))
                        with _ ->
                            None
                    else
                        None
                let info = {
                    hwnd = group.hwnd
                    tabNames = tabNames
                    tabCount = tabs.count
                    firstTabIcon = firstTabIcon
                    tabHwnds = tabHwnds
                }
                lock groupInfos (fun () -> groupInfos.[group.hwnd] <- info)
        with _ -> ()

    member private this.init() =
        Services.registerLocal(_ts)
        group.init(this.ts)
        // Register this decorator in the global registry
        lock decorators (fun () -> decorators.[group.hwnd] <- this)

        Services.dragDrop.registerTarget(this.ts.hwnd, this:>IDragDropTarget)
    
        dropTarget.set(Some(OleDropTarget(this.ts)))
        
        this.initAutoHide()

        let capturedHwnd = ref None

        this.mouse.Add <| fun(hwnd, btn, action, pt) ->
            match action, btn with
            | MouseDblClick, MouseLeft ->
                // Check if double-click hide mode is enabled
                let autoHideDoubleClick = group.bb.read("autoHideDoubleClick", false)
                
                // Only hide tabs if:
                // 1. Double-click mode is enabled
                // 2. Tabs are positioned at bottom
                // 3. The clicked tab is the active tab
                // 4. The first click was also on this same tab (prevents activation+hide)
                if autoHideDoubleClick && this.ts.direction = TabDown && 
                   hwnd = group.topWindow && !firstClickTab = Some(hwnd) then
                    // Hide tabs on double-click of active tab
                    // Set flag BEFORE hiding to prevent immediate re-show
                    hiddenByDoubleClick := true
                    doubleClickProtectUntil := System.DateTime.Now.AddMilliseconds(300.0) // 300ms protection
                    // Use invokeAsync to ensure flag is set before Cell update
                    group.invokeAsync <| fun() ->
                        this.ts.isShrunk <- true
                else
                    group.isIconOnly <- false
                // Disable tab rename on double-click
                // this.beginRename(hwnd)
                // Clear first click tracking after double-click
                firstClickTab := None
            | MouseUp, MouseRight ->
                let ptScreen = os.windowFromHwnd(group.hwnd).ptToScreen(pt)
                group.bb.write("contextMenuVisible", true)
                Win32Menu.show group.hwnd ptScreen (this.contextMenu(hwnd))
                group.bb.write("contextMenuVisible", false)
            | MouseDown, MouseLeft ->
                capturedHwnd := Some(hwnd)
                // Track the first click for double-click detection
                // Only set if it's the active tab to ensure double-click only works on already active tabs
                if hwnd = group.topWindow then
                    firstClickTab := Some(hwnd)
                else
                    firstClickTab := None
            | MouseDown, _ ->
                capturedHwnd := Some(hwnd)
            | MouseUp, MouseMiddle -> 
                capturedHwnd.Value.iter <| fun capturedHwnd ->
                    if hwnd = capturedHwnd then
                        this.onCloseWindow hwnd
            | _ -> ()
        
        group.bounds.changed.Add <| fun() ->
            this.updateTsPlacement()

        // Update placement when tab appearance changes (for height and indent values)
        Services.settings.notifyValue "tabAppearance" <| fun(_) ->
            this.invokeAsync <| fun() ->
                this.updateTsPlacement()

        group.exited.Add <| fun() ->
            Services.dragDrop.unregisterTarget(this.ts.hwnd)
    

    member private this.tabSlide =
        dragInfoCell.value.map <| fun dragInfo ->
                dragInfo.tab, dragPtCell.value.sub(dragInfo.tabOffset).x

    member private this.updateTsSlide() =
        this.ts.slide <- this.tabSlide

    member private this.updateTsPlacement() = 
        if group.bounds.value.IsNone then
            this.ts.visible <- false
        else
            this.ts.setPlacement(this.placement)
            this.ts.visible <- true
            
            // Handle UWP application tab visibility
            let hasUWPWindow = group.windows.items.any(fun hwnd ->
                let window = os.windowFromHwnd(hwnd)
                window.className = "ApplicationFrameWindow"
            )
            
            let tsWindow = os.windowFromHwnd(this.ts.hwnd)
            // Make topmost for UWP apps when a UWP app is active
            if hasUWPWindow then
                // Check if any UWP window in the group is foreground
                let isUWPForeground = group.windows.items.any(fun hwnd ->
                    let window = os.windowFromHwnd(hwnd)
                    window.className = "ApplicationFrameWindow" && hwnd = os.foreground.hwnd
                )
                if isUWPForeground then
                    tsWindow.makeTopMost()
                else
                    tsWindow.makeNotTopMost()
            else
                tsWindow.makeNotTopMost()

    member private this.invokeAsync f = group.invokeAsync f
    member private this.invokeSync f = group.invokeSync f

    member this.placement =
        let decorator =  {
            windowBounds = group.bounds.value.def(Rect())
            monitorBounds = Mon.all.map(fun m -> m.workRect)
            decoratorHeight = group.tabAppearance.tabHeight
            decoratorHeightOffset = group.tabAppearance.tabHeightOffset
            decoratorIndentFlipped = group.tabAppearance.tabIndentFlipped
            decoratorIndentNormal = group.tabAppearance.tabIndentNormal
        }
        {
            showInside = decorator.shouldShowInside
            bounds = decorator.bounds
        }

    member this.beginRename(hwnd) =
        let tab = Tab(hwnd)
        let textBounds = 
            this.ts.sprite.children.pick <| fun (tabOffset, tabSprite) ->
                let tabSprite = tabSprite :?> TabSprite<Tab>
                if tabSprite.id = tab then 
                    Some(Rect(tabSprite.textLocation.add(tabOffset), tabSprite.textSize))
                else None
        let verticalMargin = 2
        let form = new FloatingTextBox()
        form.textBox.Font <- SystemFonts.MenuFont
        form.Location <- textBounds.location.add(this.placement.bounds.location).add(Pt(0, verticalMargin)).Point
        form.SetSize(textBounds.size.add(Sz(0, -2 * verticalMargin)).Size)
        form.textBox.KeyPress.Add <| fun e ->
            if e.KeyChar = char(Keys.Enter) then
                let newName = form.textBox.Text
                group.setTabName(hwnd, if newName.Length = 0 then None else Some(newName))
                form.Close()
            elif e.KeyChar = char(Keys.Escape) then
                form.Close()
        let tabText = this.ts.tabInfo(Tab(hwnd)).text
        form.textBox.Text <- tabText
        form.textBox.SelectionStart <- 0
        form.textBox.SelectionLength <- tabText.Length
        form.textBox.LostFocus.Add <| fun _ ->
            form.Close()
        group.bb.write("renamingTab", true)
        form.Closed.Add <| fun _ ->
            group.bb.write("renamingTab", false)
        form.Show()

    member private this.onCloseWindow hwnd =
        os.windowFromHwnd(hwnd).close()

    member private this.onCloseOtherWindows hwnd =
        group.windows.items.where((<>)hwnd).iter this.onCloseWindow

    member private this.onCloseRightTabWindows hwnd =
        let currentTab = Tab(hwnd)
        let tabIndex = this.ts.lorder.tryFindIndex((=) currentTab)
        tabIndex.iter <| fun index ->
            let rightTabs = this.ts.lorder.skip(index + 1)
            rightTabs.iter <| fun tab ->
                let (Tab(tabHwnd)) = tab
                this.onCloseWindow tabHwnd

    member private this.onCloseLeftTabWindows hwnd =
        let currentTab = Tab(hwnd)
        let tabIndex = this.ts.lorder.tryFindIndex((=) currentTab)
        tabIndex.iter <| fun index ->
            let leftTabs = this.ts.lorder.take(index)
            leftTabs.iter <| fun tab ->
                let (Tab(tabHwnd)) = tab
                this.onCloseWindow tabHwnd

    member private this.onCloseAllWindows() =
        group.windows.items.iter this.onCloseWindow

    member private this.detachTab(hwnd: IntPtr) =
        this.detachTabToPosition(hwnd, None)

    member private this.moveTabToGroup(hwnd: IntPtr, targetGroup: WindowGroup) =
        // Move tab to another group if it's a different group
        if targetGroup.hwnd <> group.hwnd then
            let tab = Tab(hwnd)
            let window = os.windowFromHwnd(hwnd)

            try
                // Suspend tab monitoring to prevent auto-grouping during the move
                Services.program.suspendTabMonitoring()

                try
                    // First ensure the window is not in the target group already
                    if targetGroup.windows.contains hwnd then
                        System.Diagnostics.Debug.WriteLine(sprintf "Window %A is already in target group %A, skipping move" hwnd targetGroup.hwnd)
                        ()
                    else
                        // Store original window state
                        let wasMinimized = window.isMinimized
                        let wasMaximized = window.isMaximized

                        // Remove tab from current group first (ensure it's actually removed)
                        if this.ts.tabs.contains(tab) then
                            this.ts.removeTab(tab)
                        if group.windows.contains hwnd then
                            group.removeWindow(hwnd)

                        // Wait for removal to complete
                        System.Threading.Thread.Sleep(50)

                        // Hide window temporarily to prevent flashing
                        window.hideOffScreen(None)

                        // Restore window state if necessary
                        if wasMinimized || wasMaximized then
                            window.showWindow(ShowWindowCommands.SW_RESTORE)

                        // Use synchronous invoke to ensure completion
                        let moveCompleted = ref false
                        let moveException = ref None

                        targetGroup.invokeSync(fun() ->
                            try
                                // Double-check window is not already in target group
                                if not (targetGroup.windows.contains hwnd) then
                                    targetGroup.addWindow(hwnd, false)
                                    // Show window again (target group will handle positioning)
                                    window.showWindow(ShowWindowCommands.SW_SHOW)
                                    moveCompleted := true
                                else
                                    System.Diagnostics.Debug.WriteLine(sprintf "Window %A already in target during sync, skipping" hwnd)
                            with ex ->
                                moveException := Some ex
                        )

                        // Check if move failed
                        match !moveException with
                        | Some ex ->
                            // Restore to original group on failure
                            System.Diagnostics.Debug.WriteLine(sprintf "Move failed, restoring to original group: %s" ex.Message)
                            group.addWindow(hwnd, false)
                            window.showWindow(ShowWindowCommands.SW_SHOW)
                            raise ex
                        | None when not !moveCompleted ->
                            // Move didn't complete, restore
                            System.Diagnostics.Debug.WriteLine("Move didn't complete, restoring to original group")
                            group.addWindow(hwnd, false)
                            window.showWindow(ShowWindowCommands.SW_SHOW)
                        | None ->
                            // Move successful
                            System.Diagnostics.Debug.WriteLine(sprintf "Successfully moved tab %A from group %A to group %A" hwnd group.hwnd targetGroup.hwnd)

                            // Wait a bit for UI to update
                            System.Threading.Thread.Sleep(100)

                            // Move successful - no need to update group info here
                            // as it will be updated when menu is opened

                finally
                    // Resume tab monitoring
                    Services.program.resumeTabMonitoring()
            with ex ->
                System.Diagnostics.Debug.WriteLine(sprintf "Error moving tab: %s" ex.Message)
                // Resume monitoring even on error
                try Services.program.resumeTabMonitoring() with _ -> ()

    member private this.detachTabToPosition(hwnd: IntPtr, position: Option<string>) =
        // Only detach if there's more than one tab
        if group.windows.items.count > 1 then
            let tab = Tab(hwnd)
            let window = os.windowFromHwnd(hwnd)
            let bounds = window.bounds

            // Suspend tab monitoring to prevent auto-grouping
            Services.program.suspendTabMonitoring()

            try
                // Remove tab from current group first
                this.ts.removeTab(tab)
                group.removeWindow(hwnd)

                // Hide window temporarily
                window.hideOffScreen(None)

                // Restore window to its original position
                if window.isMinimized || window.isMaximized then
                    window.showWindow(ShowWindowCommands.SW_RESTORE)

                // Set position based on the option
                // First restore to original position to determine correct screen
                window.setPositionOnly bounds.location.x bounds.location.y

                // Calculate window center point to determine which screen it belongs to
                let centerX = bounds.location.x + bounds.size.width / 2
                let centerY = bounds.location.y + bounds.size.height / 2
                let centerPoint = System.Drawing.Point(centerX, centerY)
                let screen = Screen.FromPoint(centerPoint)

                match position with
                | Some "right" ->
                    let width = bounds.size.width
                    let x = screen.WorkingArea.Right - width
                    let y = bounds.location.y
                    // Keep Y within screen bounds
                    let y = max screen.WorkingArea.Top (min y (screen.WorkingArea.Bottom - bounds.size.height))
                    window.setPositionOnly x y
                | Some "left" ->
                    let x = screen.WorkingArea.Left
                    let y = bounds.location.y
                    // Keep Y within screen bounds
                    let y = max screen.WorkingArea.Top (min y (screen.WorkingArea.Bottom - bounds.size.height))
                    window.setPositionOnly x y
                | Some "top" ->
                    let x = bounds.location.x
                    let y = screen.WorkingArea.Top
                    // Keep X within screen bounds
                    let x = max screen.WorkingArea.Left (min x (screen.WorkingArea.Right - bounds.size.width))
                    window.setPositionOnly x y
                | Some "bottom" ->
                    let height = bounds.size.height
                    let x = bounds.location.x
                    let y = screen.WorkingArea.Bottom - height
                    // Keep X within screen bounds
                    let x = max screen.WorkingArea.Left (min x (screen.WorkingArea.Right - bounds.size.width))
                    window.setPositionOnly x y
                | _ ->
                    () // Already positioned at original location

                // Notify that this window was detached so it gets a new group
                // This will trigger dragDrop and dragEnd which creates a new group
                notifyDetached(hwnd)

                // Detach successful
            finally
                // Resume tab monitoring after a delay
                (ThreadHelper.cancelablePostBack 200 <| fun() ->
                    Services.program.resumeTabMonitoring()) |> ignore

    member private  this.contextMenu(hwnd) =
        let checked(isChecked) = if isChecked then List2([MenuFlags.MF_CHECKED]) else List2()
        let grayed(isGrayed) = if isGrayed then List2([MenuFlags.MF_GRAYED]) else List2()
        let window = os.windowFromHwnd(hwnd)
        let pid = window.pid
        let exeName = pid.exeName
        let processPath = pid.processPath

        let newWindowItem = 
            CmiRegular({
                text = resources.GetString("NewWindow")
                flags = List2()
                image = None
                click = fun() -> Process.Start(processPath) |> ignore
            })

        
        let renameTabItem =
            CmiRegular({
                text = resources.GetString("RenameTab")
                image = None
                flags = List2()
                click = fun() ->
                    this.beginRename(hwnd)
            })
        let restoreTabNameItem =
            CmiRegular({
                text = resources.GetString("RestoreTabName")
                image = None
                click = fun() -> group.setTabName(hwnd, None)
                flags = List2()
            })

                 
        let closeTabItem = 
            let tabText = this.ts.tabInfo(Tab(hwnd)).text
            let displayText = 
                if tabText.Length <= 9 then
                    tabText
                else
                    tabText.Substring(0, 9) + "..."
            CmiRegular({
                text = sprintf "%s(%s)" (resources.GetString("CloseTab")) displayText
                image = None
                click = fun() -> this.onCloseWindow hwnd
                flags = List2()
            })

        let closeRightTabsItem =
            let currentTab = Tab(hwnd)
            let tabIndex = this.ts.lorder.tryFindIndex((=) currentTab)
            let rightTabCount = 
                tabIndex |> Option.map(fun index -> this.ts.lorder.count - index - 1) |> Option.defaultValue 0
            let displayText = 
                if rightTabCount = 0 then
                    resources.GetString("CloseTabsToTheRight")
                else
                    let formatKey = "CloseTabsToTheRightFormat"
                    let formatString = resources.GetString(formatKey)
                    if formatString = null then
                        failwithf "Resource string '%s' not found" formatKey
                    let tabWord = 
                        if rightTabCount = 1 then 
                            resources.GetString("TabSingular")
                        else 
                            resources.GetString("TabPlural")
                    String.Format(formatString, rightTabCount, tabWord)
            CmiRegular({
                text = displayText
                image = None
                click = fun() -> this.onCloseRightTabWindows hwnd
                flags = grayed(rightTabCount = 0)
            })

        let closeLeftTabsItem =
            let currentTab = Tab(hwnd)
            let tabIndex = this.ts.lorder.tryFindIndex((=) currentTab)
            let leftTabCount = 
                tabIndex |> Option.defaultValue 0
            let displayText = 
                if leftTabCount = 0 then
                    resources.GetString("CloseTabsToTheLeft")
                else
                    let formatKey = "CloseTabsToTheLeftFormat"
                    let formatString = resources.GetString(formatKey)
                    if formatString = null then
                        failwithf "Resource string '%s' not found" formatKey
                    let tabWord = 
                        if leftTabCount = 1 then 
                            resources.GetString("TabSingular")
                        else 
                            resources.GetString("TabPlural")
                    String.Format(formatString, leftTabCount, tabWord)
            CmiRegular({
                text = displayText
                image = None
                click = fun() -> this.onCloseLeftTabWindows hwnd
                flags = grayed(leftTabCount = 0)
            })

        let closeOtherTabsItem =
            CmiRegular({
                text = resources.GetString("CloseOtherTabs")
                image = None
                click = fun() -> this.onCloseOtherWindows hwnd
                flags = List2()
            })

        let closeAllTabsItem =
            CmiRegular({
                text = resources.GetString("CloseAllTabs")
                image = None
                click = fun() -> this.onCloseAllWindows()
                flags = List2()
            })

        let managerItem =
            CmiRegular({
                text = resources.GetString("SettingsMenu")
                image = None
                click = fun() -> Services.managerView.show()
                flags = List2()
            })

        let detachTabSubMenu =
            let isEnabled = group.windows.items.count > 1
            Some(CmiPopUp({
                text = resources.GetString("DetachTab")
                image = None
                items = List2([
                    CmiRegular({
                        text = resources.GetString("DetachTabSamePosition")
                        image = None
                        click = fun() -> this.detachTab(hwnd)
                        flags = List2()
                    })
                    CmiRegular({
                        text = resources.GetString("DetachTabMoveRight")
                        image = None
                        click = fun() -> this.detachTabToPosition(hwnd, Some "right")
                        flags = List2()
                    })
                    CmiRegular({
                        text = resources.GetString("DetachTabMoveLeft")
                        image = None
                        click = fun() -> this.detachTabToPosition(hwnd, Some "left")
                        flags = List2()
                    })
                    CmiRegular({
                        text = resources.GetString("DetachTabMoveTop")
                        image = None
                        click = fun() -> this.detachTabToPosition(hwnd, Some "top")
                        flags = List2()
                    })
                    CmiRegular({
                        text = resources.GetString("DetachTabMoveBottom")
                        image = None
                        click = fun() -> this.detachTabToPosition(hwnd, Some "bottom")
                        flags = List2()
                    })
                ])
                flags = if isEnabled then List2() else List2([MenuFlags.MF_GRAYED])
            }))

        List2([
            Some(newWindowItem)
            Some(CmiSeparator)
            Some(closeTabItem)
            Some(closeRightTabsItem)
            Some(closeLeftTabsItem)
            Some(closeOtherTabsItem)
            Some(closeAllTabsItem)
            Some(CmiSeparator)
            Some(renameTabItem)
            (if group.isRenamed(hwnd) then Some(restoreTabNameItem) else None)
            Some(CmiSeparator)
            detachTabSubMenu
            // Add "Move tab" menu - use static group infos for thread-safe access
            (
                // Update all group infos before building menu
                let allDecorators = lock decorators (fun () ->
                    decorators.Values
                    |> List.ofSeq
                    |> List.filter (fun d ->
                        // Filter out invalid decorators
                        try
                            d.ts.hwnd <> IntPtr.Zero &&
                            WinUserApi.IsWindow(d.group.hwnd) &&
                            WinUserApi.IsWindow(d.ts.hwnd)
                        with _ -> false
                    )
                )

                // First, update the current group's info synchronously
                this.updateGroupInfo()

                // Update all other decorators' group info and wait for completion
                let updateTasks =
                    allDecorators
                    |> List.filter (fun d -> d.group.hwnd <> group.hwnd)  // Skip current group (already updated)
                    |> List.map (fun d ->
                        async {
                            try
                                // Double check the window is still valid
                                if WinUserApi.IsWindow(d.group.hwnd) && WinUserApi.IsWindow(d.ts.hwnd) then
                                    d.group.invokeSync(fun () -> d.updateGroupInfo())
                            with _ -> ()
                        }
                    )

                // Wait for all updates to complete
                updateTasks |> Async.Parallel |> Async.RunSynchronously |> ignore

                // Now get the updated group infos
                let allGroupInfos = lock groupInfos (fun () ->
                    groupInfos.Values
                    |> List.ofSeq
                    |> List.filter (fun info ->
                        info.hwnd <> group.hwnd && // Not current group
                        info.tabCount > 0 && // Has at least one tab
                        // Don't show groups that contain the tab we're moving
                        not (info.tabHwnds |> List.contains hwnd)
                    )
                )
                System.Diagnostics.Debug.WriteLine(sprintf "Total visible groups found: %d" allGroupInfos.Length)
                System.Diagnostics.Debug.WriteLine(sprintf "Current group hwnd: %A" group.hwnd)

                if not (List.isEmpty allGroupInfos) then
                    // Build menu items for each other group
                    // Use distinctBy to prevent duplicates
                    let uniqueGroupInfos = allGroupInfos |> List.distinctBy (fun info -> info.hwnd)
                    let menuItems =
                        uniqueGroupInfos
                        |> List.choose (fun info ->
                            try
                                System.Diagnostics.Debug.WriteLine(sprintf "Group hwnd=%A, tabs count=%d" info.hwnd info.tabCount)

                                // Get the decorator for this group to handle the click
                                let targetDecorator = lock decorators (fun () ->
                                    decorators.Values |> Seq.tryFind (fun d ->
                                        d.group.hwnd = info.hwnd &&
                                        WinUserApi.IsWindow(d.group.hwnd) &&
                                        WinUserApi.IsWindow(d.ts.hwnd)
                                    )
                                )

                                // Only create menu item if we have a valid decorator
                                match targetDecorator with
                                | Some decorator ->
                                    // Build menu text with tab names
                                    let fullNameString =
                                        if info.tabCount = 1 then
                                            // Single tab: show first 22 chars
                                            let tabName = info.tabNames |> List.head
                                            if tabName.Length > 22 then
                                                tabName.Substring(0, 22) + "..."
                                            else
                                                tabName
                                        elif info.tabCount = 2 then
                                            // 2 tabs: show 9 chars each
                                            let tabNames = info.tabNames |> List.take 2
                                            let truncatedNames = tabNames |> List.map (fun name ->
                                                if name.Length > 9 then
                                                    name.Substring(0, 9) + "..."
                                                else
                                                    name
                                            )
                                            String.Join(" ", truncatedNames)
                                        else
                                            // 3+ tabs: show first 3 tabs with 5 chars each
                                            let tabNames = info.tabNames |> List.take (min 3 info.tabCount)
                                            let truncatedNames = tabNames |> List.map (fun name ->
                                                if name.Length > 5 then
                                                    name.Substring(0, 5) + "..."
                                                else
                                                    name
                                            )
                                            let nameString = String.Join(" ", truncatedNames)
                                            // For 4+ tabs, still show only 3 tab names
                                            if info.tabCount > 3 then
                                                nameString + "..."
                                            else
                                                nameString

                                    // Use same pattern as CloseTabsToTheRight
                                    let formatString = resources.GetString("MoveTabGroupFormat")
                                    let tabWord =
                                        if info.tabCount = 1 then
                                            resources.GetString("TabSingular")
                                        else
                                            resources.GetString("TabPlural")
                                    let menuText = String.Format(formatString, info.tabCount, tabWord, fullNameString)

                                    System.Diagnostics.Debug.WriteLine(sprintf "Menu text: %s" menuText)

                                    Some(CmiRegular({
                                        text = menuText
                                        image = info.firstTabIcon
                                        click = fun() ->
                                            // Move the tab to the target group
                                            this.moveTabToGroup(hwnd, decorator.group)
                                        flags = List2()
                                    }))
                                | None ->
                                    // No valid decorator found, skip this item
                                    System.Diagnostics.Debug.WriteLine(sprintf "No valid decorator for group hwnd=%A" info.hwnd)
                                    None
                            with ex ->
                                System.Diagnostics.Debug.WriteLine(sprintf "Exception in menu item creation: %s" ex.Message)
                                None
                        )

                    Some(CmiPopUp({
                        text = resources.GetString("MoveTab")
                        image = None
                        items = List2(menuItems)
                        flags = List2()
                    }))
                else
                    // No other groups available
                    None
            )
            Some(CmiSeparator)
            Some(managerItem)
        ]).choose(id)

    member private this.initAutoHide() =
        let callbackRef = ref None
        // Create a cell that tracks whether tabs are shown inside
        let isWindowInside = Cell.create(false)
        let isMouseOver = Cell.import(group.isMouseOver)
        let propCell(key,def) =
            let cell = Cell.create(group.bb.read(key, def))
            let update() = cell.value <- group.bb.read(key, def)
            group.bb.subscribe key update
            cell
        let autoHideCell = propCell("autoHide", false)
        let autoHideMaximizedCell = propCell("autoHideMaximized", false)
        let autoHideDoubleClickCell = propCell("autoHideDoubleClick", false)
        let contextMenuVisibleCell = propCell("contextMenuVisible", false)
        let renamingTabCell = propCell("renamingTab", false)
        // Create a cell that tracks the hide delay setting
        let hideDelayCell = Cell.create(
            try
                Services.settings.getValue("hideTabsDelayMilliseconds") :?> int
            with
            | _ -> 3000
        )
        let isRecentlyChangedZorderCell =
            let cell = Cell.create(false)
            let cbRef = ref None
            group.zorder.changed.Add <| fun() ->
                cell.value <- true
                cbRef.Value.iter <| fun(d:IDisposable) -> d.Dispose()
                cbRef := Some(ThreadHelper.cancelablePostBack hideDelayCell.value <| fun() ->
                    cell.value <- false
                )
            cell
        // Create a function to handle the auto-hide logic
        let updateAutoHide() =
            // Update isWindowInside based on current tab position
            isWindowInside.value <- this.ts.showInside
            
            // Handle double-click mode separately
            if autoHideDoubleClickCell.value && this.ts.direction = TabDown then
                
                // Check if protection period has expired
                let protectionExpired = System.DateTime.Now > !doubleClickProtectUntil
                
                // In double-click mode, only show tabs when mouse is over and hidden
                if this.ts.isShrunk && isMouseOver.value && not isDraggingCell.value then
                    // Check if we should show tabs (protection period expired OR mouse left and returned)
                    if not !hiddenByDoubleClick || protectionExpired then
                        if protectionExpired && !hiddenByDoubleClick then
                            hiddenByDoubleClick := false
                        if not !hiddenByDoubleClick then
                            this.ts.isShrunk <- false
                // Clear the flag when mouse leaves
                elif not isMouseOver.value then
                    if !hiddenByDoubleClick && protectionExpired then
                        hiddenByDoubleClick := false
            else
                // Normal auto-hide logic for other modes
                let shrink = 
                    ((isWindowInside.value && autoHideCell.value) ||
                     (group.isMaximized.value && autoHideMaximizedCell.value)) && 
                    isMouseOver.value.not && 
                    isDraggingCell.value.not &&
                    contextMenuVisibleCell.value.not &&
                    renamingTabCell.value.not &&
                    isRecentlyChangedZorderCell.value.not
                callbackRef.Value.iter <| fun(d:IDisposable) -> d.Dispose()
                callbackRef := None
                if shrink then
                    callbackRef := Some(ThreadHelper.cancelablePostBack hideDelayCell.value <| fun() ->
                        this.ts.isShrunk <- true
                    )
                else
                    this.ts.isShrunk <- false
        
        // Listen for changes to trigger auto-hide update
        Cell.listen updateAutoHide
        
        // When hideDelayCell value changes via notifyValue, restart timer if running
        Services.settings.notifyValue "hideTabsDelayMilliseconds" <| fun value ->
            group.invokeAsync <| fun() ->
                hideDelayCell.value <- 
                    try
                        value :?> int
                    with
                    | _ -> 3000
                // If a timer is running, restart it with the new delay
                if callbackRef.Value.IsSome then
                    updateAutoHide()

    interface ITabStripMonitor with
        member x.tabClick((btn, tab, part, action, pt)) = 
            let (Tab(hwnd)) = tab
            mouseEvent.Trigger(hwnd, btn, action, pt)
            let ptScreen = os.windowFromHwnd(this.ts.hwnd).ptToScreen(pt)
            match action with
            | MouseDown ->
                group.flashTab(tab, false)
                match btn with
                | MouseRight ->
                    os.windowFromHwnd(group.topWindow).setForeground(false)
                | MouseLeft ->
                    group.tabActivate(tab, false)
                    if part <> TabClose then
                        let dragOffset = pt.sub(this.ts.tabLocation tab)
                        let dragImage = fun() -> this.ts.dragImage(tab)
                        let dragInfo = box({ tab = tab; tabOffset = dragOffset; tabInfo = this.ts.tabInfo(tab)})
                        Services.dragDrop.beginDrag(this.ts.hwnd, dragImage, dragOffset, ptScreen, dragInfo)
                | MouseMiddle ->
                    group.tabActivate(tab, false)
            | _ -> ()
    
        member x.tabActivate((tab)) =
            group.tabActivate(tab, false)

        member x.tabMoved(Tab(hwnd), index) =
            group.onTabMoved(hwnd, index)

        member x.tabClose(Tab(hwnd)) =
            // Get all tabs and current tab index before closing
            let currentTab = Tab(hwnd)
            let allTabs = this.ts.lorder
            let currentIndex = allTabs.tryFindIndex((=) currentTab)
            
            // Check if this is the active tab
            let isActiveTab = (group.topWindow = hwnd)
            
            // If closing the active tab and there are more tabs, activate the next one
            if isActiveTab && allTabs.count > 1 then
                currentIndex.iter <| fun index ->
                    // Determine which tab to activate
                    let nextTab = 
                        if index < allTabs.count - 1 then
                            Some(allTabs.at(index + 1))  // Next tab
                        elif index > 0 then
                            Some(allTabs.at(index - 1))  // Previous tab
                        else
                            None
                    
                    // Activate the next tab before closing
                    nextTab.iter <| fun tab ->
                        group.tabActivate(tab, true)
            
            // Close the window
            os.windowFromHwnd(hwnd).close()

        member x.windowMsg(msg) =
            ()
            
    interface IDragDropTarget with
        member this.dragBegin() =
            this.invokeAsync <| fun() -> 
                isDraggingCell.value <- true
                this.ts.transparent <- false
                
        member this.dragEnter dragInfo pt =
            this.invokeSync <| fun() -> 
                let dragInfo = dragInfo :?> TabDragInfo
                let (Tab(hwnd)) = dragInfo.tab
                let result = 
                    if this.ts.tabs.contains(dragInfo.tab) &&
                        this.ts.tabs.count = 1 then
                        dragPtCell.set(pt)
                        dragInfoCell.set(Some(dragInfo))
                        false
                    else 
                        dragPtCell.set(pt)
                        dragInfoCell.set(Some(dragInfo))
                        this.ts.addTabSlide dragInfo.tab this.tabSlide
                        this.ts.setTabInfo(dragInfo.tab, dragInfo.tabInfo)
                        group.addWindow(hwnd, false)
                        true
                this.updateTsSlide()
                result

        member this.dragMove(pt) =
            this.invokeSync <| fun() ->
                dragPtCell.set(pt)
                this.updateTsSlide()

        member this.dragExit() =
            this.invokeSync <| fun() ->
                match dragInfoCell.value with
                | Some(dragInfo) ->
                    let tab = dragInfo.tab
                    if this.ts.tabs.contains(tab) then
                        this.ts.removeTab(tab)
                        dragInfoCell.set(None)       
                        let (Tab(hwnd)) = tab
                        let window = os.windowFromHwnd(hwnd)
                        group.removeWindow(hwnd)
                        window.hideOffScreen(None)
                | None -> ()
                this.updateTsSlide()

        member this.dragEnd() =
            this.invokeAsync <| fun() ->
                isDraggingCell.value <- false
                this.ts.transparent <- true
                match this.ts.movedTab with
                | Some(tab, index) ->
                    this.ts.moveTab(tab, index)
                | None -> ()
                dragInfoCell.set(None)
                this.updateTsSlide()

    interface IDisposable with
        member this.Dispose() =
            // Unregister from the global registry
            lock decorators (fun () -> decorators.Remove(group.hwnd) |> ignore)
            lock groupInfos (fun () -> groupInfos.Remove(group.hwnd) |> ignore)
