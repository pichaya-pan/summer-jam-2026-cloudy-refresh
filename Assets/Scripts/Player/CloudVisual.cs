using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class CloudVisual : MonoBehaviour
{
    [SerializeField] private CloudController cloudController;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite movingSprite;
    [SerializeField] private float movingThreshold = 0.2f;

    private SpriteRenderer sr;
    private Animator animator;
    private static readonly int IsMoving = Animator.StringToHash("isMoving");

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (cloudController == null)
            cloudController = GetComponent<CloudController>();
    }

    private void Update()
    {
        if (cloudController == null) return;

        bool moving = cloudController.Velocity.magnitude > movingThreshold;
        animator.SetBool(IsMoving, moving);
        
    }

}
