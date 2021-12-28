using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class Animate : MonoBehaviour
{
    public Animator animator;

    // Update is called once per frame
    void Update()
    {    
        if (Input.GetKeyDown(KeyCode.A))
        {
            animator.SetBool("IsShootAnimating", true);
        }
        else if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
        {
            animator.SetBool("IsShootAnimating", false);
        }
    }
}
