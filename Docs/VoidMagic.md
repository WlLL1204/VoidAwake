# VoidMagic（捻じれた魔術）システム解説

> 開発の出発点。繋がり Def が対象・数値・得られるアビリティを決め、アビリティ側が効果を持つ。
> 段階に中身があるのは **サイトスティーラーだけ**。他種族は既定ダミー [`VoidAwake_VoidMagicDefault`](../Defs/VoidAwake_VoidMagicDefs/VoidMagics_VoidAwake.xml) に落ち、タブは「能力は未実装」と出す。

## 一言でいうと

収容したアノマリーの近くで**瞑想**すると、その入植者とアノマリー種の**繋がり**が伸びていき、閾値を超えるごとに段階が解放される。アノマリーを失う／瞑想を放置すると繋がりは減衰し、閾値を割ると段階を失う。進捗は入植者の専用タブ「捻じれた魔術」の横棒ゲージで確認できる。

---

## 全体構成

```mermaid
flowchart TB
  Spot[VoidAwake_VoidMeditationSpot]
  Anchor[VoidAwake_CompMeditationAnchor]
  Platform[Building_HoldingPlatform.HeldPawn]
  Job[VoidAwake_VoidMeditate]
  PawnComp[VoidAwake_CompVoidMagic]
  MagicDef[VoidAwake_VoidMagicDef]
  Tab[VoidAwake_ITab_Pawn_VoidMagic]

  Spot --> Anchor
  Anchor -->|"radius 9.9 を走査"| Platform
  Anchor -->|"フロートメニューで発行"| Job
  Job -->|"瞑想状態にする"| PawnComp
  Vanilla["バニラの瞑想 (JobDriver_Meditate)"] -->|"瞑想状態にする"| PawnComp
  PawnComp -->|"250 tick ごとに周囲を見て加算"| Platform
  MagicDef -->|"閾値 / 成長 / 減衰"| PawnComp
  PawnComp --> Tab
```

| 役割 | パス |
|------|------|
| 繋がりの定義（既定テンプレート） | [`Defs/VoidAwake_VoidMagicDefs/VoidMagics_VoidAwake.xml`](../Defs/VoidAwake_VoidMagicDefs/VoidMagics_VoidAwake.xml) / [`VoidAwake_VoidMagicDef.cs`](../Source/VoidAwake/Systems/VoidMagic/VoidAwake_VoidMagicDef.cs) |
| サイトスティーラー専用の段階 | [`VoidMagics_Sightstealer.xml`](../Defs/VoidAwake_VoidMagicDefs/VoidMagics_Sightstealer.xml) + [`Abilities_Sightstealer.xml`](../Defs/AbilityDefs/Abilities_Sightstealer.xml) + [`Hediffs_Sightstealer.xml`](../Defs/HediffDefs/Hediffs_Sightstealer.xml) |
| Def 解決・アノマリー判定 | [`VoidAwake_VoidMagicUtility.cs`](../Source/VoidAwake/Systems/VoidMagic/VoidAwake_VoidMagicUtility.cs) |
| 入植者ごとの繋がり保存・減衰・段階付与 | [`VoidAwake_CompVoidMagic.cs`](../Source/VoidAwake/Systems/VoidMagic/VoidAwake_CompVoidMagic.cs) |
| 瞑想スポット | [`Buildings_VoidMeditationSpot.xml`](../Defs/ThingDefs_Buildings/Buildings_VoidMeditationSpot.xml) + [`VoidAwake_Building_VoidMeditationSpot.cs`](../Source/VoidAwake/Systems/VoidMagic/VoidAwake_Building_VoidMeditationSpot.cs) |
| 半径スキャン・フロートメニュー | [`VoidAwake_CompMeditationAnchor.cs`](../Source/VoidAwake/Systems/VoidMagic/VoidAwake_CompMeditationAnchor.cs) |
| 瞑想 Job | [`Jobs_VoidMagic.xml`](../Defs/JobDefs/Jobs_VoidMagic.xml) + [`VoidAwake_JobDriver_VoidMeditate.cs`](../Source/VoidAwake/Systems/VoidMagic/VoidAwake_JobDriver_VoidMeditate.cs) |
| 専用タブ UI | [`VoidAwake_ITab_Pawn_VoidMagic.cs`](../Source/VoidAwake/Systems/VoidMagic/VoidAwake_ITab_Pawn_VoidMagic.cs) |
| comps / タブ / Royalty 連携パッチ | [`Patches/Patch_VoidMagic.xml`](../Patches/Patch_VoidMagic.xml) |
| Dev デバッグ | [`VoidAwake_DebugActions_VoidMagic.cs`](../Source/VoidAwake/Systems/VoidMagic/VoidAwake_DebugActions_VoidMagic.cs)（VoidAwake → VoidMagic: add connection +25 / fill connection / clear connections / dump links） |
| 文言 | [`English`](../Languages/English/Keyed/VoidAwake_VoidMagic.xml) / [`Japanese`](../Languages/Japanese/Keyed/VoidAwake_VoidMagic.xml) + [`DefInjected`](../Languages/Japanese/DefInjected) |

