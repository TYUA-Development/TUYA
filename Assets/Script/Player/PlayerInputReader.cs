using System.Collections;
using System.Collections.Generic;
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
    public PlayerInputData InputData {  get; private set; }

    public void ReadInput()
    {
        // PlayerInputData는 Struct이기에 생성 비용이 굉장히 싸다. 때문에 new로 매 프레임 생성해도 성능에 영향을 거의 주지 않는다.
        PlayerInputData data = new PlayerInputData();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        data.moveAxis = new Vector2(h, v);

        data.jumpPressed = Input.GetButtonDown("Jump");
        data.dashPressed = Input.GetButtonDown("Dash");

        // 마우스 우클릭 지속
        bool rawFire2 = Input.GetMouseButton(1);
        data.aimingPressed = Input.GetButton("Fire2");
        // 마우스 좌클릭 클릭
        data.attackPressed = Input.GetButtonDown("Fire1");

        InputData = data;

        Debug.Log($"rawFire2={rawFire2}, mappedFire2={data.aimingPressed}");
    }
}
