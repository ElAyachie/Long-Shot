using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball_Spawn : MonoBehaviour
{
    public Color full;
    public Color trans;
    public float speed;
    public GameObject ball;
    public GameObject lastBall;

    public static bool ready;

    // Start is called before the first frame update
    void Start()
    {
        ready = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (lastBall == null)
        {
            ready = true;
        }
        if (ready)
        {
            Spawn();
        }
    }

    public void Spawn()
    {
        lastBall = Instantiate(ball, this.transform.position, this.transform.rotation);
        ready = false;
    }

}
