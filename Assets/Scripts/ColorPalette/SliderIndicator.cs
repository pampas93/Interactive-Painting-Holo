using System;
using UnityEngine;

public class SliderIndicator : MonoBehaviour
{
    public GameObject OtherIndicator;
    public Action<Vector3> OnCollision;

    void OnCollisionStay(Collision colliderObj)
    {
        if (colliderObj.gameObject != OtherIndicator) return;
        // Debug.Log("First point that collided: " + other.contacts[0].point);
        OnCollision?.Invoke(colliderObj.contacts[0].point);
    }
}
