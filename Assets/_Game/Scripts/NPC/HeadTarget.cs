using UnityEngine;

public class HeadTarget : MonoBehaviour
{
    public Transform target;
    public Transform head;
    public float lookSpeed = 5.0f;

    void Update()
    {
        if (target != null && head != null)
        {
            Vector3 direction = target.position - head.position;
            direction.y = 0;

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            float angle = Quaternion.Angle(head.rotation, targetRotation);
            {
                head.rotation = Quaternion.Slerp(head.rotation, targetRotation, Time.deltaTime * lookSpeed);
            }
        }
    }
}