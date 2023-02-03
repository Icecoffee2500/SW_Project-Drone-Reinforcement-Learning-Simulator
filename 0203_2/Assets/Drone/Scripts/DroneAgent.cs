using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using PA_DronePack;
//using System.Numerics;
using Vector3 = UnityEngine.Vector3;
//using Unity.VisualScripting;


public class DroneAgent : Agent
{
    private PA_DroneController dcoScript;
    public RayScript rayscript;


    public DroneSettings area;
    public GameObject goal;


    float preDist;

    private Transform agentTrans;
    private Transform goalTrans;

    private Rigidbody agent_Rigidbody;
    private RayPerceptionSensorComponent3D[] rayPerceptionSensor;
    private float restrictDistance;

    /// <summary>
    /// 초기화 작업을 위해 한번 호출되는 메소드
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        dcoScript = gameObject.GetComponent<PA_DroneController>();

        agentTrans = gameObject.transform;
        goalTrans = goal.transform;

        agentTrans.LookAt(goal.transform);  //시작하자마자 goal쪽을 바라보게 함

        agent_Rigidbody = gameObject.GetComponent<Rigidbody>();
        rayPerceptionSensor = gameObject.GetComponents<RayPerceptionSensorComponent3D>();

        MaxStep = 20000;

        Academy.Instance.AgentPreStep += WaitTimeInference;
        
