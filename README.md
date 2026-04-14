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
- Windows 起動時の自動起動設定
- 複数起動の抑止と既存ウィンドウの再表示
- メイン画面でのログ表示
- メイン画面での実行中表示とアプリバージョン表示
- トレイ常駐

## 対象環境

- .NET 8 SDK
- Windows 推奨

補足:

- Avalonia ベースのためコード自体はクロスプラットフォーム寄りですが、設定保存時のパスワード保護は Windows では DPAPI を使用します。
- 現在の publish 実績は `win-x64` です。

## 設定項目

アプリでは次の設定を扱います。

### 一般

- Windows 起動時に自動起動するか

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

詳細な POP3 接続テストログが必要な場合は、`--verbose` を付けて起動します。

```bash
dotnet run -- --verbose
```

## Windows 向け publish

```bash
DOTNET_CLI_HOME=/tmp/dotnet-cli-home dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

生成物:

- `bin/Release/net8.0/win-x64/publish/MailForwarder.exe`

## 設定ファイルとログ

設定ファイルはローカルアプリケーションデータ配下に保存されます。

- 設定ファイル: `%LocalAppData%\\MailForwarder\\settings.json`
- POP3 接続テストログ: `%LocalAppData%\\MailForwarder\\logs\\`

Windows では保存時にパスワードを DPAPI で保護します。

## 使い方

1. アプリを起動します。
2. 既に起動している状態で再度起動した場合は、既存のメイン画面が前面に表示されます。
3. 設定画面で自動起動の要否、POP3 / SMTP / 転送先を入力します。
4. 必要に応じて `Windows起動時に自動起動する` を有効にします。
5. `POP3接続確認` と `SMTP接続確認` を実行します。
6. 設定を保存します。
7. 必要に応じて `1回実行` で動作確認します。
8. 問題なければ監視開始します。

補足:

- `1回実行` や監視中のメール確認・転送は UI スレッドとは別で実行され、処理中はメイン画面に `実行中...` と表示されます。
- メイン画面右下にはアプリバージョンが表示されます。

## 注意事項

- POP3 のため、サーバー側フォルダ同期や IMAP 特有の状態管理は行いません。
- 重複判定は主に `Message-Id` を使用し、無い場合は差出人・件名・日時の組み合わせで代替します。
- SMTP の差出人アドレスは、基本的に SMTP ユーザー名、なければ POP3 ユーザー名から推定します。
- Windows の自動起動設定は、ユーザー単位の `Run` レジストリに登録します。
- Windows 配布物は未署名のため、ダウンロード後の初回起動時にセキュリティ警告が表示されることがあります。

## リポジトリ

- GitHub: `git@github.com:silvia-hacks/MailForwarder.git`
