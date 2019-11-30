using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberPadlockDigits : MonoBehaviour
{
    public enum RotateAround { X, Y, Z }

    [HideInInspector] public NumberPadlock numberPadlock;
    public NumberPadlock.DigitPrefix DigitPrefix = NumberPadlock.DigitPrefix.One;
    public RotateAround rotateAround = RotateAround.X;

    private Vector3 nextDigitRot;
    private Vector3 euler;
    private float velocity;

    [HideInInspector] public bool isUsable = true;

    void Start()
    {
        euler = transform.localEulerAngles;
        nextDigitRot = transform.localEulerAngles;
    }

    public void Interact()
    {
        if (!numberPadlock || !isUsable) return;

        numberPadlock.InteractDigit(DigitPrefix);

        if (rotateAround == RotateAround.X)
        {
            nextDigitRot.x += numberPadlock.DigitRotateAngle;
            nextDigitRot.y = 0;
            nextDigitRot.z = 0;
        }
        else if(rotateAround == RotateAround.Y)
        {
            nextDigitRot.x = 0;
            nextDigitRot.y += numberPadlock.DigitRotateAngle;
            nextDigitRot.z = 0;
        }
        else if (rotateAround == RotateAround.Z)
        {
            nextDigitRot.x = 0;
            nextDigitRot.y = 0;
            nextDigitRot.z += numberPadlock.DigitRotateAngle;
        }
    }

    void Update()
    {
        if (rotateAround == RotateAround.X)
        {
            euler.x = Mathf.SmoothDampAngle(euler.x, nextDigitRot.x, ref velocity, Time.deltaTime * numberPadlock.RotateSpeed);
        }
        else if (rotateAround == RotateAround.Y)
        {
            euler.y = Mathf.SmoothDampAngle(euler.y, nextDigitRot.y, ref velocity, Time.deltaTime * numberPadlock.RotateSpeed);
        }
        else if (rotateAround == RotateAround.Z)
        {
            euler.z = Mathf.SmoothDampAngle(euler.z, nextDigitRot.z, ref velocity, Time.deltaTime * numberPadlock.RotateSpeed);
        }

        transform.localEulerAngles = euler;
    }
}
