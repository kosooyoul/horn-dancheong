using UnityEngine;

namespace KD
{
    public class StatCalculatorSmokeTest : MonoBehaviour
    {
        [Header("Run")]
        [SerializeField] private bool runOnStart = true;

        private void Start()
        {
            if (runOnStart)
                RunAllTests();
        }

        [ContextMenu("Run All Stat Tests")]
        public void RunAllTests()
        {
            Debug.Log("======================================");
            Debug.Log("=== StatCalculator Smoke Test Start ===");
            Debug.Log("======================================");

            TestCase(
                "기본 낮은 스탯",
                new UnitBaseStats
                {
                    agility = 1,
                    spirit = 1,
                    guard = 1,
                    luck = 1
                }
            );

            TestCase(
                "일반 전투 유닛",
                new UnitBaseStats
                {
                    agility = 10,
                    spirit = 8,
                    guard = 5,
                    luck = 12
                }
            );

            TestCase(
                "이동 최대치 확인",
                new UnitBaseStats
                {
                    agility = 25,
                    spirit = 10,
                    guard = 10,
                    luck = 10
                }
            );

            TestCase(
                "치명타/회피 최대치 확인",
                new UnitBaseStats
                {
                    agility = 5,
                    spirit = 5,
                    guard = 5,
                    luck = 200
                }
            );

            Debug.Log("====================================");
            Debug.Log("=== StatCalculator Smoke Test End ===");
            Debug.Log("====================================");
        }

        private void TestCase(string testName, UnitBaseStats baseStats)
        {
            UnitDerivedStats actual = StatCalculator.Calculate(baseStats);

            int expectedMaxHP = 50 + baseStats.guard * 10;
            int expectedMaxSP = 150;
            int expectedInitiative = baseStats.agility;

            int expectedMoveRange = Mathf.Clamp(
                1 + baseStats.agility / 5,
                1,
                5
            );

            int expectedAttackPower = baseStats.spirit * 5;
            int expectedHealPower = baseStats.spirit * 5;
            int expectedDefense = baseStats.guard * 5;

            float expectedCritChance = Mathf.Clamp(
                0.05f + baseStats.luck * 0.01f,
                0.05f,
                1.0f
            );

            float expectedEvasionChance = Mathf.Clamp(
                0.05f + baseStats.luck * 0.01f,
                0.05f,
                1.0f
            );

            Debug.Log($"--- [{testName}] ---");
            Debug.Log($"BaseStats / 민첩:{baseStats.agility}, 영력:{baseStats.spirit}, 보호:{baseStats.guard}, 행운:{baseStats.luck}");

            CheckInt("maxHP", actual.maxHP, expectedMaxHP);
            CheckInt("maxSP", actual.maxSP, expectedMaxSP);
            CheckInt("initiative", actual.initiative, expectedInitiative);
            CheckInt("moveRange", actual.moveRange, expectedMoveRange);
            CheckInt("attackPower", actual.attackPower, expectedAttackPower);
            CheckInt("healPower", actual.healPower, expectedHealPower);
            CheckInt("defense", actual.defense, expectedDefense);
            CheckFloat("critChance", actual.critChance, expectedCritChance);
            CheckFloat("evasionChance", actual.evasionChance, expectedEvasionChance);
        }

        private void CheckInt(string statName, int actual, int expected)
        {
            if (actual == expected)
            {
                Debug.Log($"[PASS] {statName}: {actual}");
            }
            else
            {
                Debug.LogError($"[FAIL] {statName}: actual={actual}, expected={expected}");
            }
        }

        private void CheckFloat(string statName, float actual, float expected)
        {
            if (Mathf.Approximately(actual, expected))
            {
                Debug.Log($"[PASS] {statName}: {actual:P1}");
            }
            else
            {
                Debug.LogError($"[FAIL] {statName}: actual={actual:P1}, expected={expected:P1}");
            }
        }
    }
}