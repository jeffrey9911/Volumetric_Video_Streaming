using UnityEngine;

public class SelfRotate : MonoBehaviour
{
    // Self rotate the object
    public float rotateSpeed = 10f;

    void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }
}
