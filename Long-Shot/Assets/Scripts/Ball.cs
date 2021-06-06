using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Ball : MonoBehaviour
{
    public Vector2 mySpawn;
    public float speed;
    public bool gameStarted;
    public bool travel;
    // Start is called before the first frame update


    void Start()
    {
        mySpawn = this.transform.position;
        travel = false;
        speed = .3f;

    }

    // Update is called once per frame
    void Update()
    {

        if (travel)
        {
            mySpawn.y += 9;
            this.transform.position = Vector2.MoveTowards(this.transform.position, mySpawn, speed);
        }
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began || Input.GetMouseButtonDown(0))
        {
            this.GetComponent<Ball>().GetComponent<AudioSource>().Play();
            Goal.gameStarted = true;
            travel = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "defender")
        {
            PlayerPrefs.SetInt("LastScore", Goal.playerScore);
            collision.gameObject.GetComponent<Defender>().GetComponent<AudioSource>().Play();
            this.transform.parent = collision.transform;
            Death();
        }
        if (collision.gameObject.tag == "goal")
        {
            collision.gameObject.GetComponent<Goal>().GetComponent<AudioSource>().Play();
            collision.gameObject.GetComponent<Goal>().AddPoints();
            this.transform.parent = collision.transform;
            Ball_Spawn.ready = true;
            Destroy(gameObject);

        }
    }

    void Death()
    {
        travel = false;
        Restart();

    }
    void Restart()
    {
        Goal.playerScore = 0;
        Goal.defenders = new List<Defender>();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
