# WindowTabs Standard-Software Version

## version ss_jp_2025.08.07_next
- Unify tab alignment setting across window inside/outside positions
  - Tab alignment (left/center/right) now uses single setting regardless of tab position
  - Removed separate settings for TabUp (outside) and TabDown (inside) states
  - Simplified tab position management for consistent user experience
- Increase auto-hide delay for tabs when window inside
  - Changed delay from 100ms to 1000ms (1 second) when mouse leaves tabs
  - Provides more time before tabs automatically hide
  - Improves usability when tabs are positioned inside window
- Remove "Switch Tabs" group box in Behavior settings tab
  - Removed unnecessary group box border from Switch Tabs section
  - Cleaner UI presentation in settings dialog
- Add "Default: Make tabs narrower" setting in Behavior tab
  - Added makeTabsNarrowerByDefault option in settings
  - When enabled, new tab groups start with narrower tabs by default
  - Individual tab groups can still toggle width via context menu
- Add "Default: Tab position" setting in Behavior tab
  - Added defaultTabPosition option with dropdown for Left/Center/Right
  - New tab groups use the selected default position
  - Individual tab groups can still change position via context menu
- Rename Appearance tab indent options
  - "Indent (Window Inside Tab)" → "Indent for Tabs Down"
  - "Indent (Window Outside Tab)" → "Indent for Tabs Up"
- Change "Hide tabs when maximized" to "Hide tabs when window inside"
  - Tabs now hide when window is inside (not just when maximized)
  - Detects when tabs are positioned inside the window (TabDown)
  - Context menu text updated accordingly
- Add "Default: Hide tabs when window inside" setting in Behavior tab
  - New hideTabsWhenInsideByDefault option
  - When enabled, new tab groups start with auto-hide enabled for inside tabs
  - Individual tab groups can still toggle via context menu
- Prevent tab switching flash when tabs are inside window
  - Temporarily set TOPMOST flag during tab switch for non-UWP windows
  - Removes TOPMOST flag after 50ms delay to ensure smooth transition
  - UWP windows are excluded as they already have TOPMOST handling
  - Eliminates visual flashing when switching between tabs positioned inside window

## version ss_jp_2025.08.07
- Disable tab rename on double-click
  - Removed double-click rename functionality from tabs
  - Tab rename can still be accessed via right-click context menu
  - Reduces accidental tab renaming

## version ss_jp_2025.08.06
- Add language switching functionality
  - Added Language submenu in tray icon context menu (English/Japanese)
  - Auto-restart application with confirmation dialog when language is changed
  - Language setting saved to configuration file
- Rename "Indent Flipped" to "Indent (Window Inside)" and "Indent Normal" to "Indent (Window Outside)"
- Fix issue where WindowTabs tabs go behind UWP applications
  - Added TOPMOST flag for all UWP app windows regardless of tab position
  - Handle UWP app Z-order changes to maintain tab visibility
  - Automatically remove TOPMOST flag when non-UWP window or window outside group gets focus
  - Insert tabs after the new foreground window when removing TOPMOST
- Prevent multiple settings dialogs from opening simultaneously
  - Settings dialog closes existing instance before opening new one
  - Only one settings dialog can be open at a time

## version ss_jp_2025.08.04
- Remove "Combine icons in taskbar" feature
  - This feature is not supported on Windows 11
  - Removed combineIconsInTaskbar setting from all related files
  - Always pass false to createGroup() to disable SuperBarPlugin
- Remove ALT+TAB replacement and task switcher features
  - Removed replaceAltTab setting that replaced ALT+TAB with WindowTabs task switcher
  - Removed groupWindowsInSwitcher setting that grouped windows in task switcher
  - Deleted TaskSwitch.fs file and removed from project
  - Removed all related UI controls and settings
- Delete: Fix tabs overlap the minimize button when align right
  - This item can be configured in the settings, so no source code modifications are necessary.
- Improve Japanese translations for tab context menu
- Add "Close tabs to the right" feature
  - Added new menu item "Close tabs to the right"
  - Closes all tabs positioned to the right of the current tab
  - Added onCloseRightTabWindows method in TabStripDecorator.fs
- Remove "Close all tabs of specific process" feature
  - Removed menu item "Close all %s tabs in this window"
  - Deleted onCloseAllExeWindows method
  - Simplified tab closing options in context menu
