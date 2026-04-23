using UnityEngine;

[RequireComponent(typeof(Trees))]
[RequireComponent(typeof(Animator))]
public class TreeHealthDisplay : MonoBehaviour
{
    private Trees tree;
    private Animator animator;
    private static readonly int StateTree = Animator.StringToHash("stateTree");

    private void Awake()
    {
        tree = GetComponent<Trees>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        int CurrentTreeState = (int)tree.CurrentWiltState;
        animator.SetInteger(StateTree, CurrentTreeState);
        //Debug.Log($"Tree health: {tree.CurrentWiltState}, {tree.Health}");
    }

}
