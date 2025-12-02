<img src="README_Image/LargeIcon.png" width="60" height="60" alt="icon" align="left" />

# WindowTabs

WindowTabs はインターフェースを持たない Windows アプリケーションや、異なる実行ファイル間でタブ UI を有効にするユーティリティです。Chrome と Edge をタブで管理、複数の Excel や Word のウィンドウをタブで管理が可能になります。

![Tabs](README_Image/Tabs.png)

元々は Maurice Flanagan 氏によって開発され、当時は無料版と有料版が提供されていました。開発者は現在、このユーティリティをオープンソース化しています:

**⚠ 404 Not Found**
- https://github.com/mauricef/WindowTabs

redgis 氏による VS2017 / .NET 4.0 に移行したフォーク:

**⚠ 404 Not Found**
- https://github.com/redgis/WindowTabs

payaneco 氏のソースコードをフォークしたリポジトリ:
- https://github.com/payaneco/WindowTabs
- https://github.com/payaneco/WindowTabs/network/members
- https://ja.stackoverflow.com/a/53822

leafOfTree 氏のソースコードをフォークしたリポジトリ:
- https://github.com/leafOfTree/WindowTabs
- https://github.com/leafOfTree/WindowTabs/network/members

そして私 (Satoshi Yamamoto@standard-software) がソースコードをフォークし、VS 2022 Community Edition でコンパイルを行っています。

