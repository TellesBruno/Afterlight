using System.Collections;
using UnityEngine;

/// <summary>
/// Mostra os ícones de arma (WeaponIcons) quando o jogador troca de arma,
/// mantém visível por <see cref="visibleDuration"/> segundos e depois faz
/// um fade suave até ficar invisível.
///
/// Setup:
///   1. Adicione um CanvasGroup no GameObject WeaponIcons (filho de PlayerUI).
///   2. Adicione este script no mesmo objeto que PlayerHUD (ou no Player).
///   3. Arraste as referências de WeaponManager e PlayerInputHandler no Inspector.
/// </summary>
public class WeaponIconsHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponManager      weaponManager;
    [SerializeField] private PlayerInputHandler inputHandler;

    [Header("Timing")]
    [Tooltip("Segundos que os ícones ficam totalmente visíveis após a troca")]
    [SerializeField] private float visibleDuration = 5f;
    [Tooltip("Duração em segundos do fade de visível para invisível")]
    [SerializeField] private float fadeDuration    = 1f;

    private CanvasGroup _iconsGroup;
    private Coroutine   _fadeCoroutine;

    void Start()
    {
        var root = GameObject.FindWithTag("PlayerUI");
        if (root != null)
        {
            var iconsTransform = root.transform.Find("WeaponIcons");
            if (iconsTransform != null)
                _iconsGroup = iconsTransform.GetComponent<CanvasGroup>();
        }

        if (_iconsGroup == null)
        {
            Debug.LogWarning("[WeaponIconsHUD] CanvasGroup não encontrado em PlayerUI/WeaponIcons. " +
                             "Verifique se o CanvasGroup foi adicionado ao GameObject WeaponIcons.");
            enabled = false;
            return;
        }

        // Começa invisível
        _iconsGroup.alpha = 0f;

        // Escuta a troca de arma concluída (ex.: adicionar arma no start)
        if (weaponManager != null)
            weaponManager.OnSwitchedToWeapon += _ => ShowIcons();
    }

    void OnDestroy()
    {
        if (weaponManager != null)
            weaponManager.OnSwitchedToWeapon -= _ => ShowIcons();
    }

    void Update()
    {
        if (inputHandler != null && inputHandler.GetWeaponSwitchInput() != 0)
            ShowIcons();
    }

    private void ShowIcons()
    {
        if (_iconsGroup == null) return;

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        _iconsGroup.alpha = 1f;
        _fadeCoroutine    = StartCoroutine(FadeOutAfterDelay());
    }

    private IEnumerator FadeOutAfterDelay()
    {
        yield return new WaitForSeconds(visibleDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed           += Time.deltaTime;
            _iconsGroup.alpha  = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        _iconsGroup.alpha = 0f;
        _fadeCoroutine    = null;
    }
}
