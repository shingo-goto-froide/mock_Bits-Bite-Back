using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private GameBalanceSO balance;
    [SerializeField] private GameObject battleUnitPrefab;

    public List<BattleUnit> playerUnits = new List<BattleUnit>();
    public List<BattleUnit> enemyUnits = new List<BattleUnit>();
    public int currentTurn;
    public bool isAutoPlaying;
    [SerializeField] private float turnSpeed = 1f;

    private int allyDeathCountThisWave;
    private List<string> battleLog = new List<string>();

    public GameBalanceSO Balance => balance;

    public event Action OnBattleStartEvent;
    public event Action<int> OnTurnStartEvent;
    public event Action<BattleAction> OnUnitActionEvent;
    public event Action<BattleUnit> OnUnitDeathEvent;
    public event Action<BattleResult> OnBattleEndEvent;
    public event Action<string> OnLogEvent;

    public void Init(GameBalanceSO balanceSO)
    {
        balance = balanceSO;
    }

    public void SetupBattle(List<MonsterInstance> allies, EnemyWaveSO wave, MonsterDataSO[] allMonsterData)
    {
        ClearBattle();
        currentTurn = 0;
        allyDeathCountThisWave = 0;
        battleLog.Clear();

        // プレイヤーユニット生成
        for (int i = 0; i < allies.Count; i++)
        {
            var unit = CreateBattleUnit(allies[i], i, true);
            playerUnits.Add(unit);
        }

        // 敵ユニット生成
        int enemyIndex = 0;
        foreach (var entry in wave.enemies)
        {
            var enemyMonster = new MonsterInstance(entry.monsterData);
            // レベル補正
            if (entry.level > 1)
            {
                float multiplier = 1f + (entry.level - 1) * 0.5f;
                enemyMonster.maxHp = Mathf.RoundToInt(enemyMonster.maxHp * multiplier);
                enemyMonster.currentHp = enemyMonster.maxHp;
                enemyMonster.currentAttack = Mathf.RoundToInt(enemyMonster.currentAttack * multiplier);
            }
            var unit = CreateBattleUnit(enemyMonster, enemyIndex, false);
            enemyUnits.Add(unit);
            enemyIndex += entry.monsterData.slotSize;
        }
    }

    private BattleUnit CreateBattleUnit(MonsterInstance monster, int index, bool isPlayer)
    {
        GameObject go;
        if (battleUnitPrefab != null)
            go = Instantiate(battleUnitPrefab, transform);
        else
            go = new GameObject($"BattleUnit_{monster.baseData.monsterName}");

        go.transform.SetParent(transform);
        var unit = go.GetComponent<BattleUnit>();
        if (unit == null)
            unit = go.AddComponent<BattleUnit>();
        unit.Initialize(monster, index, isPlayer);
        return unit;
    }

    public void StartBattle()
    {
        isAutoPlaying = true;
        StartCoroutine(BattleRoutine());
    }

    private IEnumerator BattleRoutine()
    {
        OnBattleStartEvent?.Invoke();
        AddLog("=== バトル開始 ===");

        // OnBattleStart能力を発動
        ExecuteAbilitiesWithTrigger(AbilityTrigger.OnBattleStart);
        yield return new WaitForSeconds(turnSpeed);

        while (isAutoPlaying)
        {
            currentTurn++;
            OnTurnStartEvent?.Invoke(currentTurn);
            AddLog($"--- ターン {currentTurn} ---");

            // ターン開始時の状態異常処理
            ProcessAllTurnStartEffects();
            if (CheckAndEndBattle()) yield break;

            // 行動順を決定
            var actionOrder = GetActionOrder();

            foreach (var unit in actionOrder)
            {
                if (!unit.isAlive) continue;
                if (!isAutoPlaying) yield break;

                // ピヨリチェック
                if (unit.HasStatusEffect(StatusEffectType.Stun))
                {
                    if (UnityEngine.Random.value < balance.stunChance)
                    {
                        AddLog($"{unit.monster.baseData.monsterName}はピヨリで動けない！");
                        yield return new WaitForSeconds(turnSpeed * 0.5f);
                        continue;
                    }
                }

                // シェイプシフター: 行動前にコピー
                ShapeShifterAbility shapeShifter = null;
                if (unit.ability is ShapeShifterAbility ss)
                {
                    shapeShifter = ss;
                    ss.Execute(this);
                }

                // 殉教者: 攻撃せず回復行動
                if (unit.monster.baseData.monsterType == MonsterType.Martyr)
                {
                    unit.ability?.Execute(this);
                    if (!unit.isAlive)
                    {
                        ProcessDeath(unit);
                        if (CheckAndEndBattle()) yield break;
                    }
                    shapeShifter?.RevertStats();
                    yield return new WaitForSeconds(turnSpeed * 0.5f);
                    continue;
                }

                // 通常攻撃
                if (unit.monster.currentRange > 0 || unit.monster.baseData.monsterType == MonsterType.ShadowWalker)
                {
                    ExecuteAttack(unit);
                    if (CheckAndEndBattle()) yield break;

                    // アーチャー2連射
                    if (unit.ability is ArcherAbility archer && archer.ShouldDoubleAttack(this))
                    {
                        AddLog($"{unit.monster.baseData.monsterName}の2連射！");
                        ExecuteAttack(unit);
                        if (CheckAndEndBattle()) yield break;
                    }
                }

                // シェイプシフター: 行動後にステータスを戻す
                shapeShifter?.RevertStats();

                yield return new WaitForSeconds(turnSpeed * 0.5f);
            }

            yield return new WaitForSeconds(turnSpeed * 0.3f);
        }
    }

    private void ExecuteAttack(BattleUnit attacker)
    {
        var targets = FindTarget(attacker);
        if (targets.Count == 0) return;

        var action = new BattleAction { actor = attacker };

        foreach (var target in targets)
        {
            int damage = ApplyDamage(attacker, target);
            action.targets.Add(target);
            action.damage += damage;

            // 毒攻撃付与チェック
            if (attacker.hasPoisonAttack && !target.HasStatusEffect(StatusEffectType.Poison))
            {
                target.AddStatusEffect(new StatusEffect(StatusEffectType.Poison, -1));
                AddLog($"{target.monster.baseData.monsterName}は毒を受けた！");
            }

            // オーク: ピヨリ付与
            if (attacker.monster.baseData.monsterType == MonsterType.Orc)
            {
                if (!target.HasStatusEffect(StatusEffectType.Stun))
                {
                    target.AddStatusEffect(new StatusEffect(StatusEffectType.Stun, -1));
                    AddLog($"{target.monster.baseData.monsterName}はピヨリ状態になった！");
                }
            }

            // 死亡チェック
            if (!target.isAlive)
            {
                ProcessDeath(target);
            }
        }

        OnUnitActionEvent?.Invoke(action);
    }

    public List<BattleUnit> FindTarget(BattleUnit attacker)
    {
        var targets = new List<BattleUnit>();
        var enemySide = attacker.isPlayerSide ? enemyUnits : playerUnits;
        var livingEnemies = enemySide.FindAll(u => u.isAlive);

        if (livingEnemies.Count == 0) return targets;

        // ガーディアンチェック: 生存しているガーディアンがいれば全攻撃をリダイレクト
        BattleUnit guardian = livingEnemies.Find(u =>
            u.monster.baseData.monsterType == MonsterType.Guardian && u.isAlive);

        // シャドウウォーカー: ランダムターゲット
        if (attacker.monster.baseData.monsterType == MonsterType.ShadowWalker)
        {
            int randomIndex = UnityEngine.Random.Range(0, livingEnemies.Count);
            targets.Add(livingEnemies[randomIndex]);
            AddLog($"{attacker.monster.baseData.monsterName}が{livingEnemies[randomIndex].monster.baseData.monsterName}に奇襲！");
            return targets;
        }

        if (guardian != null)
        {
            // 貫通攻撃: ガーディアンを含む射程内の全敵
            if (attacker.monster.baseData.isPenetrate)
            {
                targets.Add(guardian);
                foreach (var enemy in livingEnemies)
                {
                    if (enemy != guardian && enemy.formationIndex < attacker.monster.currentRange)
                        targets.Add(enemy);
                }
            }
            else
            {
                targets.Add(guardian);
            }
            return targets;
        }

        // 通常ターゲット: 射程内で一番遠い敵
        int range = attacker.monster.currentRange;
        BattleUnit farthest = null;

        foreach (var enemy in livingEnemies)
        {
            if (enemy.formationIndex < range)
            {
                if (farthest == null || enemy.formationIndex > farthest.formationIndex)
                    farthest = enemy;
            }
        }

        // 射程内に敵がいない場合は一番近い敵を攻撃
        if (farthest == null && livingEnemies.Count > 0)
        {
            farthest = livingEnemies[0];
            foreach (var enemy in livingEnemies)
            {
                if (enemy.formationIndex < farthest.formationIndex)
                    farthest = enemy;
            }
        }

        if (farthest != null)
        {
            if (attacker.monster.baseData.isPenetrate)
            {
                // 貫通: 射程内の全敵
                foreach (var enemy in livingEnemies)
                {
                    if (enemy.formationIndex < range)
                        targets.Add(enemy);
                }
                if (targets.Count == 0)
                    targets.Add(farthest);
            }
            else
            {
                targets.Add(farthest);
            }
        }

        return targets;
    }

    public int ApplyDamage(BattleUnit attacker, BattleUnit target)
    {
        float damage = attacker.monster.currentAttack;
        AttackType atkType = attacker.monster.baseData.attackType;

        // シェイプシフターのコピー中は元のタイプを使用
        if (attacker.ability is ShapeShifterAbility ss && ss.IsCopying)
        {
            // コピー中は攻撃タイプもコピー元に依存（currentAttackはすでにコピー済み）
        }

        // 魔法バリア（魔法攻撃に対して）
        if (atkType == AttackType.Magic && target.HasStatusEffect(StatusEffectType.MagicBarrier))
        {
            damage *= balance.magicBarrierReduction;
        }

        // ガーディアンの魔法被ダメ増加
        if (atkType == AttackType.Magic && target.monster.baseData.monsterType == MonsterType.Guardian)
        {
            damage *= balance.guardianMagicMultiplier;
        }

        int finalDamage = Mathf.Max(1, Mathf.FloorToInt(damage));
        bool died = target.monster.TakeDamage(finalDamage);
        if (died)
            target.isAlive = false;

        AddLog($"{attacker.monster.baseData.monsterName} → {target.monster.baseData.monsterName} に{finalDamage}ダメージ" +
               (died ? "（撃破！）" : $"（残HP:{target.monster.currentHp}）"));

        return finalDamage;
    }

    private void ProcessDeath(BattleUnit deadUnit)
    {
        AddLog($"{deadUnit.monster.baseData.monsterName}が倒れた！");
        OnUnitDeathEvent?.Invoke(deadUnit);

        // 味方死亡カウント
        if (deadUnit.isPlayerSide)
            allyDeathCountThisWave++;

        // OnDeath能力
        if (deadUnit.ability != null && deadUnit.ability.trigger == AbilityTrigger.OnDeath)
        {
            deadUnit.ability.Execute(this);
        }

        // リベンジャーへの通知
        var allies = deadUnit.isPlayerSide ? playerUnits : enemyUnits;
        foreach (var ally in allies)
        {
            if (ally.isAlive && ally.ability is RevengerAbility revenger)
            {
                revenger.OnAllyDeath(this);
            }
        }
    }

    private void ProcessAllTurnStartEffects()
    {
        var allUnits = new List<BattleUnit>();
        allUnits.AddRange(playerUnits);
        allUnits.AddRange(enemyUnits);

        foreach (var unit in allUnits)
        {
            if (!unit.isAlive) continue;
            int poisonDmg = unit.ProcessTurnStartEffects(balance);
            if (poisonDmg > 0)
            {
                AddLog($"{unit.monster.baseData.monsterName}は毒で{poisonDmg}ダメージ" +
                       (!unit.isAlive ? "（毒で倒れた！）" : ""));
                if (!unit.isAlive)
                    ProcessDeath(unit);
            }
        }
    }

    private void ExecuteAbilitiesWithTrigger(AbilityTrigger trigger)
    {
        var allUnits = new List<BattleUnit>();
        allUnits.AddRange(playerUnits);
        allUnits.AddRange(enemyUnits);

        // 速度順にソート
        allUnits.Sort((a, b) =>
        {
            int speedDiff = b.monster.currentSpeed - a.monster.currentSpeed;
            if (speedDiff != 0) return speedDiff;
            return a.formationIndex - b.formationIndex;
        });

        foreach (var unit in allUnits)
        {
            if (!unit.isAlive) continue;
            if (unit.ability == null) continue;
            if (unit.ability.trigger != trigger) continue;
            if (!unit.ability.CanExecute(this)) continue;
            unit.ability.Execute(this);
        }
    }

    public List<BattleUnit> GetActionOrder()
    {
        var allUnits = new List<BattleUnit>();
        allUnits.AddRange(playerUnits);
        allUnits.AddRange(enemyUnits);

        allUnits.Sort((a, b) =>
        {
            int speedDiff = b.monster.currentSpeed - a.monster.currentSpeed;
            if (speedDiff != 0) return speedDiff;
            return a.formationIndex - b.formationIndex;
        });

        return allUnits;
    }

    private bool CheckAndEndBattle()
    {
        var result = CheckBattleEnd();
        if (result.HasValue)
        {
            EndBattle(result.Value);
            return true;
        }
        return false;
    }

    public BattleResult? CheckBattleEnd()
    {
        bool allPlayerDead = playerUnits.TrueForAll(u => !u.isAlive);
        bool allEnemyDead = enemyUnits.TrueForAll(u => !u.isAlive);

        if (allEnemyDead) return BattleResult.Victory;
        if (allPlayerDead) return BattleResult.Defeat;
        return null;
    }

    private void EndBattle(BattleResult result)
    {
        isAutoPlaying = false;

        if (result == BattleResult.Victory)
        {
            AddLog("=== 勝利！ ===");
            // OnBattleEnd能力を発動（骸骨僧侶の回復等）
            ExecuteAbilitiesWithTrigger(AbilityTrigger.OnBattleEnd);
        }
        else
        {
            AddLog("=== 全滅… ===");
        }

        OnBattleEndEvent?.Invoke(result);
    }

    public void AddLog(string message)
    {
        battleLog.Add(message);
        OnLogEvent?.Invoke(message);
        Debug.Log($"[Battle] {message}");
    }

    public List<string> GetBattleLog()
    {
        return new List<string>(battleLog);
    }

    public void ClearBattle()
    {
        foreach (var unit in playerUnits)
        {
            if (unit != null) Destroy(unit.gameObject);
        }
        foreach (var unit in enemyUnits)
        {
            if (unit != null) Destroy(unit.gameObject);
        }
        playerUnits.Clear();
        enemyUnits.Clear();
    }

    public void SetTurnSpeed(float speed)
    {
        turnSpeed = speed;
    }
}
