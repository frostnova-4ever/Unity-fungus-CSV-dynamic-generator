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
            // 创建两句对话
            string[] dialogues = new string[] {
                "这是第一句对话",
                "这是第二句对话"
            };

            flowchart = this.gameObject.AddComponent<Flowchart>();

            Block block = flowchart.CreateBlock(Vector2.zero);
            block.BlockName = "StoryBlock";

            /*添加事件*/
            MessageReceived handle = this.gameObject.AddComponent<MessageReceived>();

            handle.ParentBlock = block;
            block._EventHandler = handle;

            foreach (string s in dialogues)
            {
                /*添加Command*/
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

            // 立即开始执行对话
            block.StartExecution(); }
        flowchart = FindAnyObjectByType<Flowchart>();
        List<List<string>> csvData = CSVReader.ReadCSVToList(csv);
        List<DialogueEntry> entrys = CSVReader.FindBlockinCSV(csvData);
        CreateDialogueBlock(entrys);
        //StartDialogue(blocks[0]);
    }

    public void CreateDialogueCommands(DialogueEntry data,Block block)
    {
        var rows = data.rows;
        Say prevCommand = new Say();
        var type = new CommandType();
        for (int i=0;i<rows.Count;i++)
        {
            if (i + 1 < rows.Count)
                type = DIalogueBlockManager.CheckCommand(rows[i], rows[i + 1]);
            if(type == CommandType.Menu)
            {
                //关闭对话后隐藏，使对话时可以看见对话框
                if (prevCommand != null) 
                prevCommand.SetFadeWhenDone(false);
                //foreach (var k in Name2Block.Keys)
                //    print(k);
                CreateMenuCommand(rows[i], block, rows[i][(int)KeywordIndex.Tag]);
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
    /// 批量制作block模板，再逐个镶嵌命令
    /// </summary>
    public void CreateDialogueBlock(List<DialogueEntry> entry)
    {
        for (int i = 0; i < entry.Count; i++)
        {
            Block block = flowchart.CreateBlock(Vector2.zero);
            block.BlockName = entry[i].blockName;
            blocks.Add(block);
            Name2Block[block.BlockName] = block;
        }
        for(int i = 0;i < entry.Count; i++)
        {
            CreateDialogueBlock(blocks[i], entry[i]);
        }
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
        Menu menu = block.gameObject.AddComponent<Menu>();
        menu.ParentBlock = block;
        menu.ItemId = flowchart.NextItemId();
        menu.CommandIndex = block.CommandList.Count;
        menu.SetStandardText(list[(int)KeywordIndex.Text]);
        menu.SetDesciption(list[(int)KeywordIndex.Description]);
        menu.SetTargetBlock(Name2Block[targetBlockName]);
        block.CommandList.Add(menu);
        return menu;
    }
    public void StartDialogue(Block block)
    {
        if (block.CommandList.Count > 0)
        {
            // 选中第一个命令（可选）
            flowchart.AddSelectedCommand(block.CommandList[0]);
            flowchart.AddSelectedBlock(block);

            // 开始执行对话
            block.StartExecution();
            Debug.Log($"开始执行对话块: {block.BlockName}");
        }
    }
}