- Add "Close tabs to the left" feature
  - Added new menu item "Close tabs to the left"
  - Closes all tabs positioned to the left of the current tab
  - Added onCloseLeftTabWindows method in TabStripDecorator.fs
  - Menu item positioned right after "Close tabs to the right"
- Remove "Don't use tabs for %s" and "Auto-group %s" menu items
  - Removed menu item "Don't use tabs for %s"
  - Removed menu item "Auto-group %s"
  - These settings can be easily configured in the settings dialog
  - Simplified tab context menu by removing redundant options
- Reorganize tab context menu order for better user experience
- Disable mouse wheel tab switching functionality
  - Removed MouseScrollPlugin from Desktop.fs
  - Deleted MouseScrollPlugin.fs file and removed from project
  - Mouse wheel scrolling over tabs no longer switches between tabs

## version ss_jp_2025.08.03
- Fix null exception when toggling Fade out option
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/dce4f67
- Update Resources.ja-JP.resx hideInactiveTabs 
- Fix desktop Programs title missing issue (from leafOfTree)
  - Added missing "Programs" value in Resources.resx
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/4314877
- Add Font resource for UI consistency
  - Added "Font" resource with value "Segoe UI" in Resources.resx
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/ac0df82
- Re-add SmoothNodeTextBox implementation for better text rendering
  - Added SmoothNodeTextBox class with ClearTypeGridFit text rendering
  - Updated TaskSwitch.fs to use SmoothNodeTextBox and increased RowHeight (36→48)
  - Updated ProgramView.fs to use SmoothNodeTextBox and increased RowHeight (18→24)
  - Updated WorkspaceView.fs to use SmoothNodeTextBox and increased RowHeight (18→24)
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/a62c0d6
- Add option to deactivate hotkeys ctrl+1,ctrl+2
  - Added enableCtrlNumberHotKey setting to control numeric tab hotkeys
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/c416a49
- Update option default values and text
  - Changed enableCtrlNumberHotKey Japanese text to "Activate tabs with Ctrl+1...+9"
  - Changed enableCtrlNumberHotKey default value to false
  - Changed hideInactiveTabs default value to false
- Remove all peek code to fix alt-tab error
  - Removed DWM preview functionality from TaskSwitch.fs
  - Removed peekTimer, doShowPeek, and peekSelected method
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/4fed82a
- Add New window item to tab context menu
  - Added "New window" option to tab right-click menu
  - Launches a new instance of the same application
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/96c6387
- Improve task switch form appearance and window filtering
  - Apply FormBorderStyle.None for modern borderless appearance
  - Filter out windows with empty text and 'Microsoft Text Input Application'
  - Enhance Alt+Tab experience
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/3b4fd83
- Add diagnostic view improvements
  - Add button to copy settings file to exe path for easier troubleshooting
  - Add toolbar separator for better UI organization
  - Enhance support capabilities
  - leafOfTree commits: https://github.com/leafOfTree/WindowTabs/commit/faf7623, https://github.com/leafOfTree/WindowTabs/commit/cf3089f
- Fix WindowTabs own alt+tab collapse when there is no window
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/5cb3cf5
- Add a text color option to the setting appearance panel
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/fce3a8d
  - Added tabTextColor property to TabAppearanceInfo
- Update text color in Resources.ja-JP.resx
- Add color theme dark mode and dark mode blue variant
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/d582a4f
  - Added Dark Mode and Dark Mode (Blue) appearance options
- Adjust dark mode blue colors
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/e3f1df0
  - Adjusted Dark Mode (Blue) color scheme for better visibility
- Support mouse hover to active tab
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/34d8dd1
  - Added option to activate tabs on mouse hover

## version ss_jp_2025.08.02
- Fix Window Title Icon Size
- Add tooltip support

## version ss_jp_2025.07.19
- Support compiles with VS2022 Community Edition.
- Place WindowTabs.exe and required DLLs in the exe folder.
- Multi-display support, multi-DPI support.

## version ss_jp_2020.08.03
- Japanese text support
- Default tab alignment set to right
- Default auto-hide set to false
- ./exe/WindowTabs/WindowTabs.exe
- Support compiles with VS2017 Community Edition.
