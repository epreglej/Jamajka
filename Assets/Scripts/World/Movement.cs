using UnityEngine;

public class Movement : MonoBehaviour
{
    public SquareManager s_manager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.gameObject.transform.position = s_manager.squares[0].gameObject.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
