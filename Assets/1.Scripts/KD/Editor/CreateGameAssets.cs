using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using KD;

namespace KD.Editor
{
    public static class CreateGameAssets
    {
        private const string GridPatternPath   = "Assets/2.ScriptableObject/Grid/";
        private const string DeploymentPath    = "Assets/2.ScriptableObject/Enemy/";

        [MenuItem("KD/에셋 생성/GridPattern - 15x15 전체 (자신 제외)")]
        static void CreateAll15x15()
        {
            GridPatternData p = ScriptableObject.CreateInstance<GridPatternData>();
            p.patternId   = "all_15x15";
            p.patternName = "15x15 전체 (자신 제외)";
            p.directionMode  = PatternDirectionMode.UseSelectedDirection;
            p.includeOrigin  = false;
            p.fixedCells     = new List<PatternFixedCell>();
            p.rays           = new List<PatternRay>();

            // 15x15 맵 최대 범위: 중앙 기준 ±7
            for (int x = -7; x <= 7; x++)
                for (int y = -7; y <= 7; y++)
                    if (x != 0 || y != 0)
                        p.fixedCells.Add(new PatternFixedCell { localOffset = new Vector2Int(x, y) });

            string path = GridPatternPath + "GridPattern_All15x15.asset";
            AssetDatabase.CreateAsset(p, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = p;
            Debug.Log($"[CreateGameAssets] GridPattern_All15x15 생성 완료 ({p.fixedCells.Count}셀) → {path}");
        }

        // 3x3(Chebyshev 1) 부터 4x4(Chebyshev 2) 까지의 링
        // → 자신 기준 Chebyshev 거리 1~2인 타일 (인접 + 2칸 범위)
        // 총 24타일: 거리1=8개, 거리2=16개
        [MenuItem("KD/에셋 생성/GridPattern - 3x3~4x4 링 (Chebyshev 1~2)")]
        static void CreateRing1To2()
        {
            GridPatternData p = ScriptableObject.CreateInstance<GridPatternData>();
            p.patternId   = "ring_cheby_1_to_2";
            p.patternName = "3x3~4x4 링";
            p.directionMode  = PatternDirectionMode.UseSelectedDirection;
            p.includeOrigin  = false;
            p.fixedCells     = new List<PatternFixedCell>();
            p.rays           = new List<PatternRay>();

            for (int x = -2; x <= 2; x++)
                for (int y = -2; y <= 2; y++)
                {
                    int cheby = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                    if (cheby >= 1 && cheby <= 2)
                        p.fixedCells.Add(new PatternFixedCell { localOffset = new Vector2Int(x, y) });
                }

            string path = GridPatternPath + "GridPattern_Ring_3x3To4x4.asset";
            AssetDatabase.CreateAsset(p, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = p;
            Debug.Log($"[CreateGameAssets] GridPattern_Ring_3x3To4x4 생성 완료 ({p.fixedCells.Count}셀) → {path}");
        }

        // 가운데 5x5를 제외한 15x15 전체를 배치 후보 타일로 설정
        // 15x15 맵: 타일 (0,0)~(14,14), 가운데 5x5 = (5,5)~(9,9)
        [MenuItem("KD/에셋 생성/DeploymentRule - 15x15 가운데 5x5 제외")]
        static void CreateDeploymentRuleExcludeCenter()
        {
            DeploymentRuleData r = ScriptableObject.CreateInstance<DeploymentRuleData>();
            r.maxDeployCount        = 4;
            r.forbiddenPatternFromEnemy = null;
            r.candidateDeployTiles  = new List<Vector2Int>();

            for (int x = 0; x < 15; x++)
                for (int y = 0; y < 15; y++)
                {
                    bool inCenter = x >= 5 && x <= 9 && y >= 5 && y <= 9;
                    if (!inCenter)
                        r.candidateDeployTiles.Add(new Vector2Int(x, y));
                }

            string path = DeploymentPath + "DeploymentRule_Exclude_Center5x5.asset";
            AssetDatabase.CreateAsset(r, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = r;
            Debug.Log($"[CreateGameAssets] DeploymentRule_Exclude_Center5x5 생성 완료 ({r.candidateDeployTiles.Count}타일) → {path}");
        }

        [MenuItem("KD/에셋 생성/전체 생성 (위 3개 모두)")]
        static void CreateAll()
        {
            CreateAll15x15();
            CreateRing1To2();
            CreateDeploymentRuleExcludeCenter();
        }
    }
}
