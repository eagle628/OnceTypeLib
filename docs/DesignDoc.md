# OnceTypeLib 設計書 (Design Doc)

- [OnceTypeLib 設計書 (Design Doc)](#oncetypelib-設計書-design-doc)
  - [概要と開発動機 (Overview \& Motivation)](#概要と開発動機-overview--motivation)
  - [コア設計原則 (Core Principles)](#コア設計原則-core-principles)
  - [型一覧・特性比較 (Type List \& Matrix)](#型一覧特性比較-type-list--matrix)
    - [各型の詳細と解決する課題](#各型の詳細と解決する課題)
      - [`WriteOnce<T>`（遅延セット・後から確定）](#writeoncet遅延セット後から確定)
      - [`ReadOnce<T>`（線形消費・機密保護）](#readoncet線形消費機密保護)
      - [`WriteOnceReadOnce<T>`（セーフハンドオフ・使い捨て）](#writeoncereadoncetセーフハンドオフ使い捨て)
  - [API 仕様 (API Specifications)](#api-仕様-api-specifications)
    - [`WriteOnce<T>`](#writeoncet)
    - [`ReadOnce<T>`](#readoncet)
    - [`WriteOnceReadOnce<T>`](#writeoncereadoncet)
    - [`OnceExtensions`（厳格モード用ヘルパー）](#onceextensions厳格モード用ヘルパー)
  - [設計上の重要判断 (Design Decisions)](#設計上の重要判断-design-decisions)
  - [テスト・検証要件 (Testing Requirements)](#テスト検証要件-testing-requirements)

## 概要と開発動機 (Overview & Motivation)

`OnceTypeLib` は、特定のリソースに対する「1回限りのアクセス契約」を明示的な型として提供する、ゼロアロケーション指向の C# 軽量ユーティリティライブラリです。

- 開発の背景
  - 初期化タイミングのズレ: 業務アプリ等において、クラス群のインスタンス化後に非同期通信や環境ロードを経て1度だけ決定される環境値に対し、不変（イミュータブル）な書き込み制御を行いたい。
  - 機密情報の保護: 『Secure by Design』のドメインプリミティブ思想に基づき、パスワードやトークン等の機密データがメモリ上に長期残留したり、複数箇所から不正に参照されたりすることを防ぐため、読み出し直後に無効化する線形消費セマンティクスを提供したい。
  - ワンタイムバッファ: スレッド間やクラス間で安全に使い捨てデータを1回限りハンドオフしたい。

## コア設計原則 (Core Principles)

- 型安全性と完全性 (Type Safety & Integrity): 暗黙の値コピーによる状態破壊（Once保証の崩壊）を防ぐため、すべての型は `sealed class` として実装する。
- 純粋なTryパターン (Pure Try-Pattern Core): コア型は例外をスローせず、`bool TrySet` / `bool TryGet` によるステートマシンのみを提供する。
- ロックフリー設計 (Non-Blocking / Lock-Free): `lock` 文や同期プリミティブを全廃し、`Interlocked.CompareExchange` および `Volatile` によるロックフリーな状態遷移とメモリバリアを徹底する。
- 厳格モード拡張 (Strict Mode Extensions): ロジックエラーを即時検知したいユースケースのために、失敗時に例外を投げるヘルパー（`SetOrThrow` / `GetOrThrow`）を拡張メソッドとして提供する。

## 型一覧・特性比較 (Type List & Matrix)

| 型名 | 書き込み (Set) | 読み込み (Get) | 読み込みの副作用 | 主な用途 |
| :--- | :--- | :--- | :--- | :--- |
| `WriteOnce<T>` | 1回のみ | 何度でも可能 | なし（常に同じ値を返す） | 起動後の遅延設定値、環境コンテキストの注入 |
| `ReadOnce<T>` | 0回 (生成時のみ) | 1回のみ | あり（読んだら中身を無効化） | 平文パスワード、使い捨て認証トークンの保護 |
| `WriteOnceReadOnce<T>` | 1回のみ | 1回のみ | あり（読んだら中身を無効化） | スレッド間のワンタイムハンドオフ、使い捨てチケット |

### 各型の詳細と解決する課題

#### `WriteOnce<T>`（遅延セット・後から確定）

- 解決する課題: C# の `init` アクセサーはコンストラクタまたはオブジェクト初期化子内でしかセットできず、`Lazy<T>` は外部から値を後注入するユースケースには適さない。
- 想定用途: 起動時に非同期取得する環境設定値、DIコンテナ生成後に外から1度だけ決定されるコンテキスト情報。

#### `ReadOnce<T>`（線形消費・機密保護）

- 解決する課題: パスワード等の機密データが複数箇所から参照・複製されることを防ぎ、読まれた瞬間に中身を無効化（参照型の切り離しやメモリゼロクリア）して2回目の読み出しを検知・遮断する。
- 想定用途: 認証ヘッダー作成用の生トークン、一度暗号化したら即座に破棄したい平文パスワード。

#### `WriteOnceReadOnce<T>`（セーフハンドオフ・使い捨て）

- 解決する課題: 一度セットされたら一度しか取り出せないワンタイムバッファ（所有権移動・ムーブセマンティクスに近い挙動）を実現する。
- 想定用途: 別スレッドや別クラスへのワンタイムチケットの受け渡し、一度しか実行を許可しない使い捨てコールバック引数。

## API 仕様 (API Specifications)

### `WriteOnce<T>`

```csharp
namespace OnceTypeLib;

public sealed class WriteOnce<T>
{
    // 初期値なしで生成
    public WriteOnce();

    /// <summary>値がセット済みかどうかを取得します。</summary>
    public bool HasValue { get; }

    /// <summary>値を1度だけセットします。すでにセット済みの場合は false を返します。</summary>
    public bool TrySet(T value);

    /// <summary>値を取得します。未セットの場合は false を返します（複数回呼び出し可能）。</summary>
    public bool TryGet(out T? value);
}
```

- 内部ステートマシン: `0: Empty` ──(TrySet 成功時)──> `1: Ready`
  - TrySet: CAS（Compare-And-Swap）で `0 -> 1` の遷移に成功したスレッドのみが値を書き込み、メモリバリアを発行。
  - TryGet: 状態が `1` であれば `Volatile.Read` 経由で値を返却。

### `ReadOnce<T>`

```csharp
namespace OnceTypeLib;

public sealed class ReadOnce<T>
{
    /// <summary>初期値を設定して初期化します（書き込みAPIは持たない）。</summary>
    public ReadOnce(T value);

    /// <summary>まだ一度も読み出されていない（消費されていない）かどうかを取得します。</summary>
    public bool HasValue { get; }

    /// <summary>値を消費して取得します。2回目以降の呼び出しは false を返します。</summary>
    public bool TryGet(out T? value);
}
```

- 内部ステートマシン: `1: Unread` ──(TryGet 成功時)──> `0: Consumed`
  - TryGet: CAS で `1 -> 0` の遷移に成功したスレッドのみが値を取得し、内部フィールドを `default!` でクリア。

### `WriteOnceReadOnce<T>`

```csharp
namespace OnceTypeLib;

public enum WriteOnceReadOnceState
{
    /// <summary>値が未セットです。</summary>
    Empty = 0,

    /// <summary>値がセット済みで、未消費です。</summary>
    Ready = 1,

    /// <summary>値が消費済みです。</summary>
    Consumed = 2,
}

public sealed class WriteOnceReadOnce<T>
{
    // 初期値なしで生成
    public WriteOnceReadOnce();

    /// <summary>現在の状態を取得します。TrySet による書き込み中は Empty として報告されます。</summary>
    public WriteOnceReadOnceState State { get; }

    /// <summary>値がセット済み、かつ未消費であるかどうかを取得します（State == Ready と等価）。</summary>
    public bool HasValue { get; }

    /// <summary>値を1度だけセットします。セット済みまたは消費済みの場合は false を返します。</summary>
    public bool TrySet(T value);

    /// <summary>値を消費して取得します。未セットまたは2回目以降の呼び出しは false を返します。</summary>
    public bool TryGet(out T? value);
}
```

- 内部ステートマシン: `0: Empty` ──(TrySet 成功時)──> `1: Ready` ──(TryGet 成功時)──> `2: Consumed`
  - TrySet: CAS で `Empty -> Writing`（内部状態、値 3）に遷移できたスレッドのみが値を書き込み、`Volatile.Write` で `Ready` を公開する。
  - TryGet: CAS で `Ready -> Consumed` に遷移できたスレッドのみが値を取得し、内部フィールドを `default` でクリアする。
  - `Writing` は公開 enum に含めない内部状態であり、`State` プロパティでは `Empty` として報告される（下記「設計上の重要判断」3 を参照）。

### `OnceExtensions`（厳格モード用ヘルパー）

バグ検知を優先したいユースケース向けに、失敗時に例外をスローする拡張メソッド群を提供します。

```csharp
namespace OnceTypeLib;

public static class OnceExtensions
{
    // WriteOnce 向け
    public static void SetOrThrow<T>(this WriteOnce<T> source, T value);
    public static T GetOrThrow<T>(this WriteOnce<T> source);

    // ReadOnce 向け
    public static T GetOrThrow<T>(this ReadOnce<T> source);

    // WriteOnceReadOnce 向け
    public static void SetOrThrow<T>(this WriteOnceReadOnce<T> source, T value);
    public static T GetOrThrow<T>(this WriteOnceReadOnce<T> source);
}
```

## 設計上の重要判断 (Design Decisions)

1. 型構造（struct ではなく sealed class の採用）
   - C# の値型（struct）は代入や引数渡し時に暗黙のメモリコピーが発生するため、状態フラグが複製されて「Once保証」が容易に破綻します。安全性を担保するため `sealed class` が必須となります。
   - パフォーマンス面でも、`WriteOnce` は長期生存（Gen 2常駐）、`ReadOnce` は超軽量（Gen 0ですぐ回収）のため、GC負荷は無視できるレベルに抑えられます。
2. スレッドセーフ（ロックフリー設計）
   - 同期ロック（`lock` / `Monitor`）によるオーバーヘッドを避けるため、`Interlocked.CompareExchange` によるアトミックな状態遷移と、`Volatile` によるメモリバリア（命令再並べ替えの防止）を採用します。

3. 書き込み中状態（Writing）と `WriteOnceReadOnceState` の扱い
   - `WriteOnce<T>` / `WriteOnceReadOnce<T>` の `TrySet` は、値の書き込み完了前に `Ready` が公開されて未初期化の値を読まれることを防ぐため、`Empty -> Writing -> Ready` の2段階で遷移する。`Writing` は内部状態であり公開しない。
   - `WriteOnceReadOnceState`（`Empty = 0` / `Ready = 1` / `Consumed = 2`）は公開用の状態であり、`Writing` 中の `State` は `Empty` として報告される。
   - 既知のリスク（トレードオフ）:
     - `TrySet` の書き込み中は、実際には他スレッドが `TrySet` に失敗する（`false`）にもかかわらず、`State` は `Empty` を返す。つまり `State == Empty` は「`TrySet` が必ず成功する」ことを意味しない。`State` はあくまで観測時点のスナップショットであり、判定後の `TrySet` / `TryGet` の成否を保証しない。
     - `Writing` の継続時間は通常 `_value = value` の代入のみで、参照型や小さな値型ではほぼ 0 とみなせる。ただし `T` が巨大な構造体の場合はコピーコストに比例して長くなる。また、書き込みスレッドがこの間にプリエンプトされると、`T` のサイズに関わらず OS のスケジューリング遅延分だけ延びる。
   - 採用しなかった代替案:
     - `State` で `Writing` の間スピン待ちして `Ready` を返す案: `State` / `HasValue` がブロッキングになりロックフリー原則から外れるため不採用。
     - `Writing` を公開 enum に含める案: 公開 API が複雑になるため、シンプルさを優先して不採用。
   - 利用者向けには、README 等でこのリスクを明記する。
4. 厳格モード（`OrThrow`）の例外設計
   - `SetOrThrow` / `GetOrThrow` は「ここでは必ず成功する」という不変条件の表明であり、失敗はプログラムのバグを意味する。そのため例外は catch して回復する対象ではなく、`ArgumentNullException` と同様に最上位でログ出力して終了する扱いを想定する。回復したい場合は `TrySet` / `TryGet` を使う。
   - 未設定と消費済みはどちらも `InvalidOperationException` とし、区別はメッセージのみで行う。診断時に原因が読めれば足り、catch して分岐する利用者は想定しないためである。
   - 採用しなかった代替案:
     - 消費済み専用の例外型（`InvalidOperationException` の派生クラス等）を設ける案: 分岐する利用者がおらず、公開 API が増えるだけでシンプルさを失うため不採用。
     - 並行実行中の書き込み途中（`Writing`）を `GetOrThrow` で別メッセージにする案: 内部状態を例外処理に漏らす複雑さに見合わないため不採用。`Writing` 中の失敗は「未設定」として報告する。
   - 前提: `OrThrow` を使う箇所では、他スレッドとの同期（書き込み完了の happens-before）が済んでいることを呼び出し側が保証する。並行して競合しうる箇所では `Try` 系を使う。

## テスト・検証要件 (Testing Requirements)

- プロジェクト構成: `<ProjectName>/Test/<ProjectName>.Tests.csproj` 配下にテストプロジェクトを配置。
- テスト検証観点:
  1. 正常系 (Happy Path): 単一スレッドでの確実な値設定・取得。
  2. 境界値・再呼び出し: 2回目以降の `TrySet` / `TryGet` が確実に `false` を返し、内部状態が破壊されないこと。
  3. マルチスレッド競合 (Race Condition): `Parallel.For` やマルチタスク並行実行時に、複数スレッドから同時に `TrySet` / `TryGet` が呼ばれても、成功するスレッドが厳密に1つだけであること。
