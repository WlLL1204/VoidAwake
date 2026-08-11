# Trapper システム解説

## 一言でいうと

Sightstealer 風に**レターなしでマップ端から現れ**、**隠密中はコロニードア周りに熊罠を敷き**、条件を満たすと**可視化して戦闘**し、落ち着いたらまた隠密に戻る Anomaly エンティティ。

---

## 全体構成

```mermaid
flowchart TB
  Incident[VoidAwake_TrapperArrival]
  Spawn[Edge spawn 1-3 Trapper]
  Comp[VoidAwake_TrapperComp]
  Stealth[Stealth: invisible + place traps]
  Combat[Combat: visible + fight]
  Trap[VoidAwake_BearTrap]

  Incident --> Spawn --> Comp
  Comp --> Stealth
  Comp --> Combat
  Stealth --> Trap
  Trap -->|5 destroyed| Combat
```

| 役割 | パス |
|------|------|
| 種族 / PawnKind | [`Defs/ThingDefs/Entities/Trapper.xml`](../Defs/ThingDefs/Entities/Trapper.xml) |
| ThinkTree | [`Defs/ThinkTreeDefs/Trapper.xml`](../Defs/ThinkTreeDefs/Trapper.xml) |
| 状態・タイマー Comp | [`VoidAwake_TrapperComp.cs`](../Source/ClassLibrary1/Entitys/Basic/Trapper/VoidAwake_TrapperComp.cs) |
| 罠設置 AI | [`JobGiver_TrapperPlaceTrap.cs`](../Source/ClassLibrary1/Entitys/Basic/Trapper/JobGiver_TrapperPlaceTrap.cs) |
| 罠設置 Job | [`JobDriver_PlaceBearTrap.cs`](../Source/ClassLibrary1/Entitys/Basic/Trapper/JobDriver_PlaceBearTrap.cs) / [`Trapper_Job.xml`](../Defs/Trapper/Trapper_Job.xml) |
| 熊罠 | [`BearTrap.xml`](../Defs/ThingDefs/Buildings/BearTrap.xml) + [`Building_VoidAwake_BearTrap.cs`](../Source/ClassLibrary1/Entitys/Basic/Trapper/Building_VoidAwake_BearTrap.cs) |
| 透明 Hediff | [`TrapperStealth.xml`](../Defs/HediffDefs/TrapperStealth.xml) |
| 襲来 Incident | [`Incidents_Trapper.xml`](../Defs/Storyteller/Incidents_Trapper.xml) + [`IncidentWorker_TrapperArrival.cs`](../Source/ClassLibrary1/Entitys/Basic/Trapper/IncidentWorker_TrapperArrival.cs) |
| Dev 起動 | [`DebugActions_Trapper.cs`](../Source/ClassLibrary1/Entitys/Basic/Trapper/DebugActions_Trapper.cs)（VoidAwake → Trapper arrival） |
| 妨害フレア連携 | [`Patch_TrapperStealth_DisruptInvisibility.cs`](../Source/ClassLibrary1/Entitys/Basic/Trapper/Patch_TrapperStealth_DisruptInvisibility.cs) |

---

## 状態マシン（中核）

```mermaid
stateDiagram-v2
  [*] --> Stealth
  Stealth --> Combat: damage_or_disruptor_or_5trapsDestroyed
  Combat --> Stealth: afterMin1h_and_noReachableColonist_3s
```

状態は [`VoidAwake_TrapperComp`](../Source/ClassLibrary1/Entitys/Basic/Trapper/VoidAwake_TrapperComp.cs) の `TrapperCombatState`（`Stealth` / `Combat`）。ThinkTree は [`ThinkNode_ConditionalTrapperStealth` / `Combat`](../Source/ClassLibrary1/Entitys/Basic/Trapper/ThinkNode_ConditionalTrapperState.cs) で分岐。

### Stealth（隠密）

- Hediff `VoidAwake_TrapperStealth`（`HediffCompProperties_Invisibility`、`visibleToPlayer` false）
- AI: ドア周囲に罠 → なければ徘徊。**積極戦闘なし**（`JobGiver_ReactToCloseMeleeThreat` のみ残る）
- 移動中は `footprintIntervalCells` = 2 マスごとに足跡 Fleck（`VoidAwake_TrapperFootstep` / `FootStep.png`）を残す
- 設置クールダウン `placeCooldownTicks` = 2500（約1時間）
- マップ罠上限 `maxTrapsOnMap` = 8

### Combat（交戦）

- 透明解除、可視化
- AI: `JobGiver_MetalhorrorFight` → 徘徊。**罠は置かない**
- 最低持続 `combatMinDurationTicks` = 2500（約1時間）。`EnterCombat()` のたびにタイマー更新
- 最低時間のあと、到達可能な入植者（人間・Downed 以外）が 0 の状態が `stealthReturnDelayTicks` = 180（約3秒）続くと Stealth 復帰

### Stealth → Combat のトリガー

