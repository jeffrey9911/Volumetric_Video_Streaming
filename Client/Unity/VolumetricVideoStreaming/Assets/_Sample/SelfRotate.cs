using UnityEngine;

public class SelfRotate : MonoBehaviour
{
    public Vector3 RotateSpeed = new Vector3(0, 0, 0);

    void Update()
    {
        //transform.Rotate(RotateSpeed * Time.deltaTime);

        transform.localEulerAngles += RotateSpeed * Time.deltaTime;
    }
}
