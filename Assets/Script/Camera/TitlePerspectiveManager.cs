using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class TitlePerspectiveManager : MonoBehaviour
{
    public CameraPerspectiveData TitleScriptableObject;
    public List<Pair<GameObject, int>> imageInfo = new List<Pair<GameObject, int>>();
    private Camera mainCamera;
    public int CameraSpeed;

    private bool isZoom;

    private void Awake()
    {
        isZoom = true;
        mainCamera = Camera.main;
    }

    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0; i < TitleScriptableObject.datas.Count; i++)
        {
            GameObject ob = new GameObject("sprite" + i);
            SpriteRenderer sp = ob.AddComponent<SpriteRenderer>();
            sp.sprite = TitleScriptableObject.datas[i].ObjectSprite;
            sp.sortingOrder = TitleScriptableObject.datas[i].OrderValue;
            ob.transform.position = TitleScriptableObject.datas[i].Position;

            ob.layer = 3;

            imageInfo.Add(new Pair<GameObject, int>(ob, TitleScriptableObject.datas[i].Distance));
        }
    }

    // Update is called once per frame
    void Update()
    {
        float mouse01 = (Input.mousePosition.x / Screen.width) * 2f - 1f;

        Debug.Log(mouse01.ToString());

        if (isZoom)
        {
            mainCamera.orthographicSize += CameraSpeed * Time.deltaTime * 0.01f;
        }
        else 
        {
            mainCamera.orthographicSize -= CameraSpeed * Time.deltaTime * 0.01f;
        }

        // 이미지 별 카메라 무빙
        if (mainCamera.orthographicSize > 13.8f)
        {
            isZoom = false;
        }
        else if(mainCamera.orthographicSize < 12.5f)
        {
            isZoom = true;
        }

        if(mouse01 > -0.8f && mouse01 < 0.8f)
        {

        }
        else if(mouse01 < 0f && mainCamera.transform.position.x > -1.0f)
        {
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x - 1.0f * Time.deltaTime, 0, -10);
        }
        else if (mouse01 > 0f && mainCamera.transform.position.x < 1.0f)
        {
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x + 1.0f * Time.deltaTime, 0, -10);
        }
    }
}
