using Riptide.Demos.PlayerHosted;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Interpolator : MonoBehaviour
{
    [SerializeField] private float timeElapsed = 0f;
    [SerializeField] private float timeToReachTarget = 0.05f;
    [SerializeField] private float movementThreshold = 0.05f;
    [SerializeField] private Transform camTransform;


    private readonly List<TransformUpdate> futureTransformUpdates = new List<TransformUpdate>();

    private float squareMovementThreshold;
    private TransformUpdate to;
    private TransformUpdate from;
    private TransformUpdate previous;

    private void Start()
    {
        squareMovementThreshold = movementThreshold * movementThreshold;
        to = new TransformUpdate(NetworkManager.Singleton.ServerTick, false, transform.position, camTransform.forward);
        from = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, false, transform.position, camTransform.forward);
        previous = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, false, transform.position, camTransform.forward);
    }

    private void Update()
    {
        for (int i = 0; i < futureTransformUpdates.Count; i++)
        {
            if (NetworkManager.Singleton.ServerTick >= futureTransformUpdates[i].Tick)
            {
                if (futureTransformUpdates[i].IsTeleport)
                {
                    to = futureTransformUpdates[i];
                    from = to;
                    previous = to;
                    transform.position = to.Position;
                    camTransform.forward = to.Forward;
                }
                else
                {
                    previous = to;
                    to = futureTransformUpdates[i];
                    from = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, false, transform.position, camTransform.forward);
                }

                futureTransformUpdates.RemoveAt(i);
                i--;
                timeElapsed = 0f;
                if(to.Tick != from.Tick)
                    timeToReachTarget = (to.Tick - from.Tick) * Time.fixedDeltaTime;
            }
        }

        timeElapsed += Time.deltaTime;
        InterpolatePosition(timeElapsed / timeToReachTarget);
        InterpolateForward(timeElapsed / timeToReachTarget);
    }

    private void InterpolatePosition(float lerpAmount)
    {
        lerpAmount = Mathf.Clamp(lerpAmount, 0f, 2f);
        if ((to.Position - previous.Position).sqrMagnitude < squareMovementThreshold)
        {
            if (to.Position != from.Position)
                transform.position = Vector3.Lerp(from.Position, to.Position, lerpAmount);

            return;
        }

        Vector3 newPos = Vector3.LerpUnclamped(from.Position, to.Position, lerpAmount);
        if (lerpAmount > 4f)
            Debug.LogError("lerpAmount je v pici");


        // ošetri invalid values
        if (IsFiniteVector(newPos))
        {
            transform.position = newPos;
        }
        else
        {
            Debug.LogError($"InterpolatePosition | From: {from.Position}, To: {to.Position}, LerpAmount: {lerpAmount}");
        }
    }

    private bool IsFiniteVector(Vector3 v)
    {
        return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                 float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
    }

    private void InterpolateForward(float lerpAmount)
    {
        lerpAmount = Mathf.Clamp(lerpAmount, 0f, 2f);
        if ((to.Forward - previous.Forward).sqrMagnitude < squareMovementThreshold)
        {
            if (to.Forward!= from.Forward)
      
                camTransform.forward = Vector3.Slerp(from.Forward, to.Forward, lerpAmount);

            return;
        }

        camTransform.forward = Vector3.SlerpUnclamped(from.Forward, to.Forward, lerpAmount);
    }

    public void NewUpdate(ushort tick, bool isTeleport, Vector3 position, Vector3 forward)
    {
        if (tick <= NetworkManager.Singleton.InterpolationTick && !isTeleport)
            return;

        futureTransformUpdates.Sort(delegate (TransformUpdate x, TransformUpdate y)
        {
            return x.Tick.CompareTo(y.Tick);
        });

        for (int i = 0; i < futureTransformUpdates.Count; i++)
        {
            if (tick < futureTransformUpdates[i].Tick)
            {
                futureTransformUpdates.Insert(i, new TransformUpdate(tick, isTeleport, position, forward));
                return;
            }
        }

        futureTransformUpdates.Add(new TransformUpdate(tick, isTeleport, position, forward));
    }
}
