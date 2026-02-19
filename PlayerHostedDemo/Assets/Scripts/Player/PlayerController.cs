using UnityEngine;

namespace Riptide.Demos.PlayerHosted
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Player player;
        [SerializeField] private CharacterController controller;
        [SerializeField] private Transform camTransform;
        [SerializeField] private float gravity;
        [SerializeField] private float movementSpeed;
        [SerializeField] private float jumpHeight;

        private float gravityAcceleration;
        private float moveSpeed;
        private float jumpSpeed;

        private bool[] inputs;
        private float yVelocity;
        private bool didTeleport;

        private void OnValidate()
        {
            if (controller == null)
                controller = GetComponent<CharacterController>();

            if (player == null)
                player = GetComponent<Player>();

            Initialize();
        }

        private void Start()
        {
            Initialize();
            inputs = new bool[6];
        }

        private void Update()
        {
            // Sample inputs every frame and store them until they're sent. This ensures no inputs are missed because they happened between FixedUpdate calls
            if (Input.GetKey(KeyCode.W))
                inputs[0] = true;

            if (Input.GetKey(KeyCode.S))
                inputs[1] = true;

            if (Input.GetKey(KeyCode.A))
                inputs[2] = true;

            if (Input.GetKey(KeyCode.D))
                inputs[3] = true;

            if (Input.GetKey(KeyCode.Space))
                inputs[4] = true;

            if (Input.GetKey(KeyCode.LeftShift))
                inputs[5] = true;

            if (Input.GetKeyDown(KeyCode.X))
                SendSwitchActiveWeapon(WeaponType.none);

            if (Input.GetKeyDown(KeyCode.Alpha1))
                SendSwitchActiveWeapon(WeaponType.pistol);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                SendSwitchActiveWeapon(WeaponType.teleporter);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                SendSwitchActiveWeapon(WeaponType.laser);

            if (Input.GetMouseButtonDown(0))
                SendPrimaryUse();

            if (Input.GetKeyDown(KeyCode.R))
                SendReload();
        }

        private void Initialize()
        {
            gravityAcceleration = gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;
            moveSpeed = movementSpeed * Time.fixedDeltaTime;
            jumpSpeed = Mathf.Sqrt(jumpHeight * -2f * gravityAcceleration);
        }

        private void FixedUpdate()
        {
            Vector2 inputDirection = Vector2.zero;
            if (inputs[0])
                inputDirection.y += 1;

            if (inputs[1])
                inputDirection.y -= 1;

            if (inputs[2])
                inputDirection.x -= 1;

            if (inputs[3])
                inputDirection.x += 1;

            Move(inputDirection, inputs[4], inputs[5]);

            for (int i = 0; i < inputs.Length; i++)
                inputs[i] = false;
        }

        public void Teleport(Vector3 toPosition)
        {
            bool isEnabled = controller.enabled;
            controller.enabled = false;
            transform.position = toPosition;
            controller.enabled = isEnabled;

            didTeleport = true;
        }

        private void Move(Vector2 inputDirection, bool jump, bool sprint)
        {
            Vector3 moveDirection = camTransform.right * inputDirection.x + camTransform.forward * inputDirection.y;
            moveDirection *= moveSpeed;

            if (sprint)
                moveDirection *= 2f;

            if (controller.isGrounded)
            {
                yVelocity = 0f;
                if (inputs[4])
                    yVelocity = jumpSpeed;
            }
            yVelocity += gravity;

            moveDirection.y = yVelocity;
            controller.Move(moveDirection);

            SendMovement();
        }

        public void SendMovement()
        {
            Message message = Message.Create(MessageSendMode.Unreliable, MessageId.PlayerMovement);
            message.AddUShort(player.Id);
            message.AddUShort(NetworkManager.Singleton.ServerTick);
            message.AddVector3(transform.position);
            message.AddVector3(transform.forward);
            NetworkManager.Singleton.Client.Send(message);
        }

        private void SendSwitchActiveWeapon(WeaponType newType)
        {
            Message message = Message.Create(MessageSendMode.Reliable, MessageId.switchActiveWeapon);
            message.AddByte((byte)newType);
            NetworkManager.Singleton.Client.Send(message);
        }

        private void SendPrimaryUse()
        {
            NetworkManager.Singleton.Client.Send(Message.Create(MessageSendMode.Reliable, MessageId.primaryUse));
        }

        private void SendReload()
        {
            NetworkManager.Singleton.Client.Send(Message.Create(MessageSendMode.Reliable, MessageId.reload));
        }
    }
}
