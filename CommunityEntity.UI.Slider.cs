using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class CommunityEntity
{
#if CLIENT
    private class SliderEventHandler : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {
        private Slider _slider;
        public string onPointerUpCommand;
        public string onPointerDownCommand;
        private GameObject _toggleGo;

        void Start()
        {
            _slider = GetComponent<Slider>();
            if (!_slider)
            {
                Destroy(this);
            }
        }
        
        public void SetToggleGO(GameObject toggleGO)
        {
            _toggleGo = toggleGO;
            if (toggleGO)
            {
                toggleGO.SetActive(false);
            }
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_slider.interactable)
            {
                return;
            }
            
            if (_toggleGo)
            {
                _toggleGo.SetActive(false);
            }
            
            if (!string.IsNullOrEmpty(onPointerUpCommand))
            {
                ConsoleNetwork.ClientRunOnServer(onPointerUpCommand + " " + _slider.value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_slider.interactable)
            {
                return;
            }
            
            if (_toggleGo)
            {
                _toggleGo.SetActive(true);
            }
            
            if (!string.IsNullOrEmpty(onPointerDownCommand))
            {
                ConsoleNetwork.ClientRunOnServer(onPointerDownCommand + " " + _slider.value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }
#endif
}