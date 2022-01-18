using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] float moveSpeed;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float input = Input.GetAxisRaw("Horizontal");
        transform.Translate(Vector2.right* input* moveSpeed * Time.deltaTime);
    }
}
