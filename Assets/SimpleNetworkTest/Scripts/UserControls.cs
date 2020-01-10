using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class UserControls : NetworkBehaviour
{
    public float speed = 2f;
    // Start is called before the first frame update
    void Start()
    {
        if (isLocalPlayer)
        {
            Camera.main.GetComponent<CamFollow>().target = transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasAuthority) return;
        Vector3 movementInput = new Vector3(
            Input.GetAxis("Horizontal"),
            0f,
            Input.GetAxis("Vertical")).normalized;

        if (movementInput.magnitude > 0f)
        {
            Vector3 movement = movementInput * speed * Time.deltaTime;
            transform.position = transform.position + movement;
            transform.rotation = Quaternion.LookRotation(movementInput, Vector3.up);
        }
    }
}
