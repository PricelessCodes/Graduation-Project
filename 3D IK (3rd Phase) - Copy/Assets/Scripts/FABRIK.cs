using UnityEditor;
using UnityEngine;

//[ExecuteInEditMode]
public class FABRIK : MonoBehaviour
{
    //Chain length of bones
    public int ChainLength = 2;
    public Transform Target;
    public Transform Pole;

    //FABRIK Solver iterations per update
    [Header("Solver Parameters")]
    public int NumberOfIterations = 16;
    //Distance between End-effector and Target
    public float ValueOfDelta = 0.001f;
    
    [Range(0, 1)]
    public float StrengthOfStretching = 1f;


    protected float[] LengthOfBones; //Target to Origin
    protected float TotalLengthOfBones;
    protected Transform[] Bones;
    protected Vector3[] PositionsOfBones;
    protected Vector3[] StartDirectionSucc;
    protected Quaternion[] StartRotationBone;
    protected Quaternion StartRotationTarget;
    protected Transform Root;


    // Start is called before the first frame update
    void Awake()
    {
        Initializing();
    }

    void Initializing()
    {
        //Initialize Data Array
        Bones = new Transform[ChainLength + 1];
        PositionsOfBones = new Vector3[ChainLength + 1];
        LengthOfBones = new float[ChainLength];
        StartDirectionSucc = new Vector3[ChainLength + 1];
        StartRotationBone = new Quaternion[ChainLength + 1];
        
        //Search For Root and make it Base
        Root = transform;
        for (var i = 0; i <= ChainLength; i++)
        {
            if (Root == null)
                throw new UnityException("The chain value is longer than the ancestor chain!");
            Root = Root.parent;
        }

        //Initialize Target
        if (Target == null)
        {
            Target = new GameObject(gameObject.name + " Target").transform;
            SetPositionRootSpace(Target, GetPositionOfRoot(transform));
        }
        StartRotationTarget = GetRotationOfRoot(Target);


        //Initialize Bones
        var current = transform;
        TotalLengthOfBones = 0;
        for (var i = Bones.Length - 1; i >= 0; i--)
        {
            Bones[i] = current;
            StartRotationBone[i] = GetRotationOfRoot(current);

            if (i == Bones.Length - 1)
            {
                //End-effector
                StartDirectionSucc[i] = GetPositionOfRoot(Target) - GetPositionOfRoot(current);
            }
            else
            {
                //intermidiate joints (Bones in the middle)
                StartDirectionSucc[i] = GetPositionOfRoot(Bones[i + 1]) - GetPositionOfRoot(current);
                LengthOfBones[i] = StartDirectionSucc[i].magnitude;
                TotalLengthOfBones += LengthOfBones[i];
            }

            current = current.parent;
        }



    }

    // Update is called once per frame
    void LateUpdate()
    {
        Initializing();
        ResolveIK();
    }

