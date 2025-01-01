using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class State
{
    public interface IState
    {
        public void OnEnter(AIControllerBase c);

        public void UpdateState(AIControllerBase c);

        public void FixedUpdateState(AIControllerBase c);

        public void OnHurt(AIControllerBase c);

        public void OnExit(AIControllerBase c);
    }
}
