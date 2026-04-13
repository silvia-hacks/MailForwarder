# MailForwarder

POP3 で受信したメールを定期的に確認し、指定した宛先へ SMTP で自動転送する Windows デスクトップアプリです。Avalonia UI で構成されており、常駐監視と手動実行の両方に対応しています。

## ダウンロード

- 最新版: [MailForwarder-win-x64.zip](https://github.com/silvia-hacks/MailForwarder/releases/latest/download/MailForwarder-win-x64.zip)

## 主な機能

- POP3 サーバーからメールを取得
- SMTP サーバー経由で指定アドレスへ転送
- 一定間隔での自動監視
- 1 通だけの手動確認・手動転送
- 転送済みメールの重複スキップ
- 転送後に元メールをサーバーから削除するオプション
- POP3 / SMTP 接続確認
- メイン画面でのログ表示
- トレイ常駐

## 対象環境

- .NET 8 SDK
- Windows 推奨

補足:

- Avalonia ベースのためコード自体はクロスプラットフォーム寄りですが、設定保存時のパスワード保護は Windows では DPAPI を使用します。
- 現在の publish 実績は `win-x64` です。

## 設定項目

アプリでは次の設定を扱います。

### POP3

- ホスト
- ポート
- SSL 使用有無
- ユーザー名
- パスワード

### SMTP

- ホスト
- ポート
- SSL 使用有無
- ユーザー名
- パスワード

### 転送

- 転送先メールアドレス
- 確認間隔（分）
- 転送後にサーバーから削除するか

## 動作概要

1. POP3 サーバーへ接続してメール一覧を取得します。
2. 既に転送済みのメールは履歴を使ってスキップします。
3. 未転送メールを SMTP で転送します。
4. オプション有効時は、転送済みメールを POP3 サーバーから削除します。

転送メールの件名は `Fwd:` 形式になり、本文の先頭に元メールの差出人・宛先・日時・件名を付与します。

## 開発用実行

```bash
dotnet restore
dotnet run
```

## Windows 向け publish

```bash
DOTNET_CLI_HOME=/tmp/dotnet-cli-home dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

生成物:

- `bin/Release/net8.0/win-x64/publish/MailForwarder.exe`

## GitHub Actions 配布

- GitHub Actions で `win-x64` 向けの単一 exe を build し、zip 化します。
- `v*` 形式のタグを push すると GitHub Release を作成し、`MailForwarder-win-x64.zip` を添付します。
- 手動実行時は Actions の artifact として zip を取得できます。

例:

```bash
git tag v1.0.0
git push origin v1.0.0
```

## 設定ファイルとログ

設定ファイルはローカルアプリケーションデータ配下に保存されます。

- 設定ファイル: `%LocalAppData%\\MailForwarder\\settings.json`
- POP3 接続テストログ: `%LocalAppData%\\MailForwarder\\logs\\`

Windows では保存時にパスワードを DPAPI で保護します。

## 使い方

1. アプリを起動します。
2. 設定画面で POP3 / SMTP / 転送先を入力します。
3. `POP3接続確認` と `SMTP接続確認` を実行します。
4. 設定を保存します。
5. 必要に応じて `1回実行` で動作確認します。
6. 問題なければ監視開始します。

## 注意事項

- POP3 のため、サーバー側フォルダ同期や IMAP 特有の状態管理は行いません。
- 重複判定は主に `Message-Id` を使用し、無い場合は差出人・件名・日時の組み合わせで代替します。
- SMTP の差出人アドレスは、基本的に SMTP ユーザー名、なければ POP3 ユーザー名から推定します。

## リポジトリ

- GitHub: `git@github.com:silvia-hacks/MailForwarder.git`
