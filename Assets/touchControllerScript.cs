using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class touchControllerScript : MonoBehaviour
{
    public FixedJoystick MoveJoystick;
    public FixedButton jumpButton;
    public FixedTouchField TouchField;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var fps = GetComponent<RigidbodyFirstPersonController>();

        fps.RunAxis = MoveJoystick.Direction;
        fps.JumpAxis = jumpButton.Pressed;
        fps.mouseLook.LookAxis= TouchField.TouchDist;
    }
}