1. **被ダメージ** — Comp `PostPostApplyDamage`
2. **妨害フレア** — Harmony で `HediffComp_Invisibility.DisruptInvisibility` をフック
3. **マップ上の熊罠が累計5つ破壊**（作動破壊含む）— `MapComponent_VoidAwake_TrapperTraps` がカウントし、5で全 Trapper を交戦へ
4. **罠作動（Spring）** — `SpringSub` で全 Trapper を交戦へ
5. **浮いたドアコンボ発動** — 隣接罠3個以上で全 Trapper を交戦へ

いずれの場合も **1匹が交戦に入ると同マップの全 Trapper が交戦**（`RevealAllTrappersOnMap` → `ApplyEnterCombat`）。

---

## 罠設置フロー

```mermaid
flowchart TD
  JG[JobGiver_TrapperPlaceTrap] --> Pick[ドア周囲8マスから候補]
  Pick --> Job[VoidAwake_PlaceBearTrap]
  Job --> Goto[移動]
  Goto --> Wait[短待機]
  Wait --> Spawn[BearTrapをEntities派閥でSpawn]
  Spawn --> Chain{同じドア3x3に空き?}
  Chain -->|yes| Queue[次の設置JobをEnqueue]
  Chain -->|no| CD[チェーン終了+クールダウン]
```

- 対象: コロニー `Building_Door` の周囲 3×3（ドア自身除く8マス）
- 連続設置はそのドアの周囲内だけ（通路へ広がらない）
- 候補条件: Standable・既存罠なし・到達可・`CanReserve`（セル予約）
- **ドア予約（Trapper 間）**: 設置開始時にドアを MapComponent で予約。他の Trapper は予約済みドアを選ばない。チェーン終了後も交戦/死亡まで保持
- セル予約失敗時はエラーログを出さない（`errorOnFailed: false`）

### 熊罠 `VoidAwake_BearTrap`

- `Building_TrapDamager` 相当（`TrapMeleeDamage` 80、作動で破壊）
- プレイヤー建築メニューには出ない（エンティティが `GenSpawn`）
- Trapper は PawnKind で `immuneToTraps`
- テクスチャ: `Textures/Entitys/Trapper/bearTrap.png`（`Transparent`、alpha 0.85）
- 地形に応じて草・苔・地表オーバーレイでカモフラージュ

---

## 浮いたドアコンボ

壁に挟まれていない**浮いたドア**を狩るための罠連動。通常の壁付きドアには効果なし。

```mermaid
flowchart LR
  DoorOpen[DoorOpen] --> Float{浮いたドア?}
  Float -->|no| Skip[何もしない]
  Float -->|yes| Count[上下左右の罠]
  Count -->|lt 3| Skip
  Count -->|gte 3| Blast[9マス罠ダメージ]
  Blast --> KillDoor[ドア破壊]
  KillDoor --> KillTraps[隣接罠破壊]
```

| 項目 | 内容 |
|------|------|
| 対象 | プレイヤー派閥の浮いたドア（cardinal 4マスの壁 ≤1、または standable ≥3） |
| カウント | ドアの上下左右のみ（対角は含めない） |
| 発動 | `Building_Door.DoorOpen` 時に隣接罠 **3個以上**（1〜2個は無効） |
| 効果 | ドア中心 3×3 のポーンに `TrapMeleeDamage`（Stab）→ ドア破壊 → 隣接罠全破壊 → **全 Trapper 交戦** |
| 紐 | 同じ浮いたドアの cardinal 罠同士を全ペアで紐描画（対面含む）。紐は不透明、罠は alpha 0.85 |
| 実装 | [`DoorTrapComboUtility.cs`](../Source/ClassLibrary1/Entitys/Basic/Trapper/DoorTrapComboUtility.cs) + [`Patch_Building_Door_DoorOpen.cs`](../Source/ClassLibrary1/Entitys/Basic/Trapper/Patch_Building_Door_DoorOpen.cs) |

コンボで壊れた罠も通常破壊と同様に `NotifyTrapDestroyed` へ加算される。

---

## 襲来イベント

- Def: `VoidAwake_TrapperArrival`（`hidden`、`baseChance` 0、レターなし）
- マップ端スポーン、脅威ポイントで 1〜3 体
- 起動方法:
  - Dev Mode → **VoidAwake → Trapper arrival**
  - Dev Mode → Incidents → Do incident (Map) → trapper arrival

---

## 数値まとめ（現行）

| 項目 | 値 |
|------|-----|
| 設置クールダウン | 2500 tick ≈ 1時間 |
| 足跡間隔（隠密移動） | 2 マス |
| 交戦最低時間 | 2500 tick ≈ 1時間 |
| 隠密復帰待ち（入植者不在後） | 180 tick ≈ 3秒 |
| マップ罠上限 | 8 |
| 破壊で交戦 | 5 |
| 襲来頭数 | points &lt; 400 → 1、&lt; 800 → 2、それ以上 → 3 |
| 罠ダメージ | TrapMeleeDamage 80 |
| 罠の透明度 | alpha 0.85 |
| 紐の透明度 | 不透明（alpha 1） |
| 浮いたドアコンボ | 隣接罠 3個以上で 9マスダメージ + ドア/罠破壊 + 全交戦 |
| 交戦伝播 | 1匹が交戦 → 同マップの全 Trapper が交戦 |