---

## 対象アノマリーの決まり方

Def を 1 体ずつ書く必要はない。`VoidAwake_VoidMagicUtility.IsLinkableEntityDef` が **`CompProperties_HoldingPlatformTarget` を持つ非人型 ThingDef** を収容可能アノマリーとみなし、`DefFor` が

1. `entityDef` 指定の `VoidAwake_VoidMagicDef` があればそれを使う
2. なければ既定テンプレート `VoidAwake_VoidMagicDefault` を使う

という順で解決する。つまり Anomaly DLC の収容可能エンティティは**追加作業なしで全種が対象**になる。人型を除外しているのは、シャンブラーやクリープジョイナーが `Human` ThingDef になり「human との繋がり」という表示になってしまうため。

個別に数値や段階を変えたい場合は、`entityDef` を指定した Def を追加する。書き方は次節。完成例は[サイトスティーラーの段階](#サイトスティーラーの段階)。

---

## Def テンプレート仕様

繋がり Def が「誰に・いつ・どの AbilityDef / HediffDef を渡すか」を決め、アビリティ側が効果を持つ。効果は XML のバニラ comps が基本で、独自 C# は必要なときだけ足す。

```mermaid
flowchart LR
  MagicDef["VoidAwake_VoidMagicDef"]
  Tier["tiers"]
  Ability["AbilityDef"]
  Hediff["HediffDef"]
  Comp["ApplyTierContent"]
  MagicDef -->|"entityDef 数値"| Tier
  Tier -->|"abilities"| Ability
  Tier -->|"hediff"| Hediff
  Comp -->|"GainAbility / AddHediff"| Ability
```

### 繋がり Def（`VoidAwake_VoidMagicDef`）

[`VoidAwake_VoidMagicDef.cs`](../Source/VoidAwake/Systems/VoidMagic/VoidAwake_VoidMagicDef.cs) のフィールド。ルート XML は `VoidAwake.VoidAwake_VoidMagicDef`。`defName` は `VoidAwake_VoidMagic_<Entity>`。

| フィールド | 役割 |
|-----------|------|
| `entityDef` | 対象アノマリーの ThingDef。省略すると既定ダミーになる |
| `maxConnection` | 繋がり上限 |
| `connectionPerHourMeditating` | 瞑想 1 時間あたりの獲得 |
| `decayPerDayLost` | 対象が収容されていないときの 1 日あたり減衰 |
| `decayPerDayIdle` | 収容中だが瞑想放置の 1 日あたり減衰 |
| `idleGraceDays` | 放置減衰が始まるまでの猶予日数 |
| `tiers` | 段階リスト。`threshold` 昇順に解決される |

各段階（`VoidAwake_VoidMagicTier`）:

| フィールド | 型 | 役割 |
|-----------|----|------|
| `label` | string | 段階名 |
| `threshold` | float | 繋がりがこの値以上で解放 |
| `abilities` | `AbilityDef` のリスト | 解放中は `GainAbility`、閾値割れで `RemoveAbility` |
| `hediff` | `HediffDef` 1 つ | 解放中は常時付与、閾値割れで除去 |

`abilities` も `hediff` も空でよい。両方同時も可。`HasContent` はどちらかがあれば true。

付与・剥奪は [`VoidAwake_CompVoidMagic.ApplyTierContent`](../Source/VoidAwake/Systems/VoidMagic/VoidAwake_CompVoidMagic.cs) が Def のリストをそのまま使う。どの能力かを C# 側で選んではいない。

### 既定ダミー

専用 Def が無いアノマリーは全部 [`VoidAwake_VoidMagicDefault`](../Defs/VoidAwake_VoidMagicDefs/VoidMagics_VoidAwake.xml) を共有する。種族ごとのプレースホルダは無い。

- `entityDef` なし
- 数値と段階名（25 / 50 / 75 / 100、微かな共鳴 / 共振 / 深き共鳴 / 同化）だけ
- `abilities` / `hediff` は空 → タブツールチップは「能力は未実装」

### アビリティ側

繋がり Def が知っているのは **AbilityDef の defName** だけ。効果は別ファイルの `AbilityDef`（必要なら `HediffDef`）に置く。

- サイトスティーラーは **XML のバニラ comps のみ**（`CompProperties_AbilityGiveHediff` など）。VoidAwake 独自クラスは付いていない
- 独自挙動が要るときだけ `verbClass` や自前 `CompAbilityEffect` を書く

完成例: [`VoidMagics_Sightstealer.xml`](../Defs/VoidAwake_VoidMagicDefs/VoidMagics_Sightstealer.xml) + [`Abilities_Sightstealer.xml`](../Defs/AbilityDefs/Abilities_Sightstealer.xml) + [`Hediffs_Sightstealer.xml`](../Defs/HediffDefs/Hediffs_Sightstealer.xml)

### 新規アノマリーのファイルセット

Sightstealer をコピーして `entityDef` と defName を差し替える。

- `Defs/VoidAwake_VoidMagicDefs/VoidMagics_<主題>.xml` — 繋がり Def（必須）
- `Defs/AbilityDefs/Abilities_<主題>.xml` — 段階で使う能力があれば
- `Defs/HediffDefs/Hediffs_<主題>.xml` — 常時効果があれば
- `Languages/Japanese/DefInjected/VoidAwake_VoidMagicDef/VoidMagics_<主題>.xml` — 段階名など。AbilityDef / HediffDef も必要なら同様
- C# の `<Compile Include>` は、独自クラスを足すときだけ。Def とバニラ comps だけなら不要

雛形:

```xml
<VoidAwake.VoidAwake_VoidMagicDef>
	<defName>VoidAwake_VoidMagic_Revenant</defName>
	<label>revenant resonance</label>
	<description>...</description>
	<entityDef>Revenant</entityDef>
	<maxConnection>100</maxConnection>
	<connectionPerHourMeditating>5</connectionPerHourMeditating>
	<decayPerDayLost>6</decayPerDayLost>
	<decayPerDayIdle>1</decayPerDayIdle>
	<idleGraceDays>3</idleGraceDays>
	<tiers>
		<li>
			<label>faint resonance</label>
			<threshold>25</threshold>
			<hediff>VoidAwake_SomeHediff</hediff>
		</li>
		<li>
			<label>second tier</label>
			<threshold>50</threshold>
			<abilities>
				<li>VoidAwake_SomeAbility</li>
			</abilities>
		</li>
	</tiers>
</VoidAwake.VoidAwake_VoidMagicDef>
```

---

## 瞑想の流れ

加算は**ジョブではなく `VoidAwake_CompVoidMagic` 側**で行う。`CompTickRare`（≒250 tick ごと）で「その入植者が今瞑想しているか」を見て、周囲の収容アノマリーへ繋がりを配る。

```mermaid
sequenceDiagram
  participant P as 入植者
  participant C as VoidMagicComp
  participant A as 収容アノマリー

  Note over P: 捻じれた瞑想 / バニラの瞑想 / 娯楽の瞑想
  loop 250 tick ごと
    C->>P: 瞑想中か？（IsMeditatingNow）
    C->>A: 足元のスポット半径、無ければ 9.9 で走査
    A->>C: 繋がり加算（対象数で分割）
  end
  C->>C: 閾値を超えたら段階を解放
```

- 瞑想の判定は `VoidAwake_VoidMagicUtility.IsMeditationJob`。自前の `VoidAwake_VoidMeditate` に加えて、**`JobDriver_Meditate` を driverClass に持つジョブ全て**（バニラの `Meditate`、娯楽としての瞑想、Royalty のサイフォーカス瞑想、それらを継承した他 mod のジョブ）が対象。移動中は加算しない。
- 半径は足元に瞑想スポット（`VoidAwake_CompMeditationAnchor`）があればその `radius` / `gainMultiplier` を使い、無ければ `DefaultMeditationRadius` = 9.9。つまり**スポットが無くても収容所の近くで瞑想すれば伸びる**が、スポットを置けば倍率などを調整できる。
- スポット側のロジックは全て `VoidAwake_CompMeditationAnchor` に入っているため、**自前のスポットでもバニラの `MeditationSpot`（Royalty）でも同じように動く**。Royalty 側へは `Patches/Patch_VoidMagic.xml` が `success=Always` で comp を差し込む（Royalty 未所持なら xpath が一致せず無視される）。
- 範囲内に対象が複数いる場合は獲得量を頭数で割るので、**1 体に絞った配置の方が速く伸びる**。
- 捻じれた瞑想ジョブは 60 tick ごとに範囲内の収容アノマリーを確認し、居なくなったらメッセージを出して中断する。
- 建物を選択すると半径リングを描画し、Inspect 欄に範囲内のアノマリー名を並べる。

---

## 減衰と段階

加算と同じ `VoidAwake_CompVoidMagic.CompTickRare`（≒250 tick ごと、経過 tick から日数換算するのでティック間隔に依存しない）で判定する。加算を先に行うため、瞑想中の繋がりが減ることはない。

- 対象種が**どのマップにも収容されていない**：`decayPerDayLost` で減衰（喪失）
- 収容中だが**最後の瞑想から `idleGraceDays` 経過**：`decayPerDayIdle` で減衰（放置）
- 収容中かつ猶予内：減衰なし（維持）

収容状況の判定は `VoidAwake_VoidMagicUtility.ContainedEntityDefsNow()` が全プレイヤーマップの収容プラットフォームを 600 tick キャッシュで走査する。繋がりが 0 まで落ちた行はリストから削除され、タブから消える。

段階が上下すると `ApplyTierContent` が走り、解放済み段階の `abilities` / `hediff` を付与、失った段階のものを剥奪する。既定テンプレートは段階に中身が無いため実質何もしないが、サイトスティーラーのように中身を書いた Def ではそのまま能力の増減になる。段階の増減時は入植者に対してメッセージが出る。

種類を問わず、全繋がりの％合計が **50 ごとに心情 -2** が重なる（`VoidAwake_TwistedBondStrain`）。スタックに上限はない。サイトスティーラー 75% とキメラ 25% なら合計 100% で 2 スタック（-4）。49% 以下では付かない。

---

## サイトスティーラーの段階

`VoidAwake_VoidMagic_Sightstealer` が `entityDef` に `Sightstealer` を指定して既定テンプレートを上書きしている。段階に中身を入れた唯一の Def で、C# の追加は無く Def だけで成立している。得られるアビリティは **2 つ**（サイトスティール / 不可視の帳）。夜目は常時 hediff。

| 段階 | 閾値 | 中身 |
|------|------|------|
| 夜目 | 25 | 常時 hediff `VoidAwake_NightEyes`。移動速度 +0.3 / 近接回避 +4 / 精神感応度 +0.2 |
| サイトスティール | 50 | 能力 `VoidAwake_StealPresence`。自分だけ 20 秒透明化（`PsychicInvisibility`）。CD 30000 tick |
| 不可視の帳 | 100 | 能力 `VoidAwake_VeilOfThePack`。自分と周囲 9.9 の味方を 30 秒まとめて透明化。CD 60000 tick |

- 喪失減衰だけ既定の 6.0 から 4.0 に緩めてある。対象を一時的に失っただけで能力まで消えるのを避けるため。
- `Ability_Duration` の単位は秒（60 tick）で、術者の精神感応度が倍率としてかかる。夜目で感応度が上がるので透明化の時間も自然に伸びる。
- 不可視の帳は 2 段構え。術者に付くオーラ hediff `VoidAwake_VeilOfThePack` の `HediffCompProperties_GiveHediffsInRange` が、範囲内の入植者へ透明化 hediff `VoidAwake_VeiledPack` を配る。範囲外に出た味方は `VoidAwake_VeiledPack` 側の `HediffCompProperties_Link`（`maxDistance` 10）が剥がすため、追従処理を自前で持つ必要がない。Ideology の `CombatCommand` と同じ作りだが、使っている comp は本体側（Assembly-CSharp）にあるので Ideology は不要。

```mermaid
flowchart LR
  Ability["能力: 不可視の帳"] --> Aura["オーラ hediff<br/>VoidAwake_VeilOfThePack"]
  Aura -->|"GiveHediffsInRange 9.9"| Buff["VoidAwake_VeiledPack"]
  Buff -->|"HediffCompProperties_Invisibility"| Hidden["味方が透明化"]
  Buff -->|"Link maxDistance 10"| Removed["範囲外で解除"]
```

- アイコンは暫定でバニラの `UI/Abilities/RevenantInvisibility` を流用している。専用テクスチャは未着手。

---

## タブ UI

- `inspectorTabs` はバニラの `BasePawn`（`Data/Core/Defs/ThingDefs_Races/Races_Animal_Base.xml`）側で定義されているため、そこへ 1 行追加している。動物やメカにも付くが `IsVisible` が「Anomaly 有効 かつ プレイヤー操作の入植者 かつ comp 持ち」で絞るので表示されない。
- 行は「繋がりを持つアノマリー」＋「現在収容中のアノマリー」の合併。まだ 0 の収容アノマリーも行として出るので、これから伸ばせる対象が分かる。
- 横棒ゲージ上に各段階の閾値マーカーを描き、到達済みは黄色、未到達は灰色。右側に現在の段階名と状態（維持／喪失 -X/日／放置 -X/日／瞑想したことがない）を出す。
- 行のツールチップに段階一覧（未実装の能力は「能力は未実装」と表示）と成長・減衰の数値を出す。

---

## 数値まとめ

| 項目 | 値 | 備考 |
|------|----|------|
| 繋がり上限 | 100 | `maxConnection` |
| 瞑想での獲得 | 5.0 / 1時間 | `connectionPerHourMeditating`、2500 tick 換算。範囲内の対象数で分割 |
| 加算・減衰の判定間隔 | 250 tick | `VoidAwake_CompVoidMagic.UpdateIntervalTicks` |
| 捻じれた瞑想 1 回の長さ | 2500 tick（約1時間） | `VoidAwake_JobDriver_VoidMeditate.MeditateTicks` |
| スポットの検出半径 | 9.9 セル | `VoidAwake_CompProperties_MeditationAnchor.radius` |
| スポット無しの検出半径 | 9.9 セル | `VoidAwake_VoidMagicUtility.DefaultMeditationRadius` |
| 喪失時の減衰 | 6.0 / 日 | `decayPerDayLost` |
| 放置の猶予 | 3 日 | `idleGraceDays` |
| 放置時の減衰 | 1.0 / 日 | `decayPerDayIdle` |
| 収容状況キャッシュ | 600 tick | `VoidAwake_VoidMagicUtility.ContainedScanIntervalTicks` |
| 段階の閾値 | 25 / 50 / 75 / 100 | 既定は 微かな共鳴 / 共振 / 深き共鳴 / 同化。能力は未設定 |
| サイトスティーラーの喪失減衰 | 4.0 / 日 | 既定より緩い。それ以外の数値は既定と同じ |

---

## 未実装（拡張ポイント）

- サイトスティーラー以外の種族専用 Def。既定ダミーのままなので、[Def テンプレート仕様](#def-テンプレート仕様) に従って `entityDef` 付き Def と Ability / Hediff を足す。
- 繋がりのデメリット：全アノマリー種の繋がり％を合計し、**50% ごとに心情 -2 が 1 スタック（上限なし）**（例: サイトスティーラー 75% + キメラ 25% = 100% → 2 スタックで -4）。種類は問わない。
- 繋がりの数値に応じた連続的な効果スケーリング。今の Def 構造は段階単位の付与しか表現できないため、必要なら C# 側の拡張が要る。
- 研究前提、放射状（星座風）グラフ表示。
- 超能力の専用アイコン。
- 瞑想スポットの専用テクスチャ（現在はバニラの `Things/Building/Misc/PartySpot` を暫定利用）。
