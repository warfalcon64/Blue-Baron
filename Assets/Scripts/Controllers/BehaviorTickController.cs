using Unity.Behavior;
using UnityEngine;

[RequireComponent(typeof(BehaviorGraphAgent))]
public class BehaviorTickController : MonoBehaviour
{
    [SerializeField] private float tickInterval = 0.1f;

    private BehaviorGraphAgent agent;
    private float timer;

    private void Awake()
    {
        agent = GetComponent<BehaviorGraphAgent>();
    }

    private void Start()
    {
        agent.enabled = false;
        // Stagger agents so they don't all tick on the same frame
        timer = Random.Range(0f, tickInterval);
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            agent.Update();
            timer = tickInterval;
        }
    }
}
