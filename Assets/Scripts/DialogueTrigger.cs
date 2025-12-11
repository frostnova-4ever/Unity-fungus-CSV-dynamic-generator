using Fungus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public Flowchart flowchart;
    public string blockName;
    public string targetTag = "Player";
    public bool start = false;
    public bool trigger = true;
    public bool collision = false;
    public bool value = false;
    [HideInInspector]
    public bool isTalking = false;
    private void Start()
    {
        if (flowchart == null)
            flowchart = FindObjectOfType<Flowchart>();
        BlockSignals.OnBlockEnd += BlockSignals_OnBlockEnd;
        var block = flowchart.FindBlock(blockName);
        if (start)
        {
            flowchart.ExecuteBlock(blockName);
            isTalking = true;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (trigger&&other.gameObject.tag == targetTag && !isTalking)
        {
            flowchart.ExecuteBlock(blockName);
            isTalking = true;
        }
    }

    private void BlockSignals_OnBlockEnd(Block block)
    {
        isTalking=false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == targetTag)
        {
            isTalking = false;
        }
    }
}
