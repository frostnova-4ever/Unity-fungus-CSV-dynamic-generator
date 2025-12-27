using Fungus;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class CreateDialogue : MonoBehaviour
{
    private Flowchart flowchart;
    public TextAsset csv;
    List<Block> blocks = new List<Block>();
    Dictionary<string,Block> Name2Block = new Dictionary<string,Block>();

    private void Awake()
    {
        if (1 == 0) {
            // ��������Ի�
            string[] dialogues = new string[] {
                "���ǵ�һ��Ի�",
                "���ǵڶ���Ի�"
            };

            flowchart = this.gameObject.AddComponent<Flowchart>();

            Block block = flowchart.CreateBlock(Vector2.zero);
            block.BlockName = "StoryBlock";

            /*�����¼�*/
            MessageReceived handle = this.gameObject.AddComponent<MessageReceived>();

            handle.ParentBlock = block;
            block._EventHandler = handle;

            foreach (string s in dialogues)
            {
                /*����Command*/
                Say say = this.gameObject.AddComponent<Say>();
                say.ParentBlock = block;
                say.ItemId = flowchart.NextItemId();
                say.CommandIndex = block.CommandList.Count;
                say.OnCommandAdded(block);
                say.SetStandardText(s);

                block.CommandList.Add(say);
            }

            if (block.CommandList.Count > 0)
                flowchart.AddSelectedCommand(block.CommandList[0]);
            flowchart.AddSelectedBlock(block);

            // ������ʼִ�жԻ�
            block.StartExecution(); }
        flowchart = FindAnyObjectByType<Flowchart>();
        
        Debug.Log("=== 开始解析 CSV ===");
        Debug.Log($"CSV 文件: {csv?.name}");
        
        List<List<string>> csvData = CSVReader.ReadCSVToList(csv);
        Debug.Log($"读取到 {csvData.Count} 行数据");
        
        // 打印前10行数据用于调试
        for (int i = 0; i < Math.Min(10, csvData.Count); i++)
        {
            Debug.Log($"行{i}: [{string.Join(", ", csvData[i])}]");
        }
        
        List<DialogueEntry> entrys = CSVReader.FindBlockinCSV(csvData);
        Debug.Log($"找到 {entrys.Count} 个对话块");
        
        for (int i = 0; i < entrys.Count; i++)
        {
            Debug.Log($"块[{i}]: 名称={entrys[i].blockName}, 数据行数={entrys[i].rows?.Count ?? 0}");
        }
        
        if (entrys.Count == 0)
        {
            Debug.LogError("没有找到任何对话块！检查 CSV 中的关键词'对话'是否正确");
            return;
        }
        
        CreateDialogueBlock(entrys);
    }

    public void CreateDialogueCommands(DialogueEntry data,Block block)
    {
        var rows = data.rows;
        Say prevCommand = new Say();
        var type = new CommandType();
        for (int i=0;i<rows.Count;i++)
        {
            CommandType currentType = CommandType.Say;
            if (i + 1 < rows.Count)
                currentType = DIalogueBlockManager.CheckCommand(rows[i], rows[i + 1]);
            
            if(currentType == CommandType.Menu)
            {
                if (prevCommand != null) 
                    prevCommand.SetFadeWhenDone(false);
                
                string targetBlockName = rows[i][(int)KeywordIndex.Tag];
                if (!string.IsNullOrEmpty(targetBlockName))
                    CreateMenuCommand(rows[i], block, targetBlockName);
                print("create menu");
            }
            else
            {
                Say say = CreateSayCommand(rows[i], block);
                prevCommand = say;
            }
        }
    }
    public void CreateDialogueBlock(Block block,DialogueEntry entry)
    {
        block.BlockName = entry.blockName;
        CreateDialogueCommands(entry, block);
    }
    /// <summary>
    /// 创建所有block模块，循环遍历传参
    /// </summary>
    public void CreateDialogueBlock(List<DialogueEntry> entry)
    {
        Debug.Log($"=== 开始创建 {entry.Count} 个对话块 ===");
        
        for (int i = 0; i < entry.Count; i++)
        {
            Debug.Log($"创建块[{i}]: {entry[i].blockName}");
            Block block = flowchart.CreateBlock(Vector2.zero);
            block.BlockName = entry[i].blockName;
            blocks.Add(block);
            Name2Block[block.BlockName] = block;
            Debug.Log($"  → 已添加到 Name2Block，当前字典包含: [{string.Join(", ", Name2Block.Keys)}]");
        }
        
        Debug.Log("=== 所有块创建完成，开始创建命令 ===");
        for(int i = 0; i < entry.Count; i++)
        {
            Debug.Log($"为块[{i}] {entry[i].blockName} 创建命令，数据行数: {entry[i].rows?.Count ?? 0}");
            CreateDialogueBlock(blocks[i], entry[i]);
        }
        
        Debug.Log("=== 所有命令创建完成 ===");
        Debug.Log($"Name2Block 最终包含: [{string.Join(", ", Name2Block.Keys)}]");
    }

    public Say CreateSayCommand(List<string> list,Block block)
    {
        Say say = this.gameObject.AddComponent<Say>();
        say.ParentBlock = block;
        say.ItemId = flowchart.NextItemId();
        say.CommandIndex = block.CommandList.Count;
        say.OnCommandAdded(block);
        say.SetStandardText(list[(int)KeywordIndex.Text]);
        block.CommandList.Add(say);
        return say;
    }
    public Menu CreateMenuCommand(List<string> list,Block block, string targetBlockName )
    {
        Debug.Log($"  [Menu] 目标块: {targetBlockName}");
        Debug.Log($"  [Menu] Name2Block 包含: [{string.Join(", ", Name2Block.Keys)}]");
        
        if (!Name2Block.ContainsKey(targetBlockName))
        {
            Debug.LogError($"  [Menu] 错误: 找不到目标块 '{targetBlockName}'！");
            return null;
        }
        
        Menu menu = block.gameObject.AddComponent<Menu>();
        menu.ParentBlock = block;
        menu.ItemId = flowchart.NextItemId();
        menu.CommandIndex = block.CommandList.Count;
        menu.SetStandardText(list[(int)KeywordIndex.Text]);
        menu.SetDesciption(list[(int)KeywordIndex.Description]);
        menu.SetTargetBlock(Name2Block[targetBlockName]);
        block.CommandList.Add(menu);
        
        Debug.Log($"  [Menu] 创建成功: {list[(int)KeywordIndex.Text]} → {targetBlockName}");
        return menu;
    }
    public void StartDialogue(Block block)
    {
        if (block == null)
        {
            Debug.LogError("StartDialogue: block 为空");
            return;
        }
        if (block.CommandList.Count > 0)
        {
            flowchart.AddSelectedCommand(block.CommandList[0]);
            flowchart.AddSelectedBlock(block);
            block.StartExecution();
            Debug.Log($"开始执行对话: {block.BlockName}");
        }
    }
    
    private void Update()
    {
        // 按 F1 键测试对话1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("=== 按下 F1，测试对话1 ===");
            if (blocks.Count > 0)
                StartDialogue(blocks[0]);
        }
        // 按 F2 键测试对话2
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("=== 按下 F2，测试对话2 ===");
            if (blocks.Count > 1)
                StartDialogue(blocks[1]);
        }
        // 按 F3 键测试对话3
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("=== 按下 F3，测试对话3 ===");
            if (blocks.Count > 2)
                StartDialogue(blocks[2]);
        }
        // 按 F12 打印所有块
        if (Input.GetKeyDown(KeyCode.F12))
        {
            Debug.Log($"=== 当前共有 {blocks.Count} 个对话块 ===");
            for (int i = 0; i < blocks.Count; i++)
            {
                Debug.Log($"块[{i}]: {blocks[i].BlockName}, 命令数: {blocks[i].CommandList.Count}");
            }
        }
    }
}