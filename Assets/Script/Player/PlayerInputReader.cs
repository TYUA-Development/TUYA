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
        return KeyBindingSettings.IsKeyHeld(KeyBindingSettings.Aim);
    }

    public void ClearInput()
    {
        InputData = new PlayerInputData();
    }

    public void ReadInput()
    {
        PlayerInputData data = new PlayerInputData();

        float h = GetHorizontalInput();
        float v = Input.GetAxisRaw("Vertical");
        data.moveAxis = new Vector2(h, v);

        data.jumpPressed = KeyBindingSettings.IsKeyDown(KeyBindingSettings.Jump);
        data.dashPressed = Input.GetButtonDown("Dash");
        data.aimingPressed = IsAimingHeld();
        data.attackPressed = KeyBindingSettings.IsKeyDown(KeyBindingSettings.Shoot);

        InputData = data;
    }

    private float GetHorizontalInput()
    {
        bool leftHeld = KeyBindingSettings.IsKeyHeld(KeyBindingSettings.MoveLeft);
        bool rightHeld = KeyBindingSettings.IsKeyHeld(KeyBindingSettings.MoveRight);

        if (leftHeld == rightHeld)
            return 0f;

        return leftHeld ? -1f : 1f;
    }
}
