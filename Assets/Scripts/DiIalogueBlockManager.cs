using Fungus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum KeywordIndex
{
    ID = 0,
    Layer = 1,
    Speaker = 2,
    Tag = 3,
    Text = 4,
    LeftRole = 5,
    RightRole = 6,
    Description = 7,
}
public enum CommandType
{
    Say,
    Menu
}
public class DIalogueBlockManager : MonoBehaviour
{
    //对话块集
    public List<Block> blocks = new List<Block>();
   
    //检查下一行与下二行是否重合值来提前判断是否为menu指令
    public static CommandType CheckCommand(List<string> list1, List<string> list12)
    {
        if(list1.Count > 0 && list12.Count>0)
        {
            if(list1[(int)KeywordIndex.Layer] == list12[(int)KeywordIndex.Layer])
                return CommandType.Menu;
        }
        return CommandType.Say;
    }
    public List<Block> GetBlocks()
    {
        return blocks;
    }

    private void Start()
    {
    }

}
