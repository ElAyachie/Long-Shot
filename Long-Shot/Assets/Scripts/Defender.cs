
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Defender : MonoBehaviour
{
    public bool alive;
    public Vector2 direction;
    public Vector2 defenderSpawn;
    public float speed;
    public bool animating;

    // Start is called before the first frame update

    void Start()
    {   
        direction = transform.position;
        defenderSpawn = transform.position;
        direction.x = 2.5f;
        speed = Random.Range(.03f, .05f);
        alive = false;
        this.GetComponent<Animator>().enabled = false;
        animating = false;
        Goal.defenders.Add(this);
        
    }

    // Update is called once per frame
    void Update()
    {
        if (alive)
        {
            if (!animating)
            {
                this.GetComponent<Animator>().enabled = true;
                animating = true;
            }
            if (Vector2.Distance(transform.position, direction) > .2f)
            {
                transform.position = Vector2.MoveTowards(transform.position, direction, speed);
            }
            else
            {
                direction.x = direction.x * -1;
                speed = Random.Range(.03f, .09f);
            }
            if (direction.x > this.transform.position.x)
            {
                this.GetComponent<SpriteRenderer>().flipX = false;
            }
            else
            {
                this.GetComponent<SpriteRenderer>().flipX = true;
            }
        }
    }
}