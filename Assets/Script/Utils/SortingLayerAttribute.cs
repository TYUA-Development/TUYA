using UnityEngine;

// int 필드(sortingLayerID 저장용)에 붙이면 SpriteRenderer의 Sorting Layer와 동일한
// 드롭다운으로 Inspector에 표시된다. 실제 드롭다운 처리는 Assets/Editor/SortingLayerAttributeDrawer.cs.
public class SortingLayerAttribute : PropertyAttribute
{
}
