using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class State
{
    public interface IState
    {
        public void OnEnter(AIControllerBase c);

        public void UpdateState(AIControllerBase c);

        public void FixedUpdateState(AIControllerBase c);

        public void OnHurt(AIControllerBase c, WeaponsBase weapon, ShipBase attacker);

        public void OnExit(AIControllerBase c);
    }
}
