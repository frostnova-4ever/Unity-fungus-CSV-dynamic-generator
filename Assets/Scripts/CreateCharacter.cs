using UnityEngine;
using Fungus;

public class SimpleCharacterCreator : MonoBehaviour
{

    public DialogueActor actorData = new DialogueActor()
    {
        actorName = "manba",
        actorID = 0,
        actorColor = Color.white,
    };

    void Start() 
    {
            //CreateCharacterObject();
    }

    [ContextMenu("创建角色物体")]
    public void CreateCharacterObject()
    {
        // 检查是否已存在同名角色
        Character existingCharacter = GameObject.Find(actorData.actorName)?.GetComponent<Character>();
        if (existingCharacter != null)
        {
            Debug.LogWarning($"角色 {actorData.actorName} 已存在，跳过创建");
            return;
        }
        GameObject characterObj = new GameObject(actorData.actorName);
        characterObj.transform.SetParent(this.transform);
        // 添加Fungus Character组件
        Character actorComponet = characterObj.AddComponent<Character>();
        // 设置角色属性
        actorComponet.NameText = actorData.actorName;
        actorComponet.NameColor = actorData.actorColor;
    }

    [ContextMenu("检查场景中的角色")]
    public void CheckCharactersInScene()
    {
        Character[] allCharacters = FindObjectsOfType<Character>();
        Debug.Log($"场景中找到 {allCharacters.Length} 个Character组件:");

        foreach (Character character in allCharacters)
        {
            Debug.Log($" - {character.name} (显示名: {character.NameText})");
        }
    }

    [ContextMenu("删除所有角色物体")]
    public void DeleteAllCharacterObjects()
    {
        Character[] allCharacters = FindObjectsOfType<Character>();
        foreach (Character character in allCharacters)
        {
            if (character.transform.parent == this.transform)
            {
                Debug.Log($"删除角色: {character.name}");
                DestroyImmediate(character.gameObject);
            }
        }
    }
}