    private void ResolveIK()
    {
        if (Target == null)
        {
            return;
        }

        if (LengthOfBones.Length != ChainLength)
        {
            Initializing();
        }

        //Get Bones
        for (int i = 0; i < Bones.Length; i++)
        {
            PositionsOfBones[i] = GetPositionOfRoot(Bones[i]);
        }

        var PositionOfTarget = GetPositionOfRoot(Target);
        var RotationOfTarget = GetRotationOfRoot(Target);

        //is Target possible to be reached ?
        if ((PositionOfTarget - GetPositionOfRoot(Bones[0])).sqrMagnitude >= TotalLengthOfBones * TotalLengthOfBones)
        {
            //Then Stretch
            var direction = (PositionOfTarget - PositionsOfBones[0]).normalized;
            //set everything after root
            for (int i = 1; i < PositionsOfBones.Length; i++)
                PositionsOfBones[i] = PositionsOfBones[i - 1] + direction * LengthOfBones[i - 1];
        }
        else
        {
            for (int i = 0; i < PositionsOfBones.Length - 1; i++)
                PositionsOfBones[i + 1] = Vector3.Lerp(PositionsOfBones[i + 1], PositionsOfBones[i] + StartDirectionSucc[i], StrengthOfStretching);

            for (int j = 0; j < NumberOfIterations; j++)
            {
                //Backward Reaching IK
                for (int i = PositionsOfBones.Length - 1; i > 0; i--)
                {
                    //Set End-effector To Target
                    if (i == PositionsOfBones.Length - 1)
                        PositionsOfBones[i] = PositionOfTarget;
                    else
                        PositionsOfBones[i] = PositionsOfBones[i + 1] + (PositionsOfBones[i] - PositionsOfBones[i + 1]).normalized * LengthOfBones[i]; //set in line on distance
                }

                //Forward Reaching IK
                for (int i = 1; i < PositionsOfBones.Length; i++)
                    PositionsOfBones[i] = PositionsOfBones[i - 1] + (PositionsOfBones[i] - PositionsOfBones[i - 1]).normalized * LengthOfBones[i - 1];

                //is End-effector close to Target ?
                if ((PositionsOfBones[PositionsOfBones.Length - 1] - PositionOfTarget).sqrMagnitude < ValueOfDelta * ValueOfDelta)
                    break;
            }
        }

        //Baised To Pole
        if (Pole != null)
        {
            var polePosition = GetPositionOfRoot(Pole);
            for (int i = 1; i < PositionsOfBones.Length - 1; i++)
            {
                var CreatedPlane = new Plane(PositionsOfBones[i + 1] - PositionsOfBones[i - 1], PositionsOfBones[i - 1]);
                var ProjectionOfPole = CreatedPlane.ClosestPointOnPlane(polePosition);
                var ProjectionOfBone = CreatedPlane.ClosestPointOnPlane(PositionsOfBones[i]);
                var TheAngleBetween = Vector3.SignedAngle(ProjectionOfBone - PositionsOfBones[i - 1], ProjectionOfPole - PositionsOfBones[i - 1], CreatedPlane.normal);
                PositionsOfBones[i] = Quaternion.AngleAxis(TheAngleBetween, CreatedPlane.normal) * (PositionsOfBones[i] - PositionsOfBones[i - 1]) + PositionsOfBones[i - 1];
            }
        }

        //Set Bones
        for (int i = 0; i < PositionsOfBones.Length; i++)
        {
            if (i == PositionsOfBones.Length - 1)
                SetRotationOfRoot(Bones[i], Quaternion.Inverse(RotationOfTarget) * StartRotationTarget * Quaternion.Inverse(StartRotationBone[i]));
            else
                SetRotationOfRoot(Bones[i], Quaternion.FromToRotation(StartDirectionSucc[i], PositionsOfBones[i + 1] - PositionsOfBones[i]) * Quaternion.Inverse(StartRotationBone[i]));
            SetPositionRootSpace(Bones[i], PositionsOfBones[i]);
        }
    }

    private Vector3 GetPositionOfRoot(Transform current)
    {
        if (Root == null)
            return current.position;
        else
            return Quaternion.Inverse(Root.rotation) * (current.position - Root.position);
    }

    private void SetPositionRootSpace(Transform current, Vector3 position)
    {
        if (Root == null)
            current.position = position;
        else
            current.position = Root.rotation * position + Root.position;
    }

    private Quaternion GetRotationOfRoot(Transform current)
    {
        if (Root == null)
            return current.rotation;
        else
            return Quaternion.Inverse(current.rotation) * Root.rotation;
    }

    private void SetRotationOfRoot(Transform current, Quaternion rotation)
    {
        if (Root == null)
            current.rotation = rotation;
        else
            current.rotation = Root.rotation * rotation;
    }

    void OnDrawGizmos()
    {
        var current = this.transform;
        for (int i = 0; i < ChainLength && current != null && current.parent != null; i++)
        {
            var scale = Vector3.Distance(current.position, current.parent.position) * 0.1f;
            Handles.matrix = Matrix4x4.TRS(current.position, Quaternion.FromToRotation(Vector3.up, current.parent.position - current.position), new Vector3(scale, Vector3.Distance(current.parent.position, current.position), scale));
            Handles.color = Color.green;
            Handles.DrawWireCube(Vector3.up * 0.5f, Vector3.one);
            current = current.parent;
        }
    }

}