using UnityEngine;

public class CameraPivotController : MonoBehaviour
{
    public Transform cameraTransform;
    public Vector3 pivotPoint = Vector3.zero;
    public float height = 5f;
    public float distance = 10f;
    public float rotationSpeed = 50f;

    private float currentRotationAngle;

    void Update()
    {
        // Rotate the camera around the pivot using user input
        // float horizontalInput = Input.GetAxis("Horizontal");
        currentRotationAngle += 1 * rotationSpeed * Time.deltaTime;

        // Calculate the new position of the camera
        Vector3 offset = new Vector3(0, height, -distance);
        Quaternion rotation = Quaternion.Euler(0, currentRotationAngle, 0);
        Vector3 newPosition = pivotPoint + rotation * offset;

        // Move and look at the pivot
        cameraTransform.position = newPosition;
        cameraTransform.LookAt(pivotPoint);
    }

    public void RotateCamera(){
        currentRotationAngle += 1 * rotationSpeed * Time.deltaTime;

        // Calculate the new position of the camera
        Vector3 offset = new Vector3(0, height, -distance);
        Quaternion rotation = Quaternion.Euler(0, currentRotationAngle, 0);
        Vector3 newPosition = pivotPoint + rotation * offset;

        // Move and look at the pivot
        cameraTransform.position = newPosition;
        cameraTransform.LookAt(pivotPoint);
    }

    public void SetPivot(Vector3 newPivot)
    {
        pivotPoint = newPivot;
    }

    
    public void AdjustHeightAndDistance(float newHeight, float newDistance)
    {
        height = newHeight;
        distance = newDistance;
    }
}
