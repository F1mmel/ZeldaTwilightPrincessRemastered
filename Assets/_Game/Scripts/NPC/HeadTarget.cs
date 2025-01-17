using UnityEngine;

public class HeadTarget : MonoBehaviour
{
    public Transform target; // The object the NPC should look at (e.g. the player)
    public Transform head;   // The Transform of the NPC's head
    public float lookSpeed = 5.0f; // The speed of turning towards the target

    void Update()
    {
        if (target != null && head != null)
        {
            Vector3 direction = target.position - head.position;
            direction.y = 0; // Optional: Keep the head level by ignoring vertical rotation

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Limit the head rotation within a specific angle range (e.g., 60 degrees)
            float angle = Quaternion.Angle(head.rotation, targetRotation);
            //if (angle < 60.0f) 
            {
                head.rotation = Quaternion.Slerp(head.rotation, targetRotation, Time.deltaTime * lookSpeed);
            }
        }
    }
}