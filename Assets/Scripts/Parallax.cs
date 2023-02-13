using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] private float parallax;

    Material material;
    Vector2 offset;
    // Start is called before the first frame update
    void Awake()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        material = mr.material;
        offset = material.mainTextureOffset;

    }

    // Update is called once per frame
    void Update()
    {
        offset.x = transform.position.x / transform.localScale.x / parallax;
        offset.y = transform.position.y / transform.localScale.y / parallax;
        material.mainTextureOffset = offset;
        
    }
}
