using UnityEngine;

public struct PlayerInputData
{
    public Vector2 moveAxis;
    public bool jumpPressed;
    public bool dashPressed;
    public bool attackPressed;
    public bool aimingPressed;
}

public class PlayerInputReader : MonoBehaviour
{
    public PlayerInputData InputData { get; private set; }

    public bool IsAimingHeld()
    {
        return Input.GetMouseButton(1) || Input.GetButton("Fire2");
    }

    public void ClearInput()
    {
        InputData = new PlayerInputData();
    }

    public void ReadInput()
    {
        PlayerInputData data = new PlayerInputData();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        data.moveAxis = new Vector2(h, v);

        data.jumpPressed = Input.GetButtonDown("Jump");
        data.dashPressed = Input.GetButtonDown("Dash");
        data.aimingPressed = IsAimingHeld();
        data.attackPressed = Input.GetButtonDown("Fire1");

        InputData = data;
    }
}
