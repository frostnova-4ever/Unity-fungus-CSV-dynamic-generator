using UnityEngine;

public class SuperSimpleMove : MonoBehaviour
{
    public float moveSpeed = 5f;
    void Update()
    {
        Vector3 dir = transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal");
        transform.Translate(dir.normalized * moveSpeed * Time.deltaTime);
    }
}