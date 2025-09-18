namespace Bemo
open System
open System.Drawing
open System.Windows.Forms
open System.Diagnostics
open Bemo.Win32.Forms
open System.Reflection
open System.Resources

type TabStripDecorator(group:WindowGroup, notifyDetached: IntPtr -> unit) as this =
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
    member private this.mouse = mouseEvent.Publish
    member private this.init() =
        Services.registerLocal(_ts)
        group.init(this.ts)

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
                window.setPositionOnly bounds.location.x bounds.location.y

                // Notify that this window was detached so it gets a new group
                // This will trigger dragDrop and dragEnd which creates a new group
                notifyDetached(hwnd)
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
            if group.windows.items.count > 1 then
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
                    ])
                }))
            else
                None

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