        restrictDistance = Vector3.Magnitude(goalTrans.position - agentTrans.position) + 10f;
        //Debug.Log("startDistance : " + (restrictDistance - 10f));
    }

    /// <summary>
    /// 환경 정보를 관측 및 수집해 정책 결정을 위해 브레인에 전달하는 메소드
    /// </summary>
    /// <param name="sensor"></param>
    public override void CollectObservations(VectorSensor sensor)
    {
        //거리 벡터
        sensor.AddObservation(agentTrans.position - goalTrans.position);
        //속도 벡터
        sensor.AddObservation(agent_Rigidbody.velocity);
        //각 속도 벡터
        sensor.AddObservation(agent_Rigidbody.angularVelocity);
        //바닥 거리
        sensor.AddObservation(rayscript.distance);
    }

    /// <summary>
    /// RayPerceptionSensor3D를 통해 ray에 닿는 물체와 가장 가까운 거리 return
    /// </summary>
    /// <param name="rayComponent"></param>
    /// <returns></returns>
    private float[] MinRayCastDist(RayPerceptionSensorComponent3D[] rayComponent)
    {
        float[] min = { 100f, 100f };   // { staticObstacle, dynamicObstacle }
        GameObject goHit;

        for (int i = 0; i < rayComponent.Length; i++)
        {
            var rayOutputs = RayPerceptionSensor
                    .Perceive(rayComponent[i].GetRayPerceptionInput())
                    .RayOutputs;

            if (rayOutputs != null)     //raycast가 하나라도 측정될 경우
            {
                var lengthOfRayOutputs = RayPerceptionSensor
                        .Perceive(rayComponent[i].GetRayPerceptionInput())
                        .RayOutputs
                        .Length;

                for (int j = 0; j < lengthOfRayOutputs; j++)
                {
                    goHit = rayOutputs[j].HitGameObject;

                    if (goHit != null)  //한 줄기 레이저에 물체가 측정될 경우
                    {
                        var rayDirection = rayOutputs[j].EndPositionWorld - rayOutputs[j].StartPositionWorld;
                        var scaledRayLength = rayDirection.magnitude;
                        float rayHitDistance = rayOutputs[j].HitFraction * scaledRayLength;

                        if (goHit.CompareTag("StaticObstacle") && min[0] > rayHitDistance) min[0] = rayHitDistance;     //가장 가까운 정적 장애물과의 거리 update
                        if (goHit.CompareTag("DynamicObstacle") && min[1] > rayHitDistance) min[1] = rayHitDistance;     //가장 가까운 동적 장애물과의 거리거리 update
                        
                    }
                }
            }
        }
        return min;
    }


    /// <summary>
    /// 브레인(정책)으로 부터 전달 받은 행동을 실행하는 메소드
    /// </summary>
    /// <param name="actions"></param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-0.1f);

        var actions = actionBuffers.ContinuousActions;

        float moveX = Mathf.Clamp(actions[0], -1, 1f);
        float moveY = Mathf.Clamp(actions[1], -1, 1f);
        float moveZ = Mathf.Clamp(actions[2], -1, 1f);

        dcoScript.DriveInput(moveX);
        dcoScript.StrafeInput(moveY);
        dcoScript.LiftInput(moveZ);


        float distance = Vector3.Magnitude(goalTrans.position - agentTrans.position);   //target과 드론과의 거리
        float[] ObstacleDistance = MinRayCastDist(rayPerceptionSensor);   //장애물과의 거리를 ray perception sensor로 측정한 10개의 값 중 최소 거리
        float distfrombottom = rayscript.distance;  //바닥과의 거리 가져옴

        if (distance <= 10)    //목표지점 도달
        {
            //Debug.Log("Goal!!!!!(" + StepCount + ")");
            //SetReward(60f);
            SetReward(600f);
            EndEpisode();
        }

        
        else if (distance > restrictDistance)    //목표지점과 너무 멀어질 때
        {
            //Debug.Log(distance);
            //Debug.Log("it's too far!!" + StepCount);
            SetReward(-600f);
            EndEpisode();
        }
        
        else if(agentTrans.localPosition.x < 0 || agentTrans.localPosition.z < 0 || agentTrans.position.y < 0)   //훈련장 위로 넘어갈 때
        {
            //Debug.Log("it's out of bound!!" + StepCount);
            SetReward(-500f);
            EndEpisode();
        }

        else if (distfrombottom > 20 || distfrombottom < 3)
        {
            SetReward(-400f);
            EndEpisode();

        }

        else if (ObstacleDistance[0] < 1f)     //정적장애물과 부딪힐 경우
        {
            //Debug.Log("collapse!!" + StepCount);
            //Debug.Log(StepCount);
            SetReward(-300f);
            EndEpisode();
        }

        

        else if (ObstacleDistance[1] < 3f)  //동적장애물과 부딪힐 경우
        {
            SetReward(-300f);
            EndEpisode();
        }

        else    //목표지점에 가까이 갈 때
        {
            //Debug.Log(ObstacleDistance[0]);
            float reward = preDist - distance;  //최대 0.3034정도


            AddReward(2*reward);
            preDist = distance;
        }
    }


    /// <summary>
    /// 에피소드(학습단위)가 시작할때마다 호출
    /// </summary>
    public override void OnEpisodeBegin()
    {
        area.AreaSetting();
        preDist = Vector3.Magnitude(goalTrans.position - agentTrans.position);
        rayscript.distance = 5;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.CompareTag("DynamicObstacle") || collision.collider.CompareTag("StaticObstacle"))
        {
            Debug.Log("부딪힘");
            SetReward(-300);
            EndEpisode();
        }
    }

    /// <summary>
    /// 개발자(사용자)가 직접 명령을 내릴 때 호출하는 메소드(주로 테스트용도 or 모방학습에 사용)
    /// </summary>
    /// <param name="actionsOut"></param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionOut = actionsOut.ContinuousActions;

        continuousActionOut[0] = Input.GetAxis("Vertical");
        continuousActionOut[1] = Input.GetAxis("Horizontal");
        continuousActionOut[2] = Input.GetAxis("Mouse ScrollWheel");
    }
    
    public float DecisionWatingTime = 5f;
    float m_currentTime = 0f;

    public void WaitTimeInference(int action)
    {
        if(Academy.Instance.IsCommunicatorOn)
        {
            RequestDecision();
        }

        else
        {
            if(m_currentTime >= DecisionWatingTime)
            {
                m_currentTime = 0f;
                RequestDecision();
            }

            else
            {
                m_currentTime += Time.fixedDeltaTime;
            }
        }
    }

}
