using Fungus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public Flowchart flowchart;
    public string blockName;
    public string targetTag = "Player";
    [HideInInspector]
    public bool isTalking = false;
    private void Start()
    {
        BlockSignals.OnBlockEnd += BlockSignals_OnBlockEnd;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == targetTag && !isTalking)
        {
            var block=flowchart.FindBlock(blockName);

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
