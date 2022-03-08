﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public class NavMeshBehaviour : Behaviour { 

    public static readonly int MAX_REJECTION_SAMPLE_ATTEMPTS = 100;
    [Tooltip("Navmesh surfaces, allows the navmesh to be updated at runtime.")]
    public NavMeshSurface[] surfaces;

    public float walkRadius = 5f;
    public float rejectionRadius = 1f;
    [RangeAttribute(0f,1f), Tooltip("Probability of taking a random action instead of attempting to move to the target point.")]
    public float mistakeProb = 0.1f;

    [RangeAttribute(0f,180f), Tooltip("Angle +/- from forward direction to sample next target point.")]
    public float sampleAngle;
    [RangeAttribute(1f, 180f), Tooltip("Amount to increment the sample angle upon failing to find a valid target point.")]
    public float sampleAngleIncrement; 

    //public bool debug = false;

    private Vector3 targetPosition;
    private Vector3 currentPosition { get { return player.transform.position; }}

    private NavMeshPath path;
    private Vector3 nextPosition { get { return path.corners[1]; }}
    private Vector3 goalPosition { get { return path.corners[path.corners.Length-1]; }}
    private bool onNavMesh { get { 
        NavMeshHit hit;
        return NavMesh.SamplePosition(currentPosition, out hit, player.radius, NavMesh.AllAreas);
    }}

    // used to ensure heuristic updates are done properly with a decisionPeriod > 1
    private float time; 
    private bool isHeuristic = false;

    void Awake() {
        time = Time.time;
        player = transform.parent.gameObject.GetComponent<Player>();
        //TODO write missing exception if null...
    }

    void Start() {
        path = new NavMeshPath();
        BuildNavMesh();
        UpdateNavPath();
    }

    void BuildNavMesh() {
        foreach (NavMeshSurface surface in surfaces) {
            surface.BuildNavMesh();
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut) {  
        isHeuristic = true; // this heuristic was used to generate the action buffer...  
        float dt = Time.time - time; // estimated time between heuristic calls...
        time = Time.time;
        
        // none, forward, rotate_left, rotate_right
        var _actionsOut = actionsOut.DiscreteActions;
        _actionsOut[0] = 0; //default do nothing
        if (path.corners.Length == 0) {
            return; // there way not path found, just do nothing.
        }

        if (UnityEngine.Random.value < mistakeProb) {
            // do a random action...
            _actionsOut[0] = UnityEngine.Random.Range(0,3);
            return;
        }
        
        // check should rotate
        Vector3 position = player.transform.position;
        position.y = nextPosition.y; // rotation will happen in the xz plane
        Vector3 direction = (nextPosition - position).normalized;
        float angle = Vector3.SignedAngle(player.transform.forward, direction, Vector3.up);
        float rangle = Mathf.Sign(angle) * dt * player.angularSpeed; 
        
        if (Mathf.Abs(angle) > Mathf.Abs(rangle)) {
            // should rotate
            if (angle < 0) {
                _actionsOut[0] = 2;
            } else if (angle > 0) {
                _actionsOut[0] = 3;
            }
        } else {
            // move forward
            _actionsOut[0] = 1;
        }
        Debug.DrawLine(player.transform.position, player.transform.position + direction, Color.blue, dt);
        Debug.DrawLine(player.transform.position, player.transform.position + player.transform.forward, Color.green, dt);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers){
        // IGNORE ANY INCOMING ACTIONS IF THEY ARE NOT FROM HEURISTIC -- always use heuristic policy, this is to trick python into getting obsevations anyway...
        if (!isHeuristic) {
            //Debug.Log($"A:{actionBuffers.DiscreteActions[0]}");
            Heuristic(actionBuffers);
            //Debug.Log($"B:{actionBuffers.DiscreteActions[0]}"); 
        }
        isHeuristic = false;
        base.OnActionReceived(actionBuffers);
    }

    void Update() {
        BuildNavMesh(); // TODO maybe done always rebuild... its fine for small environments but not big ones!
        UpdateNavPath();
    }

    void UpdateNavPath() {
        if (onNavMesh) {
            //Debug.Log(Vector3.Distance(player.transform.position, goalPosition));
            if (path.corners.Length == 0 || Vector3.Distance(player.transform.position, goalPosition) < player.radius) {
                targetPosition = nextPoint();
            }
            NavMesh.CalculatePath(currentPosition, targetPosition, NavMesh.AllAreas, path);
            for (int i = 0; i < path.corners.Length - 1; i++) {
                Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);
            }
        } else {
            // the agent is no longer on the nav mesh, stop trying to find new target points!
            //Debug.Log("NOT ON NAVMESH");
            path = new NavMeshPath(); // create a new path, the previous one was probably bad so ditch it.
        }
    }

    void OnDrawGizmos() {
        if (path.corners.Length > 0) {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(nextPosition, .3f);
            Gizmos.DrawSphere(targetPosition, .5f);
            //Gizmos.color = new Color(1f,1f,1f,0.2f);
            //Gizmos.DrawSphere(player.transform.position, walkRadius);
        }
    }

    public class RejectionSamplingException : Exception {
        public RejectionSamplingException() : base("Maximum rejection sampling attempts reached... perhaps the walk radius is too large.") {}
    }

    Vector3 nextPoint() {
        float range = sampleAngle;
        for (int i = 0; i < MAX_REJECTION_SAMPLE_ATTEMPTS; i ++) {
            NavMeshHit hit;
            float r = Mathf.Min(range + i * sampleAngleIncrement, 180f);
            Vector3 rand = RandomSegmentPoint(UnityEngine.Random.Range(0, walkRadius), UnityEngine.Random.Range(-r,r));
            rand = player.transform.TransformPoint(rand);
            //Vector3 rand = transform.position + (UnityEngine.Random.insideUnitSphere * walkRadius);
            if (NavMesh.SamplePosition(rand, out hit, rejectionRadius, NavMesh.AllAreas)) {
                return hit.position; // a suitable point was found
            } // try again...
        }
        throw new RejectionSamplingException();
    }

    Vector3 RandomSegmentPoint(float radius, float angle){
        float rad = angle * Mathf.Deg2Rad;
        Vector3 position = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
        return position * radius;
    }
}