## 目次
- [バージョン](#バージョン)
- [ダウンロード](#ダウンロード)
- [インストール](#インストール)
- [使用方法](#使用方法)
- [機能](#機能)
- [設定](#設定)
- [リンク](#リンク)
- [ライセンス](#ライセンス)
- [コメント](#コメント)

## バージョン

最新のバージョン: **ss_jp_2025.11.24**

詳細な更新履歴と変更ログについては、[version.md](version.md) を参照してください。


## ダウンロード

**対応している OS:** Windows 10、 Windows 11

<a href="https://github.com/standard-software/WindowTabs/releases">![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/standard-software/windowtabs/total)</a>

[releases](https://github.com/standard-software/WindowTabs/releases) ページからビルド済みのファイルをダウンロードできます。

2 つのダウンロードオプションがあります:

- **WtSetup.msi** - 自動インストールとアンインストールをサポートしている Windows インストーラーパッケージ版
- **WindowTabs.zip** - 任意の場所で展開して実行可能なポータブル版

提供しているビルドスクリプトを使用して、インストーラー版とポータブル版を自分でビルドすることもできます。

## インストール

### MSI インストーラー版の使用方法 (WtSetup.msi)

1. [Releases](https://github.com/standard-software/WindowTabs/releases) ページから `WtSetup.msi` をダウンロード
2. インストーラーを実行してインストールウィザードに従って操作します
3. インストール先のディレクトリを選択 (既定: Program Files\WindowTabs)
4. デスクトップとスタートメニューにショートカットが自動で作成されます
5. オプションでインストール後に WindowTabs を起動

### ポータブル版の使用方法 (WindowTabs.zip)

1. [Releases](https://github.com/standard-software/WindowTabs/releases) ページから `WtSetup.msi` をダウンロード
2. アーカイブを任意の場所に展開します
3. `WindowTabs.exe` を実行
4. WindowTabs がバックグラウンドで実行され、トレイアイコンが表示されます

WindowTabs をスタートアップ時に起動:
- オプションから`スタートアップ時に起動`を有効 > タブの動作

## 使用方法

1. `WindowTabs.exe` を実行
2. Window をグループ化すると自動でタブが表示されます
3. トレイアイコンを右クリックで設定にアクセスできます
4. タブを右クリックでタブ固有のオプションにアクセスできます
5. タブをドラッグ&ドロップでウィンドウを整理できます

## 機能

### タブのドラッグ&ドロップ

これは元の WindowTabs の機能から変更されていません。

- Drag tabs to reorder within the same group
- Drag tabs to separate into new windows with preview
- ウィンドウをドロップで新規タブグループを作成
- Respects tab alignment settings (left/center/right)

### タブの管理

- **タブのコンテキストメニュー**: 右クリックでタブの様々な機能にアクセスできます
  - タブを閉じる (このタブ、右側のタブ、左側のタブ、その他タブ、すべてのタブ)
  - 新規ウィンドウ
  - Make tabs wider / Make tabs narrower
  - タブの名前を変更
  - Detach and reposition tab
  - Reposition tab group
  - Detach and link tab to another group
  - Link tab group to another group
  - 設定

![Popup Menu](README_Image/PopupMenu.png)

### Detach and reposition tab

タブをグループから切り離しとマルチディスプレイのサポートで再配置します:
- Detach at same position
- Move to display edges (右/左/上/下)
- DPI-aware percentage-based positioning for correct placement across different DPI displays

![Detach Tab](README_Image/DetachTab.png)

### Reposition tab group

Move entire tab group to different display positions:
- Move to current display edges (右/左/上/下)
- Move to other displays with edge positioning options
- DPI-aware positioning for correct placement across different DPI displays
- Maintains tab group integrity while repositioning

![Reposition tab group](README_Image/MoveTabGroup.png)

### Detach and link tab to another group

Detach a single tab from current group and link it to another existing group:
- Shows other groups with tab names and counts
- Adaptive tab name truncation for easy identification
- Display application icons for group recognition
- Disabled when tab group has only one tab

![Detach and link tab to another group](README_Image/MoveTab.png)

### Link tab group to another group

Transfer all tabs from current group to another existing group:
- Shows other groups with tab names and counts
- Transfers all tabs at once from current group to target group
- Adaptive tab name truncation for easy identification
- Display application icons for group recognition

![Link tab group to another group](README_Image/MoveTabGroupToGroup.png)


### ダークモード / ライトモードのメニュー

While light mode is the default, dark mode is also supported for context menus (popup menus) as shown in the screenshots.

- Toggle via "Menu Dark Mode" checkbox in Appearance settings
- Applies to tab context menu and drag-and-drop menus

### マルチディスプレイと高 DPI をサポート

- Multi-display support with proper window positioning
- DPI-aware window placement
- Automatic window resizing when dropped to prevent exceeding monitor dimensions
- Fixed tab rename floating textbox positioning on high-DPI displays


### UWP アプリをサポート

- UWP (Universal Windows Platform) をサポート
- Automatically handles UWP window Z-order for proper tab visibility
- Maintains tab visibility when working with UWP apps


### 多言語をサポート

- 英語と日本語の言語をサポート
- Runtime language switching without restart
- トレイメニューから言語を変更可能

![Task Tray Menu](README_Image/TaskTrayMenuImage.png)

### 無効にしている機能

Temporarily disable WindowTabs functionality via tray menu:
- **Disable** checkbox in tray icon context menu
- When enabled:
  - Immediately hides all existing tab groups
  - Stops automatic tab grouping for new windows
  - Disables Settings menu to prevent configuration changes

## 設定

Access settings by right-clicking the tray icon and selecting "Settings" or by right-clicking on a tab and selecting "Settings...".

### プログラムタブ

This feature remains unchanged from the original WindowTabs functionality.

Configure which programs should use tabs and auto-grouping behavior.

![Settings Programs](README_Image/SettingsPrograms.png)

### タブの外観

タブの視覚的な外観をカスタマイズできます:
- Height, width, and overlap settings
- Border and text color
- 背景の色 (active, normal, highlight, flash)
- カラーテーマのプリセット (ライト、ダーク、ダークブルー)
- Distance from edge settings

![Settings Appearance](README_Image/SettingsAppearance.png)

### タブの動作

タブの動作を構成することができます:
- タブの位置 (左/中央/右)
- タブの幅 (縮小/拡張)
- Toggle tab width on active tab icon double-click
- Hide tabs when positioned at bottom (never/always/double-click)
- Delay before hiding tabs
- 自動グループ設定
- ホットキー設定 (Ctrl+1...+9 でタブをアクティブ)
- マウスホバーでアクティブ

![Settings Behavior](README_Image/SettingsBehavior.png)

### ワークスペースタブ

This feature remains unchanged from the original WindowTabs functionality.

### タブの診断

This feature remains unchanged from the original WindowTabs functionality.

## ソースからビルド

### Prerequisites

- Visual Studio 2022 Community Edition (またはそれ以降)
- WiX Toolset v3.11 またはそれ以降 (MSI インストーラー版のビルド)

### ビルドスクリプト

プロジェクトのルートに 2 種類のビルドスクリプトが用意されています:

- **build_installer.bat** - MSI インストーラー版をビルド (WtSetup.msi)
  - 出力: `exe\installer\WtSetup.msi`

- **build_release_zip.bat** - ポータブル ZIP 版の配布パッケージをビルド
  - 出力: `exe\zip\WindowTabs.zip`

必要なバッチファイルを実行で配布パッケージを作成することができます。


## リンク

### 日本語のリソース

[C# - WindowTabs というオープンソースを改良してみたいのですがビルドができません。何か必要なものがありますか？ - スタック・オーバーフロー](https://ja.stackoverflow.com/questions/53770/windowtabs-%E3%81%A8%E3%81%84%E3%81%86%E3%82%AA%E3%83%BC%E3%83%97%E3%83%B3%E3%82%BD%E3%83%BC%E3%82%B9%E3%82%92%E6%94%B9%E8%89%AF%E3%81%97%E3%81%A6%E3%81%BF%E3%81%9F%E3%81%84%E3%81%AE%E3%81%A7%E3%81%99%E3%81%8C%E3%83%93%E3%83%AB%E3%83%89%E3%81%8C%E3%81%A7%E3%81%8D%E3%81%BE%E3%81%9B%E3%82%93-%E4%BD%95%E3%81%8B%E5%BF%85%E8%A6%81%E3%81%AA%E3%82%82%E3%81%AE%E3%81%8C%E3%81%82%E3%82%8A%E3%81%BE%E3%81%99%E3%81%8B)

[全Windowタブ化。Setsで頓挫した夢の操作性をオープンソースのWindowTabsで再現する。 #Windows - Qiita](https://qiita.com/standard-software/items/dd25270fa3895365fced)

## ライセンス

This project is open source. See the original repository for license information.

## クレジット

- オリジナルの開発者: Maurice Flanagan
- フォークの貢献者: redgis、payaneco、leafOfTree
- 現在のメンテナー: Satoshi Yamamoto (standard-software)

## コメント

If you have any issues, please contact us via GitHub Issues or email: standard.software.net@gmail.com

Thanks to Claude Code's hard work, development has progressed significantly. However, I've given up on making the Settings dialog dark mode-compatible as I couldn't get it to look right. I'm hoping someone might fork this project and improve it.
