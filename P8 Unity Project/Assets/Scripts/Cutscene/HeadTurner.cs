using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Cutscene
{
    public class HeadTurner : MonoBehaviour
    {
        private Rig myRig;

        [SerializeField] public Transform lookatTarget;

        void Awake()
        {
            myRig = GetComponent<Rig>();
        }

        /// <summary>
        /// Makes this robot look towards the target with a set weight
        /// </summary>
        /// <param name="targetPosition">The world space coordinates of the target</param>
        /// <param name="turnWeight">How much the robot looks towards the target, 1 being fully and 0 being not</param>
        public void LookAt(Vector3 targetPosition, float turnWeight)
        {
            lookatTarget.position = targetPosition;
            myRig.weight = turnWeight;
        }

        private void OnEnable()
        {
            HeadTurnManager.manager.Subscribe(this);
        }

        private void OnDisable()
        {
            HeadTurnManager.manager.Unsubscribe(this);
        }
    }
}
