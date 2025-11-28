using Fungus;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GetActor : MonoBehaviour
{
    public GameObject actorObject;
    private List<GameObject> actorObjects = new List<GameObject>();
    private Dictionary<string, Character> name2Character = new Dictionary<string, Character>();
    public void GetAllchildren()
    {
        foreach(Transform child in transform)
        {
            GameObject tmpObj = child.gameObject;
            Character character = child.GetComponent<Character>();
            string nameText = character.NameText;
            name2Character[nameText] = character;
            actorObjects.Add(tmpObj);
        }
    }
    public void Awake()
    {
        GetAllchildren();
    }
}
