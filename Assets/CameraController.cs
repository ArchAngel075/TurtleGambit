using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Vector2 movement;
    public float flightChange;
    public Vector2 look;
    public bool lookEnabled = true;
    public float click;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.position += (this.gameObject.transform.forward * movement.y) * (Time.smoothDeltaTime*16);
        this.transform.position += (this.gameObject.transform.right * movement.x) * (Time.smoothDeltaTime * 16);
        this.transform.position += (this.gameObject.transform.up * flightChange) * (Time.smoothDeltaTime * 10);

        this.transform.Rotate(new Vector3(0,1,0) * (look.x * (Time.smoothDeltaTime * 20)) );
        this.transform.Rotate(new Vector3(1,0,0) * ((-look.y) * (Time.smoothDeltaTime * 20)) );
        Vector3 rotEuler = this.gameObject.transform.rotation.eulerAngles;
        rotEuler.z = 0;
        this.gameObject.transform.rotation = Quaternion.Euler(rotEuler);
    }

    public void OnPointToUI(InputAction.CallbackContext context)
    {

    }

    public void OnClick(InputAction.CallbackContext context)
    {
        // read the value for the "move" action each event call
        click = context.ReadValue<float>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // read the value for the "move" action each event call
        look = context.ReadValue<Vector2>();
    }

    public void OnLookEnable(InputAction.CallbackContext context)
    {
        // read the value for the "move" action each event call
        //look = context.ReadValue<float>();
    }

    public void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // read the value for the "move" action each event call
        movement = context.ReadValue<Vector2>();
    }

    public void OnFlight(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // read the value for the "move" action each event call
        flightChange = context.ReadValue<float>();
    }

}
