using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TripleShot : MonoBehaviour
{
    [SerializeField]
    private float _speed = 10.0f;

    private void Start()
    {
        
    }

    private void Update()
    {
        transform.Translate(Vector3.up * _speed * Time.deltaTime);

        if (transform.position.y >= 5.55f)
        {
            Destroy(this.gameObject);
        }
    }
}
