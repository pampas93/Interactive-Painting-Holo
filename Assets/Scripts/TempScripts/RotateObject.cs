using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public Vector3 Rotation = new Vector3(10.0f, 0.0f, 0.0f);
    public float Speed = 5f;

    void Update()
    {
        transform.Rotate(Rotation * Time.deltaTime * Speed);
    }
}
