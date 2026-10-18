using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LaDeudaDelTio.Bicicleta
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class LDLT_BicicletaMovimiento : MonoBehaviour
    {
        [Header("Controles de esta bicicleta")]
        public bool aceptarTeclado = true;
        [Min(0f)] public float velocidad = 3f;
        [Min(0f)] public float velocidadRapida = 5f;
        [Min(0f)] public float fuerzaSalto = 7f;
        public LayerMask capasSuelo = ~0;
        [Min(0.001f)] public float distanciaSuelo = 0.08f;

        private Rigidbody2D cuerpo;
        private CapsuleCollider2D colision;
        private Animator animador;
        private SpriteRenderer imagen;
        private readonly RaycastHit2D[] contactos = new RaycastHit2D[16];
        private float horizontal;
        private bool rapido;
        private bool saltoPendiente;
        private static readonly int Velocidad = Animator.StringToHash("Velocidad");
        private static readonly int EnSuelo = Animator.StringToHash("EnSuelo");

        private void Awake()
        {
            cuerpo = GetComponent<Rigidbody2D>();
            colision = GetComponent<CapsuleCollider2D>();
            animador = GetComponent<Animator>();
            imagen = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (!aceptarTeclado) return;
            horizontal = 0f;
            rapido = false;
#if ENABLE_INPUT_SYSTEM
            Keyboard teclado = Keyboard.current;
            if (teclado != null)
            {
                if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) horizontal -= 1f;
                if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) horizontal += 1f;
                rapido = teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;
                saltoPendiente |= teclado.spaceKey.wasPressedThisFrame;
                return;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            rapido = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            saltoPendiente |= Input.GetKeyDown(KeyCode.Space);
#endif
        }

        // Permite que el sistema de montar/desmontar entregue su propia entrada.
        public void EstablecerEntrada(float direccion, bool acelerar, bool saltar)
        {
            aceptarTeclado = false;
            horizontal = Mathf.Clamp(direccion, -1f, 1f);
            rapido = acelerar;
            saltoPendiente |= saltar;
        }

        private void FixedUpdate()
        {
            bool suelo = EstaEnSuelo() && cuerpo.linearVelocity.y <= 0.1f;
            Vector2 movimiento = cuerpo.linearVelocity;
            movimiento.x = horizontal * (rapido ? velocidadRapida : velocidad);
            if (saltoPendiente && suelo)
            {
                movimiento.y = fuerzaSalto;
                suelo = false;
            }
            saltoPendiente = false;
            cuerpo.linearVelocity = movimiento;
            if (Mathf.Abs(horizontal) > 0.01f) imagen.flipX = horizontal < 0f;
            animador.SetFloat(Velocidad, Mathf.Abs(movimiento.x));
            animador.SetBool(EnSuelo, suelo);
        }

        private bool EstaEnSuelo()
        {
            ContactFilter2D filtro = new ContactFilter2D();
            filtro.SetLayerMask(capasSuelo);
            filtro.useTriggers = false;
            int cantidad = colision.Cast(Vector2.down, filtro, contactos, distanciaSuelo, true);
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit2D contacto = contactos[i];
                if (contacto.collider != null && contacto.rigidbody != cuerpo && contacto.normal.y > 0.5f)
                    return true;
            }
            return false;
        }

        private void OnDisable()
        {
            horizontal = 0f;
            rapido = false;
            saltoPendiente = false;
            if (cuerpo != null)
                cuerpo.linearVelocity = new Vector2(0f, cuerpo.linearVelocity.y);
        }
    }
}
