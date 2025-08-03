# Standard-Software Version

## next version
- Remove "Combine icons in taskbar" feature
  - This feature is not supported on Windows 11
  - Removed combineIconsInTaskbar setting from all related files
  - Always pass false to createGroup() to disable SuperBarPlugin

## version ss_jp_2025.08.03
- Fix null exception when toggling Fade out option
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/dce4f67
- update Resources.ja-JP.resx hideInactiveTabs 
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
- Fix tabs overlap the minimize button when align right
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/d89acfc
- Fix WindowTabs own alt+tab collapse when there is no window
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/5cb3cf5
- Add a text color option to the setting appearance panel
  - leafOfTree commit: https://github.com/leafOfTree/WindowTabs/commit/fce3a8d
  - Added tabTextColor property to TabAppearanceInfo
- text color Resources.ja-JP.resx
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
- Tooltip

## version ss_jp_2025.07.19
- support compiles with VS2022 Community Edition.
- Place WindowTabs.exe and required DLLs in the exe folder.
- Multi-display support, multi-DPI support.

## version ss_jp_2020.08.03
- Japanese Text
- default Tab Align Right
- default AutoHide false
- ./exe/WindowTabs/WindowTabs.exe
- support compiles with VS2017 Community Edition.
