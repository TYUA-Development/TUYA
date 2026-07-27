using System.Collections;
using UnityEngine;

// 오브젝트가 사라질 때 재생할 애니메이션을 Inspector에서 받아 레거시 Animation으로 재생하고,
// 재생이 끝나면 GameObject를 파괴한다. 다른 컴포넌트가 "이 오브젝트를 없애야 할 때"
// 이 컴포넌트가 있는지 확인하고 있으면 PlayAndDestroy()를 호출해 사용한다.
[RequireComponent(typeof(Animation))]
public class DisappearMethod : MonoBehaviour
{
    [SerializeField] private AnimationClip disappearClip;

    private Animation animationComponent;

    private void Awake()
    {
        animationComponent = GetComponent<Animation>();
    }

    public void PlayAndDestroy()
    {
        StartCoroutine(PlayAndDestroyRoutine());
    }

    private IEnumerator PlayAndDestroyRoutine()
    {
        if (disappearClip != null && animationComponent != null)
        {
            animationComponent.clip = disappearClip;
            animationComponent.Play(disappearClip.name);
            yield return new WaitForSeconds(disappearClip.length);
        }

        Destroy(gameObject);
    }
}
