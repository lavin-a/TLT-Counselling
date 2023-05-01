using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Anim : MonoBehaviour
{
    public GameObject Amy;
    public GameObject Steven;
    public GameObject John;
    public GameObject Susan;
    public GameObject Jin;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void JinAnim()
    {
        if (Jin != null)
        {
            Jin.GetComponent<Animator>().SetBool("isTalking", true);
            StartCoroutine(StopAnim(Jin));
        }
    }

    public void AmyAnim()
    {
        if (Amy != null)
        {
            Amy.GetComponent<Animator>().SetBool("isTalking", true);
            StartCoroutine(StopAnim(Amy));
        }
    }

    public void StevenAnim()
    {
        if (Steven != null)
        {
            Steven.GetComponent<Animator>().SetBool("isTalking", true);
            StartCoroutine(StopAnim(Steven));
        }
    }

    public void SusanAnim()
    {
        if (Susan != null)
        {
            Susan.GetComponent<Animator>().SetBool("isTalking", true);
            StartCoroutine(StopAnim(Susan));
        }
    }

    public void JohnAnim()
    {
        if (John != null)
        {
            John.GetComponent<Animator>().SetBool("isTalking", true);
            StartCoroutine(StopAnim(John));
        }
        
    }

    IEnumerator StopAnim(GameObject go)
    {
        yield return new WaitForSeconds(2);
        go.GetComponent<Animator>().SetBool("isTalking", false);
    }

}
