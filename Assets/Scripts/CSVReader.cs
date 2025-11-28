using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public static class CSVReader
{
    /// <summary>
    /// 读取CSV文件并返回列表结果
    /// </summary>
    /// <param name="file">CSV文件</param>
    /// <returns>二维列表，每行是一个字符串列表</returns>
    public static List<List<string>> ReadCSVToList(TextAsset file)
    {
        List<List<string>> resultList = new List<List<string>>();
        if (file == null || file.bytes == null)
        {
            Debug.LogError("CSV文件为空");
            return resultList;
        }
        try
        {
            Encoding encoding = DetectEncoding(file);
            Debug.Log($"使用编码: {encoding.EncodingName}");

            string csvText = encoding.GetString(file.bytes);
            string[] lines = csvText.Split('\n');

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;
                string[] fields = ParseCSVLine(trimmedLine);
                // 创建新行并添加到结果列表
                List<string> row = new List<string>();
                foreach (string field in fields)
                {
                    row.Add(field);
                }
                resultList.Add(row);
            }
            return resultList;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"读取CSV文件时出错: {e.Message}");
            return resultList;
        }
    }

    /// <summary>
    /// 检测文件编码
    /// </summary>
    public static Encoding DetectEncoding(TextAsset file)
    {
        if (file == null || file.bytes == null)
            return Encoding.UTF8;
        byte[] bytes = file.bytes;
        // 检查UTF-8 BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8;
        }
        // 尝试用GB2312解码
        try
        {
            Encoding gb2312 = Encoding.GetEncoding("GB2312");
            string gbText = gb2312.GetString(bytes);

            // 检查是否包含中文字符且没有乱码
            if (ContainsChinese(gbText) && !ContainsGarbage(gbText))
            {
                return gb2312;
            }
        }
        catch { }
        return Encoding.UTF8;
    }

    /// <summary>
    /// 检查是否包含中文字符
    /// </summary>
    private static bool ContainsChinese(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        for (int i = 0; i < Mathf.Min(text.Length, 100); i++)
        {
            char c = text[i];
            if (c >= 0x4E00 && c <= 0x9FFF) // 中文字符范围
                return true;
        }
        return false;
    }

    /// <summary>
    /// 检查是否包含乱码字符
    /// </summary>
    private static bool ContainsGarbage(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        for (int i = 0; i < Mathf.Min(text.Length, 100); i++)
        {
            char c = text[i];
            // 检查是否为替换字符或不可见控制字符
            if (c == '�' || c == '\uFFFD' || (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t'))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 解析CSV行
    /// </summary>
    private static string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        StringBuilder currentField = new StringBuilder();
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

    /// <summary>
    /// 根据关键词序列查找数据块
    /// </summary>
    public static List<DialogueEntry> FindDataByKeywords(List<List<string>> csvData, List<string> keywords)
    {
        DialogueEntry entry = new DialogueEntry();
        List<DialogueEntry> entryList = new List<DialogueEntry>();
        List<List<string>> resultData = new List<List<string>>();
        
        if (csvData.Count == 0 || keywords == null || keywords.Count == 0)
            return entryList;

        bool foundKeywords = false;

        for (int i = 0; i < csvData.Count; i++)
        {
            List<string> row = csvData[i];
            bool isEmptyRow = true;// 检查是否为空行
            foreach (string cell in row)
            {
                if (!string.IsNullOrWhiteSpace(cell))
                {
                    isEmptyRow = false;
                    break;
                }
            }
            // 如果遇到空行且已经找到关键词序列，则结束读取
            if (isEmptyRow && foundKeywords)
            {
                entry.rows = resultData;
                entryList.Add(entry);
                resultData = new List<List<string>>();
                entry = new DialogueEntry();
                foundKeywords = false;
            }
            if (!foundKeywords)
            {
                // 检查当前行是否包含按顺序排列的关键词
                if (IsRowMatchKeywords(row, keywords))
                {
                    entry.blockName = string.Join("", row.Take(3));
                    foundKeywords = true;
                    if (i + 1 < csvData.Count)
                        i++;//跳属性行
                }
            }
            else
            {
                // 已经找到关键词序列，直接添加
                resultData.Add(new List<string>(row));
            }
        }
        return entryList;
    }

    // 辅助函数：检查一行是否按顺序包含所有关键词
    private static bool IsRowMatchKeywords(List<string> row, List<string> keywords)
    {
        if (row.Count < keywords.Count) return false;
        int keywordIndex = 0;
        // 遍历行的每个单元格，按顺序匹配关键词
        for (int i = 0; i < row.Count && keywordIndex < keywords.Count; i++)
        {
            string cell = row[i].Trim();
            string currentKeyword = keywords[keywordIndex];
            if (cell == currentKeyword)
            {
                keywordIndex++;
            }
        }
        // 如果所有关键词都按顺序匹配成功，返回true
        return keywordIndex == keywords.Count;
    }

    /// <summary>
    /// 查找对话数据块
    /// </summary>
    public static List<DialogueEntry> FindBlockinCSV(List<List<string>> csvData)
    {

        return FindDataByKeywords(csvData, Keywords.block);
    }
    public static bool CheckCommand(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return false;
        }
        if (str[0] == '/')
        {
            string command = str.Substring(1);
            return command == "choose";
        }
        return false;
    }

}