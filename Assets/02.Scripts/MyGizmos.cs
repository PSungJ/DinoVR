using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyGizmos : MonoBehaviour
{
    public enum TYPE { NORMAL = 0, WAYPOINT = 1 }
    private readonly string wayPointFie = "Enemy";
    public TYPE type = TYPE.NORMAL;

    public float _radius = 0.05f;
    public Color _color = Color.red;

    private void OnDrawGizmos() //콜백함수:씬화면에 색상이나 선을 그려주는 유니티 지원함수 
    {
        if (type == TYPE.NORMAL)
        {
            Gizmos.color = _color; //색상
            Gizmos.DrawSphere(transform.position, _radius);
            //원형으로 그린다(포지션, 반경)
        }
        else
        {
            Gizmos.color = _color;
            Gizmos.DrawIcon(transform.position + Vector3.up * 1.0f, wayPointFie, true);
            //위치  , 기즈모폴더안에 있는 이미지명, 스케일 적용 여부
            Gizmos.DrawWireSphere(transform.position, _radius);

        }
    }
}