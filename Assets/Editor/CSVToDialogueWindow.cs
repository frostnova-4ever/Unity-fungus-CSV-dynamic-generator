using Fungus;
using Fungus.EditorUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class CSVToDialogueWindow : EditorWindow
{
    private TextAsset selectedCSV;
    private Flowchart targetFlowchart;
    private bool createNewFlowchart = true;
    private string outputPath = "Assets/DialogueBlocks";
    private Vector2 scrollPosition;
    private string conversionLog = "";
    private int currentProgress = 0;
    private int totalBlocks = 0;

    [MenuItem("工具/CSV转对话块")]
    public static void ShowWindow()
    {
        GetWindow<CSVToDialogueWindow>("CSV转对话块工具");
    }

    [MenuItem("Assets/CSV转对话块")]
    public static void ShowFromAsset()
    {
        var window = GetWindow<CSVToDialogueWindow>("CSV转对话块工具");
        var selection = Selection.activeObject as TextAsset;
        if (selection != null)
        {
            window.selectedCSV = selection;
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("CSV 静态转换工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("将 CSV 文件转换为 Fungus 对话块，支持 Say 和 Menu 命令", MessageType.Info);
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("CSV 文件", EditorStyles.boldLabel);
        selectedCSV = (TextAsset)EditorGUILayout.ObjectField("选择 CSV 文件", selectedCSV, typeof(TextAsset), false);

        if (selectedCSV != null)
        {
            EditorGUILayout.HelpBox($"已选择: {selectedCSV.name}", MessageType.None);
        }

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("输出设置", EditorStyles.boldLabel);
        createNewFlowchart = EditorGUILayout.Toggle("创建新的 Flowchart", createNewFlowchart);

        if (!createNewFlowchart)
        {
            targetFlowchart = (Flowchart)EditorGUILayout.ObjectField("目标 Flowchart", targetFlowchart, typeof(Flowchart), true);
        }
        else
        {
            outputPath = EditorGUILayout.TextField("输出路径", outputPath);
        }

        EditorGUILayout.Space(20);

        EditorGUI.BeginDisabledGroup(selectedCSV == null);
        if (GUILayout.Button("开始转换", GUILayout.Height(30)))
        {
            ConvertCSVToDialogue();
        }
        EditorGUI.EndDisabledGroup();

        if (!string.IsNullOrEmpty(conversionLog))
        {
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("转换日志", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(conversionLog, GUILayout.Height(200));
        }

        EditorGUILayout.EndScrollView();
    }

    private void ConvertCSVToDialogue()
    {
        if (selectedCSV == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择 CSV 文件", "确定");
            return;
        }

        conversionLog = "";
        Log($"=== 开始转换: {selectedCSV.name} ===\n");

        Flowchart flowchart = null;
        if (createNewFlowchart)
        {
            flowchart = CreateNewFlowchart();
        }
        else if (targetFlowchart != null)
        {
            flowchart = targetFlowchart;
        }
        else
        {
            var flowcharts = FindObjectsOfType<Flowchart>();
            if (flowcharts.Length > 0)
            {
                flowchart = flowcharts[0];
                Log($"使用现有 Flowchart: {flowchart.name}");
            }
            else
            {
                flowchart = CreateNewFlowchart();
            }
        }

        if (flowchart == null)
        {
            Log("错误: 无法创建或找到 Flowchart");
            return;
        }

        List<List<string>> csvData = ParseCSV(selectedCSV);
        Log($"读取到 {csvData.Count} 行数据");

        List<DialogueEntry> entries = FindDialogueBlocks(csvData);
        Log($"找到 {entries.Count} 个对话块\n");

        totalBlocks = entries.Count;
        currentProgress = 0;

        var blocks = new List<Block>();
        var name2Block = new Dictionary<string, Block>();

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            Log($"创建块 [{i + 1}/{entries.Count}]: {entry.blockName}");

            Block block = flowchart.CreateBlock(Vector2.zero);
            block.BlockName = entry.blockName;
            blocks.Add(block);
            name2Block[block.BlockName] = block;

            currentProgress++;
            EditorUtility.DisplayProgressBar("转换中", $"创建对话块: {entry.blockName}", (float)currentProgress / totalBlocks);
        }

        Log("\n=== 创建命令 ===\n");

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var block = blocks[i];
            Log($"处理块: {entry.blockName} ({entry.rows.Count} 行数据)");

            CreateCommandsForEntry(entry, block, name2Block, flowchart);
        }

        EditorUtility.ClearProgressBar();
        Log("\n=== 转换完成 ===");
        Log($"共创建 {blocks.Count} 个对话块");
        Log($"保存位置: {AssetDatabase.GetAssetPath(flowchart)}");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("转换完成", $"成功创建 {blocks.Count} 个对话块！\n\n查看 Unity 场景中的 Flowchart 组件。", "确定");
    }

    private Flowchart CreateNewFlowchart()
    {
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        var go = new GameObject("DialogueFlowchart");
        var flowchart = go.AddComponent<Flowchart>();

        string prefabPath = $"{outputPath}/DialogueFlowchart.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        DestroyImmediate(go);

        var loadedPrefab = AssetDatabase.LoadAssetAtPath<Flowchart>(prefabPath);
        Log($"创建新 Flowchart: {prefabPath}");

        return loadedPrefab;
    }

    private List<List<string>> ParseCSV(TextAsset csv)
    {
        var result = new List<List<string>>();

        Encoding encoding = DetectEncoding(csv);
        string csvText = encoding.GetString(csv.bytes);
        string[] lines = csvText.Split('\n');

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
                continue;

            string[] fields = ParseCSVLine(trimmedLine);
            var row = new List<string>();
            foreach (string field in fields)
            {
                row.Add(field);
            }
            result.Add(row);
        }

        return result;
    }

    private Encoding DetectEncoding(TextAsset file)
    {
        if (file == null || file.bytes == null)
            return Encoding.UTF8;

        byte[] bytes = file.bytes;
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8;
        }

        try
        {
            Encoding gb2312 = Encoding.GetEncoding("GB2312");
            string gbText = gb2312.GetString(bytes);
            if (ContainsChinese(gbText) && !ContainsGarbage(gbText))
            {
                return gb2312;
            }
        }
        catch { }

        return Encoding.UTF8;
    }

    private bool ContainsChinese(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        for (int i = 0; i < Math.Min(text.Length, 100); i++)
        {
            char c = text[i];
            if (c >= 0x4E00 && c <= 0x9FFF)
                return true;
        }
        return false;
    }

    private bool ContainsGarbage(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        for (int i = 0; i < Math.Min(text.Length, 100); i++)
        {
            char c = text[i];
            if (c == '�' || c == '\uFFFD' || (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t'))
            {
                return true;
            }
        }
        return false;
    }

    private string[] ParseCSVLine(string line)
    {
        var result = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        result.Add(currentField.ToString());
        return result.ToArray();
    }

    private List<DialogueEntry> FindDialogueBlocks(List<List<string>> csvData)
    {
        var entries = new List<DialogueEntry>();
        DialogueEntry currentEntry = null;
        bool foundBlock = false;

        for (int i = 0; i < csvData.Count; i++)
        {
            var row = csvData[i];

            if (row.Count >= 2 && row[0] == "对话" && !string.IsNullOrEmpty(row[1]))
            {
                if (currentEntry != null && currentEntry.rows.Count > 0)
                {
                    entries.Add(currentEntry);
                }

                currentEntry = new DialogueEntry();
                currentEntry.blockName = row[0] + row[1];
                foundBlock = true;

                if (i + 1 < csvData.Count)
                    i++;
            }
            else if (foundBlock && currentEntry != null)
            {
                bool isEmpty = true;
                foreach (var cell in row)
                {
                    if (!string.IsNullOrWhiteSpace(cell))
                    {
                        isEmpty = false;
                        break;
                    }
                }

                if (!isEmpty && row.Count >= 5 && !string.IsNullOrEmpty(row[4]))
                {
                    currentEntry.rows.Add(new List<string>(row));
                }
            }
        }

        if (currentEntry != null && currentEntry.rows.Count > 0)
        {
            entries.Add(currentEntry);
        }

        return entries;
    }

    private void CreateCommandsForEntry(DialogueEntry entry, Block block, Dictionary<string, Block> name2Block, Flowchart flowchart)
    {
        Say prevSay = null;

        for (int i = 0; i < entry.rows.Count; i++)
        {
            var row = entry.rows[i];

            if (row.Count < 5)
                continue;

            bool isMenu = row.Count > 3 && row[3] == "/choose";
            string targetBlockName = isMenu && row.Count > 4 ? row[4] : "";

            if (isMenu && !string.IsNullOrEmpty(targetBlockName))
            {
                if (prevSay != null)
                    prevSay.SetFadeWhenDone(false);

                var menu = CreateMenuCommand(row, block, targetBlockName, name2Block, flowchart);
                Log($"  - Menu: {row[4]} → {targetBlockName}");
            }
            else
            {
                var say = CreateSayCommand(row, block, flowchart);
                prevSay = say;
                Log($"  - Say: {row[2]}: {row[4].Substring(0, Math.Min(20, row[4].Length))}...");
            }
        }
    }

    private Say CreateSayCommand(List<string> row, Block block, Flowchart flowchart)
    {
        var say = block.gameObject.AddComponent<Say>();
        say.ParentBlock = block;
        say.ItemId = flowchart.NextItemId();
        say.CommandIndex = block.CommandList.Count;
        say.SetStandardText(row[4]);

        if (row.Count > 2 && !string.IsNullOrEmpty(row[2]))
            {
                var character = FindCharacter(row[2]);
                if (character != null)
                    say.SetCharacter(character);
            }

        block.CommandList.Add(say);
        return say;
    }

    private Fungus.Menu CreateMenuCommand(List<string> row, Block block, string targetBlockName, Dictionary<string, Block> name2Block, Flowchart flowchart)
    {
        var menu = block.gameObject.AddComponent<Fungus.Menu>();
        menu.ParentBlock = block;
        menu.ItemId = flowchart.NextItemId();
        menu.CommandIndex = block.CommandList.Count;
        menu.SetStandardText(row[4]);

        if (name2Block.ContainsKey(targetBlockName))
        {
            menu.SetTargetBlock(name2Block[targetBlockName]);
        }
        else
        {
            Log($"  警告: 找不到目标块 '{targetBlockName}'");
        }

        block.CommandList.Add(menu);
        return menu;
    }

    private void Log(string message)
    {
        conversionLog += message + "\n";
    }

    private Character FindCharacter(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        var characters = FindObjectsOfType<Character>();
        foreach (var character in characters)
        {
            if (character.NameText == name)
                return character;
        }

        Log($"  警告: 找不到角色 '{name}'");
        return null;
    }

    private class DialogueEntry
    {
        public string blockName = "";
        public List<List<string>> rows = new List<List<string>>();
    }
}
