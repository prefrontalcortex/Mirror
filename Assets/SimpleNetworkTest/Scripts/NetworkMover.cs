using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkMover : NetworkBehaviour
{
    public float networkSyncInterval = 0.1f;


    // server
    Vector3 lastPosition;
    Quaternion lastRotation;
    Vector3 lastScale;

    // client
    public class DataPoint
    {
        public float timeStamp;
        // use local position/rotation for VR support
        public Vector3 localPosition;
        public Quaternion localRotation;
    }
    // interpolation start and goal
    DataPoint start;
    DataPoint goal;

    // local authority send time
    float lastClientSendTime;

    static void SerializeIntoWriter(NetworkWriter writer, Vector3 position, Quaternion rotation)
    {
        // serialize position
        writer.WriteVector3(position);

        // serialize rotation
        // writing quaternion = 16 byte
        // writing euler angles = 12 byte
        // -> quaternion->euler->quaternion always works.
        // -> gimbal lock only occurs when adding.
        Vector3 euler = rotation.eulerAngles;

        // write 3 floats = 12 byte
        writer.WriteSingle(euler.x);
        writer.WriteSingle(euler.y);
        writer.WriteSingle(euler.z);

    }

    public override bool OnSerialize(NetworkWriter writer, bool initialState)
    {
        SerializeIntoWriter(writer, transform.localPosition, transform.localRotation);
        return true;
    }

    void DeserializeFromReader(NetworkReader reader)
    {
        // put it into a data point immediately
        DataPoint temp = new DataPoint
        {
            // deserialize position
            localPosition = reader.ReadVector3()
        };

        // deserialize rotation

        // read 3 floats = 16 byte
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();
        temp.localRotation = Quaternion.Euler(x, y, z);


        temp.timeStamp = Time.time;


        // reassign start wisely
        // -> first ever data point? then make something up for previous one
        //    so that we can start interpolation without waiting for next.
        if (start == null)
        {
            start = new DataPoint
            {
                timeStamp = Time.time - syncInterval,
                localPosition = transform.localPosition,
                localRotation = transform.localRotation
            };
        }
        else
        {
            float oldDistance = Vector3.Distance(start.localPosition, goal.localPosition);
            float newDistance = Vector3.Distance(goal.localPosition, temp.localPosition);

            start = goal;

            if (Vector3.Distance(transform.localPosition, start.localPosition) < oldDistance + newDistance)
            {
                start.localPosition = transform.localPosition;
                start.localRotation = transform.localRotation;
            }
        }

        // set new destination in any case. new data is best data.
        goal = temp;
    }

    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        // deserialize
        DeserializeFromReader(reader);
    }

    [Command]
    void CmdClientToServerSync(byte[] payload)
    {
        // deserialize payload
        Debug.Log("[NetworkMover] sending from client to server!", this.gameObject);

        NetworkReader reader = new NetworkReader(payload);
        DeserializeFromReader(reader);

        // server-only mode does no interpolation to save computations,
        // but let's set the position directly
        if (isServer && !isClient)
            ApplyPositionRotation(goal.localPosition, goal.localRotation);

        // set dirty so that OnSerialize broadcasts it
        SetDirtyBit(1UL);
    }

    bool HasEitherMovedRotated()
    {
        bool moved = lastPosition != transform.localPosition;
        bool rotated = lastRotation != transform.localRotation;

        bool change = moved || rotated;
        if (change)
        {
            lastPosition = transform.localPosition;
            lastRotation = transform.localRotation;
        }
        return change;
    }

    public void SendRotation()
    {

    }

    public void ApplyPositionRotation(Vector3 localPos, Quaternion localRot)
    {
        transform.localPosition = localPos;
        transform.localRotation = localRot;
    }

    private void Update()
    {
        if (isServer)
        {
            // just use OnSerialize via SetDirtyBit only sync when position
            // changed. set dirty bits 0 or 1
            SetDirtyBit(HasEitherMovedRotated() ? 1UL : 0UL);
        }

        if (isClient)
        {
            if (!isServer)
            {
                if(Time.time - lastClientSendTime >= syncInterval)
                {
                    if (goal != null)
                    {
                        ApplyPositionRotation(goal.localPosition, goal.localRotation);
                    }
                    lastClientSendTime = Time.time;
                }
            }
        }
    }


}
