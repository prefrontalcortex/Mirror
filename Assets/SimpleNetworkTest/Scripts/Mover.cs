using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Mover : NetworkBehaviour
{
    Vector3 mousePosOld;
    NetworkIdentity currentSelected;
    public float rotationSpeed = 10f;

    private void Start()
    {
        mousePosOld = Input.mousePosition;
    }

    void Update()
    {
        if (!hasAuthority) return;
        Vector3 mousePos = Input.mousePosition;
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos),out hit))
            {
                if (hit.collider.attachedRigidbody) currentSelected = hit.collider.attachedRigidbody?.GetComponent<NetworkIdentity>();
                var ni = currentSelected?.GetComponent<NetworkIdentity>();
                if (ni) CmdRequestAuthority(ni);
            }
        }
        if (Input.GetMouseButton(0) && currentSelected)
        {
            CmdRotateObject(currentSelected, mousePosOld.x - mousePos.x);
        }
        else currentSelected = null;
        mousePosOld = mousePos;
    }

    [Command]
    void CmdRequestAuthority(NetworkIdentity targetId)
    {
        if (targetId.connectionToClient != connectionToClient)
        {
            targetId.RemoveClientAuthority();
            targetId.AssignClientAuthority(base.connectionToClient);
        }
    }

    [Command]
    void CmdRotateObject(NetworkIdentity target, float _mov)
    {
        target.transform.Rotate(Vector3.up, _mov * Time.deltaTime * rotationSpeed);
    }
}
